using BU_Love.Models;
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
    /// Логика взаимодействия для CheckoutWindow.xaml
    /// </summary>
    public partial class CheckoutWindow : Window
    {
        private readonly MainViewModel _mainViewModel;

        public CheckoutWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
            DataContext = _mainViewModel;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Введите имя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(PhoneBox.Text))
            {
                MessageBox.Show("Введите телефон!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(AddressBox.Text))
            {
                MessageBox.Show("Введите адрес!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!_mainViewModel.CartItems.Any())
            {
                MessageBox.Show("Корзина пуста!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем количество товаров
            foreach (var item in _mainViewModel.CartItems)
            {
                if (item.Quantity > item.Product.StockQuantity)
                {
                    MessageBox.Show(
                        $"Товара \"{item.Product.Name}\" недостаточно на складе!\n" +
                        $"В наличии: {item.Product.StockQuantity}, вы запросили: {item.Quantity}\n" +
                        $"Пожалуйста, уменьшите количество.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                SubmitButton.IsEnabled = false;
                SubmitButton.Content = "ОФОРМЛЕНИЕ...";

                var orderId = await _mainViewModel.PlaceOrderAsync(
                    NameBox.Text,
                    PhoneBox.Text,
                    AddressBox.Text);

                MessageBox.Show(
                    $"Заказ №{orderId} успешно оформлен!\nСпасибо за покупку!",
                    "Успех!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _mainViewModel.ClearCart();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оформления заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SubmitButton.IsEnabled = true;
                SubmitButton.Content = "ПОДТВЕРДИТЬ ЗАКАЗ";
            }
        }
        
    }
}
