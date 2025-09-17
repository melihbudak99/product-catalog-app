using System.Reflection;
using System.Text.RegularExpressions;
using product_catalog_app.src.models;

namespace product_catalog_app.src.services
{
    public class ExportColumnService
    {
        private readonly List<ExportColumn> _availableColumns;

        public ExportColumnService()
        {
            _availableColumns = InitializeColumns();
        }

        /// <summary>
        /// Tüm mevcut sütunları döndürür
        /// </summary>
        public List<ExportColumn> GetAvailableColumns()
        {
            return _availableColumns.ToList();
        }

        /// <summary>
        /// Kategoriye göre sütunları döndürür
        /// </summary>
        public List<ExportColumn> GetColumnsByCategory(string category)
        {
            return _availableColumns.Where(c => c.Category == category).ToList();
        }

        /// <summary>
        /// Seçili sütunları döndürür
        /// </summary>
        public List<ExportColumn> GetSelectedColumns(List<string> selectedColumnNames)
        {
            return _availableColumns.Where(c => selectedColumnNames.Contains(c.PropertyName)).ToList();
        }

        /// <summary>
        /// Varsayılan sütun seçimini döndürür
        /// </summary>
        public List<string> GetDefaultSelectedColumns()
        {
            // Varsayılan seçilmiş sütunlar
            var defaultColumns = new List<string>
            {
                // Temel Bilgiler
                "EanCode",
                "Id", 
                "Name",
                "SKU",
                "Brand",
                "Category",
                
                // Genel Özellikler
                "Desi",
                
                // Pazaryeri Barkodları
                "TrendyolBarcode",
                "HepsiburadaBarcode",
                "HepsiburadaSellerStockCode",
                "KoctasBarcode",
                "KoctasIstanbulBarcode",
                "KoctasEanBarcode",
                "KoctasEanIstanbulBarcode",
                "HepsiburadaTedarikBarcode",
                "PttAvmBarcode",
                "PttUrunStokKodu",
                "PazaramaBarcode",
                "HaceyapiBarcode",
                "AmazonBarcode",
                "N11CatalogId",
                "N11ProductCode",
                "EntegraUrunId",
                "EntegraUrunKodu", 
                "EntegraBarkod",
                "SpareBarcode1",
                "SpareBarcode2",
                "SpareBarcode3",
                "SpareBarcode4"
            };

            return defaultColumns;
        }

        /// <summary>
        /// HTML açıklamayı plain text'e çevirir
        /// </summary>
        public string ConvertHtmlToPlainText(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return string.Empty;

            var plainText = htmlContent;

            try
            {
                Console.WriteLine("🔄 HTML to PlainText dönüşümü başlatıldı...");
                Console.WriteLine($"📝 Orjinal HTML: {htmlContent.Substring(0, Math.Min(100, htmlContent.Length))}...");

                // Remove Microsoft Word specific classes and styles
                plainText = Regex.Replace(plainText, @"class=""MsoNormal""", "", RegexOptions.IgnoreCase);
                plainText = Regex.Replace(plainText, @"style=""[^""]*mso-[^""]*""", "", RegexOptions.IgnoreCase);
                
                // Fix nested paragraph tags - remove inner p tags
                plainText = Regex.Replace(plainText, @"<p[^>]*>(\s*<p[^>]*>)+", "<p>", RegexOptions.IgnoreCase);
                plainText = Regex.Replace(plainText, @"(</p>\s*)+</p>", "</p>", RegexOptions.IgnoreCase);
                
                // Remove empty paragraphs
                plainText = Regex.Replace(plainText, @"<p[^>]*>\s*</p>", "", RegexOptions.IgnoreCase);
                
                // Fix list structure - move ul outside of p tags  
                plainText = Regex.Replace(plainText, @"<p[^>]*>(\s*<ul>)", "$1", RegexOptions.IgnoreCase);
                plainText = Regex.Replace(plainText, @"(</ul>\s*)</p>", "$1", RegexOptions.IgnoreCase);
                
                // Process indent classes before removing HTML tags
                plainText = ProcessIndentClassesToPlainText(plainText);
                
                // Replace list items with bullet points
                plainText = Regex.Replace(plainText, @"<li[^>]*>", "• ", RegexOptions.IgnoreCase);
                plainText = Regex.Replace(plainText, @"</li>", "\n", RegexOptions.IgnoreCase);
                
                // Replace paragraph breaks with line breaks
                plainText = Regex.Replace(plainText, @"</p>\s*<p[^>]*>", "\n\n", RegexOptions.IgnoreCase);
                plainText = Regex.Replace(plainText, @"<p[^>]*>", "", RegexOptions.IgnoreCase);
                plainText = Regex.Replace(plainText, @"</p>", "\n", RegexOptions.IgnoreCase);
                
                // Replace line breaks
                plainText = Regex.Replace(plainText, @"<br\s*/?>\s*", "\n", RegexOptions.IgnoreCase);
                
                // Remove all remaining HTML tags
                plainText = Regex.Replace(plainText, @"<[^>]+>", "", RegexOptions.IgnoreCase);
                
                // Clean up whitespace
                plainText = Regex.Replace(plainText, @"\n\s*\n", "\n\n", RegexOptions.Multiline);
                plainText = Regex.Replace(plainText, @"^\s+|\s+$", "", RegexOptions.Multiline);
                
                // Decode HTML entities
                plainText = System.Net.WebUtility.HtmlDecode(plainText);
                
                Console.WriteLine($"✅ Temizlenmiş PlainText: {plainText.Substring(0, Math.Min(100, plainText.Length))}...");
                
                return plainText.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTML temizleme hatası: {ex.Message}");
                // Hata durumunda basit temizleme yap
                plainText = Regex.Replace(htmlContent, @"<[^>]*>", "");
                plainText = System.Net.WebUtility.HtmlDecode(plainText);
                return Regex.Replace(plainText, @"\s+", " ").Trim();
            }
        }

        /// <summary>
        /// HTML export için temizler ve formatlar
        /// </summary>
        public string CleanHtmlForExport(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return string.Empty;

            string cleaned = htmlContent;
            
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
                
                // Remove style attributes but preserve indent classes
                cleaned = Regex.Replace(cleaned, @"\s*style=""[^""]*""", "", RegexOptions.IgnoreCase);
                
                // Preserve indent-level classes for export compatibility
                // These classes will be properly converted to plain text indentation
                
                return cleaned.Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ HTML temizleme hatası: {ex.Message}");
                return htmlContent; // Hata durumunda orijinal HTML'i döndür
            }
        }

        /// <summary>
        /// Girinti CSS class'larını plain text girinti formatına çevirir
        /// </summary>
        private string ProcessIndentClassesToPlainText(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return string.Empty;

            var processed = htmlContent;
            
            try
            {
                // indent-level-1 to indent-level-5 class'larını işle
                for (int level = 1; level <= 5; level++)
                {
                    var indentSpaces = new string(' ', level * 4); // Her seviye için 4 boşluk
                    var pattern = $@"<([^>]+)\s+class=""[^""]*indent-level-{level}[^""]*""([^>]*)>";
                    
                    // Class'ı kaldır ve içeriği girinti ile değiştir
                    processed = Regex.Replace(processed, pattern, (match) =>
                    {
                        var tag = match.Groups[1].Value;
                        var attributes = match.Groups[2].Value;
                        
                        // Class attribute'unu temizle
                        attributes = Regex.Replace(attributes, @"\s*class=""[^""]*""", "", RegexOptions.IgnoreCase);
                        
                        return $"<{tag}{attributes}>{indentSpaces}";
                    }, RegexOptions.IgnoreCase);
                }
                
                Console.WriteLine($"✅ Girinti class'ları plain text'e dönüştürüldü");
                return processed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Girinti işleme hatası: {ex.Message}");
                return htmlContent; // Hata durumunda orijinal içeriği döndür
            }
        }

        /// <summary>
        /// Product nesnesinden değer alır
        /// </summary>
        public object? GetProductValue(Product product, string propertyName)
        {
            try
            {
                // Özel durumlar için kontrol
                switch (propertyName)
                {
                    // Logo barkodları - ayrı indeksli erişim
                    case "LogoBarcode1":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 0);
                    case "LogoBarcode2":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 1);
                    case "LogoBarcode3":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 2);
                    case "LogoBarcode4":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 3);
                    case "LogoBarcode5":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 4);
                    case "LogoBarcode6":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 5);
                    case "LogoBarcode7":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 6);
                    case "LogoBarcode8":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 7);
                    case "LogoBarcode9":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 8);
                    case "LogoBarcode10":
                        return GetLogoBarcodeByIndex(product.LogoBarcodes, 9);
                    
                    // Ürün görselleri - List'ten ayrı indeksli erişim
                    case "ProductImage1":
                        return GetImageUrlByIndex(product.ImageUrls, 0);
                    case "ProductImage2":
                        return GetImageUrlByIndex(product.ImageUrls, 1);
                    case "ProductImage3":
                        return GetImageUrlByIndex(product.ImageUrls, 2);
                    case "ProductImage4":
                        return GetImageUrlByIndex(product.ImageUrls, 3);
                    case "ProductImage5":
                        return GetImageUrlByIndex(product.ImageUrls, 4);
                    case "ProductImage6":
                        return GetImageUrlByIndex(product.ImageUrls, 5);
                    case "ProductImage7":
                        return GetImageUrlByIndex(product.ImageUrls, 6);
                    case "ProductImage8":
                        return GetImageUrlByIndex(product.ImageUrls, 7);
                    case "ProductImage9":
                        return GetImageUrlByIndex(product.ImageUrls, 8);
                    case "ProductImage10":
                        return GetImageUrlByIndex(product.ImageUrls, 9);
                    
                    // Pazaryeri görselleri - List'ten ayrı indeksli erişim
                    case "MarketplaceImage1":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 0);
                    case "MarketplaceImage2":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 1);
                    case "MarketplaceImage3":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 2);
                    case "MarketplaceImage4":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 3);
                    case "MarketplaceImage5":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 4);
                    case "MarketplaceImage6":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 5);
                    case "MarketplaceImage7":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 6);
                    case "MarketplaceImage8":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 7);
                    case "MarketplaceImage9":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 8);
                    case "MarketplaceImage10":
                        return GetImageUrlByIndex(product.MarketplaceImageUrls, 9);
                    
                    // Video URL'leri - List'ten ayrı indeksli erişim
                    case "Video1":
                        return GetImageUrlByIndex(product.VideoUrls, 0);
                    case "Video2":
                        return GetImageUrlByIndex(product.VideoUrls, 1);
                    case "Video3":
                        return GetImageUrlByIndex(product.VideoUrls, 2);
                    case "Video4":
                        return GetImageUrlByIndex(product.VideoUrls, 3);
                    case "Video5":
                        return GetImageUrlByIndex(product.VideoUrls, 4);
                    
                    default:
                        // Reflection ile değer al
                        var propertyInfo = typeof(Product).GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            var value = propertyInfo.GetValue(product);
                            
                            // List<string> türündeki değerleri olduğu gibi döndür
                            if (value is List<string> stringList)
                            {
                                return stringList; // List olarak döndür, join yapmayacağız
                            }
                            
                            return value;
                        }
                        break;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Export verilerini hazırlar
        /// </summary>
        public List<ExportProductData> PrepareExportData(List<Product> products, List<string> selectedColumns, DescriptionExportOptions? descriptionOptions = null)
        {
            var exportData = new List<ExportProductData>();
            var selectedColumnDefinitions = GetSelectedColumns(selectedColumns);

            // Açıklama seçenekleri varsayılan değerler
            descriptionOptions ??= new DescriptionExportOptions();

            foreach (var product in products)
            {
                var productData = new ExportProductData();

                foreach (var column in selectedColumnDefinitions.OrderBy(c => c.Order))
                {
                    var value = GetProductValue(product, column.PropertyName);
                    
                    // Özel açıklama işleme - sadece HTML ve PlainText sütunları için
                    if (column.PropertyName == "Description_HTML" && !string.IsNullOrEmpty(product.Description))
                    {
                        // HTML formatında açıklama - temizlenmiş HTML
                        var cleanedHtml = CleanHtmlForExport(product.Description);
                        productData.SetValue(column.PropertyName, cleanedHtml);
                    }
                    else if (column.PropertyName == "Description_PlainText" && !string.IsNullOrEmpty(product.Description))
                    {
                        // Düz metin formatında açıklama
                        var plainText = ConvertHtmlToPlainText(product.Description);
                        productData.SetValue(column.PropertyName, plainText);
                    }
                    else
                    {
                        // Diğer tüm sütunlar normal şekilde işlenir
                        productData.SetValue(column.PropertyName, value);
                    }
                }

                // Eğer açıklama seçenekleri aktifse ama sütunlar seçilmemişse, dinamik olarak ekle
                if (descriptionOptions.IncludeHtml && !selectedColumns.Contains("Description_HTML") && !string.IsNullOrEmpty(product.Description))
                {
                    var cleanedHtml = CleanHtmlForExport(product.Description);
                    productData.SetValue(descriptionOptions.HtmlColumnName, cleanedHtml);
                }

                if (descriptionOptions.IncludePlainText && !selectedColumns.Contains("Description_PlainText") && !string.IsNullOrEmpty(product.Description))
                {
                    var plainText = ConvertHtmlToPlainText(product.Description);
                    productData.SetValue(descriptionOptions.PlainTextColumnName, plainText);
                }

                exportData.Add(productData);
            }

            return exportData;
        }

        /// <summary>
        /// Sütun tanımlarını başlatır - Yeni kategori düzeni
        /// </summary>
        private List<ExportColumn> InitializeColumns()
        {
            var columns = new List<ExportColumn>();

            // Tarihler
            columns.AddRange(new[]
            {
                new ExportColumn { PropertyName = "CreatedDate", DisplayName = "Oluşturma Tarihi", Category = "Tarihler", IsSelected = false, DataType = "datetime", Order = 1 },
                new ExportColumn { PropertyName = "UpdatedDate", DisplayName = "Güncelleme Tarihi", Category = "Tarihler", IsSelected = false, DataType = "datetime", Order = 2 },
            });

            // Statü
            columns.AddRange(new[]
            {
                new ExportColumn { PropertyName = "IsArchived", DisplayName = "Arşivlenmiş", Category = "Statü", IsSelected = false, DataType = "bool", Order = 11 },
            });

            // Temel Bilgiler
            columns.AddRange(new[]
            {
                new ExportColumn { PropertyName = "EanCode", DisplayName = "EAN Kodu", Category = "Temel Bilgiler", IsSelected = true, DataType = "string", Order = 20 },
                new ExportColumn { PropertyName = "Id", DisplayName = "ID", Category = "Temel Bilgiler", IsRequired = true, DataType = "int", Order = 21 },
                new ExportColumn { PropertyName = "Name", DisplayName = "Ürün Adı", Category = "Temel Bilgiler", IsRequired = true, DataType = "string", Order = 22 },
                new ExportColumn { PropertyName = "SKU", DisplayName = "SKU", Category = "Temel Bilgiler", IsSelected = true, DataType = "string", Order = 23 },
                new ExportColumn { PropertyName = "Brand", DisplayName = "Marka", Category = "Temel Bilgiler", IsSelected = true, DataType = "string", Order = 24 },
                new ExportColumn { PropertyName = "Category", DisplayName = "Kategori", Category = "Temel Bilgiler", IsSelected = true, DataType = "string", Order = 25 },
            });

            // Açıklama ve Notlar
            columns.AddRange(new[]
            {
                new ExportColumn { PropertyName = "Description_HTML", DisplayName = "Açıklama (HTML)", Category = "Açıklama ve Notlar", IsSelected = false, DataType = "string", Order = 30 },
                new ExportColumn { PropertyName = "Description_PlainText", DisplayName = "Açıklama (Düz Metin)", Category = "Açıklama ve Notlar", IsSelected = false, DataType = "string", Order = 31 },
                new ExportColumn { PropertyName = "Features", DisplayName = "Özellikler", Category = "Açıklama ve Notlar", IsSelected = false, DataType = "string", Order = 32 },
                new ExportColumn { PropertyName = "Notes", DisplayName = "Notlar", Category = "Açıklama ve Notlar", IsSelected = false, DataType = "string", Order = 33 },
            });

            // Genel Özellikler
            columns.AddRange(new[]
            {
                new ExportColumn { PropertyName = "Weight", DisplayName = "Ağırlık (kg)", Category = "Genel Özellikler", IsSelected = false, DataType = "decimal", Order = 40 },
                new ExportColumn { PropertyName = "Desi", DisplayName = "Desi", Category = "Genel Özellikler", IsSelected = false, DataType = "decimal", Order = 41 },
                new ExportColumn { PropertyName = "Width", DisplayName = "Genişlik (cm)", Category = "Genel Özellikler", IsSelected = false, DataType = "decimal", Order = 42 },
                new ExportColumn { PropertyName = "Height", DisplayName = "Yükseklik (cm)", Category = "Genel Özellikler", IsSelected = false, DataType = "decimal", Order = 43 },
                new ExportColumn { PropertyName = "Depth", DisplayName = "En (cm)", Category = "Genel Özellikler", IsSelected = false, DataType = "decimal", Order = 44 },
                new ExportColumn { PropertyName = "Length", DisplayName = "Uzunluk (cm)", Category = "Genel Özellikler", IsSelected = false, DataType = "decimal", Order = 45 },
                new ExportColumn { PropertyName = "Material", DisplayName = "Malzeme", Category = "Genel Özellikler", IsSelected = false, DataType = "string", Order = 46 },
                new ExportColumn { PropertyName = "Color", DisplayName = "Renk", Category = "Genel Özellikler", IsSelected = false, DataType = "string", Order = 47 },
                new ExportColumn { PropertyName = "WarrantyMonths", DisplayName = "Garanti", Category = "Genel Özellikler", IsSelected = false, DataType = "int", Order = 48 },
            });

            // Ürün Özellikleri (Kabin Tipi kaldırıldı)
            var productFeatures = new[]
            {
                ("KlozetKanalYapisi", "Klozet Kanal Yapısı"),
                ("KlozetTipi", "Klozet Tipi"),
                ("KlozetKapakCinsi", "Klozet Kapak Cinsi"),
                ("KlozetMontajTipi", "Klozet Montaj Tipi"),
                ("LawaboSuTasmaDeligi", "Lavabo Su Taşma Deliği"),
                ("LawaboArmaturDeligi", "Lavabo Armatur Deliği"),
                ("LawaboTipi", "Lavabo Tipi"),
                ("LawaboOzelligi", "Lavabo Özelliği"),
                ("BataryaCikisUcuUzunlugu", "Batarya Çıkış Ucu Uzunluğu"),
                ("BataryaYuksekligi", "Batarya Yüksekliği"),
            };

            int productFeatureOrder = 50;
            foreach (var (prop, display) in productFeatures)
            {
                columns.Add(new ExportColumn 
                { 
                    PropertyName = prop, 
                    DisplayName = display, 
                    Category = "Ürün Özellikleri", 
                    IsSelected = false, 
                    DataType = "string", 
                    Order = productFeatureOrder++ 
                });
            }

            // Pazaryeri Barkodları
            var marketplaceBarcodes = new[]
            {
                ("TrendyolBarcode", "Trendyol Barkod"),
                ("HepsiburadaBarcode", "Hepsiburada Barkod"),
                ("HepsiburadaSellerStockCode", "Hepsiburada Satıcı Stok Kodu"),
                ("KoctasBarcode", "Koçtaş Barkod"),
                ("KoctasIstanbulBarcode", "Koçtaş İstanbul Barkod"),
                ("KoctasEanBarcode", "Koçtaş EAN Barkod"),
                ("KoctasEanIstanbulBarcode", "Koçtaş EAN İstanbul Barkod"),
                ("HepsiburadaTedarikBarcode", "Hepsiburada Tedarik Barkod"),
                ("PttAvmBarcode", "PTT AVM Barkod"),
                ("PttUrunStokKodu", "PTT Ürün ID"),
                ("PazaramaBarcode", "Pazarama Barkod"),
                ("HaceyapiBarcode", "Haceyapı Barkod"),
                ("AmazonBarcode", "Amazon Barkod"),
                ("N11CatalogId", "N11 Katalog ID"),
                ("N11ProductCode", "N11 Ürün Kodu"),
                ("EntegraUrunId", "Entegra Ürün ID"),
                ("EntegraUrunKodu", "Entegra Ürün Kodu"),
                ("EntegraBarkod", "Entegra Barkod"),
                ("SpareBarcode1", "Yedek Barkod 1"),
                ("SpareBarcode2", "Yedek Barkod 2"),
                ("SpareBarcode3", "Yedek Barkod 3"),
                ("SpareBarcode4", "Yedek Barkod 4"),
            };

            int barcodeOrder = 60;
            foreach (var (prop, display) in marketplaceBarcodes)
            {
                columns.Add(new ExportColumn 
                { 
                    PropertyName = prop, 
                    DisplayName = display, 
                    Category = "Pazaryeri Barkodları", 
                    IsSelected = true, 
                    DataType = "string", 
                    Order = barcodeOrder++ 
                });
            }

            // Logo Barkodları
            for (int i = 1; i <= 10; i++)
            {
                columns.Add(new ExportColumn 
                { 
                    PropertyName = $"LogoBarcode{i}", 
                    DisplayName = $"Logo Barkodu {i}", 
                    Category = "Logo Barkodları", 
                    IsSelected = true, 
                    DataType = "string", 
                    Order = 80 + i 
                });
            }

            // Ürün Görselleri
            for (int i = 1; i <= 10; i++)
            {
                columns.Add(new ExportColumn 
                { 
                    PropertyName = $"ProductImage{i}", 
                    DisplayName = $"Ürün Görseli {i}", 
                    Category = "Ürün Görselleri", 
                    IsSelected = false, 
                    DataType = "string", 
                    Order = 90 + i 
                });
            }

            // Pazaryeri Görselleri
            for (int i = 1; i <= 10; i++)
            {
                columns.Add(new ExportColumn 
                { 
                    PropertyName = $"MarketplaceImage{i}", 
                    DisplayName = $"Pazaryeri Görseli {i}", 
                    Category = "Pazaryeri Görselleri", 
                    IsSelected = false, 
                    DataType = "string", 
                    Order = 100 + i 
                });
            }

            // Videolar
            for (int i = 1; i <= 5; i++)
            {
                columns.Add(new ExportColumn 
                { 
                    PropertyName = $"Video{i}", 
                    DisplayName = $"Video {i}", 
                    Category = "Videolar", 
                    IsSelected = false, 
                    DataType = "string", 
                    Order = 110 + i 
                });
            }

            return columns;
        }

        /// <summary>
        /// Logo barkodları JSON string'inden belirli index'teki değeri alır
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
        /// URL listesinden belirli index'teki değeri alır
        /// </summary>
        private string GetImageUrlByIndex(List<string>? urlList, int index)
        {
            if (urlList == null || index < 0 || index >= urlList.Count)
                return string.Empty;
                
            return urlList[index] ?? string.Empty;
        }
    }
}
