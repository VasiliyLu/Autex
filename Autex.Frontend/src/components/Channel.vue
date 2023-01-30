<template>
  <ComboboxVue v-model="channel" :options="channels" />
</template>

<script setup>
import { emit } from "process";
import { ref, watch } from "vue";
import ComboboxVue from "./UI/Combobox.vue";
</script>

<style lang="scss" scoped></style>
