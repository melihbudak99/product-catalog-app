using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using product_catalog_app.src.models;
using product_catalog_app.src.interfaces;
using System.Globalization;

namespace product_catalog_app.src.services
{
    public class ImportService
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ImportService> _logger;

        public ImportService(IProductService productService, ICategoryService categoryService, ILogger<ImportService> logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _logger = logger;
        }

        #region XML Import
        /// <summary>
        /// XML dosyasından ürün import eder
        /// </summary>
        public async Task<ImportResult> ImportFromXmlAsync(Stream fileStream, ImportOptions options)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("XML import başlıyor...");
                
                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                var xmlContent = await reader.ReadToEndAsync();

                var serializer = new XmlSerializer(typeof(ProductCatalog));
                using var stringReader = new StringReader(xmlContent);
                var catalog = (ProductCatalog?)serializer.Deserialize(stringReader);

                if (catalog?.Products == null || !catalog.Products.Any())
                {
                    stopwatch.Stop();
                    return new ImportResult
                    {
                        Success = false,
                        ErrorMessage = "XML dosyasında ürün bulunamadı",
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                _logger.LogInformation("XML dosyasında {Count} ürün bulundu", catalog.Products.Count);

                var products = catalog.Products.Select(ConvertFromProductXml).ToList();
                var result = await ProcessImportedProductsAsync(products, options);
                
                result.ProcessingTime = stopwatch.Elapsed;
                result.BatchCount = (products.Count + options.BatchSize - 1) / options.BatchSize;

                _logger.LogInformation("XML import tamamlandı: {Count} ürün işlendi, süre: {Duration}ms", 
                    products.Count, stopwatch.ElapsedMilliseconds);
                    
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "XML import sırasında hata oluştu");
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = $"XML import hatası: {ex.Message}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }
        #endregion

        #region JSON Import
        /// <summary>
        /// JSON dosyasından ürün import eder
        /// </summary>
        public async Task<ImportResult> ImportFromJsonAsync(Stream fileStream, ImportOptions options)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("JSON import başlıyor...");
                
                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                var jsonContent = await reader.ReadToEndAsync();

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var products = JsonSerializer.Deserialize<List<Product>>(jsonContent, jsonOptions);

                if (products == null || !products.Any())
                {
                    stopwatch.Stop();
                    return new ImportResult
                    {
                        Success = false,
                        ErrorMessage = "JSON dosyasında ürün bulunamadı",
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                _logger.LogInformation("JSON dosyasında {Count} ürün bulundu", products.Count);

                var result = await ProcessImportedProductsAsync(products, options);
                
                result.ProcessingTime = stopwatch.Elapsed;
                result.BatchCount = (products.Count + options.BatchSize - 1) / options.BatchSize;

                _logger.LogInformation("JSON import tamamlandı: {Count} ürün işlendi, süre: {Duration}ms", 
                    products.Count, stopwatch.ElapsedMilliseconds);
                    
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "JSON import sırasında hata oluştu");
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = $"JSON import hatası: {ex.Message}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }
        #endregion

        #region CSV Import
        /// <summary>
        /// CSV dosyasından ürün import eder
        /// </summary>
        public async Task<ImportResult> ImportFromCsvAsync(Stream fileStream, ImportOptions options)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("CSV import başlıyor...");
                
                using var reader = new StreamReader(fileStream, Encoding.UTF8);
                var lines = new List<string>();
                
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }

                if (lines.Count < 2) // En az başlık + 1 veri satırı
                {
                    stopwatch.Stop();
                    return new ImportResult
                    {
                        Success = false,
                        ErrorMessage = "CSV dosyasında yeterli veri bulunamadı",
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                var products = new List<Product>();
                var errors = new List<string>();

                // İlk satır başlık
                var headers = ParseCsvLine(lines[0]);
                var totalDataRows = lines.Count - 1;
                
                _logger.LogInformation("CSV dosyasında {TotalRows} veri satırı bulundu, işleme başlanıyor...", totalDataRows);
                
                var progressInterval = Math.Max(1, totalDataRows / 20); // Her %5'te bir log
                
                // Veri satırlarını işle
                for (int i = 1; i < lines.Count; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        if (values.Count != headers.Count)
                        {
                            errors.Add($"Satır {i + 1}: Sütun sayısı uyumsuz");
                            continue;
                        }

                        var product = ParseCsvRowToProduct(headers, values);
                        if (product != null)
                        {
                            products.Add(product);
                        }
                        
                        // Progress logging
                        var processedRows = i;
                        if (processedRows % progressInterval == 0 || processedRows == totalDataRows)
                        {
                            var percentage = Math.Round((double)processedRows / totalDataRows * 100, 1);
                            _logger.LogInformation("CSV okuma ilerlemesi: {ProcessedRows}/{TotalRows} (%{Percentage}) - {ProductCount} ürün işlendi", 
                                processedRows, totalDataRows, percentage, products.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Satır {i + 1}: {ex.Message}");
                        _logger.LogWarning("CSV Satır {Row} işlenirken hata: {Error}", i + 1, ex.Message);
                    }
                }

                if (!products.Any())
                {
                    stopwatch.Stop();
                    return new ImportResult
                    {
                        Success = false,
                        ErrorMessage = "CSV dosyasında geçerli ürün bulunamadı",
                        Errors = errors,
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                var result = await ProcessImportedProductsAsync(products, options);
                result.Errors = errors;
                result.ProcessingTime = stopwatch.Elapsed;
                result.BatchCount = (products.Count + options.BatchSize - 1) / options.BatchSize;

                _logger.LogInformation("CSV import tamamlandı: {Count} ürün işlendi, {ErrorCount} hata, süre: {Duration}ms", 
                    products.Count, errors.Count, stopwatch.ElapsedMilliseconds);
                    
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "CSV import sırasında hata oluştu");
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = $"CSV import hatası: {ex.Message}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }
        #endregion

        #region Excel Import
        /// <summary>
        /// Excel dosyasından ürün import eder
        /// </summary>
        public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream, ImportOptions options)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Excel import başlıyor...");
                
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheets.First();
                
                var products = new List<Product>();
                var errors = new List<string>();

                // Başlık satırını al
                var headerRow = worksheet.Row(1);
                var headers = new List<string>();
                
                var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                for (int col = 1; col <= lastColumn; col++)
                {
                    headers.Add(headerRow.Cell(col).GetString());
                }

                // Header'ları debug için logla
                _logger.LogInformation("Excel'den okunan tüm header'lar ({Count} adet): {Headers}", 
                    headers.Count, string.Join(", ", headers));

                // Header'ları normalize et ve sadece unmapped olanları göster
                var unmappedHeaders = new List<string>();
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i].Trim();
                    var normalized = NormalizeHeaderName(header);
                    if (normalized == header.ToLower().Trim().Replace(" ", ""))
                    {
                        unmappedHeaders.Add($"'{header}' -> '{normalized}'");
                    }
                }
                
                if (unmappedHeaders.Any())
                {
                    _logger.LogWarning("Unmapped headers bulundu: {UnmappedHeaders}", string.Join(", ", unmappedHeaders));
                }

                // Arşiv sütunu olup olmadığını kontrol et
                var hasArchiveColumn = headers.Any(h => 
                {
                    var normalized = h.ToLower().Trim();
                    return normalized == "archived" || normalized == "arşivlenmiş" || 
                           normalized == "active" || normalized == "aktif" ||
                           normalized == "arşiv";
                });

                // Eğer arşiv sütunu yoksa PreserveArchiveStatus'u true yap
                if (!hasArchiveColumn)
                {
                    options.PreserveArchiveStatus = true;
                    _logger.LogInformation("Excel'de arşiv sütunu bulunamadı, mevcut arşiv durumu korunacak");
                }
                else
                {
                    options.PreserveArchiveStatus = false; // Arşiv sütunu varsa güncelle
                    _logger.LogInformation("Excel'de arşiv sütunu bulundu, arşiv durumu güncellenecek");
                }

                // Veri satırlarını işle
                var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
                var totalDataRows = lastRow - 1; // Başlık satırını çıkar
                
                _logger.LogInformation("Excel dosyasında {TotalRows} veri satırı bulundu, işleme başlanıyor...", totalDataRows);
                
                var processedRows = 0;
                var progressInterval = Math.Max(1, totalDataRows / 20); // Her %5'te bir log
                
                for (int row = 2; row <= lastRow; row++)
                {
                    try
                    {
                        var values = new List<string>();
                        for (int col = 1; col <= headers.Count; col++)
                        {
                            values.Add(worksheet.Cell(row, col).GetString());
                        }

                        // İlk 3 satır için detaylı debug
                        if (row <= 4)
                        {
                            _logger.LogInformation("EXCEL SATIR {Row} DEBUG:", row);
                            for (int i = 0; i < Math.Min(headers.Count, values.Count); i++)
                            {
                                if (!string.IsNullOrEmpty(values[i]))
                                {
                                    _logger.LogInformation("  {Header} = '{Value}'", headers[i], values[i]);
                                    
                                    // Pazaryeri görsel header'larını özel olarak işaretle
                                    var headerLower = headers[i].ToLower();
                                    if (headerLower.Contains("pazaryeri") && headerLower.Contains("görsel"))
                                    {
                                        _logger.LogWarning("  🔍 PAZARYERI GÖRSEL HEADER BULUNDU: '{Header}' = '{Value}'", headers[i], values[i]);
                                    }
                                    if (headerLower.Contains("marketplace") && headerLower.Contains("image"))
                                    {
                                        _logger.LogWarning("  🔍 MARKETPLACE IMAGE HEADER BULUNDU: '{Header}' = '{Value}'", headers[i], values[i]);
                                    }
                                }
                            }
                        }

                        var product = ParseExcelRowToProduct(headers, values);
                        if (product != null)
                        {
                            products.Add(product);
                        }
                        else
                        {
                            // Null döndü, neden olduğunu logla
                            if (row <= 4)
                            {
                                _logger.LogWarning("Satır {Row} null döndü - Name alanı boş olabilir", row);
                            }
                        }
                        
                        processedRows++;
                        
                        // Progress logging - her progress interval'da bir log
                        if (processedRows % progressInterval == 0 || processedRows == totalDataRows)
                        {
                            var percentage = Math.Round((double)processedRows / totalDataRows * 100, 1);
                            _logger.LogInformation("Excel okuma ilerlemesi: {ProcessedRows}/{TotalRows} (%{Percentage}) - {ProductCount} ürün işlendi", 
                                processedRows, totalDataRows, percentage, products.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Satır {row}: {ex.Message}");
                        _logger.LogWarning("Satır {Row} işlenirken hata: {Error}", row, ex.Message);
                    }
                }

                if (!products.Any())
                {
                    stopwatch.Stop();
                    _logger.LogError("HATA: Excel dosyasında hiç geçerli ürün bulunamadı!");
                    _logger.LogError("İşlenen satır sayısı: {ProcessedRows}", processedRows);
                    _logger.LogError("Toplam satır sayısı: {TotalRows}", totalDataRows);
                    _logger.LogError("Header sayısı: {HeaderCount}", headers.Count);
                    _logger.LogError("İlk 5 header: {FirstHeaders}", string.Join(", ", headers.Take(5)));
                    
                    return new ImportResult
                    {
                        Success = false,
                        ErrorMessage = "Excel dosyasında geçerli ürün bulunamadı",
                        Errors = errors,
                        ProcessingTime = stopwatch.Elapsed
                    };
                }

                var result = await ProcessImportedProductsAsync(products, options);
                result.Errors = errors;
                result.ProcessingTime = stopwatch.Elapsed;
                result.BatchCount = (products.Count + options.BatchSize - 1) / options.BatchSize;

                _logger.LogInformation("Excel import tamamlandı: {Count} ürün işlendi, {ErrorCount} hata, süre: {Duration}ms", 
                    products.Count, errors.Count, stopwatch.ElapsedMilliseconds);
                    
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Excel import sırasında hata oluştu: {Message}", ex.Message);
                
                // Hata tipine göre özel mesajlar
                string errorMessage = ex switch
                {
                    ArgumentException => $"Excel dosyası format hatası: {ex.Message}",
                    InvalidOperationException => $"Excel dosyası işleme hatası: {ex.Message}",
                    FileNotFoundException => "Excel dosyası bulunamadı",
                    UnauthorizedAccessException => "Excel dosyasına erişim izni yok",
                    OutOfMemoryException => "Excel dosyası çok büyük (hafıza yetersiz)",
                    _ => $"Excel import hatası: {ex.Message}"
                };
                
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    ProcessingTime = stopwatch.Elapsed
                };
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Import edilen ürünleri işle - 3000+ ürün için optimize edilmiş
        /// </summary>
        private async Task<ImportResult> ProcessImportedProductsAsync(List<Product> products, ImportOptions options)
        {
            var result = new ImportResult();
            var errors = new List<string>();

            _logger.LogInformation("Import işlemi başlıyor: {Count} ürün işlenecek", products.Count);

            // Performans optimizasyonu: Tüm ürünleri bir kez çek
            var allProducts = await _productService.GetAllProductsAsync();
            var productLookup = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
            var skuLookup = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
            var idLookup = new Dictionary<int, Product>();

            // Lookup tablolarını oluştur
            foreach (var existingProduct in allProducts)
            {
                if (existingProduct.Id > 0)
                    idLookup[existingProduct.Id] = existingProduct;
                
                if (!string.IsNullOrEmpty(existingProduct.SKU))
                    skuLookup[existingProduct.SKU] = existingProduct;
                
                if (!string.IsNullOrEmpty(existingProduct.Name))
                    productLookup[existingProduct.Name] = existingProduct;
            }

            // Batch processing için 100'lük gruplar halinde işle
            var batchSize = options.BatchSize;
            var batches = ChunkList(products, batchSize);
            
            _logger.LogInformation("Ürünler {BatchCount} batch halinde işlenecek (batch boyutu: {BatchSize})", 
                batches.Count, batchSize);

            for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
            {
                var batch = batches[batchIndex];
                var batchProgress = Math.Round((double)(batchIndex + 1) / batches.Count * 100, 1);
                _logger.LogInformation("Veritabanı işlemleri - Batch {Current}/{Total} işleniyor... (%{Progress})", 
                    batchIndex + 1, batches.Count, batchProgress);

                foreach (var product in batch)
                {
                    try
                    {
                        // Kategorileri otomatik oluştur ve CategoryId'yi ata
                        if (!string.IsNullOrEmpty(product.Category))
                        {
                            var categoryId = await _categoryService.GetOrCreateCategoryIdAsync(product.Category);
                            product.CategoryId = categoryId;
                            
                            // Sadece ilk batch'te kategori loglarını göster
                            if (batchIndex == 0)
                            {
                                _logger.LogInformation("Ürün '{ProductName}' için kategori '{Category}' oluşturuldu/bulundu (ID: {CategoryId})", 
                                    product.Name, product.Category, categoryId);
                            }
                        }
                        else
                        {
                            if (batchIndex == 0) // Sadece ilk batch'te uyarı ver
                            {
                                _logger.LogWarning("Ürün '{ProductName}' için kategori bilgisi boş!", product.Name ?? "Bilinmeyen");
                            }
                        }

                        // Mevcut ürünü bul (ID, SKU veya isim ile)
                        Product? existingProduct = null;
                        
                        // Önce ID ile ara
                        if (product.Id > 0 && idLookup.TryGetValue(product.Id, out existingProduct))
                        {
                            // ID ile bulundu
                        }
                        // ID ile bulunamazsa SKU ile ara
                        else if (!string.IsNullOrEmpty(product.SKU) && skuLookup.TryGetValue(product.SKU, out existingProduct))
                        {
                            // SKU ile bulundu
                        }
                        // SKU ile de bulunamazsa tam isim ile ara
                        else if (!string.IsNullOrEmpty(product.Name) && productLookup.TryGetValue(product.Name, out existingProduct))
                        {
                            // İsim ile bulundu
                        }

                        // Ürün güncelleme veya ekleme
                        if (existingProduct != null && options.UpdateExisting)
                        {
                            // KIŞISEL GÜNCELLEME: Sadece Excel'de gelen alanları güncelle
                            var updatedProduct = await MergeProductUpdatesAsync(existingProduct, product, options);
                            
                            await _productService.UpdateProductAsync(updatedProduct);
                            result.UpdatedCount++;
                            
                            // Her 100 güncellemeyi ve her batch'in sonunu logla
                            if (result.UpdatedCount % 100 == 0 || batchIndex == 0)
                            {
                                _logger.LogInformation("Ürün güncellendi: ID={Id}, Name={Name}, IsArchived={IsArchived}", 
                                    updatedProduct.Id, updatedProduct.Name, updatedProduct.IsArchived);
                            }

                            // Lookup'ları güncelle
                            idLookup[updatedProduct.Id] = updatedProduct;
                            if (!string.IsNullOrEmpty(updatedProduct.SKU))
                                skuLookup[updatedProduct.SKU] = updatedProduct;
                            if (!string.IsNullOrEmpty(updatedProduct.Name))
                                productLookup[updatedProduct.Name] = updatedProduct;
                        }
                        else if (existingProduct == null)
                        {
                            // Yeni ürün ekle
                            product.Id = 0;
                            product.CreatedDate = DateTime.Now;
                            product.UpdatedDate = DateTime.Now;
                            
                            await _productService.AddProductAsync(product);
                            result.InsertedCount++;
                            
                            if (batchIndex == 0 || result.InsertedCount % 50 == 0) // Her 50 eklemeyi logla
                            {
                                _logger.LogInformation("Yeni ürün eklendi: Name={Name}", product.Name);
                            }

                            // Lookup'lara ekle (ID database'den gelir)
                            if (product.Id > 0)
                            {
                                idLookup[product.Id] = product;
                                if (!string.IsNullOrEmpty(product.SKU))
                                    skuLookup[product.SKU] = product;
                                if (!string.IsNullOrEmpty(product.Name))
                                    productLookup[product.Name] = product;
                            }
                        }
                        else
                        {
                            // Ürün mevcut ama güncelleme aktif değil
                            errors.Add($"Ürün '{product.Name}' zaten mevcut (güncelleme devre dışı)");
                            result.ErrorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Ürün '{product.Name}': {ex.Message}");
                        result.ErrorCount++;
                        _logger.LogError(ex, "Ürün işlenirken hata: {ProductName}", product.Name);
                    }
                }
                
                // Her batch sonunda kısa bir bekleme
                if (batchIndex < batches.Count - 1)
                {
                    await Task.Delay(10); // 10ms bekleme
                }
            }

            result.Success = result.InsertedCount > 0 || result.UpdatedCount > 0;
            result.TotalProcessed = products.Count;
            result.Errors = errors;

            _logger.LogInformation("Import tamamlandı: {Total} işlendi, {Inserted} yeni, {Updated} güncellendi, {Errors} hata", 
                result.TotalProcessed, result.InsertedCount, result.UpdatedCount, result.ErrorCount);

            return result;
        }

        private Product ConvertFromProductXml(ProductXml xmlProduct)
        {
            // XML'den seçici veri çıkarımı - sadece dolu alanları işle
            var product = new Product
            {
                Id = 0, // Yeni ürün olarak ekle (ID hariç diğer alanlar seçici)
                ImageUrls = new List<string>(),
                MarketplaceImageUrls = new List<string>(),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            // Sadece XML'de dolu olan alanları set et
            if (!string.IsNullOrWhiteSpace(xmlProduct.Name))
                product.Name = xmlProduct.Name;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.Description))
                product.Description = xmlProduct.Description;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.Category))
                product.Category = xmlProduct.Category;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.Brand))
                product.Brand = xmlProduct.Brand;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.SKU))
                product.SKU = xmlProduct.SKU;

            // Numeric alanlar - sadece 0'dan farklı değerler
            if (xmlProduct.Weight > 0)
                product.Weight = xmlProduct.Weight;
                
            if (xmlProduct.Desi > 0)
                product.Desi = xmlProduct.Desi;
                
            if (xmlProduct.Width > 0)
                product.Width = xmlProduct.Width;
                
            if (xmlProduct.Height > 0)
                product.Height = xmlProduct.Height;
                
            if (xmlProduct.Depth > 0)
                product.Depth = xmlProduct.Depth;
                
            if (xmlProduct.WarrantyMonths > 0)
                product.WarrantyMonths = xmlProduct.WarrantyMonths;

            // String alanlar
            if (!string.IsNullOrWhiteSpace(xmlProduct.Material))
                product.Material = xmlProduct.Material;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.Color))
                product.Color = xmlProduct.Color;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.EanCode))
                product.EanCode = xmlProduct.EanCode;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.Features))
                product.Features = xmlProduct.Features;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.Notes))
                product.Notes = xmlProduct.Notes;

            // Pazaryeri barkodları
            if (!string.IsNullOrWhiteSpace(xmlProduct.TrendyolBarcode))
                product.TrendyolBarcode = xmlProduct.TrendyolBarcode;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.HepsiburadaBarcode))
                product.HepsiburadaBarcode = xmlProduct.HepsiburadaBarcode;
                
            if (!string.IsNullOrWhiteSpace(xmlProduct.AmazonBarcode))
                product.AmazonBarcode = xmlProduct.AmazonBarcode;

            // Arşiv durumu - XML'de belirtilmiş ise kullan
            product.IsArchived = xmlProduct.IsArchived;

            // Image URL'lerini seçici olarak işle
            var imageFields = new[] {
                xmlProduct.ImageUrl1, xmlProduct.ImageUrl2, xmlProduct.ImageUrl3,
                xmlProduct.ImageUrl4, xmlProduct.ImageUrl5, xmlProduct.ImageUrl6,
                xmlProduct.ImageUrl7, xmlProduct.ImageUrl8, xmlProduct.ImageUrl9,
                xmlProduct.ImageUrl10
            };

            foreach (var imageUrl in imageFields)
            {
                if (!string.IsNullOrWhiteSpace(imageUrl))
                    product.ImageUrls.Add(imageUrl);
            }

            // İlk görsel URL'yi ana görsel olarak ayarla (eğer görsel varsa)
            if (product.ImageUrls.Any())
                product.ImageUrl = product.ImageUrls.FirstOrDefault() ?? string.Empty;

            return product;
        }

        private List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Çift tırnak escape
                        currentValue.Append('"');
                        i++; // Bir sonraki karakteri atla
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }

        private Product? ParseCsvRowToProduct(List<string> headers, List<string> values)
        {
            var product = new Product
            {
                ImageUrls = new List<string>(),
                MarketplaceImageUrls = new List<string>(),
                VideoUrls = new List<string>() // VideoUrls listesini de initialize et
            };

            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                var header = headers[i].Trim();
                var value = values[i].Trim();

                try
                {
                    switch (header.ToLower())
                    {
                        case "id":
                            if (int.TryParse(value, out int id))
                                product.Id = id;
                            break;
                        case "ürün adı":
                        case "name":
                            product.Name = value;
                            break;
                        case "sku":
                            product.SKU = value;
                            break;
                        case "marka":
                        case "brand":
                            product.Brand = value;
                            break;
                        case "kategori":
                        case "category":
                            product.Category = value;
                            break;
                        case "açıklama":
                        case "description":
                            product.Description = value;
                            break;
                        case "ağırlık":
                        case "weight":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal weight))
                                product.Weight = weight;
                            break;
                        case "desi":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal desi))
                                product.Desi = desi;
                            break;
                        case "genişlik":
                        case "width":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal width))
                                product.Width = width;
                            break;
                        case "yükseklik":
                        case "height":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal height))
                                product.Height = height;
                            break;
                        case "derinlik":
                        case "en":
                        case "depth":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal depth))
                                product.Depth = depth;
                            break;
                        case "garanti ayı":
                        case "warranty":
                            if (int.TryParse(value, out int warranty))
                                product.WarrantyMonths = warranty;
                            break;
                        case "malzeme":
                        case "material":
                            product.Material = value;
                            break;
                        case "renk":
                        case "color":
                            product.Color = value;
                            break;
                        case "ean kodu":
                        case "eancode":
                            product.EanCode = value;
                            break;
                        case "özellikler":
                        case "features":
                            product.Features = value;
                            break;
                        case "notlar":
                        case "notes":
                            product.Notes = value;
                            break;
                        case "ana görsel":
                        case "imageurl":
                            product.ImageUrl = value;
                            if (!string.IsNullOrEmpty(value))
                                product.ImageUrls.Add(value);
                            break;
                        case "arşiv":
                        case "archived":
                            product.IsArchived = value.ToLower() == "true" || value.ToLower() == "evet" || value == "1";
                            break;
                        case "trendyol barkod":
                        case "trendyolbarcode":
                            product.TrendyolBarcode = value;
                            break;
                        case "hepsiburada barkod":
                        case "hepsiburadabarcode":
                            product.HepsiburadaBarcode = value;
                            break;
                        case "amazon barkod":
                        case "amazonbarcode":
                            product.AmazonBarcode = value;
                            break;
                        // Koçtaş barkodları
                        case "koçtaş barkod":
                        case "koctasbarcode":
                            product.KoctasBarcode = value;
                            break;
                        case "koçtaş istanbul barkod":
                        case "koctasistanbulbarcode":
                            product.KoctasIstanbulBarcode = value;
                            break;
                        // Hepsiburada ek barkodları
                        case "hepsiburada tedarik barkod":
                        case "hepsiburadatedarikbarcode":
                            product.HepsiburadaTedarikBarcode = value;
                            break;
                        case "hepsiburada seller stock code":
                        case "hepsiburadasekkerstockcode":
                            product.HepsiburadaSellerStockCode = value;
                            break;
                        // PTT AVM
                        case "ptt avm barkod":
                        case "pttavmbarcode":
                            product.PttAvmBarcode = value;
                            break;
                        // Pazarama
                        case "pazarama barkod":
                        case "pazaramabarcode":
                            product.PazaramaBarcode = value;
                            break;
                        // Haceyapı
                        case "haceyapı barkod":
                        case "haceyapibarcode":
                            product.HaceyapiBarcode = value;
                            break;
                        // N11
                        case "n11 catalog id":
                        case "n11catalogid":
                            product.N11CatalogId = value;
                            break;
                        case "n11 product code":
                        case "n11productcode":
                            product.N11ProductCode = value;
                            break;
                        
                        // Entegra barkodları
                        case "entegra ürün id":
                        case "entegraurunid":
                            product.EntegraUrunId = value;
                            break;
                        case "entegra ürün kodu":
                        case "entegraurunkodu":
                            product.EntegraUrunKodu = value;
                            break;
                        case "entegra barkod":
                        case "entegrabarkod":
                            product.EntegraBarkod = value;
                            break;
                            
                        // Yedek barkodlar
                        case "spare barcode 1":
                        case "sparebarcode1":
                            product.SpareBarcode1 = value;
                            break;
                        case "spare barcode 2":
                        case "sparebarcode2":
                            product.SpareBarcode2 = value;
                            break;
                        case "spare barcode 3":
                        case "sparebarcode3":
                            product.SpareBarcode3 = value;
                            break;
                        case "spare barcode 4":
                        case "sparebarcode4":
                            product.SpareBarcode4 = value;
                            break;
                        // Logo barkodları
                        case "logo barkodları":
                        case "logobarcodes":
                            product.LogoBarcodes = value;
                            break;
                        // Klozet özellikleri
                        case "klozet kanal yapısı":
                        case "klozetkanlyapisi":
                            product.KlozetKanalYapisi = value;
                            break;
                        case "klozet tipi":
                        case "klozettipi":
                            product.KlozetTipi = value;
                            break;
                        case "klozet kapak cinsi":
                        case "klozetkapakcinsi":
                            product.KlozetKapakCinsi = value;
                            break;
                        case "klozetmontajtipi":
                            product.KlozetMontajTipi = value;
                            break;
                        // Lavabo özellikleri
                        case "lawabosuasmaseligu":
                            product.LawaboSuTasmaDeligi = value;
                            break;
                        case "lawaboarmaturdeligi":
                            product.LawaboArmaturDeligi = value;
                            break;
                        case "lawabotipi":
                            product.LawaboTipi = value;
                            break;
                        case "lawaboozellix":
                            product.LawaboOzelligi = value;
                            break;
                        // Batarya özellikleri
                        case "bataryacikisucuuzunlugu":
                            product.BataryaCikisUcuUzunlugu = value;
                            break;
                        case "bataryayuksekligi":
                            product.BataryaYuksekligi = value;
                            break;
                        // Uzunluk alanı da ekleyelim
                        case "uzunluk":
                        case "length":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal length))
                                product.Length = length;
                            break;
                        
                        // Logo barkodları - ayrı indeksli sütunlar (CSV için)
                        case "logo barkodu 1":
                            SetLogoBarcodeByIndex(product, 0, value);
                            break;
                        case "logo barkodu 2":
                            SetLogoBarcodeByIndex(product, 1, value);
                            break;
                        case "logo barkodu 3":
                            SetLogoBarcodeByIndex(product, 2, value);
                            break;
                        case "logo barkodu 4":
                            SetLogoBarcodeByIndex(product, 3, value);
                            break;
                        case "logo barkodu 5":
                            SetLogoBarcodeByIndex(product, 4, value);
                            break;
                        case "logo barkodu 6":
                            SetLogoBarcodeByIndex(product, 5, value);
                            break;
                        case "logo barkodu 7":
                            SetLogoBarcodeByIndex(product, 6, value);
                            break;
                        case "logo barkodu 8":
                            SetLogoBarcodeByIndex(product, 7, value);
                            break;
                        case "logo barkodu 9":
                            SetLogoBarcodeByIndex(product, 8, value);
                            break;
                        case "logo barkodu 10":
                            SetLogoBarcodeByIndex(product, 9, value);
                            break;
                            
                        // Ürün görselleri - ayrı indeksli sütunlar (CSV için)
                        case "ürün görseli 1":
                            SetImageUrlByIndex(product.ImageUrls, 0, value);
                            break;
                        case "ürün görseli 2":
                            SetImageUrlByIndex(product.ImageUrls, 1, value);
                            break;
                        case "ürün görseli 3":
                            SetImageUrlByIndex(product.ImageUrls, 2, value);
                            break;
                        case "ürün görseli 4":
                            SetImageUrlByIndex(product.ImageUrls, 3, value);
                            break;
                        case "ürün görseli 5":
                            SetImageUrlByIndex(product.ImageUrls, 4, value);
                            break;
                        case "ürün görseli 6":
                            SetImageUrlByIndex(product.ImageUrls, 5, value);
                            break;
                        case "ürün görseli 7":
                            SetImageUrlByIndex(product.ImageUrls, 6, value);
                            break;
                        case "ürün görseli 8":
                            SetImageUrlByIndex(product.ImageUrls, 7, value);
                            break;
                        case "ürün görseli 9":
                            SetImageUrlByIndex(product.ImageUrls, 8, value);
                            break;
                        case "ürün görseli 10":
                            SetImageUrlByIndex(product.ImageUrls, 9, value);
                            break;
                            
                        // Pazaryeri görselleri - ayrı indeksli sütunlar (CSV için)
                        case "pazaryeri görseli 1":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 0, value);
                            break;
                        case "pazaryeri görseli 2":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 1, value);
                            break;
                        case "pazaryeri görseli 3":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 2, value);
                            break;
                        case "pazaryeri görseli 4":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 3, value);
                            break;
                        case "pazaryeri görseli 5":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 4, value);
                            break;
                        case "pazaryeri görseli 6":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 5, value);
                            break;
                        case "pazaryeri görseli 7":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 6, value);
                            break;
                        case "pazaryeri görseli 8":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 7, value);
                            break;
                        case "pazaryeri görseli 9":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 8, value);
                            break;
                        case "pazaryeri görseli 10":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 9, value);
                            break;
                            
                        // Video URL'leri (CSV için)
                        case "video 1":
                            SetImageUrlByIndex(product.VideoUrls, 0, value);
                            break;
                        case "video 2":
                            SetImageUrlByIndex(product.VideoUrls, 1, value);
                            break;
                        case "video 3":
                            SetImageUrlByIndex(product.VideoUrls, 2, value);
                            break;
                        case "video 4":
                            SetImageUrlByIndex(product.VideoUrls, 3, value);
                            break;
                        case "video 5":
                            SetImageUrlByIndex(product.VideoUrls, 4, value);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Sütun '{Header}' işlenirken hata: {Error}", header, ex.Message);
                }
            }

            // Zorunlu alanları kontrol et
            if (string.IsNullOrEmpty(product.Name))
                return null;

            product.CreatedDate = DateTime.Now;
            product.UpdatedDate = DateTime.Now;

            return product;
        }

        private Product? ParseExcelRowToProduct(List<string> headers, List<string> values)
        {
            var product = new Product
            {
                ImageUrls = new List<string>(),
                MarketplaceImageUrls = new List<string>(),
                VideoUrls = new List<string>(), // VideoUrls listesini de initialize et
                LogoBarcodes = "" // Başlangıçta boş string
            };

            // Excel'de hangi sütunların mevcut olduğunu takip et
            var availableColumns = new HashSet<string>();
            var processedValues = new Dictionary<string, string>();
            var skippedColumns = new List<string>();
            
            // Açıklama sütunları için özel işleme
            string? htmlDescription = null;
            string? plainDescription = null;

            _logger.LogDebug("Excel satırı işleniyor: {HeaderCount} başlık, {ValueCount} değer", headers.Count, values.Count);

            // İlk geçiş: HTML ve düz metin açıklama sütunlarını tespit et
            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                var header = headers[i].Trim();
                var value = values[i].Trim();
                var normalizedHeader = NormalizeHeaderName(header);

                if (normalizedHeader == "description")
                {
                    var lowerHeader = header.ToLower();
                    if (lowerHeader.Contains("html"))
                    {
                        htmlDescription = value;
                        _logger.LogInformation("HTML açıklama sütunu bulundu: '{Header}' = '{Value}'", header, value);
                    }
                    else if (lowerHeader.Contains("düz metin") || lowerHeader.Contains("plain"))
                    {
                        plainDescription = value;
                        _logger.LogInformation("Düz metin açıklama sütunu bulundu: '{Header}' = '{Value}'", header, value);
                    }
                    else if (string.IsNullOrEmpty(htmlDescription) && string.IsNullOrEmpty(plainDescription))
                    {
                        // Genel açıklama sütunu, HTML/düz metin özelliği yok
                        plainDescription = value;
                    }
                }
            }

            // HTML açıklama varsa onu kullan, yoksa düz metin açıklamayı kullan
            if (!string.IsNullOrEmpty(htmlDescription))
            {
                product.Description = htmlDescription;
                _logger.LogInformation("Ürün açıklaması HTML formatından alındı: '{Description}'", htmlDescription);
            }
            else if (!string.IsNullOrEmpty(plainDescription))
            {
                product.Description = plainDescription;
                _logger.LogInformation("Ürün açıklaması düz metin formatından alındı: '{Description}'", plainDescription);
            }

            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                var header = headers[i].Trim();
                var value = values[i].Trim();
                
                availableColumns.Add(header.ToLower());
                if (!string.IsNullOrEmpty(value))
                {
                    processedValues[header] = value;
                }
                
                // Normalize header for better matching
                var normalizedHeader = NormalizeHeaderName(header);
                
                _logger.LogDebug("Header processing: '{OriginalHeader}' -> '{NormalizedHeader}' = '{Value}'", 
                    header, normalizedHeader, value);

                try
                {
                    var fieldProcessed = false;
                    
                    switch (normalizedHeader)
                    {
                        case "id":
                            if (int.TryParse(value, out int id))
                            {
                                product.Id = id;
                                fieldProcessed = true;
                            }
                            break;
                        case "name":
                            product.Name = value;
                            fieldProcessed = true;
                            _logger.LogDebug("Name field set: '{Value}' from header '{OriginalHeader}'", value, header);
                            break;
                        case "sku":
                            product.SKU = value;
                            fieldProcessed = true;
                            break;
                        case "brand":
                            product.Brand = value;
                            fieldProcessed = true;
                            break;
                        case "category":
                            product.Category = value;
                            fieldProcessed = true;
                            _logger.LogDebug("Excel'den kategori okundu: '{Category}' (Ürün: {ProductName})", value, product.Name ?? "Bilinmeyen");
                            break;
                        case "description":
                            // Açıklama alanı yukarıda önceden işlenmiş, burada tekrar işleme
                            fieldProcessed = true;
                            break;
                        case "weight":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal weight))
                            {
                                product.Weight = weight;
                                fieldProcessed = true;
                            }
                            break;
                        case "desi":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal desi))
                            {
                                product.Desi = desi;
                                fieldProcessed = true;
                            }
                            break;
                        case "width":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal width))
                            {
                                product.Width = width;
                                fieldProcessed = true;
                            }
                            break;
                        case "height":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal height))
                            {
                                product.Height = height;
                                fieldProcessed = true;
                            }
                            break;
                        case "depth":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal depth))
                            {
                                product.Depth = depth;
                                fieldProcessed = true;
                            }
                            break;
                        case "length":
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal length))
                            {
                                product.Length = length;
                                fieldProcessed = true;
                            }
                            break;
                        case "warrantymonths":
                            if (int.TryParse(value, out int warranty))
                            {
                                product.WarrantyMonths = warranty;
                                fieldProcessed = true;
                            }
                            break;
                        case "material":
                            product.Material = value;
                            fieldProcessed = true;
                            break;
                        case "color":
                            product.Color = value;
                            fieldProcessed = true;
                            break;
                        case "eancode":
                            product.EanCode = value;
                            fieldProcessed = true;
                            break;
                        case "features":
                            product.Features = value;
                            fieldProcessed = true;
                            break;
                        case "notes":
                            product.Notes = value;
                            fieldProcessed = true;
                            break;
                        case "imageurl":
                            product.ImageUrl = value;
                            if (!string.IsNullOrEmpty(value))
                                product.ImageUrls.Add(value);
                            fieldProcessed = true;
                            break;
                        case "active":
                            // Aktif alanı işle - ters çevir çünkü IsArchived kullanıyoruz
                            var lowerActiveValue = value.ToLower().Trim();
                            var isActive = lowerActiveValue == "true" || lowerActiveValue == "aktif" || lowerActiveValue == "1" ||
                                         lowerActiveValue == "evet" || lowerActiveValue == "yes";
                            product.IsArchived = !isActive;
                            fieldProcessed = true;
                            _logger.LogDebug("Excel'den aktif durumu okundu: '{Value}' -> IsActive={IsActive}, IsArchived={IsArchived} (Ürün: {ProductName})", 
                                value, isActive, product.IsArchived, product.Name ?? "Bilinmeyen");
                            break;
                        case "archived":
                            var lowerValue = value.ToLower().Trim();
                            product.IsArchived = lowerValue == "true" || lowerValue == "evet" || lowerValue == "arşiv" || 
                                               lowerValue == "arsiv" || lowerValue == "yes" || lowerValue == "1" ||
                                               lowerValue == "arşivlenmiş" || lowerValue == "arşivlendi";
                            fieldProcessed = true;
                            _logger.LogDebug("Excel'den arşiv durumu okundu: '{Value}' -> IsArchived={IsArchived} (Ürün: {ProductName})", 
                                value, product.IsArchived, product.Name ?? "Bilinmeyen");
                            break;
                        case "createddate":
                            if (DateTime.TryParse(value, out DateTime createdDate))
                            {
                                product.CreatedDate = createdDate;
                                fieldProcessed = true;
                            }
                            break;
                        case "updateddate":
                            if (DateTime.TryParse(value, out DateTime updatedDate))
                            {
                                product.UpdatedDate = updatedDate;
                                fieldProcessed = true;
                            }
                            break;
                        // Tüm barkod alanları
                        case "trendyolbarcode":
                            product.TrendyolBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "hepsiburadabarcode":
                            product.HepsiburadaBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "amazonbarcode":
                            product.AmazonBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "koctasbarcode":
                            product.KoctasBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "koctasistanbulbarcode":
                            product.KoctasIstanbulBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "koctaseanbarcode":
                            product.KoctasEanBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "koctaseanistanbulbarcode":
                            product.KoctasEanIstanbulBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "hepsiburadatedarikbarcode":
                            product.HepsiburadaTedarikBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "hepsiburadasekkerstockcode":
                            product.HepsiburadaSellerStockCode = value;
                            fieldProcessed = true;
                            _logger.LogDebug("Hepsiburada Seller Stock Code set: '{Value}'", value);
                            break;
                        case "pttavmbarcode":
                            product.PttAvmBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "ptturunstokodu":
                            product.PttUrunStokKodu = value;
                            fieldProcessed = true;
                            break;
                        case "pazaramabarcode":
                            product.PazaramaBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "haceyapibarcode":
                            product.HaceyapiBarcode = value;
                            fieldProcessed = true;
                            break;
                        case "n11catalogid":
                            product.N11CatalogId = value;
                            fieldProcessed = true;
                            break;
                        case "n11productcode":
                            product.N11ProductCode = value;
                            fieldProcessed = true;
                            break;
                            
                        // Entegra barkodları
                        case "entegraurunid":
                            product.EntegraUrunId = value;
                            fieldProcessed = true;
                            break;
                        case "entegraurunkodu":
                            product.EntegraUrunKodu = value;
                            fieldProcessed = true;
                            break;
                        case "entegrabarkod":
                            product.EntegraBarkod = value;
                            fieldProcessed = true;
                            break;
                            
                        case "sparebarcode1":
                            product.SpareBarcode1 = value;
                            fieldProcessed = true;
                            break;
                        case "sparebarcode2":
                            product.SpareBarcode2 = value;
                            fieldProcessed = true;
                            break;
                        case "sparebarcode3":
                            product.SpareBarcode3 = value;
                            fieldProcessed = true;
                            break;
                        case "sparebarcode4":
                            product.SpareBarcode4 = value;
                            fieldProcessed = true;
                            break;
                        case "logobarcodes":
                            product.LogoBarcodes = value;
                            fieldProcessed = true;
                            break;
                        // Logo barkodları - ayrı sütunlar (Excel için)
                        case "logobarcode1":
                            SetLogoBarcodeByIndex(product, 0, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode2":
                            SetLogoBarcodeByIndex(product, 1, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode3":
                            SetLogoBarcodeByIndex(product, 2, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode4":
                            SetLogoBarcodeByIndex(product, 3, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode5":
                            SetLogoBarcodeByIndex(product, 4, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode6":
                            SetLogoBarcodeByIndex(product, 5, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode7":
                            SetLogoBarcodeByIndex(product, 6, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode8":
                            SetLogoBarcodeByIndex(product, 7, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode9":
                            SetLogoBarcodeByIndex(product, 8, value);
                            fieldProcessed = true;
                            break;
                        case "logobarcode10":
                            SetLogoBarcodeByIndex(product, 9, value);
                            fieldProcessed = true;
                            break;
                        // Klozet özellikleri
                        case "klozetkanlyapisi":
                            product.KlozetKanalYapisi = value;
                            fieldProcessed = true;
                            break;
                        case "klozettipi":
                            product.KlozetTipi = value;
                            fieldProcessed = true;
                            break;
                        case "klozetkapakcinsi":
                            product.KlozetKapakCinsi = value;
                            fieldProcessed = true;
                            break;
                        case "klozetmontajtipi":
                            product.KlozetMontajTipi = value;
                            fieldProcessed = true;
                            break;
                        // Lavabo özellikleri
                        case "lawabosuasmaseligu":
                            product.LawaboSuTasmaDeligi = value;
                            fieldProcessed = true;
                            break;
                        case "lawaboarmaturdeligi":
                            product.LawaboArmaturDeligi = value;
                            fieldProcessed = true;
                            break;
                        case "lawabotipi":
                            product.LawaboTipi = value;
                            fieldProcessed = true;
                            break;
                        case "lawaboozellix":
                            product.LawaboOzelligi = value;
                            fieldProcessed = true;
                            break;
                        // Batarya özellikleri
                        case "bataryacikisucuuzunlugu":
                            product.BataryaCikisUcuUzunlugu = value;
                            fieldProcessed = true;
                            break;
                        case "bataryayuksekligi":
                            product.BataryaYuksekligi = value;
                            fieldProcessed = true;
                            break;
                        
                        // Ürün görselleri - ayrı indeksli sütunlar
                        case "productimage1":
                            SetImageUrlByIndex(product.ImageUrls, 0, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Product Image 1 set: '{Value}'", value);
                            break;
                        case "productimage2":
                            SetImageUrlByIndex(product.ImageUrls, 1, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Product Image 2 set: '{Value}'", value);
                            break;
                        case "productimage3":
                            SetImageUrlByIndex(product.ImageUrls, 2, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Product Image 3 set: '{Value}'", value);
                            break;
                        case "productimage4":
                            SetImageUrlByIndex(product.ImageUrls, 3, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Product Image 4 set: '{Value}'", value);
                            break;
                        case "productimage5":
                            SetImageUrlByIndex(product.ImageUrls, 4, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Product Image 5 set: '{Value}'", value);
                            break;
                        case "productimage6":
                            SetImageUrlByIndex(product.ImageUrls, 5, value);
                            fieldProcessed = true;
                            break;
                        case "productimage7":
                            SetImageUrlByIndex(product.ImageUrls, 6, value);
                            fieldProcessed = true;
                            break;
                        case "productimage8":
                            SetImageUrlByIndex(product.ImageUrls, 7, value);
                            fieldProcessed = true;
                            break;
                        case "productimage9":
                            SetImageUrlByIndex(product.ImageUrls, 8, value);
                            fieldProcessed = true;
                            break;
                        case "productimage10":
                            SetImageUrlByIndex(product.ImageUrls, 9, value);
                            fieldProcessed = true;
                            break;
                            
                        // Pazaryeri görselleri - ayrı indeksli sütunlar
                        case "marketplaceimage1":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 0, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Marketplace Image 1 set: '{Value}'", value);
                            break;
                        case "marketplaceimage2":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 1, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Marketplace Image 2 set: '{Value}'", value);
                            break;
                        case "marketplaceimage3":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 2, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Marketplace Image 3 set: '{Value}'", value);
                            break;
                        case "marketplaceimage4":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 3, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Marketplace Image 4 set: '{Value}'", value);
                            break;
                        case "marketplaceimage5":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 4, value);
                            fieldProcessed = true;
                            _logger.LogDebug("Marketplace Image 5 set: '{Value}'", value);
                            break;
                        case "marketplaceimage6":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 5, value);
                            fieldProcessed = true;
                            break;
                        case "marketplaceimage7":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 6, value);
                            fieldProcessed = true;
                            break;
                        case "marketplaceimage8":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 7, value);
                            fieldProcessed = true;
                            break;
                        case "marketplaceimage9":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 8, value);
                            fieldProcessed = true;
                            break;
                        case "marketplaceimage10":
                            SetImageUrlByIndex(product.MarketplaceImageUrls, 9, value);
                            fieldProcessed = true;
                            break;
                            
                        // Video URL'leri - ayrı indeksli sütunlar
                        case "video1":
                            SetImageUrlByIndex(product.VideoUrls, 0, value);
                            fieldProcessed = true;
                            break;
                        case "video2":
                            SetImageUrlByIndex(product.VideoUrls, 1, value);
                            fieldProcessed = true;
                            break;
                        case "video3":
                            SetImageUrlByIndex(product.VideoUrls, 2, value);
                            fieldProcessed = true;
                            break;
                        case "video4":
                            SetImageUrlByIndex(product.VideoUrls, 3, value);
                            fieldProcessed = true;
                            break;
                        case "video5":
                            SetImageUrlByIndex(product.VideoUrls, 4, value);
                            fieldProcessed = true;
                            break;
                        
                        // Eğer hiçbir case eşleşmezse debug log
                        default:
                            fieldProcessed = false;
                            break;
                    }
                    
                    // Eğer alan işlenmemişse kaydet
                    if (!fieldProcessed && !string.IsNullOrEmpty(value))
                    {
                        skippedColumns.Add($"{header} ({normalizedHeader}) = '{value}'");
                        _logger.LogWarning("Excel header '{Header}' (normalized: '{NormalizedHeader}') değeri '{Value}' işlenemedi - switch case'e eklenmiş değil", 
                            header, normalizedHeader, value);
                    }
                    else if (fieldProcessed && !string.IsNullOrEmpty(value))
                    {
                        _logger.LogDebug("Excel header '{Header}' başarıyla işlendi: '{Value}'", header, value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Sütun '{Header}' işlenirken hata: {Error}", header, ex.Message);
                }
            }

            // Zorunlu alanları kontrol et - daha detaylı log
            if (string.IsNullOrEmpty(product.Name))
            {
                _logger.LogWarning("ÜRÜN REDDEDİLDİ - Name alanı boş.");
                _logger.LogWarning("Excel başlıkları: {Headers}", 
                    string.Join(", ", headers));
                _logger.LogWarning("Excel değerleri: {Values}", 
                    string.Join(", ", values.Select((v, i) => $"{headers.ElementAtOrDefault(i)}='{v}'")));
                
                // Name ile ilgili tüm header'ları kontrol et
                var nameHeaders = new[] { "name", "urunadi", "ürün adı", "product name", "ürünadi" };
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i].ToLower().Trim();
                    if (nameHeaders.Any(nh => header.Contains(nh)))
                    {
                        _logger.LogWarning("Name benzeri header bulundu: '{Header}' = '{Value}' (index: {Index})", 
                            headers[i], values.ElementAtOrDefault(i), i);
                    }
                }
                
                return null;
            }

            // Eğer tarihler set edilmemişse şimdiki zamanı kullan
            if (product.CreatedDate == default)
                product.CreatedDate = DateTime.Now;
            if (product.UpdatedDate == null || product.UpdatedDate == default)
                product.UpdatedDate = DateTime.Now;

            // Ana görsel URL'yi ayarla (ImageUrls'den ilk boş olmayan URL'yi al)
            if (product.ImageUrls != null && product.ImageUrls.Any(img => !string.IsNullOrEmpty(img)))
            {
                var firstImage = product.ImageUrls.FirstOrDefault(img => !string.IsNullOrEmpty(img));
                if (!string.IsNullOrEmpty(firstImage))
                {
                    product.ImageUrl = firstImage;
                    _logger.LogDebug("Ana görsel URL ayarlandı: '{ImageUrl}'", firstImage);
                }
            }

            // Debug: Parse edilen ürün bilgilerini özetle
            _logger.LogDebug("Ürün parse edildi - Name: '{Name}', Category: '{Category}', LogoBarcodes: '{LogoBarcodes}', Images: {ImageCount}, MarketplaceImages: {MarketplaceImageCount}, Videos: {VideoCount}", 
                product.Name, product.Category ?? "BOŞ", product.LogoBarcodes ?? "BOŞ", 
                product.ImageUrls?.Count ?? 0, product.MarketplaceImageUrls?.Count ?? 0, product.VideoUrls?.Count ?? 0);

            // Eğer pazaryeri görselleri varsa detayını logla
            if (product.MarketplaceImageUrls != null && product.MarketplaceImageUrls.Any(img => !string.IsNullOrEmpty(img)))
            {
                var validMarketplaceImages = product.MarketplaceImageUrls.Where(img => !string.IsNullOrEmpty(img)).ToList();
                _logger.LogInformation("Pazaryeri görselleri yüklendi ({Count} adet): {Images}", 
                    validMarketplaceImages.Count, string.Join(", ", validMarketplaceImages.Take(3)));
            }

            return product;
        }
        
        /// <summary>
        /// Header isimlerini normalize eder - Excel export ve import uyumluluğu için
        /// </summary>
        private string NormalizeHeaderName(string header)
        {
            var normalized = header.ToLower().Trim()
                .Replace("ı", "i")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ş", "s")
                .Replace("ö", "o")
                .Replace("ç", "c")
                .Replace("İ", "i")
                .Replace("Ğ", "g")
                .Replace("Ü", "u")
                .Replace("Ş", "s")
                .Replace("Ö", "o")
                .Replace("Ç", "c")
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");
            
            _logger.LogDebug("Header normalization: '{Original}' -> '{Normalized}'", header, normalized);
            
            // Dictionary güvenli oluşturma - duplicatelerden kaçın
            var headerMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            // Helper metod ile güvenli ekle
            void SafeAdd(string key, string value)
            {
                if (!headerMappings.ContainsKey(key))
                {
                    headerMappings[key] = value;
                }
                else
                {
                    _logger.LogWarning("Duplicate header mapping key ignored: '{Key}' -> '{Value}'", key, value);
                }
            }
            
            // Temel alanlar
            SafeAdd("id", "id");
            SafeAdd("urunadi", "name");
            SafeAdd("name", "name");
            SafeAdd("sku", "sku");
            SafeAdd("marka", "brand");
            SafeAdd("brand", "brand");
            SafeAdd("kategori", "category");
            SafeAdd("category", "category");
            SafeAdd("aciklamahtml", "description");
            SafeAdd("aciklamaduzmetin", "description");
            SafeAdd("description", "description");
            SafeAdd("ozellikler", "features");
            SafeAdd("features", "features");
            SafeAdd("notlar", "notes");
            SafeAdd("notes", "notes");
            
            // Fiziksel özellikler
            SafeAdd("agirlikkg", "weight");
            SafeAdd("weight", "weight");
            SafeAdd("desi", "desi");
            SafeAdd("genislikcm", "width");
            SafeAdd("width", "width");
            SafeAdd("yukseklikcm", "height");
            SafeAdd("height", "height");
            SafeAdd("encm", "depth");
            SafeAdd("depth", "depth");
            SafeAdd("uzunlukcm", "length");
            SafeAdd("length", "length");
            SafeAdd("malzeme", "material");
            SafeAdd("material", "material");
            SafeAdd("renk", "color");
            SafeAdd("color", "color");
            SafeAdd("garanti", "warrantymonths");
            SafeAdd("warrantymonths", "warrantymonths");
            SafeAdd("eankodu", "eancode");
            SafeAdd("eancode", "eancode");
            
            // Statü ve tarihler
            SafeAdd("arsivlenmis", "archived");
            SafeAdd("archived", "archived");
            SafeAdd("olusturmatarihi", "createddate");
            SafeAdd("createddate", "createddate");
            SafeAdd("guncellenmetarihi", "updateddate");
            SafeAdd("updateddate", "updateddate");
            
            // Pazaryeri barkodları - her biri için tek mapping
            SafeAdd("trendyolbarkod", "trendyolbarcode");
            SafeAdd("trendyolbarcode", "trendyolbarcode");
            SafeAdd("hepsiburadabarkod", "hepsiburadabarcode");
            SafeAdd("hepsiburadabarcode", "hepsiburadabarcode");
            SafeAdd("amazonbarkod", "amazonbarcode");
            SafeAdd("amazonbarcode", "amazonbarcode");
            
            // Hepsiburada Seller Stock Code - tüm varyasyonlar
            SafeAdd("hepsiburadaticikstokkodu", "hepsiburadasekkerstockcode");
            SafeAdd("hepsiburadasekkerstockcode", "hepsiburadasekkerstockcode");
            SafeAdd("hepsiburadasaticisokkodu", "hepsiburadasekkerstockcode");
            SafeAdd("hepsiburadasaticistokkodu", "hepsiburadasekkerstockcode");
            SafeAdd("hepsiburadasekkersockcode", "hepsiburadasekkerstockcode");
            
            // Koçtaş barkodları
            SafeAdd("koctasbarkod", "koctasbarcode");
            SafeAdd("koctasbarcode", "koctasbarcode");
            SafeAdd("koctasistanbulbarkod", "koctasistanbulbarcode");
            SafeAdd("koctasistanbulbarcode", "koctasistanbulbarcode");
            SafeAdd("koctaseanbarkod", "koctaseanbarcode");
            SafeAdd("koctaseanbarcode", "koctaseanbarcode");
            SafeAdd("koctaseanistanbulbarkod", "koctaseanistanbulbarcode");
            SafeAdd("koctaseanistanbulbarcode", "koctaseanistanbulbarcode");
            
            // Diğer pazaryeri barkodları
            SafeAdd("hepsiburadatedarikbarkod", "hepsiburadatedarikbarcode");
            SafeAdd("hepsiburadatedarikbarcode", "hepsiburadatedarikbarcode");
            SafeAdd("pttavmbarkod", "pttavmbarcode");
            SafeAdd("pttavmbarcode", "pttavmbarcode");
            SafeAdd("ptturunid", "ptturunstokodu");
            SafeAdd("ptturunstokodu", "ptturunstokodu");
            SafeAdd("pazaramabarkod", "pazaramabarcode");
            SafeAdd("pazaramabarcode", "pazaramabarcode");
            SafeAdd("haceyapibarkod", "haceyapibarcode");
            SafeAdd("haceyapibarcode", "haceyapibarcode");
            SafeAdd("n11katalogid", "n11catalogid");
            SafeAdd("n11catalogid", "n11catalogid");
            SafeAdd("n11urunkodu", "n11productcode");
            SafeAdd("n11productcode", "n11productcode");

            // Entegra barkodları
            SafeAdd("entegraurunid", "entegraurunid");
            SafeAdd("entegraurunid", "entegraurunid");
            SafeAdd("entegraurunkodu", "entegraurunkodu");
            SafeAdd("entegraurunkodu", "entegraurunkodu");
            SafeAdd("entegrabarkod", "entegrabarkod");
            SafeAdd("entegrabarkod", "entegrabarkod");
            
            // Yedek barkodlar
            SafeAdd("yedekbarkod1", "sparebarcode1");
            SafeAdd("sparebarcode1", "sparebarcode1");
            SafeAdd("yedekbarkod2", "sparebarcode2");
            SafeAdd("sparebarcode2", "sparebarcode2");
            SafeAdd("yedekbarkod3", "sparebarcode3");
            SafeAdd("sparebarcode3", "sparebarcode3");
            SafeAdd("yedekbarkod4", "sparebarcode4");
            SafeAdd("sparebarcode4", "sparebarcode4");
            
            // Ürün özellikleri
            SafeAdd("klozetkanalyapisi", "klozetkanlyapisi");
            SafeAdd("klozetkanlyapisi", "klozetkanlyapisi");
            SafeAdd("klozettipi", "klozettipi");
            SafeAdd("klozetkapakcinsi", "klozetkapakcinsi");
            SafeAdd("klozetmontajtipi", "klozetmontajtipi");
            SafeAdd("lawabosumasadeligi", "lawabosuasmaseligu");
            SafeAdd("lawabosuasmaseligu", "lawabosuasmaseligu");
            SafeAdd("lawaboarmaturdeligi", "lawaboarmaturdeligi");
            SafeAdd("lawabotipi", "lawabotipi");
            SafeAdd("lawaboozeligi", "lawaboozellix");
            SafeAdd("lawaboozellix", "lawaboozellix");
            SafeAdd("bataryacikisucuuzunlugu", "bataryacikisucuuzunlugu");
            SafeAdd("bataryayuksekligi", "bataryayuksekligi");
            
            // Dinamik olarak numbered mappings ekle
            AddNumberedMappings(SafeAdd, "logobarkodu", "logobarcode", 10);
            AddNumberedMappings(SafeAdd, "logobarkod", "logobarcode", 10);
            AddNumberedMappings(SafeAdd, "logobarcode", "logobarcode", 10);
            
            AddNumberedMappings(SafeAdd, "urungovseli", "productimage", 10);
            AddNumberedMappings(SafeAdd, "productimage", "productimage", 10);
            AddNumberedMappings(SafeAdd, "gorsel", "productimage", 10);
            AddNumberedMappings(SafeAdd, "image", "productimage", 10);
            
            AddNumberedMappings(SafeAdd, "pazaryerigovseli", "marketplaceimage", 10);
            AddNumberedMappings(SafeAdd, "marketplaceimage", "marketplaceimage", 10);
            AddNumberedMappings(SafeAdd, "marketgorsel", "marketplaceimage", 10);
            AddNumberedMappings(SafeAdd, "pazaryerigorsel", "marketplaceimage", 10);
            AddNumberedMappings(SafeAdd, "pazaryerigorseli", "marketplaceimage", 10);
            
            AddNumberedMappings(SafeAdd, "video", "video", 5);
            AddNumberedMappings(SafeAdd, "videourl", "video", 5);
            
            // Sadece önemli header mapping hatalarını logla
            if (headerMappings.TryGetValue(normalized, out var mapped))
            {
                return mapped;
            }
            
            // Unmapped header - sadece warning ver
            return normalized;
        }
        
        /// <summary>
        /// Numbered mappings eklemek için helper metod
        /// </summary>
        private void AddNumberedMappings(Action<string, string> safeAdd, string prefix, string targetPrefix, int count)
        {
            for (int i = 1; i <= count; i++)
            {
                safeAdd($"{prefix}{i}", $"{targetPrefix}{i}");
            }
        }
        
        /// <summary>
        /// Sadece Excel'de gelen alanları günceller, diğerlerini mevcut halinde bırakır
        /// </summary>
        private async Task<Product> MergeProductUpdatesAsync(Product existingProduct, Product importedProduct, ImportOptions options)
        {
            // Mevcut ürünün kopyasını oluştur
            var updatedProduct = new Product
            {
                Id = existingProduct.Id,
                Name = existingProduct.Name,
                Description = existingProduct.Description,
                SKU = existingProduct.SKU,
                Brand = existingProduct.Brand,
                Category = existingProduct.Category,
                CategoryId = existingProduct.CategoryId, // CategoryId'yi de kopyala
                ImageUrl = existingProduct.ImageUrl,
                Weight = existingProduct.Weight,
                Desi = existingProduct.Desi,
                Width = existingProduct.Width,
                Height = existingProduct.Height,
                Depth = existingProduct.Depth,
                Length = existingProduct.Length,
                WarrantyMonths = existingProduct.WarrantyMonths,
                Material = existingProduct.Material,
                Color = existingProduct.Color,
                EanCode = existingProduct.EanCode,
                Features = existingProduct.Features,
                Notes = existingProduct.Notes,
                IsArchived = existingProduct.IsArchived,
                CreatedDate = existingProduct.CreatedDate,
                UpdatedDate = DateTime.Now, // Her zaman güncelle
                
                // Pazaryeri barkodları
                TrendyolBarcode = existingProduct.TrendyolBarcode,
                HepsiburadaBarcode = existingProduct.HepsiburadaBarcode,
                AmazonBarcode = existingProduct.AmazonBarcode,
                KoctasBarcode = existingProduct.KoctasBarcode,
                KoctasIstanbulBarcode = existingProduct.KoctasIstanbulBarcode,
                HepsiburadaTedarikBarcode = existingProduct.HepsiburadaTedarikBarcode,
                PttAvmBarcode = existingProduct.PttAvmBarcode,
                PazaramaBarcode = existingProduct.PazaramaBarcode,
                HaceyapiBarcode = existingProduct.HaceyapiBarcode,
                HepsiburadaSellerStockCode = existingProduct.HepsiburadaSellerStockCode,
                N11CatalogId = existingProduct.N11CatalogId,
                N11ProductCode = existingProduct.N11ProductCode,
                SpareBarcode1 = existingProduct.SpareBarcode1,
                SpareBarcode2 = existingProduct.SpareBarcode2,
                SpareBarcode3 = existingProduct.SpareBarcode3,
                SpareBarcode4 = existingProduct.SpareBarcode4,
                LogoBarcodes = existingProduct.LogoBarcodes,
                
                // Yeni Pazaryeri Barkodları
                KoctasEanBarcode = existingProduct.KoctasEanBarcode,
                KoctasEanIstanbulBarcode = existingProduct.KoctasEanIstanbulBarcode,
                PttUrunStokKodu = existingProduct.PttUrunStokKodu,
                
                // Ürün özellikleri
                KlozetKanalYapisi = existingProduct.KlozetKanalYapisi,
                KlozetTipi = existingProduct.KlozetTipi,
                KlozetKapakCinsi = existingProduct.KlozetKapakCinsi,
                KlozetMontajTipi = existingProduct.KlozetMontajTipi,
                LawaboSuTasmaDeligi = existingProduct.LawaboSuTasmaDeligi,
                LawaboArmaturDeligi = existingProduct.LawaboArmaturDeligi,
                LawaboTipi = existingProduct.LawaboTipi,
                LawaboOzelligi = existingProduct.LawaboOzelligi,
                BataryaCikisUcuUzunlugu = existingProduct.BataryaCikisUcuUzunlugu,
                BataryaYuksekligi = existingProduct.BataryaYuksekligi,
                
                // Görseller ve videolar
                ImageUrls = existingProduct.ImageUrls ?? new List<string>(),
                MarketplaceImageUrls = existingProduct.MarketplaceImageUrls ?? new List<string>(),
                VideoUrls = existingProduct.VideoUrls ?? new List<string>()
            };

            // Sadece Excel'de gelen (boş olmayan) alanları güncelle
            
            // Temel bilgiler - ID hariç diğerlerini kontrol et
            if (!string.IsNullOrWhiteSpace(importedProduct.Name) && importedProduct.Name != existingProduct.Name)
                updatedProduct.Name = importedProduct.Name;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.Description))
                updatedProduct.Description = importedProduct.Description;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.SKU))
                updatedProduct.SKU = importedProduct.SKU;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.Brand))
                updatedProduct.Brand = importedProduct.Brand;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.Category))
            {
                updatedProduct.Category = importedProduct.Category;
                // CategoryId'yi de güncelle - async olarak
                var categoryId = await _categoryService.GetOrCreateCategoryIdAsync(importedProduct.Category);
                updatedProduct.CategoryId = categoryId;
                _logger.LogInformation("Kategori güncellendi: '{OldCategory}' -> '{NewCategory}' (ID: {CategoryId}) [Ürün: {ProductName}]", 
                    existingProduct.Category ?? "Boş", importedProduct.Category, categoryId, existingProduct.Name);
            }
            else
            {
                _logger.LogDebug("Kategori güncellenmedi - Excel'de kategori boş (Ürün: {ProductName})", existingProduct.Name);
            }

            // Numeric alanlar - sadece 0'dan farklı değerler gelirse güncelle
            if (importedProduct.Weight > 0)
                updatedProduct.Weight = importedProduct.Weight;
                
            if (importedProduct.Desi > 0)
                updatedProduct.Desi = importedProduct.Desi;
                
            if (importedProduct.Width > 0)
                updatedProduct.Width = importedProduct.Width;
                
            if (importedProduct.Height > 0)
                updatedProduct.Height = importedProduct.Height;
                
            if (importedProduct.Depth > 0)
                updatedProduct.Depth = importedProduct.Depth;
                
            if (importedProduct.Length > 0)
                updatedProduct.Length = importedProduct.Length;
                
            if (importedProduct.WarrantyMonths > 0)
                updatedProduct.WarrantyMonths = importedProduct.WarrantyMonths;

            // String alanlar
            if (!string.IsNullOrWhiteSpace(importedProduct.Material))
                updatedProduct.Material = importedProduct.Material;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.Color))
                updatedProduct.Color = importedProduct.Color;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.EanCode))
                updatedProduct.EanCode = importedProduct.EanCode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.Features))
                updatedProduct.Features = importedProduct.Features;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.Notes))
                updatedProduct.Notes = importedProduct.Notes;

            // Arşiv durumu - Boolean alanlar için özel kontrol
            // PreserveArchiveStatus seçeneği aktifse mevcut değeri koru
            if (!options.PreserveArchiveStatus)
            {
                updatedProduct.IsArchived = importedProduct.IsArchived;
            }
            // Eğer PreserveArchiveStatus true ise, mevcut IsArchived değeri korunur

            // Pazaryeri barkodları - boş olmayan değerleri güncelle
            if (!string.IsNullOrWhiteSpace(importedProduct.TrendyolBarcode))
                updatedProduct.TrendyolBarcode = importedProduct.TrendyolBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.HepsiburadaBarcode))
                updatedProduct.HepsiburadaBarcode = importedProduct.HepsiburadaBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.AmazonBarcode))
                updatedProduct.AmazonBarcode = importedProduct.AmazonBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.KoctasBarcode))
                updatedProduct.KoctasBarcode = importedProduct.KoctasBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.KoctasIstanbulBarcode))
                updatedProduct.KoctasIstanbulBarcode = importedProduct.KoctasIstanbulBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.HepsiburadaTedarikBarcode))
                updatedProduct.HepsiburadaTedarikBarcode = importedProduct.HepsiburadaTedarikBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.PttAvmBarcode))
                updatedProduct.PttAvmBarcode = importedProduct.PttAvmBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.PazaramaBarcode))
                updatedProduct.PazaramaBarcode = importedProduct.PazaramaBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.HaceyapiBarcode))
                updatedProduct.HaceyapiBarcode = importedProduct.HaceyapiBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.HepsiburadaSellerStockCode))
                updatedProduct.HepsiburadaSellerStockCode = importedProduct.HepsiburadaSellerStockCode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.N11CatalogId))
                updatedProduct.N11CatalogId = importedProduct.N11CatalogId;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.N11ProductCode))
                updatedProduct.N11ProductCode = importedProduct.N11ProductCode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.SpareBarcode1))
                updatedProduct.SpareBarcode1 = importedProduct.SpareBarcode1;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.SpareBarcode2))
                updatedProduct.SpareBarcode2 = importedProduct.SpareBarcode2;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.SpareBarcode3))
                updatedProduct.SpareBarcode3 = importedProduct.SpareBarcode3;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.SpareBarcode4))
                updatedProduct.SpareBarcode4 = importedProduct.SpareBarcode4;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.LogoBarcodes))
                updatedProduct.LogoBarcodes = importedProduct.LogoBarcodes;
                
            // Yeni Pazaryeri Barkodları
            if (!string.IsNullOrWhiteSpace(importedProduct.KoctasEanBarcode))
                updatedProduct.KoctasEanBarcode = importedProduct.KoctasEanBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.KoctasEanIstanbulBarcode))
                updatedProduct.KoctasEanIstanbulBarcode = importedProduct.KoctasEanIstanbulBarcode;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.PttUrunStokKodu))
                updatedProduct.PttUrunStokKodu = importedProduct.PttUrunStokKodu;

            // Ürün özellikleri
            if (!string.IsNullOrWhiteSpace(importedProduct.KlozetKanalYapisi))
                updatedProduct.KlozetKanalYapisi = importedProduct.KlozetKanalYapisi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.KlozetTipi))
                updatedProduct.KlozetTipi = importedProduct.KlozetTipi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.KlozetKapakCinsi))
                updatedProduct.KlozetKapakCinsi = importedProduct.KlozetKapakCinsi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.KlozetMontajTipi))
                updatedProduct.KlozetMontajTipi = importedProduct.KlozetMontajTipi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.LawaboSuTasmaDeligi))
                updatedProduct.LawaboSuTasmaDeligi = importedProduct.LawaboSuTasmaDeligi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.LawaboArmaturDeligi))
                updatedProduct.LawaboArmaturDeligi = importedProduct.LawaboArmaturDeligi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.LawaboTipi))
                updatedProduct.LawaboTipi = importedProduct.LawaboTipi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.LawaboOzelligi))
                updatedProduct.LawaboOzelligi = importedProduct.LawaboOzelligi;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.BataryaCikisUcuUzunlugu))
                updatedProduct.BataryaCikisUcuUzunlugu = importedProduct.BataryaCikisUcuUzunlugu;
                
            if (!string.IsNullOrWhiteSpace(importedProduct.BataryaYuksekligi))
                updatedProduct.BataryaYuksekligi = importedProduct.BataryaYuksekligi;

            // Görseller - sadece yeni görseller varsa güncelle
            if (importedProduct.ImageUrls != null && importedProduct.ImageUrls.Any())
                updatedProduct.ImageUrls = importedProduct.ImageUrls;
                
            if (importedProduct.MarketplaceImageUrls != null && importedProduct.MarketplaceImageUrls.Any())
                updatedProduct.MarketplaceImageUrls = importedProduct.MarketplaceImageUrls;
                
            if (importedProduct.VideoUrls != null && importedProduct.VideoUrls.Any())
                updatedProduct.VideoUrls = importedProduct.VideoUrls;

            // Ana görsel URL'yi güncelle (eğer ImageUrls varsa ilkini al)
            if (updatedProduct.ImageUrls != null && updatedProduct.ImageUrls.Any())
            {
                var firstImage = updatedProduct.ImageUrls.FirstOrDefault(img => !string.IsNullOrEmpty(img));
                if (!string.IsNullOrEmpty(firstImage))
                    updatedProduct.ImageUrl = firstImage;
            }

            return updatedProduct;
        }
        
        /// <summary>
        /// Liste'yi batch'lere böler (.NET 6 için Chunk alternatifi)
        /// </summary>
        private static List<List<T>> ChunkList<T>(List<T> source, int chunkSize)
        {
            var result = new List<List<T>>();
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                var chunk = source.Skip(i).Take(chunkSize).ToList();
                result.Add(chunk);
            }
            return result;
        }

        /// <summary>
        /// Logo barkodlarını indekse göre ayarlar
        /// </summary>
        private void SetLogoBarcodeByIndex(Product product, int index, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            // LogoBarcodes string'ini parse et veya oluştur
            var logoBarcodes = new List<string>();
            
            if (!string.IsNullOrEmpty(product.LogoBarcodes))
            {
                // Virgül ile ayrılmış değerleri parse et - boş değerleri de koru
                logoBarcodes = product.LogoBarcodes.Split(',')
                    .Select(s => s.Trim())
                    .ToList();
            }

            // Liste boyutunu en az index+1 kadar yap
            while (logoBarcodes.Count <= index)
            {
                logoBarcodes.Add("");
            }

            // İlgili indekse değeri ata
            logoBarcodes[index] = value;

            // Sondaki boş değerleri temizle ama aralarındaki boş değerleri koru
            while (logoBarcodes.Count > 0 && string.IsNullOrEmpty(logoBarcodes[logoBarcodes.Count - 1]))
            {
                logoBarcodes.RemoveAt(logoBarcodes.Count - 1);
            }
            
            // Eğer hiç barcode yoksa boş string ata
            if (!logoBarcodes.Any() || logoBarcodes.All(s => string.IsNullOrEmpty(s)))
            {
                product.LogoBarcodes = "";
            }
            else
            {
                // Virgül ile birleştir - bu sayede pozisyonlar korunur
                product.LogoBarcodes = string.Join(",", logoBarcodes);
            }

            _logger.LogDebug("Logo barcode {Index} set to '{Value}'. Full LogoBarcodes: '{LogoBarcodes}'", 
                index, value, product.LogoBarcodes);
        }

        /// <summary>
        /// Image URL'lerini indekse göre ayarlar
        /// </summary>
        private void SetImageUrlByIndex(List<string> imageUrls, int index, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            // Liste boyutunu gerektiği kadar genişlet
            while (imageUrls.Count <= index)
            {
                imageUrls.Add("");
            }

            // İlgili indekse değeri ata
            imageUrls[index] = value;
            
            _logger.LogDebug("Image URL set at index {Index}: '{Value}' (Total images: {Count})", 
                index, value, imageUrls.Count);
        }
        #endregion
    }

    #region Model Classes
    public class ImportOptions
    {
        public bool UpdateExisting { get; set; } = true; // Varsayılan olarak güncellemeyi aç
        public bool CreateCategories { get; set; } = true;
        public bool SkipErrors { get; set; } = true;
        public bool PreserveArchiveStatus { get; set; } = true; // true = mevcut durumu koru, false = Excel'den gelen değeri kullan
        public int BatchSize { get; set; } = 100; // 3000+ ürün için batch boyutu
        public bool EnableBatchLogging { get; set; } = true; // Batch işlemleri için detaylı log
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string? ErrorMessage { get; set; }
        public TimeSpan ProcessingTime { get; set; } // İşlem süresi
        public int BatchCount { get; set; } // Kaç batch işlendiği
    }
    #endregion
}
