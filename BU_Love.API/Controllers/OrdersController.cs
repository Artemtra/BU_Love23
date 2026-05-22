using BU_Love.API.DB;
using BU_Love.API.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

                var order = new Order
                {
                    CustomerName = orderDto.CustomerName,
                    Phone = orderDto.Phone,
                    Address = orderDto.Address,
                    OrderDate = DateTime.Now,
                    TotalAmount = orderDto.Items.Sum(i => i.Price * i.Quantity),
                    Orderitems = orderDto.Items.Select(i => new Orderitem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        Price = i.Price
                    }).ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return Ok(new { OrderId = order.Id, TotalAmount = order.TotalAmount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Получить все заказы (только админ)
        [Authorize(Roles = "Admin")]
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

        // Получить заказ по ID (только админ)
        [Authorize(Roles = "Admin")]
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

        // Удалить заказ (только админ)
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

                // Возвращаем товары на склад
                foreach (var item in order.Orderitems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += (int)item.Quantity;
                    }
                }

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Заказ удален, товары возвращены на склад" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
