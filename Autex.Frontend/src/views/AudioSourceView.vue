<template>
  <div class="center">
    <AudioSource />
  </div>
</template>

<script setup>
import AudioSource from "../components/AudioSource.vue";

function startStream() {
  console.log("start stream");
}
</script>

<style lang="scss" scoped></style>
