<template>
  <div class="p-6 mx-auto">
    <div class="flex items-center gap-3 mb-4">
      <button @click="disconnectStrava" class="px-3 py-2 border rounded">
        Disconnect Strava
      </button>
      <div>
        {{ limitStatus }}
        <input class="border border-gray-300" v-model="LIMIT" @update:modelValue="debouncedFetchActivities"></input>
      </div>
    </div>

    <!-- Grid layout for leaderboard and modifiers -->
    <div v-if="loading" class="flex justify-center items-center h-64">
      <div class="loader ease-linear rounded-full border-8 border-t-8 border-gray-MAX_PER_PAGE h-16 w-16"></div>

    </div>
    <template v-if="!loading">
      <div class="grid grid-cols-1 lg:grid-cols-4 gap-6 mb-8">
        <!-- Athlete Leaderboard - takes 3 columns on large screens -->
        <div class="lg:col-span-3">
          <h2 class="mb-4 text-xl font-bold text-orange-600">
            Athlete Leaderboard {{ LIMIT ? `(${LIMIT})` : "" }}
          </h2>
          <div class="overflow-x-auto">
            <table class="w-full border-collapse">
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
                <tr v-for="(athlete, i) in athleteLeaderboard" :key="athlete.name" class="border-b">
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
          </div>
        </div>

        <!-- Sport Type Modifiers - takes 1 column on large screens -->
        <div class="lg:col-span-1">
          <h3 class="text-lg font-bold text-orange-600 mb-2">
            Sport Type Modifiers
          </h3>
          <div class="bg-white rounded shadow overflow-hidden">
            <table class="w-full border-collapse">
              <thead>
                <tr class="bg-orange-50 text-left">
                  <th class="py-2 px-3 text-sm">Sport Type</th>
                  <th class="py-2 px-3 text-sm">Modifier</th>
                </tr>
              </thead>
              <tbody>
                <tr class="border-b border-gray-100">
                  <td class="py-2 px-3 text-sm">Couple Activity</td>
                  <td class="py-2 px-3 text-sm font-semibold">×2</td>
                </tr>
                <tr class="border-b border-gray-100">
                  <td class="py-2 px-3 text-sm">Ride</td>
                  <td class="py-2 px-3 text-sm font-semibold">×0.25</td>
                </tr>
                <tr v-for="sport in waterSports" :key="sport" class="border-b border-gray-100">
                  <td class="py-2 px-3 text-sm">{{ sport }}</td>
                  <td class="py-2 px-3 text-sm font-semibold">×2</td>
                </tr>
                <tr>
                  <td class="py-2 px-3 text-sm">Other</td>
                  <td class="py-2 px-3 text-sm font-semibold">×1</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <h2 class="mt-8 mb-4 text-xl font-bold text-orange-600">
        Recent Club Activities {{ LIMIT ? `(${LIMIT})` : "" }}
      </h2>
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div v-for="(activity, i) in allActivities" :key="i"
          class="relative rounded-lg shadow-md p-4 bg-white border border-orange-100 flex flex-col gap-2">
          <!-- Weighted KM badge -->
          <span class="absolute top-2 right-2 bg-orange-500 text-white text-xs font-bold px-2 py-1 rounded shadow"
            title="Weighted KM">
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
            <span class="font-medium">{{ activity.athlete.firstname }}
              {{ activity.athlete.lastname }}</span>
            <span class="ml-2 px-2 py-1 rounded bg-orange-50 text-orange-700 text-xs">{{ activity.sport_type }}</span>
            <div v-if="activity.isCouple" class="text-xs text-pink-600 font-bold">
              Couple Activity 💑
            </div>
          </div>
          <div class="flex gap-4 mt-2 text-sm">
            <span>Distance:
              <span class="font-bold">{{ (activity.distance / 1000).toFixed(1) }} km</span></span>
            <span>Elevation:
              <span class="font-bold">{{ activity.total_elevation_gain }} m</span></span>
          </div>
          <div class="flex gap-4 text-sm">
            <span>Moving Time:
              <span class="font-bold">{{
                formatTime(activity.moving_time)
                }}</span></span>
            <span>Elapsed:
              <span class="font-bold">{{
                formatTime(activity.elapsed_time)
                }}</span></span>
          </div>
        </div>
      </div>

    </template>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted, computed, watch } from "vue";
import { useDebounceFn } from "@vueuse/core";
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

const disconnectStrava = () => {
  setAccessToken(null);
  navigateTo("/stravaLogin", { replace: true });
};

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

const clubId = "piedsIntenses";

const allActivities = ref<any[]>([]);

const activityCountSinceChallengeStart = computed(() => {
  return allActivities.value.findIndex(x => x.name.includes("Challenge STARTS TOMORROW"));
});

const limitStatus = computed(() => {
  if(activityCountSinceChallengeStart.value < 0) return 'Try a bigger LIMIT';
  if(allActivities.value?.length - activityCountSinceChallengeStart.value -1 === 0) return 'This is the perfect amount of activities!';
  if(activityCountSinceChallengeStart.value > 0) return`You can remove ${allActivities.value?.length - activityCountSinceChallengeStart.value -1} activities`;

});

const LIMIT = ref(150);

// Debounced fetch function
const debouncedFetchActivities = useDebounceFn(async () => {
  console.log('debounce', LIMIT.value);
  await fetchLIMITActivities();
}, 300); // 500ms debounce

watch(() => LIMIT.value, (newVal) => {
  console.log('newVal', newVal);
  LIMIT.value = newVal;
  debouncedFetchActivities();
});

const MAX_PER_PAGE = 200;//strava max per page api number

async function fetchLIMITActivities() {
  allActivities.value = []; // Reset activities before fetching
  const totalLimit = LIMIT.value;
  console.log('totalLimit', totalLimit);
  const pageCount = Math.ceil(totalLimit / MAX_PER_PAGE);
  console.log('pageCount', pageCount);
  for (let page = 1; page <= pageCount; page++) {
    if (page === pageCount) {
      // const remaining = (MAX_PER_PAGE - (totalLimit % MAX_PER_PAGE)) % MAX_PER_PAGE;
      const fullPageCount = Math.floor(totalLimit / MAX_PER_PAGE);
      const listedItemsPreviously = fullPageCount * MAX_PER_PAGE;
      const remaining = totalLimit -  (listedItemsPreviously);
      await getRecentClubActivities(page, remaining);
    } else {
      await getRecentClubActivities(page, MAX_PER_PAGE);
    }
  }
}

async function getRecentClubActivities(page: number = 1, limit = LIMIT.value) {
  console.log('limit', limit);
  const url = `https://www.strava.com/api/v3/clubs/${clubId}/activities?per_page=${limit}&page=${page}`;

  try {
    loading.value = true;
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
    allActivities.value = allActivities.value.concat(activities);
    return allActivities.value;
  } catch (error) {
    console.error("Failed to fetch club activities:", error);
    loading.value = false;
    return null;
  } finally {
    loading.value = false;
  }
}


fetchLIMITActivities().then(() =>
  console.log("Recent Club Activities:", allActivities.value)
);

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
  if (!allActivities.value || !Array.isArray(allActivities.value)) return [];
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

  for (const activity of allActivities.value) {
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
