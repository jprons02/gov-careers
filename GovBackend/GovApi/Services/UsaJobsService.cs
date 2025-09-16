using System.Net.Http;       // like "require('request')" in Node
using System.Text.Json;      // like JSON.parse in Node
using GovApi.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration; // 👈 for IConfiguration

namespace GovApi.Services
{
    public class UsaJobsService
    {
        private readonly HttpClient _httpClient;

        public UsaJobsService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;

            // Pull from appsettings.Development.json
            var userAgent = config["UsaJobs:UserAgent"];
            var authKey = config["UsaJobs:ApiKey"];

            Console.WriteLine($"UserAgent: {userAgent}");
            Console.WriteLine($"AuthKey: {authKey?.Substring(0, 4)}..."); // safer logging

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Host", "data.usajobs.gov");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
            _httpClient.DefaultRequestHeaders.Add("Authorization-Key", authKey);
        }

        public async Task<IEnumerable<JobResult>> SearchJobsAsync(string keyword)
{
    var response = await _httpClient.GetAsync(
        $"https://data.usajobs.gov/api/search?Keyword={keyword}"
    );
    response.EnsureSuccessStatusCode();

    var body = await response.Content.ReadAsStringAsync();
    var doc = JsonDocument.Parse(body);

    return doc.RootElement
        .GetProperty("SearchResult")
        .GetProperty("SearchResultItems")
        .EnumerateArray()
        .Select(item =>
        {
            var descriptor = item.GetProperty("MatchedObjectDescriptor");

            var remuneration = descriptor.GetProperty("PositionRemuneration")[0];
            var min = remuneration.GetProperty("MinimumRange").GetString();
            var max = remuneration.GetProperty("MaximumRange").GetString();
            var type = remuneration.GetProperty("Description").GetString();

            return new JobResult(
    descriptor.GetProperty("PositionTitle").GetString() ?? "N/A",
    descriptor.GetProperty("PositionLocationDisplay").GetString() ?? "N/A",
    descriptor.GetProperty("OrganizationName").GetString() ?? "N/A",
    descriptor.GetProperty("PositionURI").GetString() ?? "#",
    min ?? "0",
    max ?? "0",
    type ?? "Unknown"
);
        });
}
    }
}
