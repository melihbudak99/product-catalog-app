using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;
using System.Xml.Serialization;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using product_catalog_app.src.models;
using product_catalog_app.src.interfaces;
using System.Globalization;

namespace product_catalog_app.src.services
{
    public class ExportService
    {
        private readonly IProductService _productService;
        private readonly ILogger<ExportService> _logger;
        private readonly ExportColumnService _columnService;

        public ExportService(IProductService productService, ILogger<ExportService> logger, ExportColumnService columnService)
        {
            _productService = productService;
            _logger = logger;
            _columnService = columnService;
        }

        /// <summary>
        /// Türkçe karakterleri normalize eder ve arama için hazırlar
        /// </summary>
        private static string NormalizeSearchTerm(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // Türkçe karakterleri İngilizce eşdeğerlerine çevir
            var normalized = text
                .Replace('ı', 'i').Replace('İ', 'I')
                .Replace('ğ', 'g').Replace('Ğ', 'G')
                .Replace('ü', 'u').Replace('Ü', 'U')
                .Replace('ş', 's').Replace('Ş', 'S')
                .Replace('ö', 'o').Replace('Ö', 'O')
                .Replace('ç', 'c').Replace('Ç', 'C');

            return normalized.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// Ürünleri belirtilen kriterlere göre filtreler ve export için hazırlar
        /// </summary>
        private async Task<List<Product>> GetFilteredProductsAsync(ExportFilter filter)
        {
            List<Product> products = new List<Product>();

            // Durum filtresi
            switch (filter.Status?.ToLower())
            {
                case "active":
                    products = await _productService.GetAllProductsAsync();
                    products = products.Where(p => !p.IsArchived).ToList(); // Aktif = arşivde olmayan
                    break;
                case "archived":
                    products = await _productService.GetAllProductsAsync();
                    products = products.Where(p => p.IsArchived).ToList();
                    break;
                default: // "all"
                    products = await _productService.GetAllProductsAsync();
                    break;
            }

            // Kategori filtresi
            if (!string.IsNullOrEmpty(filter.Category))
            {
                products = products.Where(p => 
                    p.Category?.Equals(filter.Category, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            // Marka filtresi
            if (!string.IsNullOrEmpty(filter.Brand))
            {
                products = products.Where(p => 
                    p.Brand?.Equals(filter.Brand, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            // Arama filtresi - Türkçe karakter destekli
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.Trim();
                var normalizedSearchTerm = NormalizeSearchTerm(searchTerm);
                
                products = products.Where(p => 
                    (p.Name != null && (p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                       NormalizeSearchTerm(p.Name).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Description != null && (p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                              NormalizeSearchTerm(p.Description).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.SKU != null && (p.SKU.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                      NormalizeSearchTerm(p.SKU).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Brand != null && (p.Brand.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                        NormalizeSearchTerm(p.Brand).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Category != null && (p.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                           NormalizeSearchTerm(p.Category).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Features != null && (p.Features.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                           NormalizeSearchTerm(p.Features).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Material != null && (p.Material.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                           NormalizeSearchTerm(p.Material).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Color != null && (p.Color.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                        NormalizeSearchTerm(p.Color).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.Notes != null && (p.Notes.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                        NormalizeSearchTerm(p.Notes).Contains(normalizedSearchTerm, StringComparison.OrdinalIgnoreCase))) ||
                    (p.EanCode != null && p.EanCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            _logger.LogInformation("Filtrelenmiş ürün sayısı: {Count}", products.Count);
            return products;
        }

        /// <summary>
        /// Ürünleri belirtilen kriterlere göre filtreler ve export için hazırlar (ExportColumnFilter overload)
        /// </summary>
        private async Task<List<Product>> GetFilteredProductsAsync(ExportColumnFilter filter)
        {
            // Eğer seçili ürün ID'leri varsa, sadece onları al
            if (filter.SelectedProductIds != null && filter.SelectedProductIds.Any())
            {
                var allProducts = await _productService.GetAllProductsAsync();
                var selectedProducts = allProducts.Where(p => filter.SelectedProductIds.Contains(p.Id)).ToList();
                
                _logger.LogInformation("Seçili ürünler filtrelendi: {Count} ürün", selectedProducts.Count);
                return selectedProducts;
            }
            
            // Normal filtreleme
            var baseFilter = new ExportFilter
            {
                Status = filter.Status,
                Category = filter.Category,
                Brand = filter.Brand,
                SearchTerm = filter.SearchTerm
            };
            
            return await GetFilteredProductsAsync(baseFilter);
        }

        #region Column Selection API
        /// <summary>
        /// Mevcut sütunları döndürür
        /// </summary>
        public List<ExportColumn> GetAvailableColumns()
        {
            return _columnService.GetAvailableColumns();
        }

        /// <summary>
        /// Kategoriye göre sütunları döndürür
        /// </summary>
        public List<ExportColumn> GetColumnsByCategory(string category)
        {
            return _columnService.GetColumnsByCategory(category);
        }

        /// <summary>
        /// Varsayılan sütun seçimini döndürür
        /// </summary>
        public List<string> GetDefaultSelectedColumns()
        {
            return _columnService.GetDefaultSelectedColumns();
        }
        #endregion

        #region XML Export
        /// <summary>
        /// XML formatında export
        /// </summary>
        public async Task<ExportResult> ExportToXmlAsync(ExportFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                var xmlProducts = products.Select(ConvertToProductXml).ToList();

                var catalog = new ProductCatalog { Products = xmlProducts };
                var serializer = new XmlSerializer(typeof(ProductCatalog));
                
                using var stringWriter = new ExportUtf8StringWriter();
                using var xmlWriter = System.Xml.XmlWriter.Create(stringWriter, new System.Xml.XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false
                });

                serializer.Serialize(xmlWriter, catalog);
                var xmlContent = stringWriter.ToString();

                var fileName = GenerateFileName("xml", filter);
                var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);

                _logger.LogInformation("XML export tamamlandı: {Count} ürün", products.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = xmlBytes,
                    FileName = fileName,
                    ContentType = "application/xml",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML export sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"XML export hatası: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Seçilen sütunlarla XML formatında export
        /// </summary>
        public async Task<ExportResult> ExportToXmlWithColumnsAsync(ExportColumnFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                var exportData = _columnService.PrepareExportData(products, filter.SelectedColumns, 
                    new DescriptionExportOptions 
                    { 
                        IncludeHtml = filter.IncludeHtmlDescription, 
                        IncludePlainText = filter.IncludePlainTextDescription 
                    });

                var xmlContent = ConvertToCustomXml(exportData, filter.SelectedColumns);
                var fileName = GenerateFileName("xml", filter);
                var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);

                _logger.LogInformation("XML export (sütun seçimli) tamamlandı: {Count} ürün, {ColumnCount} sütun", 
                    products.Count, filter.SelectedColumns.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = xmlBytes,
                    FileName = fileName,
                    ContentType = "application/xml",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XML export (sütun seçimli) sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"XML export hatası: {ex.Message}"
                };
            }
        }
        #endregion

        #region JSON Export
        /// <summary>
        /// JSON formatında export
        /// </summary>
        public async Task<ExportResult> ExportToJsonAsync(ExportFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(products, jsonOptions);
                var fileName = GenerateFileName("json", filter);
                var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);

                _logger.LogInformation("JSON export tamamlandı: {Count} ürün", products.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = jsonBytes,
                    FileName = fileName,
                    ContentType = "application/json",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON export sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"JSON export hatası: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Seçilen sütunlarla JSON formatında export
        /// </summary>
        public async Task<ExportResult> ExportToJsonWithColumnsAsync(ExportColumnFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                var exportData = _columnService.PrepareExportData(products, filter.SelectedColumns, 
                    new DescriptionExportOptions 
                    { 
                        IncludeHtml = filter.IncludeHtmlDescription, 
                        IncludePlainText = filter.IncludePlainTextDescription 
                    });

                var jsonData = ConvertToCustomJson(exportData, filter.SelectedColumns);
                var fileName = GenerateFileName("json", filter);
                var jsonBytes = Encoding.UTF8.GetBytes(jsonData);

                _logger.LogInformation("JSON export (sütun seçimli) tamamlandı: {Count} ürün, {ColumnCount} sütun", 
                    products.Count, filter.SelectedColumns.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = jsonBytes,
                    FileName = fileName,
                    ContentType = "application/json",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON export (sütun seçimli) sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"JSON export hatası: {ex.Message}"
                };
            }
        }
        #endregion

        #region CSV Export
        /// <summary>
        /// CSV formatında export
        /// </summary>
        public async Task<ExportResult> ExportToCsvAsync(ExportFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                var csv = new StringBuilder();

                // CSV başlıkları
                csv.AppendLine("ID,Ürün Adı,SKU,Marka,Kategori,Açıklama,Ağırlık,Desi,Genişlik,Yükseklik,Derinlik,Uzunluk,Garanti Ayı,Malzeme,Renk,EAN Kodu,Özellikler,Notlar,Ana Görsel,Aktif,Arşiv,Oluşturulma Tarihi,Güncelleme Tarihi,Trendyol Barkod,Hepsiburada Barkod,Amazon Barkod,Koçtaş Barkod,Koçtaş Istanbul Barkod,Hepsiburada Tedarik Barkod,PTT AVM Barkod,Pazarama Barkod,Haceyapı Barkod,Hepsiburada Seller Stock Code,N11 Catalog ID,N11 Product Code,Spare Barcode 1,Spare Barcode 2,Spare Barcode 3,Spare Barcode 4,Koçtaş EAN Barkod,Koçtaş EAN Istanbul Barkod,PTT Ürün ID,Logo Barkodu 1,Logo Barkodu 2,Logo Barkodu 3,Logo Barkodu 4,Logo Barkodu 5,Logo Barkodu 6,Logo Barkodu 7,Logo Barkodu 8,Logo Barkodu 9,Logo Barkodu 10");

                // Ürün verileri
                foreach (var product in products)
                {
                    csv.AppendLine(string.Join(",", new[]
                    {
                        EscapeCsvField(product.Id.ToString()),
                        EscapeCsvField(product.Name ?? ""),
                        EscapeCsvField(product.SKU ?? ""),
                        EscapeCsvField(product.Brand ?? ""),
                        EscapeCsvField(product.Category ?? ""),
                        EscapeCsvField(product.Description ?? ""),
                        EscapeCsvField(product.Weight.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsvField(product.Desi.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsvField(product.Width.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsvField(product.Height.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsvField(product.Depth.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsvField((product.Length ?? 0).ToString(CultureInfo.InvariantCulture)),
                        EscapeCsvField(product.WarrantyMonths.ToString()),
                        EscapeCsvField(product.Material ?? ""),
                        EscapeCsvField(product.Color ?? ""),
                        EscapeCsvField(product.EanCode ?? ""),
                        EscapeCsvField(product.Features ?? ""),
                        EscapeCsvField(product.Notes ?? ""),
                        EscapeCsvField(product.ImageUrl ?? ""),
                        EscapeCsvField(product.IsArchived ? "Arşiv" : "Aktif"), // IsActive yerine IsArchived
                        EscapeCsvField(product.IsArchived.ToString()),
                        EscapeCsvField(product.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")),
                        EscapeCsvField(product.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""),
                        EscapeCsvField(product.TrendyolBarcode ?? ""),
                        EscapeCsvField(product.HepsiburadaBarcode ?? ""),
                        EscapeCsvField(product.AmazonBarcode ?? ""),
                        EscapeCsvField(product.KoctasBarcode ?? ""),
                        EscapeCsvField(product.KoctasIstanbulBarcode ?? ""),
                        EscapeCsvField(product.HepsiburadaTedarikBarcode ?? ""),
                        EscapeCsvField(product.PttAvmBarcode ?? ""),
                        EscapeCsvField(product.PazaramaBarcode ?? ""),
                        EscapeCsvField(product.HaceyapiBarcode ?? ""),
                        EscapeCsvField(product.HepsiburadaSellerStockCode ?? ""),
                        EscapeCsvField(product.N11CatalogId ?? ""),
                        EscapeCsvField(product.N11ProductCode ?? ""),
                        EscapeCsvField(product.SpareBarcode1 ?? ""),
                        EscapeCsvField(product.SpareBarcode2 ?? ""),
                        EscapeCsvField(product.SpareBarcode3 ?? ""),
                        EscapeCsvField(product.SpareBarcode4 ?? ""),
                        EscapeCsvField(product.KoctasEanBarcode ?? ""),
                        EscapeCsvField(product.KoctasEanIstanbulBarcode ?? ""),
                        EscapeCsvField(product.PttUrunStokKodu ?? ""),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 0)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 1)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 2)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 3)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 4)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 5)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 6)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 7)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 8)),
                        EscapeCsvField(GetLogoBarcodeByIndex(product.LogoBarcodes, 9))
                    }));
                }

                var fileName = GenerateFileName("csv", filter);
                var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());

                _logger.LogInformation("CSV export tamamlandı: {Count} ürün", products.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = csvBytes,
                    FileName = fileName,
                    ContentType = "text/csv",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV export sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"CSV export hatası: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Seçilen sütunlarla CSV formatında export
        /// </summary>
        public async Task<ExportResult> ExportToCsvWithColumnsAsync(ExportColumnFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                var exportData = _columnService.PrepareExportData(products, filter.SelectedColumns, 
                    new DescriptionExportOptions 
                    { 
                        IncludeHtml = filter.IncludeHtmlDescription, 
                        IncludePlainText = filter.IncludePlainTextDescription 
                    });

                var csvContent = ConvertToCustomCsv(exportData, filter.SelectedColumns);
                var fileName = GenerateFileName("csv", filter);
                var csvBytes = Encoding.UTF8.GetBytes(csvContent);

                _logger.LogInformation("CSV export (sütun seçimli) tamamlandı: {Count} ürün, {ColumnCount} sütun", 
                    products.Count, filter.SelectedColumns.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = csvBytes,
                    FileName = fileName,
                    ContentType = "text/csv",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CSV export (sütun seçimli) sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"CSV export hatası: {ex.Message}"
                };
            }
        }
        #endregion

        #region Excel Export
        /// <summary>
        /// Excel formatında export
        /// </summary>
        public async Task<ExportResult> ExportToExcelAsync(ExportFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Ürünler");

                // Başlık satırı
                var headers = new[]
                {
                    "ID", "Ürün Adı", "SKU", "Marka", "Kategori", "Açıklama (HTML)", "Açıklama (Düz Metin)", 
                    "Özellikler", "Notlar", "Ağırlık (kg)", "Desi", "Genişlik (cm)", "Yükseklik (cm)", "En (cm)", 
                    "Uzunluk (cm)", "Malzeme", "Renk", "Garanti", "EAN Kodu", "Oluşturma Tarihi", 
                    "Güncelleme Tarihi", "Arşivlenmiş",
                    // Özel Ürün Özellikleri
                    "Klozet Kanal Yapısı", "Klozet Tipi", "Klozet Kapak Cinsi", "Klozet Montaj Tipi",
                    "Lavabo Su Taşma Deliği", "Lavabo Armatur Deliği", "Lavabo Tipi", "Lavabo Özelliği",
                    "Batarya Çıkış Ucu Uzunluğu", "Batarya Yüksekliği",
                    // Pazaryeri Barkodları
                    "Trendyol Barkod", "Hepsiburada Barkod", "Hepsiburada Satıcı Stok Kodu", "Amazon Barkod",
                    "Koçtaş Barkod", "Koçtaş İstanbul Barkod", "Koçtaş EAN Barkod", "Koçtaş EAN İstanbul Barkod", 
                    "Hepsiburada Tedarik Barkod", "PTT AVM Barkod", "PTT Ürün ID", "Pazarama Barkod", 
                    "Haceyapı Barkod", "N11 Katalog ID", "N11 Ürün Kodu",
                    // Entegra Barkodları
                    "Entegra Ürün ID", "Entegra Ürün Kodu", "Entegra Barkod",
                    // Yedek Barkodlar
                    "Yedek Barkod 1", "Yedek Barkod 2", "Yedek Barkod 3", "Yedek Barkod 4",
                    // Logo Barkodları
                    "Logo Barkodu 1", "Logo Barkodu 2", "Logo Barkodu 3", "Logo Barkodu 4", 
                    "Logo Barkodu 5", "Logo Barkodu 6", "Logo Barkodu 7", "Logo Barkodu 8", "Logo Barkodu 9", "Logo Barkodu 10",
                    // Ürün Görselleri
                    "Ürün Görseli 1", "Ürün Görseli 2", "Ürün Görseli 3", "Ürün Görseli 4", "Ürün Görseli 5",
                    "Ürün Görseli 6", "Ürün Görseli 7", "Ürün Görseli 8", "Ürün Görseli 9", "Ürün Görseli 10",
                    // Pazaryeri Görselleri
                    "Pazaryeri Görseli 1", "Pazaryeri Görseli 2", "Pazaryeri Görseli 3", "Pazaryeri Görseli 4", "Pazaryeri Görseli 5",
                    "Pazaryeri Görseli 6", "Pazaryeri Görseli 7", "Pazaryeri Görseli 8", "Pazaryeri Görseli 9", "Pazaryeri Görseli 10",
                    // Video URL'leri
                    "Video 1", "Video 2", "Video 3", "Video 4", "Video 5"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Veri satırları
                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    var row = i + 2;
                    var col = 1;

                    // Temel bilgiler
                    worksheet.Cell(row, col++).Value = product.Id;
                    worksheet.Cell(row, col++).Value = product.Name ?? "";
                    worksheet.Cell(row, col++).Value = product.SKU ?? "";
                    worksheet.Cell(row, col++).Value = product.Brand ?? "";
                    worksheet.Cell(row, col++).Value = product.Category ?? "";
                    worksheet.Cell(row, col++).Value = CleanHtmlForExport(product.Description ?? ""); // HTML açıklama - temizlenmiş
                    worksheet.Cell(row, col++).Value = StripHtmlTagsForExport(product.Description ?? ""); // Düz metin açıklama
                    worksheet.Cell(row, col++).Value = product.Features ?? "";
                    worksheet.Cell(row, col++).Value = product.Notes ?? "";
                    worksheet.Cell(row, col++).Value = product.Weight;
                    worksheet.Cell(row, col++).Value = product.Desi;
                    worksheet.Cell(row, col++).Value = product.Width;
                    worksheet.Cell(row, col++).Value = product.Height;
                    worksheet.Cell(row, col++).Value = product.Depth;
                    worksheet.Cell(row, col++).Value = product.Length ?? 0;
                    worksheet.Cell(row, col++).Value = product.Material ?? "";
                    worksheet.Cell(row, col++).Value = product.Color ?? "";
                    worksheet.Cell(row, col++).Value = product.WarrantyMonths;
                    worksheet.Cell(row, col++).Value = product.EanCode ?? "";
                    worksheet.Cell(row, col++).Value = product.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(row, col++).Value = product.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    worksheet.Cell(row, col++).Value = product.IsArchived ? "Evet" : "Hayır";

                    // Özel Ürün Özellikleri
                    worksheet.Cell(row, col++).Value = product.KlozetKanalYapisi ?? "";
                    worksheet.Cell(row, col++).Value = product.KlozetTipi ?? "";
                    worksheet.Cell(row, col++).Value = product.KlozetKapakCinsi ?? "";
                    worksheet.Cell(row, col++).Value = product.KlozetMontajTipi ?? "";
                    worksheet.Cell(row, col++).Value = product.LawaboSuTasmaDeligi ?? "";
                    worksheet.Cell(row, col++).Value = product.LawaboArmaturDeligi ?? "";
                    worksheet.Cell(row, col++).Value = product.LawaboTipi ?? "";
                    worksheet.Cell(row, col++).Value = product.LawaboOzelligi ?? "";
                    worksheet.Cell(row, col++).Value = product.BataryaCikisUcuUzunlugu ?? "";
                    worksheet.Cell(row, col++).Value = product.BataryaYuksekligi ?? "";

                    // Pazaryeri Barkodları
                    worksheet.Cell(row, col++).Value = product.TrendyolBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.HepsiburadaBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.HepsiburadaSellerStockCode ?? "";
                    worksheet.Cell(row, col++).Value = product.AmazonBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.KoctasBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.KoctasIstanbulBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.KoctasEanBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.KoctasEanIstanbulBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.HepsiburadaTedarikBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.PttAvmBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.PttUrunStokKodu ?? "";
                    worksheet.Cell(row, col++).Value = product.PazaramaBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.HaceyapiBarcode ?? "";
                    worksheet.Cell(row, col++).Value = product.N11CatalogId ?? "";
                    worksheet.Cell(row, col++).Value = product.N11ProductCode ?? "";

                    // Entegra Barkodları
                    worksheet.Cell(row, col++).Value = product.EntegraUrunId ?? "";
                    worksheet.Cell(row, col++).Value = product.EntegraUrunKodu ?? "";
                    worksheet.Cell(row, col++).Value = product.EntegraBarkod ?? "";

                    // Yedek Barkodlar
                    worksheet.Cell(row, col++).Value = product.SpareBarcode1 ?? "";
                    worksheet.Cell(row, col++).Value = product.SpareBarcode2 ?? "";
                    worksheet.Cell(row, col++).Value = product.SpareBarcode3 ?? "";
                    worksheet.Cell(row, col++).Value = product.SpareBarcode4 ?? "";

                    // Logo barkodları - ayrı sütunlarda
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 0);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 1);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 2);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 3);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 4);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 5);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 6);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 7);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 8);
                    worksheet.Cell(row, col++).Value = GetLogoBarcodeByIndex(product.LogoBarcodes, 9);

                    // Ürün Görselleri
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 0);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 1);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 2);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 3);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 4);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 5);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 6);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 7);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 8);
                    worksheet.Cell(row, col++).Value = GetImageUrlByIndex(product.ImageUrls, 9);

                    // Pazaryeri Görselleri
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 0);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 1);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 2);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 3);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 4);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 5);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 6);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 7);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 8);
                    worksheet.Cell(row, col++).Value = GetMarketplaceImageUrlByIndex(product.MarketplaceImageUrls, 9);

                    // Video URL'leri
                    worksheet.Cell(row, col++).Value = GetVideoUrlByIndex(product.VideoUrls, 0);
                    worksheet.Cell(row, col++).Value = GetVideoUrlByIndex(product.VideoUrls, 1);
                    worksheet.Cell(row, col++).Value = GetVideoUrlByIndex(product.VideoUrls, 2);
                    worksheet.Cell(row, col++).Value = GetVideoUrlByIndex(product.VideoUrls, 3);
                    worksheet.Cell(row, col++).Value = GetVideoUrlByIndex(product.VideoUrls, 4);
                }

                // Sütunları otomatik boyutlandır
                worksheet.Columns().AdjustToContents();

                // Satır yüksekliğini ayarla
                worksheet.Rows().Height = 15;

                // Excel dosyasını byte array'e dönüştür
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var fileName = GenerateFileName("xlsx", filter);
                var excelBytes = stream.ToArray();

                _logger.LogInformation("Excel export tamamlandı: {Count} ürün", products.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = excelBytes,
                    FileName = fileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel export sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"Excel export hatası: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Seçilen sütunlarla Excel formatında export
        /// </summary>
        public async Task<ExportResult> ExportToExcelWithColumnsAsync(ExportColumnFilter filter)
        {
            try
            {
                var products = await GetFilteredProductsAsync(filter);
                var exportData = _columnService.PrepareExportData(products, filter.SelectedColumns, 
                    new DescriptionExportOptions 
                    { 
                        IncludeHtml = filter.IncludeHtmlDescription, 
                        IncludePlainText = filter.IncludePlainTextDescription 
                    });

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Ürünler");

                var selectedColumnDefinitions = _columnService.GetSelectedColumns(filter.SelectedColumns);

                // Başlık satırı
                for (int i = 0; i < selectedColumnDefinitions.Count; i++)
                {
                    var column = selectedColumnDefinitions.OrderBy(c => c.Order).ElementAt(i);
                    worksheet.Cell(1, i + 1).Value = column.DisplayName;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // Veri satırları
                for (int rowIndex = 0; rowIndex < exportData.Count; rowIndex++)
                {
                    var productData = exportData[rowIndex];
                    var row = rowIndex + 2;

                    for (int colIndex = 0; colIndex < selectedColumnDefinitions.Count; colIndex++)
                    {
                        var column = selectedColumnDefinitions.OrderBy(c => c.Order).ElementAt(colIndex);
                        var value = productData.GetValue<object>(column.PropertyName);
                        
                        // Excel cell'e değer atama
                        if (value == null)
                        {
                            worksheet.Cell(row, colIndex + 1).Value = "";
                        }
                        else if (column.DataType == "datetime" && value is DateTime dt)
                        {
                            worksheet.Cell(row, colIndex + 1).Value = dt;
                        }
                        else if (column.DataType == "bool" && value is bool b)
                        {
                            worksheet.Cell(row, colIndex + 1).Value = b ? "Evet" : "Hayır";
                        }
                        else if (column.DataType == "decimal" && decimal.TryParse(value.ToString(), out var decVal))
                        {
                            worksheet.Cell(row, colIndex + 1).Value = decVal;
                        }
                        else if (column.DataType == "int" && int.TryParse(value.ToString(), out var intVal))
                        {
                            worksheet.Cell(row, colIndex + 1).Value = intVal;
                        }
                        else
                        {
                            worksheet.Cell(row, colIndex + 1).Value = value.ToString() ?? "";
                        }
                    }
                }

                // Sütunları otomatik boyutlandır
                worksheet.Columns().AdjustToContents();

                // Satır yüksekliğini ayarla
                worksheet.Rows().Height = 15;

                // Excel dosyasını byte array'e dönüştür
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                var fileName = GenerateFileName("xlsx", filter);
                var excelBytes = stream.ToArray();

                _logger.LogInformation("Excel export (sütun seçimli) tamamlandı: {Count} ürün, {ColumnCount} sütun", 
                    products.Count, filter.SelectedColumns.Count);

                return new ExportResult
                {
                    Success = true,
                    Content = excelBytes,
                    FileName = fileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    RecordCount = products.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excel export (sütun seçimli) sırasında hata oluştu");
                return new ExportResult
                {
                    Success = false,
                    ErrorMessage = $"Excel export hatası: {ex.Message}"
                };
            }
        }
        #endregion

        #region Helper Methods
        private ProductXml ConvertToProductXml(Product product)
        {
            var xmlProduct = new ProductXml
            {
                Id = product.Id,
                Name = product.Name ?? string.Empty,
                Description = product.Description ?? string.Empty,
                Category = product.Category ?? string.Empty,
                Brand = product.Brand ?? string.Empty,
                SKU = product.SKU ?? string.Empty,
                Weight = product.Weight,
                Desi = product.Desi,
                Width = product.Width,
                Height = product.Height,
                Depth = product.Depth,
                WarrantyMonths = product.WarrantyMonths,
                Material = product.Material ?? string.Empty,
                Color = product.Color ?? string.Empty,
                EanCode = product.EanCode ?? string.Empty,
                Features = product.Features ?? string.Empty,
                Notes = product.Notes ?? string.Empty,
                TrendyolBarcode = product.TrendyolBarcode ?? string.Empty,
                HepsiburadaBarcode = product.HepsiburadaBarcode ?? string.Empty,
                HepsiburadaSellerStockCode = product.HepsiburadaSellerStockCode ?? string.Empty,
                KoctasBarcode = product.KoctasBarcode ?? string.Empty,
                KoctasIstanbulBarcode = product.KoctasIstanbulBarcode ?? string.Empty,
                HepsiburadaTedarikBarcode = product.HepsiburadaTedarikBarcode ?? string.Empty,
                PttAvmBarcode = product.PttAvmBarcode ?? string.Empty,
                PazaramaBarcode = product.PazaramaBarcode ?? string.Empty,
                HaceyapiBarcode = product.HaceyapiBarcode ?? string.Empty,
                AmazonBarcode = product.AmazonBarcode ?? string.Empty,
                N11CatalogId = product.N11CatalogId ?? string.Empty,
                N11ProductCode = product.N11ProductCode ?? string.Empty,
                SpareBarcode1 = product.SpareBarcode1 ?? string.Empty,
                SpareBarcode2 = product.SpareBarcode2 ?? string.Empty,
                SpareBarcode3 = product.SpareBarcode3 ?? string.Empty,
                SpareBarcode4 = product.SpareBarcode4 ?? string.Empty,
                LogoBarcodes = product.LogoBarcodes ?? string.Empty,
                KoctasEanBarcode = product.KoctasEanBarcode ?? string.Empty,
                KoctasEanIstanbulBarcode = product.KoctasEanIstanbulBarcode ?? string.Empty,
                PttUrunStokKodu = product.PttUrunStokKodu ?? string.Empty,
                IsArchived = product.IsArchived, // IsActive kaldırıldı
                CreatedDate = product.CreatedDate,
                UpdatedDate = product.UpdatedDate
            };

            // ImageUrls listesini ayrı ayrı URL'lere dönüştür
            SetImageUrlsFromList(xmlProduct, product.ImageUrls);

            return xmlProduct;
        }

        private void SetImageUrlsFromList(ProductXml xmlProduct, List<string> imageUrls)
        {
            if (imageUrls != null && imageUrls.Count > 0)
            {
                if (imageUrls.Count > 0) xmlProduct.ImageUrl1 = imageUrls[0];
                if (imageUrls.Count > 1) xmlProduct.ImageUrl2 = imageUrls[1];
                if (imageUrls.Count > 2) xmlProduct.ImageUrl3 = imageUrls[2];
                if (imageUrls.Count > 3) xmlProduct.ImageUrl4 = imageUrls[3];
                if (imageUrls.Count > 4) xmlProduct.ImageUrl5 = imageUrls[4];
                if (imageUrls.Count > 5) xmlProduct.ImageUrl6 = imageUrls[5];
                if (imageUrls.Count > 6) xmlProduct.ImageUrl7 = imageUrls[6];
                if (imageUrls.Count > 7) xmlProduct.ImageUrl8 = imageUrls[7];
                if (imageUrls.Count > 8) xmlProduct.ImageUrl9 = imageUrls[8];
                if (imageUrls.Count > 9) xmlProduct.ImageUrl10 = imageUrls[9];
            }
        }

        private string GenerateFileName(string extension, ExportFilter filter)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var prefix = "products";

            if (!string.IsNullOrEmpty(filter.Status))
                prefix = $"{filter.Status}_products";

            if (!string.IsNullOrEmpty(filter.Category))
                prefix = $"{prefix}_category_{filter.Category}";

            if (!string.IsNullOrEmpty(filter.Brand))
                prefix = $"{prefix}_brand_{filter.Brand}";

            // Dosya adını güvenli hale getir
            prefix = string.Join("_", prefix.Split(Path.GetInvalidFileNameChars()));

            return $"{prefix}_{timestamp}.{extension}";
        }

        private string GenerateFileName(string extension, ExportColumnFilter filter)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var prefix = "products";

            if (!string.IsNullOrEmpty(filter.Status))
                prefix = $"{filter.Status}_products";

            if (!string.IsNullOrEmpty(filter.Category))
                prefix = $"{prefix}_category_{filter.Category}";

            if (!string.IsNullOrEmpty(filter.Brand))
                prefix = $"{prefix}_brand_{filter.Brand}";

            // Sütun sayısını dosya adına ekle
            prefix = $"{prefix}_custom_{filter.SelectedColumns.Count}columns";

            // Dosya adını güvenli hale getir
            prefix = string.Join("_", prefix.Split(Path.GetInvalidFileNameChars()));

            return $"{prefix}_{timestamp}.{extension}";
        }

        private string ConvertToCustomXml(List<ExportProductData> exportData, List<string> selectedColumns)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<Products>");

            var selectedColumnDefinitions = _columnService.GetSelectedColumns(selectedColumns);

            foreach (var productData in exportData)
            {
                xml.AppendLine("  <Product>");

                // Standart seçili sütunları ekle
                foreach (var column in selectedColumnDefinitions.OrderBy(c => c.Order))
                {
                    var value = productData.GetValue<object>(column.PropertyName);
                    var stringValue = FormatValueForXml(value, column.DataType);
                    
                    xml.AppendLine($"    <{column.PropertyName}><![CDATA[{stringValue}]]></{column.PropertyName}>");
                }

                // Dinamik açıklama sütunlarını ekle (eğer veriler varsa)
                var htmlDescription = productData.GetValue<string>("Description_HTML");
                if (!string.IsNullOrEmpty(htmlDescription) && !selectedColumns.Contains("Description_HTML"))
                {
                    xml.AppendLine($"    <Description_HTML><![CDATA[{htmlDescription}]]></Description_HTML>");
                }

                var plainDescription = productData.GetValue<string>("Description_PlainText");
                if (!string.IsNullOrEmpty(plainDescription) && !selectedColumns.Contains("Description_PlainText"))
                {
                    xml.AppendLine($"    <Description_PlainText><![CDATA[{plainDescription}]]></Description_PlainText>");
                }

                xml.AppendLine("  </Product>");
            }

            xml.AppendLine("</Products>");
            return xml.ToString();
        }

        private string FormatValueForXml(object? value, string dataType)
        {
            if (value == null)
                return string.Empty;

            return dataType.ToLower() switch
            {
                "datetime" => value is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : value.ToString() ?? "",
                "bool" => value is bool b ? (b ? "true" : "false") : value.ToString() ?? "",
                "decimal" => value is decimal d ? d.ToString(CultureInfo.InvariantCulture) : value.ToString() ?? "",
                "int" => value is int i ? i.ToString() : value.ToString() ?? "",
                _ => value.ToString() ?? ""
            };
        }

        private string ConvertToCustomJson(List<ExportProductData> exportData, List<string> selectedColumns)
        {
            var jsonObjects = new List<Dictionary<string, object?>>();
            var selectedColumnDefinitions = _columnService.GetSelectedColumns(selectedColumns);

            foreach (var productData in exportData)
            {
                var jsonObject = new Dictionary<string, object?>();

                // Standart seçili sütunları ekle
                foreach (var column in selectedColumnDefinitions.OrderBy(c => c.Order))
                {
                    var value = productData.GetValue<object>(column.PropertyName);
                    jsonObject[column.PropertyName] = FormatValueForJson(value, column.DataType);
                }

                // Dinamik açıklama sütunlarını ekle (eğer veriler varsa)
                var htmlDescription = productData.GetValue<string>("Description_HTML");
                if (!string.IsNullOrEmpty(htmlDescription) && !selectedColumns.Contains("Description_HTML"))
                {
                    jsonObject["Description_HTML"] = htmlDescription;
                }

                var plainDescription = productData.GetValue<string>("Description_PlainText");
                if (!string.IsNullOrEmpty(plainDescription) && !selectedColumns.Contains("Description_PlainText"))
                {
                    jsonObject["Description_PlainText"] = plainDescription;
                }

                jsonObjects.Add(jsonObject);
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(jsonObjects, jsonOptions);
        }

        private object? FormatValueForJson(object? value, string dataType)
        {
            if (value == null)
                return null;

            return dataType.ToLower() switch
            {
                "datetime" => value is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : value.ToString(),
                "bool" => value is bool b ? b : bool.TryParse(value.ToString(), out var boolVal) ? boolVal : false,
                "decimal" => value is decimal d ? d : decimal.TryParse(value.ToString(), out var decVal) ? decVal : 0m,
                "int" => value is int i ? i : int.TryParse(value.ToString(), out var intVal) ? intVal : 0,
                _ => value.ToString()
            };
        }

        private string ConvertToCustomCsv(List<ExportProductData> exportData, List<string> selectedColumns)
        {
            var csv = new StringBuilder();
            var selectedColumnDefinitions = _columnService.GetSelectedColumns(selectedColumns);

            // Dinamik açıklama sütunlarını da başlıklara ekle
            var allHeaders = new List<string>();
            allHeaders.AddRange(selectedColumnDefinitions.OrderBy(c => c.Order).Select(c => c.DisplayName));
            
            // Açıklama sütunlarını kontrol et
            bool hasHtmlDescription = exportData.Any(p => !string.IsNullOrEmpty(p.GetValue<string>("Description_HTML"))) && !selectedColumns.Contains("Description_HTML");
            bool hasPlainDescription = exportData.Any(p => !string.IsNullOrEmpty(p.GetValue<string>("Description_PlainText"))) && !selectedColumns.Contains("Description_PlainText");
            
            if (hasHtmlDescription) allHeaders.Add("Açıklama (HTML)");
            if (hasPlainDescription) allHeaders.Add("Açıklama (Düz Metin)");

            // CSV başlıkları
            csv.AppendLine(string.Join(",", allHeaders.Select(EscapeCsvField)));

            // Veri satırları
            foreach (var productData in exportData)
            {
                var values = new List<string>();
                
                // Standart seçili sütunlar
                foreach (var column in selectedColumnDefinitions.OrderBy(c => c.Order))
                {
                    var value = productData.GetValue<object>(column.PropertyName);
                    var stringValue = FormatValueForCsv(value, column.DataType);
                    values.Add(EscapeCsvField(stringValue));
                }

                // Dinamik açıklama sütunları
                if (hasHtmlDescription)
                {
                    var htmlDesc = productData.GetValue<string>("Description_HTML") ?? "";
                    values.Add(EscapeCsvField(htmlDesc));
                }

                if (hasPlainDescription)
                {
                    var plainDesc = productData.GetValue<string>("Description_PlainText") ?? "";
                    values.Add(EscapeCsvField(plainDesc));
                }

                csv.AppendLine(string.Join(",", values));
            }

            return csv.ToString();
        }

        private string FormatValueForCsv(object? value, string dataType)
        {
            if (value == null)
                return string.Empty;

            return dataType.ToLower() switch
            {
                "datetime" => value is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm:ss") : value.ToString() ?? "",
                "bool" => value is bool b ? (b ? "Evet" : "Hayır") : value.ToString() ?? "",
                "decimal" => value is decimal d ? d.ToString(CultureInfo.InvariantCulture) : value.ToString() ?? "",
                "int" => value is int i ? i.ToString() : value.ToString() ?? "",
                _ => value.ToString() ?? ""
            };
        }

        /// <summary>
        /// Logo barkodları string'inden belirli index'teki değeri alır
        /// </summary>
        private string GetLogoBarcodeByIndex(string logoBarcodes, int index)
        {
            if (string.IsNullOrEmpty(logoBarcodes))
                return string.Empty;

            try
            {
                // JSON formatında ise parse et
                if (logoBarcodes.Trim().StartsWith("["))
                {
                    var barcodeList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(logoBarcodes);
                    return barcodeList != null && index < barcodeList.Count ? barcodeList[index] : string.Empty;
                }
                else if (logoBarcodes.Contains(","))
                {
                    // Virgül ile ayrılmış format
                    var barcodes = logoBarcodes.Split(',')
                        .Select(x => x.Trim())
                        .ToList();
                    return index < barcodes.Count ? barcodes[index] : string.Empty;
                }
                else
                {
                    // Satır satır ayrılmış format
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

        /// <summary>
        /// ImageUrls listesinden belirli index'teki değeri alır
        /// </summary>
        private string GetImageUrlByIndex(List<string> imageUrls, int index)
        {
            if (imageUrls == null || index < 0 || index >= imageUrls.Count)
                return string.Empty;
            
            return imageUrls[index] ?? string.Empty;
        }

        /// <summary>
        /// MarketplaceImageUrls listesinden belirli index'teki değeri alır
        /// </summary>
        private string GetMarketplaceImageUrlByIndex(List<string> marketplaceImageUrls, int index)
        {
            if (marketplaceImageUrls == null || index < 0 || index >= marketplaceImageUrls.Count)
                return string.Empty;
            
            return marketplaceImageUrls[index] ?? string.Empty;
        }

        /// <summary>
        /// VideoUrls listesinden belirli index'teki değeri alır
        /// </summary>
        private string GetVideoUrlByIndex(List<string> videoUrls, int index)
        {
            if (videoUrls == null || index < 0 || index >= videoUrls.Count)
                return string.Empty;
            
            return videoUrls[index] ?? string.Empty;
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            // Çift tırnak, virgül veya yeni satır içeriyorsa escape et
            if (field.Contains("\"") || field.Contains(",") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        // HTML temizleme ve formatlama metodları
        private string CleanHtmlForExport(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            string cleaned = html;
            
            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTML temizleme hatası");
                return html; // Hata durumunda orijinal HTML'i döndür
            }
        }

        private string StripHtmlTagsForExport(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            try
            {
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
                cleaned = WebUtility.HtmlDecode(cleaned);
                
                return cleaned.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTML temizleme hatası");
                return Regex.Replace(html, @"<.*?>", string.Empty).Trim();
            }
        }
        #endregion
    }

    #region Model Classes
    public class ExportFilter
    {
        public string? Status { get; set; } // "all", "active", "archived"
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class ExportOptions
    {
        public string Format { get; set; } = "xml";
        public string Status { get; set; } = "all";
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public List<string> SelectedColumns { get; set; } = new List<string>();
        public bool IncludeImages { get; set; } = true;
        public bool CompressOutput { get; set; } = false;
    }

    public class ExportResult
    {
        public bool Success { get; set; }
        public byte[]? Content { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public int RecordCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ExportUtf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
    #endregion
}
