using BU_Love.Services;
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
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly ApiService _api;
        private bool _isLoginMode = true;

        public LoginWindow(ApiService api)
        {
            InitializeComponent();
            _api = api;
        }

        private void LoginTab_Click(object sender, RoutedEventArgs e)
        {
            _isLoginMode = true;
            LoginTabBtn.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
            LoginTabBtn.Foreground = Brushes.White;
            RegisterTabBtn.Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d));
            RegisterTabBtn.Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa));

            LoginPanel.Visibility = Visibility.Visible;
            RegisterPanel.Visibility = Visibility.Collapsed;

            TitleText.Text = "Вход";
            SubmitBtn.Content = "ВОЙТИ";
            BonusInfo.Text = "";
        }

        private void RegisterTab_Click(object sender, RoutedEventArgs e)
        {
            _isLoginMode = false;
            RegisterTabBtn.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
            RegisterTabBtn.Foreground = Brushes.White;
            LoginTabBtn.Background = new SolidColorBrush(Color.FromRgb(0x2d, 0x2d, 0x2d));
            LoginTabBtn.Foreground = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa));

            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;

            TitleText.Text = "Регистрация";
            SubmitBtn.Content = "ЗАРЕГИСТРИРОВАТЬСЯ";
            BonusInfo.Text = "🎁 100 приветственных бонусов!";
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsEnabled = false;

                if (_isLoginMode)
                {
                    if (string.IsNullOrWhiteSpace(LoginUsername.Text) ||
                        string.IsNullOrWhiteSpace(LoginPassword.Password))
                    {
                        MessageBox.Show("Заполните все поля");
                        return;
                    }

                    SubmitBtn.Content = "ВХОД...";
                    var user = await _api.LoginAsync(LoginUsername.Text.Trim(), LoginPassword.Password);

                    MessageBox.Show($"Добро пожаловать, {user.Username}!\nБонусов: {user.BonusPointsDisplay}",
                        "Успех");

                    DialogResult = true;
                    Close();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(RegUsername.Text) ||
                        string.IsNullOrWhiteSpace(RegPassword.Password) ||
                        string.IsNullOrWhiteSpace(RegPhone.Text) ||
                        string.IsNullOrWhiteSpace(RegAddress.Text))
                    {
                        MessageBox.Show("Заполните все поля");
                        return;
                    }

                    SubmitBtn.Content = "РЕГИСТРАЦИЯ...";
                    var user = await _api.RegisterAsync(
                        RegUsername.Text.Trim(),
                        RegPassword.Password,
                        RegPhone.Text.Trim(),
                        RegAddress.Text.Trim());

                    MessageBox.Show($"Регистрация успешна!\nДобро пожаловать, {user.Username}!\nНачислено 100 бонусов!",
                        "Успех");

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
            finally
            {
                IsEnabled = true;
                SubmitBtn.Content = _isLoginMode ? "ВОЙТИ" : "ЗАРЕГИСТРИРОВАТЬСЯ";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}