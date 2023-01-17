<template>
  <select :value="modelValue" @change="changeOption" class="form-control">
    <option disabled value="">Please select</option>
    <option v-for="item in options" :key="item.value" :value="item.value">
      {{ item.text }}
    </option>
  </select>
</template>

<script setup>
defineProps({
  modelValue: {
    type: String,
  },
  options: {
    type: Array,
  },
});
const emit = defineEmits(["update:modelValue"]);

function changeOption(event) {
  emit("update:modelValue", event.target.value);
}
</script>

<style lang="scss" scoped></style>
