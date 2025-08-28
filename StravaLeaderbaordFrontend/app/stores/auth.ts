import { defineStore } from "pinia";
import { useCookies } from "@vueuse/integrations/useCookies";

export const useAuthStore = defineStore("auth", {
  state: () => ({
    accessToken: useCookies().get("access_token") || null,
  }),
  actions: {
    setAccessToken(token: string | null) {
      this.accessToken = token;
      useCookies().set("access_token", token, { path: "/" });
    },
    getAccessToken() {
      this.accessToken = useCookies().get("access_token");
      return this.accessToken;
    },
    clearAccessToken() {
      this.accessToken = null;
      useCookies().remove("access_token", { path: "/" });
    },
  },
});
