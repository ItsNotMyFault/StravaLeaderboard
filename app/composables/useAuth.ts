import { useAuthStore } from "@/stores/auth";

export function useAuth() {
  const store = useAuthStore();

  function setAccessToken(token: string) {
    store.setAccessToken(token);
  }

  function getAccessToken() {
    return store.getAccessToken();
  }

  function clearAccessToken() {
    store.clearAccessToken();
  }

  return {
    setAccessToken,
    getAccessToken,
    clearAccessToken,
  };
}
