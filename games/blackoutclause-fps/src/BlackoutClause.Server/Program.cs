using System.Text;
using System.Threading.RateLimiting;
using BlackoutClause.Server.Configuration;
using BlackoutClause.Server.Endpoints;
using BlackoutClause.Server.Infrastructure.Clerk;
using BlackoutClause.Server.Infrastructure.Data;
using BlackoutClause.Server.Infrastructure.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<BlackoutClause.Server.Configuration.ClerkSettings>(builder.Configuration.GetSection("Clerk"));
builder.Services.Configure<BlackoutClause.Server.Configuration.AppSettings>(builder.Configuration.GetSection("App"));
builder.Services.Configure<BlackoutClause.Server.Configuration.RedisSettings>(builder.Configuration.GetSection("Redis"));

// Database
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.EnableRetryOnFailure(3);
            npgsql.CommandTimeout(30);
        });
    }

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Clerk Authentication
var clerkSettings = builder.Configuration.GetSection("Clerk").Get<ClerkSettings>()
    ?? throw new InvalidOperationException("Clerk settings not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{clerkSettings.Domain}";
        options.Audience = clerkSettings.PublishableKey;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = $"https://{clerkSettings.Domain}",
            ValidAudience = clerkSettings.PublishableKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                // Sync user from Clerk claims to local DB
                var userSync = context.HttpContext.RequestServices.GetRequiredService<IUserSyncService>();
                await userSync.SyncUserFromClaimsAsync(context.Principal!);
            }
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Services
builder.Services.AddHttpClient<IClerkClient, ClerkClient>();
builder.Services.AddScoped<IUserSyncService, UserSyncService>();
builder.Services.AddScoped<IClerkWebhookHandler, ClerkWebhookHandler>();

// Redis Service (StackExchange or Upstash)
builder.Services.AddSingleton<IRedisService>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<BlackoutClause.Server.Configuration.RedisSettings>>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return RedisServiceFactory.Create(settings, loggerFactory);
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddNpgSql(connectionString)
    .AddCheck<RedisHealthCheck>("redis");

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BlackoutClause API",
        Version = "v1"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("GameClient", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://blackoutclause.game")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlackoutClause API v1");
        c.RoutePrefix = "swagger";
    });

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseCors("GameClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Endpoints
app.MapAuthEndpoints();
app.MapGameEndpoints();
app.MapClerkWebhookEndpoints();

// Root
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

app.Run();
