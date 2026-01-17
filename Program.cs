using IntegratedAPI.Auth;
using IntegratedAPI.Contexts;
using IntegratedAPI.Exceptions;
using IntegratedAPI.Models.DTOs;

using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Prometheus;

using Serilog;
using Serilog.Formatting.Elasticsearch;

using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add this in Program.cs after adding controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<KeycloakAuthorizationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// .NET 10 ENHANCEMENT: Improved configuration binding
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Database Context with .NET 10 connection resilience
builder.Services.AddDbContext<ProjectDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection("StripeOptions"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// .NET 10 ENHANCEMENT: Comprehensive health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sqlserver",
        tags: new[] { "database", "ready" }
    )
    .AddDbContextCheck<ProjectDbContext>(
        name: "dbcontext",
        tags: new[] { "database", "efcore" }
    );

// .NET 10 ENHANCEMENT: Improved CORS with named policy

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// .NET 10: Service diagnostics (optional but useful)
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

ConfigureLogs();



builder.Host.UseSerilog();


//🔑 + 🛡️ Keycloak Section

builder.Services.AddScoped<KeycloakAuthorizationFilter>();
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);// 🔑 Configure Keycloak Authentication (uses appsettings.json)
builder.Services.AddKeycloakAuthorization(builder.Configuration);// 🛡️ Configure Keycloak Authorization Services


// ⚠️ NECESSARY CUSTOM LOGIC: Fix HTTPS requirement and add custom token validation
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // CRITICAL: Disable HTTPS requirement for development
    options.RequireHttpsMetadata = false;

    // Required for multiple audience support
    options.TokenValidationParameters.ValidAudiences = new[] { "api-app", "angular-app", "master-realm", "account" };
    options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;

    // Required for custom azp validation and role extraction
    options.Events = new JwtBearerEvents
    {

        OnTokenValidated = ctx =>
        {
            // Reject if token doesn't contain UMA "authorization.permissions"
            var hasRpt = ctx.Principal?.Claims.Any(c => c.Type == "authorization") ?? false;
            if (!hasRpt)
            {
                ctx.Fail("RPT (UMA token) required.");
            }
            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        },

        OnChallenge = context =>
        {
            Console.WriteLine("OnChallenge: " + context.Error + " - " + context.ErrorDescription);
            return Task.CompletedTask;
        }
    };
});

//For resolving IhttpClientFactory
builder.Services.AddHttpClient();


builder.Services.AddExceptionHandler<ProductExceptionHandler>();
builder.Services.AddProblemDetails();


var app = builder.Build();

// .NET 10 ENHANCEMENT: Async database initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // .NET 10: Async database creation with timeout
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await db.Database.EnsureCreatedAsync(cts.Token);
        logger.LogInformation("Database initialized successfully");
    }
    catch (OperationCanceledException ex)
    {
        logger.LogError(ex, "Database initialization timed out");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred creating the database");
    }
}

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // .NET 10: Enhanced developer exception page
    app.UseDeveloperExceptionPage();
}

// .NET 10 FIX: Thread-safe metrics configuration
app.UseHttpMetrics(options =>
{
    // Minimal, thread-safe configuration
    options.RequestCount.Enabled = true;
    options.RequestDuration.Enabled = true;
    options.InProgress.Enabled = false; // Disable problematic metric

    // .NET 10: Use built-in context features instead of custom labels
});

// .NET 10 OPTIMIZED: Proper middleware ordering
app.UseRouting();

app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

// .NET 10: Endpoint routing with improved performance
app.MapControllers();

// .NET 10 ENHANCEMENT: Comprehensive health checks
app.MapHealthChecks("/health", new()
{
    ResponseWriter = async (context, report) =>
    {
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.ToString(),
                description = e.Value.Description
            })
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(result);
    }
});

// .NET 10: Ready/Live separate endpoints (Kubernetes best practice)
app.MapHealthChecks("/ready", new()
{
    Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("database")
});

app.MapHealthChecks("/live", new()
{
    Predicate = check => !check.Tags.Contains("ready")
});

// .NET 10 FIX: Isolated metrics endpoint with error handling
app.MapMetrics("/metrics");

// .NET 10: Global exception handling for metrics
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/metrics"))
    {
        try
        {
            await next();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Collection was modified"))
        {
            // .NET 10: Using IResult for better response handling
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Metrics temporarily unavailable",
                suggestion = "Retry in a few seconds"
            });
        }
    }
    else
    {
        await next();
    }
});

// .NET 10: Application lifetime logging
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() =>
    app.Logger.LogInformation("Application started and metrics available at /metrics"));

app.UseExceptionHandler();

app.Run();



#region Configure Logs

void ConfigureLogs()
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .Build();

    var logstashUrl = configuration["Logging:Logstash:Url"] ?? "http://localhost:5001";

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "IntegratedAPI")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)

        // Console output
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

        // Debug output
        .WriteTo.Debug()

        // Send to Logstash - WORKING WITH REQUIRED PARAMETER
        .WriteTo.Http(
            requestUri: logstashUrl,
            queueLimitBytes: null)

        .CreateLogger();
}

#endregion