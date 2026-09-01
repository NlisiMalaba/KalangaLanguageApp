using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kalanga.Domain;
using Kalanga.Domain.Enums;
using Kalanga.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kalanga.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class ApiRequestResponseCycleTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private KalangaWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new KalangaWebApplicationFactory(postgres.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        await _factory.SeedKalangaLanguageAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact(Timeout = 180_000)]
    public async Task Auth_catalog_sync_audio_upload_and_review_complete_over_http()
    {
        var languageId = WellKnownLanguages.KalangaId.Value;
        var password = "Password1!";
        var suffix = Guid.NewGuid().ToString("N");
        var adminEmail = $"admin-{suffix}@example.test";
        var learnerEmail = $"learner-{suffix}@example.test";
        var contributorEmail = $"contributor-{suffix}@example.test";
        var reviewerEmail = $"reviewer-{suffix}@example.test";

        await _factory.SeedAdminAsync(adminEmail, password);

        var health = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        var learnerId = await RegisterAsync(languageId, learnerEmail, password, "Learner");
        var contributorId = await RegisterAsync(languageId, contributorEmail, password, "Contributor");
        var reviewerId = await RegisterAsync(languageId, reviewerEmail, password, "Reviewer");

        var admin = await LoginAsync(languageId, adminEmail, password);
        Assert.Equal(Role.Admin, admin.Role);
        await ChangeRoleAsync(admin.AccessToken, contributorId, Role.Contributor);
        await ChangeRoleAsync(admin.AccessToken, reviewerId, Role.Reviewer);

        var contributor = await LoginAsync(languageId, contributorEmail, password);
        Assert.Equal(Role.Contributor, contributor.Role);
        var reviewer = await LoginAsync(languageId, reviewerEmail, password);
        Assert.Equal(Role.Reviewer, reviewer.Role);
        var learner = await LoginAsync(languageId, learnerEmail, password);
        Assert.Equal(Role.Learner, learner.Role);
        Assert.Equal(learnerId, learner.UserId);

        var created = await SendAsync(
            HttpMethod.Post,
            "/lessons",
            contributor.AccessToken,
            new
            {
                title = "At the market",
                level = "Beginner",
                category = "Everyday",
                isScenario = true,
                scenarioContext = "Buying tomatoes.",
                xpReward = 10,
            });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = await ReadJsonAsync(created);
        var lessonId = ReadId(createdBody.RootElement, "lessonId");

        var draftSaved = await SendAsync(
            HttpMethod.Put,
            $"/lessons/{lessonId}",
            contributor.AccessToken,
            new
            {
                title = "At the market",
                level = "Beginner",
                category = "Everyday",
                isScenario = true,
                scenarioContext = "Buying tomatoes.",
                xpReward = 10,
                phrases = new[]
                {
                    new
                    {
                        phraseId = (Guid?)null,
                        kalangaText = "Ndinoda tomato",
                        englishTranslation = "I want tomatoes",
                        sortOrder = 0,
                        variations = Array.Empty<object>(),
                    },
                },
                exercises = new[]
                {
                    new
                    {
                        exerciseId = (Guid?)null,
                        exerciseType = "Flashcard",
                        promptData = """{"kalanga_text":"Ndinoda tomato","prompt":"Ndinoda tomato"}""",
                        correctAnswer = "I want tomatoes",
                        sortOrder = 0,
                    },
                },
            });
        Assert.Equal(HttpStatusCode.OK, draftSaved.StatusCode);

        var draft = await SendAsync(HttpMethod.Get, $"/lessons/{lessonId}/draft", contributor.AccessToken);
        Assert.Equal(HttpStatusCode.OK, draft.StatusCode);
        using var draftBody = await ReadJsonAsync(draft);
        var phraseId = ReadId(draftBody.RootElement.GetProperty("lesson").GetProperty("phrases")[0], "phraseId");

        var mp3 = "ID3"u8.ToArray().Concat(Enumerable.Repeat((byte)0x11, 64)).ToArray();
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(mp3);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        form.Add(file, "file", "phrase.mp3");
        form.Add(new StringContent("1500"), "durationMs");
        form.Add(new StringContent(phraseId.ToString()), "phraseId");
        form.Add(new StringContent("Female"), "speakerGender");
        form.Add(new StringContent("Western"), "dialectLabel");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/audio/upload") { Content = form };
        uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", contributor.AccessToken);
        var uploaded = await _client.SendAsync(uploadRequest);
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        using var uploadedBody = await ReadJsonAsync(uploaded);
        Assert.Equal("PendingReview", uploadedBody.RootElement.GetProperty("status").GetString());
        Assert.StartsWith("https://cdn.test/", uploadedBody.RootElement.GetProperty("cdnUrl").GetString());

        var submitted = await SendAsync(HttpMethod.Post, $"/lessons/{lessonId}/submit", contributor.AccessToken);
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        using var submittedBody = await ReadJsonAsync(submitted);
        Assert.Equal("PendingReview", submittedBody.RootElement.GetProperty("status").GetString());

        var queue = await SendAsync(HttpMethod.Get, "/review/queue", reviewer.AccessToken);
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        using var queueBody = await ReadJsonAsync(queue);
        Assert.Contains(
            queueBody.RootElement.GetProperty("lessons").EnumerateArray(),
            item => ReadId(item, "lessonId") == lessonId);

        var approved = await SendAsync(HttpMethod.Post, $"/review/{lessonId}/approve", reviewer.AccessToken);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var approvedBody = await ReadJsonAsync(approved);
        Assert.Equal("Published", approvedBody.RootElement.GetProperty("status").GetString());

        var catalog = await SendAsync(HttpMethod.Get, "/lessons?isScenario=true", learner.AccessToken);
        Assert.Equal(HttpStatusCode.OK, catalog.StatusCode);
        using var catalogBody = await ReadJsonAsync(catalog);
        Assert.Contains(
            catalogBody.RootElement.GetProperty("lessons").EnumerateArray(),
            item => ReadId(item, "lessonId") == lessonId);

        var now = DateTimeOffset.UtcNow;
        var pushed = await SendAsync(
            HttpMethod.Post,
            "/sync/push",
            learner.AccessToken,
            new
            {
                clientOperationId = $"op-{suffix}",
                progress = new[]
                {
                    new
                    {
                        lessonId,
                        completedAt = now,
                        score = 90,
                        xpAwarded = 10,
                        updatedAt = now,
                    },
                },
                spacedRepetition = new[]
                {
                    new
                    {
                        phraseId,
                        variationId = (Guid?)null,
                        easeFactor = 2.6m,
                        intervalDays = 1,
                        repetitions = 1,
                        nextReviewAt = DateOnly.FromDateTime(now.UtcDateTime.AddDays(1)),
                        lastReviewedAt = now,
                        updatedAt = now,
                    },
                },
                gamification = new
                {
                    totalXp = 10,
                    currentStreak = 1,
                    longestStreak = 1,
                    lastActivityDate = DateOnly.FromDateTime(now.UtcDateTime),
                    xpDelta = 10,
                    updatedAt = now,
                },
            });
        Assert.Equal(HttpStatusCode.OK, pushed.StatusCode);
        using var pushedBody = await ReadJsonAsync(pushed);
        Assert.False(pushedBody.RootElement.GetProperty("idempotentReplay").GetBoolean());
        Assert.True(pushedBody.RootElement.GetProperty("syncVersion").GetInt64() >= 1);

        var replay = await SendAsync(
            HttpMethod.Post,
            "/sync/push",
            learner.AccessToken,
            new
            {
                clientOperationId = $"op-{suffix}",
                progress = Array.Empty<object>(),
                spacedRepetition = Array.Empty<object>(),
                gamification = (object?)null,
            });
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var replayBody = await ReadJsonAsync(replay);
        Assert.True(replayBody.RootElement.GetProperty("idempotentReplay").GetBoolean());

        var pulled = await SendAsync(HttpMethod.Get, "/sync/pull?since=0", learner.AccessToken);
        Assert.Equal(HttpStatusCode.OK, pulled.StatusCode);
        using var pulledBody = await ReadJsonAsync(pulled);
        Assert.Contains(
            pulledBody.RootElement.GetProperty("serverChanges").GetProperty("progress").EnumerateArray(),
            item => ReadId(item, "lessonId") == lessonId);
    }

    private async Task<Guid> RegisterAsync(Guid languageId, string email, string password, string displayName)
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/register",
            new { languageId, email, password, displayName },
            Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        return ReadId(body.RootElement, "userId");
    }

    private async Task<AuthSession> LoginAsync(Guid languageId, string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { languageId, email, password },
            Json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJsonAsync(response);
        var root = body.RootElement;
        return new AuthSession(
            ReadId(root, "userId"),
            Enum.Parse<Role>(root.GetProperty("role").GetString()!),
            root.GetProperty("accessToken").GetString()!);
    }

    private async Task ChangeRoleAsync(string adminToken, Guid userId, Role role)
    {
        var response = await SendAsync(
            HttpMethod.Put,
            $"/admin/users/{userId}/role",
            adminToken,
            new { role = role.ToString() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        }

        return await _client.SendAsync(request);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(json), $"Empty body for {(int)response.StatusCode} {response.RequestMessage?.RequestUri}");
        return JsonDocument.Parse(json);
    }

    private static Guid ReadId(JsonElement element, string name)
    {
        var property = element.GetProperty(name);
        if (property.ValueKind == JsonValueKind.String)
        {
            return Guid.Parse(property.GetString()!);
        }

        return property.GetProperty("value").GetGuid();
    }

    private sealed record AuthSession(Guid UserId, Role Role, string AccessToken);
}
