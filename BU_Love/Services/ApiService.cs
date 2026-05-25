using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BU_Love.Models;

namespace BU_Love.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private UserProfile _currentUser;

        public UserProfile CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        public bool IsLoggedIn => _currentUser != null;

        public ApiService(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = "http://localhost:5000";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public void SetToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                _currentUser = null;
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // ===== ТОВАРЫ =====
        public async Task<Product> CreateProductAsync(Product product)
        {
            var response = await _httpClient.PostAsJsonAsync("api/products", product);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Product>();
        }

        public async Task UpdateProductAsync(int id, Product product)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", product);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteProductAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/products/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAllProductsAsync()
        {
            var response = await _httpClient.DeleteAsync("api/products/delete-all");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка удаления всех товаров: {response.StatusCode} - {error}");
            }
        }

        // ===== КАТЕГОРИИ =====
        public async Task<Category> CreateCategoryAsync(Category category)
        {
            var response = await _httpClient.PostAsJsonAsync("api/categories", category);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Category>();
        }

        public async Task UpdateCategoryAsync(int id, Category category)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/categories/{id}", category);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/categories/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteAllCategoriesAsync()
        {
            var response = await _httpClient.DeleteAsync("api/categories/delete-all");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка удаления всех категорий: {response.StatusCode} - {error}");
            }
        }

        // ===== АВТОРИЗАЦИЯ =====
        public async Task<UserProfile> LoginAsync(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login",
                new { username, password });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка входа: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            SetToken(result.Token);

            _currentUser = new UserProfile
            {
                Username = result.Username ?? "",
                Role = result.Role ?? "Customer",
                Phone = result.Phone ?? "",
                Address = result.Address ?? "",
                BonusPoints = result.BonusPoints
            };

            return _currentUser;
        }

        public async Task<UserProfile> RegisterAsync(string username, string password,
     string phone, string address)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register",
                new { username, password, phone, address });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка регистрации: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            SetToken(result.Token);

            _currentUser = new UserProfile
            {
                Username = result.Username ?? "",
                Role = result.Role ?? "Customer",
                Phone = result.Phone ?? "",
                Address = result.Address ?? "",
                BonusPoints = result.BonusPoints
            };

            return _currentUser;
        }

        public void Logout()
        {
            SetToken(null);
            _currentUser = null;
        }

        // ===== КАТЕГОРИИ (получение) =====
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var response = await _httpClient.GetAsync("api/categories");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Category>>();
        }

        // ===== ТОВАРЫ (получение) =====
        public async Task<List<Product>> GetProductsAsync(int? categoryId = null)
        {
            var url = categoryId.HasValue
                ? $"api/products?categoryId={categoryId.Value}"
                : "api/products";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Product>>();
        }

        // ===== ЗАКАЗЫ =====
        public async Task DeleteOrderAsync(int orderId)
        {
            var response = await _httpClient.DeleteAsync($"api/orders/{orderId}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка удаления заказа: {response.StatusCode} - {error}");
            }
        }

        public async Task DeleteAllOrdersAsync()
        {
            var response = await _httpClient.DeleteAsync("api/orders/delete-all");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка удаления всех заказов: {response.StatusCode} - {error}");
            }
        }

        public async Task<int> CreateOrderAsync(string customerName, string phone,
    string address, List<CartItem> items, bool useBonusPoints = false,
    decimal bonusPointsToUse = 0)
        {
            var orderData = new
            {
                customerName,
                phone,
                address,
                useBonusPoints,
                bonusPointsToUse,
                items = items.ConvertAll(i => new
                {
                    productId = i.Product.Id,
                    quantity = i.Quantity,
                    price = i.Product.Price
                })
            };

            // Проверяем, передан ли токен
            Console.WriteLine($"Token: {_httpClient.DefaultRequestHeaders.Authorization}");

            var response = await _httpClient.PostAsJsonAsync("api/orders", orderData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка создания заказа: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<OrderResult>();

            if (IsLoggedIn)
            {
                await RefreshUserProfileAsync();
            }

            return result.OrderId;
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            var response = await _httpClient.GetAsync("api/orders");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка загрузки заказов: {response.StatusCode} - {error}");
            }
            return await response.Content.ReadFromJsonAsync<List<Order>>();
        }

        // ===== ПРОФИЛЬ =====
        public async Task<UserProfile> GetProfileAsync()
        {
            var response = await _httpClient.GetAsync("api/auth/profile");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Не удалось загрузить профиль");
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            _currentUser = new UserProfile
            {
                Username = result.Username ?? "",
                Role = result.Role ?? "Customer",
                Phone = result.Phone ?? "",
                Address = result.Address ?? "",
                BonusPoints = result.BonusPoints
            };
            return _currentUser;
        }

        private async Task RefreshUserProfileAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/auth/profile");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                    if (result != null && _currentUser != null)
                    {
                        _currentUser.BonusPoints = result.BonusPoints;
                    }
                }
            }
            catch { }
        }

        // ===== ЗАГРУЗКА ИЗОБРАЖЕНИЙ =====
        public async Task<string> UploadImageAsync(string filePath)
        {
            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync("api/upload", form);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка загрузки: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<UploadResult>();
            return result?.ImageUrl ?? "";
        }

        // ===== ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ =====
        private class UploadResult
        {
            public string ImageUrl { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
        }

        private class AuthResult
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public decimal BonusPoints { get; set; }
        }

        private class OrderResult
        {
            public int OrderId { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }
}