using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Speechkit.Stt.V3;
using static Speechkit.Stt.V3.Recognizer;
using Autex.Backend.Contracts;

namespace Autex.Backend.Transcriber;

public class YandexTranscriberService : ISttServiceClient
{
    //private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<YandexTranscriberService> _logger;
    private RecognizerClient? _client;
    private readonly Uri _endpointAddress;
    private readonly string _iamToken;
    private readonly int _callDeadline = 5000;
    private RecognizerClient RecognizerClient
    {
        get
        {
            _client ??= new RecognizerClient(MakeChannel());
            return _client;
        }
    }
    private readonly Mutex _callMutex = new(false);
    private AsyncDuplexStreamingCall<StreamingRequest, StreamingResponse>? _call;
    private Task? _readingTask;

    public event EventHandler<TextEventArgs>? TextEventHandler;
    public YandexTranscriberService(IConfiguration configuration, ILogger<YandexTranscriberService> logger)
    {
        _logger = logger;

        _endpointAddress = new Uri(configuration["YandexSpeechKit:Endpoint"] ?? "https://stt.api.cloud.yandex.net:443");
        _iamToken = configuration["YandexSpeechKit:iam"]!;
    }

    private GrpcChannel MakeChannel()
    {
        return GrpcChannel.ForAddress(this._endpointAddress/*, new GrpcChannelOptions { LoggerFactory = _loggerFactory }*/);
    }
    private Metadata MakeMetadata()
    {
        var metadata = new Metadata
        {
            { "authorization", $"Bearer {_iamToken}" },
            { "x-node-alias", "speechkit.stt.stable" }
        };
        //metadata.Add("x-data-logging-enabled", "true"); // 

        //String requestId = Guid.NewGuid().ToString();

        //metadata.Add("x-client-request-id", requestId); /* уникальный идентификатор запроса. Рекомендуем использовать UUID. 
        //Сообщите этот идентификатор технической поддержке, чтобы мы смогли найти конкретрный запрос в системе и помочь вам.*/
        //log.Information($"Metadata configured for request: {requestId}");
        return metadata;
    }

    private static StreamingOptions MakeStreamingOptions(AudioSettings audioSettings)
    {
        var streamingOptions = new StreamingOptions()
        {
            RecognitionModel = new RecognitionModelOptions
            {
                AudioFormat = audioSettings.AudioFormat switch
                {
                    AudioFormat.LPCM16 => new AudioFormatOptions
                    {
                        RawAudio = new()
                        {
                            AudioEncoding = RawAudio.Types.AudioEncoding.Linear16Pcm,
                            SampleRateHertz = audioSettings.SampleRate,
                            AudioChannelCount = audioSettings.ChannelCount
                        }
                    },
                    AudioFormat.OggOpus => new AudioFormatOptions
                    {
                        ContainerAudio = new ContainerAudio
                        {
                            ContainerAudioType = ContainerAudio.Types.ContainerAudioType.OggOpus
                        },
                    },
                    AudioFormat.WebMOpus => throw new NotImplementedException(),
                    AudioFormat.MP3 => throw new NotImplementedException(),
                    _ => throw new NotImplementedException()
                },
                LanguageRestriction = new LanguageRestrictionOptions
                {
                    RestrictionType = LanguageRestrictionOptions.Types.LanguageRestrictionType.Whitelist
                },
                AudioProcessingType = RecognitionModelOptions.Types.AudioProcessingType.RealTime,
                TextNormalization = new TextNormalizationOptions
                {
                    TextNormalization = TextNormalizationOptions.Types.TextNormalization.Enabled,
                    ProfanityFilter = true,
                    LiteratureText = false
                }
            }
        };
        streamingOptions.RecognitionModel.LanguageRestriction.LanguageCode.Add("ru-RU");
        return streamingOptions;
    }

    private AsyncDuplexStreamingCall<StreamingRequest, StreamingResponse> MakeStreamingCall()
    {
        return RecognizerClient.RecognizeStreaming(MakeMetadata()/*, DateTime.UtcNow.AddMilliseconds(_callDeadline)*/);
    }

    private async Task ReadAll()
    {
        if (_call == null) return;
        await foreach (var resp in _call.ResponseStream.ReadAllAsync())
        {
            switch (resp.EventCase)
            {
                case StreamingResponse.EventOneofCase.None:
                    break;
                case StreamingResponse.EventOneofCase.Partial:
                    this.TextEventHandler?.Invoke(this, new(new TextEventMessage
                    {
                        EventType = TextEventType.Partial,
                        Alternatives = resp.Partial.Alternatives.Select(alt => alt.Text).ToList()
                    }));
                    break;
                case StreamingResponse.EventOneofCase.Final:
                    TextEventHandler?.Invoke(this, new(new TextEventMessage
                    {
                        EventType = TextEventType.Final,
                        Alternatives = resp.Final.Alternatives.Select(alt => alt.Text).ToList()
                    }));
                    break;
                case StreamingResponse.EventOneofCase.EouUpdate:
                    break;
                case StreamingResponse.EventOneofCase.FinalRefinement:
                    TextEventHandler?.Invoke(this, new(new TextEventMessage
                    {
                        EventType = TextEventType.FinalRefinement,
                        Alternatives = resp.FinalRefinement.NormalizedText.Alternatives.Select(alt => alt.Text).ToList()
                    }));
                    break;
                case StreamingResponse.EventOneofCase.StatusCode:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    private void WriteAudioToStream(AudioSettings audioSettings, ReadOnlySpan<byte> chunk)
    {
        if (_call == null)
        {
            _call = MakeStreamingCall();
            _call.RequestStream.WriteAsync(new StreamingRequest()
            {
                SessionOptions = MakeStreamingOptions(audioSettings)
            }).Wait();
            // create read task
            _readingTask?.Dispose();
            _readingTask = Task.Run(ReadAll);
        }
        var streamingRequest = new StreamingRequest
        {
            Chunk = new()
            {
                Data = ByteString.CopyFrom(chunk)
            }
        };
        _call.RequestStream.WriteAsync(streamingRequest).Wait();
    }

    public void WriteChunk(AudioSettings audioSettings, ReadOnlySpan<byte> chunk)
    {
        bool locked = _callMutex.WaitOne(_callDeadline);
        if (locked)
        {
            // check sent size limit
            try
            {
                WriteAudioToStream(audioSettings, chunk);
            }
            catch (RpcException)
            {
                _call?.Dispose();
                _call = null;
                WriteAudioToStream(audioSettings, chunk);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _callMutex.ReleaseMutex();
            }
        }
    }
}