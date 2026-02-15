namespace IntegratedAPI.Services
{
    public interface IVaultService
    {
        Task<string?> GetConnectionString(string tenant);
    }
}