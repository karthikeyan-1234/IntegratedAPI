using IntegratedAPI.Contexts;
using IntegratedAPI.Models;
using IntegratedAPI.Models.DTOs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Services
{
    public class CartService : ICartService
    {
        ProjectDbContext _context;

        public CartService(ProjectDbContext _context)
        {
            this._context = _context;
        }

        public async Task<List<cartItemInfo>> GetCartItemsAsync()
        {
            var cartItems = from cartItem in _context.CartItems
                            join product in _context.Products
                            on cartItem.product_id equals product.id
                            select new Models.DTOs.cartItemInfo
                            {
                                product = product,
                                quantity = cartItem.quantity
                            };

            var _cartItems = await cartItems.ToListAsync();

            return _cartItems;
        }

        public async Task<cartItem> AddCartItemAsync(newCartItem newCartItem)
        {

            // check if product exists
            var product = await _context.Products.FindAsync(newCartItem.product_id);

            if (product != null)
            {
                // update quantity if product already in cart
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.product_id == newCartItem.product_id);
                if (existingCartItem != null)
                {
                    existingCartItem.quantity += newCartItem.quantity;
                    await _context.SaveChangesAsync();
                    return existingCartItem;
                }
            }


            var cartItem = new Models.cartItem
            {
                product_id = newCartItem.product_id,
                quantity = newCartItem.quantity
            };

            var newItem = _context.CartItems.Add(cartItem).Entity;
            await _context.SaveChangesAsync();
            return newItem;
        }
    }
}
