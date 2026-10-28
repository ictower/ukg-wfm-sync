using System.Net.Http.Headers;
using System.Text.Json;

namespace ukg_wfm_sync.Services;

public class AuthenticationService(IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<(string token, string baseUrl, string appKey)> Authenticate(KeyVaultService kv)
    {
        var baseUrl = Environment.GetEnvironmentVariable("UKG_WFM_BASE_URL_PROD")!;
        var username = await kv.GetSecret("ukg-wfm-username");
        var password = await kv.GetSecret("ukg-wfm-password");
        var clientId = await kv.GetSecret("ukg-wfm-clientid");
        var clientSecret = await kv.GetSecret("ukg-wfm-clientsecret");
        var appKey = await kv.GetSecret("ukg-wfm-appkey");

        HttpClient http = _httpClientFactory.CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/authentication/access_token");

        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        req.Content = new FormUrlEncodedContent(
        [
            new("username", username),
            new("password", password),
            new("client_id", clientId),
            new("client_secret", clientSecret),
            new("grant_type", "password"),
            new("auth_chain", "OAuthLdapService")
        ]);

        HttpResponseMessage res = await http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            Console.WriteLine($"Authentication Failed | StatusCode: {(int) res.StatusCode}");
            throw new Exception(json);
        }

        JsonElement root = JsonSerializer.Deserialize<JsonElement>(json);
        var token = root.GetProperty("access_token").GetString()!;

        Console.WriteLine("Authentication Complete");

        return (token, baseUrl, appKey);
    }
}
