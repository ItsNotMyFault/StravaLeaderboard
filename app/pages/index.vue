<template>
  <div>
    <NuxtRouteAnnouncer />
    <NuxtWelcome />
    <h1>Welcome to the Strava Leaderboard App</h1>
    <p>
      Connect your Strava account to view your activities and see how you rank
      against others!
    </p>
    <nuxt-link to="/leaderboard" class="btn"> Go to Leaderboard </nuxt-link>
  </div>
</template>

<script setup lang="ts">
import { useAuth } from "@/composables/useAuth";

const { accessToken } = useAuth();
</script>

<style scoped>
.btn {
  display: inline-block;
  padding: 10px 15px;
  background-color: #007bff;
  color: white;
  border-radius: 5px;
  text-decoration: none;
}
.btn:hover {
  background-color: #0056b3;
}
</style>
