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
using Backend.Services.Auth;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:8080");
var aspnetEnv = builder.Environment.EnvironmentName;
Console.WriteLine($"ASPNETCORE_ENVIRONMENT = {aspnetEnv}");


// ------------------------------------------------------------------------------------
// 1. Load `.env` only when running locally
// ------------------------------------------------------------------------------------
var isLocal =
    builder.Environment.IsDevelopment() ||
    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));

if (isLocal)
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (!File.Exists(envPath))
        envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");

    if (File.Exists(envPath))
        Env.Load(envPath);
}

// ------------------------------------------------------------------------------------
// 2. Environment variables (AUTH / JWT / GOOGLE ONLY)
// ------------------------------------------------------------------------------------
string? jwtSecret   = Environment.GetEnvironmentVariable("JWT_SECRET");
string? jwtIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER");
string? jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

if (string.IsNullOrWhiteSpace(jwtSecret))   throw new Exception("JWT_SECRET missing");
if (string.IsNullOrWhiteSpace(jwtIssuer))   throw new Exception("JWT_ISSUER missing");
if (string.IsNullOrWhiteSpace(jwtAudience)) throw new Exception("JWT_AUDIENCE missing");

builder.Configuration["Jwt:Secret"]   = jwtSecret;
builder.Configuration["Jwt:Issuer"]   = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;

string? googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");

if (string.IsNullOrWhiteSpace(googleClientId))
    throw new Exception("GOOGLE_CLIENT_ID missing");

builder.Configuration["Google:ClientId"] = googleClientId;

// ------------------------------------------------------------------------------------
// 3. DATABASE CONFIGURATION
//    - Local  → SQLite (file-based)
//    - Docker → PostgreSQL (env-injected connection string)
// ------------------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isLocal)
    {
        // LOCAL: SQLite database in /Data/auth.db
        var appdataDir = Path.Combine(builder.Environment.ContentRootPath, "AppData");
        Directory.CreateDirectory(appdataDir);

        var sqlitePath = Path.Combine(appdataDir, "auth.db");
        options.UseSqlite($"Data Source={sqlitePath}");
    }
    else
    {
        // DOCKER: PostgreSQL (unchanged behavior)
        var connectionString =
            builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("PostgreSQL connection string missing");

        options.UseNpgsql(connectionString);
    }
});

// ------------------------------------------------------------------------------------
// 4. Services
// ------------------------------------------------------------------------------------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();

// ------------------------------------------------------------------------------------
// 5. FIDO2 Setup
// ------------------------------------------------------------------------------------
var fido2Config = builder.Configuration.GetSection("Fido2");
var origin = fido2Config["Origin"] ?? "https://localhost:3000";
var rpId   = fido2Config["RpId"] ?? "localhost";

builder.Services.AddSingleton(sp => new Fido2(new Fido2Configuration
{
    ServerDomain = rpId,
    ServerName = fido2Config["ServerName"] ?? "My Auth API",
    Origins = new HashSet<string> { origin },
    TimestampDriftTolerance = int.Parse(fido2Config["TimestampDriftTolerance"] ?? "300000")
}));

builder.Services.AddScoped<IPasskeyService, PasskeyService>();

// ------------------------------------------------------------------------------------
// 6. JWT Authentication
// ------------------------------------------------------------------------------------
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtIssuer,
        ValidAudience            = jwtAudience,
        IssuerSigningKey         = new SymmetricSecurityKey(key)
    };

    // TokenVersion validation
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async ctx =>
        {
            try
            {
                var claims = ctx.Principal?.Claims;
                if (claims == null)
                {
                    ctx.Fail("Missing claims.");
                    return;
                }

                var idClaim =
                    claims.FirstOrDefault(c => c.Type == "id")?.Value ??
                    claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                if (!int.TryParse(idClaim, out var userId))
                {
                    ctx.Fail("Invalid ID.");
                    return;
                }

                var versionClaim = claims.FirstOrDefault(c => c.Type == "tokenVersion")?.Value;

                if (!int.TryParse(versionClaim, out var tokenVersionFromToken))
                {
                    ctx.Fail("Missing tokenVersion.");
                    return;
                }

                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

                var user = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Id, u.TokenVersion })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    ctx.Fail("User not found.");
                    return;
                }

                if (user.TokenVersion != tokenVersionFromToken)
                {
                    ctx.Fail("Token outdated.");
                }
            }
            catch (Exception ex)
            {
                ctx.Fail("TokenVersion validation failed: " + ex.Message);
            }
        }
    };
});

// ------------------------------------------------------------------------------------
// 7. CORS
// ------------------------------------------------------------------------------------
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

// ------------------------------------------------------------------------------------
// 8. MVC / Swagger
// ------------------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// ------------------------------------------------------------------------------------
// ⭐ 9. APPLY DATABASE MIGRATIONS (LOCAL + DOCKER)
// ------------------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ------------------------------------------------------------------------------------
// 10. Middleware pipeline
// ------------------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
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
