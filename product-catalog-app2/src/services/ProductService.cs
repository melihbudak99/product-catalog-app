using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using product_catalog_app.src.models;
using product_catalog_app.src.data;
using product_catalog_app.src.common;
using product_catalog_app.src.interfaces;
using System.Text;
using System.Text.Json;

namespace product_catalog_app.src.services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProductService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(Constants.CacheDuration.CATEGORIES);
        private readonly TimeSpan _shortCacheExpiration = TimeSpan.FromMinutes(Constants.CacheDuration.SEARCH_RESULTS);

        // Cache keys - Use constants for consistency
        private const string CATEGORIES_CACHE_KEY = "distinct_categories"; // Different from CategoryService
        private const string BRANDS_CACHE_KEY = Constants.CacheKeys.ALL_BRANDS;
        private const string PRODUCTS_COUNT_CACHE_PREFIX = "products_count_";

        public ProductService(IProductRepository productRepository, IMemoryCache cache, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _cache = cache;
            _logger = logger;
        }

        // Async operations only - optimized for performance
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllProductsAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetProductByIdAsync(id);
        }

        // Sync operations for compatibility
        public List<Product> GetAllProducts()
        {
            return _productRepository.GetAllProducts();
        }

        public Product? GetProductById(int id)
        {
            return _productRepository.GetProductById(id);
        }

        public async Task<List<Product>> GetProductsByIdsAsync(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return new List<Product>();

            return await _productRepository.GetProductsByIdsAsync(ids);
        }

        // Benzersizlik kontrol metotları
        public async Task<bool> IsSkuUniqueAsync(string sku, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(sku)) return true; // Boş SKU benzersiz kabul edilir
            
            return await _productRepository.IsSkuUniqueAsync(sku, excludeProductId);
        }

        public async Task<bool> IsEanCodeUniqueAsync(string eanCode, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(eanCode)) return true; // Boş EAN benzersiz kabul edilir
            
            return await _productRepository.IsEanCodeUniqueAsync(eanCode, excludeProductId);
        }

        public void AddProduct(Product product)
        {
            ValidateProductFields(product);
            _productRepository.AddProduct(product);
            ClearCache();
        }

        public async Task AddProductAsync(Product product)
        {
            ValidateProductFields(product);
            await _productRepository.AddProductAsync(product);
            ClearCache();
        }

        public void AddProducts(List<Product> products)
        {
            foreach (var product in products)
            {
                _productRepository.AddProduct(product);
            }
        }

        // Interface'e uygun async Update metodu
        public async Task UpdateProductAsync(Product product)
        {
            ValidateProductFields(product);
            await _productRepository.UpdateProductAsync(product);
            ClearCache();
        }

        private void ValidateProductFields(Product product)
        {
            // Güvenlik kontrolü: null alanları boş string olarak ayarla
            product.Description ??= string.Empty;
            product.Features ??= string.Empty;
            product.Notes ??= string.Empty;
            product.Name ??= string.Empty;
            product.Brand ??= string.Empty;
            product.Category ??= string.Empty;
            product.SKU ??= string.Empty;
            product.EanCode ??= string.Empty;
            product.Material ??= string.Empty;
            product.Color ??= string.Empty;
            product.ImageUrl ??= string.Empty;
            product.LogoBarcodes ??= string.Empty;

            // Numeric alanlar için varsayılan değer kontrolü
            if (product.Weight < 0) product.Weight = 0;
            if (product.Desi < 0) product.Desi = 0;
            if (product.Width < 0) product.Width = 0;
            if (product.Height < 0) product.Height = 0;
            if (product.Depth < 0) product.Depth = 0;
            if (!product.Length.HasValue || product.Length < 0) product.Length = 0;
            if (product.WarrantyMonths < 0) product.WarrantyMonths = 0;

            // Tüm barcode alanları için null check (alfabetik sırada)
            product.AmazonBarcode ??= string.Empty;
            product.HaceyapiBarcode ??= string.Empty;
            product.HepsiburadaBarcode ??= string.Empty;
            product.HepsiburadaSellerStockCode ??= string.Empty;
            product.HepsiburadaTedarikBarcode ??= string.Empty;
            product.KoctasBarcode ??= string.Empty;
            product.KoctasEanBarcode ??= string.Empty;
            product.KoctasEanIstanbulBarcode ??= string.Empty;
            product.KoctasIstanbulBarcode ??= string.Empty;
            product.N11CatalogId ??= string.Empty;
            product.N11ProductCode ??= string.Empty;
            product.PazaramaBarcode ??= string.Empty;
            product.PttAvmBarcode ??= string.Empty;
            product.PttUrunStokKodu ??= string.Empty;
            product.TrendyolBarcode ??= string.Empty;
            product.SpareBarcode1 ??= string.Empty;
            product.SpareBarcode2 ??= string.Empty;
            product.SpareBarcode3 ??= string.Empty;
            product.SpareBarcode4 ??= string.Empty;

            // Special product features - ensure they are not null
            product.KlozetKanalYapisi ??= string.Empty;
            product.KlozetTipi ??= string.Empty;
            product.KlozetKapakCinsi ??= string.Empty;
            product.KlozetMontajTipi ??= string.Empty;
            product.LawaboSuTasmaDeligi ??= string.Empty;
            product.LawaboArmaturDeligi ??= string.Empty;
            product.LawaboTipi ??= string.Empty;
            product.LawaboOzelligi ??= string.Empty;
            
            // Battery features - ensure they are not null
            product.BataryaCikisUcuUzunlugu ??= string.Empty;
            product.BataryaYuksekligi ??= string.Empty;
        }

        // XML export/import methods using repository
        public List<ProductXml> GetAllXmlProducts()
        {
            return _productRepository.GetAllXmlProducts();
        }

        public List<ProductXml> GetActiveXmlProducts()
        {
            return _productRepository.GetActiveXmlProducts();
        }

        public List<ProductXml> GetArchivedXmlProducts()
        {
            return _productRepository.GetArchivedXmlProducts();
        }

        public void ImportXmlProducts(List<ProductXml> xmlProducts)
        {
            _productRepository.ImportXmlProducts(xmlProducts);
            ClearCache(); // Cache'i temizle çünkü yeni ürünler eklendi
        }

        public void DeleteProduct(int id)
        {
            _productRepository.DeleteProduct(id);
            ClearCache();
        }

        // Optimized search methods
        public async Task<List<Product>> SearchProductsAsync(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50)
        {
            return await _productRepository.SearchProductsAsync(searchTerm, category, brand, page, pageSize);
        }

        public async Task<List<string>> GetDistinctCategoriesAsync()
        {
            try
            {
                return _cache.GetOrCreate(CATEGORIES_CACHE_KEY, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
                    entry.SetPriority(CacheItemPriority.High);
                    entry.SetSize(1); // Set cache entry size for SizeLimit compatibility
                    var task = _productRepository.GetDistinctCategoriesAsync();
                    var categories = task.GetAwaiter().GetResult(); // Safe for cache factory
                    _logger.LogInformation("Categories loaded into cache: {Count} items", categories.Count);
                    return categories;
                }) ?? new List<string>();
            }
            catch (InvalidCastException ex)
            {
                _logger.LogError(ex, "Cache casting error in GetDistinctCategories. Clearing cache and trying again.");
                _cache.Remove(CATEGORIES_CACHE_KEY);
                
                // Direct call without cache
                var categories = await _productRepository.GetDistinctCategoriesAsync();
                _logger.LogInformation("Categories loaded directly (no cache): {Count} items", categories.Count);
                return categories;
            }
        }

        // Generic cache helper to eliminate duplicate cache patterns
        private async Task<List<string>> GetCachedDistinctDataAsync<T>(
            string cacheKey, 
            Func<Task<List<string>>> dataProvider,
            string dataTypeName)
        {
            try
            {
                var cachedData = _cache.Get<List<string>>(cacheKey);
                if (cachedData != null)
                {
                    return cachedData;
                }

                var data = await dataProvider();
                
                _cache.Set(cacheKey, data, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration,
                    Priority = CacheItemPriority.High,
                    Size = 1
                });
                
                _logger.LogInformation("{DataType} loaded into cache: {Count} items", dataTypeName, data.Count);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache error in Get{DataType}. Clearing cache and trying again.", dataTypeName);
                _cache.Remove(cacheKey);
                
                var data = await dataProvider();
                _logger.LogInformation("{DataType} loaded directly (no cache): {Count} items", dataTypeName, data.Count);
                return data;
            }
        }

        public async Task<List<string>> GetDistinctBrandsAsync()
        {
            return await GetCachedDistinctDataAsync<string>(
                BRANDS_CACHE_KEY,
                () => _productRepository.GetDistinctBrandsAsync(),
                "Brands"
            );
        }

        public async Task<List<string>> GetDistinctMaterialsAsync()
        {
            return await GetCachedDistinctDataAsync<string>(
                "distinct_materials",
                () => _productRepository.GetDistinctMaterialsAsync(),
                "Materials"
            );
        }

        public async Task<List<string>> GetDistinctColorsAsync()
        {
            return await GetCachedDistinctDataAsync<string>(
                "distinct_colors",
                () => _productRepository.GetDistinctColorsAsync(),
                "Colors"
            );
        }

        public async Task<List<string>> GetDistinctSpecialFeaturesAsync()
        {
            return await GetCachedDistinctDataAsync<string>(
                "distinct_special_features",
                () => _productRepository.GetDistinctSpecialFeaturesAsync(),
                "Special Features"
            );
        }

        // Sync versions for compatibility
        public List<string> GetDistinctBrands()
        {
            return _productRepository.GetDistinctBrands();
        }

        public List<string> GetDistinctMaterials()
        {
            return _productRepository.GetDistinctMaterials();
        }

        public List<string> GetDistinctColors()
        {
            return _productRepository.GetDistinctColors();
        }

        public List<string> GetDistinctSpecialFeatures()
        {
            return _productRepository.GetDistinctSpecialFeatures();
        }

        // Advanced search methods with all product properties
        public async Task<List<Product>> SearchProductsAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null, string sortBy = "updated", string sortDirection = "desc",
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "",
            int page = 1, int pageSize = 50)
        {
            return await _productRepository.SearchProductsAdvancedAsync(
                searchTerm, category, brand, status, material, color, eanCode,
                minWeight, maxWeight, minDesi, maxDesi, minWarranty, maxWarranty,
                sortBy, sortDirection, hasImage, hasEan, hasBarcode, barcodeType, page, pageSize);
        }

        public async Task<int> GetProductCountAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null,
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "")
        {
            return await _productRepository.GetProductCountAdvancedAsync(
                searchTerm, category, brand, status, material, color, eanCode,
                minWeight, maxWeight, minDesi, maxDesi, minWarranty, maxWarranty,
                hasImage, hasEan, hasBarcode, barcodeType);
        }

        public int GetProductCount(string searchTerm = "", string category = "", string brand = "")
        {
            var cacheKey = $"{PRODUCTS_COUNT_CACHE_PREFIX}{searchTerm}_{category}_{brand}";
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _shortCacheExpiration;
                entry.SetSize(1); // Set cache entry size for SizeLimit compatibility
                return _productRepository.GetProductCount(searchTerm, category, brand);
            });
        }

        public async Task<int> GetProductCountAsync(string searchTerm = "", string category = "", string brand = "")
        {
            var cacheKey = $"{PRODUCTS_COUNT_CACHE_PREFIX}{searchTerm}_{category}_{brand}";
            return await Task.FromResult(_cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _shortCacheExpiration;
                entry.SetSize(1); // Set cache entry size for SizeLimit compatibility
                return _productRepository.GetProductCountAsync(searchTerm, category, brand).Result;
            }));
        }

        private void ClearCache()
        {
            try
            {
                _cache.Remove(CATEGORIES_CACHE_KEY);
                _cache.Remove(BRANDS_CACHE_KEY);
                _cache.Remove("distinct_materials");
                _cache.Remove("distinct_colors");
                _cache.Remove("distinct_special_features");

                // Clear common count cache patterns
                var commonKeys = new[]
                {
                    PRODUCTS_COUNT_CACHE_PREFIX + "__",
                    PRODUCTS_COUNT_CACHE_PREFIX + "___",
                    PRODUCTS_COUNT_CACHE_PREFIX + "____"
                };

                foreach (var key in commonKeys)
                {
                    _cache.Remove(key);
                }

                _logger.LogInformation("Cache cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache, continuing anyway");
            }
        }

        // Public method for interface implementation
        void IProductService.ClearCache()
        {
            ClearCache();
        }

        // Missing DeleteProductAsync implementation
        public async Task DeleteProductAsync(int id)
        {
            await _productRepository.DeleteProductAsync(id);
            ClearCache();
        }

        // Bulk operations for better performance
        public async Task BulkDeleteProductsAsync(List<int> productIds)
        {
            await _productRepository.BulkDeleteProductsAsync(productIds);
            ClearCache();
        }

        // Archive management methods
        public void ArchiveProduct(int productId)
        {
            var product = _productRepository.GetProductById(productId);
            if (product != null)
            {
                product.IsArchived = true;
                product.UpdatedDate = DateTime.UtcNow;
                _productRepository.UpdateProduct(product);
                ClearCache();
            }
        }

        public void UnarchiveProduct(int productId)
        {
            var product = _productRepository.GetProductById(productId);
            if (product != null)
            {
                product.IsArchived = false;
                product.UpdatedDate = DateTime.UtcNow;
                _productRepository.UpdateProduct(product);
                ClearCache();
            }
        }

        public void BulkArchiveProducts(List<int> productIds)
        {
            var products = _productRepository.GetAllProducts().Where(p => productIds.Contains(p.Id)).ToList();
            foreach (var product in products)
            {
                product.IsArchived = true;
                product.UpdatedDate = DateTime.UtcNow;
                _productRepository.UpdateProduct(product);
            }
            ClearCache();
        }

        public void BulkUnarchiveProducts(List<int> productIds)
        {
            var products = _productRepository.GetAllProducts().Where(p => productIds.Contains(p.Id)).ToList();
            foreach (var product in products)
            {
                product.IsArchived = false;
                product.UpdatedDate = DateTime.UtcNow;
                _productRepository.UpdateProduct(product);
            }
            ClearCache();
        }

        // Export methods
        public string ExportToCsv(List<Product> products)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id,Name,SKU,Brand,Category,Weight,Desi,IsArchived");
            
            foreach (var product in products)
            {
                csv.AppendLine($"{product.Id},{product.Name},{product.SKU},{product.Brand},{product.Category},{product.Weight},{product.Desi},{product.IsArchived}");
            }
            
            return csv.ToString();
        }

        public string ExportToJson(List<Product> products)
        {
            return JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
        }

        public void ImportFromJson(string jsonContent)
        {
            try
            {
                var products = JsonSerializer.Deserialize<List<Product>>(jsonContent);
                if (products != null)
                {
                    foreach (var product in products)
                    {
                        product.CreatedDate = DateTime.UtcNow;
                        product.UpdatedDate = DateTime.UtcNow;
                        _productRepository.AddProduct(product);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products from JSON");
                throw;
            }
        }

        // Archive methods
        public List<Product> GetArchivedProducts(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50)
        {
            var allProducts = _productRepository.GetAllProducts().Where(p => p.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                allProducts = allProducts.Where(p => p.Name.Contains(searchTerm) || 
                                                   p.SKU.Contains(searchTerm) || 
                                                   p.Brand.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category))
            {
                allProducts = allProducts.Where(p => p.Category == category);
            }

            if (!string.IsNullOrEmpty(brand))
            {
                allProducts = allProducts.Where(p => p.Brand == brand);
            }

            return allProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        public int GetArchivedProductCount(string searchTerm = "", string category = "", string brand = "")
        {
            var allProducts = _productRepository.GetAllProducts().Where(p => p.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                allProducts = allProducts.Where(p => p.Name.Contains(searchTerm) || 
                                                   p.SKU.Contains(searchTerm) || 
                                                   p.Brand.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(category))
            {
                allProducts = allProducts.Where(p => p.Category == category);
            }

            if (!string.IsNullOrEmpty(brand))
            {
                allProducts = allProducts.Where(p => p.Brand == brand);
            }

            return allProducts.Count();
        }

        public async Task<int> GetArchivedProductCountAsync()
        {
            var products = await _productRepository.GetAllProductsAsync();
            return products.Count(p => p.IsArchived);
        }

        public void ClearSearchCache()
        {
            var cacheKeys = new[]
            {
                CATEGORIES_CACHE_KEY,
                BRANDS_CACHE_KEY,
                "distinct_materials",
                "distinct_colors",
                "distinct_special_features"
            };

            foreach (var key in cacheKeys)
            {
                _cache.Remove(key);
            }
            
            _logger.LogInformation("Search cache cleared");
        }

        public T GetOrSetCache<T>(string cacheKey, Func<T> fetchFunction, TimeSpan cacheDuration)
        {
            if (_cache.TryGetValue(cacheKey, out T cachedValue))
            {
                _logger.LogInformation($"Cache hit for key: {cacheKey}");
                return cachedValue;
            }

            T value = fetchFunction();
            _cache.Set(cacheKey, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheDuration,
                Size = 1 // Set cache entry size for SizeLimit compatibility
            });
            _logger.LogInformation($"Cache miss for key: {cacheKey}. Value cached.");
            return value;
        }

        // Missing interface methods - Sync versions
        public void UpdateProduct(Product product)
        {
            _productRepository.UpdateProduct(product);
        }

        public List<Product> SearchProducts(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50)
        {
            return _productRepository.SearchProducts(searchTerm, category, brand, page, pageSize);
        }

        public List<string> GetDistinctCategories()
        {
            return _productRepository.GetDistinctCategories();
        }
    }
}
