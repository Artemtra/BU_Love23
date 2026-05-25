using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BU_Love.Services;

namespace BU_Love.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService _api;
        private bool _isLoginMode = true;
        private bool _loginPasswordVisible = false;
        private bool _regPasswordVisible = false;

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

        private void ToggleLoginPassword_Click(object sender, RoutedEventArgs e)
        {
            _loginPasswordVisible = !_loginPasswordVisible;

            if (_loginPasswordVisible)
            {
                LoginPasswordVisible.Text = LoginPassword.Password;
                LoginPassword.Visibility = Visibility.Collapsed;
                LoginPasswordVisible.Visibility = Visibility.Visible;
                LoginEyeBtn.Content = "👁‍🗨";
            }
            else
            {
                LoginPassword.Password = LoginPasswordVisible.Text;
                LoginPassword.Visibility = Visibility.Visible;
                LoginPasswordVisible.Visibility = Visibility.Collapsed;
                LoginEyeBtn.Content = "👁";
            }
        }

        private void ToggleRegPassword_Click(object sender, RoutedEventArgs e)
        {
            _regPasswordVisible = !_regPasswordVisible;

            if (_regPasswordVisible)
            {
                RegPasswordVisible.Text = RegPassword.Password;
                RegPassword.Visibility = Visibility.Collapsed;
                RegPasswordVisible.Visibility = Visibility.Visible;
                RegEyeBtn.Content = "👁‍🗨";
            }
            else
            {
                RegPassword.Password = RegPasswordVisible.Text;
                RegPassword.Visibility = Visibility.Visible;
                RegPasswordVisible.Visibility = Visibility.Collapsed;
                RegEyeBtn.Content = "👁";
            }
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

                    string password = _loginPasswordVisible ? LoginPasswordVisible.Text : LoginPassword.Password;
                    var user = await _api.LoginAsync(LoginUsername.Text.Trim(), password);

                    MessageBox.Show($"Добро пожаловать, {user.Username}!\nБонусов: {user.BonusPointsDisplay}", "Успех");

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

                    string phone = RegPhone.Text.Trim();
                    if (!phone.StartsWith("+"))
                    {
                        MessageBox.Show("Номер телефона должен начинаться с +", "Ошибка");
                        return;
                    }
                    if (phone.Length != 12)
                    {
                        MessageBox.Show("Номер телефона должен содержать + и 11 цифр\nНапример: +79001234567", "Ошибка");
                        return;
                    }
                    if (!phone.Substring(1).All(char.IsDigit))
                    {
                        MessageBox.Show("Номер телефона должен содержать только цифры после +", "Ошибка");
                        return;
                    }

                    SubmitBtn.Content = "РЕГИСТРАЦИЯ...";

                    string password = _regPasswordVisible ? RegPasswordVisible.Text : RegPassword.Password;
                    var user = await _api.RegisterAsync(
                        RegUsername.Text.Trim(), password, phone, RegAddress.Text.Trim());

                    MessageBox.Show($"Регистрация успешна!\nДобро пожаловать, {user.Username}!\nНачислено 100 бонусов!", "Успех");

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