using IntegratedAPI.Contexts;
using IntegratedAPI.Models;
using IntegratedAPI.Models.DTOs;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegratedAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly ILogger<ProductController> _logger;
        private readonly ProjectDbContext _projectDbContext;

        public ProductController(ILogger<ProductController> logger, ProjectDbContext projectDbContext)
        {
            _logger = logger;
            _projectDbContext = projectDbContext;
        }

        [HttpGet("GetProductsAsync")]
        public async Task<IActionResult> GetProductsAsync()
        {
            return Ok(await _projectDbContext.Products.ToListAsync());
        }


        [HttpPost("AddProductAsync")]
        public async Task<IActionResult> AddProductAsync([FromBody] newProduct product)
        {
            product newProduct1 = new product
            {
                name = product.name,
                price = product.price,
                image = product.image,
                description = product.description
            };


            _projectDbContext.Products.Add(newProduct1);
            await _projectDbContext.SaveChangesAsync();
            return Ok(product);
        }

        [HttpPut("UpdateProductAsync")]
        public async Task<IActionResult> UpdateProductAsync([FromBody] product updatedProduct)
        {
            var existingProduct = await _projectDbContext.Products.FindAsync(updatedProduct.id);
            if (existingProduct == null)
            {
                return NotFound();
            }
            existingProduct.name = updatedProduct.name;
            existingProduct.price = updatedProduct.price;
            existingProduct.image = updatedProduct.image;
            existingProduct.description = updatedProduct.description;
            await _projectDbContext.SaveChangesAsync();
            return Ok(existingProduct);
        }
    }
}
