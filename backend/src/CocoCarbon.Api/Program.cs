using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CocoCarbon.Api.Authentication;
using CocoCarbon.Domain.Services;
using CocoCarbon.Infrastructure.AiClient;
using CocoCarbon.Infrastructure.Auth;
using CocoCarbon.Infrastructure.Persistence;
using CocoCarbon.Infrastructure.Services;
using CocoCarbon.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database configuration: PostgreSQL + PostGIS by default, SQLite fallback if explicitly configured for local/test
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? builder.Configuration["ConnectionStrings__DefaultConnection"]
              ?? "";
var dbProvider = builder.Configuration["DB_PROVIDER"] ?? "postgres";

if (string.Equals(dbProvider, "sqlite", StringComparison.OrdinalIgnoreCase) ||
    connStr.Contains("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    var sqliteConn = string.IsNullOrWhiteSpace(connStr) ? "Data Source=cococarbon_dev.db" : connStr;
    builder.Services.AddDbContext<CocoCarbonDbContext>(opt => opt.UseSqlite(sqliteConn));
}
else
{
    if (string.IsNullOrWhiteSpace(connStr))
    {
        connStr = "Host=localhost;Port=5432;Database=cococarbon;Username=cococarbon_app;Password=cococarbon_dev_pwd";
    }
    builder.Services.AddDbContext<CocoCarbonDbContext>(opt =>
        opt.UseNpgsql(connStr, npgsql => npgsql.UseNetTopologySuite()));
}

// Domain & Infrastructure Services
builder.Services.AddSingleton<CoconutCarbonCalculator>();
builder.Services.AddSingleton<IFirebaseTokenVerifier, FirebaseAdminTokenVerifier>();
builder.Services.AddSingleton<IPrivateFileStorageService, PrivateFileStorageService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpClient<IPythonAiServiceClient, PythonAiServiceClient>();

// Authentication (Firebase ID token) & Authorization
builder.Services
    .AddAuthentication(FirebaseAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(
        FirebaseAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

// CORS strict allowlist
var allowedOrigins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CocoCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 120;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 10;
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CocoCarbon AI API",
        Version = "v1",
        Description = "API lõi nghiệp vụ cho nền tảng CocoCarbon AI (dữ liệu vườn dừa vùng Bến Tre - tỉnh Vĩnh Long, calculator CTU 2015, AI document audit và Project Directory/RFQ)."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Firebase ID Token",
        In = ParameterLocation.Header,
        Description = "Nhập Firebase ID Token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Ensure DB schema/migrations & seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CocoCarbonDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var autoMigrate = !string.Equals(config["SKIP_DB_INIT"], "true", StringComparison.OrdinalIgnoreCase);
    if (autoMigrate)
    {
        if (db.Database.IsNpgsql() && db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        var seedDemo = !string.Equals(config["SEED_DEMO_DATA"], "false", StringComparison.OrdinalIgnoreCase);
        await DatabaseSeeder.SeedAsync(db, seedDemoData: seedDemo);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// OWASP Security Headers on all API responses
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(self), camera=(self)";
    await next();
});

if (string.Equals(app.Configuration["ENFORCE_HTTPS_REDIRECT"], "true", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseCors("CocoCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");

app.Run();

public partial class Program { }
