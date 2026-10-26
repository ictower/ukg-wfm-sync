using System.Net.Http.Headers;
using System.Text.Json;

namespace ukg_wfm_sync.Services;

public class AuthenticationService(IHttpClientFactory httpClientFactory)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<(string token, string baseUrl, string appKey)> Authenticate(KeyVaultService kv)
    {
        return await Authenticate(kv, null);
    }

    public async Task<(string token, string baseUrl, string appKey)> Authenticate(
        KeyVaultService kv,
        string? env)
    {
        env = env?.ToUpperInvariant() ?? "UAT";

        string baseUrl;
        string username;
        string password;
        string clientId;
        string clientSecret;
        string appKey;

        if (env == "PROD")
        {
            baseUrl = Environment.GetEnvironmentVariable("UKG_WFM_BASE_URL_PROD")!;

            username = await kv.GetSecret("ukg-wfm-username");
            password = await kv.GetSecret("ukg-wfm-password");
            clientId = await kv.GetSecret("ukg-wfm-clientid");
            clientSecret = await kv.GetSecret("ukg-wfm-clientsecret");
            appKey = await kv.GetSecret("ukg-wfm-appkey");
        }
        else
        {
            baseUrl = Environment.GetEnvironmentVariable("UKG_WFM_BASE_URL_UAT")!;

            username = await kv.GetSecret("ukg-wfm-uat-username");
            password = await kv.GetSecret("ukg-wfm-uat-password");
            clientId = await kv.GetSecret("ukg-wfm-uat-clientid");
            clientSecret = await kv.GetSecret("ukg-wfm-uat-clientsecret");
            appKey = await kv.GetSecret("ukg-wfm-uat-appkey");
        }

        HttpClient http = _httpClientFactory.CreateClient();

        var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/api/authentication/access_token");

        req.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

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
            Console.WriteLine(
                $"Authentication Failed | Environment: {env} | StatusCode: {(int) res.StatusCode}");

            throw new Exception(json);
        }

        JsonElement root = JsonSerializer.Deserialize<JsonElement>(json);

        var token = root.GetProperty("access_token").GetString()!;

        Console.WriteLine($"Authentication Complete | Environment: {env}");

        return (token, baseUrl, appKey);
    }
}
