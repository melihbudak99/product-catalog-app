using product_catalog_app.src.models;
using product_catalog_app.src.interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace product_catalog_app.src.services
{
    /// <summary>
    /// Simple CategoryService implementation for compatibility
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _categoryRepository.GetAllCategoriesAsync();
        }

        public async Task<List<string>> GetCategoryNamesAsync()
        {
            var categories = await _categoryRepository.GetAllCategoriesAsync();
            return categories.Select(c => c.Name).ToList();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _categoryRepository.GetCategoryByIdAsync(id);
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            return await _categoryRepository.GetCategoryByNameAsync(name);
        }

        public async Task<Category> AddCategoryAsync(string name, string? description = null)
        {
            var category = new Category 
            { 
                Name = name, 
                Description = description ?? "", 
                IsActive = true 
            };
            return await _categoryRepository.AddCategoryAsync(category);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            await _categoryRepository.UpdateCategoryAsync(category);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            // Kategoriyi silmeden önce bu kategoriye ait ürün olup olmadığını kontrol et
            var productCount = await _categoryRepository.GetProductCountByCategoryAsync(id);
            if (productCount > 0)
            {
                throw new InvalidOperationException($"Bu kategori {productCount} ürün tarafından kullanılmaktadır. Kategoriyi silmek için önce ürünleri başka kategoriye taşıyın veya silin.");
            }

            await _categoryRepository.DeleteCategoryAsync(id);
        }

        public async Task<bool> CategoryExistsAsync(string name)
        {
            return await _categoryRepository.CategoryExistsAsync(name);
        }

        public async Task<int> GetProductCountByCategoryAsync(int categoryId)
        {
            return await _categoryRepository.GetProductCountByCategoryAsync(categoryId);
        }

        public async Task<int> GetActiveCategoryCountAsync()
        {
            return await _categoryRepository.GetCategoryCountAsync();
        }

        public async Task<int?> GetOrCreateCategoryIdAsync(string categoryName)
        {
            var existingCategory = await GetCategoryByNameAsync(categoryName);
            if (existingCategory != null)
            {
                return existingCategory.Id;
            }

            var newCategory = await AddCategoryAsync(categoryName);
            return newCategory.Id;
        }
    }
}
