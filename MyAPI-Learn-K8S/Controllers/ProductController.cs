using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAPI_Learn_K8S.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace MyAPI_Learn_K8S.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController(ProductDbContext _dbContext, IConnectionMultiplexer _redis) : ControllerBase
    {
        
        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetAll()
        {
            var products = await _dbContext.Products.ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            var db = _redis.GetDatabase();
            var cacheKey = $"product:{id}";


            var cachedProduct = await db.StringGetAsync(cacheKey);

            if (cachedProduct.HasValue)
            {
                var productFromCache = JsonSerializer.Deserialize<Product>(cachedProduct.ToString());

                return Ok(new
                {
                    Source = "Redis Cache",
                    Product = productFromCache
                });
            }

            var productFromDatabase = await _dbContext.Products
            .FirstOrDefaultAsync(p => p.ProductId == id);

            if (productFromDatabase is null)
                return NotFound();

            var serializedProduct = JsonSerializer.Serialize(productFromDatabase);

            await db.StringSetAsync(
                cacheKey,
                serializedProduct,
                TimeSpan.FromMinutes(5));

            return Ok(new
            {
                Source = "SQL Server",
                Product = productFromDatabase
            });
        }
    }
}
