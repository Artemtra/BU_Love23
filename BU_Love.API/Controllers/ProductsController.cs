using BU_Love.API.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BU_Love.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using System;

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
                var query = _context.Products.AsQueryable();

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                var products = await query.ToListAsync();
                return Ok(products);
            }

            [HttpGet("{id}")]
            public async Task<IActionResult> GetProduct(int id)
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound();
                return Ok(product);
            }

            [HttpPost]
            public async Task<IActionResult> CreateProduct([FromBody] Product product)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }

            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
            {
                if (id != product.Id)
                    return BadRequest();

                _context.Entry(product).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }

            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteProduct(int id)
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound();

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            [HttpDelete("delete-all")]
            public async Task<IActionResult> DeleteAllProducts()
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Отключаем внешние ключи, чтобы не мешали
                    await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");

                    // Обнуляем ProductId в OrderItems, чтобы сохранить историю заказов
                    // но убрать связь с удаляемыми товарами
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE OrderItems SET ProductId = NULL WHERE ProductId IS NOT NULL");

                    // Удаляем все товары
                    var allProducts = await _context.Products.ToListAsync();

                    if (!allProducts.Any())
                    {
                        await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
                        await transaction.CommitAsync();
                        return Ok(new { message = "Нет товаров для удаления" });
                    }

                    _context.Products.RemoveRange(allProducts);
                    await _context.SaveChangesAsync();

                    // Включаем обратно внешние ключи
                    await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");

                    // Сбрасываем автоинкремент
                    await _context.Database.ExecuteSqlRawAsync("ALTER TABLE Products AUTO_INCREMENT = 1");

                    await transaction.CommitAsync();

                    return Ok(new { message = $"Удалено товаров: {allProducts.Count}. История заказов сохранена." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _context.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");

                    var innerException = ex.InnerException?.Message ?? ex.Message;
                    return StatusCode(500, new { message = $"Ошибка удаления всех товаров: {innerException}" });
                }
            }
        }
    }
}
