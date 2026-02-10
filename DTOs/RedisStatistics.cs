namespace IntegratedAPI.DTOs
{
    /// <summary>
    /// Redis statistics DTO
    /// </summary>
    public class RedisStatistics
    {
        public string ConnectedClients { get; set; } = "N/A";
        public string UsedMemory { get; set; } = "N/A";
        public string TotalConnectionsReceived { get; set; } = "N/A";
        public string TotalCommandsProcessed { get; set; } = "N/A";
        public string KeyspaceHits { get; set; } = "N/A";
        public string KeyspaceMisses { get; set; } = "N/A";
        public string UptimeInSeconds { get; set; } = "N/A";
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; }
        public double HitRate =>
            double.TryParse(KeyspaceHits, out var hits) &&
            double.TryParse(KeyspaceMisses, out var misses) &&
            (hits + misses) > 0 ? hits / (hits + misses) * 100 : 0;
    }
}
