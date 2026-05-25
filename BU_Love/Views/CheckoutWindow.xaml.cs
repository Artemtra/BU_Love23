using BU_Love.Models;
using BU_Love.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace BU_Love.Views
{
    public partial class CheckoutWindow : Window
    {
        private readonly MainViewModel _mainVm;
        private readonly ApiService _api;
        private bool _isLoggedIn;
        private decimal _totalAmount;

        public CheckoutWindow(MainViewModel mainViewModel, ApiService api = null)
        {
            InitializeComponent();
            _mainVm = mainViewModel;
            _api = api;
            Loaded += (s, e) => LoadCheckout();
        }

        private void LoadCheckout()
        {
            _totalAmount = _mainVm.CartItems.Sum(i => i.TotalPrice);
            var count = _mainVm.CartItems.Sum(i => i.Quantity);

            TotalAmountText.Text = $"{_totalAmount:C}";
            TotalCountText.Text = $"Товаров: {count} шт.";

            _isLoggedIn = _api != null && _api.IsLoggedIn;

            if (_isLoggedIn)
            {
                UserDataPanel.Visibility = Visibility.Collapsed;
                LoggedInInfoPanel.Visibility = Visibility.Visible;
                BonusPanel.Visibility = Visibility.Visible;

                var user = _api.CurrentUser;
                if (user != null)
                {
                    LoggedInNameText.Text = $"👤 {user.Username}";
                    LoggedInPhoneText.Text = $"📞 {user.Phone}";
                    LoggedInAddressText.Text = $"📍 {user.Address}";
                    BonusInfoText.Text = $"Доступно: {user.BonusPointsDisplay}";
                }
                UpdateBonusInfo();
            }
            else
            {
                UserDataPanel.Visibility = Visibility.Visible;
                LoggedInInfoPanel.Visibility = Visibility.Collapsed;
                BonusPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateBonusInfo()
        {
            if (UseBonusCheckBox.IsChecked == true)
            {
                var bonusToUse = Math.Min(_api.CurrentUser.BonusPoints, _totalAmount);
                TotalAmountText.Text = $"{_totalAmount - bonusToUse:C}";
                BonusAfterPurchaseText.Text = $"Будет списано {bonusToUse:N0} бонусов";
            }
            else
            {
                TotalAmountText.Text = $"{_totalAmount:C}";
                var bonusToEarn = _totalAmount * 0.01m;
                BonusAfterPurchaseText.Text = $"Будет начислено {bonusToEarn:N0} бонусов (1%)";
            }
        }

        private void UseBonusCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateBonusInfo();
        }

        private async void ConfirmOrder_Click(object sender, RoutedEventArgs e)
        {
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

                var api = _api ?? new ApiService("http://localhost:5000");
                var items = _mainVm.CartItems.ToList();
                var useBonus = _isLoggedIn && UseBonusCheckBox.IsChecked == true;
                var bonusToUse = useBonus ? Math.Min(api.CurrentUser?.BonusPoints ?? 0, _totalAmount) : 0;

                var orderId = await api.CreateOrderAsync(customerName, phone, address, items, useBonus, bonusToUse);

                _mainVm.CartItems.Clear();

                var finalAmount = _totalAmount - bonusToUse;
                var bonusEarned = (!useBonus && _isLoggedIn) ? finalAmount * 0.01m : 0;

                string message = $"✅ Заказ №{orderId} оформлен!\n\nИтого: {finalAmount:C}";
                if (useBonus) message += $"\nСписано бонусов: {bonusToUse:N0}";
                if (bonusEarned > 0) message += $"\nНачислено бонусов: {bonusEarned:N0}";
                message += "\n\nСпасибо за покупку! 🎉";

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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}