using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BU_Love.Models;
using BU_Love.Services;

namespace BU_Love.Views
{
    public partial class CartWindow : Window
    {
        private readonly MainViewModel _mainVm;
        private readonly ApiService _api;
        private bool _isLoggedIn;

        public CartWindow(MainViewModel mainViewModel, ApiService api = null)
        {
            InitializeComponent();
            _mainVm = mainViewModel;
            _api = api;
            _isLoggedIn = _api != null && _api.IsLoggedIn;
            Loaded += (s, e) => RefreshCart();
        }
        private void RefreshCart()
        {
            CartItemsPanel.Children.Clear();

            if (!_mainVm.CartItems.Any())
            {
                CartItemsPanel.Children.Add(new TextBlock
                {
                    Text = "Корзина пуста",
                    FontSize = 24,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                });
                GuestDataPanel.Visibility = Visibility.Collapsed;
                BonusPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Показываем нужную панель в зависимости от авторизации
                if (_isLoggedIn)
                {
                    GuestDataPanel.Visibility = Visibility.Collapsed;
                    BonusPanel.Visibility = Visibility.Visible;

                    var user = _api.CurrentUser;
                    BonusInfoText.Text = $"Доступно: {user.BonusPointsDisplay}";
                }
                else
                {
                    GuestDataPanel.Visibility = Visibility.Visible;
                    BonusPanel.Visibility = Visibility.Collapsed;
                }

                foreach (var item in _mainVm.CartItems)
                {
                    var border = new Border
                    {
                        Padding = new Thickness(20),
                        Margin = new Thickness(0, 10, 0, 10),
                        Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                        CornerRadius = new CornerRadius(10)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });

                    var image = new Image
                    {
                        Width = 80,
                        Height = 80,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    if (!string.IsNullOrEmpty(item.Product.ImageUrl))
                    {
                        try
                        {
                            var imageUrl = "http://localhost:5000" + item.Product.ImageUrl;
                            image.Source = new BitmapImage(new Uri(imageUrl, UriKind.Absolute));
                        }
                        catch { }
                    }
                    Grid.SetColumn(image, 0);
                    grid.Children.Add(image);

                    var infoStack = new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(15, 0, 15, 0)
                    };
                    infoStack.Children.Add(new TextBlock { Text = item.Product.Name, FontSize = 20, FontWeight = FontWeights.Bold });
                    infoStack.Children.Add(new TextBlock { Text = item.Product.Description, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 0) });
                    infoStack.Children.Add(new TextBlock { Text = $"Цена: {item.Product.Price:C}", FontSize = 16, Margin = new Thickness(0, 10, 0, 0) });
                    Grid.SetColumn(infoStack, 1);
                    grid.Children.Add(infoStack);

                    var rightStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };

                    var quantityPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };

                    var minusBtn = new Button { Content = "−", Width = 30, Height = 30, FontSize = 18, FontWeight = FontWeights.Bold, Background = new SolidColorBrush(Color.FromRgb(0xff, 0x44, 0x44)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Tag = item };
                    minusBtn.Click += MinusButton_Click;
                    quantityPanel.Children.Add(minusBtn);

                    quantityPanel.Children.Add(new TextBlock { Text = item.Quantity.ToString(), FontSize = 18, FontWeight = FontWeights.Bold, Width = 40, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center });

                    var plusBtn = new Button { Content = "+", Width = 30, Height = 30, FontSize = 18, FontWeight = FontWeights.Bold, Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Tag = item };
                    plusBtn.Click += PlusButton_Click;
                    quantityPanel.Children.Add(plusBtn);
                    rightStack.Children.Add(quantityPanel);

                    rightStack.Children.Add(new TextBlock { Text = $"{item.TotalPrice:C}", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)), HorizontalAlignment = HorizontalAlignment.Center });

                    var removeBtn = new Button { Content = "✕ Удалить", Background = new SolidColorBrush(Color.FromRgb(0xff, 0x44, 0x44)), Foreground = Brushes.White, FontSize = 14, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, Tag = item };
                    removeBtn.Click += RemoveButton_Click;
                    rightStack.Children.Add(removeBtn);

                    Grid.SetColumn(rightStack, 2);
                    grid.Children.Add(rightStack);
                    border.Child = grid;
                    CartItemsPanel.Children.Add(border);
                }
            }
            UpdateTotals();
        }

        private void PlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CartItem item)
            {
                if (item.Quantity + 1 > item.Product.StockQuantity)
                {
                    MessageBox.Show($"Нельзя добавить больше! В наличии только {item.Product.StockQuantity} шт.", "Ограничение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                item.Quantity++;
                RefreshCart();
            }
        }

        private void MinusButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CartItem item)
            {
                if (item.Quantity > 1) item.Quantity--;
                else _mainVm.CartItems.Remove(item);
                RefreshCart();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CartItem item)
            {
                _mainVm.CartItems.Remove(item);
                RefreshCart();
            }
        }

        private void UseBonusCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            var total = _mainVm.CartItems.Sum(i => i.TotalPrice);
            var count = _mainVm.CartItems.Sum(i => i.Quantity);

            if (UseBonusCheckBox.IsChecked == true && _isLoggedIn)
            {
                var bonusToUse = Math.Min(_api.CurrentUser.BonusPoints, total);
                total -= bonusToUse;
                BonusAfterPurchaseText.Text = $"Будет списано {bonusToUse:N0} бонусов";
            }
            else if (_isLoggedIn)
            {
                var bonusToEarn = total * 0.01m;
                BonusAfterPurchaseText.Text = $"Будет начислено {bonusToEarn:N0} бонусов (1%)";
            }

            TotalAmountText.Text = $"{total:C}";
            TotalCountText.Text = count.ToString();
        }

        private void ClearCartButton_Click(object sender, RoutedEventArgs e)
        {
            _mainVm.CartItems.Clear();
            RefreshCart();
        }

        private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_mainVm.CartItems.Any())
            {
                MessageBox.Show("Корзина пуста!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string customerName, phone, address;

            if (_isLoggedIn)
            {
                customerName = _api.CurrentUser.Username;
                phone = _api.CurrentUser.Phone;
                address = _api.CurrentUser.Address;
            }
            else
            {
                customerName = CustomerNameTextBox.Text?.Trim();
                phone = PhoneTextBox.Text?.Trim();
                address = AddressTextBox.Text?.Trim();

                if (string.IsNullOrWhiteSpace(customerName))
                {
                    MessageBox.Show("Введите имя", "Ошибка");
                    return;
                }
                if (string.IsNullOrWhiteSpace(phone))
                {
                    MessageBox.Show("Введите телефон", "Ошибка");
                    return;
                }
                if (string.IsNullOrWhiteSpace(address))
                {
                    MessageBox.Show("Введите адрес", "Ошибка");
                    return;
                }
            }

            try
            {
                IsEnabled = false;

                var items = _mainVm.CartItems.ToList();
                var totalAmount = _mainVm.CartItems.Sum(i => i.TotalPrice);

                bool useBonus = false;
                decimal bonusToUse = 0;

                if (_isLoggedIn && UseBonusCheckBox.IsChecked == true)
                {
                    useBonus = true;
                    bonusToUse = Math.Min(_api.CurrentUser.BonusPoints, totalAmount);
                }

                var orderId = await _api.CreateOrderAsync(
                    customerName,
                    phone,
                    address,
                    items,
                    useBonus,
                    bonusToUse);

                _mainVm.CartItems.Clear();

                var finalAmount = totalAmount - bonusToUse;
                decimal bonusEarned = 0;

                // Обновляем бонусы локально (без запроса к серверу)
                if (_isLoggedIn && _api.CurrentUser != null)
                {
                    if (useBonus)
                    {
                        _api.CurrentUser.BonusPoints -= bonusToUse;
                    }
                    else
                    {
                        bonusEarned = finalAmount * 0.01m;
                        _api.CurrentUser.BonusPoints += bonusEarned;
                    }
                }

                string message = $"✅ Заказ №{orderId} оформлен!\n\n";
                message += $"Сумма заказа: {totalAmount:C}\n";
                if (useBonus)
                    message += $"Списано бонусов: {bonusToUse:N0}\n";
                message += $"Итого: {finalAmount:C}\n";
                if (bonusEarned > 0)
                    message += $"Начислено бонусов: {bonusEarned:N0} (1%)\n";
                message += "\nСпасибо за покупку! 🎉";

                MessageBox.Show(message, "Заказ оформлен", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
            finally
            {
                IsEnabled = true;
            }
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}