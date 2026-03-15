using IntegratedAPI.Contexts;
using IntegratedAPI.Models;
using IntegratedAPI.Models.DTOs;
using IntegratedAPI.Services;

using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;


namespace IntegratedAPITests
{
    public class CartServiceTest
    {
        ProjectDbContext _context;
        ICartService _cartService;

        public CartServiceTest()
        {
            var options = new DbContextOptionsBuilder<ProjectDbContext>()
                          .UseInMemoryDatabase(databaseName: "TestDatabase") // Use a unique name for each test to ensure isolation
                            .Options;

            _context = new ProjectDbContext(options);
            _cartService = new CartService(_context);
        }

        //Test GetCartItemsAsync

        [Fact]
        public async Task GetCartItemsAsync_ReturnsEmpty_WhenCartIsEmpty()
        {
            // Act
            var result = await _cartService.GetCartItemsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCartItemsAsync_ReturnsCartItems_WhenDataExists()
        {
            // Arrange
            var product = new product { id = 1, name = "Test Product", price = 50, image = "image1", description = "Test Product" };
            var cartItem = new cartItem { product_id = 1, quantity = 3 };

            _context.Products.Add(product);
            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _cartService.GetCartItemsAsync();

            // Assert
            result.Should().HaveCount(1);
            result[0].quantity.Should().Be(3);
            result[0]?.product?.id.Should().Be(1);
        }

        [Fact] //        Task<cartItem> AddCartItemAsync(newCartItem newCartItem);
        public async Task AddCartItemAsync_AddsItems()
        {
            //Arrange
            var cartItem = new cartItem { product_id = 1, quantity = 3 };
            _context.CartItems.Add(cartItem);

            var newCartItem = new cartItem { product_id = 1, quantity = 20 };
            var oldCnt = (await _cartService.GetCartItemsAsync()).Count;

            _context.CartItems.Add(newCartItem);
            await _context.SaveChangesAsync();

            //Act
            var newCnt = (await _cartService.GetCartItemsAsync()).Count;
            bool result = newCnt > oldCnt;

            //Assert
            result.Should().BeTrue();
        }

    }
}
