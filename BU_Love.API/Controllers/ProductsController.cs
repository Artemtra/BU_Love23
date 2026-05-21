using BU_Love.API.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BU_Love.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    namespace BU_Love.API.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class ProductsController : ControllerBase
        {
            private readonly BuLoveDbContext _context;

            public ProductsController(BuLoveDbContext context)
            {
                _context = context;
            }

            [HttpGet]
            public async Task<IActionResult> GetProducts([FromQuery] int? categoryId)
            {
                try
                {
                    var query = _context.Products.AsQueryable();

                    if (categoryId.HasValue && categoryId.Value > 0)
                    {
                        query = query.Where(p => p.CategoryId == categoryId.Value);
                    }

                    var products = await query.ToListAsync();
                    return Ok(products);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
                }
            }

            [HttpGet("{id}")]
            public async Task<IActionResult> GetProduct(int id)
            {
                try
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                        return NotFound(new { message = $"Товар с ID {id} не найден" });
                    return Ok(product);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = ex.Message });
                }
            }

            [HttpPost]
            public async Task<IActionResult> Create([FromBody] Product product)
            {
                try
                {
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    return Ok(product);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
                }
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> Update(int id, [FromBody] Product product)
            {
                try
                {
                    var existing = await _context.Products.FindAsync(id);
                    if (existing == null)
                        return NotFound(new { message = "Товар не найден" });

                    existing.Name = product.Name;
                    existing.Description = product.Description;
                    existing.Price = product.Price;
                    existing.CategoryId = product.CategoryId;
                    existing.StockQuantity = product.StockQuantity;
                    existing.Condition = product.Condition;
                    existing.ImageUrl = product.ImageUrl;

                    await _context.SaveChangesAsync();
                    return Ok(existing);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = ex.Message });
                }
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> Delete(int id)
            {
                try
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                        return NotFound();

                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = ex.Message });
                }
            }
        }
    }
}
