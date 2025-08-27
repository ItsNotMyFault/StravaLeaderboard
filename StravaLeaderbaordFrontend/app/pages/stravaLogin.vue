<template>
  <div class="flex flex-col items-center justify-center min-h-screen">
    <h2
      class="text-xl font-bold mb-4 text-orange-600 cursor-pointer"
      @click="stravaLogin"
    >
      Strava Login
    </h2>
    <p v-if="error" class="text-red-600">{{ error }}</p>
    <p v-else-if="loading">Logging in...</p>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuth } from "@/composables/useAuth";

const { setAccessToken } = useAuth();
const router = useRouter();
const error = ref("");
const loading = ref(false);

const config = useRuntimeConfig();
const STRAVA_CLIENT_ID = config.public.STRAVA_CLIENT_ID;
const STRAVA_CLIENT_SECRET = config.public.STRAVA_CLIENT_SECRET;

console.log("STRAVA_CLIENT_ID:", STRAVA_CLIENT_ID);
console.log("STRAVA_CLIENT_SECRET:", STRAVA_CLIENT_SECRET);
const REDIRECT_URI = "http://localhost:3000/stravaLogin"; // must match Strava app settings

const stravaLogin = () => {
  const authUrl = `https://www.strava.com/oauth/authorize?client_id=${STRAVA_CLIENT_ID}&response_type=code&redirect_uri=${encodeURIComponent(
    REDIRECT_URI
  )}&approval_prompt=auto&scope=read,activity:read_all`;
  window.location.href = authUrl;
};

onMounted(async () => {
  const params = new URLSearchParams(window.location.search);
  const code = params.get("code");

  if (!code) {
    error.value = "No OAuth code found. Please login with Strava.";
    return;
  }

  loading.value = true;
  try {
    // Exchange code for access token
    const response = await fetch("https://www.strava.com/oauth/token", {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        client_id: STRAVA_CLIENT_ID,
        client_secret: STRAVA_CLIENT_SECRET,
        code,
        grant_type: "authorization_code",
        redirect_uri: REDIRECT_URI,
      }),
    });

    const data = await response.json();
    if (data.access_token) {
      setAccessToken(data.access_token);
      window.history.replaceState({}, document.title, window.location.pathname);
      router.replace("/leaderboard");
    } else {
      error.value = "Failed to get access token from Strava.";
      console.error(data);
    }
  } catch (e) {
    error.value = "OAuth error.";
    console.error(e);
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped></style>
