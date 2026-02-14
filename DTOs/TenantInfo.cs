namespace IntegratedAPI.DTOs
{
    public class TenantInfo
    {
        public string TenantId { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Indicates if this is a valid, resolved tenant
        /// </summary>
        public bool IsResolved => !string.IsNullOrEmpty(TenantId) && !string.IsNullOrEmpty(ConnectionString);
    }
}
