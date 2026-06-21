using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestProject
{
    public class TestProduct
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public int StockQuantity { get; set; }
        public string Condition { get; set; } = "Good";
    }

    public class TestCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // ==========================================
    // ТЕСТОВЫЙ КОНТЕКСТ БД (InMemory)
    // ==========================================
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestProduct> Products { get; set; }
        public DbSet<TestCategory> Categories { get; set; }
    }

    // ==========================================
    // ТЕСТОВЫЙ КОНТРОЛЛЕР
    // ==========================================
    public class TestProductsController : ControllerBase
    {
        private readonly TestDbContext _context;

        public TestProductsController(TestDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(int? categoryId)
        {
            var query = _context.Products.AsQueryable();
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);
            return Ok(await query.OrderBy(p => p.Name).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] TestProduct product)
        {
            if (string.IsNullOrWhiteSpace(product.Name))
                return BadRequest("Название обязательно");
            if (product.Price < 0)
                return BadRequest("Цена не может быть отрицательной");
            if (product.StockQuantity < 0)
                return BadRequest("Количество не может быть отрицательным");
            if (product.Name.Length > 200)
                return BadRequest("Название превышает 200 символов");

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    // ==========================================
    // ОСНОВНОЙ КЛАСС ТЕСТОВ
    // ==========================================
    public class UnitTest1
    {
        /// <summary>
        /// Создает новый контекст с уникальной InMemory базой для каждого теста
        /// </summary>
        private TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestDbContext(options);
        }

        // ==========================================
        // 1. ПРОВЕРКА ВАЛИДАЦИИ ВХОДНЫХ ДАННЫХ
        // ==========================================

        [Fact]
        public async Task CreateProduct_EmptyName_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);
            var product = new TestProduct { Name = "", Price = 100, StockQuantity = 10 };

            // Act
            var result = await controller.CreateProduct(product);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateProduct_NegativePrice_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);
            var product = new TestProduct { Name = "Test", Price = -500, StockQuantity = 10 };

            // Act
            var result = await controller.CreateProduct(product);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateProduct_NegativeStock_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);
            var product = new TestProduct { Name = "Test", Price = 100, StockQuantity = -5 };

            // Act
            var result = await controller.CreateProduct(product);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ==========================================
        // 2. ГРАНИЧНЫЕ ЗНАЧЕНИЯ
        // ==========================================

        [Fact]
        public async Task CreateProduct_NameExceedsMaxLength_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);
            var longName = new string('A', 201);
            var product = new TestProduct { Name = longName, Price = 100, StockQuantity = 10 };

            // Act
            var result = await controller.CreateProduct(product);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateProduct_ZeroPrice_ReturnsCreated()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);
            var product = new TestProduct { Name = "Free", Price = 0, StockQuantity = 10 };

            // Act
            var result = await controller.CreateProduct(product);

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
        }

        // ==========================================
        // 3. МАППИНГ ENTITY → DTO
        // ==========================================

        [Fact]
        public async Task GetProduct_ReturnsCorrectFields()
        {
            // Arrange
            var context = CreateContext();
            context.Products.Add(new TestProduct
            {
                Name = "iPhone 12",
                Price = 45000,
                StockQuantity = 5,
                Condition = "Excellent"
            });
            context.SaveChanges();
            var controller = new TestProductsController(context);

            // Act
            var result = await controller.GetProduct(1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var product = Assert.IsType<TestProduct>(okResult.Value);

            // Assert
            Assert.Equal("iPhone 12", product.Name);
            Assert.Equal(45000, product.Price);
            Assert.Equal(5, product.StockQuantity);
            Assert.Equal("Excellent", product.Condition);
        }

        // ==========================================
        // 4. ПЕРЕХВАТ ИСКЛЮЧЕНИЙ (NOT FOUND)
        // ==========================================

        [Fact]
        public async Task GetProduct_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);

            // Act
            var result = await controller.GetProduct(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);

            // Act
            var result = await controller.DeleteProduct(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // ==========================================
        // 5. ИНТЕГРАЦИОННЫЙ ТЕСТ: СОЗДАНИЕ И ЗАПИСЬ В БД
        // ==========================================

        [Fact]
        public async Task CreateProduct_ThroughController_SavedInDatabase()
        {
            // Arrange
            var context = CreateContext();
            var controller = new TestProductsController(context);
            var product = new TestProduct
            {
                Name = "Тестовый товар",
                Price = 1500m,
                CategoryId = 1,
                StockQuantity = 10,
                Condition = "Excellent"
            };

            // Act
            var result = await controller.CreateProduct(product);
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var created = Assert.IsType<TestProduct>(createdResult.Value);

            // Assert — проверка ответа
            Assert.Equal("Тестовый товар", created.Name);
            Assert.Equal(1500m, created.Price);

            // Assert — проверка записи в БД
            var inDb = await context.Products.FindAsync(created.Id);
            Assert.NotNull(inDb);
            Assert.Equal("Тестовый товар", inDb.Name);
        }

        // ==========================================
        // 6. ПРОВЕРКА ФИЛЬТРАЦИИ ПО КАТЕГОРИЯМ
        // ==========================================

        [Fact]
        public async Task GetProducts_FilterByCategory_ReturnsFiltered()
        {
            // Arrange
            var context = CreateContext();
            context.Categories.AddRange(
                new TestCategory { Name = "Смартфоны" },
                new TestCategory { Name = "Ноутбуки" }
            );
            context.SaveChanges();

            context.Products.AddRange(
                new TestProduct { Name = "iPhone", CategoryId = 1, Price = 100, StockQuantity = 10 },
                new TestProduct { Name = "Samsung", CategoryId = 1, Price = 200, StockQuantity = 5 },
                new TestProduct { Name = "MacBook", CategoryId = 2, Price = 1000, StockQuantity = 3 }
            );
            context.SaveChanges();

            var controller = new TestProductsController(context);

            // Act
            var result = await controller.GetProducts(categoryId: 1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var products = Assert.IsType<List<TestProduct>>(okResult.Value);

            // Assert
            Assert.Equal(2, products.Count);
            Assert.All(products, p => Assert.Equal(1, p.CategoryId));
        }

        // ==========================================
        // 7. ПРОВЕРКА СОРТИРОВКИ
        // ==========================================

        [Fact]
        public async Task GetProducts_ReturnsSortedByName()
        {
            // Arrange
            var context = CreateContext();
            context.Products.AddRange(
                new TestProduct { Name = "Zulu", Price = 300, StockQuantity = 1 },
                new TestProduct { Name = "Alpha", Price = 100, StockQuantity = 10 },
                new TestProduct { Name = "Mike", Price = 200, StockQuantity = 5 }
            );
            context.SaveChanges();

            var controller = new TestProductsController(context);

            // Act
            var result = await controller.GetProducts(null);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var products = Assert.IsType<List<TestProduct>>(okResult.Value);

            // Assert
            Assert.Equal(3, products.Count);
            Assert.Equal("Alpha", products[0].Name);
            Assert.Equal("Mike", products[1].Name);
            Assert.Equal("Zulu", products[2].Name);
        }
    }
}