using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Tevscare.Api.Tests;

public class ApiFlowTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public ApiFlowTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Register_rejects_a_short_password()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email = $"short-{Guid.NewGuid():N}@tevscare.test",
            password = "short",
            timezone = "Asia/Kolkata"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_can_register_track_and_read_budget_and_shopping()
    {
        var email = $"user-{Guid.NewGuid():N}@tevscare.test";
        var auth = await RegisterAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Access);
        using var request = Authorized(auth);

        var profile = await _client.PutAsJsonAsync("/api/profile", new
        {
            fullName = "Test User",
            age = 34,
            heightCm = 168,
            currentWeightKg = 74,
            targetWeightKg = 68,
            dietaryPreference = "Eggetarian",
            activityLevel = "Light",
            waterGoalMl = 2500,
            sleepGoalMinutes = 450,
            activityGoalMinutes = 30,
            timezone = "Asia/Kolkata",
            allergies = new[] { "peanuts" },
            foodPreferences = new[] { new { name = "idly", kind = "Like" } },
            wakeTime = "06:30",
            breakfastTime = "08:00",
            lunchTime = "13:00",
            dinnerTime = "19:30",
            sleepTime = "22:00"
        });
        var profileBody = await profile.Content.ReadAsStringAsync();
        if (profile.StatusCode != HttpStatusCode.OK)
        {
            throw new Xunit.Sdk.XunitException($"{(int)profile.StatusCode} {profileBody}");
        }

        var dashboard = await SendAsync(request, HttpMethod.Get, "/api/dashboard");
        var dashboardJson = await dashboard.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.True(dashboardJson.GetProperty("planDayNumber").GetInt32() >= 1);
        Assert.Contains("not a substitute", dashboardJson.GetProperty("disclaimer").GetString());

        var meals = dashboardJson.GetProperty("meals");
        Assert.True(meals.GetArrayLength() >= 4);
        var breakfast = meals.EnumerateArray().First(item => item.GetProperty("mealType").GetString() == "Breakfast");
        Assert.Equal("2 idly", breakfast.GetProperty("title").GetString());
        Assert.DoesNotContain(breakfast.GetProperty("items").EnumerateArray(), item => item.GetProperty("name").GetString()!.Contains("peanut", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(breakfast.GetProperty("items").EnumerateArray(), item => item.GetProperty("name").GetString()!.Contains("egg", StringComparison.OrdinalIgnoreCase));

        var water = await SendAsync(request, HttpMethod.Post, "/api/water", new { amountMl = 250 });
        var waterJson = await water.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(250, waterJson.GetProperty("summary").GetProperty("consumedMl").GetInt32());

        var weight = await SendAsync(request, HttpMethod.Post, "/api/weight", new { weightKg = 73.4, isMorning = true, note = "Morning" });
        Assert.Equal(HttpStatusCode.OK, weight.StatusCode);

        var mealId = breakfast.GetProperty("id").GetGuid();
        var mealLog = await SendAsync(request, HttpMethod.Post, "/api/meals/log", new
        {
            mealType = "Breakfast",
            status = "Completed",
            notes = "Ate the planned breakfast",
            mealId,
            items = new[] { new { name = "Idly", quantity = 2, unit = "piece" } }
        });
        Assert.Equal(HttpStatusCode.OK, mealLog.StatusCode);

        var activity = await SendAsync(request, HttpMethod.Post, "/api/activity", new { activityType = "Walk", durationMinutes = 30, completed = true });
        Assert.Equal(HttpStatusCode.OK, activity.StatusCode);
        var sleep = await SendAsync(request, HttpMethod.Post, "/api/sleep", new { durationMinutes = 450, quality = 4 });
        Assert.Equal(HttpStatusCode.OK, sleep.StatusCode);

        var budget = await SendAsync(request, HttpMethod.Get, "/api/budget");
        var budgetJson = await budget.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(675, budgetJson.GetProperty("dailyBudget").GetDecimal());
        Assert.True(budgetJson.GetProperty("plannedToday").GetDecimal() > 0);
        Assert.True(budgetJson.GetProperty("projectedPeriod").GetDecimal() > budgetJson.GetProperty("plannedToday").GetDecimal());

        var shopping = await SendAsync(request, HttpMethod.Get, "/api/shopping-list?days=15");
        var shoppingJson = await shopping.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(shoppingJson.GetProperty("lines").GetArrayLength() > 5);
        Assert.True(shoppingJson.GetProperty("totalKnownCost").GetDecimal() > 0);

        var notifications = await SendAsync(request, HttpMethod.Get, "/api/notifications/preferences");
        var notificationJson = await notifications.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Contains(notificationJson.GetProperty("schedule").EnumerateArray(), item => item.GetProperty("category").GetString() == "Water");

        var refreshed = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.Refresh });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
    }

    [Fact]
    public async Task Forgot_password_reset_round_trip_works_when_tokens_are_exposed_for_local_development()
    {
        var email = $"reset-{Guid.NewGuid():N}@tevscare.test";
        await RegisterAsync(email);
        var forgot = await _client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        var forgotJson = await forgot.Content.ReadFromJsonAsync<JsonElement>(Json);
        var token = forgotJson.GetProperty("developmentResetToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var reset = await _client.PostAsJsonAsync("/api/auth/reset-password", new { email, token, newPassword = "Reset1234" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Reset1234" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private async Task<(string Access, string Refresh)> RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Test1234",
            timezone = "Asia/Kolkata"
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return (json.GetProperty("accessToken").GetString()!, json.GetProperty("refreshToken").GetString()!);
    }

    private static HttpRequestMessage Authorized((string Access, string Refresh) auth) 
    {
        var message = new HttpRequestMessage();
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Access);
        return message;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage template, HttpMethod method, string url, object? body = null)
    {
        var message = new HttpRequestMessage(method, url);
        message.Headers.Authorization = template.Headers.Authorization;
        if (body is not null)
        {
            message.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(message);
    }
}
