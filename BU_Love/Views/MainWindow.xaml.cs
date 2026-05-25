using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BU_Love.Models;
using BU_Love.Services;
using BU_Love.Views;

namespace BU_Love
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private readonly ApiService _api;
        private DateTime _lastUpdateTime;

        public MainWindow()
        {
            InitializeComponent();

            _api = new ApiService("http://localhost:5000");
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                await LoadDataAsync();
                UpdateProfileButton();
            };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                RefreshButton.IsEnabled = false;

                await _viewModel.LoadCategoriesAsync();
                ShowCategories();
                UpdateCartInfo();

                _lastUpdateTime = DateTime.Now;
                UpdateInfo.Text = $"Последнее обновление: {_lastUpdateTime:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "❌ Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateInfo.Text = "Ошибка обновления";
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                RefreshButton.IsEnabled = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();

            RefreshButton.Content = "✓ Обновлено";
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, args) =>
            {
                RefreshButton.Content = "🔄 Обновить";
                timer.Stop();
            };
            timer.Start();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_api.IsLoggedIn)
            {
                var loginWindow = new LoginWindow(_api);
                loginWindow.Owner = this;

                if (loginWindow.ShowDialog() == true)
                {
                    UpdateProfileButton();
                    UpdateCartInfo();
                }
            }
            else
            {
                var profileWindow = new ProfileWindow(_api, _viewModel);
                profileWindow.Owner = this;
                profileWindow.ShowDialog();
                UpdateProfileButton();
                UpdateCartInfo();
            }
        }

        private void UpdateProfileButton()
        {
            if (_api.IsLoggedIn)
            {
                ProfileButton.Content = $"👤 {_api.CurrentUser.Username}";
                ProfileButton.Background = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
            }
            else
            {
                ProfileButton.Content = "👤 ВОЙТИ";
                ProfileButton.Background = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
            }
        }

private void CartButton_Click(object sender, RoutedEventArgs e)
{
    var cartWindow = new CartWindow(_viewModel, _api);
    cartWindow.Owner = this;
    cartWindow.ShowDialog();
    UpdateCartInfo();
}

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new AdminLoginWindow(_api);
            loginWindow.Owner = this;

            loginWindow.Closed += async (s, args) =>
            {
                await LoadDataAsync();
            };

            loginWindow.ShowDialog();
        }

        private void UpdateCartInfo()
        {
            var count = _viewModel.CartItems.Count;
            var total = _viewModel.CartItems.Sum(i => i.TotalPrice);

            string bonusInfo = "";
            if (_api.IsLoggedIn)
            {
                bonusInfo = $" | 🎁 {_api.CurrentUser.BonusPointsDisplay}";
            }

            CartInfo.Text = $"🛒 В корзине: {count} товаров на {total:C}{bonusInfo}";
        }

        private void ShowCategories()
        {
            var categoriesPanel = this.FindName("CategoriesPanel") as WrapPanel;
            if (categoriesPanel == null) return;

            categoriesPanel.Children.Clear();

            if (_viewModel.Categories == null || _viewModel.Categories.Count == 0)
            {
                var noDataText = new TextBlock
                {
                    Text = "📭 Нет доступных категорий\nНажмите 'Обновить' для загрузки",
                    FontSize = 24,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 50, 0, 0)
                };
                categoriesPanel.Children.Add(noDataText);
                return;
            }

            foreach (var category in _viewModel.Categories)
            {
                var button = new Button
                {
                    Width = 280,
                    Height = 300,
                    Margin = new Thickness(15),
                    Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d)),
                    BorderThickness = new Thickness(3),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x3d, 0x3d, 0x3d)),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = category
                };

                button.MouseEnter += (s, e) =>
                {
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
                    button.Background = new SolidColorBrush(Color.FromRgb(0x3d, 0x3d, 0x3d));
                };
                button.MouseLeave += (s, e) =>
                {
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3d, 0x3d, 0x3d));
                    button.Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d));
                };

                button.Click += CategoryButton_Click;

                var stack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var emojiText = GetCategoryEmoji(category.Name);
                var emojiBlock = new TextBlock
                {
                    Text = emojiText,
                    FontSize = 80,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                stack.Children.Add(emojiBlock);

                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    try
                    {
                        var image = new Image
                        {
                            Width = 120,
                            Height = 120,
                            Stretch = Stretch.Uniform,
                            Margin = new Thickness(0, 0, 0, 15)
                        };

                        var imageUrl = "http://localhost:5000" + category.ImageUrl;
                        image.Source = new BitmapImage(new Uri(imageUrl, UriKind.Absolute));

                        stack.Children.Remove(emojiBlock);
                        stack.Children.Insert(0, image);
                    }
                    catch { }
                }

                stack.Children.Add(new TextBlock
                {
                    Text = category.Name,
                    FontSize = 26,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                });

                button.Content = stack;
                categoriesPanel.Children.Add(button);
            }
        }

        private string GetCategoryEmoji(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                string s when s.Contains("смартфон") || s.Contains("телефон") => "📱",
                string s when s.Contains("ноутбук") || s.Contains("лэптоп") => "💻",
                string s when s.Contains("планшет") => "📱",
                string s when s.Contains("комплектующ") || s.Contains("запчаст") => "🔧",
                string s when s.Contains("аксессуар") => "🎧",
                string s when s.Contains("часы") || s.Contains("watch") => "⌚",
                string s when s.Contains("аудио") || s.Contains("наушник") => "🎵",
                string s when s.Contains("игров") || s.Contains("приставк") => "🎮",
                string s when s.Contains("монитор") || s.Contains("экран") => "🖥️",
                string s when s.Contains("клавиатур") => "⌨️",
                string s when s.Contains("мыш") || s.Contains("mouse") => "🖱️",
                _ => "📦"
            };
        }

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Category category)
            {
                var productsWindow = new ProductsWindow(category, _viewModel, _api);
                productsWindow.Owner = this;
                productsWindow.ShowDialog();
                UpdateCartInfo();
            }
        }
    }
}