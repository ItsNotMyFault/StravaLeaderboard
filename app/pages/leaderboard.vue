<template>
  <div class="p-6 max-w-3xl mx-auto">
    <div class="flex items-end gap-3 mb-4">
      <div>
        <label class="block text-sm">Start</label>
        <input v-model="start" type="date" class="border rounded px-2 py-1" />
      </div>
      <div>
        <label class="block text-sm">End</label>
        <input v-model="end" type="date" class="border rounded px-2 py-1" />
      </div>
      <button @click="load" class="px-3 py-2 border rounded">
        Load Leaderboard
      </button>
      <button @click="getActivity" class="px-3 py-2 border rounded">
        getActivity
      </button>
      <button @click="connectStrava" class="px-3 py-2 border rounded">
        Connect Strava
      </button>
    </div>

    <h2 class="mt-8 mb-4 text-xl font-bold text-orange-600">
      Athlete Leaderboard {{ LIMIT ? `(${LIMIT})` : "" }}
    </h2>
    <table class="w-full border-collapse mb-8">
      <thead>
        <tr class="text-left border-b">
          <th class="py-2">#</th>
          <th class="py-2">Athlete</th>
          <th class="py-2">Total KM</th>
          <th class="py-2 text-orange-700 font-bold">Weighted KM</th>
          <th class="py-2">Couple KM</th>
          <th class="py-2">Activities</th>
          <th class="py-2">Total Moving Time</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="(athlete, i) in athleteLeaderboard"
          :key="athlete.name"
          class="border-b"
        >
          <td class="py-2">{{ i + 1 }}</td>
          <td class="py-2 font-semibold">{{ athlete.name }}</td>
          <td class="py-2">{{ athlete.totalKm.toFixed(1) }}</td>
          <td class="py-2 text-orange-700 font-bold">
            {{ athlete.coupleWeightedKm.toFixed(1) }}
          </td>
          <td class="py-2">{{ athlete.coupleKm.toFixed(1) }}</td>
          <td class="py-2">{{ athlete.count }}</td>
          <td class="py-2">{{ formatTime(athlete.totalMovingTime) }}</td>
        </tr>
      </tbody>
    </table>

    <h2 class="mt-8 mb-4 text-xl font-bold text-orange-600">
      Recent Club Activities {{ LIMIT ? `(${LIMIT})` : "" }}
    </h2>
    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
      <div
        v-for="(activity, i) in activitiesRef"
        :key="i"
        class="relative rounded-lg shadow-md p-4 bg-white border border-orange-100 flex flex-col gap-2"
      >
        <!-- Weighted KM badge -->
        <span
          class="absolute top-2 right-2 bg-orange-500 text-white text-xs font-bold px-2 py-1 rounded shadow"
          title="Weighted KM"
        >
          {{ getWeightedKm(activity).toFixed(1) }} km
        </span>

        <div class="flex items-center gap-2">
          <span class="text-2xl">{{ getSportIcon(activity.sport_type) }}</span>
          <span class="font-semibold text-lg">{{ activity.name }}</span>
        </div>
        <div class="text-xs text-gray-500">
          {{
            activity?.start_date
              ? new Date(activity.start_date).toLocaleString()
              : "no date"
          }}
        </div>
        <div class="text-gray-700">
          <span class="font-medium"
            >{{ activity.athlete.firstname }}
            {{ activity.athlete.lastname }}</span
          >
          <span
            class="ml-2 px-2 py-1 rounded bg-orange-50 text-orange-700 text-xs"
            >{{ activity.sport_type }}</span
          >
          <div v-if="activity.isCouple" class="text-xs text-pink-600 font-bold">
            Couple Activity 💑
          </div>
        </div>
        <div class="flex gap-4 mt-2 text-sm">
          <span
            >Distance:
            <span class="font-bold"
              >{{ (activity.distance / 1000).toFixed(1) }} km</span
            ></span
          >
          <span
            >Elevation:
            <span class="font-bold"
              >{{ activity.total_elevation_gain }} m</span
            ></span
          >
        </div>
        <div class="flex gap-4 text-sm">
          <span
            >Moving Time:
            <span class="font-bold">{{
              formatTime(activity.moving_time)
            }}</span></span
          >
          <span
            >Elapsed:
            <span class="font-bold">{{
              formatTime(activity.elapsed_time)
            }}</span></span
          >
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useAuth } from "@/composables/useAuth";

const { getAccessToken, setAccessToken } = useAuth();

const start = ref(
  new Date(new Date().setDate(new Date().getDate() - 20))
    .toISOString()
    .slice(0, 10)
);
const end = ref(new Date().toISOString().slice(0, 10));
const rows = ref<{ athleteId: string; totalKm: number }[]>([]);
const loading = ref(false);

function connectStrava() {
  window.location.href = "http://localhost:5086/auth/strava/login";
}

// Handle OAuth redirect and save access token
onMounted(() => {
  const params = new URLSearchParams(window.location.search);
  const payloadRaw = params.get("payload");
  if (payloadRaw) {
    try {
      const payload = JSON.parse(decodeURIComponent(payloadRaw));
      if (payload.accesstoken) {
        setAccessToken(payload.accesstoken);
        window.history.replaceState(
          {},
          document.title,
          window.location.pathname
        );
      }
    } catch (e) {
      console.error("Invalid OAuth payload", e);
    }
  }
});

async function load() {
  loading.value = true;
  try {
    const q = new URLSearchParams({
      start: start.value,
      end: end.value,
    }).toString();
    const res = await $fetch(`http://localhost:5086/leaderboard?${q}`, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${getAccessToken()}`,
      },
    });
    console.log("res", res);
    rows.value = (res as any[]).sort((a, b) => b.totalKm - a.totalKm);
  } finally {
    loading.value = false;
  }
}

const activityId = 15586923826;
const clubId = "piedsIntenses";

const activitiesRef = ref();

async function getActivity() {
  const response = await fetch(
    `https://www.strava.com/api/v3/activities/${activityId}?include_all_efforts=true`,
    {
      method: "GET",
      headers: {
        Authorization: `Bearer ${getAccessToken()}`,
      },
    }
  );

  if (!response.ok) {
    console.error(
      "Error fetching activity:",
      response.status,
      await response.text()
    );
    return;
  }

  const data = await response.json();
  console.log("Activity data:", data);
}

const LIMIT = 150;

async function getRecentClubActivities() {
  const url = `https://www.strava.com/api/v3/clubs/${clubId}/activities?per_page=${LIMIT}`;

  try {
    const response = await fetch(url, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${getAccessToken()}`,
        "Content-Type": "application/json",
      },
    });

    if (!response.ok) {
      throw new Error(`Error ${response.status}: ${response.statusText}`);
    }

    const activities = await response.json();
    activitiesRef.value = activities;
    return activitiesRef.value;
  } catch (error) {
    console.error("Failed to fetch club activities:", error);
    return null;
  }
}

getRecentClubActivities().then((data) =>
  console.log("Recent Club Activities:", data)
);

getActivity();

function formatTime(seconds: number) {
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = seconds % 60;
  return `${h}h ${m}m ${s}s`;
}

function getSportIcon(sportType: string) {
  switch (sportType) {
    case "Ride":
      return "🚴";
    case "Run":
      return "🏃";
    case "Swim":
      return "🏊";
    case "Walk":
      return "🚶";
    case "Hike":
      return "🥾";
    case "Rowing":
      return "🚣";
    case "Kayaking":
      return "🛶";
    case "VirtualRide":
      return "🖥️🚴";
    case "StandUpPaddling":
      return "🏄";
    case "AlpineSki":
      return "⛷️";
    case "Snowboard":
      return "🏂";
    default:
      return "🎽";
  }
}

function getWeightedKm(activity: any) {
  const km = activity.distance / 1000;
  let weight = 1;
  if (activity.sport_type === "Ride") weight = 0.25;
  else if (waterSports.includes(activity.sport_type)) weight = 2;
  return km * weight * (activity.isCouple ? 2 : 1);
}

const waterSports = [
  "Swim",
  "Rowing",
  "Kayaking",
  "Canoeing",
  "StandUpPaddling",
];

const couplePairs = [
  { a: "alexis", b: "gaëlle" },
  { a: "gaëlle", b: "alexis" },
  { a: "william", b: "émilie" },
  { a: "émilie", b: "william" },
  { a: "émilie", b: "will" },
];

function normalize(str: string) {
  return str
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "") // Remove accents
    .replace(/\s+/g, ""); // Remove spaces
}

// Athlete leaderboard computed from activitiesRef
const athleteLeaderboard = computed(() => {
  if (!activitiesRef.value || !Array.isArray(activitiesRef.value)) return [];
  if (!clubMembers.value || !Array.isArray(clubMembers.value)) return [];

  const memberFirstNames = clubMembers.value
    .map((m) => m.firstname?.toLowerCase())
    .filter(Boolean);

  const map = new Map<
    string,
    {
      name: string;
      totalKm: number;
      coupleWeightedKm: number; // weighted km with couple multiplier
      coupleKm: number; // raw km for couple activities
      count: number;
      totalMovingTime: number;
    }
  >();

  for (const activity of activitiesRef.value) {
    const athleteFirst = activity.athlete.firstname?.toLowerCase() || "";
    const activityNameLower = activity.name?.toLowerCase() || "";

    // Couple rule: check if activity name mentions the couple pair
    const isCouple = couplePairs.some(
      (pair) =>
        normalize(athleteFirst) === normalize(pair.a) &&
        normalize(activityNameLower).includes(normalize(pair.b))
    );

    const name = `${activity.athlete.firstname} ${activity.athlete.lastname}`;
    if (!map.has(name)) {
      map.set(name, {
        name,
        totalKm: 0,
        coupleWeightedKm: 0,
        coupleKm: 0,
        count: 0,
        totalMovingTime: 0,
      });
    }
    const entry = map.get(name)!;
    const km = activity.distance / 1000;
    let weight = 1;
    if (activity.sport_type === "Ride") weight = 0.25;
    else if (waterSports.includes(activity.sport_type)) weight = 2;

    const weighted = km * weight;
    entry.totalKm += km;
    entry.coupleWeightedKm += isCouple ? weighted * 2 : weighted;
    entry.count += 1;
    entry.totalMovingTime += activity.moving_time;
    // Couple KM: sum raw km for couple activities only
    if (isCouple) entry.coupleKm += km;

    activity.isCouple = isCouple;
  }

  return Array.from(map.values()).sort(
    (a, b) => b.coupleWeightedKm - a.coupleWeightedKm
  );
});

const clubMembers = ref([]);

async function getClubMembers() {
  const url = `https://www.strava.com/api/v3/clubs/${clubId}/members`;

  try {
    const response = await fetch(url, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${getAccessToken()}`,
        "Content-Type": "application/json",
      },
    });

    if (!response.ok) {
      throw new Error(`Error ${response.status}: ${response.statusText}`);
    }

    const members = await response.json();
    clubMembers.value = members;
    return clubMembers.value;
  } catch (error) {
    console.error("Failed to fetch club members:", error);
    return null;
  }
}

// Example usage: fetch members on mount
onMounted(() => {
  getClubMembers().then((data) => console.log("Club Members:", data));
});
</script>

<style scoped>
.grid {
  margin-bottom: 2rem;
}
</style>
