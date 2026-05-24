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
                LoadCategoriesIntoComboBox();
                ShowOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        // ===== ЗАГРУЗКА КАТЕГОРИЙ В ВЫПАДАЮЩИЙ СПИСОК =====
        private void LoadCategoriesIntoComboBox()
        {
            ProductCategoryCombo.ItemsSource = null;
            ProductCategoryCombo.ItemsSource = _categories;
        }

        // ===== ОТОБРАЖЕНИЕ СПИСКА ТОВАРОВ =====
        private void RefreshProductsList()
        {
            ProductsList.ItemsSource = null;

            var displayProducts = _products.Select(p => new
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

            ProductsList.ItemsSource = displayProducts;
        }

        // ===== ОТОБРАЖЕНИЕ СПИСКА КАТЕГОРИЙ =====
        private void RefreshCategoriesList()
        {
            CategoriesList.ItemsSource = null;
            CategoriesList.ItemsSource = _categories;
        }

        // ===== НАВИГАЦИЯ =====
        private void ShowProducts_Click(object sender, RoutedEventArgs e)
        {
            ProductsPanel.Visibility = Visibility.Visible;
            CategoriesPanel.Visibility = Visibility.Collapsed;
            OrdersPanel.Visibility = Visibility.Collapsed;

            // Подсвечиваем активную кнопку
            ResetButtonStyles();
            BtnProducts.Background = new SolidColorBrush(Color.FromRgb(0x15, 0x6B, 0xC1));
        }

        private void ShowCategories_Click(object sender, RoutedEventArgs e)
        {
            ProductsPanel.Visibility = Visibility.Collapsed;
            CategoriesPanel.Visibility = Visibility.Visible;
            OrdersPanel.Visibility = Visibility.Collapsed;

            ResetButtonStyles();
            BtnCategories.Background = new SolidColorBrush(Color.FromRgb(0x7B, 0x1F, 0xA2));
        }

        private void ShowOrders_Click(object sender, RoutedEventArgs e)
        {
            ProductsPanel.Visibility = Visibility.Collapsed;
            CategoriesPanel.Visibility = Visibility.Collapsed;
            OrdersPanel.Visibility = Visibility.Visible;
            ShowOrders();

            ResetButtonStyles();
            BtnOrders.Background = new SolidColorBrush(Color.FromRgb(0xBF, 0x36, 0x0C));
        }

        private void ResetButtonStyles()
        {
            BtnProducts.Background = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
            BtnCategories.Background = new SolidColorBrush(Color.FromRgb(0x9C, 0x27, 0xB0));
            BtnOrders.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x57, 0x22));
        }

        // ===== ТОВАРЫ =====
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            _editingProductId = 0;
            _productImageUrl = "";
            ProductFormTitle.Text = "➕ ДОБАВЛЕНИЕ ТОВАРА";
            ProductName.Text = "";
            ProductDesc.Text = "";
            ProductPrice.Text = "";
            ProductStock.Text = "";
            ProductImagePath.Text = "";

            // Выбираем первую категорию по умолчанию
            if (ProductCategoryCombo.Items.Count > 0)
                ProductCategoryCombo.SelectedIndex = 0;

            // Выбираем "Хорошее" состояние по умолчанию
            SelectCondition("Good");

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
                ProductStock.Text = product.StockQuantity.ToString();
                ProductImagePath.Text = product.ImageUrl ?? "";

                // Выбираем категорию товара
                var category = _categories.FirstOrDefault(c => c.Id == product.CategoryId);
                if (category != null)
                    ProductCategoryCombo.SelectedItem = category;

                // Выбираем состояние товара
                SelectCondition(product.Condition);

                ProductForm.Visibility = Visibility.Visible;
                CategoryForm.Visibility = Visibility.Collapsed;
            }
        }

        private void SelectCondition(string condition)
        {
            foreach (ComboBoxItem item in ProductConditionCombo.Items)
            {
                if (item.Tag?.ToString() == condition)
                {
                    item.IsSelected = true;
                    return;
                }
            }
            // По умолчанию выбираем "Good"
            ProductConditionCombo.SelectedIndex = 1;
        }

        private async void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (string.IsNullOrWhiteSpace(ProductName.Text))
                {
                    MessageBox.Show("Введите название товара", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ProductCategoryCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите категорию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(ProductPrice.Text, out decimal price) || price < 0)
                {
                    MessageBox.Show("Введите корректную цену", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(ProductStock.Text, out int stock) || stock < 0)
                {
                    MessageBox.Show("Введите корректное количество", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем выбранную категорию
                var selectedCategory = ProductCategoryCombo.SelectedItem as Category;
                if (selectedCategory == null)
                {
                    MessageBox.Show("Ошибка выбора категории", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Получаем выбранное состояние
                var selectedCondition = (ProductConditionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Good";

                // Создаем объект товара
                var product = new Product
                {
                    Id = _editingProductId,
                    Name = ProductName.Text.Trim(),
                    Description = ProductDesc.Text?.Trim() ?? "",
                    Price = price,
                    CategoryId = selectedCategory.Id,
                    StockQuantity = stock,
                    Condition = selectedCondition,
                    ImageUrl = _productImageUrl ?? ""
                };

                // Сохраняем
                if (_editingProductId == 0)
                    await _api.CreateProductAsync(product);
                else
                    await _api.UpdateProductAsync(_editingProductId, product);

                // Обновляем список
                _products = await _api.GetProductsAsync();
                RefreshProductsList();
                ProductForm.Visibility = Visibility.Collapsed;
                MessageBox.Show("Товар сохранен!", "✅ Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                var result = MessageBox.Show(
                    $"Удалить товар \"{product.Name}\"?\n\nЭто действие нельзя отменить.",
                    "⚠️ Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _api.DeleteProductAsync(product.Id);
                        _products = await _api.GetProductsAsync();
                        RefreshProductsList();
                        MessageBox.Show("Товар удален!", "✅ Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DeleteAllProducts_Click(object sender, RoutedEventArgs e)
        {
            if (_products == null || !_products.Any())
            {
                MessageBox.Show("Нет товаров для удаления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить ВСЕ товары ({_products.Count} шт.)?\n\n" +
                "Это действие нельзя отменить!\n" +
                "История заказов будет сохранена.",
                "⚠️ Подтверждение удаления всех товаров",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsEnabled = false;
                    await _api.DeleteAllProductsAsync();
                    _products = await _api.GetProductsAsync();
                    RefreshProductsList();

                    MessageBox.Show("Все товары успешно удалены!", "✅ Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении товаров: {ex.Message}",
                        "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsEnabled = true;
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
                    IsEnabled = false;
                    ProductImagePath.Text = "Загрузка...";

                    var imageUrl = await _api.UploadImageAsync(dialog.FileName);
                    _productImageUrl = imageUrl;
                    ProductImagePath.Text = "✓ Загружено: " + imageUrl;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ProductImagePath.Text = "Ошибка загрузки";
                }
                finally
                {
                    IsEnabled = true;
                }
            }
        }

        // ===== КАТЕГОРИИ =====
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
                    MessageBox.Show("Введите название категории", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                LoadCategoriesIntoComboBox();
                CategoryForm.Visibility = Visibility.Collapsed;
                MessageBox.Show("Категория сохранена!", "✅ Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                var result = MessageBox.Show(
                    $"Удалить категорию \"{category.Name}\"?\n\nТовары в этой категории останутся без категории.",
                    "⚠️ Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _api.DeleteCategoryAsync(category.Id);
                        _categories = await _api.GetCategoriesAsync();
                        RefreshCategoriesList();
                        LoadCategoriesIntoComboBox();
                        MessageBox.Show("Категория удалена!", "✅ Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DeleteAllCategories_Click(object sender, RoutedEventArgs e)
        {
            if (_categories == null || !_categories.Any())
            {
                MessageBox.Show("Нет категорий для удаления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить ВСЕ категории ({_categories.Count} шт.)?\n\n" +
                "Это действие нельзя отменить!\n" +
                "Все связанные товары также будут удалены.",
                "⚠️ Подтверждение удаления всех категорий",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsEnabled = false;
                    await _api.DeleteAllCategoriesAsync();
                    _categories = await _api.GetCategoriesAsync();
                    _products = await _api.GetProductsAsync();
                    RefreshCategoriesList();
                    RefreshProductsList();
                    LoadCategoriesIntoComboBox();

                    MessageBox.Show("Все категории успешно удалены!", "✅ Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении категорий: {ex.Message}",
                        "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsEnabled = true;
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
                    IsEnabled = false;
                    CategoryImagePath.Text = "Загрузка...";

                    var imageUrl = await _api.UploadImageAsync(dialog.FileName);
                    _categoryImageUrl = imageUrl;
                    CategoryImagePath.Text = "✓ Загружено: " + imageUrl;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    CategoryImagePath.Text = "Ошибка загрузки";
                }
                finally
                {
                    IsEnabled = true;
                }
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
                    Text = "📭 Заказов пока нет",
                    FontSize = 18,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (var order in _orders.OrderByDescending(o => o.OrderDate))
            {
                var orderCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20),
                    Margin = new Thickness(0, 5, 0, 5)
                };

                var mainStack = new StackPanel();

                // Верхняя часть с информацией и суммой
                var headerGrid = new Grid();
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Информация о заказе
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

                // Сумма и кнопка удаления
                var priceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                priceStack.Children.Add(new TextBlock
                {
                    Text = $"{order.TotalAmount:C}",
                    FontSize = 28,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    HorizontalAlignment = HorizontalAlignment.Right
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

                // Разделитель
                mainStack.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(0x4a, 0x4a, 0x4a)),
                    Margin = new Thickness(0, 10, 0, 10)
                });

                // Товары в заказе
                if (order.Orderitems != null && order.Orderitems.Any())
                {
                    mainStack.Children.Add(new TextBlock
                    {
                        Text = "📦 Товары в заказе:",
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 10)
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
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Colors.White)
                        });
                        itemInfo.Children.Add(new TextBlock
                        {
                            Text = $"Количество: {item.Quantity} шт.",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                            Margin = new Thickness(0, 3, 0, 0)
                        });

                        Grid.SetColumn(itemInfo, 0);
                        itemGrid.Children.Add(itemInfo);

                        var itemPriceStack = new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        itemPriceStack.Children.Add(new TextBlock
                        {
                            Text = $"Цена: {item.Price:C}",
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                            HorizontalAlignment = HorizontalAlignment.Right
                        });
                        itemPriceStack.Children.Add(new TextBlock
                        {
                            Text = $"Сумма: {(item.Price * item.Quantity):C}",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 3, 0, 0)
                        });

                        Grid.SetColumn(itemPriceStack, 1);
                        itemGrid.Children.Add(itemPriceStack);

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
                var result = MessageBox.Show(
                    $"Удалить заказ №{orderId}?\nТовары будут возвращены на склад.",
                    "⚠️ Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _api.DeleteOrderAsync(orderId);
                        _orders = await _api.GetOrdersAsync();
                        ShowOrders();
                        MessageBox.Show("Заказ удален!", "✅ Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void DeleteAllOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_orders == null || !_orders.Any())
            {
                MessageBox.Show("Нет заказов для удаления", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить ВСЕ заказы ({_orders.Count} шт.)?\n\n" +
                "Это действие нельзя отменить!\n" +
                "Товары будут возвращены на склад.",
                "⚠️ Подтверждение удаления всех заказов",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsEnabled = false;
                    await _api.DeleteAllOrdersAsync();
                    _orders = await _api.GetOrdersAsync();
                    ShowOrders();

                    MessageBox.Show("Все заказы успешно удалены!", "✅ Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении заказов: {ex.Message}",
                        "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsEnabled = true;
                }
            }
        }

        private async void RefreshOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;
                _orders = await _api.GetOrdersAsync();
                ShowOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления: {ex.Message}", "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        // ===== ОБЩИЕ МЕТОДЫ =====
        private void CancelForm_Click(object sender, RoutedEventArgs e)
        {
            ProductForm.Visibility = Visibility.Collapsed;
            CategoryForm.Visibility = Visibility.Collapsed;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Выйти из админ-панели?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Close();
            }
        }
    }
}