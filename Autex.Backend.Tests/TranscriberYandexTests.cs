using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Speechkit.Stt.V3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Speechkit.Stt.V3.Recognizer;

namespace Autex.Backend.Tests
{
    public class TranscriberTests
    {
        private readonly IConfigurationRoot _configuration;

        private readonly ITestOutputHelper _testOutputHelper;

        public TranscriberTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            _configuration = new ConfigurationBuilder()
                .AddUserSecrets<TranscriberTests>()
                .Build();
        }

        //[Fact]
        public async Task TextReceived()
        {
            var iamToken = _configuration["Yandex:iam"];

            const string OggChunksDir = "pcm_chunks";

            var metadata = new Metadata
            {
                { "authorization", $"Bearer {iamToken}" },
                { "x-node-alias", "speechkit.stt.stable" }
            };
            //stt streaming options
            var streamingOptions = new StreamingOptions()
            {
                RecognitionModel = new RecognitionModelOptions
                {
                    AudioFormat = new AudioFormatOptions
                    {
                        //ContainerAudio = new ContainerAudio
                        //{
                        //    ContainerAudioType = ContainerAudio.Types.ContainerAudioType.OggOpus
                        //},
                        RawAudio = new ()
                        {
                            AudioEncoding = RawAudio.Types.AudioEncoding.Linear16Pcm,
                            SampleRateHertz = 48000,
                            AudioChannelCount = 1
                        }
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

            var channel = GrpcChannel.ForAddress("https://stt.api.cloud.yandex.net:443");
            var client = new RecognizerClient(channel);

            using var asyncDuplexStreamingCall = client.RecognizeStreaming(metadata, DateTime.UtcNow.AddSeconds(5));

            await asyncDuplexStreamingCall.RequestStream.WriteAsync(new StreamingRequest() { 
                SessionOptions = streamingOptions
            });

            // read text chunks in background task
            _testOutputHelper.WriteLine("Starting background task to receive messages");
            var readTask = Task<string>.Run(async () =>
            {
                string result = "";
                await foreach (var resp in asyncDuplexStreamingCall.ResponseStream.ReadAllAsync())
                {
                    _testOutputHelper.WriteLine(resp.ToString());
                    if (resp.EventCase == StreamingResponse.EventOneofCase.Final)
                    {
                        result = resp.Final.Alternatives.First().Text;
                    }
                }
                return result;
            });

            //write audio chunks
            _testOutputHelper.WriteLine("Starting to send messages");
            int no = 1;
            foreach (var oggFileName in Directory.EnumerateFiles(OggChunksDir))
            {
                var streamingRequest = new StreamingRequest();
                using var stream = new FileStream(oggFileName, FileMode.Open);
                streamingRequest.Chunk = new() { 
                    Data = ByteString.FromStream(stream) 
                };
                await asyncDuplexStreamingCall.RequestStream.WriteAsync(streamingRequest);
                Thread.Sleep(1000);
                if (no >= 4) break;
                no++;
            }

            _testOutputHelper.WriteLine("Disconnecting");
            await asyncDuplexStreamingCall.RequestStream.CompleteAsync();
            await readTask;
            Assert.Equal("раз два три четыре пять раз два три четыре", readTask.Result);
        }
    }
}
