using BU_Love.Models;
using BU_Love.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BU_Love.Views
{
    public partial class ProductsWindow : Window
    {
        private readonly Category _category;
        private readonly MainViewModel _mainVm;
        private readonly ApiService _api;
        private List<Product> _allProducts;

        public ProductsWindow(Category category, MainViewModel mainViewModel, ApiService api = null)
        {
            InitializeComponent();
            _category = category;
            _mainVm = mainViewModel;
            _api = api;
            CategoryTitle.Text = category.Name.ToUpper();
            Loaded += async (s, e) => await LoadProducts();
        }

        private async Task LoadProducts()
        {
            try
            {
                var allProducts = await _api.GetProductsAsync(_category.Id);

                _allProducts = allProducts
                    .Where(p => p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .ToList();

                DisplayProducts(_allProducts);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private void DisplayProducts(List<Product> products)
        {
            ProductsWrapPanel.Children.Clear();

            if (!products.Any())
            {
                ProductsWrapPanel.Children.Add(new TextBlock
                {
                    Text = "📭 Товары не найдены",
                    FontSize = 24,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                });
                return;
            }

            foreach (var product in products)
            {
                var border = new Border
                {
                    Width = 300,
                    Margin = new Thickness(15),
                    Padding = new Thickness(20),
                    Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                    CornerRadius = new CornerRadius(15),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x3d, 0x3d, 0x3d)),
                    BorderThickness = new Thickness(2)
                };

                var stack = new StackPanel();

                var image = new Image { Width = 200, Height = 200, Stretch = Stretch.Uniform, Margin = new Thickness(0, 0, 0, 15) };
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    try
                    {
                        var imageUrl = "http://localhost:5000" + product.ImageUrl;
                        image.Source = new BitmapImage(new Uri(imageUrl, UriKind.Absolute));
                    }
                    catch { }
                }
                stack.Children.Add(image);

                stack.Children.Add(new TextBlock
                {
                    Text = product.Name,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 5)
                });
                stack.Children.Add(new TextBlock
                {
                    Text = product.Description,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var stockColor = product.StockQuantity <= 3 ? Color.FromRgb(0xFF, 0x98, 0x00) : Color.FromRgb(0x4C, 0xAF, 0x50);
                stack.Children.Add(new TextBlock
                {
                    Text = $"📦 В наличии: {product.StockQuantity} шт.",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(stockColor),
                    Margin = new Thickness(0, 0, 0, 5)
                });

                stack.Children.Add(new TextBlock
                {
                    Text = $"{product.Price:C}",
                    FontSize = 28,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    Margin = new Thickness(0, 0, 0, 15),
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                var button = new Button
                {
                    Content = "🛒 В КОРЗИНУ",
                    Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(15, 12, 15, 12),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = product
                };
                button.Click += AddToCart_Click;
                stack.Children.Add(button);

                border.Child = stack;
                ProductsWrapPanel.Children.Add(border);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                DisplayProducts(_allProducts);
            }
            else
            {
                var filtered = _allProducts.Where(p =>
                    p.Name.ToLower().Contains(searchText) ||
                    (p.Description?.ToLower().Contains(searchText) ?? false) ||
                    p.Price.ToString().Contains(searchText) ||
                    p.Condition.ToLower().Contains(searchText)
                ).ToList();

                DisplayProducts(filtered);
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                var existingItem = _mainVm.CartItems.FirstOrDefault(i => i.Product.Id == product.Id);
                var currentQty = existingItem?.Quantity ?? 0;

                if (currentQty + 1 > product.StockQuantity)
                {
                    MessageBox.Show($"Нельзя добавить больше! Доступно: {product.StockQuantity} шт.",
                        "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _mainVm.AddToCart(product);
                MessageBox.Show($"{product.Name} добавлен в корзину!", "✅ Успех");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) { Close(); }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new CartWindow(_mainVm, _api);
            cartWindow.Owner = this;
            cartWindow.ShowDialog();
        }
    }
}