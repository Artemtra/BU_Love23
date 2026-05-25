using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BU_Love.Models;
using BU_Love.Services;
using Microsoft.Win32;

namespace BU_Love.Views
{
    public partial class AdminPanelWindow : Window
    {
        private readonly ApiService _api;
        private List<Product> _products;
        private List<Category> _categories;
        private List<Order> _orders;
        private int _editingProductId = 0;
        private int _editingCategoryId = 0;
        private string _productImageUrl = "";
        private string _categoryImageUrl = "";
        private string _orderSearchText = "";
        public AdminPanelWindow(ApiService api)
        {
            InitializeComponent();
            _api = api;
            Loaded += async (s, e) => await LoadAllData();
        }


        private void OrderSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _orderSearchText = OrderSearchBox.Text?.ToLower() ?? "";
            ShowOrders();
        }
        private void ClearOrderSearch_Click(object sender, RoutedEventArgs e)
        {
            OrderSearchBox.Text = "";
        }
        private async Task LoadAllData()
        {
            try
            {
                _products = await _api.GetProductsAsync();
                _categories = await _api.GetCategoriesAsync();
                _orders = await _api.GetOrdersAsync();
                RefreshProductsList();
                RefreshCategoriesList();
                LoadCategoriesIntoComboBox();
                ShowOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        // ===== ПОИСК =====
        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = ProductSearchBox.Text?.ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(search) ? _products :
                _products.Where(p => p.Name.ToLower().Contains(search) ||
                                    (p.Description?.ToLower().Contains(search) ?? false)).ToList();

            ProductsList.ItemsSource = filtered.OrderBy(p => p.Name).Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.StockQuantity,
                p.Condition,
                p.Description,
                CategoryName = _categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? "Без категории",
                Product = p
            }).ToList();
        }

        private void ClearProductSearch_Click(object sender, RoutedEventArgs e) => ProductSearchBox.Text = "";

        private void CategorySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = CategorySearchBox.Text?.ToLower() ?? "";
            var filtered = string.IsNullOrEmpty(search) ? _categories :
                _categories.Where(c => c.Name.ToLower().Contains(search)).ToList();
            CategoriesList.ItemsSource = filtered.OrderBy(c => c.Name);
        }

        private void ClearCategorySearch_Click(object sender, RoutedEventArgs e) => CategorySearchBox.Text = "";

        // ===== ЗАГРУЗКА =====
        private void LoadCategoriesIntoComboBox()
        {
            ProductCategoryCombo.ItemsSource = _categories.OrderBy(c => c.Name).ToList();
        }

        private void RefreshProductsList()
        {
            ProductsList.ItemsSource = _products.OrderBy(p => p.Name).Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.StockQuantity,
                p.Condition,
                p.Description,
                CategoryName = _categories.FirstOrDefault(c => c.Id == p.CategoryId)?.Name ?? "Без категории",
                Product = p
            }).ToList();
        }

        private void RefreshCategoriesList()
        {
            CategoriesList.ItemsSource = _categories.OrderBy(c => c.Name).ToList();
        }

        // ===== НАВИГАЦИЯ =====
        private void ShowProducts_Click(object sender, RoutedEventArgs e)
        { ProductsPanel.Visibility = Visibility.Visible; CategoriesPanel.Visibility = Visibility.Collapsed; OrdersPanel.Visibility = Visibility.Collapsed; }

        private void ShowCategories_Click(object sender, RoutedEventArgs e)
        { ProductsPanel.Visibility = Visibility.Collapsed; CategoriesPanel.Visibility = Visibility.Visible; OrdersPanel.Visibility = Visibility.Collapsed; }

        private void ShowOrders_Click(object sender, RoutedEventArgs e)
        { ProductsPanel.Visibility = Visibility.Collapsed; CategoriesPanel.Visibility = Visibility.Collapsed; OrdersPanel.Visibility = Visibility.Visible; ShowOrders(); }

        // ===== ТОВАРЫ =====
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            _editingProductId = 0; _productImageUrl = "";
            ProductFormTitle.Text = "Добавление товара";
            ProductName.Text = ""; ProductDesc.Text = ""; ProductPrice.Text = "";
            ProductStock.Text = ""; ProductImagePath.Text = "";
            if (ProductCategoryCombo.Items.Count > 0) ProductCategoryCombo.SelectedIndex = 0;
            ProductConditionCombo.SelectedIndex = 1;
            ProductForm.Visibility = Visibility.Visible; CategoryForm.Visibility = Visibility.Collapsed;
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                _editingProductId = product.Id; _productImageUrl = product.ImageUrl ?? "";
                ProductFormTitle.Text = "Редактирование товара";
                ProductName.Text = product.Name ?? ""; ProductDesc.Text = product.Description ?? "";
                ProductPrice.Text = product.Price.ToString(); ProductStock.Text = product.StockQuantity.ToString();
                ProductImagePath.Text = product.ImageUrl ?? "";
                var category = _categories.FirstOrDefault(c => c.Id == product.CategoryId);
                if (category != null) ProductCategoryCombo.SelectedItem = category;
                foreach (ComboBoxItem item in ProductConditionCombo.Items)
                { if (item.Tag?.ToString() == product.Condition) { item.IsSelected = true; break; } }
                ProductForm.Visibility = Visibility.Visible; CategoryForm.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProductName.Text)) { MessageBox.Show("Введите название"); return; }
                if (ProductCategoryCombo.SelectedItem == null) { MessageBox.Show("Выберите категорию"); return; }
                if (!decimal.TryParse(ProductPrice.Text, out decimal price) || price < 0) { MessageBox.Show("Введите цену"); return; }
                if (!int.TryParse(ProductStock.Text, out int stock) || stock < 0) { MessageBox.Show("Введите количество"); return; }

                var selectedCategory = ProductCategoryCombo.SelectedItem as Category;
                var selectedCondition = (ProductConditionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Good";

                var product = new Product
                {
                    Id = _editingProductId,
                    Name = ProductName.Text.Trim(),
                    Description = ProductDesc.Text?.Trim() ?? "",
                    Price = price,
                    CategoryId = selectedCategory.Id,
                    StockQuantity = stock,
                    Condition = selectedCondition,
                    ImageUrl = _productImageUrl
                };

                if (_editingProductId == 0) await _api.CreateProductAsync(product);
                else await _api.UpdateProductAsync(_editingProductId, product);

                _products = await _api.GetProductsAsync();
                RefreshProductsList();
                ProductForm.Visibility = Visibility.Collapsed;
                MessageBox.Show("Сохранено!", "Успех");
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка"); }
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                if (MessageBox.Show($"Удалить \"{product.Name}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await _api.DeleteProductAsync(product.Id);
                    _products = await _api.GetProductsAsync();
                    RefreshProductsList();
                }
            }
        }

        private async void DeleteAllProducts_Click(object sender, RoutedEventArgs e)
        {
            if (_products == null || !_products.Any()) { MessageBox.Show("Нет товаров"); return; }
            if (MessageBox.Show($"Удалить ВСЕ товары ({_products.Count} шт.)?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                await _api.DeleteAllProductsAsync();
                _products = await _api.GetProductsAsync();
                RefreshProductsList();
                MessageBox.Show("Удалено!");
            }
        }

        private async void UploadProductImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*" };
            if (dialog.ShowDialog() == true)
            {
                try { _productImageUrl = await _api.UploadImageAsync(dialog.FileName); ProductImagePath.Text = "✓ " + _productImageUrl; }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
            }
        }

        // ===== КАТЕГОРИИ =====
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            _editingCategoryId = 0; _categoryImageUrl = "";
            CategoryFormTitle.Text = "Добавление категории";
            CategoryName.Text = ""; CategoryImagePath.Text = "";
            CategoryForm.Visibility = Visibility.Visible; ProductForm.Visibility = Visibility.Collapsed;
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                _editingCategoryId = category.Id; _categoryImageUrl = category.ImageUrl ?? "";
                CategoryFormTitle.Text = "Редактирование категории";
                CategoryName.Text = category.Name ?? ""; CategoryImagePath.Text = category.ImageUrl ?? "";
                CategoryForm.Visibility = Visibility.Visible; ProductForm.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryName.Text)) { MessageBox.Show("Введите название"); return; }
            var category = new Category { Id = _editingCategoryId, Name = CategoryName.Text.Trim(), ImageUrl = _categoryImageUrl };
            if (_editingCategoryId == 0) await _api.CreateCategoryAsync(category);
            else await _api.UpdateCategoryAsync(_editingCategoryId, category);
            _categories = await _api.GetCategoriesAsync();
            RefreshCategoriesList(); LoadCategoriesIntoComboBox();
            CategoryForm.Visibility = Visibility.Collapsed;
            MessageBox.Show("Сохранено!");
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                if (MessageBox.Show($"Удалить \"{category.Name}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await _api.DeleteCategoryAsync(category.Id);
                    _categories = await _api.GetCategoriesAsync();
                    RefreshCategoriesList(); LoadCategoriesIntoComboBox();
                }
            }
        }

        private async void DeleteAllCategories_Click(object sender, RoutedEventArgs e)
        {
            if (_categories == null || !_categories.Any()) { MessageBox.Show("Нет категорий"); return; }
            if (MessageBox.Show($"Удалить ВСЕ категории ({_categories.Count} шт.)?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                await _api.DeleteAllCategoriesAsync();
                _categories = await _api.GetCategoriesAsync();
                _products = await _api.GetProductsAsync();
                RefreshCategoriesList(); RefreshProductsList(); LoadCategoriesIntoComboBox();
            }
        }

        private async void UploadCategoryImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*" };
            if (dialog.ShowDialog() == true)
            {
                try { _categoryImageUrl = await _api.UploadImageAsync(dialog.FileName); CategoryImagePath.Text = "✓ " + _categoryImageUrl; }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}"); }
            }
        }

        // ===== ЗАКАЗЫ =====
        private void ShowOrders()
        {
            OrdersList.Children.Clear();

            if (_orders == null || !_orders.Any())
            {
                OrdersList.Children.Add(new TextBlock
                {
                    Text = "Заказов нет",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            // Фильтрация по поиску
            var filteredOrders = _orders.AsEnumerable();
            if (!string.IsNullOrEmpty(_orderSearchText))
            {
                filteredOrders = filteredOrders.Where(o =>
                    o.Id.ToString().Contains(_orderSearchText) ||
                    (o.CustomerName?.ToLower().Contains(_orderSearchText) ?? false) ||
                    (o.Phone?.Contains(_orderSearchText) ?? false) ||
                    (o.Address?.ToLower().Contains(_orderSearchText) ?? false) ||
                    o.TotalAmount.ToString().Contains(_orderSearchText)
                );
            }

            foreach (var order in filteredOrders.OrderByDescending(o => o.OrderDate))
            {
                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20),
                    Margin = new Thickness(0, 5, 0, 5)
                };
                var stack = new StackPanel();
                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition());
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var info = new StackPanel();
                info.Children.Add(new TextBlock
                {
                    Text = $"Заказ №{order.Id}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00))
                });
                info.Children.Add(new TextBlock
                {
                    Text = order.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                });
                info.Children.Add(new TextBlock { Text = order.CustomerName, FontSize = 16, Margin = new Thickness(0, 8, 0, 0) });
                info.Children.Add(new TextBlock
                {
                    Text = $"📞 {order.Phone}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                });
                info.Children.Add(new TextBlock
                {
                    Text = $"📍 {order.Address}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                });
                Grid.SetColumn(info, 0); header.Children.Add(info);

                var price = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                price.Children.Add(new TextBlock
                {
                    Text = $"{order.TotalAmount:C}",
                    FontSize = 28,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                var delBtn = new Button
                {
                    Content = "🗑️",
                    Tag = order.Id,
                    Background = new SolidColorBrush(Color.FromRgb(0xff, 0x44, 0x44)),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 10, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                delBtn.Click += DeleteOrder_Click; price.Children.Add(delBtn);
                Grid.SetColumn(price, 1); header.Children.Add(price); stack.Children.Add(header);

                if (order.Orderitems != null && order.Orderitems.Any())
                {
                    foreach (var item in order.Orderitems)
                    {
                        var itemBorder = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                            CornerRadius = new CornerRadius(5),
                            Padding = new Thickness(10),
                            Margin = new Thickness(0, 3, 0, 3)
                        };
                        itemBorder.Child = new TextBlock
                        {
                            Text = $"{item.Product?.Name ?? "Товар"} x{item.Quantity} = {item.Price * item.Quantity:C}",
                            FontSize = 14,
                            Foreground = Brushes.White
                        };
                        stack.Children.Add(itemBorder);
                    }
                }
                card.Child = stack; OrdersList.Children.Add(card);
            }
        }

        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                if (MessageBox.Show($"Удалить заказ №{orderId}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await _api.DeleteOrderAsync(orderId);
                    _orders = await _api.GetOrdersAsync(); ShowOrders();
                }
            }
        }

        private async void DeleteAllOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_orders == null || !_orders.Any()) { MessageBox.Show("Нет заказов"); return; }
            if (MessageBox.Show($"Удалить ВСЕ заказы ({_orders.Count} шт.)?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                await _api.DeleteAllOrdersAsync();
                _orders = await _api.GetOrdersAsync(); ShowOrders();
            }
        }

        private async void RefreshOrders_Click(object sender, RoutedEventArgs e)
        { _orders = await _api.GetOrdersAsync(); ShowOrders(); }

        private void CancelForm_Click(object sender, RoutedEventArgs e)
        { ProductForm.Visibility = Visibility.Collapsed; CategoryForm.Visibility = Visibility.Collapsed; }

        private void ExitButton_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}