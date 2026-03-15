using IntegratedAPI.Models;
using IntegratedAPI.Models.DTOs;

namespace IntegratedAPI.Services
{
    public interface ICartService
    {
        Task<List<cartItemInfo>> GetCartItemsAsync();
        Task<cartItem> AddCartItemAsync(newCartItem newCartItem);
    }
}