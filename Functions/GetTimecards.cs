using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using ukg_wfm_sync.Services;

namespace ukg_wfm_sync.Functions;

public class GetTimecards(
    GetTimecardsService timecardService,
    AuthenticationService auth,
    KeyVaultService kv)
{
    private readonly GetTimecardsService _timecardService = timecardService;
    private readonly AuthenticationService _auth = auth;
    private readonly KeyVaultService _kv = kv;

    [Function("GetTimecards")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
        HttpRequestData req)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(req.Url.Query);

        var env = query["env"]?.ToUpperInvariant() ?? "UAT";

        Console.WriteLine(
            $"GetTimecards Started | Environment: {env}");

        (var token, var baseUrl, var appKey) =
            await _auth.Authenticate(_kv, env);

        var totalSegments =
            await _timecardService.Run(
                token,
                baseUrl,
                appKey);

        Console.WriteLine(
            $"GetTimecards Complete | Segments: {totalSegments}");

        HttpResponseData response =
            req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(new
        {
            Environment = env,
            SegmentsProcessed = totalSegments
        });

        return response;
    }
}
