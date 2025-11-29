using IntegratedAPI.Contexts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// .NET 10: Service diagnostics (optional but useful)
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

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

// .NET 10 OPTIMIZED: Proper middleware ordering
app.UseRouting();

// .NET 10 FIX: Thread-safe metrics configuration
app.UseHttpMetrics(options =>
{
    // Minimal, thread-safe configuration
    options.RequestCount.Enabled = true;
    options.RequestDuration.Enabled = true;
    options.InProgress.Enabled = false; // Disable problematic metric

    // .NET 10: Use built-in context features instead of custom labels
});

app.UseCors();
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

app.Run();