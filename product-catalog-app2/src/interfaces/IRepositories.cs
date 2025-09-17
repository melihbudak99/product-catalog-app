using product_catalog_app.src.models;

namespace product_catalog_app.src.interfaces
{
    /// <summary>
    /// Product Repository Interface - Both sync and async for compatibility
    /// </summary>
    public interface IProductRepository
    {
        // Basic operations - Both sync and async for compatibility
        List<Product> GetAllProducts();
        Product? GetProductById(int id);
        void AddProduct(Product product);
        void UpdateProduct(Product product);
        void DeleteProduct(int id);

        // Async operations
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<List<Product>> GetProductsByIdsAsync(List<int> ids);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);

        // Search and pagination - Both sync and async for compatibility
        List<Product> SearchProducts(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50);
        int GetProductCount(string searchTerm = "", string category = "", string brand = "");
        Task<List<Product>> SearchProductsAsync(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50);
        Task<int> GetProductCountAsync(string searchTerm = "", string category = "", string brand = "");

        // Advanced search with comprehensive filtering
        Task<List<Product>> SearchProductsAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null, string sortBy = "name", string sortDirection = "asc",
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "",
            int page = 1, int pageSize = 50);

        Task<int> GetProductCountAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null,
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "");

        // Bulk operations
        Task BulkUpdateProductsAsync(List<Product> products);
        Task BulkDeleteProductsAsync(List<int> productIds);
        Task BulkArchiveProductsAsync(List<int> productIds);
        Task BulkUnarchiveProductsAsync(List<int> productIds);

        // Archive operations
        Task<List<Product>> GetArchivedProductsAsync(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50);
        Task<int> GetArchivedProductCountAsync(string searchTerm = "", string category = "", string brand = "");

        // Utility methods for dropdowns and filters - Both sync and async for compatibility
        List<string> GetDistinctCategories();
        List<string> GetDistinctBrands();
        List<string> GetDistinctMaterials();
        List<string> GetDistinctColors();
        List<string> GetDistinctSpecialFeatures();
        
        Task<List<string>> GetDistinctCategoriesAsync();
        Task<List<string>> GetDistinctBrandsAsync();
        Task<List<string>> GetDistinctMaterialsAsync();
        Task<List<string>> GetDistinctColorsAsync();
        Task<List<string>> GetDistinctSpecialFeaturesAsync();

        // XML operations - Both sync and async for compatibility  
        List<ProductXml> GetAllXmlProducts();
        List<ProductXml> GetActiveXmlProducts();
        List<ProductXml> GetArchivedXmlProducts();
        void ImportXmlProducts(List<ProductXml> xmlProducts);
        
        Task<List<ProductXml>> GetAllXmlProductsAsync();
        Task<List<ProductXml>> GetActiveXmlProductsAsync();
        Task<List<ProductXml>> GetArchivedXmlProductsAsync();
        Task ImportXmlProductsAsync(List<ProductXml> xmlProducts);

        // Health and statistics
        Task<Dictionary<string, object>> GetHealthStatsAsync();
        Task<bool> TestConnectionAsync();

        // Benzersizlik kontrol metotları
        Task<bool> IsSkuUniqueAsync(string sku, int? excludeProductId = null);
        Task<bool> IsEanCodeUniqueAsync(string eanCode, int? excludeProductId = null);
    }

    /// <summary>
    /// Category Repository Interface - Async-only for production performance
    /// </summary>
    public interface ICategoryRepository
    {
        // Async operations only
        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category?> GetCategoryByNameAsync(string name);
        Task<Category> AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);
        Task<int> GetProductCountByCategoryAsync(int categoryId);
        Task<bool> CategoryExistsAsync(string name);
        Task<int> GetCategoryCountAsync();
    }

    /// <summary>
    /// Product Service Interface - Both sync and async for compatibility
    /// </summary>
    public interface IProductService
    {
        // Basic CRUD operations - Both sync and async
        List<Product> GetAllProducts();
        Product? GetProductById(int id);
        void AddProduct(Product product);
        void UpdateProduct(Product product);
        void DeleteProduct(int id);
        
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);

        // Search operations - Both sync and async
        List<Product> SearchProducts(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50);
        int GetProductCount(string searchTerm = "", string category = "", string brand = "");
        
        Task<List<Product>> SearchProductsAsync(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50);
        Task<int> GetProductCountAsync(string searchTerm = "", string category = "", string brand = "");

        // Utility methods - Both sync and async
        List<string> GetDistinctCategories();
        List<string> GetDistinctBrands();
        List<string> GetDistinctMaterials();
        List<string> GetDistinctColors();
        List<string> GetDistinctSpecialFeatures();
        
        Task<List<string>> GetDistinctCategoriesAsync();
        Task<List<string>> GetDistinctBrandsAsync();
        Task<List<string>> GetDistinctMaterialsAsync();
        Task<List<string>> GetDistinctColorsAsync();
        Task<List<string>> GetDistinctSpecialFeaturesAsync();

        // Advanced search with comprehensive filtering
        Task<List<Product>> SearchProductsAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null, string sortBy = "name", string sortDirection = "asc",
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "",
            int page = 1, int pageSize = 50);

        Task<int> GetProductCountAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null,
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "");

        // Bulk operations
        Task BulkDeleteProductsAsync(List<int> productIds);

        // Cache management
        void ClearCache();
    }

    /// <summary>
    /// Category Service Interface
    /// </summary>
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<string>> GetCategoryNamesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category?> GetCategoryByNameAsync(string name);
        Task<Category> AddCategoryAsync(string name, string? description = null);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(int id);
        Task<bool> CategoryExistsAsync(string name);
        Task<int> GetProductCountByCategoryAsync(int categoryId);
        Task<int> GetActiveCategoryCountAsync();
        
        // Backward compatibility methods
        Task<int?> GetOrCreateCategoryIdAsync(string categoryName);
    }
}
