namespace IntegratedAPI.Services
{
    public interface IVaultService
    {
        Task<object?> GetConnectionString(string tenant);
    }
}