<template>
  <form @submit.prevent="login" class="center">
    <h2 class="mb-3">Login</h2>
    <div class="input">
      <label for="email">Email address</label>
      <input
        v-model="user.username"
        class="form-control"
        type="text"
        name="email"
        placeholder="email@adress.com"
      />
    </div>
    <div class="input">
      <label for="password">Password</label>
      <input
        v-model="user.password"
        class="form-control"
        type="password"
        name="password"
        placeholder="password123"
      />
    </div>
    <div class="alternative-option mt-4">
      You don't have an account? <span @click="moveToRegister">Register</span>
    </div>
    <button type="submit" class="mt-4 btn-pers" id="login_button">Login</button>
  </form>
</template>

<script setup>
import { reactive } from "vue";

const user = reactive({
  username: "",
  password: "",
});

function login() {
  console.log("login func");
}
</script>

<style scoped></style>
