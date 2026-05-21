using BU_Love.Models;
using BU_Love.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BU_Love.ViewModels
{
    class ProductsViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private ObservableCollection<Product> _products;
        private bool _isLoading;
        private string _categoryName;

        public ProductsViewModel(Category category, MainViewModel mainViewModel)
        {
            _apiService = new ApiService("http://localhost:5000");
            MainViewModel = mainViewModel;
            CategoryName = category.Name;
            CategoryId = category.Id;
            AddToCartCommand = new RelayCommand<Product>(AddToCart);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }

        public int CategoryId { get; set; }
        public MainViewModel MainViewModel { get; }
        public ICommand AddToCartCommand { get; }

        public async Task LoadProductsAsync()
        {
            try
            {
                IsLoading = true;
                var products = await _apiService.GetProductsAsync(CategoryId);
                Products = new ObservableCollection<Product>(products);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddToCart(Product product)
        {
            MainViewModel.AddToCart(product);
            MessageBox.Show($"{product.Name} добавлен в корзину!", "Добавлено",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
