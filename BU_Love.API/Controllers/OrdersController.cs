using BU_Love.API.DB;
using BU_Love.API.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BU_Love.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly BuLoveDbContext _context;

        public OrdersController(BuLoveDbContext context)
        {
            _context = context;
        }

        // Создать заказ (доступно всем)
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateOrderDto orderDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(orderDto.CustomerName))
                    return BadRequest("Имя покупателя обязательно");
                if (string.IsNullOrWhiteSpace(orderDto.Phone))
                    return BadRequest("Телефон обязателен");
                if (string.IsNullOrWhiteSpace(orderDto.Address))
                    return BadRequest("Адрес обязателен");
                if (orderDto.Items == null || !orderDto.Items.Any())
                    return BadRequest("Корзина пуста");

                // Проверяем наличие товаров
                foreach (var item in orderDto.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        return BadRequest($"Товар с ID {item.ProductId} не найден");
                    if (product.StockQuantity < item.Quantity)
                        return BadRequest($"Недостаточно товара \"{product.Name}\". В наличии: {product.StockQuantity}");
                    product.StockQuantity -= item.Quantity;
                }

                var totalAmount = orderDto.Items.Sum(i => i.Price * i.Quantity);

                // Получаем ID пользователя из токена
                var userIdClaim = User.FindFirst("userId")?.Value;
                User currentUser = null;

                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    currentUser = await _context.Users.FindAsync(userId);
                }

                // Списание бонусов
                if (orderDto.UseBonusPoints && orderDto.BonusPointsToUse > 0 && currentUser != null)
                {
                    if (currentUser.BonusPoints >= orderDto.BonusPointsToUse)
                    {
                        currentUser.BonusPoints -= (int?)orderDto.BonusPointsToUse;
                        totalAmount -= orderDto.BonusPointsToUse;
                        if (totalAmount < 0) totalAmount = 0;
                    }
                }

                var order = new Order
                {
                    CustomerName = orderDto.CustomerName,
                    Phone = orderDto.Phone,
                    Address = orderDto.Address,
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Orderitems = orderDto.Items.Select(i => new Orderitem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Начисление бонусов 1% (только если НЕ списывали)
                if (!orderDto.UseBonusPoints && currentUser != null)
                {
                    decimal bonusEarned = totalAmount * 0.01m;
                    currentUser.BonusPoints += (int?)bonusEarned;
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(new { OrderId = order.Id, TotalAmount = order.TotalAmount });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Orderitems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = "Заказ не найден" });

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllOrders()
        {
            try
            {
                var allOrderItems = await _context.Orderitems.ToListAsync();
                _context.Orderitems.RemoveRange(allOrderItems);

                var allOrders = await _context.Orders.ToListAsync();
                _context.Orders.RemoveRange(allOrders);

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Удалено заказов: {allOrders.Count}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка удаления: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Orderitems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = "Заказ не найден" });

                foreach (var item in order.Orderitems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += (int)item.Quantity;
                    }
                }

                var orderItems = await _context.Orderitems
                    .Where(oi => oi.OrderId == id)
                    .ToListAsync();
                _context.Orderitems.RemoveRange(orderItems);
                await _context.SaveChangesAsync();

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Заказ удален, товары возвращены на склад" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}