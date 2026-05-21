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

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            Loaded += async (s, e) =>
            {
                await _viewModel.LoadCategoriesAsync();
                ShowCategories();
            };
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
            // Находим WrapPanel в XAML (добавь x:Name="CategoriesPanel" в WrapPanel)
            var categoriesPanel = this.FindName("CategoriesPanel") as WrapPanel;
            if (categoriesPanel == null) return;

            categoriesPanel.Children.Clear();

            if (_viewModel.Categories == null) return;

            foreach (var category in _viewModel.Categories)
            {
                // Кнопка категории
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
                button.Click += CategoryButton_Click;

                var stack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Картинка категории
                var image = new Image
                {
                    Width = 120,
                    Height = 120,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    try
                    {
                        var imageUrl = "http://localhost:5000" + category.ImageUrl;
                        image.Source = new BitmapImage(new Uri(imageUrl, UriKind.Absolute));
                    }
                    catch { }
                }

                stack.Children.Add(image);

                // Название категории
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

        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Category category)
            {
                var productsWindow = new ProductsWindow(category, _viewModel);
                productsWindow.Owner = this;
                productsWindow.ShowDialog();
            }
        }
    }
}