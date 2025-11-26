using IntegratedAPI.Contexts;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private ProjectDbContext _context;

        public CartController(ProjectDbContext context)
        {
            _context = context;
        }

        //Get all cart items as a list of CartItemInfo DTOs
        [HttpGet("GetCartItemsAsync")]
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
    }
}
