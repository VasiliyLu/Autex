<template>
  <div class="input">
    <label for="channel">Channel</label>
    <Combobox v-model="channelId" :options="channels" class="form-control" id="channel" />
  </div>
  <div>
    <button @click="enabled = !enabled">
      {{ enabled? 'Stop': 'Start' }}
    </button>
  </div>
  <div class="SpeechKit_Text">
    {{ resultText }}
    <p id="anch"></p>
  </div>
</template>

<script setup>
import { useViewer } from '../hooks/useViewer'
import Combobox from './UI/Combobox.vue';
import { watch } from 'vue'
import { useChannels } from '../hooks/useChannels';

const { enabled, alternatives, resultText, channelId } = useViewer();
const channels = useChannels()

watch(resultText, ()=>{
  document.getElementById('anch').scrollIntoView(false)
})

</script>

<style scoped>
.SpeechKit_Text {
  font-size: 20px;
  font-family: "YS Text", "Helvetica Neue", "Arial", "Helvetica", sans-serif;
  overflow: auto;
  height: 300px;
}
</style>
