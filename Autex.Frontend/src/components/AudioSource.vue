<template>
    <div class="input">
        <label for="audioInputDeviceSelect">Audio Input Device:</label>
        <!-- @change="selectAudioInputDevice" -->
        <select v-model="selectedAudioInputDeviceId" id="audioInputDeviceSelect">
            <option v-for="device in audioInputDevices" :key="device.deviceId" :value="device.deviceId">{{
                device.label
            }}
            </option>
        </select>
    </div>
    <div class="input">
        <label for="channel">Channel</label>
        <Channel class="form-control" id="channel" />
    </div>
    <div>
        <button @click="enabled = !enabled">
            {{ enabled? 'Stop': 'Start' }}
        </button>
    </div>

</template>

<script setup>
import { ref, onMounted, watch } from 'vue';
import { useDevicesList, useUserMedia } from '@vueuse/core';
import Channel from './Channel.vue';

const selectedAudioInputDeviceId = ref("");

const {
    audioInputs: audioInputDevices,
} = useDevicesList({
    requestPermissions: true,
    constraints: { audio: true, video: false }
});

const { stream, enabled } = useUserMedia({
    enabled: false,
    videoDeviceId: false,
    audioDeviceId: selectedAudioInputDeviceId
})

watch(stream, (newStream) => {
    if (newStream) {
        startStream(newStream, "")
    } else {
        console.log("stream " + stream)
    }
})

</script>

<script>
import { useWebSocket } from '@vueuse/core'

function buf2hex(buffer) { // buffer is an ArrayBuffer
  return [...new Uint8Array(buffer)]
      .map(x => x.toString(16).padStart(2, '0'))
      .join('');
}

function startStream(stream, channelId) {
    //const audioContext = new AudioContext();
    // todo: start session 
    console.log(stream)

    const { status, data, send, open, close, ws } = useWebSocket('wss://localhost:7285/api/Audio/ws')

    const mediaRecorder = new MediaRecorder(stream, {
        mimeType: "audio/webm; codecs=opus",
        audioBitsPerSecond: 128000
    })
    mediaRecorder.addEventListener('dataavailable', (ev) => {
        //const r = new Response(ev.data)
        ev.data.slice(0,10).arrayBuffer().then((val) => {
            //const arr = new Uint8Array(val)
            //console.log(arr[0].toString(16)+" "+arr[2].toString(16))
            //console.log(buf2hex(val))            
        })
        
        send(ev.data)
    })

    mediaRecorder.start(1000);
}
</script>