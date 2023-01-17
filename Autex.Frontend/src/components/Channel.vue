<template>
  <ComboboxVue v-model="channel" :options="channels" />
</template>

<script setup>
import { ref, watch } from "vue";
import ComboboxVue from "./UI/Combobox.vue";

const channel = ref("");
const channels = ref([
  { value: "1", text: "BBC" },
  { value: "2", text: "MSC" },
]);

watch(channel, (nv) => {
  console.log(nv);
});
</script>

<style lang="scss" scoped></style>
