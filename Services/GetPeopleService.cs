using System.Data;
using System.Text;
using System.Text.Json;
using ukg_wfm_sync.Models;

namespace ukg_wfm_sync.Services;

public class GetPeopleService(
    IHttpClientFactory httpClientFactory,
    SqlService sqlService)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly SqlService _sqlService = sqlService;

    public async Task<List<PersonRecord>> Run(
        string token,
        string baseUrl,
        string appKey)
    {
        Console.WriteLine("GetPeopleService Started");

        HttpClient http = _httpClientFactory.CreateClient();

        var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/api/v1/commons/persons/apply_read");

        req.Headers.Add("Authorization", $"Bearer {token}");
        req.Headers.Add("appkey", appKey);

        req.Content = new StringContent(
                                 /*lang=json,strict*/
                                 """
            {
              "where": {},
              "index": 0,
              "count": 10000
            }
            """,
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage res = await http.SendAsync(req);

        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }

        JsonElement root = JsonSerializer.Deserialize<JsonElement>(json);

        if (!root.TryGetProperty("records", out JsonElement records))
        {
            throw new Exception("Property 'records' was not found in response.");
        }

        Console.WriteLine(
            $"GetPeopleService Complete | TotalElements: {root.GetProperty("totalElements").GetString()}");

        List<PersonRecord> people = [];

        foreach (JsonElement item in records.EnumerateArray())
        {
            people.Add(new PersonRecord
            {
                PersonNumber = item.TryGetProperty("personNumber", out JsonElement personNumber)
                    ? personNumber.GetString() ?? ""
                    : "",

                FirstName = item.TryGetProperty("firstName", out JsonElement firstName)
                    ? firstName.GetString() ?? ""
                    : "",

                LastName = item.TryGetProperty("lastName", out JsonElement lastName)
                    ? lastName.GetString() ?? ""
                    : ""
            });
        }

        Console.WriteLine(
            $"GetPeopleService Complete | PeopleReturned: {people.Count}");

        await _sqlService.ExecuteAsync(
            """
            TRUNCATE TABLE ukg_wfm.people;
            """);

        DataTable table = new();

        _ = table.Columns.Add("person_number", typeof(string));
        _ = table.Columns.Add("first_name", typeof(string));
        _ = table.Columns.Add("last_name", typeof(string));

        foreach (PersonRecord person in people)
        {
            _ = table.Rows.Add(
                person.PersonNumber,
                person.FirstName,
                person.LastName);
        }

        await _sqlService.BulkInsertAsync(
            "ukg_wfm.people",
            table);

        Console.WriteLine(
            $"People Bulk Insert Complete | Count: {people.Count}");

        return people;
    }
}
