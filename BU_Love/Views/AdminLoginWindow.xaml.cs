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
using BU_Love.Services;

namespace BU_Love.Views
{
    public partial class AdminLoginWindow : Window
    {
        private readonly ApiService _apiService;

        public AdminLoginWindow(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoginButton.IsEnabled = false;
                LoginButton.Content = "ВХОД...";

                var user = await _apiService.LoginAsync(
                    LoginBox.Text,
                    PasswordBox.Password
                );

                if (user.Role == "Admin")
                {
                    var adminPanel = new AdminPanelWindow(_apiService);
                    adminPanel.Owner = this;
                    Hide();
                    adminPanel.ShowDialog();
                    Close();
                }
                else
                {
                    MessageBox.Show("Недостаточно прав! Требуется роль Admin.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginButton.Content = "ВОЙТИ";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
