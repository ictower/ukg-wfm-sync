using System.Data;
using System.Text;
using System.Text.Json;
using ukg_wfm_sync.Models;

namespace ukg_wfm_sync.Services;

public class GetTimecardsService(IHttpClientFactory httpClientFactory, SqlService sqlService)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly SqlService _sqlService = sqlService;

    public async Task<long> Run(string token, string baseUrl, string appKey)
    {
        DataTable employeeTable = await _sqlService.QueryAsync("""
            SELECT person_number
            FROM ukg_wfm.raw_people
            ORDER BY person_number;
            """);

        List<string> employeeNumbers = [.. employeeTable.Rows.Cast<DataRow>().Select(x => x["person_number"].ToString()!)];

        Console.WriteLine($"Employees Loaded | Count: {employeeNumbers.Count}");

        long totalSegments = 0;

        for (var year = 2024; year <= DateTime.Today.Year; year++)
        {
            var startDate = $"{year}-01-01";
            var endDate = year == DateTime.Today.Year ? DateTime.Today.ToString("yyyy-MM-dd") : $"{year}-12-31";

            Console.WriteLine($"Year Started | Year: {year}");

            for (var i = 0; i < employeeNumbers.Count; i += 75)
            {
                List<string> batch = [.. employeeNumbers.Skip(i).Take(75)];

                Console.WriteLine($"Batch Started | Year: {year} | Offset: {i} | Employees: {batch.Count}");

                List<TimecardRecord> records = await GetBatch(token, baseUrl, appKey, batch, startDate, endDate);

                totalSegments += records.Count;

                if (records.Count > 0)
                {
                    DataTable table = new();

                    _ = table.Columns.Add("employee_number", typeof(string));
                    _ = table.Columns.Add("apply_date", typeof(DateTime));
                    _ = table.Columns.Add("hours_worked", typeof(double));
                    _ = table.Columns.Add("project_code", typeof(string));
                    _ = table.Columns.Add("project_description", typeof(string));
                    _ = table.Columns.Add("activity_name", typeof(string));
                    _ = table.Columns.Add("activity_description", typeof(string));

                    foreach (TimecardRecord record in records)
                    {
                        _ = table.Rows.Add(
                            record.EmployeeNumber,
                            string.IsNullOrWhiteSpace(record.ApplyDate) ? DBNull.Value : DateTime.Parse(record.ApplyDate),
                            record.HoursWorked,
                            record.ProjectCode,
                            record.ProjectDescription,
                            record.ActivityName,
                            record.ActivityDescription);
                    }

                    await _sqlService.BulkInsertAsync("ukg_wfm.raw_timecards", table);
                }

                Console.WriteLine($"Batch Complete | Year: {year} | Records: {records.Count} | RunningTotal: {totalSegments}");
            }
        }

        return totalSegments;
    }

    private async Task<List<TimecardRecord>> GetBatch(string token, string baseUrl, string appKey, List<string> employeeNumbers, string startDate, string endDate)
    {
        HttpClient http = _httpClientFactory.CreateClient();

        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/v1/timekeeping/timecard/multi_read")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    where = new
                    {
                        employees = new { qualifiers = employeeNumbers },
                        dateRange = new { startDate, endDate }
                    },
                    select = new[] { "PROCESSED_SEGMENTS" }
                }),
                Encoding.UTF8,
                "application/json")
        };

        req.Headers.Add("Authorization", $"Bearer {token}");
        req.Headers.Add("appkey", appKey);

        HttpResponseMessage res = await http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
        {
            throw new Exception(json);
        }

        JsonElement root = JsonSerializer.Deserialize<JsonElement>(json);
        List<TimecardRecord> records = [];

        foreach (JsonElement employee in root.EnumerateArray())
        {
            if (!employee.TryGetProperty("processedSegments", out JsonElement segments))
            {
                continue;
            }

            foreach (JsonElement segment in segments.EnumerateArray())
            {
                var projectCode = "";
                var projectDescription = "";
                var activityName = "";
                var activityDescription = "";

                if (segment.TryGetProperty("laborCategories", out JsonElement laborCategories)
                    && laborCategories.TryGetProperty("entries", out JsonElement entries))
                {
                    List<JsonElement> entryList = [.. entries.EnumerateArray()];

                    if (entryList.Count > 0)
                    {
                        if (entryList[0].TryGetProperty("laborCategoryEntry", out JsonElement projectEntry)
                            && projectEntry.TryGetProperty("name", out JsonElement projectName))
                        {
                            projectCode = projectName.GetString() ?? "";
                        }

                        if (entryList[0].TryGetProperty("laborCategoryEntryDescription", out JsonElement projectDesc))
                        {
                            projectDescription = projectDesc.GetString() ?? "";
                        }
                    }

                    if (entryList.Count > 1)
                    {
                        if (entryList[1].TryGetProperty("laborCategoryEntry", out JsonElement activityEntry)
                            && activityEntry.TryGetProperty("name", out JsonElement activityValue))
                        {
                            activityName = activityValue.GetString() ?? "";
                        }

                        if (entryList[1].TryGetProperty("laborCategoryEntryDescription", out JsonElement activityDesc))
                        {
                            activityDescription = activityDesc.GetString() ?? "";
                        }
                    }
                }

                records.Add(new TimecardRecord
                {
                    EmployeeNumber = segment.GetProperty("employee").GetProperty("qualifier").GetString() ?? "",
                    ApplyDate = segment.TryGetProperty("applyDate", out JsonElement applyDate) ? applyDate.GetString() ?? "" : "",
                    HoursWorked = segment.TryGetProperty("durationInSeconds", out JsonElement seconds) ? seconds.GetInt64() / 3600.0 : 0,
                    ProjectCode = projectCode,
                    ProjectDescription = projectDescription,
                    ActivityName = activityName,
                    ActivityDescription = activityDescription
                });
            }
        }

        return records;
    }
}
