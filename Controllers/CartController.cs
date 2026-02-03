using IntegratedAPI.Auth;
using IntegratedAPI.Contexts;
using IntegratedAPI.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Resource("Cart")]
    public class CartController : ControllerBase
    {
        private ProjectDbContext _context;

        public CartController(ProjectDbContext context)
        {
            _context = context;
        }

        //Get all cart items as a list of CartItemInfo DTOs
        [HttpGet("GetCartItemsAsync")]
        [Permission("read")]
        public async Task<IActionResult> GetCartItemsAsync()
        {
            var cartItems = from cartItem in _context.CartItems
                            join product in _context.Products
                            on cartItem.product_id equals product.id
                            select new Models.DTOs.cartItemInfo
                            {
                                product = product,
                                quantity = cartItem.quantity
                            };

            return Ok(await cartItems.ToListAsync());
        }

        //Add new cartItem
        [HttpPost("AddCartItemAsync")]
        [Permission("create")]
        public async Task<IActionResult> AddCartItemAsync([FromBody] Models.DTOs.newCartItem newCartItem)
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
                    return Ok(newCartItem);
                }
            }


            var cartItem = new Models.cartItem
            {
                product_id = newCartItem.product_id,
                quantity = newCartItem.quantity
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();
            return Ok(newCartItem);
        }


        //Delete an existing cartItem
        [HttpDelete("DeleteCartItemAsync")]
        [Permission("delete")]
        public async Task<bool> DeleteCartItemAsync(int itemId)
        {
            if(_context.CartItems != null)
            {
                if(_context.CartItems.Any(x => x.product_id == itemId))
                {
                    var cartItem =  await _context.CartItems.Where(x => x.product_id == itemId).FirstAsync();
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }

            return false;
        }
    }
}
