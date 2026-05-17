using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlatformWellSync.Configuration;
using PlatformWellSync.Models;

namespace PlatformWellSync.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly ApiSettings _settings;
    private readonly ILogger<ApiService> _logger;

    public ApiService(HttpClient http, IOptions<ApiSettings> settings, ILogger<ApiService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> LoginAsync()
    {
        _logger.LogInformation("Logging in to API...");

        var payload = new
        {
            username = _settings.Username,
            password = _settings.Password
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await _http.PostAsync($"{_settings.BaseUrl}/Account/login", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // API returns a plain string token, not a JSON object
        // So we deserialize it directly as a string
        var token = JsonConvert.DeserializeObject<string>(json)
                    ?? throw new Exception("No token returned by login endpoint.");

        _logger.LogInformation("Login successful.");
        return token;
    }

    public async Task<List<Platform>> GetPlatformWellActualAsync(string token)
    {
        _logger.LogInformation("Fetching platform well data...");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.GetAsync($"{_settings.BaseUrl}/PlatformWell/GetPlatformWellActual");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        // Deserialize safely — missing keys return null, extra keys are ignored
        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,  // extra keys in API = no crash
            NullValueHandling = NullValueHandling.Ignore            // missing keys = no crash
        };

        var platforms = JsonConvert.DeserializeObject<List<Platform>>(json, settings)
                        ?? new List<Platform>();

        _logger.LogInformation("Fetched {Count} platforms from API.", platforms.Count);
        return platforms;
    }
}