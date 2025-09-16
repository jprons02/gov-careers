using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using GovApi.Services;

namespace GovApi.Endpoints;

public static class JobsEndpoints
{
    public static void MapJobsEndpoints(this IEndpointRouteBuilder app)
    {
app.MapGet("/api/jobs", async (string keyword, UsaJobsService usaJobsService) =>
{
    var results = await usaJobsService.SearchJobsAsync(keyword);
    return Results.Json(results);
});
    }
}
