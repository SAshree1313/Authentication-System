using Backend.Data;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Load .env from the parent folder (project root)
Env.Load(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, ".env"));

// Build PostgreSQL connection string from environment variables
var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");
var user = Environment.GetEnvironmentVariable("POSTGRES_USER");
var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

var connectionString = $"Server=localhost;Port=5433;Database={dbName};Username={user};Password={password}";

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
