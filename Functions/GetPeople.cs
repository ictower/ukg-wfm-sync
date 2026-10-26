using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Collections.Specialized;
using System.Net;
using System.Web;
using ukg_wfm_sync.Services;

namespace ukg_wfm_sync.Functions;

public class GetPeople(
    GetPeopleService peopleService,
    AuthenticationService auth,
    KeyVaultService kv)
{
    private readonly GetPeopleService _peopleService = peopleService;
    private readonly AuthenticationService _auth = auth;
    private readonly KeyVaultService _kv = kv;

    [Function("GetPeople")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get")]
        HttpRequestData req)
    {
        NameValueCollection query = HttpUtility.ParseQueryString(req.Url.Query);

        var env = query["env"]?.ToUpperInvariant() ?? "UAT";

        Console.WriteLine($"GetPeople Started | Environment: {env}");

        (var token, var baseUrl, var appKey) =
            await _auth.Authenticate(_kv, env);

        List<Models.PersonRecord> people =
            await _peopleService.Run(token, baseUrl, appKey);

        Console.WriteLine($"GetPeople Complete | Count: {people.Count}");

        HttpResponseData response =
            req.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(new
        {
            PeopleSaved = people.Count
        });

        return response;
    }
}
