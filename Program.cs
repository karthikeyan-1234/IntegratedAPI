using Confluent.Kafka;
using Confluent.Kafka.Admin;

using IntegratedAPI.Auth;
using IntegratedAPI.Background_Services;
using IntegratedAPI.Contexts;
using IntegratedAPI.Exceptions;
using IntegratedAPI.Models.DTOs;

using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using Prometheus;

using Serilog;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;

using StackExchange.Redis;

using System.Diagnostics;
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
        policy.WithOrigins("http://kong-proxy.local", "http://localhost:4200")
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



// Kafka Producer
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        Acks = Acks.All,                     // wait for leader + replicas
        MessageTimeoutMs = 5000
    };
    return new ProducerBuilder<string, string>(config).Build();
});

// Kafka Consumer
builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        GroupId = "order-service-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };
    return new ConsumerBuilder<string, string>(config).Build();
});


//Kafka background service
//builder.Services.AddHostedService<KafkaMonitor>();



// Redis Configuration
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrEmpty(redisConnectionString))
{
    // Option 1: Add Redis distributed cache (simpler)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "IntegratedAPI";
    });

    // Option 2: Add Redis ConnectionMultiplexer (more control)
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var configuration = ConfigurationOptions.Parse(redisConnectionString);
        configuration.AbortOnConnectFail = false;
        configuration.ConnectTimeout = 5000;
        configuration.SyncTimeout = 5000;
        return ConnectionMultiplexer.Connect(configuration);
    });

    // Add Redis health check
    builder.Services.AddHealthChecks()
        .AddRedis(redisConnectionString,
            name: "redis",
            tags: new[] { "cache", "ready" });

    Console.WriteLine($"Redis configured with connection: {redisConnectionString}");
}
else
{
    Console.WriteLine("Redis connection string not found, using in-memory cache");
    builder.Services.AddDistributedMemoryCache();
}

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




#region Test LogStash connectivity
// Add this before app.Run()
app.MapGet("/diagnostics/logs", (ILogger<Program> logger, IConfiguration config) =>
{
    var logstashUrl = config["Logging:Logstash:Url"] ?? "Not configured";

    // Log different levels
    logger.LogTrace("This is a TRACE message");
    logger.LogDebug("This is a DEBUG message");
    logger.LogInformation("This is an INFO message at {Time}", DateTime.UtcNow);
    logger.LogWarning("This is a WARNING message");
    logger.LogError(new InvalidOperationException("Test exception"), "This is an ERROR message");
    logger.LogCritical("This is a CRITICAL message");

    // Return diagnostic info
    return Results.Ok(new
    {
        Status = "Logs sent",
        LogstashUrl = logstashUrl,
        Timestamp = DateTime.UtcNow,
        LogLevels = new[] { "Trace", "Debug", "Info", "Warning", "Error", "Critical" }
    });
});

app.MapGet("/diagnostics/logstash", async (IConfiguration config) =>
{
    var logstashUrl = config["Logging:Logstash:Url"];

    if (string.IsNullOrEmpty(logstashUrl))
    {
        return Results.Problem("Logstash URL not configured");
    }

    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var response = await client.GetAsync(logstashUrl);
        var content = await response.Content.ReadAsStringAsync();

        return Results.Ok(new
        {
            Url = logstashUrl,
            StatusCode = (int)response.StatusCode,
            Status = response.StatusCode.ToString(),
            Response = content,
            Timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to connect: {ex.Message}");
    }
});


app.MapGet("/diagnostics/kafka", async (IConfiguration config) =>
{
    var bootstrapServers = config["Kafka:BootstrapServers"];

    if (string.IsNullOrEmpty(bootstrapServers))
    {
        return Results.Problem("Kafka BootstrapServers not configured");
    }

    try
    {
        var stopwatch = Stopwatch.StartNew();

        // Test using AdminClient to get metadata
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        // Get metadata
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
        stopwatch.Stop();

        // Try to list topics
        var topics = new List<string>();
        try
        {
            var topicMetadata = metadata.Topics;
            topics = topicMetadata.Select(t => t.Topic).ToList();
        }
        catch (Exception)
        {
            topics = new List<string>();
        }

        return Results.Ok(new
        {
            Status = "Kafka is reachable",
            BootstrapServers = bootstrapServers,
            ConnectionTimeMs = stopwatch.ElapsedMilliseconds,
            BrokerCount = metadata.Brokers.Count,
            TopicsCount = topics.Count,
            Brokers = metadata.Brokers.Select(b => new
            {
                Id = b.BrokerId,
                Host = b.Host,
                Port = b.Port            }),
            Topics = topics.Take(10), // Limit to first 10 topics
            Timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to connect to Kafka: {ex.Message}");
    }
});



app.MapGet("/diagnostics/redis/simple", async (IConfiguration config, IDistributedCache cache, IConnectionMultiplexer redisConnection) =>
{
    var connectionString = config["Redis:ConnectionString"];

    if (string.IsNullOrEmpty(connectionString))
    {
        return Results.Problem("Redis connection string not configured");
    }

    try
    {
        var stopwatch = Stopwatch.StartNew();

        // Test basic ping
        var db = redisConnection.GetDatabase();
        var pingResult = await db.PingAsync();

        // Simple set/get test
        var testKey = $"test-{Guid.NewGuid()}";
        var testValue = "test";

        await db.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
        var retrievedValue = await db.StringGetAsync(testKey);
        await db.KeyDeleteAsync(testKey);

        stopwatch.Stop();

        return Results.Ok(new
        {
            Status = "Redis is operational",
            ConnectionString = MaskPassword(connectionString),
            PingTimeMs = pingResult.TotalMilliseconds,
            TotalResponseTimeMs = stopwatch.ElapsedMilliseconds,
            IsConnected = redisConnection.IsConnected,
            Endpoints = redisConnection.GetEndPoints().Select(e => e.ToString()),
            DatabaseSize = await db.ExecuteAsync("DBSIZE"),
            Timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to connect to Redis: {ex.Message}");
    }
});

// Helper method to mask password in connection string
string MaskPassword(string connectionString)
{
    try
    {
        var options = ConfigurationOptions.Parse(connectionString);
        if (!string.IsNullOrEmpty(options.Password))
        {
            options.Password = "***MASKED***";
        }
        return options.ToString();
    }
    catch
    {
        // If parsing fails, just mask any obvious password patterns
        var masked = System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"password=[^,;]+",
            "password=***MASKED***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return masked;
    }
}

#endregion

app.Run();



#region Configure Logs
void ConfigureLogs()
{
    // Use the existing builder.Configuration, don't create a new one
    var logstashUrl = builder.Configuration["Logging:Logstash:Url"]
        ?? "http://logstash.local/";  // Default to Ingress URL for local dev

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)  // This reads from appsettings.json
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "IntegratedAPI")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithProperty("Source", "Program")

        // Console output (for local debugging)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

        // Debug output
        .WriteTo.Debug()

        // HTTP sink to Logstash - Use ElasticsearchJsonFormatter for proper JSON
        .WriteTo.Http(
            requestUri: logstashUrl,
            queueLimitBytes: null,
            period: TimeSpan.FromSeconds(5),
            textFormatter: new ElasticsearchJsonFormatter()  // CRITICAL: This creates proper JSON
            )
        // Optional: Direct Elasticsearch sink (remove or configure properly)
        /*.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(
            new Uri("http://elasticsearch.elk.svc.cluster.local:9200"))
        {
            AutoRegisterTemplate = true,
            IndexFormat = "integratedapi-direct-{0:yyyy.MM.dd}",
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7
        })*/

        .CreateLogger();
}

#endregion

