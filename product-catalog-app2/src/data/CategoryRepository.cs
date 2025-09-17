using Microsoft.EntityFrameworkCore;
using product_catalog_app.src.models;
using product_catalog_app.src.interfaces;

namespace product_catalog_app.src.data
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ProductDbContext _context;

        public CategoryRepository(ProductDbContext context)
        {
            _context = context;
        }

        // Sync methods for backward compatibility
        public List<Category> GetAllCategories()
        {
            return _context.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            Console.WriteLine($"📋 GetAllCategoriesAsync: {categories.Count} aktif kategori bulundu");
            foreach (var cat in categories.Take(5)) // İlk 5'ini göster
            {
                Console.WriteLine($"   - ID: {cat.Id}, Name: {cat.Name}, IsActive: {cat.IsActive}");
            }
            return categories;
        }

        public Category? GetCategoryById(int id)
        {
            return _context.Categories.AsNoTracking().FirstOrDefault(c => c.Id == id);
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public Category? GetCategoryByName(string name)
        {
            return _context.Categories.AsNoTracking().FirstOrDefault(c => c.Name == name);
        }

        public async Task<Category?> GetCategoryByNameAsync(string name)
        {
            return await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Name == name);
        }

        public void AddCategory(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public void UpdateCategory(Category category)
        {
            _context.Categories.Update(category);
            _context.SaveChanges();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public void DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                category.IsActive = false;
                _context.SaveChanges();
            }
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                Console.WriteLine($"🔥 Kategori siliniyor - ID: {id}, Name: {category.Name}, IsActive: {category.IsActive}");
                Console.WriteLine($"📊 Kategori tracking durumu: {_context.Entry(category).State}");
                
                category.IsActive = false;
                category.UpdatedDate = DateTime.Now;
                
                // Entity'nin değiştiğini açıkça belirtiyoruz
                _context.Entry(category).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                Console.WriteLine($"📊 Kategori tracking durumu sonrası: {_context.Entry(category).State}");
                
                var result = await _context.SaveChangesAsync();
                Console.WriteLine($"✅ SaveChanges sonucu: {result} değişiklik kaydedildi");
                Console.WriteLine($"📍 Kategori durumu sonrası - IsActive: {category.IsActive}");
            }
            else
            {
                Console.WriteLine($"❌ Kategori bulunamadı - ID: {id}");
            }
        }

        public int GetCategoryCount()
        {
            return _context.Categories.Count(c => c.IsActive);
        }

        public async Task<int> GetCategoryCountAsync()
        {
            return await _context.Categories.CountAsync(c => c.IsActive);
        }

        // Interface required methods
        public async Task<int> GetProductCountByCategoryAsync(int categoryId)
        {
            // Önce CategoryId ile kontrol et
            var countByCategoryId = await _context.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == categoryId && !p.IsArchived)
                .CountAsync();

            // Eğer CategoryId ile ürün bulunamazsa, Category string alanı ile kontrol et
            if (countByCategoryId == 0)
            {
                // İlgili kategorinin adını al
                var category = await _context.Categories.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.IsActive);

                if (category != null)
                {
                    var countByCategoryName = await _context.Products
                        .AsNoTracking()
                        .Where(p => p.Category == category.Name && !p.IsArchived)
                        .CountAsync();
                    
                    return countByCategoryName;
                }
            }

            return countByCategoryId;
        }

        public async Task<bool> CategoryExistsAsync(string name)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Name == name && c.IsActive);
        }
    }
}
