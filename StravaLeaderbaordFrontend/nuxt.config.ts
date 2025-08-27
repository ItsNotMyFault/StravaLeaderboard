// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: "2025-07-15",
  devtools: { enabled: true },
  modules: ["@pinia/nuxt", "@nuxtjs/tailwindcss"],
  runtimeConfig: {
    public: {
      STRAVA_CLIENT_ID: process.env.NUXT_STRAVA_CLIENT_ID,
      STRAVA_CLIENT_SECRET: process.env.NUXT_STRAVA_CLIENT_SECRET,
    },
  },
});
