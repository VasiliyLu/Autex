<template>
  <div class="center">
    <AudioSource3 />
  </div>
</template>

<script setup>
import AudioSource3 from "../components/AudioSource.vue";

function startStream() {
  console.log("start stream");
}
</script>

<style lang="scss" scoped></style>
