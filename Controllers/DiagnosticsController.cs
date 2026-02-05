using Confluent.Kafka;

using IntegratedAPI.Models.DTOs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using StackExchange.Redis;

using System.Diagnostics;

namespace IntegratedAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IConnectionMultiplexer _redisConnection;

        public DiagnosticsController(IConfiguration config, IConnectionMultiplexer redisConnection)
        {
            _config = config;
            _redisConnection = redisConnection;
        }

        [HttpGet("redis/simple")]
        public async Task<IActionResult> GetRedisDiagnostics()
        {
            var connectionString = _config["Redis:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                return Problem("Redis connection string not configured");
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Test basic ping
                var db = _redisConnection.GetDatabase();
                var pingResult = await db.PingAsync();

                // Simple set/get test
                var testKey = $"test-{Guid.NewGuid()}";
                var testValue = "test";

                await db.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
                var retrievedValue = await db.StringGetAsync(testKey);
                await db.KeyDeleteAsync(testKey);

                stopwatch.Stop();

                return Ok(new
                {
                    Status = "Redis is operational",
                    ConnectionString = MaskPassword(connectionString),
                    PingTimeMs = pingResult.TotalMilliseconds,
                    TotalResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    IsConnected = _redisConnection.IsConnected,
                    Endpoints = _redisConnection.GetEndPoints().Select(e => e.ToString()),
                    DatabaseSize = await db.ExecuteAsync("DBSIZE"),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Problem($"Failed to connect to Redis: {ex.Message}");
            }
        }

        [HttpGet("logs")]
        public IActionResult GetLogsDiagnostics([FromServices] ILogger<DiagnosticsController> logger)
        {
            var logstashUrl = _config["Logging:Logstash:Url"] ?? "Not configured";

            // Log different levels
            logger.LogTrace("This is a TRACE message");
            logger.LogDebug("This is a DEBUG message");
            logger.LogInformation("This is an INFO message at {Time}", DateTime.UtcNow);
            logger.LogWarning("This is a WARNING message");
            logger.LogError(new InvalidOperationException("Test exception"), "This is an ERROR message");
            logger.LogCritical("This is a CRITICAL message");

            // Return diagnostic info
            return Ok(new
            {
                Status = "Logs sent",
                LogstashUrl = logstashUrl,
                Timestamp = DateTime.UtcNow,
                LogLevels = new[] { "Trace", "Debug", "Info", "Warning", "Error", "Critical" }
            });
        }

        [HttpGet("logstash")]
        public async Task<IActionResult> GetLogstashDiagnostics()
        {
            var logstashUrl = _config["Logging:Logstash:Url"];

            if (string.IsNullOrEmpty(logstashUrl))
            {
                return Problem("Logstash URL not configured");
            }

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = await client.GetAsync(logstashUrl);
                var content = await response.Content.ReadAsStringAsync();

                return Ok(new
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
                return Problem($"Failed to connect: {ex.Message}");
            }
        }

        [HttpGet("kafka")]
        public IActionResult GetKafkaDiagnostics()
        {
            var bootstrapServers = _config["Kafka:BootstrapServers"];

            if (string.IsNullOrEmpty(bootstrapServers))
            {
                return Problem("Kafka BootstrapServers not configured");
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

                return Ok(new
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
                        Port = b.Port
                    }),
                    Topics = topics.Take(10), // Limit to first 10 topics
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Problem($"Failed to connect to Kafka: {ex.Message}");
            }
        }

        private static string MaskPassword(string connectionString)
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
    }
}