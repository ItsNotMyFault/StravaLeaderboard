<template>
  <div>
    Strava login here

    <nuxt-link to="/leaderboard" class="btn"> Go to Leaderboard </nuxt-link>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from "vue";
import { useRouter } from "vue-router";
import { useAuth } from "@/composables/useAuth";

const { setAccessToken } = useAuth();
const router = useRouter();

onMounted(() => {
  const params = new URLSearchParams(window.location.search);
  const payloadRaw = params.get("payload");

  try {
    if (payloadRaw) {
      const payload = JSON.parse(decodeURIComponent(payloadRaw));
      if (payload.accesstoken) {
        setAccessToken(payload.accesstoken);
        window.history.replaceState({}, document.title, window.location.pathname);
        // Redirect to leaderboard after setting token
        router.replace("/leaderboard");
      }
    }
  } catch (e) {
    console.error("Invalid OAuth payload", e);
  }
});
</script>

<style scoped></style>
