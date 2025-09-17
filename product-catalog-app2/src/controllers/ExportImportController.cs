using Microsoft.AspNetCore.Mvc;
using product_catalog_app.src.interfaces;
using product_catalog_app.src.services;
using product_catalog_app.src.models;
using Microsoft.Extensions.Logging;

namespace product_catalog_app.src.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportImportController : ControllerBase
    {
        private readonly ExportService _exportService;
        private readonly ImportService _importService;
        private readonly CategoryService _categoryService;
        private readonly ProductService _productService;
        private readonly ILogger<ExportImportController> _logger;

        public ExportImportController(
            ExportService exportService,
            ImportService importService,
            CategoryService categoryService,
            ProductService productService,
            ILogger<ExportImportController> logger)
        {
            _exportService = exportService;
            _importService = importService;
            _categoryService = categoryService;
            _productService = productService;
            _logger = logger;
        }

        #region Export Endpoints

        /// <summary>
        /// Mevcut sütunları getir
        /// </summary>
        [HttpGet("columns")]
        public IActionResult GetAvailableColumns()
        {
            try
            {
                _logger.LogInformation("GetAvailableColumns called");
                var columns = _exportService.GetAvailableColumns();
                _logger.LogInformation("Retrieved {Count} columns from export service", columns?.Count ?? 0);
                
                if (columns == null || !columns.Any())
                {
                    _logger.LogWarning("No columns found from export service");
                    return BadRequest(new { success = false, message = "No columns found" });
                }
                
                // Kategoriye göre grupla
                var columnsByCategory = columns.GroupBy(c => c.Category)
                    .Select(g => new 
                    {
                        category = g.Key,  // lowercase for JavaScript compatibility
                        columns = g.OrderBy(c => c.Order).ToList()  // lowercase for JavaScript compatibility
                    })
                    .OrderBy(g => g.category)
                    .ToList();
                
                _logger.LogInformation("Grouped into {Count} categories", columnsByCategory.Count);
                
                var result = new { 
                    success = true, 
                    categories = columnsByCategory,
                    defaultSelected = _exportService.GetDefaultSelectedColumns()
                };
                
                _logger.LogInformation("Returning result with {CategoryCount} categories", columnsByCategory.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAvailableColumns");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Sütun seçimli export (tüm formatlar için)
        /// </summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportWithColumns([FromBody] ExportColumnFilter filter)
        {
            try
            {
                _logger.LogInformation("ExportWithColumns başlatılıyor - Format: {Format}", filter.ExportFormat);

                // Format'a göre mevcut metodları kullan
                var result = filter.ExportFormat.ToLower() switch
                {
                    "xml" => await _exportService.ExportToXmlWithColumnsAsync(filter),
                    "json" => await _exportService.ExportToJsonWithColumnsAsync(filter),
                    "csv" => await _exportService.ExportToCsvWithColumnsAsync(filter),
                    "xlsx" => await _exportService.ExportToExcelWithColumnsAsync(filter),
                    _ => await _exportService.ExportToExcelWithColumnsAsync(filter) // default to Excel
                };

                if (!result.Success)
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }

                return File(result.Content!, result.ContentType!, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sütun seçimli export sırasında hata oluştu");
                return BadRequest(new { error = "Export işlemi sırasında hata oluştu: " + ex.Message });
            }
        }

        #endregion

        #region Import Endpoints

        /// <summary>
        /// XML dosyasından import
        /// </summary>
        [HttpPost("import/xml")]
        public async Task<IActionResult> ImportXml(IFormFile file, [FromQuery] bool updateExisting = true)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Dosya seçilmedi" });
            }

            if (!file.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Sadece XML dosyaları kabul edilir" });
            }

            _logger.LogInformation("XML import başlatılıyor - Dosya: {FileName}, Boyut: {Size} bytes", 
                file.FileName, file.Length);

            var options = new ImportOptions
            {
                UpdateExisting = updateExisting,
                CreateCategories = true,
                SkipErrors = true
            };

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _importService.ImportFromXmlAsync(stream, options);

                if (!result.Success)
                {
                    _logger.LogError("XML import başarısız - Hata: {Error}", result.ErrorMessage);
                    return BadRequest(new { error = result.ErrorMessage, details = result.Errors });
                }

                _logger.LogInformation("XML import başarılı - İşlenen: {Total}, Eklenen: {Inserted}, Güncellenen: {Updated}, Hata: {Errors}, Süre: {Duration}ms",
                    result.TotalProcessed, result.InsertedCount, result.UpdatedCount, result.ErrorCount, result.ProcessingTime.TotalMilliseconds);

                return Ok(new
                {
                    success = true,
                    message = "Import başarılı",
                    totalProcessed = result.TotalProcessed,
                    inserted = result.InsertedCount,
                    updated = result.UpdatedCount,
                    errors = result.ErrorCount,
                    errorDetails = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML import sırasında beklenmeyen hata oluştu");
                return BadRequest(new { error = "XML import sırasında hata oluştu: " + ex.Message });
            }
        }

        /// <summary>
        /// JSON dosyasından import
        /// </summary>
        [HttpPost("import/json")]
        public async Task<IActionResult> ImportJson(IFormFile file, [FromQuery] bool updateExisting = true)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Dosya seçilmedi" });
            }

            if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Sadece JSON dosyaları kabul edilir" });
            }

            var options = new ImportOptions
            {
                UpdateExisting = updateExisting,
                CreateCategories = true,
                SkipErrors = true
            };

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportFromJsonAsync(stream, options);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage, details = result.Errors });
            }

            return Ok(new
            {
                success = true,
                message = "Import başarılı",
                totalProcessed = result.TotalProcessed,
                inserted = result.InsertedCount,
                updated = result.UpdatedCount,
                errors = result.ErrorCount,
                errorDetails = result.Errors
            });
        }

        /// <summary>
        /// CSV dosyasından import
        /// </summary>
        [HttpPost("import/csv")]
        public async Task<IActionResult> ImportCsv(IFormFile file, [FromQuery] bool updateExisting = true)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Dosya seçilmedi" });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Sadece CSV dosyaları kabul edilir" });
            }

            var options = new ImportOptions
            {
                UpdateExisting = updateExisting,
                CreateCategories = true,
                SkipErrors = true
            };

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportFromCsvAsync(stream, options);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage, details = result.Errors });
            }

            return Ok(new
            {
                success = true,
                message = "Import başarılı",
                totalProcessed = result.TotalProcessed,
                inserted = result.InsertedCount,
                updated = result.UpdatedCount,
                errors = result.ErrorCount,
                errorDetails = result.Errors
            });
        }

        /// <summary>
        /// Excel dosyasından import
        /// </summary>
        [HttpPost("import/excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file, [FromQuery] bool updateExisting = true, [FromQuery] bool preserveArchiveStatus = true)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Dosya seçilmedi" });
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "Sadece Excel dosyaları kabul edilir" });
            }

            _logger.LogInformation("Excel import başlatılıyor - Dosya: {FileName}, Boyut: {Size} bytes", 
                file.FileName, file.Length);

            var options = new ImportOptions
            {
                UpdateExisting = updateExisting,
                CreateCategories = true,
                SkipErrors = true,
                PreserveArchiveStatus = preserveArchiveStatus
            };

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _importService.ImportFromExcelAsync(stream, options);

                if (!result.Success)
                {
                    _logger.LogError("Excel import başarısız - Hata: {Error}", result.ErrorMessage);
                    return BadRequest(new { error = result.ErrorMessage, details = result.Errors });
                }

                _logger.LogInformation("Excel import başarılı - İşlenen: {Total}, Eklenen: {Inserted}, Güncellenen: {Updated}, Hata: {Errors}, Süre: {Duration}ms",
                    result.TotalProcessed, result.InsertedCount, result.UpdatedCount, result.ErrorCount, result.ProcessingTime.TotalMilliseconds);

                return Ok(new
                {
                    success = true,
                    message = "Excel import başarılı",
                    totalProcessed = result.TotalProcessed,
                    inserted = result.InsertedCount,
                    updated = result.UpdatedCount,
                    errors = result.ErrorCount,
                    errorDetails = result.Errors,
                    processingTimeMs = Math.Round(result.ProcessingTime.TotalMilliseconds, 2),
                    batchCount = result.BatchCount,
                    performance = new
                    {
                        avgItemsPerSecond = Math.Round(result.TotalProcessed / Math.Max(result.ProcessingTime.TotalSeconds, 0.001), 2),
                        totalTimeSeconds = Math.Round(result.ProcessingTime.TotalSeconds, 2)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel import sırasında beklenmeyen hata oluştu");
                return BadRequest(new { error = "Excel import sırasında hata oluştu: " + ex.Message });
            }
        }

        #endregion

        #region Helper Endpoints

        /// <summary>
        /// Mevcut kategorileri getir
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetCategoryNamesAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Mevcut markaları getir
        /// </summary>
        [HttpGet("brands")]
        public IActionResult GetBrands()
        {
            var brands = _productService.GetDistinctBrands();
            return Ok(brands);
        }

        /// <summary>
        /// Export/Import istatistikleri
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var allProducts = await _productService.GetAllProductsAsync();
            
            var stats = new
            {
                totalProducts = allProducts.Count,
                activeProducts = allProducts.Count(p => !p.IsArchived), // Arşivde olmayan = Aktif
                activeCategoriesCount = await _categoryService.GetActiveCategoryCountAsync(), // Aktif kategoriler
                archivedProducts = allProducts.Count(p => p.IsArchived), // Arşivde olan
                categories = await _categoryService.GetCategoryNamesAsync(),
                brands = _productService.GetDistinctBrands()
            };

            return Ok(stats);
        }

        #endregion
    }
}
