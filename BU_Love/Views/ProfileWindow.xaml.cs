using BU_Love.Models;
using BU_Love.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BU_Love.Views
{
    public partial class ProfileWindow : Window
    {
        private readonly ApiService _api;
        private readonly MainViewModel _viewModel;

        public ProfileWindow(ApiService api, MainViewModel viewModel)
        {
            InitializeComponent();
            _api = api;
            _viewModel = viewModel;

            Loaded += async (s, e) => await LoadProfile();
        }

        private async Task LoadProfile()
        {
            var user = _api.CurrentUser;
            if (user == null)
            {
                Close();
                return;
            }

            UsernameText.Text = $"👤 {user.Username}";
            PhoneText.Text = $"📞 {user.Phone}";
            AddressText.Text = $"📍 {user.Address}";
            BonusText.Text = $"🎁 {user.BonusPointsDisplay}";
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
            try
            {
                var orders = await _api.GetOrdersAsync();

                var userOrders = orders
                    .Where(o => o.CustomerName == _api.CurrentUser.Username ||
                                o.Phone == _api.CurrentUser.Phone)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                OrdersPanel.Children.Clear();

                if (!userOrders.Any())
                {
                    OrdersPanel.Children.Add(new TextBlock
                    {
                        Text = "📭 У вас пока нет заказов",
                        FontSize = 18,
                        Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                }
                else
                {
                    foreach (var order in userOrders)
                    {
                        var orderCard = new Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(15),
                            Margin = new Thickness(0, 0, 0, 10)
                        };

                        var stack = new StackPanel();
                        stack.Children.Add(new TextBlock
                        {
                            Text = $"📦 Заказ №{order.Id} от {order.OrderDate:dd.MM.yyyy}",
                            FontSize = 16,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00))
                        });
                        stack.Children.Add(new TextBlock
                        {
                            Text = $"Сумма: {order.TotalAmount:C}",
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                            Margin = new Thickness(0, 5, 0, 0)
                        });
                        stack.Children.Add(new TextBlock
                        {
                            Text = $"Товаров: {order.Orderitems?.Count ?? 0} шт.",
                            FontSize = 14,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa))
                        });

                        orderCard.Child = stack;
                        OrdersPanel.Children.Add(orderCard);
                    }
                }

                OrdersScrollViewer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}");
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _api.Logout();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}