using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using ukg_wfm_sync.Services;

namespace ukg_wfm_sync.Functions;

public class GetTimecards(GetTimecardsService timecardService, AuthenticationService auth, KeyVaultService kv)
{
    private readonly GetTimecardsService _timecardService = timecardService;
    private readonly AuthenticationService _auth = auth;
    private readonly KeyVaultService _kv = kv;

    [Function("GetTimecards")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        Console.WriteLine("GetTimecards Started");

        (var token, var baseUrl, var appKey) = await _auth.Authenticate(_kv);
        var totalSegments = await _timecardService.Run(token, baseUrl, appKey);

        Console.WriteLine($"GetTimecards Complete | Segments: {totalSegments}");

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { SegmentsProcessed = totalSegments });

        return response;
    }
}
