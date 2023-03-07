import { useDevicesList, useUserMedia, useWebSocket } from "@vueuse/core";
import { ref, watch } from 'vue';
import config from "@/config/config"

export function useIngestion() {
    const {
        audioInputs: audioInputDevices
    } = useDevicesList({
        requestPermissions: true,
        constraints: { audio: true, video: false }
    });

    const selectedAudioInputDeviceId = ref("");
    const channel = ref("-1")

    const { stream, enabled } = useUserMedia({
        enabled: false,
        videoDeviceId: false,
        audioDeviceId: selectedAudioInputDeviceId
    })

    watch(stream, (newStream) => {
        const interval = 1000;
        if (newStream) {
            console.log("start stream")
            const mediaRecorder = new MediaRecorder(newStream, {
                mimeType: "audio/webm; codecs=opus",
                audioBitsPerSecond: 128000
            })
            const { status, data, send, open, close, ws } = useWebSocket(`${config.WS_SCHEMA}://${config.BACKEND_HOST}:${config.BACKEND_PORT}/api/Audio/ws`, {
                autoReconnect: {
                    retries: 3,
                    delay: 2000,
                    onFailed() {
                        console.log("onfailed emit")
                        enabled.value = false;
                        //alert('Failed to connect WebSocket after 3 retries')
                    },
                },
                onDisconnected() {
                    enabled.value = false;
                }
            })
            send(JSON.stringify({
                channelId: parseInt(channel.value)
            }))

            mediaRecorder.addEventListener('dataavailable', (ev) => {
                send(ev.data);
            })

            mediaRecorder.onstop = () => {
                close()
            }

            mediaRecorder.start(500);
        } else {
            console.log("stop")
        }
    })

    return { channel, audioInputDevices, selectedAudioInputDeviceId, enabled }
}