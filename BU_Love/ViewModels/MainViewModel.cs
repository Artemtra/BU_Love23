using BU_Love.Services;
using BU_Love.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BU_Love.Models
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<CartItem> _cartItems;
        private bool _isLoading;

        public MainViewModel()
        {
            _apiService = new ApiService("http://localhost:5000");
            _cartItems = new ObservableCollection<CartItem>();
            _cartItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TotalAmount));

            OpenProductsCommand = new RelayCommand<Category>(async (cat) => await OpenProducts(cat));
            OpenCartCommand = new RelayCommand(OpenCart);
            OpenAdminCommand = new RelayCommand(OpenAdmin);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<CartItem> CartItems
        {
            get => _cartItems;
            set
            {
                if (_cartItems != null)
                    _cartItems.CollectionChanged -= OnCartChanged;

                SetProperty(ref _cartItems, value);

                if (_cartItems != null)
                    _cartItems.CollectionChanged += OnCartChanged;
            }
        }

        public decimal TotalAmount => CartItems?.Sum(i => i.TotalPrice) ?? 0;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand OpenProductsCommand { get; }
        public ICommand OpenCartCommand { get; }
        public ICommand OpenAdminCommand { get; }

        private void OnCartChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(TotalAmount));
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                IsLoading = true;
                var categories = await _apiService.GetCategoriesAsync();
                Categories = new ObservableCollection<Category>(categories);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OpenProducts(Category category)
        {
            var productsWindow = new ProductsWindow(category, this);
            productsWindow.Owner = Application.Current.MainWindow;
            productsWindow.ShowDialog();
        }

        private void OpenCart()
        {
            var cartWindow = new CartWindow(this);
            cartWindow.Owner = Application.Current.MainWindow;
            cartWindow.ShowDialog();
        }

        private void OpenAdmin()
        {
            var loginWindow = new AdminLoginWindow(_apiService);
            loginWindow.Owner = Application.Current.MainWindow;
            loginWindow.ShowDialog();
        }

        public void AddToCart(Product product)
        {
            var existingItem = CartItems.FirstOrDefault(i => i.Product.Id == product.Id);
            if (existingItem != null)
                existingItem.Quantity++;
            else
                CartItems.Add(new CartItem { Product = product, Quantity = 1 });
        }

        public void RemoveFromCart(CartItem item)
        {
            CartItems.Remove(item);
        }

        public void ClearCart()
        {
            CartItems.Clear();
        }

        public async Task<int> PlaceOrderAsync(string name, string phone, string address)
        {
            return await _apiService.CreateOrderAsync(name, phone, address, CartItems.ToList());
        }
    }
}