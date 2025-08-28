# Strava Club Dashboard (Nuxt 3 + Vue 3)

A **Nuxt 3** app that fetches and displays activities from a Strava club. Includes a leaderboard, activity weighting (rides, couple activities, water sports), and activity filtering.

![Dashboard Screenshot](docs/screenshot.png) <!-- Optional: add a screenshot if available -->

---

## **Features**

- **OAuth Token Handling** – Captures and stores Strava OAuth access tokens.
- **Club Activities Fetching** – Supports Strava pagination and adjustable LIMIT.
- **Athlete Leaderboard** – Sorts athletes by weighted distance.
- **Weighted Metrics**:
  - Ride = ×0.25
  - Water Sports = ×2
  - Couple Activity = ×2
- **Real-time LIMIT adjustment** with debounced API calls.
- **Responsive UI** with TailwindCSS.

---

## **Tech Stack**

- [Nuxt 3](https://nuxt.com/) (Vue 3 + Vite)
- [TypeScript](https://www.typescriptlang.org/)
- [TailwindCSS](https://tailwindcss.com/)
- [VueUse](https://vueuse.org/)
- [Strava API](https://developers.strava.com/)

---

## **Prerequisites**

- **Node.js** v18+  
- **Strava API Client** (`client_id`, `client_secret`)  
- **Strava OAuth Redirect URL**

---

## **Setup**

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/strava-club-dashboard.git
cd strava-club-dashboard
```

### 2. Install Dependencies

```bash
npm install
# or
yarn install
```

### 3. Configure Environment Variables

Create a `.env` file in the root directory:

```env
STRAVA_CLIENT_ID=your_client_id
STRAVA_CLIENT_SECRET=your_client_secret
NUXT_PUBLIC_STRAVA_REDIRECT_URL=http://localhost:3000
```

> **Note:** The app uses `useAuth()` composable to store the token in local storage.

### 4. Run Development Server

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser.

---

## **Usage**

1. Navigate to `/stravaLogin` and log in with Strava.
2. The app will store the `access_token` locally.
3. You can:
   - View **club members**
   - Adjust the **LIMIT** of activities
   - See a **weighted leaderboard**
   - Browse **recent activities**

---

## **API Endpoints Used**

- `GET /api/v3/clubs/{club_id}/activities`
- `GET /api/v3/clubs/{club_id}/members`

Authentication is done with:

```
Authorization: Bearer <access_token>
```

---

## **Build for Production**

```bash
npm run build
npm run preview
```

---

## **Project Structure**

```
components/        # UI components
composables/       # Custom hooks (e.g., useAuth)
pages/             # Nuxt pages (index.vue, stravaLogin.vue)
```

---

## **License**

MIT License © [Your Name]

---

### **Next Steps**
- Add **screenshots** for README visuals.
- Deploy to **Vercel** or **Netlify**.
- Set up **CI/CD pipeline** for automatic builds.
