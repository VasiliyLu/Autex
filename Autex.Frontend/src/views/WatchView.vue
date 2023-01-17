<template>
  <div class="center">
    <div class="input">
      <label for="channel">Channel</label>
      <Channel class="form-control" id="channel" />
    </div>
    <button
      @click.prevent="startWatch"
      type="submit"
      class="mt-4 btn-pers"
      id="login_button"
    >
      Start watch
    </button>
  </div>
</template>

<script setup>
import Channel from "../components/Channel.vue";
function startWatch() {}
</script>

<style lang="scss" scoped></style>
