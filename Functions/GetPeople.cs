using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using ukg_wfm_sync.Services;

namespace ukg_wfm_sync.Functions;

public class GetPeople(GetPeopleService peopleService, AuthenticationService auth, KeyVaultService kv)
{
    private readonly GetPeopleService _peopleService = peopleService;
    private readonly AuthenticationService _auth = auth;
    private readonly KeyVaultService _kv = kv;

    [Function("GetPeople")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        Console.WriteLine("GetPeople Started");

        (var token, var baseUrl, var appKey) = await _auth.Authenticate(_kv);
        List<Models.PersonRecord> people = await _peopleService.Run(token, baseUrl, appKey);

        Console.WriteLine($"GetPeople Complete | Count: {people.Count}");

        HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { PeopleSaved = people.Count });

        return response;
    }
}
