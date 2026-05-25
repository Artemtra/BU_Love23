using BU_Love.API.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BU_Love.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly BuLoveDbContext _context;

        public CategoriesController(BuLoveDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
        {
            if (id != category.Id)
                return BadRequest();

            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllCategories()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var allCategories = await _context.Categories.ToListAsync();

                if (!allCategories.Any())
                {
                    return Ok(new { message = "Нет категорий для удаления" });
                }

                var categoryIds = allCategories.Select(c => c.Id).ToList();

                var productsToDelete = await _context.Products
                    .Where(p => categoryIds.Contains(p.CategoryId))
                    .ToListAsync();

                var productIds = productsToDelete.Select(p => p.Id).ToList();

                var orderItems = await _context.Orderitems
                    .Where(oi => oi.ProductId.HasValue && productIds.Contains(oi.ProductId.Value))
                    .ToListAsync();

                foreach (var item in orderItems)
                {

                    var product = productsToDelete.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {

                    }
                }


                if (productsToDelete.Any())
                {
                    _context.Products.RemoveRange(productsToDelete);
                    await _context.SaveChangesAsync();
                }

                // Удаляем категории
                _context.Categories.RemoveRange(allCategories);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = $"Удалено категорий: {allCategories.Count}, товаров: {productsToDelete.Count}. История заказов сохранена."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                var innerException = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Ошибка удаления всех категорий: {innerException}" });
            }
        }
    }
}
