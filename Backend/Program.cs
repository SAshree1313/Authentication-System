using Backend.Data;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using FluentValidation;
using FluentValidation.AspNetCore;
using Backend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Services.Token;
using Fido2NetLib;
using Backend.Services.Passkey;

var builder = WebApplication.CreateBuilder(args);

// 1. Load `.env` only when running locally
// Detect local run (no Docker environment vars found)
var isLocal = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));

if (isLocal)
{
    // Look for .env in the project root (parent of Backend directory)
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(envPath))
    {
        envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
    }
    
    if (File.Exists(envPath))
    {
        Env.Load(envPath);   // overrides environment vars only locally
    }
}

// 2. Read environment variables (these now include values from .env when local)
string? dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");
string? dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
string? dbPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

string? jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
string? jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
string? jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

// Validate JWT environment variables
if (string.IsNullOrWhiteSpace(jwtSecret)) throw new Exception("JWT_SECRET is missing");
if (string.IsNullOrWhiteSpace(jwtIssuer)) throw new Exception("JWT_ISSUER is missing");
if (string.IsNullOrWhiteSpace(jwtAudience)) throw new Exception("JWT_AUDIENCE is missing");

// Push into config for services
builder.Configuration["Jwt:Secret"] = jwtSecret;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;

// 3. Build correct connection string for local or docker
string connectionString;

if (isLocal)
{
    // Local PostgreSQL (typical default host name)
    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";

    connectionString =
        $"Host={host};Port=5432;Database={dbName};Username={dbUser};Password={dbPassword}";
}
else
{
    // Docker Compose service name: postgres
    connectionString =
        $"Host=postgres;Port=5432;Database={dbName};Username={dbUser};Password={dbPassword}";
}

// 4. Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 5. Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddMemoryCache();

// 6. FIDO2 config
var fido2Section = builder.Configuration.GetSection("Fido2");
var origin = fido2Section["Origin"] ?? "https://localhost:3000";
var rpId = fido2Section["RpId"] ?? "localhost";

builder.Services.AddSingleton(sp => new Fido2(new Fido2Configuration
{
    ServerDomain = rpId,
    ServerName = fido2Section["ServerName"] ?? "My Auth API",
    Origins = new HashSet<string> { origin },
    TimestampDriftTolerance = int.Parse(fido2Section["TimestampDriftTolerance"] ?? "300000")
}));

builder.Services.AddScoped<IPasskeyService, PasskeyService>();

// 7. JWT Setup
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// ---------------------------------------------
// Dynamic CORS: supports multiple frontends
// ---------------------------------------------
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")
                   ?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                   ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// MVC + Validators + Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Allow case-insensitive matching
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // Use camelCase in JSON output.
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// 9. Middleware  
if (app.Environment.IsDevelopment())  //Used for testing endpoints must be removed in production for safety
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
