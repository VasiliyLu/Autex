import { ref, watch, computed } from 'vue';
import { useWebSocket } from "@vueuse/core";

export function useViewer() {
    const enabled = ref(false)
    const alternatives = ref([])
    const channelId = ref("0")

    function getUri() {
        return 'wss://localhost:7285/api/TextViewer/ws?channelId='
            + parseInt(channelId.value);
    }
    const url = ref(getUri)

    const { ws, data, status, open, close } = useWebSocket(url, {
        autoReconnect: {
            retries: 3,
            delay: 2000,
            onFailed() {
                console.log("onfailed emit")
                enabled.value = false;
                //alert('Failed to connect WebSocket after 3 retries')
            },
        },
        immediate: false,
        onDisconnected() {
            enabled.value = false;
            console.log("ws disconnected")
        }
    })
    watch(channelId, () => {
        //console.log("channelid changed")
        url.value = getUri()
        console.log("channelid changed. uri" + ws)
    })

    watch(enabled, (val) => {
        if (val === true) {
            console.log(`viewer started (channelId=${channelId.value})`)
            alternatives.value = []
            open()
        } else {
            console.log("stopping")
            close()
        }
    })

    watch(data, (val) => {
        console.log(val)
        const textEvent = JSON.parse(val)
        if ([3].includes(textEvent.EventType/*Final*/)) {
            console.log(textEvent.Alternatives[0])
            alternatives.value.push(textEvent.Alternatives[0])
        } else {
            //console.log(val)
        }
    })

    const resultText = computed(() => alternatives.value.join(' '))

    return { enabled, alternatives, resultText, channelId }
}