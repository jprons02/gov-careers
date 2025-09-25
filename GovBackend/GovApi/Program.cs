// This brings our UsaJobsService class into scope.
using GovApi.Data;
using Microsoft.EntityFrameworkCore;
using GovApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtConfig = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!))
        };

        // 🔎 Add logging for why the token fails
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("❌ JWT validation failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var identityName = context.Principal?.Identity?.Name ?? "(no name)";
                Console.WriteLine($"✅ JWT validated successfully for: {identityName}");
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization();

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

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GovApi", Version = "v1" });

    // Add JWT auth support
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


// Build the app (this sets up everything we registered).
var app = builder.Build();

app.UseCors("AllowFrontend");   // keep this before auth

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();   // 👈 must be before controllers
app.UseAuthorization();

app.MapControllers();

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
