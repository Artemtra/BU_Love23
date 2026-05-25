using BU_Love.Models;
using BU_Love.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BU_Love.Views
{
    public partial class ProfileWindow : Window
    {
        private readonly ApiService _api;
        private readonly MainViewModel _viewModel;
        private string _editingField = "";

        public ProfileWindow(ApiService api, MainViewModel viewModel)
        {
            InitializeComponent();
            _api = api;
            _viewModel = viewModel;
            Loaded += (s, e) => LoadProfile();
        }

        private void LoadProfile()
        {
            var user = _api.CurrentUser;
            if (user == null) { Close(); return; }

            UsernameTextBox.Text = user.Username;
            PhoneTextBox.Text = user.Phone;
            AddressTextBox.Text = user.Address;
            BonusText.Text = $"🎁 {user.BonusPointsDisplay}";
        }

        private void EditPhone_Click(object sender, RoutedEventArgs e)
        {
            _editingField = "phone";
            PhoneTextBox.IsReadOnly = false;
            PhoneTextBox.Focus();
            PhoneTextBox.SelectAll();
            SavePanel.Visibility = Visibility.Visible;
        }

        private void EditAddress_Click(object sender, RoutedEventArgs e)
        {
            _editingField = "address";
            AddressTextBox.IsReadOnly = false;
            AddressTextBox.Focus();
            AddressTextBox.SelectAll();
            SavePanel.Visibility = Visibility.Visible;
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            _editingField = "";
            PhoneTextBox.IsReadOnly = true;
            AddressTextBox.IsReadOnly = true;
            SavePanel.Visibility = Visibility.Collapsed;
            LoadProfile();
        }

        private async void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;
                string phone = PhoneTextBox.Text.Trim();
                string address = AddressTextBox.Text.Trim();

                if (_editingField == "phone")
                {
                    if (!phone.StartsWith("+"))
                    { MessageBox.Show("Номер должен начинаться с +"); return; }
                    if (phone.Length != 12)
                    { MessageBox.Show("Номер должен содержать + и 11 цифр"); return; }
                    if (!phone.Substring(1).All(char.IsDigit))
                    { MessageBox.Show("Только цифры после +"); return; }
                }
                else if (_editingField == "address")
                {
                    if (string.IsNullOrWhiteSpace(address))
                    { MessageBox.Show("Введите адрес"); return; }
                }

                await _api.UpdateProfileAsync(phone, address);
                MessageBox.Show("Профиль обновлен!", "✅ Успех");

                _editingField = "";
                PhoneTextBox.IsReadOnly = true;
                AddressTextBox.IsReadOnly = true;
                SavePanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            { MessageBox.Show($"Ошибка: {ex.Message}"); }
            finally { IsEnabled = true; }
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new CartWindow(_viewModel, _api);
            cartWindow.Owner = this;
            cartWindow.ShowDialog();
            LoadProfile();
        }

        private async void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileSection.Visibility = Visibility.Collapsed;
            OrdersSection.Visibility = Visibility.Visible;

            try
            {
                var orders = await _api.GetOrdersAsync();
                var userOrders = orders
                    .Where(o => o.CustomerName == _api.CurrentUser.Username && o.Phone == _api.CurrentUser.Phone)
                    .OrderByDescending(o => o.OrderDate).ToList();

                OrdersPanel.Children.Clear();

                if (!userOrders.Any())
                {
                    OrdersPanel.Children.Add(new TextBlock
                    {
                        Text = "📭 У вас пока нет заказов",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                }
                else
                {
                    foreach (var order in userOrders)
                    {
                        var border = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                            CornerRadius = new CornerRadius(10),
                            Padding = new Thickness(15),
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        var stack = new StackPanel();

                        var headerGrid = new Grid();
                        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var hi = new StackPanel();
                        hi.Children.Add(new TextBlock
                        {
                            Text = $"📦 Заказ №{order.Id}",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00))
                        });
                        hi.Children.Add(new TextBlock
                        {
                            Text = order.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                        });
                        Grid.SetColumn(hi, 0); headerGrid.Children.Add(hi);

                        var pb = new TextBlock
                        {
                            Text = $"{order.TotalAmount:C}",
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(pb, 1); headerGrid.Children.Add(pb);
                        stack.Children.Add(headerGrid);
                        stack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(0x3d, 0x3d, 0x3d)), Margin = new Thickness(0, 8, 0, 8) });

                        if (order.Orderitems != null && order.Orderitems.Any())
                        {
                            foreach (var item in order.Orderitems)
                            {
                                var ig = new Grid();
                                ig.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                ig.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                                ig.Children.Add(new TextBlock { Text = item.Product?.Name ?? $"Товар ID: {item.ProductId}", FontSize = 13, Foreground = Brushes.White });
                                Grid.SetColumn(ig.Children[0], 0);
                                ig.Children.Add(new TextBlock
                                {
                                    Text = $"{item.Quantity} шт. x {item.Price:C}",
                                    FontSize = 13,
                                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                                    Margin = new Thickness(10, 0, 0, 0)
                                });
                                Grid.SetColumn(ig.Children[1], 1);
                                stack.Children.Add(ig);
                            }
                        }

                        stack.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(0x3d, 0x3d, 0x3d)), Margin = new Thickness(0, 8, 0, 5) });
                        stack.Children.Add(new TextBlock
                        {
                            Text = $"📍 {order.Address}",
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                        });

                        border.Child = stack;
                        OrdersPanel.Children.Add(border);
                    }
                }
            }
            catch (Exception ex)
            {
                OrdersPanel.Children.Add(new TextBlock
                {
                    Text = $"❌ Ошибка: {ex.Message}",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x44, 0x44))
                });
            }
        }

        private void HideOrders_Click(object sender, RoutedEventArgs e)
        {
            OrdersSection.Visibility = Visibility.Collapsed;
            ProfileSection.Visibility = Visibility.Visible;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) { _api.Logout(); Close(); }
        private void CloseButton_Click(object sender, RoutedEventArgs e) { Close(); }
    }
}