using Microsoft.AspNetCore.Mvc;
using product_catalog_app.src.services;
using product_catalog_app.src.models;
using product_catalog_app.src.interfaces;

namespace product_catalog_app.src.controllers
{
    public class CategoryController : BaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger) : base(logger)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();

                // Her kategori için ürün sayısını ViewBag ile gönder
                var productCounts = new Dictionary<int, int>();
                foreach (var category in categories)
                {
                    try
                    {
                        var productCount = await _categoryService.GetProductCountByCategoryAsync(category.Id);
                        productCounts[category.Id] = productCount;
                    }
                    catch (Exception productCountEx)
                    {
                        _logger.LogWarning(productCountEx, "Kategori {CategoryId} için ürün sayısı alınamadı", category.Id);
                        productCounts[category.Id] = 0;
                    }
                }

                ViewBag.ProductCounts = productCounts;

                return View(categories);
            }
            catch (Microsoft.Data.Sqlite.SqliteException sqliteEx) when (sqliteEx.SqliteErrorCode == 1)
            {
                _logger.LogError(sqliteEx, "Database tablosu bulunamadı - 'Categories' tablosu yok");
                TempData["Error"] = "Veritabanı henüz hazır değil. Lütfen birkaç saniye bekleyip tekrar deneyin.";
                return View(new List<Category>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategoriler yüklenirken hata oluştu");
                TempData["Error"] = "Kategoriler yüklenirken bir hata oluştu.";
                return View(new List<Category>());
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            try
            {
                await _categoryService.AddCategoryAsync(category.Name, category.Description);
                TempData["Success"] = $"'{category.Name}' kategorisi başarıyla eklendi.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return View(category);
            }
            catch (Exception ex)
            {
                return await HandleCategoryFormExceptionAsync(ex, "Kategori oluşturulurken bir hata oluştu", category);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Kategori bulunamadı.";
                    return RedirectToAction("Index");
                }

                await PrepareCategoryFormViewDataAsync(id);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori düzenleme sayfası yüklenirken hata oluştu");
                TempData["Error"] = "Kategori bilgileri yüklenirken bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            try
            {
                await _categoryService.UpdateCategoryAsync(category);
                TempData["Success"] = $"'{category.Name}' kategorisi başarıyla güncellendi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return await HandleCategoryFormExceptionAsync(ex, "Kategori güncellenirken bir hata oluştu", category);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickEdit([FromForm] QuickEditCategoryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Geçersiz veriler gönderildi." });

                var category = await _categoryService.GetCategoryByIdAsync(request.Id);
                if (category == null)
                    return Json(new { success = false, message = "Kategori bulunamadı." });

                category.Name = request.Name.Trim();
                category.Description = request.Description?.Trim();

                await _categoryService.UpdateCategoryAsync(category);
                
                return Json(new { 
                    success = true, 
                    message = $"'{category.Name}' kategorisi başarıyla güncellendi." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quick edit işlemi başarısız");
                return Json(new { success = false, message = "Kategori güncellenirken hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Kategori bulunamadı.";
                    return RedirectToAction("Index");
                }

                await PrepareCategoryFormViewDataAsync(id);
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori silme sayfası yüklenirken hata oluştu");
                TempData["Error"] = "Kategori bilgileri yüklenirken bir hata oluştu.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                TempData["Success"] = "Kategori başarıyla silindi.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori silinirken hata oluştu");
                TempData["Error"] = "Kategori silinirken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        // AJAX endpoint for quick category creation
        [HttpPost]
        public async Task<IActionResult> CreateQuick([FromBody] CreateQuickCategoryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Json(new { success = false, message = "Kategori adı boş olamaz." });

                if (await _categoryService.CategoryExistsAsync(request.Name))
                    return Json(new { success = false, message = "Bu kategori zaten mevcut." });

                var category = await _categoryService.AddCategoryAsync(request.Name, request.Description);
                return Json(new
                {
                    success = true,
                    categoryId = category.Id,
                    categoryName = category.Name,
                    message = $"'{category.Name}' kategorisi başarıyla eklendi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quick category creation failed");
                return Json(new { success = false, message = "Kategori eklenirken hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                return Json(categories.Select(c => new { id = c.Id, name = c.Name, description = c.Description }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategoriler alınırken hata oluştu");
                return Json(new { error = "Kategoriler yüklenirken hata oluştu." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryNames()
        {
            try
            {
                var categoryNames = await _categoryService.GetCategoryNamesAsync();
                return Json(categoryNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategori adları alınırken hata oluştu");
                return Json(new { error = "Kategori adları yüklenirken hata oluştu." });
            }
        }
        
        #region Common Helper Methods for Category Form Operations

        /// <summary>
        /// Prepares ViewBag data required for category forms (Create/Edit/Delete)
        /// </summary>
        private async Task PrepareCategoryFormViewDataAsync(int? categoryId = null)
        {
            if (categoryId.HasValue)
            {
                var productCount = await _categoryService.GetProductCountByCategoryAsync(categoryId.Value);
                ViewBag.ProductCount = productCount;
            }
        }

        /// <summary>
        /// Handles exceptions in category form operations consistently
        /// </summary>
        private async Task<IActionResult> HandleCategoryFormExceptionAsync(Exception ex, string errorMessage, Category category)
        {
            // Prepare ViewData for the form
            if (category.Id > 0)
            {
                await PrepareCategoryFormViewDataAsync(category.Id);
            }
            
            return await HandleFormExceptionAsync(ex, errorMessage, category);
        }

        #endregion
    }

    // Request models
    public class CreateQuickCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class QuickEditCategoryRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
