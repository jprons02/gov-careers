// This brings our UsaJobsService class into scope.
using GovApi.Data;
using Microsoft.EntityFrameworkCore;
using GovApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Register UsaJobsService so .NET knows how to build it.
// AddHttpClient tells .NET: "If anyone asks for UsaJobsService,
// create it and give it an HttpClient to use."
builder.Services.AddHttpClient<UsaJobsService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);


// Build the app (this sets up everything we registered).
var app = builder.Build();

/// <summary>
/// Define an API route: GET /api/jobs
/// - Accepts a "keyword" query string (e.g., /api/jobs?keyword=developer)
/// - Uses UsaJobsService to fetch jobs from the API
/// - Returns the raw JSON to the client
/// </summary>
app.MapGet("/api/jobs", async (string keyword, UsaJobsService usaJobsService) =>
{
    // Call the service method with the keyword
    var result = await usaJobsService.SearchJobsAsync(keyword);

    return Results.Json(result);
});

// Run the web app
app.Run();
