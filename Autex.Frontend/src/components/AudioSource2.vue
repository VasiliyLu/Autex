<template>
  <div>
    <label for="audioInputDeviceSelect">Audio Input Device:</label>
    <select
      v-model="selectedAudioInputDeviceId"
      @change="selectAudioInputDevice"
      id="audioInputDeviceSelect"
    >
      <option
        v-for="device in audioInputDevices"
        :key="device.deviceId"
        :value="device.deviceId"
      >
        {{ device.label }}
      </option>
    </select>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from "vue";

const audioInputDevices = reactive([]);
const selectedAudioInputDeviceId = ref("");

const fetchAudioInputDevices = async () => {
  await navigator.mediaDevices
    .getUserMedia({ audio: true })
    .then((stream) => {
      // The user has granted access to their audio input device
      // Do something with the audio stream
      //stream.active = false;
      //console.log(stream)
    })
    .catch((error) => {
      // Handle the error
      console.log(error);
    });
  const devices = await navigator.mediaDevices.enumerateDevices();
  audioInputDevices.splice(
    0,
    audioInputDevices.length,
    ...devices.filter((device) => device.kind === "audioinput")
  );
};

onMounted(() => {
  console.log("mounted");
  fetchAudioInputDevices();
});

const selectAudioInputDevice = () => {
  navigator.mediaDevices
    .getUserMedia({ audio: { deviceId: selectedAudioInputDeviceId.value } })
    .then((stream) => {
      // The user has granted access to the selected audio input device
      // Do something with the audio stream
      console.log(stream);
    })
    .catch((error) => {
      // Handle the error
    });
};
</script>
