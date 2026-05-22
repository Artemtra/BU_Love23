using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        public AdminPanelWindow(ApiService api)
        {
            InitializeComponent();
            _api = api;
            Loaded += async (s, e) => await LoadAllData();
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
                ShowOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void RefreshProductsList()
        {
            ProductsList.ItemsSource = null;
            ProductsList.ItemsSource = _products;
        }

        private void RefreshCategoriesList()
        {
            CategoriesList.ItemsSource = null;
            CategoriesList.ItemsSource = _categories;
        }

        // ====== ВКЛАДКИ ======
        private void ShowProducts_Click(object sender, RoutedEventArgs e)
        {
            ProductsPanel.Visibility = Visibility.Visible;
            CategoriesPanel.Visibility = Visibility.Collapsed;
            OrdersPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowCategories_Click(object sender, RoutedEventArgs e)
        {
            ProductsPanel.Visibility = Visibility.Collapsed;
            CategoriesPanel.Visibility = Visibility.Visible;
            OrdersPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowOrders_Click(object sender, RoutedEventArgs e)
        {
            ProductsPanel.Visibility = Visibility.Collapsed;
            CategoriesPanel.Visibility = Visibility.Collapsed;
            OrdersPanel.Visibility = Visibility.Visible;
            ShowOrders();
        }

        // ====== ЗАКАЗЫ ======
        private void ShowOrders()
        {
            OrdersList.Children.Clear();

            if (_orders == null || !_orders.Any())
            {
                OrdersList.Children.Add(new TextBlock
                {
                    Text = "Заказов пока нет",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (var order in _orders)
            {
                var orderCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20),
                    Margin = new Thickness(0, 5, 0, 5)
                };

                var mainStack = new StackPanel();
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var infoStack = new StackPanel();
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"Заказ №{order.Id}",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00))
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"Дата: {order.OrderDate:dd.MM.yyyy HH:mm}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    Margin = new Thickness(0, 3, 0, 0)
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"Покупатель: {order.CustomerName}",
                    FontSize = 16,
                    Margin = new Thickness(0, 8, 0, 0)
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"Телефон: {order.Phone}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"Адрес: {order.Address}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                });

                Grid.SetColumn(infoStack, 0);
                headerGrid.Children.Add(infoStack);

                var priceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                priceStack.Children.Add(new TextBlock
                {
                    Text = $"{order.TotalAmount:C}",
                    FontSize = 28,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))
                });

                var deleteBtn = new Button
                {
                    Content = "🗑️ Удалить заказ",
                    Background = new SolidColorBrush(Color.FromRgb(0xff, 0x44, 0x44)),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(15, 8, 15, 8),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 10, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Tag = order.Id
                };
                deleteBtn.Click += DeleteOrder_Click;
                priceStack.Children.Add(deleteBtn);

                Grid.SetColumn(priceStack, 1);
                headerGrid.Children.Add(priceStack);
                mainStack.Children.Add(headerGrid);

                if (order.Orderitems != null && order.Orderitems.Any())
                {
                    mainStack.Children.Add(new TextBlock
                    {
                        Text = "Товары:",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 15, 0, 5)
                    });

                    foreach (var item in order.Orderitems)
                    {
                        var itemBorder = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1a)),
                            CornerRadius = new CornerRadius(5),
                            Padding = new Thickness(10),
                            Margin = new Thickness(0, 3, 0, 3)
                        };

                        var itemGrid = new Grid();
                        itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var itemInfo = new StackPanel();
                        itemInfo.Children.Add(new TextBlock
                        {
                            Text = item.Product?.Name ?? $"Товар ID: {item.ProductId}",
                            FontSize = 14,
                            FontWeight = FontWeights.Bold
                        });
                        itemInfo.Children.Add(new TextBlock
                        {
                            Text = $"Кол-во: {item.Quantity}",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                        });

                        Grid.SetColumn(itemInfo, 0);
                        itemGrid.Children.Add(itemInfo);

                        Grid.SetColumn(new TextBlock
                        {
                            Text = $"{item.Price:C}",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                            VerticalAlignment = VerticalAlignment.Center
                        }, 1);
                        itemGrid.Children.Add(new TextBlock
                        {
                            Text = $"{item.Price:C}",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                            VerticalAlignment = VerticalAlignment.Center
                        });

                        itemBorder.Child = itemGrid;
                        mainStack.Children.Add(itemBorder);
                    }
                }

                orderCard.Child = mainStack;
                OrdersList.Children.Add(orderCard);
            }
        }

        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                var result = MessageBox.Show($"Удалить заказ №{orderId}?\nТовары будут возвращены на склад.",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _api.DeleteOrderAsync(orderId);
                        _orders = await _api.GetOrdersAsync();
                        ShowOrders();
                        MessageBox.Show("Заказ удален!", "Успех");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}");
                    }
                }
            }
        }

        private async void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _orders = await _api.GetOrdersAsync();
                ShowOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}");
            }
        }

        // ====== ТОВАРЫ ======
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            _editingProductId = 0;
            _productImageUrl = "";
            ProductFormTitle.Text = "➕ ДОБАВЛЕНИЕ ТОВАРА";
            ProductName.Text = "";
            ProductDesc.Text = "";
            ProductPrice.Text = "";
            ProductCategory.Text = "";
            ProductStock.Text = "";
            ProductCondition.Text = "Good";
            ProductImagePath.Text = "";
            ProductForm.Visibility = Visibility.Visible;
            CategoryForm.Visibility = Visibility.Collapsed;
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                _editingProductId = product.Id;
                _productImageUrl = product.ImageUrl ?? "";
                ProductFormTitle.Text = "✏️ РЕДАКТИРОВАНИЕ ТОВАРА";
                ProductName.Text = product.Name ?? "";
                ProductDesc.Text = product.Description ?? "";
                ProductPrice.Text = product.Price.ToString();
                ProductCategory.Text = product.CategoryId.ToString();
                ProductStock.Text = product.StockQuantity.ToString();
                ProductCondition.Text = product.Condition ?? "";
                ProductImagePath.Text = product.ImageUrl ?? "";
                ProductForm.Visibility = Visibility.Visible;
                CategoryForm.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProductName.Text))
                {
                    MessageBox.Show("Введите название товара", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!decimal.TryParse(ProductPrice.Text, out decimal price) || price < 0)
                {
                    MessageBox.Show("Введите корректную цену", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(ProductCategory.Text, out int categoryId))
                {
                    MessageBox.Show("Введите корректный ID категории", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(ProductStock.Text, out int stock) || stock < 0)
                {
                    MessageBox.Show("Введите корректное количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var product = new Product
                {
                    Id = _editingProductId,
                    Name = ProductName.Text.Trim(),
                    Description = ProductDesc.Text?.Trim() ?? "",
                    Price = price,
                    CategoryId = categoryId,
                    StockQuantity = stock,
                    Condition = ProductCondition.Text?.Trim() ?? "Good",
                    ImageUrl = _productImageUrl
                };

                if (_editingProductId == 0)
                    await _api.CreateProductAsync(product);
                else
                    await _api.UpdateProductAsync(_editingProductId, product);

                _products = await _api.GetProductsAsync();
                RefreshProductsList();
                ProductForm.Visibility = Visibility.Collapsed;
                MessageBox.Show("Сохранено!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка");
            }
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                if (MessageBox.Show($"Удалить товар \"{product.Name}\"?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _api.DeleteProductAsync(product.Id);
                        _products = await _api.GetProductsAsync();
                        RefreshProductsList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
        }

        private async void UploadProductImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Все файлы|*.*",
                Title = "Выберите фото для товара"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imageUrl = await _api.UploadImageAsync(dialog.FileName);
                    _productImageUrl = imageUrl;
                    ProductImagePath.Text = "✓ Загружено: " + imageUrl;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
                }
            }
        }

        // ====== КАТЕГОРИИ ======
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            _editingCategoryId = 0;
            _categoryImageUrl = "";
            CategoryFormTitle.Text = "➕ ДОБАВЛЕНИЕ КАТЕГОРИИ";
            CategoryName.Text = "";
            CategoryImagePath.Text = "";
            CategoryForm.Visibility = Visibility.Visible;
            ProductForm.Visibility = Visibility.Collapsed;
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                _editingCategoryId = category.Id;
                _categoryImageUrl = category.ImageUrl ?? "";
                CategoryFormTitle.Text = "✏️ РЕДАКТИРОВАНИЕ КАТЕГОРИИ";
                CategoryName.Text = category.Name ?? "";
                CategoryImagePath.Text = category.ImageUrl ?? "";
                CategoryForm.Visibility = Visibility.Visible;
                ProductForm.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CategoryName.Text))
                {
                    MessageBox.Show("Введите название категории");
                    return;
                }

                var category = new Category
                {
                    Id = _editingCategoryId,
                    Name = CategoryName.Text.Trim(),
                    ImageUrl = _categoryImageUrl
                };

                if (_editingCategoryId == 0)
                    await _api.CreateCategoryAsync(category);
                else
                    await _api.UpdateCategoryAsync(_editingCategoryId, category);

                _categories = await _api.GetCategoriesAsync();
                RefreshCategoriesList();
                CategoryForm.Visibility = Visibility.Collapsed;
                MessageBox.Show("Сохранено!", "Успех");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                if (MessageBox.Show($"Удалить категорию \"{category.Name}\"?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _api.DeleteCategoryAsync(category.Id);
                        _categories = await _api.GetCategoriesAsync();
                        RefreshCategoriesList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
        }

        private async void UploadCategoryImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Все файлы|*.*",
                Title = "Выберите фото для категории"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imageUrl = await _api.UploadImageAsync(dialog.FileName);
                    _categoryImageUrl = imageUrl;
                    CategoryImagePath.Text = "✓ Загружено: " + imageUrl;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
                }
            }
        }

        private void CancelForm_Click(object sender, RoutedEventArgs e)
        {
            ProductForm.Visibility = Visibility.Collapsed;
            CategoryForm.Visibility = Visibility.Collapsed;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}