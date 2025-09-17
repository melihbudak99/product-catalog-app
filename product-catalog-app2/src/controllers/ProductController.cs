using Microsoft.AspNetCore.Mvc;
using product_catalog_app.src.models;
using product_catalog_app.src.services;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using ClosedXML.Excel;
using System.Text.RegularExpressions;
using System.Net;

namespace product_catalog_app.src.controllers
{
    public class ProductController : BaseController
    {
        private readonly ProductService _productService;
        private readonly XmlService _xmlService;
        private readonly CategoryService _categoryService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(ProductService productService, XmlService xmlService, CategoryService categoryService, 
            ILogger<ProductController> logger, IWebHostEnvironment hostEnvironment) : base(logger)
        {
            _productService = productService;
            _xmlService = xmlService;
            _categoryService = categoryService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 50, string search = "", string category = "", string brand = "", 
                                              string status = "", string material = "", string color = "", string eanCode = "",
                                              decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
                                              int? minWarranty = null, int? maxWarranty = null, string sortBy = "updated", string sortDirection = "desc",
                                              bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "")
        {
            try
            {
                _logger.LogInformation("Index called with advanced filters: page={Page}, pageSize={PageSize}, search='{Search}', category='{Category}', brand='{Brand}', status='{Status}', material='{Material}', color='{Color}'", 
                    page, pageSize, search, category, brand, status, material, color);

                // OPTIMIZED: Use async database operations for better performance with advanced filtering
                var products = await _productService.SearchProductsAdvancedAsync(search, category, brand, status, material, color, eanCode,
                    minWeight, maxWeight, minDesi, maxDesi, minWarranty, maxWarranty, sortBy, sortDirection,
                    hasImage, hasEan, hasBarcode, barcodeType, page, pageSize);
                
                var totalCount = await _productService.GetProductCountAdvancedAsync(search, category, brand, status, material, color, eanCode,
                    minWeight, maxWeight, minDesi, maxDesi, minWarranty, maxWarranty, hasImage, hasEan, hasBarcode, barcodeType);

                _logger.LogInformation("Index advanced results: products.Count={ProductCount}, totalCount={TotalCount}", products.Count, totalCount);

                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Get dropdown data efficiently with caching
                ViewBag.Categories = _productService.GetDistinctCategories();
                ViewBag.Brands = _productService.GetDistinctBrands();
                ViewBag.Materials = _productService.GetDistinctMaterials();
                ViewBag.Colors = _productService.GetDistinctColors();

                // ViewBag ile sayfalama bilgilerini gönder
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalCount = totalCount;
                ViewBag.PageSize = pageSize;
                ViewBag.Search = search ?? "";
                ViewBag.Category = category ?? "";
                ViewBag.Brand = brand ?? "";
                ViewBag.Status = status ?? "";
                ViewBag.Material = material ?? "";
                ViewBag.Color = color ?? "";
                ViewBag.EanCode = eanCode ?? "";
                ViewBag.MinWeight = minWeight;
                ViewBag.MaxWeight = maxWeight;
                ViewBag.MinDesi = minDesi;
                ViewBag.MaxDesi = maxDesi;
                ViewBag.MinWarranty = minWarranty;
                ViewBag.MaxWarranty = maxWarranty;
                ViewBag.SortBy = sortBy;
                ViewBag.SortDirection = sortDirection;
                ViewBag.HasImage = hasImage;
                ViewBag.HasEan = hasEan;
                ViewBag.HasBarcode = hasBarcode;
                ViewBag.BarcodeType = barcodeType ?? "";

                // Index sayfası (arşiv değil)
                ViewData["IsArchivePage"] = false;
                ViewBag.IsArchivePage = false;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index sayfası yüklenirken hata oluştu");
                TempData["Error"] = "Ürünler yüklenirken bir hata oluştu.";
                return View(new List<Product>());
            }
        }

        public IActionResult Details(int id)
        {
            var product = _productService.GetProductById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }


        // XML ürün ekleme (detaylı)
        public async Task<IActionResult> CreateProduct()
        {
            _logger.LogInformation("CreateProduct GET çağrıldı");
            await PrepareProductFormViewDataAsync();
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            _logger.LogInformation("CreateProduct POST çağrıldı!");

            try
            {
                if (product == null)
                {
                    _logger.LogWarning("Product is null");
                    await PrepareProductFormViewDataAsync();
                    return View(new Product());
                }

                // Process and validate form data
                await ProcessProductFormDataAsync(product);

                // Clear optional field validation errors
                ClearOptionalFieldValidationErrors();

                // Validate product data
                if (!ValidateProductData(product))
                {
                    await PrepareProductFormViewDataAsync();
                    return View(product);
                }

                // Validate for uniqueness
                var validationResult = await ValidateProductForSaveAsync(product, isEdit: false);
                if (!validationResult.IsValid)
                {
                    TempData["ValidationErrors"] = validationResult.ValidationErrors;
                    await PrepareProductFormViewDataAsync();
                    return View(product);
                }

                // Set required fields for new product
                product.CreatedDate = DateTime.UtcNow;
                product.UpdatedDate = DateTime.UtcNow;
                product.IsArchived = false;

                // Save product
                _productService.AddProduct(product);
                _logger.LogInformation("Ürün başarıyla eklendi: {ProductName}", product.Name);
                TempData["Success"] = "Ürün başarıyla eklendi!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return await HandleProductFormExceptionAsync(ex, "Ürün eklenirken hata oluştu", product);
            }
        }

        // GET: Product/EditProduct/5
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            // ImageUrls listesinin null olmamasını garanti et
            if (product.ImageUrls == null)
            {
                product.ImageUrls = new List<string>();
            }

            await PrepareProductFormViewDataAsync();

            _logger.LogInformation($"EditProduct GET - Ürün ID: {product.Id}, Ürün Adı: {product.Name}");
            _logger.LogInformation($"EditProduct GET - Görsel sayısı: {product.ImageUrls.Count}");
            _logger.LogInformation($"EditProduct GET - CategoryId: {product.CategoryId}");

            return View(product);
        }

        // POST: Product/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product product)
        {
            _logger.LogInformation("EditProduct POST çağrıldı!");

            try
            {
                if (product == null)
                {
                    await PrepareProductFormViewDataAsync();
                    return View(product);
                }

                // Process and validate form data
                await ProcessProductFormDataAsync(product);

                // Clear optional field validation errors
                ClearOptionalFieldValidationErrors();

                // Validate product data
                if (!ValidateProductData(product))
                {
                    await PrepareProductFormViewDataAsync();
                    return View(product);
                }

                // Validate for uniqueness (excluding current product)
                var validationResult = await ValidateProductForSaveAsync(product, isEdit: true);
                if (!validationResult.IsValid)
                {
                    TempData["ValidationErrors"] = validationResult.ValidationErrors;
                    await PrepareProductFormViewDataAsync();
                    return View(product);
                }

                // Update timestamp with microsecond precision to ensure uniqueness
                product.UpdatedDate = DateTime.UtcNow.AddTicks(product.Id % 10000);

                // Save changes
                await _productService.UpdateProductAsync(product);
                _logger.LogInformation("Ürün başarıyla güncellendi: {ProductName}", product.Name);
                TempData["Success"] = "Ürün başarıyla güncellendi!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return await HandleProductFormExceptionAsync(ex, "Ürün güncellenirken hata oluştu", product);
            }
        }

        // REMOVED: Old Export/Import methods - Using advanced export system instead

        [HttpPost]
        public async Task<IActionResult> ExportSelectedToExcel([FromBody] List<int> selectedIds)
        {
            try
            {
                if (selectedIds == null || !selectedIds.Any())
                {
                    return BadRequest("Seçili ürün bulunamadı.");
                }

                var products = await _productService.GetProductsByIdsAsync(selectedIds);
                
                if (!products.Any())
                {
                    return BadRequest("Seçili ürünler bulunamadı.");
                }

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Seçili Ürünler");
                    
                    // Header
                    ws.Cell(1, 1).Value = "Id";
                    ws.Cell(1, 2).Value = "Name";
                    ws.Cell(1, 3).Value = "SKU";
                    ws.Cell(1, 4).Value = "Brand";
                    ws.Cell(1, 5).Value = "Category";
                    ws.Cell(1, 6).Value = "DescriptionHtml";
                    ws.Cell(1, 7).Value = "DescriptionPlain";
                    ws.Cell(1, 8).Value = "Features";
                    ws.Cell(1, 9).Value = "Notes";
                    ws.Cell(1, 10).Value = "Weight";
                    ws.Cell(1, 11).Value = "Desi";
                    ws.Cell(1, 12).Value = "Width";
                    ws.Cell(1, 13).Value = "Height";
                    ws.Cell(1, 14).Value = "Depth";
                    ws.Cell(1, 15).Value = "Length";
                    ws.Cell(1, 16).Value = "WarrantyMonths";
                    ws.Cell(1, 17).Value = "Material";
                    ws.Cell(1, 18).Value = "Color";
                    ws.Cell(1, 19).Value = "EanCode";
                    ws.Cell(1, 20).Value = "TrendyolBarcode";
                    ws.Cell(1, 21).Value = "HepsiburadaBarcode";
                    ws.Cell(1, 22).Value = "HepsiburadaSellerStockCode";
                    ws.Cell(1, 23).Value = "KoctasBarcode";
                    ws.Cell(1, 24).Value = "KoctasIstanbulBarcode";
                    ws.Cell(1, 25).Value = "HepsiburadaTedarikBarcode";
                    ws.Cell(1, 26).Value = "PttAvmBarcode";
                    ws.Cell(1, 27).Value = "PazaramaBarcode";
                    ws.Cell(1, 28).Value = "HaceyapiBarcode";
                    ws.Cell(1, 29).Value = "AmazonBarcode";
                    ws.Cell(1, 30).Value = "N11CatalogId";
                    ws.Cell(1, 31).Value = "N11ProductCode";
                    ws.Cell(1, 32).Value = "SpareBarcode1";
                    ws.Cell(1, 33).Value = "SpareBarcode2";
                    ws.Cell(1, 34).Value = "SpareBarcode3";
                    ws.Cell(1, 35).Value = "SpareBarcode4";
                    ws.Cell(1, 36).Value = "EntegraUrunId";
                    ws.Cell(1, 37).Value = "EntegraUrunKodu";
                    ws.Cell(1, 38).Value = "EntegraBarkod";
                    
                    // Logo barkodları ayrı sütunlarda  
                    for (int i = 1; i <= 10; i++)
                        ws.Cell(1, 38 + i).Value = $"LogoBarcode{i}";
                    
                    ws.Cell(1, 49).Value = "IsArchived";
                    ws.Cell(1, 50).Value = "CreatedDate";
                    ws.Cell(1, 51).Value = "UpdatedDate";
                    
                    // Image columns
                    for (int i = 1; i <= 10; i++) 
                        ws.Cell(1, 51 + i).Value = $"ImageUrl{i}";
                    for (int i = 1; i <= 10; i++) 
                        ws.Cell(1, 58 + i).Value = $"MarketplaceImageUrl{i}";

                    // Header formatting
                    var headerRange = ws.Range(1, 1, 1, 68);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

                    // Data rows
                    int row = 2;
                    foreach (var p in products)
                    {
                        ws.Cell(row, 1).Value = p.Id;
                        ws.Cell(row, 2).Value = p.Name;
                        ws.Cell(row, 3).Value = p.SKU ?? string.Empty;
                        ws.Cell(row, 4).Value = p.Brand ?? string.Empty;
                        ws.Cell(row, 5).Value = p.Category ?? string.Empty;
                        ws.Cell(row, 6).Value = CleanHtmlForExport(p.Description ?? string.Empty);
                        ws.Cell(row, 7).Value = StripHtmlTags(p.Description ?? string.Empty);
                        ws.Cell(row, 8).Value = p.Features ?? string.Empty;
                        ws.Cell(row, 9).Value = p.Notes ?? string.Empty;
                        ws.Cell(row, 10).Value = p.Weight;
                        ws.Cell(row, 11).Value = p.Desi;
                        ws.Cell(row, 12).Value = p.Width;
                        ws.Cell(row, 13).Value = p.Height;
                        ws.Cell(row, 14).Value = p.Depth;
                        ws.Cell(row, 15).Value = p.Length;
                        ws.Cell(row, 16).Value = p.WarrantyMonths;
                        ws.Cell(row, 17).Value = p.Material ?? string.Empty;
                        ws.Cell(row, 18).Value = p.Color ?? string.Empty;
                        ws.Cell(row, 19).Value = p.EanCode ?? string.Empty;
                        ws.Cell(row, 20).Value = p.TrendyolBarcode ?? string.Empty;
                        ws.Cell(row, 21).Value = p.HepsiburadaBarcode ?? string.Empty;
                        ws.Cell(row, 22).Value = p.HepsiburadaSellerStockCode ?? string.Empty;
                        ws.Cell(row, 23).Value = p.KoctasBarcode ?? string.Empty;
                        ws.Cell(row, 24).Value = p.KoctasIstanbulBarcode ?? string.Empty;
                        ws.Cell(row, 25).Value = p.HepsiburadaTedarikBarcode ?? string.Empty;
                        ws.Cell(row, 26).Value = p.PttAvmBarcode ?? string.Empty;
                        ws.Cell(row, 27).Value = p.PazaramaBarcode ?? string.Empty;
                        ws.Cell(row, 28).Value = p.HaceyapiBarcode ?? string.Empty;
                        ws.Cell(row, 29).Value = p.AmazonBarcode ?? string.Empty;
                        ws.Cell(row, 30).Value = p.N11CatalogId ?? string.Empty;
                        ws.Cell(row, 31).Value = p.N11ProductCode ?? string.Empty;
                        ws.Cell(row, 32).Value = p.SpareBarcode1 ?? string.Empty;
                        ws.Cell(row, 33).Value = p.SpareBarcode2 ?? string.Empty;
                        ws.Cell(row, 34).Value = p.SpareBarcode3 ?? string.Empty;
                        ws.Cell(row, 35).Value = p.SpareBarcode4 ?? string.Empty;
                        ws.Cell(row, 36).Value = p.EntegraUrunId ?? string.Empty;
                        ws.Cell(row, 37).Value = p.EntegraUrunKodu ?? string.Empty;
                        ws.Cell(row, 38).Value = p.EntegraBarkod ?? string.Empty;
                        
                        // Logo barkodları ayrı sütunlarda
                        for (int i = 1; i <= 10; i++) 
                        {
                            ws.Cell(row, 38 + i).Value = GetLogoBarcodeByIndex(p.LogoBarcodes, i - 1);
                        }
                        
                        ws.Cell(row, 49).Value = p.IsArchived;
                        ws.Cell(row, 50).Value = p.CreatedDate;
                        ws.Cell(row, 51).Value = p.UpdatedDate;
                        
                        // Image URLs
                        for (int i = 1; i <= 10; i++) 
                        {
                            var imageUrl = (p.ImageUrls != null && p.ImageUrls.Count >= i) ? p.ImageUrls[i - 1] : "";
                            ws.Cell(row, 51 + i).Value = imageUrl;
                        }
                        
                        for (int i = 1; i <= 10; i++) 
                        {
                            var marketplaceImageUrl = (p.MarketplaceImageUrls != null && p.MarketplaceImageUrls.Count >= i) ? p.MarketplaceImageUrls[i - 1] : "";
                            ws.Cell(row, 61 + i).Value = marketplaceImageUrl;
                        }
                        
                        row++;
                    }

                    // Auto-fit columns
                    ws.Columns().AdjustToContents();

                    using (var stream = new System.IO.MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        var fileName = $"secili-urunler-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx";
                        
                        _logger.LogInformation($"Excel export tamamlandı. {products.Count} ürün dışa aktarıldı.");
                        
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seçili ürünler Excel export sırasında hata oluştu");
                return StatusCode(500, new { error = $"Excel export hatası: {ex.Message}" });
            }
        }

        // REMOVED: GenerateCsvContent and GenerateXlsContent helper methods - Using advanced export system instead

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // CSV için tırnak işaretlerini escape et
            return value.Replace("\"", "\"\"");
        }

        private string EscapeXmlValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // XML için özel karakterleri escape et
            return value.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&apos;");
        }

        // XML Manager sayfası
        public IActionResult XmlManager()
        {
            return View();
        }

        // Export/Import sayfası - XmlManager'a yönlendir
        public IActionResult ExportImport()
        {
            return RedirectToAction("XmlManager");
        }

        // E-Commerce XML Manager sayfası - XmlManager'a yönlendir
        public IActionResult ECommerceXmlManager()
        {
            return View();
        }

        // Archive Management
        public IActionResult Archive(int page = 1, int pageSize = 50, string search = "", string category = "", string brand = "")
        {
            // Get archived products with pagination
            var products = _productService.GetArchivedProducts(search, category, brand, page, pageSize);
            var totalCount = _productService.GetArchivedProductCount(search, category, brand);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Get dropdown data efficiently
            ViewBag.Categories = _productService.GetDistinctCategories();
            ViewBag.Brands = _productService.GetDistinctBrands();

            // ViewBag ile sayfalama bilgilerini gönder
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search ?? "";
            ViewBag.Category = category ?? "";
            ViewBag.Brand = brand ?? "";

            // Archive sayfası olduğunu belirt - explicit cast ile
            ViewData["IsArchivePage"] = true;
            ViewBag.IsArchivePage = true;

            return View("Index", products); // Index view'ını kullan ama arşiv modunda
        }

        [HttpPost]
        [Route("Product/ArchiveProduct/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult ArchiveProduct(int id)
        {
            try
            {
                var product = _productService.GetProductById(id);
                if (product == null)
                {
                    TempData["Error"] = "Arşivlenecek ürün bulunamadı.";
                    return RedirectToAction("Index");
                }

                var productName = product.Name;
                _productService.ArchiveProduct(id);

                _logger.LogInformation("Ürün arşivlendi: {ProductName} (ID: {ProductId})", productName, id);
                TempData["Success"] = $"'{productName}' ürünü başarıyla arşivlendi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün arşivlenirken hata oluştu: {ProductId}", id);
                TempData["Error"] = "Ürün arşivlenirken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("Product/UnarchiveProduct/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult UnarchiveProduct(int id)
        {
            try
            {
                var product = _productService.GetProductById(id);
                if (product == null)
                {
                    TempData["Error"] = "Arşivden çıkarılacak ürün bulunamadı.";
                    return RedirectToAction("Archive");
                }

                var productName = product.Name;
                _productService.UnarchiveProduct(id);

                _logger.LogInformation("Ürün arşivden çıkarıldı: {ProductName} (ID: {ProductId})", productName, id);
                TempData["Success"] = $"'{productName}' ürünü başarıyla arşivden çıkarıldı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün arşivden çıkarılırken hata oluştu: {ProductId}", id);
                TempData["Error"] = "Ürün arşivden çıkarılırken bir hata oluştu.";
            }

            return RedirectToAction("Archive");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkArchive(List<int> productIds)
        {
            try
            {
                if (productIds == null || !productIds.Any())
                {
                    TempData["Error"] = "Hiçbir ürün seçilmedi.";
                    return RedirectToAction("Index");
                }

                _productService.BulkArchiveProducts(productIds);

                _logger.LogInformation("{Count} ürün toplu olarak arşivlendi", productIds.Count);
                TempData["Success"] = $"{productIds.Count} ürün başarıyla arşivlendi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu arşivleme sırasında hata oluştu");
                TempData["Error"] = "Toplu arşivleme sırasında bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BulkUnarchive(List<int> productIds)
        {
            try
            {
                if (productIds == null || !productIds.Any())
                {
                    TempData["Error"] = "Hiçbir ürün seçilmedi.";
                    return RedirectToAction("Archive");
                }

                _productService.BulkUnarchiveProducts(productIds);

                _logger.LogInformation("{Count} ürün toplu olarak arşivden çıkarıldı", productIds.Count);
                TempData["Success"] = $"{productIds.Count} ürün başarıyla arşivden çıkarıldı.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu arşivden çıkarma sırasında hata oluştu");
                TempData["Error"] = "Toplu arşivden çıkarma sırasında bir hata oluştu.";
            }

            return RedirectToAction("Archive");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(List<int> productIds)
        {
            try
            {
                if (productIds == null || !productIds.Any())
                {
                    TempData["Error"] = "Hiçbir ürün seçilmedi.";
                    return RedirectToAction("Index");
                }

                await _productService.BulkDeleteProductsAsync(productIds);

                _logger.LogInformation("{Count} ürün toplu olarak silindi", productIds.Count);
                TempData["Success"] = $"{productIds.Count} ürün başarıyla silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu silme sırasında hata oluştu");
                TempData["Error"] = "Toplu silme sırasında bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("Product/DeleteProduct/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Ürün bulunamadı.";
                    return RedirectToAction("Index");
                }

                var productName = product.Name;
                await _productService.DeleteProductAsync(id);

                _logger.LogInformation("Ürün silindi: {ProductName} (ID: {ProductId})", productName, id);
                TempData["Success"] = $"'{productName}' ürünü başarıyla silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün silinirken hata oluştu: {ProductId}", id);
                TempData["Error"] = "Ürün silinirken bir hata oluştu.";
            }

            return RedirectToAction("Index");
        }

        // Utility method for stripping HTML tags from text
        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Remove HTML tags but preserve basic formatting
            string cleaned = html;
            
            // Replace list items with bullet points
            cleaned = Regex.Replace(cleaned, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"</li>", "\n", RegexOptions.IgnoreCase);
            
            // Replace paragraph breaks with line breaks
            cleaned = Regex.Replace(cleaned, @"</p>\s*<p[^>]*>", "\n\n", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"<p[^>]*>", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"</p>", "\n", RegexOptions.IgnoreCase);
            
            // Replace line breaks
            cleaned = Regex.Replace(cleaned, @"<br\s*/?>\s*", "\n", RegexOptions.IgnoreCase);
            
            // Remove all remaining HTML tags
            cleaned = Regex.Replace(cleaned, @"<[^>]+>", "", RegexOptions.IgnoreCase);
            
            // Clean up whitespace
            cleaned = Regex.Replace(cleaned, @"\n\s*\n", "\n\n", RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"^\s+|\s+$", "", RegexOptions.Multiline);
            
            // Decode HTML entities
            cleaned = System.Net.WebUtility.HtmlDecode(cleaned);
            
            return cleaned.Trim();
        }

        // Method to clean and format HTML for export
        private string CleanHtmlForExport(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            string cleaned = html;
            
            // Remove Microsoft Word specific classes and styles
            cleaned = Regex.Replace(cleaned, @"class=""MsoNormal""", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"style=""[^""]*mso-[^""]*""", "", RegexOptions.IgnoreCase);
            
            // Fix nested paragraph tags - remove inner p tags
            cleaned = Regex.Replace(cleaned, @"<p[^>]*>(\s*<p[^>]*>)+", "<p>", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"(</p>\s*)+</p>", "</p>", RegexOptions.IgnoreCase);
            
            // Remove empty paragraphs
            cleaned = Regex.Replace(cleaned, @"<p[^>]*>\s*</p>", "", RegexOptions.IgnoreCase);
            
            // Fix list structure - move ul outside of p tags
            cleaned = Regex.Replace(cleaned, @"<p[^>]*>(\s*<ul>)", "$1", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"(</ul>\s*)</p>", "$1", RegexOptions.IgnoreCase);
            
            // Clean up multiple consecutive line breaks and spaces
            cleaned = Regex.Replace(cleaned, @"\s+", " ", RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @">\s+<", "><", RegexOptions.IgnoreCase);
            
            // Remove style attributes for cleaner output
            cleaned = Regex.Replace(cleaned, @"\s*style=""[^""]*""", "", RegexOptions.IgnoreCase);
            
            return cleaned.Trim();
        }

        // Production-ready health check endpoint
        [HttpGet("health-status")]
        public async Task<IActionResult> GetHealthStatus()
        {
            try
            {
                var productCount = await _productService.GetProductCountAsync("", "", "");
                return Json(new { status = "healthy", productCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Json(new { status = "unhealthy", error = "Service unavailable" });
            }
        }

        // Production-ready search test endpoint with security
        [HttpGet("search-test")]
        public async Task<IActionResult> TestSearch(string search = "", string category = "", string brand = "")
        {
            // Only available in development environment for security
            if (!_hostEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                var searchResults = await _productService.SearchProductsAsync(search, category, brand, 1, 10);
                var totalCount = await _productService.GetProductCountAsync(search, category, brand);
                
                return Json(new 
                {
                    success = true,
                    resultsCount = searchResults.Count,
                    totalMatches = totalCount,
                    searchParams = new { search, category, brand }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search test failed");
                return Json(new { success = false, error = "Search test failed" });
            }
        }

        // Helper method to validate product data
        private bool ValidateProductData(Product product)
        {
            var isValid = true;

            // Only Name is required
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                ModelState.AddModelError("Name", "Ürün adı zorunludur");
                isValid = false;
            }

            // Validate numeric fields if they have values
            if (product.Weight < 0)
            {
                ModelState.AddModelError("Weight", "Ağırlık negatif olamaz");
                isValid = false;
            }

            if (product.Desi < 0)
            {
                ModelState.AddModelError("Desi", "Desi negatif olamaz");
                isValid = false;
            }

            if (product.WarrantyMonths < 0)
            {
                ModelState.AddModelError("WarrantyMonths", "Garanti süresi negatif olamaz");
                isValid = false;
            }

            // Validate URL formats for images and videos
            foreach (var imageUrl in product.ImageUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
            {
                if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                {
                    ModelState.AddModelError("ImageUrls", $"Geçersiz görsel URL: {imageUrl}");
                    isValid = false;
                }
            }

            foreach (var videoUrl in product.VideoUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
            {
                if (!Uri.IsWellFormedUriString(videoUrl, UriKind.Absolute))
                {
                    ModelState.AddModelError("VideoUrls", $"Geçersiz video URL: {videoUrl}");
                    isValid = false;
                }
            }

            return isValid;
        }

        // Helper method to process media URLs from form
        private void ProcessMediaUrls(Product product)
        {
            var imageUrls = new List<string>();
            var marketplaceImageUrls = new List<string>();
            var videoUrls = new List<string>();

            // Process image URLs
            for (int i = 0; i < 10; i++)
            {
                var imageUrl = Request.Form[$"ImageUrls[{i}]"].ToString();
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    imageUrls.Add(imageUrl.Trim());
                }

                var marketplaceImageUrl = Request.Form[$"MarketplaceImageUrls[{i}]"].ToString();
                if (!string.IsNullOrWhiteSpace(marketplaceImageUrl))
                {
                    marketplaceImageUrls.Add(marketplaceImageUrl.Trim());
                }
            }

            // Process video URLs
            for (int i = 0; i < 5; i++)
            {
                var videoUrl = Request.Form[$"VideoUrls[{i}]"].ToString();
                if (!string.IsNullOrWhiteSpace(videoUrl))
                {
                    videoUrls.Add(videoUrl.Trim());
                }
            }

            product.ImageUrls = imageUrls;
            product.MarketplaceImageUrls = marketplaceImageUrls;
            product.VideoUrls = videoUrls;
        }

        // Helper method to process logo barcodes
        private void ProcessLogoBarcodes(Product product)
        {
            var logoBarcodes = new List<string>();
            
            // First try to get from hidden field (JSON format or comma-separated)
            var logoBarcodesValue = Request.Form["LogoBarcodes"].ToString();
            
            if (!string.IsNullOrWhiteSpace(logoBarcodesValue))
            {
                try
                {
                    // Try JSON format first (backward compatibility)
                    var jsonArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(logoBarcodesValue);
                    if (jsonArray != null)
                    {
                        logoBarcodes.AddRange(jsonArray.Where(s => !string.IsNullOrWhiteSpace(s)));
                    }
                }
                catch
                {
                    // Try comma-separated format
                    if (logoBarcodesValue.Contains(","))
                    {
                        var commaSeparated = logoBarcodesValue.Split(',')
                            .Where(item => !string.IsNullOrWhiteSpace(item))
                            .Select(item => item.Trim());
                        logoBarcodes.AddRange(commaSeparated);
                    }
                    else
                    {
                        // JSON değilse, satır satır ayır (backward compatibility)
                        var lines = logoBarcodesValue.Split('\n', '\r')
                            .Where(line => !string.IsNullOrWhiteSpace(line))
                            .Select(line => line.Trim());
                        logoBarcodes.AddRange(lines);
                    }
                }
            }
            
            // If no data from hidden field, try to get from individual inputs
            if (!logoBarcodes.Any())
            {
                for (int i = 1; i <= 10; i++)
                {
                    var barcodeValue = Request.Form[$"LogoBarcode_{i}"].ToString();
                    if (!string.IsNullOrWhiteSpace(barcodeValue))
                    {
                        var trimmedValue = barcodeValue.Trim();
                        if (IsValidLogoBarcodeFormat(trimmedValue))
                        {
                            logoBarcodes.Add(trimmedValue);
                        }
                    }
                }
            }

            // Store as comma-separated string (standardized format)
            if (logoBarcodes.Any())
            {
                product.LogoBarcodes = string.Join(",", logoBarcodes);
            }
            else
            {
                product.LogoBarcodes = string.Empty;
            }
        }

        private bool IsValidLogoBarcodeFormat(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return false;
                
            // Check if it matches the pattern: numbers separated by dots (e.g., 153.12.312.17)
            return System.Text.RegularExpressions.Regex.IsMatch(barcode, @"^[0-9]+(\.[0-9]+)*$");
        }

        // Helper method to process category
        private async Task ProcessCategoryAsync(Product product)
        {
            if (product.CategoryId.HasValue && product.CategoryId.Value > 0)
            {
                var category = await _categoryService.GetCategoryByIdAsync(product.CategoryId.Value);
                if (category != null)
                {
                    product.Category = category.Name;
                }
                else
                {
                    product.CategoryId = null;
                    product.Category = string.Empty;
                }
            }
            else if (!string.IsNullOrWhiteSpace(product.Category))
            {
                var existingCategory = await _categoryService.GetCategoryByNameAsync(product.Category);
                if (existingCategory != null)
                {
                    product.CategoryId = existingCategory.Id;
                }
            }
        }

        // Helper method to sanitize string fields
        private void SanitizeStringFields(Product product)
        {
            // Trim and clean string fields
            product.Name = product.Name?.Trim() ?? string.Empty;
            product.SKU = product.SKU?.Trim() ?? string.Empty;
            product.Brand = product.Brand?.Trim() ?? string.Empty;
            product.Description = product.Description?.Trim() ?? string.Empty;
            product.Features = product.Features?.Trim() ?? string.Empty;
            product.Notes = product.Notes?.Trim() ?? string.Empty;
            product.Material = product.Material?.Trim() ?? string.Empty;
            product.Color = product.Color?.Trim() ?? string.Empty;
            product.EanCode = product.EanCode?.Trim() ?? string.Empty;

            // Trim barcode fields
            product.TrendyolBarcode = product.TrendyolBarcode?.Trim() ?? string.Empty;
            product.HepsiburadaBarcode = product.HepsiburadaBarcode?.Trim() ?? string.Empty;
            product.KoctasBarcode = product.KoctasBarcode?.Trim() ?? string.Empty;
            product.KoctasIstanbulBarcode = product.KoctasIstanbulBarcode?.Trim() ?? string.Empty;
            product.HepsiburadaTedarikBarcode = product.HepsiburadaTedarikBarcode?.Trim() ?? string.Empty;
            product.PttAvmBarcode = product.PttAvmBarcode?.Trim() ?? string.Empty;
            product.PazaramaBarcode = product.PazaramaBarcode?.Trim() ?? string.Empty;
            product.HaceyapiBarcode = product.HaceyapiBarcode?.Trim() ?? string.Empty;
            product.AmazonBarcode = product.AmazonBarcode?.Trim() ?? string.Empty;
            product.HepsiburadaSellerStockCode = product.HepsiburadaSellerStockCode?.Trim() ?? string.Empty;
            product.N11CatalogId = product.N11CatalogId?.Trim() ?? string.Empty;
            product.N11ProductCode = product.N11ProductCode?.Trim() ?? string.Empty;

            // Trim spare barcodes
            product.SpareBarcode1 = product.SpareBarcode1?.Trim() ?? string.Empty;
            product.SpareBarcode2 = product.SpareBarcode2?.Trim() ?? string.Empty;
            product.SpareBarcode3 = product.SpareBarcode3?.Trim() ?? string.Empty;
            product.SpareBarcode4 = product.SpareBarcode4?.Trim() ?? string.Empty;
            
            // Trim Entegra barcodes
            product.EntegraUrunId = product.EntegraUrunId?.Trim() ?? string.Empty;
            product.EntegraUrunKodu = product.EntegraUrunKodu?.Trim() ?? string.Empty;
            product.EntegraBarkod = product.EntegraBarkod?.Trim() ?? string.Empty;

            // Trim special product features
            product.KlozetKanalYapisi = product.KlozetKanalYapisi?.Trim() ?? string.Empty;
            product.KlozetTipi = product.KlozetTipi?.Trim() ?? string.Empty;
            product.KlozetKapakCinsi = product.KlozetKapakCinsi?.Trim() ?? string.Empty;
            product.KlozetMontajTipi = product.KlozetMontajTipi?.Trim() ?? string.Empty;
            product.LawaboSuTasmaDeligi = product.LawaboSuTasmaDeligi?.Trim() ?? string.Empty;
            product.LawaboArmaturDeligi = product.LawaboArmaturDeligi?.Trim() ?? string.Empty;
            product.LawaboTipi = product.LawaboTipi?.Trim() ?? string.Empty;
            product.LawaboOzelligi = product.LawaboOzelligi?.Trim() ?? string.Empty;
            product.KabinTipi = product.KabinTipi?.Trim() ?? string.Empty;
        }

        #region Benzersizlik Kontrol API'leri

        [HttpPost]
        public async Task<IActionResult> CheckSkuUniqueness([FromBody] SkuCheckRequest request)
        {
            try
            {
                var isUnique = await _productService.IsSkuUniqueAsync(request.Sku, request.ExcludeProductId);
                return Json(new { isUnique = isUnique });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SKU benzersizlik kontrolü sırasında hata oluştu");
                return Json(new { isUnique = false, error = "Kontrol sırasında hata oluştu" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckEanUniqueness([FromBody] EanCheckRequest request)
        {
            try
            {
                var isUnique = await _productService.IsEanCodeUniqueAsync(request.EanCode, request.ExcludeProductId);
                return Json(new { isUnique = isUnique });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EAN benzersizlik kontrolü sırasında hata oluştu");
                return Json(new { isUnique = false, error = "Kontrol sırasında hata oluştu" });
            }
        }

        #endregion

        #region Bulk Operations

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkOperation([FromBody] BulkOperationRequest request)
        {
            try
            {
                if (request?.ProductIds == null || !request.ProductIds.Any())
                {
                    return Json(new { success = false, message = "Hiçbir ürün seçilmedi" });
                }

                if (request.ProductIds.Count > 500) // Batch size limit
                {
                    return Json(new { success = false, message = "Tek seferde en fazla 500 ürün işlenebilir" });
                }

                var stopwatch = Stopwatch.StartNew();
                var result = new { successCount = 0, failCount = 0 };

                switch (request.Action?.ToUpper())
                {
                    case "ARCHIVE":
                        result = await BulkArchiveProducts(request.ProductIds);
                        break;
                    case "UNARCHIVE":
                        result = await BulkUnarchiveProducts(request.ProductIds);
                        break;
                    case "DELETE":
                        result = await BulkDeleteProducts(request.ProductIds);
                        break;
                    default:
                        return Json(new { success = false, message = "Geçersiz işlem türü" });
                }

                stopwatch.Stop();
                _logger.LogInformation("Bulk operation {Action} completed in {ElapsedMs}ms. Success: {SuccessCount}, Failed: {FailCount}", 
                    request.Action, stopwatch.ElapsedMilliseconds, result.successCount, result.failCount);

                return Json(new { 
                    success = true, 
                    message = $"İşlem tamamlandı. Başarılı: {result.successCount}, Başarısız: {result.failCount}",
                    successCount = result.successCount,
                    failCount = result.failCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk operation failed for action {Action}", request?.Action);
                return Json(new { success = false, message = "İşlem sırasında bir hata oluştu" });
            }
        }

        private async Task<dynamic> BulkArchiveProducts(List<int> productIds)
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var productId in productIds)
            {
                try
                {
                    var product = await _productService.GetProductByIdAsync(productId);
                    if (product != null && !product.IsArchived)
                    {
                        product.IsArchived = true;
                        product.UpdatedDate = DateTime.UtcNow;
                        await _productService.UpdateProductAsync(product);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to archive product {ProductId}", productId);
                    failCount++;
                }
            }

            return new { successCount, failCount };
        }

        private async Task<dynamic> BulkUnarchiveProducts(List<int> productIds)
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var productId in productIds)
            {
                try
                {
                    var product = await _productService.GetProductByIdAsync(productId);
                    if (product != null && product.IsArchived)
                    {
                        product.IsArchived = false;
                        product.UpdatedDate = DateTime.UtcNow;
                        await _productService.UpdateProductAsync(product);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to unarchive product {ProductId}", productId);
                    failCount++;
                }
            }

            return new { successCount, failCount };
        }

        private async Task<dynamic> BulkDeleteProducts(List<int> productIds)
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var productId in productIds)
            {
                try
                {
                    await _productService.DeleteProductAsync(productId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete product {ProductId}", productId);
                    failCount++;
                }
            }

            return new { successCount, failCount };
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new List<object>());
                }

                // Get products matching the query for suggestions
                var suggestions = await _productService.SearchProductsAdvancedAsync(
                    searchTerm: query.Trim(), 
                    page: 1, 
                    pageSize: 10 // Limit to 10 suggestions
                );

                // Create distinct suggestions from product names and brands
                var suggestionItems = new List<object>();
                var addedSuggestions = new HashSet<string>();

                // Add product names that match
                foreach (var product in suggestions)
                {
                    if (!string.IsNullOrEmpty(product.Name))
                    {
                        var lowerName = product.Name.ToLower();
                        var lowerQuery = query.ToLower();
                        
                        if (lowerName.Contains(lowerQuery) && !addedSuggestions.Contains(lowerName))
                        {
                            suggestionItems.Add(new 
                            { 
                                text = product.Name,
                                type = "product",
                                highlight = GetHighlightedText(product.Name, query)
                            });
                            addedSuggestions.Add(lowerName);
                        }
                    }

                    // Add brands that match
                    if (!string.IsNullOrEmpty(product.Brand))
                    {
                        var lowerBrand = product.Brand.ToLower();
                        var lowerQuery = query.ToLower();
                        
                        if (lowerBrand.Contains(lowerQuery) && !addedSuggestions.Contains(lowerBrand))
                        {
                            suggestionItems.Add(new 
                            { 
                                text = product.Brand,
                                type = "brand",
                                highlight = GetHighlightedText(product.Brand, query)
                            });
                            addedSuggestions.Add(lowerBrand);
                        }
                    }

                    // Limit total suggestions
                    if (suggestionItems.Count >= 8) break;
                }

                return Json(suggestionItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating search suggestions for query: {Query}", query);
                return Json(new List<object>());
            }
        }

        private string GetHighlightedText(string text, string query)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return text;

            var pattern = Regex.Escape(query);
            return Regex.Replace(text, pattern, $"<mark>$0</mark>", RegexOptions.IgnoreCase);
        }

        #endregion

        #region Common Helper Methods for Product Form Operations

        /// <summary>
        /// Prepares ViewBag data required for product forms (Create/Edit)
        /// </summary>
        private async Task PrepareProductFormViewDataAsync()
        {
            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
        }

        /// <summary>
        /// Processes all form data for product (media URLs, barcodes, category, sanitization, etc.)
        /// </summary>
        private async Task ProcessProductFormDataAsync(Product product)
        {
            // Process form data
            ProcessMediaUrls(product);
            ProcessLogoBarcodes(product);
            
            // Process category
            await ProcessCategoryAsync(product);
            
            // Sanitize string fields
            SanitizeStringFields(product);
            
            // Handle numeric fields
            ProcessNumericFields(product);
        }

        /// <summary>
        /// Processes numeric fields to ensure they are not negative
        /// </summary>
        private void ProcessNumericFields(Product product)
        {
            if (product.Weight < 0) product.Weight = 0;
            if (product.Width < 0) product.Width = 0;
            if (product.Height < 0) product.Height = 0;
            if (product.Depth < 0) product.Depth = 0;
            
            // Handle Desi field
            var desiValue = Request.Form["Desi"].ToString();
            if (string.IsNullOrWhiteSpace(desiValue) || !decimal.TryParse(desiValue, out decimal parsedDesi) || parsedDesi <= 0)
            {
                product.Desi = 0;
            }
            else
            {
                product.Desi = parsedDesi;
            }
        }

        /// <summary>
        /// Clears validation errors for optional fields that should not be required
        /// </summary>
        private void ClearOptionalFieldValidationErrors()
        {
            var optionalFields = new[] { 
                "SKU", "Brand", "Description", "Features", "Notes", "Material", "Color", "EanCode",
                "AmazonBarcode", "HaceyapiBarcode", "HepsiburadaBarcode", "HepsiburadaSellerStockCode", 
                "HepsiburadaTedarikBarcode", "KoctasBarcode", "KoctasIstanbulBarcode", "N11CatalogId", 
                "N11ProductCode", "PazaramaBarcode", "PttAvmBarcode", "TrendyolBarcode",
                "KlozetKanalYapisi", "KlozetTipi", "KlozetKapakCinsi", "KlozetMontajTipi",
                "LawaboSuTasmaDeligi", "LawaboArmaturDeligi", "LawaboTipi", "LawaboOzelligi"
            };

            foreach (var field in optionalFields)
            {
                ModelState.Remove(field);
            }
        }

        /// <summary>
        /// Validates product uniqueness constraints (SKU, EAN Code)
        /// </summary>
        private async Task<ProductValidationResult> ValidateProductForSaveAsync(Product product, bool isEdit)
        {
            var result = new ProductValidationResult { IsValid = true, ValidationErrors = new List<string>() };

            // SKU uniqueness check
            if (!string.IsNullOrWhiteSpace(product.SKU))
            {
                var isSkuUnique = isEdit 
                    ? await _productService.IsSkuUniqueAsync(product.SKU, product.Id)
                    : await _productService.IsSkuUniqueAsync(product.SKU);
                
                if (!isSkuUnique)
                {
                    result.ValidationErrors.Add($"Üretici Ürün Kodu '{product.SKU}' zaten kullanılıyor. Lütfen farklı bir kod girin.");
                    ModelState.AddModelError("SKU", $"Bu Üretici Ürün Kodu zaten kullanılıyor: {product.SKU}");
                    result.IsValid = false;
                }
            }

            // EAN Code uniqueness check
            if (!string.IsNullOrWhiteSpace(product.EanCode))
            {
                var isEanUnique = isEdit 
                    ? await _productService.IsEanCodeUniqueAsync(product.EanCode, product.Id)
                    : await _productService.IsEanCodeUniqueAsync(product.EanCode);
                
                if (!isEanUnique)
                {
                    result.ValidationErrors.Add($"EAN Kodu '{product.EanCode}' zaten kullanılıyor. Lütfen farklı bir kod girin.");
                    ModelState.AddModelError("EanCode", $"Bu EAN Kodu zaten kullanılıyor: {product.EanCode}");
                    result.IsValid = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Handles exceptions in product form operations consistently
        /// </summary>
        private async Task<IActionResult> HandleProductFormExceptionAsync(Exception ex, string errorMessage, Product product)
        {
            await PrepareProductFormViewDataAsync();
            return await HandleFormExceptionAsync(ex, $"{errorMessage}: {ex.Message}", product ?? new Product());
        }

        /// <summary>
        /// Result object for product validation operations
        /// </summary>
        private class ProductValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> ValidationErrors { get; set; } = new List<string>();
        }

        #endregion

        #region Request Models

        public class SkuCheckRequest
        {
            public string Sku { get; set; } = string.Empty;
            public int? ExcludeProductId { get; set; }
        }

        public class EanCheckRequest
        {
            public string EanCode { get; set; } = string.Empty;
            public int? ExcludeProductId { get; set; }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Logo barkodları string'inden belirli index'teki değeri alır
        /// </summary>
        private string GetLogoBarcodeByIndex(string logoBarcodes, int index)
        {
            if (string.IsNullOrEmpty(logoBarcodes))
                return string.Empty;

            try
            {
                // Önce virgülle ayrılmış format (yeni standard)
                if (logoBarcodes.Contains(","))
                {
                    var barcodes = logoBarcodes.Split(',')
                        .Select(x => x.Trim())
                        .ToList();
                    return index < barcodes.Count ? barcodes[index] : string.Empty;
                }
                // JSON formatında ise parse et (backward compatibility)
                else if (logoBarcodes.Trim().StartsWith("["))
                {
                    var barcodeList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(logoBarcodes);
                    return barcodeList != null && index < barcodeList.Count ? barcodeList[index] : string.Empty;
                }
                else
                {
                    // Tek değer veya satır satır ayrılmış format
                    var lines = logoBarcodes.Split('\n', '\r')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .ToList();
                    return index < lines.Count ? lines[index] : string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion
    }
}