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
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

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

        // CRUD для категорий
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
        public async Task<(string token, string role)> LoginAsync(string username, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login",
                new { username, password });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка входа: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResult>();
            SetToken(result.Token);
            return (result.Token, result.Role);
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            var response = await _httpClient.GetAsync("api/categories");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Category>>();
        }

       
        public async Task<List<Product>> GetProductsAsync(int? categoryId = null)
        {
            var url = categoryId.HasValue
                ? $"api/products?categoryId={categoryId.Value}"
                : "api/products";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<Product>>();
        }



       

        public async Task DeleteOrderAsync(int orderId)
        {
            var response = await _httpClient.DeleteAsync($"api/orders/{orderId}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка удаления заказа: {response.StatusCode} - {error}");
            }
        }
        public async Task<int> CreateOrderAsync(string customerName, string phone,
            string address, List<CartItem> items)
        {
            var orderData = new
            {
                customerName,
                phone,
                address,
                items = items.ConvertAll(i => new
                {
                    productId = i.Product.Id,
                    quantity = i.Quantity,
                    price = i.Product.Price
                })
            };

            var response = await _httpClient.PostAsJsonAsync("api/orders", orderData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка создания заказа: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<OrderResult>();
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


        public async Task<string> UploadImageAsync(string filePath)
        {
            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var fileContent = new StreamContent(fileStream);

            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync("api/upload", form);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ошибка загрузки: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<UploadResult>();
            return result.ImageUrl;
        }

        private class UploadResult
        {
            public string ImageUrl { get; set; } = string.Empty;
            public string FileName { get; set; } = string.Empty;
        }
        private class LoginResult
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }

        private class OrderResult
        {
            public int OrderId { get; set; }
            public decimal TotalAmount { get; set; }
        }

    }
}