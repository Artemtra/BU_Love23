using System.Windows;
using System.Windows.Controls;
using BU_Love.Models;
using BU_Love.Services;
using BU_Love.ViewModels;

namespace BU_Love.Views
{
    public partial class AdminPanelWindow : Window
    {
        private readonly ApiService _api;
        private List<Product> _products;
        private List<Category> _categories;
        private int _editingProductId = 0;
        private int _editingCategoryId = 0;
        private string _productImageUrl = "";
        private string _categoryImageUrl = "";
        public AdminPanelWindow(ApiService api)
        {
            InitializeComponent();
            _api = api;
            Loaded += async (s, e) => await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                _products = await _api.GetProductsAsync();
                _categories = await _api.GetCategoriesAsync();

                ProductsList.ItemsSource = _products;
                CategoriesList.ItemsSource = _categories;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        // ====== ТОВАРЫ ======
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            _editingProductId = 0;
            ProductFormTitle.Text = "➕ ДОБАВЛЕНИЕ ТОВАРА";
            ProductName.Text = "";
            ProductDesc.Text = "";
            ProductPrice.Text = "";
            ProductCategory.Text = "1";
            ProductStock.Text = "1";
            ProductCondition.Text = "Good";
            ProductForm.Visibility = Visibility.Visible;
            CategoryForm.Visibility = Visibility.Collapsed;
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                _editingProductId = product.Id;
                ProductFormTitle.Text = "✏️ РЕДАКТИРОВАНИЕ ТОВАРА";
                ProductName.Text = product.Name ?? "";
                ProductDesc.Text = product.Description ?? "";
                ProductPrice.Text = product.Price.ToString();
                ProductCategory.Text = product.CategoryId.ToString();
                ProductStock.Text = product.StockQuantity.ToString();
                ProductCondition.Text = product.Condition ?? "Good";
                ProductForm.Visibility = Visibility.Visible;
                CategoryForm.Visibility = Visibility.Collapsed;
            }
        }
        private async void UploadProductImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif|Все файлы|*.*",
                Title = "Выберите изображение для товара"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {

                    var imageUrl = await _api.UploadImageAsync(dialog.FileName);
                    _productImageUrl = imageUrl;
                    ProductImageUrl.Text = imageUrl;
                    ProductImagePath.Text = "✓ Фото загружено: " + imageUrl;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
                }
            }
        }

        // Загрузка фото для категории
        private async void UploadCategoryImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif|Все файлы|*.*",
                Title = "Выберите изображение для категории"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imageUrl = await _api.UploadImageAsync(dialog.FileName);
                    _categoryImageUrl = imageUrl;
                    CategoryImageUrl.Text = imageUrl;
                    CategoryImagePath.Text = "✓ Фото загружено: " + imageUrl;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
                }
            }
        }
        private async void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var product = new Product
                {
                    Id = _editingProductId,
                    Name = ProductName.Text,
                    Description = ProductDesc.Text,
                    Price = decimal.TryParse(ProductPrice.Text, out var p) ? p : 0,
                    CategoryId = int.TryParse(ProductCategory.Text, out var c) ? c : 1,
                    StockQuantity = int.TryParse(ProductStock.Text, out var s) ? s : 1,
                    Condition = ProductCondition.Text,
                    ImageUrl = _productImageUrl // Сохраняем URL загруженного фото
                };

                if (_editingProductId == 0)
                    await _api.CreateProductAsync(product);
                else
                    await _api.UpdateProductAsync(_editingProductId, product);

                _productImageUrl = "";
                ProductImagePath.Text = "";
                ProductForm.Visibility = Visibility.Collapsed;
                await LoadData();
                MessageBox.Show("Сохранено!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Product product)
            {
                if (MessageBox.Show($"Удалить {product.Name}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await _api.DeleteProductAsync(product.Id);
                    await LoadData();
                }
            }
        }

        // ====== КАТЕГОРИИ ======
        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            _editingCategoryId = 0;
            CategoryFormTitle.Text = "➕ ДОБАВЛЕНИЕ КАТЕГОРИИ";
            CategoryName.Text = "";
            CategoryForm.Visibility = Visibility.Visible;
            ProductForm.Visibility = Visibility.Collapsed;
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                _editingCategoryId = category.Id;
                CategoryFormTitle.Text = "✏️ РЕДАКТИРОВАНИЕ КАТЕГОРИИ";
                CategoryName.Text = category.Name ?? "";
                CategoryForm.Visibility = Visibility.Visible;
                ProductForm.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveCategory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var category = new Category
                {
                    Id = _editingCategoryId,
                    Name = CategoryName.Text,
                    ImageUrl = _categoryImageUrl // Сохраняем URL загруженного фото
                };

                if (_editingCategoryId == 0)
                    await _api.CreateCategoryAsync(category);
                else
                    await _api.UpdateCategoryAsync(_editingCategoryId, category);

                _categoryImageUrl = "";
                CategoryImagePath.Text = "";
                CategoryForm.Visibility = Visibility.Collapsed;
                await LoadData();
                MessageBox.Show("Сохранено!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Category category)
            {
                if (MessageBox.Show($"Удалить категорию {category.Name}?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await _api.DeleteCategoryAsync(category.Id);
                    await LoadData();
                }
            }
        }

        private void CancelForm_Click(object sender, RoutedEventArgs e)
        {
            ProductForm.Visibility = Visibility.Collapsed;
            CategoryForm.Visibility = Visibility.Collapsed;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}