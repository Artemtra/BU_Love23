using BU_Love.Models;
using BU_Love.Services;
using BU_Love.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BU_Love.Views
{
    /// <summary>
    /// Логика взаимодействия для ProductsWindow.xaml
    /// </summary>
    public partial class ProductsWindow : Window
    {
        private readonly ApiService _api;
        private readonly MainViewModel _mainVm;
        private Category _category;

        public ProductsWindow(Category category, MainViewModel mainViewModel)
        {
            InitializeComponent();
            _api = new ApiService("http://localhost:5000");
            _mainVm = mainViewModel;
            _category = category;
            CategoryTitle.Text = category.Name;
            Loaded += async (s, e) => await LoadProducts();
        }

        private async Task LoadProducts()
        {
            try
            {
                var products = await _api.GetProductsAsync(_category.Id);

                foreach (var product in products)
                {
                    // Создаем карточку товара
                    var border = new Border
                    {
                        Width = 300,
                        Margin = new Thickness(15),
                        Padding = new Thickness(20),
                        Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0x2d, 0x2d, 0x2d)),
                        CornerRadius = new CornerRadius(15),
                        BorderBrush = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0x3d, 0x3d, 0x3d)),
                        BorderThickness = new Thickness(2)
                    };

                    var stack = new StackPanel();

                    // Картинка товара
                    var image = new Image
                    {
                        Width = 200,
                        Height = 200,
                        Stretch = System.Windows.Media.Stretch.Uniform,
                        Margin = new Thickness(0, 0, 0, 15)
                    };

                    // Загружаем картинку из API
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

                    // Название
                    stack.Children.Add(new TextBlock
                    {
                        Text = product.Name,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 5)
                    });

                    // Описание
                    stack.Children.Add(new TextBlock
                    {
                        Text = product.Description,
                        FontSize = 14,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0xaa, 0xaa, 0xaa)),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 10)
                    });

                    // Цена
                    stack.Children.Add(new TextBlock
                    {
                        Text = $"{product.Price:C}",
                        FontSize = 28,
                        FontWeight = FontWeights.Bold,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50)),
                        Margin = new Thickness(0, 0, 0, 15),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });

                    // Кнопка "В корзину"
                    var button = new Button
                    {
                        Content = "🛒 В КОРЗИНУ",
                        Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50)),
                        Foreground = new System.Windows.Media.SolidColorBrush(Colors.White),
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
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}");
            }
        }

        private void AddToCart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                _mainVm.AddToCart(product);
                MessageBox.Show($"{product.Name} добавлен в корзину!", "Успех");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            var cartWindow = new CartWindow(_mainVm);
            cartWindow.Owner = this;
            cartWindow.ShowDialog();
        }
    }
}
