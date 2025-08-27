// Program.cs
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TokenStore>(); // demo in-memory store
builder.Services.AddScoped<StravaClient>();
builder.Services.AddCors();


var app = builder.Build();

app.UseCors(policy => policy
    .WithOrigins("http://localhost:3000")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());


string STRAVA_CLIENT_ID = builder.Configuration["Strava:ClientId"]!;
string STRAVA_CLIENT_SECRET = builder.Configuration["Strava:ClientSecret"]!;

const string APP_PUBLIC_URL = "http://localhost:5086"; // e.g., https://localhost:5243 in dev
const string REDIRECT_PATH = "/auth/strava/callback";
// string RedirectUri = $"";
string RedirectUri = $"{APP_PUBLIC_URL}{REDIRECT_PATH}";

// --- OAuth: kick off login (have an athlete click this) ---
app.MapGet("/auth/strava/login", (HttpContext ctx) =>
{
    // Ask for scopes needed to read activities
    // 'activity:read' or 'activity:read_all' if users want you to see private activities too
    var scopes = "read,activity:read";
    var url =
        $"https://www.strava.com/oauth/authorize?client_id={STRAVA_CLIENT_ID}" +
        $"&response_type=code&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
        $"&approval_prompt=autao&scope={Uri.EscapeDataString(scopes)}";
    return Results.Redirect(url);
});


app.MapGet("/healthz", (HttpContext ctx) =>
{

    return "it works";
});

// --- OAuth callback: exchange code -> tokens, save refresh token tied to your app user ---
app.MapGet("/auth/strava/callback", async (HttpContext ctx, IHttpClientFactory http, TokenStore store) =>
{
    var code = ctx.Request.Query["code"].ToString();
    if (string.IsNullOrEmpty(code)) return Results.BadRequest("Missing code");

    var client = http.CreateClient();
    var form = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = STRAVA_CLIENT_ID,
        ["client_secret"] = STRAVA_CLIENT_SECRET,
        ["code"] = code,
        ["grant_type"] = "authorization_code"
    });
    var resp = await client.PostAsync("https://www.strava.com/oauth/token", form);
    if (!resp.IsSuccessStatusCode)
        return Results.Problem($"Token exchange failed: {resp.StatusCode}");

    OAuthTokenExchangeResponse payload = JsonSerializer.Deserialize<OAuthTokenExchangeResponse>(
        await resp.Content.ReadAsStringAsync())!;

    // Save refresh token server-side
    store.Upsert(new ConnectedAthlete
    {
        AthleteId = payload.Athlete?.Id?.ToString() ?? "unknown",
        RefreshToken = payload.RefreshToken!,
        Scope = payload.Scope ?? ""
    });

    // Redirect to frontend with payload as query param
    var payloadObj = new
    {
        athleteId = payload.Athlete?.Id,
        accesstoken = payload.AccessToken,
        connected = true
    };
    var payloadJson = Uri.EscapeDataString(JsonSerializer.Serialize(payloadObj));
    var frontendUrl = $"http://localhost:3000/stravaLogin?payload={payloadJson}";
    return Results.Redirect(frontendUrl);
});


// --- Leaderboard for a window: sums distance (km) for all connected athletes ---
app.MapGet("/leaderboard", async (
    DateTime start, DateTime end, // ISO dates e.g. start=2025-07-22&end=2025-08-22
    StravaClient strava,
    TokenStore store) =>
{
    var results = new List<LeaderboardRow>();

    foreach (var a in store.All())
    {
        // Refresh access token and fetch activities for this athlete
        var token = await strava.RefreshAccessTokenAsync(a.RefreshToken);
        var activities = await strava.GetAthleteActivitiesAsync(
            token.AccessToken!, start, end);

        var km = activities.Sum(x => x.Distance / 1000.0); // Strava distance is meters
        results.Add(new LeaderboardRow
        {
            AthleteId = a.AthleteId,
            TotalKm = Math.Round(km, 2)
        });
    }

    // Sort desc by km
    results = results.OrderByDescending(r => r.TotalKm).ToList();
    return Results.Json(results);
});

app.Run();

// --- Types & helpers ---

record ConnectedAthlete
{
    public string AthleteId { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public string Scope { get; set; } = "";
}

class TokenStore
{
    private readonly Dictionary<string, ConnectedAthlete> _byAthlete =
        new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(ConnectedAthlete a) => _byAthlete[a.AthleteId] = a;
    public IEnumerable<ConnectedAthlete> All() => _byAthlete.Values;
}

class StravaClient
{
    private readonly IHttpClientFactory _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string TokenUrl = "https://www.strava.com/oauth/token";
    private const string ApiBase = "https://www.strava.com/api/v3";

    public StravaClient(IHttpClientFactory http) => _http = http;

    public async Task<TokenRefreshResponse> RefreshAccessTokenAsync(string refreshToken)
    {
        var client = _http.CreateClient();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = Program.STRAVA_CLIENT_ID,
            ["client_secret"] = Program.STRAVA_CLIENT_SECRET,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });
        var resp = await client.PostAsync(TokenUrl, form);
        resp.EnsureSuccessStatusCode();
        var t = JsonSerializer.Deserialize<TokenRefreshResponse>(
            await resp.Content.ReadAsStringAsync(), _json)!;
        return t;
    }

    public async Task<List<Activity>> GetAthleteActivitiesAsync(
        string accessToken, DateTime start, DateTime end)
    {
        // Strava expects epoch seconds (UTC) for 'after' and 'before'
        var after = new DateTimeOffset(DateTime.SpecifyKind(start, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var before = new DateTimeOffset(DateTime.SpecifyKind(end, DateTimeKind.Utc)).ToUnixTimeSeconds();


        var client = _http.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var all = new List<Activity>();
        int page = 1;
        const int perPage = 200; // max
        while (true)
        {
            var url = $"{ApiBase}/athlete/activities?after={after}&before={before}&page={page}&per_page={perPage}";
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var batch = JsonSerializer.Deserialize<List<Activity>>(
                await resp.Content.ReadAsStringAsync(), _json) ?? new();

            if (batch.Count == 0) break;
            all.AddRange(batch);
            if (batch.Count < perPage) break; // last page
            page++;
        }
        return all;
    }
}

// --- DTOs from Strava responses (minimal) ---
public sealed class OAuthTokenExchangeResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_at")] public long ExpiresAt { get; set; }
    [JsonPropertyName("athlete")] public Athlete? Athlete { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
}

public sealed class TokenRefreshResponse
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_at")] public long ExpiresAt { get; set; }
}

public sealed class Athlete
{
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("username")] public string? Username { get; set; }
    [JsonPropertyName("firstname")] public string? Firstname { get; set; }
    [JsonPropertyName("lastname")] public string? Lastname { get; set; }
}

public sealed class Activity
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("start_date")] public DateTime? StartDateUtc { get; set; }
    [JsonPropertyName("distance")] public double Distance { get; set; } // meters
    [JsonPropertyName("moving_time")] public int MovingTime { get; set; } // seconds
}

public sealed class LeaderboardRow
{
    public string AthleteId { get; set; } = default!;
    public double TotalKm { get; set; }
}

// Expose constants to StravaClient
partial class Program
{
    public static string STRAVA_CLIENT_ID => STRAVA_CLIENT_ID_CONST;
    public static string STRAVA_CLIENT_SECRET => STRAVA_CLIENT_SECRET_CONST;

    // store secrets safely in real apps (User Secrets, env vars, Key Vault, etc.)
    private const string STRAVA_CLIENT_ID_CONST = "YOUR_CLIENT_ID";
    private const string STRAVA_CLIENT_SECRET_CONST = "YOUR_CLIENT_SECRET";
}
