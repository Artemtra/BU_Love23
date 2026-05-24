using BU_Love.Models;
using BU_Love.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BU_Love
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private DateTime _lastUpdateTime;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                await LoadDataAsync();
            };
        }

        private async Task LoadDataAsync()
        {
            try
            {
                // Показываем индикатор загрузки
                LoadingOverlay.Visibility = Visibility.Visible;
                RefreshButton.IsEnabled = false;

                await _viewModel.LoadCategoriesAsync();
                ShowCategories();
                UpdateCartInfo();

                // Обновляем время последнего обновления
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
                // Скрываем индикатор загрузки
                LoadingOverlay.Visibility = Visibility.Collapsed;
                RefreshButton.IsEnabled = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();

            // Анимация кнопки обновления
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

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new CartWindow(_viewModel);
            cartWindow.Owner = this;
            cartWindow.ShowDialog();
            UpdateCartInfo();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            var api = new BU_Love.Services.ApiService("http://localhost:5000");
            var loginWindow = new AdminLoginWindow(api);
            loginWindow.Owner = this;

            // Подписываемся на событие закрытия окна администратора
            loginWindow.Closed += async (s, args) =>
            {
                // Обновляем данные после выхода из админ-панели
                await LoadDataAsync();
            };

            loginWindow.ShowDialog();
        }

        private void UpdateCartInfo()
        {
            var count = _viewModel.CartItems.Count;
            var total = _viewModel.TotalAmount;
            CartInfo.Text = $"🛒 В корзине: {count} товаров на {total:C}";
        }

        private void ShowCategories()
        {
            var categoriesPanel = this.FindName("CategoriesPanel") as WrapPanel;
            if (categoriesPanel == null) return;

            categoriesPanel.Children.Clear();

            if (_viewModel.Categories == null || _viewModel.Categories.Count == 0)
            {
                // Если категорий нет, показываем сообщение
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

                // Добавляем эффекты при наведении
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

                // Иконка категории (эмодзи вместо изображения, если нет картинки)
                var emojiText = GetCategoryEmoji(category.Name);
                var emojiBlock = new TextBlock
                {
                    Text = emojiText,
                    FontSize = 80,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                stack.Children.Add(emojiBlock);

                // Изображение категории (если есть)
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

                        // Заменяем эмодзи на изображение
                        stack.Children.Remove(emojiBlock);
                        stack.Children.Insert(0, image);
                    }
                    catch
                    {
                        // Оставляем эмодзи, если не удалось загрузить изображение
                    }
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
                var productsWindow = new ProductsWindow(category, _viewModel);
                productsWindow.Owner = this;
                productsWindow.ShowDialog();
                UpdateCartInfo();
            }
        }
    }
}