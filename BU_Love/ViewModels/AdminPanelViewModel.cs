using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BU_Love.Models;
using BU_Love.Services;
using Microsoft.Win32;

namespace BU_Love.ViewModels
{
    public class AdminPanelViewModel : BaseViewModel
    {
        private readonly ApiService _apiService;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Order> _orders;
        private Product _selectedProduct;
        private Category _selectedCategory;
        private bool _isEditingProduct;
        private bool _isEditingCategory;
        private string _editTitle;

        public AdminPanelViewModel(ApiService apiService)
        {
            _apiService = apiService;

            LoadDataCommand = new RelayCommand(async () => await LoadAllData());
            AddProductCommand = new RelayCommand(StartAddProduct);
            EditProductCommand = new RelayCommand<Product>(StartEditProduct);
            DeleteProductCommand = new RelayCommand<Product>(async (p) => await DeleteProduct(p));
            SaveProductCommand = new RelayCommand(async () => await SaveProduct());
            SaveCategoryCommand = new RelayCommand(async () => await SaveCategory());
            AddCategoryCommand = new RelayCommand(StartAddCategory);
            EditCategoryCommand = new RelayCommand<Category>(StartEditCategory);
            DeleteCategoryCommand = new RelayCommand<Category>(async (c) => await DeleteCategory(c));

            UploadImageCommand = new RelayCommand(async () => await UploadImage());
            CancelCommand = new RelayCommand(CancelEdit);
        }

        // Свойства
        public ObservableCollection<Product> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set { _orders = value; OnPropertyChanged(); }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); }
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); }
        }

        public bool IsEditingProduct
        {
            get => _isEditingProduct;
            set { _isEditingProduct = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEditingVisible)); }
        }

        public bool IsEditingCategory
        {
            get => _isEditingCategory;
            set
            {
                _isEditingCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCategoryEditVisible));
                OnPropertyChanged(nameof(IsEditingVisible));
            }
        }
        public Visibility IsCategoryEditVisible => IsEditingCategory ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsEditingVisible => IsEditingProduct || IsEditingCategory ? Visibility.Visible : Visibility.Collapsed;
        public string EditTitle
        {
            get => _editTitle;
            set { _editTitle = value; OnPropertyChanged(); }
        }


        // Команды
        public ICommand LoadDataCommand { get; }
        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand SaveProductCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand SaveCategoryCommand { get; }
        public ICommand UploadImageCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task LoadAllData()
        {
            try
            {
                var products = await _apiService.GetProductsAsync();
                Products = new ObservableCollection<Product>(products);

                var categories = await _apiService.GetCategoriesAsync();
                Categories = new ObservableCollection<Category>(categories);

                var orders = await _apiService.GetOrdersAsync();
                Orders = new ObservableCollection<Order>(orders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        // === ПРОДУКТЫ ===
        private void StartAddProduct()
        {
            SelectedProduct = new Product
            {
                Name = "",
                Description = "",
                Price = 0,
                CategoryId = 1, 
                StockQuantity = 1,
                Condition = "Good",
                ImageUrl = ""
            };
            EditTitle = "➕ ДОБАВЛЕНИЕ ТОВАРА";
            IsEditingProduct = true;
            IsEditingCategory = false;
        }

private void StartEditProduct(Product product)
{
    if (product == null) 
    {
        MessageBox.Show("Товар не выбран!");
        return;
    }
    
    // Создаем копию товара для редактирования
    SelectedProduct = new Product
    {
        Id = product.Id,
        Name = product.Name ?? "",
        Description = product.Description ?? "",
        Price = product.Price,
        CategoryId = product.CategoryId,
        StockQuantity = product.StockQuantity,
        Condition = product.Condition ?? "Good",
        ImageUrl = product.ImageUrl ?? ""
    };
    
    EditTitle = "✏️ РЕДАКТИРОВАНИЕ ТОВАРА";
    IsEditingProduct = true;
    IsEditingCategory = false;
    
    // Принудительно обновляем UI
    OnPropertyChanged(nameof(SelectedProduct));
}

        private async Task DeleteProduct(Product product)
        {
            if (MessageBox.Show($"Удалить {product.Name}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                await _apiService.DeleteProductAsync(product.Id);
                Products.Remove(product);
            }
        }

        private async Task SaveProduct()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Товар не выбран!");
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedProduct.Name))
            {
                MessageBox.Show("Введите название товара!");
                return;
            }

            try
            {
                var productToSave = new Product
                {
                    Id = SelectedProduct.Id,
                    Name = SelectedProduct.Name,
                    Description = SelectedProduct.Description,
                    Price = SelectedProduct.Price,
                    CategoryId = SelectedProduct.CategoryId,
                    StockQuantity = SelectedProduct.StockQuantity,
                    Condition = SelectedProduct.Condition,
                    ImageUrl = SelectedProduct.ImageUrl
                };

                if (productToSave.Id == 0)
                {
                    var newProduct = await _apiService.CreateProductAsync(productToSave);
                    Products.Add(newProduct);
                    MessageBox.Show("Товар добавлен!");
                }
                else
                {
                    await _apiService.UpdateProductAsync(productToSave.Id, productToSave);
                    MessageBox.Show("Товар обновлен!");
                }

                IsEditingProduct = false;
                await LoadAllData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }


        // === КАТЕГОРИИ ===
        private void StartAddCategory()
        {
            SelectedCategory = new Category();
            EditTitle = "➕ ДОБАВЛЕНИЕ КАТЕГОРИИ";
            IsEditingCategory = true;
            IsEditingProduct = false;
        }

        private void StartEditCategory(Category category)
        {
            SelectedCategory = new Category
            {
                Id = category.Id,
                Name = category.Name,
                ImageUrl = category.ImageUrl
            };
            EditTitle = "✏️ РЕДАКТИРОВАНИЕ КАТЕГОРИИ";
            IsEditingCategory = true;
            IsEditingProduct = false;
        }

        private async Task DeleteCategory(Category category)
        {
            if (MessageBox.Show($"Удалить категорию {category.Name}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                await _apiService.DeleteCategoryAsync(category.Id);
                Categories.Remove(category);
            }
        }
        private async Task SaveCategory()
        {
            if (string.IsNullOrWhiteSpace(SelectedCategory.Name))
            {
                MessageBox.Show("Введите название категории!");
                return;
            }

            try
            {
                // Если нет картинки, ставим заглушку
                if (string.IsNullOrEmpty(SelectedCategory.ImageUrl))
                {
                    SelectedCategory.ImageUrl = "/uploads/default.png";
                }

                if (SelectedCategory.Id == 0)
                {
                    var newCategory = await _apiService.CreateCategoryAsync(SelectedCategory);
                    Categories.Add(newCategory);
                    MessageBox.Show("Категория добавлена!");
                }
                else
                {
                    await _apiService.UpdateCategoryAsync(SelectedCategory.Id, SelectedCategory);
                    MessageBox.Show("Категория обновлена!");
                }

                IsEditingCategory = false;
                await LoadAllData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }
      


        // === ЗАГРУЗКА ФОТО ===
        private async Task UploadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif|Все файлы|*.*",
                Title = "Выберите изображение"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var imageUrl = await _apiService.UploadImageAsync(dialog.FileName);

                    if (IsEditingProduct)
                        SelectedProduct.ImageUrl = imageUrl;
                    else if (IsEditingCategory)
                        SelectedCategory.ImageUrl = imageUrl;

                    MessageBox.Show("Изображение загружено!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}");
                }
            }
        }

        private void CancelEdit()
        {
            IsEditingProduct = false;
            IsEditingCategory = false;
        }
    }
}