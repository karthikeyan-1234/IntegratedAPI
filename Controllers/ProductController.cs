using Confluent.Kafka;

using IntegratedAPI.Auth;
using IntegratedAPI.Contexts;
using IntegratedAPI.DTOs;
using IntegratedAPI.Exceptions;
using IntegratedAPI.Models;
using IntegratedAPI.Models.DTOs;
using IntegratedAPI.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace IntegratedAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    [Resource("Products")]
    public class ProductController : ControllerBase
    {

        private readonly ILogger<ProductController> _logger;
        private readonly TenantDbContextFactory _dbFactory;
        private readonly IProducer<string, string> _producer;
        private readonly ICacheManagerService _cache;
        private readonly HttpContext _httpContext;

        public ProductController(ILogger<ProductController> logger, IProducer<string,string> producer,ICacheManagerService cache, IHttpContextAccessor httpContextAccessor, TenantDbContextFactory dbFactory)
        {
            _logger = logger;
            _dbFactory = dbFactory;
            _producer = producer;
            _cache = cache;
            _httpContext = httpContextAccessor.HttpContext!;
        }

        [HttpGet("GetProductsAsync")]
        [Permission("read")]
        public async Task<IActionResult> GetProductsAsync()
        {
            var tenant = _httpContext.Items["group"];

            ProjectDbContext _projectDbContext = await _dbFactory.CreateAsync();

            //var cachedProducts = await _cache.GetAsync<IEnumerable<product>>(CacheKeys.Products);

            //if (cachedProducts != null)
            //{
            //    _logger.LogInformation("Returning cached products");
            //    return Ok(cachedProducts);
            //}

            var products = await _projectDbContext.Products.ToListAsync();

            if (products.Any())
            {
                await _cache.SetAsync(CacheKeys.Products, products, TimeSpan.FromMinutes(30));  // Cache for 30 minutes
                return Ok(products);
            }
            else
                throw new NoProductsException("GetProductAsync method failed");
        }


        [HttpPost("AddProductAsync")]
        [Permission("create")]
        public async Task<IActionResult> AddProductAsync([FromBody] newProduct product)
        {
            product newProduct1 = new product
            {
                name = product.name,
                price = product.price,
                image = product.image,
                description = product.description
            };

            ProjectDbContext _projectDbContext = await _dbFactory.CreateAsync();


            var newProductInfo = _projectDbContext.Products.Add(newProduct1).Entity;
            await _projectDbContext.SaveChangesAsync();

            await _producer.ProduceAsync("product-added", new Message<string, string> { Key = newProductInfo.id.ToString(), Value = JsonSerializer.Serialize(newProductInfo)});

            return Ok(product);
        }

        [HttpPut("UpdateProductAsync")]
        public async Task<IActionResult> UpdateProductAsync([FromBody] product updatedProduct)
        {
            ProjectDbContext _projectDbContext = await _dbFactory.CreateAsync();

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


        [HttpDelete("DeleteProductAsync/{productId}")]
        [Permission("delete")]
        public async Task<IActionResult> DeleteProductAsync(int productId)
        {
            ProjectDbContext _projectDbContext = await _dbFactory.CreateAsync();

            if (_projectDbContext.Products.Any())
            {
                var existingProduct = await _projectDbContext.Products.FindAsync(productId);

                if (existingProduct == null)
                    return NotFound();

                _projectDbContext.Products.Remove(existingProduct);
                await _projectDbContext.SaveChangesAsync();
                return NoContent();
            }
            else
                throw new NoProductsException("No products in DB to be deleted..!!");
        }
    }
}
