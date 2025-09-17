using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using product_catalog_app.src.models;
using product_catalog_app.src.interfaces;
using product_catalog_app.src.common;

namespace product_catalog_app.src.data
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ProductDbContext context, ILogger<ProductRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Helper Methods

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
        /// EF Core için Türkçe karakter destekli arama terimi filtresi
        /// StringComparison.OrdinalIgnoreCase kullanarak kültür-bağımsız arama yapar
        /// </summary>
        private static IQueryable<Product> ApplySearchTermFilter(IQueryable<Product> query, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm)) return query;

            // Trim the search term
            var searchTermTrimmed = searchTerm.Trim();

            // Split search term into words for better partial matching
            var searchWords = searchTermTrimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (searchWords.Length == 1)
            {
                // Single word search - use EF.Functions.Like for case-insensitive search
                var word = searchWords[0];
                var normalizedWord = NormalizeSearchTerm(word);
                var likePattern = $"%{word}%";
                var normalizedLikePattern = $"%{normalizedWord}%";
                
                return query.Where(p =>
                    // Name field - Türkçe karakter destekli arama
                    (p.Name != null && (EF.Functions.Like(p.Name, likePattern) || EF.Functions.Like(p.Name, normalizedLikePattern))) ||
                    // SKU field
                    (p.SKU != null && (EF.Functions.Like(p.SKU, likePattern) || EF.Functions.Like(p.SKU, normalizedLikePattern))) ||
                    // Brand field - Türkçe karakter destekli arama
                    (p.Brand != null && (EF.Functions.Like(p.Brand, likePattern) || EF.Functions.Like(p.Brand, normalizedLikePattern))) ||
                    // EanCode field
                    (p.EanCode != null && EF.Functions.Like(p.EanCode, likePattern)) ||
                    // Description field - Türkçe karakter destekli arama
                    (p.Description != null && (EF.Functions.Like(p.Description, likePattern) || EF.Functions.Like(p.Description, normalizedLikePattern))) ||
                    // Features field - Türkçe karakter destekli arama
                    (p.Features != null && (EF.Functions.Like(p.Features, likePattern) || EF.Functions.Like(p.Features, normalizedLikePattern))) ||
                    // Material field - Türkçe karakter destekli arama
                    (p.Material != null && (EF.Functions.Like(p.Material, likePattern) || EF.Functions.Like(p.Material, normalizedLikePattern))) ||
                    // Color field - Türkçe karakter destekli arama
                    (p.Color != null && (EF.Functions.Like(p.Color, likePattern) || EF.Functions.Like(p.Color, normalizedLikePattern))) ||
                    // Notes field - Türkçe karakter destekli arama
                    (p.Notes != null && (EF.Functions.Like(p.Notes, likePattern) || EF.Functions.Like(p.Notes, normalizedLikePattern))) ||
                    // Category field - Türkçe karakter destekli arama
                    (p.Category != null && (EF.Functions.Like(p.Category, likePattern) || EF.Functions.Like(p.Category, normalizedLikePattern))) ||
                    // Marketplace barkodları
                    (p.AmazonBarcode != null && EF.Functions.Like(p.AmazonBarcode, likePattern)) ||
                    (p.HaceyapiBarcode != null && EF.Functions.Like(p.HaceyapiBarcode, likePattern)) ||
                    (p.HepsiburadaBarcode != null && EF.Functions.Like(p.HepsiburadaBarcode, likePattern)) ||
                    (p.HepsiburadaSellerStockCode != null && EF.Functions.Like(p.HepsiburadaSellerStockCode, likePattern)) ||
                    (p.HepsiburadaTedarikBarcode != null && EF.Functions.Like(p.HepsiburadaTedarikBarcode, likePattern)) ||
                    (p.KoctasBarcode != null && EF.Functions.Like(p.KoctasBarcode, likePattern)) ||
                    (p.KoctasIstanbulBarcode != null && EF.Functions.Like(p.KoctasIstanbulBarcode, likePattern)) ||
                    (p.N11CatalogId != null && EF.Functions.Like(p.N11CatalogId, likePattern)) ||
                    (p.N11ProductCode != null && EF.Functions.Like(p.N11ProductCode, likePattern)) ||
                    (p.PazaramaBarcode != null && EF.Functions.Like(p.PazaramaBarcode, likePattern)) ||
                    (p.PttAvmBarcode != null && EF.Functions.Like(p.PttAvmBarcode, likePattern)) ||
                    (p.TrendyolBarcode != null && EF.Functions.Like(p.TrendyolBarcode, likePattern)) ||
                    (p.SpareBarcode1 != null && EF.Functions.Like(p.SpareBarcode1, likePattern)) ||
                    (p.SpareBarcode2 != null && EF.Functions.Like(p.SpareBarcode2, likePattern)) ||
                    (p.SpareBarcode3 != null && EF.Functions.Like(p.SpareBarcode3, likePattern)) ||
                    (p.SpareBarcode4 != null && EF.Functions.Like(p.SpareBarcode4, likePattern)) ||
                    (p.LogoBarcodes != null && EF.Functions.Like(p.LogoBarcodes, likePattern))
                );
            }
            else
            {
                // Multi-word search - each word must be found in at least one field
                foreach (var word in searchWords)
                {
                    var normalizedWord = NormalizeSearchTerm(word);
                    var likePattern = $"%{word}%";
                    var normalizedLikePattern = $"%{normalizedWord}%";
                    query = query.Where(p =>
                        // Name field - Türkçe karakter destekli arama
                        (p.Name != null && (EF.Functions.Like(p.Name, likePattern) || EF.Functions.Like(p.Name, normalizedLikePattern))) ||
                        // SKU field
                        (p.SKU != null && (EF.Functions.Like(p.SKU, likePattern) || EF.Functions.Like(p.SKU, normalizedLikePattern))) ||
                        // Brand field - Türkçe karakter destekli arama
                        (p.Brand != null && (EF.Functions.Like(p.Brand, likePattern) || EF.Functions.Like(p.Brand, normalizedLikePattern))) ||
                        // EanCode field
                        (p.EanCode != null && EF.Functions.Like(p.EanCode, likePattern)) ||
                        // Description field - Türkçe karakter destekli arama
                        (p.Description != null && (EF.Functions.Like(p.Description, likePattern) || EF.Functions.Like(p.Description, normalizedLikePattern))) ||
                        // Features field - Türkçe karakter destekli arama
                        (p.Features != null && (EF.Functions.Like(p.Features, likePattern) || EF.Functions.Like(p.Features, normalizedLikePattern))) ||
                        // Material field - Türkçe karakter destekli arama
                        (p.Material != null && (EF.Functions.Like(p.Material, likePattern) || EF.Functions.Like(p.Material, normalizedLikePattern))) ||
                        // Color field - Türkçe karakter destekli arama
                        (p.Color != null && (EF.Functions.Like(p.Color, likePattern) || EF.Functions.Like(p.Color, normalizedLikePattern))) ||
                        // Notes field - Türkçe karakter destekli arama
                        (p.Notes != null && (EF.Functions.Like(p.Notes, likePattern) || EF.Functions.Like(p.Notes, normalizedLikePattern))) ||
                        // Category field - Türkçe karakter destekli arama
                        (p.Category != null && (EF.Functions.Like(p.Category, likePattern) || EF.Functions.Like(p.Category, normalizedLikePattern))) ||
                        // Marketplace barkodları
                        (p.AmazonBarcode != null && EF.Functions.Like(p.AmazonBarcode, likePattern)) ||
                        (p.HaceyapiBarcode != null && EF.Functions.Like(p.HaceyapiBarcode, likePattern)) ||
                        (p.HepsiburadaBarcode != null && EF.Functions.Like(p.HepsiburadaBarcode, likePattern)) ||
                        (p.HepsiburadaSellerStockCode != null && EF.Functions.Like(p.HepsiburadaSellerStockCode, likePattern)) ||
                        (p.HepsiburadaTedarikBarcode != null && EF.Functions.Like(p.HepsiburadaTedarikBarcode, likePattern)) ||
                        (p.KoctasBarcode != null && EF.Functions.Like(p.KoctasBarcode, likePattern)) ||
                        (p.KoctasIstanbulBarcode != null && EF.Functions.Like(p.KoctasIstanbulBarcode, likePattern)) ||
                        (p.N11CatalogId != null && EF.Functions.Like(p.N11CatalogId, likePattern)) ||
                        (p.N11ProductCode != null && EF.Functions.Like(p.N11ProductCode, likePattern)) ||
                        (p.PazaramaBarcode != null && EF.Functions.Like(p.PazaramaBarcode, likePattern)) ||
                        (p.PttAvmBarcode != null && EF.Functions.Like(p.PttAvmBarcode, likePattern)) ||
                        (p.TrendyolBarcode != null && EF.Functions.Like(p.TrendyolBarcode, likePattern)) ||
                        (p.SpareBarcode1 != null && EF.Functions.Like(p.SpareBarcode1, likePattern)) ||
                        (p.SpareBarcode2 != null && EF.Functions.Like(p.SpareBarcode2, likePattern)) ||
                        (p.SpareBarcode3 != null && EF.Functions.Like(p.SpareBarcode3, likePattern)) ||
                        (p.SpareBarcode4 != null && EF.Functions.Like(p.SpareBarcode4, likePattern)) ||
                        (p.LogoBarcodes != null && EF.Functions.Like(p.LogoBarcodes, likePattern))
                    );
                }
                return query;
            }
        }

        /// <summary>
        /// Ortak kategori filtresi - Legacy ve yeni kategori sistemi desteği
        /// </summary>
        private static IQueryable<Product> ApplyCategoryFilter(IQueryable<Product> query, string category)
        {
            if (string.IsNullOrEmpty(category)) return query;

            return query.Where(p => 
                (p.Category != null && p.Category == category) ||
                (p.CategoryEntity != null && p.CategoryEntity.Name == category)
            );
        }

        /// <summary>
        /// Ortak brand filtresi
        /// </summary>
        private static IQueryable<Product> ApplyBrandFilter(IQueryable<Product> query, string brand)
        {
            if (string.IsNullOrEmpty(brand)) return query;

            return query.Where(p => p.Brand != null && p.Brand == brand);
        }

        /// <summary>
        /// Ortak aktif ürün filtresi
        /// </summary>
        private static IQueryable<Product> ApplyActiveFilter(IQueryable<Product> query, bool includeArchived = false)
        {
            return includeArchived ? query : query.Where(p => p.IsArchived == false);
        }

        /// <summary>
        /// Material filtresi
        /// </summary>
        private static IQueryable<Product> ApplyMaterialFilter(IQueryable<Product> query, string material)
        {
            if (string.IsNullOrEmpty(material)) return query;
            return query.Where(p => p.Material != null && p.Material == material);
        }

        /// <summary>
        /// Color filtresi
        /// </summary>
        private static IQueryable<Product> ApplyColorFilter(IQueryable<Product> query, string color)
        {
            if (string.IsNullOrEmpty(color)) return query;
            return query.Where(p => p.Color != null && p.Color == color);
        }

        /// <summary>
        /// EAN Code filtresi
        /// </summary>
        private static IQueryable<Product> ApplyEanCodeFilter(IQueryable<Product> query, string eanCode)
        {
            if (string.IsNullOrEmpty(eanCode)) return query;
            return query.Where(p => p.EanCode != null && p.EanCode.Contains(eanCode));
        }

        /// <summary>
        /// Weight range filtresi
        /// </summary>
        private static IQueryable<Product> ApplyWeightRangeFilter(IQueryable<Product> query, decimal? minWeight, decimal? maxWeight)
        {
            if (minWeight.HasValue)
                query = query.Where(p => p.Weight >= minWeight.Value);
            if (maxWeight.HasValue)
                query = query.Where(p => p.Weight <= maxWeight.Value);
            return query;
        }

        /// <summary>
        /// Desi range filtresi
        /// </summary>
        private static IQueryable<Product> ApplyDesiRangeFilter(IQueryable<Product> query, decimal? minDesi, decimal? maxDesi)
        {
            if (minDesi.HasValue)
                query = query.Where(p => p.Desi >= minDesi.Value);
            if (maxDesi.HasValue)
                query = query.Where(p => p.Desi <= maxDesi.Value);
            return query;
        }

        /// <summary>
        /// Warranty range filtresi
        /// </summary>
        private static IQueryable<Product> ApplyWarrantyRangeFilter(IQueryable<Product> query, int? minWarranty, int? maxWarranty)
        {
            if (minWarranty.HasValue)
                query = query.Where(p => p.WarrantyMonths >= minWarranty.Value);
            if (maxWarranty.HasValue)
                query = query.Where(p => p.WarrantyMonths <= maxWarranty.Value);
            return query;
        }

        /// <summary>
        /// Has Image filtresi
        /// </summary>
        private static IQueryable<Product> ApplyHasImageFilter(IQueryable<Product> query, bool? hasImage)
        {
            if (!hasImage.HasValue) return query;

            if (hasImage.Value)
            {
                return query.Where(p => !string.IsNullOrEmpty(p.ImageUrl) ||
                                       !string.IsNullOrEmpty(p.ImageUrl1) ||
                                       !string.IsNullOrEmpty(p.ImageUrl2) ||
                                       !string.IsNullOrEmpty(p.ImageUrl3) ||
                                       !string.IsNullOrEmpty(p.ImageUrl4) ||
                                       !string.IsNullOrEmpty(p.ImageUrl5));
            }
            else
            {
                return query.Where(p => string.IsNullOrEmpty(p.ImageUrl) &&
                                       string.IsNullOrEmpty(p.ImageUrl1) &&
                                       string.IsNullOrEmpty(p.ImageUrl2) &&
                                       string.IsNullOrEmpty(p.ImageUrl3) &&
                                       string.IsNullOrEmpty(p.ImageUrl4) &&
                                       string.IsNullOrEmpty(p.ImageUrl5));
            }
        }

        /// <summary>
        /// Has EAN filtresi
        /// </summary>
        private static IQueryable<Product> ApplyHasEanFilter(IQueryable<Product> query, bool? hasEan)
        {
            if (!hasEan.HasValue) return query;

            return hasEan.Value
                ? query.Where(p => !string.IsNullOrEmpty(p.EanCode))
                : query.Where(p => string.IsNullOrEmpty(p.EanCode));
        }

        /// <summary>
        /// Has Barcode filtresi
        /// </summary>
        private static IQueryable<Product> ApplyHasBarcodeFilter(IQueryable<Product> query, bool? hasBarcode)
        {
            if (!hasBarcode.HasValue) return query;

            if (hasBarcode.Value)
            {
                return query.Where(p => !string.IsNullOrEmpty(p.TrendyolBarcode) ||
                                       !string.IsNullOrEmpty(p.HepsiburadaBarcode) ||
                                       !string.IsNullOrEmpty(p.HepsiburadaSellerStockCode) ||
                                       !string.IsNullOrEmpty(p.HepsiburadaTedarikBarcode) ||
                                       !string.IsNullOrEmpty(p.AmazonBarcode) ||
                                       !string.IsNullOrEmpty(p.KoctasBarcode) ||
                                       !string.IsNullOrEmpty(p.KoctasIstanbulBarcode) ||
                                       !string.IsNullOrEmpty(p.N11ProductCode) ||
                                       !string.IsNullOrEmpty(p.N11CatalogId) ||
                                       !string.IsNullOrEmpty(p.PazaramaBarcode) ||
                                       !string.IsNullOrEmpty(p.PttAvmBarcode) ||
                                       !string.IsNullOrEmpty(p.HaceyapiBarcode) ||
                                       !string.IsNullOrEmpty(p.SpareBarcode1) ||
                                       !string.IsNullOrEmpty(p.SpareBarcode2) ||
                                       !string.IsNullOrEmpty(p.SpareBarcode3) ||
                                       !string.IsNullOrEmpty(p.SpareBarcode4) ||
                                       !string.IsNullOrEmpty(p.LogoBarcodes) ||
                                       !string.IsNullOrEmpty(p.KoctasEanBarcode) ||
                                       !string.IsNullOrEmpty(p.KoctasEanIstanbulBarcode) ||
                                       !string.IsNullOrEmpty(p.PttUrunStokKodu));
            }
            else
            {
                return query.Where(p => string.IsNullOrEmpty(p.TrendyolBarcode) &&
                                       string.IsNullOrEmpty(p.HepsiburadaBarcode) &&
                                       string.IsNullOrEmpty(p.HepsiburadaSellerStockCode) &&
                                       string.IsNullOrEmpty(p.HepsiburadaTedarikBarcode) &&
                                       string.IsNullOrEmpty(p.AmazonBarcode) &&
                                       string.IsNullOrEmpty(p.KoctasBarcode) &&
                                       string.IsNullOrEmpty(p.KoctasIstanbulBarcode) &&
                                       string.IsNullOrEmpty(p.N11ProductCode) &&
                                       string.IsNullOrEmpty(p.N11CatalogId) &&
                                       string.IsNullOrEmpty(p.PazaramaBarcode) &&
                                       string.IsNullOrEmpty(p.PttAvmBarcode) &&
                                       string.IsNullOrEmpty(p.HaceyapiBarcode) &&
                                       string.IsNullOrEmpty(p.SpareBarcode1) &&
                                       string.IsNullOrEmpty(p.SpareBarcode2) &&
                                       string.IsNullOrEmpty(p.SpareBarcode3) &&
                                       string.IsNullOrEmpty(p.SpareBarcode4) &&
                                       string.IsNullOrEmpty(p.LogoBarcodes) &&
                                       string.IsNullOrEmpty(p.KoctasEanBarcode) &&
                                       string.IsNullOrEmpty(p.KoctasEanIstanbulBarcode) &&
                                       string.IsNullOrEmpty(p.PttUrunStokKodu));
            }
        }

        /// <summary>
        /// Specific barcode type filtresi
        /// </summary>
        /// <summary>
        /// Barkod türü filtresi - Tüm barkod türlerini kapsayacak şekilde genişletildi
        /// </summary>
        private static IQueryable<Product> ApplyBarcodeTypeFilter(IQueryable<Product> query, string barcodeType)
        {
            if (string.IsNullOrEmpty(barcodeType)) return query;

            return barcodeType.ToLower() switch
            {
                "trendyol" => query.Where(p => !string.IsNullOrEmpty(p.TrendyolBarcode)),
                "hepsiburada" => query.Where(p => !string.IsNullOrEmpty(p.HepsiburadaBarcode)),
                "hepsiburada_seller" => query.Where(p => !string.IsNullOrEmpty(p.HepsiburadaSellerStockCode)),
                "hepsiburada_tedarik" => query.Where(p => !string.IsNullOrEmpty(p.HepsiburadaTedarikBarcode)),
                "amazon" => query.Where(p => !string.IsNullOrEmpty(p.AmazonBarcode)),
                "koctas" => query.Where(p => !string.IsNullOrEmpty(p.KoctasBarcode)),
                "koctas_istanbul" => query.Where(p => !string.IsNullOrEmpty(p.KoctasIstanbulBarcode)),
                "n11" => query.Where(p => !string.IsNullOrEmpty(p.N11ProductCode) || !string.IsNullOrEmpty(p.N11CatalogId)),
                "n11_catalog" => query.Where(p => !string.IsNullOrEmpty(p.N11CatalogId)),
                "n11_product" => query.Where(p => !string.IsNullOrEmpty(p.N11ProductCode)),
                "pazarama" => query.Where(p => !string.IsNullOrEmpty(p.PazaramaBarcode)),
                "pttavm" => query.Where(p => !string.IsNullOrEmpty(p.PttAvmBarcode)),
                "haceyapi" => query.Where(p => !string.IsNullOrEmpty(p.HaceyapiBarcode)),
                "spare1" => query.Where(p => !string.IsNullOrEmpty(p.SpareBarcode1)),
                "spare2" => query.Where(p => !string.IsNullOrEmpty(p.SpareBarcode2)),
                "spare3" => query.Where(p => !string.IsNullOrEmpty(p.SpareBarcode3)),
                "spare4" => query.Where(p => !string.IsNullOrEmpty(p.SpareBarcode4)),
                "logo" => query.Where(p => !string.IsNullOrEmpty(p.LogoBarcodes)),
                "koctas_ean" => query.Where(p => !string.IsNullOrEmpty(p.KoctasEanBarcode)),
                "koctas_ean_istanbul" => query.Where(p => !string.IsNullOrEmpty(p.KoctasEanIstanbulBarcode)),
                "ptt_urun_stok" => query.Where(p => !string.IsNullOrEmpty(p.PttUrunStokKodu)),
                "any" => query.Where(p => 
                    !string.IsNullOrEmpty(p.TrendyolBarcode) ||
                    !string.IsNullOrEmpty(p.HepsiburadaBarcode) ||
                    !string.IsNullOrEmpty(p.HepsiburadaSellerStockCode) ||
                    !string.IsNullOrEmpty(p.HepsiburadaTedarikBarcode) ||
                    !string.IsNullOrEmpty(p.AmazonBarcode) ||
                    !string.IsNullOrEmpty(p.KoctasBarcode) ||
                    !string.IsNullOrEmpty(p.KoctasIstanbulBarcode) ||
                    !string.IsNullOrEmpty(p.N11ProductCode) ||
                    !string.IsNullOrEmpty(p.N11CatalogId) ||
                    !string.IsNullOrEmpty(p.PazaramaBarcode) ||
                    !string.IsNullOrEmpty(p.PttAvmBarcode) ||
                    !string.IsNullOrEmpty(p.HaceyapiBarcode) ||
                    !string.IsNullOrEmpty(p.SpareBarcode1) ||
                    !string.IsNullOrEmpty(p.SpareBarcode2) ||
                    !string.IsNullOrEmpty(p.SpareBarcode3) ||
                    !string.IsNullOrEmpty(p.SpareBarcode4) ||
                    !string.IsNullOrEmpty(p.LogoBarcodes) ||
                    !string.IsNullOrEmpty(p.KoctasEanBarcode) ||
                    !string.IsNullOrEmpty(p.KoctasEanIstanbulBarcode) ||
                    !string.IsNullOrEmpty(p.PttUrunStokKodu)
                ),
                _ => query
            };
        }

        /// <summary>
        /// Sorting helper
        /// </summary>
        private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string sortBy, string sortDirection)
        {
            return sortBy.ToLower() switch
            {
                "name" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.Name ?? "")
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.Name ?? "")
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "brand" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.Brand ?? "")
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.Brand ?? "")
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "category" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.Category ?? "")
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.Category ?? "")
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "sku" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.SKU ?? "")
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.SKU ?? "")
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "weight" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.Weight)
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.Weight)
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "desi" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.Desi)
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.Desi)
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "warranty" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.WarrantyMonths)
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.WarrantyMonths)
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "created" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                "updated" => sortDirection == "desc" 
                    ? query.OrderByDescending(p => p.UpdatedDate ?? p.CreatedDate)
                           .ThenByDescending(p => p.CreatedDate)
                           .ThenByDescending(p => p.Id) 
                    : query.OrderBy(p => p.UpdatedDate ?? p.CreatedDate)
                           .ThenBy(p => p.CreatedDate)
                           .ThenBy(p => p.Id),
                _ => query.OrderBy(p => p.Name ?? "")
                          .ThenBy(p => p.CreatedDate)
                          .ThenBy(p => p.Id)
            };
        }

        #endregion

        // Product metodları - AsNoTracking optimizasyonu eklendi
        public List<Product> GetAllProducts()
        {
            return _context.Products.AsNoTracking().ToList();
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.AsNoTracking().ToListAsync();
        }

        public Product? GetProductById(int id)
        {
            return _context.Products.AsNoTracking().FirstOrDefault(p => p.Id == id);
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Product>> GetProductsByIdsAsync(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return new List<Product>();

            _logger.LogInformation("Getting products by IDs. Count: {Count}", ids.Count);
            
            return await _context.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public void AddProduct(Product product)
        {
            _logger.LogInformation("Adding product: {ProductName}", product.Name);
            _context.Products.Add(product);
            var saved = _context.SaveChanges();
            _logger.LogInformation("Product added successfully. Records affected: {RecordsAffected}", saved);
        }

        public async Task AddProductAsync(Product product)
        {
            _logger.LogInformation("Adding product async: {ProductName}", product.Name);
            await _context.Products.AddAsync(product);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("Product added successfully async. Records affected: {RecordsAffected}", saved);
        }

        public void UpdateProduct(Product product)
        {
            // Önce mevcut entity'yi track'ten çıkar
            var existingEntity = _context.Products.Local.FirstOrDefault(p => p.Id == product.Id);
            if (existingEntity != null)
            {
                _context.Entry(existingEntity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            
            _context.Products.Update(product);
            _context.SaveChanges();
        }

        public async Task UpdateProductAsync(Product product)
        {
            // Önce mevcut entity'yi track'ten çıkar
            var existingEntity = _context.Products.Local.FirstOrDefault(p => p.Id == product.Id);
            if (existingEntity != null)
            {
                _context.Entry(existingEntity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            }
            
            // UpdatedDate'i otomatik güncelle
            product.UpdatedDate = DateTime.UtcNow;
            
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public void DeleteProduct(int id)
        {
            try
            {
                var product = _context.Products.FirstOrDefault(p => p.Id == id);
                if (product != null)
                {
                    _logger.LogInformation("Deleting product: {ProductName} (ID: {ProductId})", product.Name, id);
                    _context.Products.Remove(product);
                    var changes = _context.SaveChanges();
                    _logger.LogInformation("Product deleted successfully. Records affected: {RecordsAffected}", changes);
                }
                else
                {
                    _logger.LogWarning("Product with ID {ProductId} not found", id);
                    throw new Exception($"Product with ID {id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
                throw;
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product != null)
                {
                    _logger.LogInformation("Deleting product async: {ProductName} (ID: {ProductId})", product.Name, id);
                    _context.Products.Remove(product);
                    var changes = await _context.SaveChangesAsync();
                    _logger.LogInformation("Product deleted successfully. Records affected: {RecordsAffected}", changes);
                }
                else
                {
                    _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {ProductId}", id);
                throw;
            }
        }

        // XML methods will be at the end of the file after archive methods

        // Optimized search with database-level pagination (excludes archived products)
        public List<Product> SearchProducts(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50)
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            // Helper method'ları kullanarak DRY prensibine uygun kod
            query = ApplyActiveFilter(query);
            query = ApplySearchTermFilter(query, searchTerm);
            query = ApplyCategoryFilter(query, category);
            query = ApplyBrandFilter(query, brand);

            // Order by Name for consistent pagination
            query = query.OrderBy(p => p.Name);

            return query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        // Get total count for pagination without loading all data (excludes archived)
        public int GetProductCount(string searchTerm = "", string category = "", string brand = "")
        {
            var query = _context.Products.AsNoTracking().AsQueryable();

            // Helper method'ları kullanarak DRY prensibine uygun kod
            query = ApplyActiveFilter(query);
            query = ApplySearchTermFilter(query, searchTerm);
            query = ApplyCategoryFilter(query, category);
            query = ApplyBrandFilter(query, brand);

            return query.Count();
        }

        // Get distinct categories efficiently - yeni sistem ile beraber legacy desteği
        public List<string> GetDistinctCategories()
        {
            // Hem CategoryId olan hem de olmayan ürünlerdeki kategorileri birleştir
            var legacyCategories = _context.Products.AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category!)
                .Distinct();

            var newCategories = _context.Products
                .Where(p => p.CategoryId.HasValue)
                .Include(p => p.CategoryEntity)
                .Select(p => p.CategoryEntity!.Name)
                .Distinct();

            return legacyCategories.Union(newCategories)
                .OrderBy(c => c)
                .ToList();
        }

        // Get distinct brands efficiently
        public List<string> GetDistinctBrands()
        {
            return _context.Products.AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToList();
        }

        // Get distinct materials efficiently
        public List<string> GetDistinctMaterials()
        {
            return _context.Products.AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Material))
                .Select(p => p.Material!)
                .Distinct()
                .OrderBy(m => m)
                .ToList();
        }

        // Get distinct colors efficiently
        public List<string> GetDistinctColors()
        {
            return _context.Products.AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.Color))
                .Select(p => p.Color!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        // Get distinct special features efficiently - OPTIMIZED: Single query instead of multiple
        public List<string> GetDistinctSpecialFeatures()
        {
            // Single query to get all special feature values in one database call
            var products = _context.Products.AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.KlozetKanalYapisi) ||
                           !string.IsNullOrEmpty(p.KlozetTipi) ||
                           !string.IsNullOrEmpty(p.KlozetKapakCinsi) ||
                           !string.IsNullOrEmpty(p.KlozetMontajTipi) ||
                           !string.IsNullOrEmpty(p.LawaboSuTasmaDeligi) ||
                           !string.IsNullOrEmpty(p.LawaboArmaturDeligi) ||
                           !string.IsNullOrEmpty(p.LawaboTipi) ||
                           !string.IsNullOrEmpty(p.LawaboOzelligi) ||
                           !string.IsNullOrEmpty(p.BataryaCikisUcuUzunlugu) ||
                           !string.IsNullOrEmpty(p.BataryaYuksekligi))
                .Select(p => new {
                    p.KlozetKanalYapisi, p.KlozetTipi, p.KlozetKapakCinsi, p.KlozetMontajTipi,
                    p.LawaboSuTasmaDeligi, p.LawaboArmaturDeligi, p.LawaboTipi, p.LawaboOzelligi,
                    p.BataryaCikisUcuUzunlugu, p.BataryaYuksekligi
                })
                .ToList();

            var features = new HashSet<string>(); // Use HashSet for automatic deduplication
            
            foreach (var p in products)
            {
                if (!string.IsNullOrEmpty(p.KlozetKanalYapisi)) features.Add(p.KlozetKanalYapisi);
                if (!string.IsNullOrEmpty(p.KlozetTipi)) features.Add(p.KlozetTipi);
                if (!string.IsNullOrEmpty(p.KlozetKapakCinsi)) features.Add(p.KlozetKapakCinsi);
                if (!string.IsNullOrEmpty(p.KlozetMontajTipi)) features.Add(p.KlozetMontajTipi);
                if (!string.IsNullOrEmpty(p.LawaboSuTasmaDeligi)) features.Add(p.LawaboSuTasmaDeligi);
                if (!string.IsNullOrEmpty(p.LawaboArmaturDeligi)) features.Add(p.LawaboArmaturDeligi);
                if (!string.IsNullOrEmpty(p.LawaboTipi)) features.Add(p.LawaboTipi);
                if (!string.IsNullOrEmpty(p.LawaboOzelligi)) features.Add(p.LawaboOzelligi);
                if (!string.IsNullOrEmpty(p.BataryaCikisUcuUzunlugu)) features.Add(p.BataryaCikisUcuUzunlugu);
                if (!string.IsNullOrEmpty(p.BataryaYuksekligi)) features.Add(p.BataryaYuksekligi);
            }

            return features.OrderBy(f => f).ToList();
        }

        // Advanced search method with all product properties - DRY optimized
        public async Task<List<Product>> SearchProductsAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null, string sortBy = "updated", string sortDirection = "desc",
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "",
            int page = 1, int pageSize = 50)
        {
            _logger.LogInformation("SearchProductsAdvancedAsync called with advanced filters");

            var query = _context.Products.AsQueryable();

            // Apply standard filters using helper methods
            query = ApplyActiveFilter(query, includeArchived: false);
            query = ApplySearchTermFilter(query, searchTerm);
            query = ApplyCategoryFilter(query, category);
            query = ApplyBrandFilter(query, brand);

            // Advanced status filter override (if specified)
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                {
                    query = query.Where(p => !p.IsArchived);
                }
                else if (status == "archived")
                {
                    query = query.Where(p => p.IsArchived);
                }
                // "all" status includes both active and archived
            }

            // Apply additional filters using helper methods
            query = ApplyMaterialFilter(query, material);
            query = ApplyColorFilter(query, color);
            query = ApplyEanCodeFilter(query, eanCode);
            query = ApplyWeightRangeFilter(query, minWeight, maxWeight);
            query = ApplyDesiRangeFilter(query, minDesi, maxDesi);
            query = ApplyWarrantyRangeFilter(query, minWarranty, maxWarranty);
            query = ApplyHasImageFilter(query, hasImage);
            query = ApplyHasEanFilter(query, hasEan);
            query = ApplyHasBarcodeFilter(query, hasBarcode);
            query = ApplyBarcodeTypeFilter(query, barcodeType);

            // Apply sorting
            query = ApplySorting(query, sortBy, sortDirection);

            // Pagination
            var results = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("SearchProductsAdvancedAsync returned {Count} products", results.Count);
            return results;
        }

        public async Task<int> GetProductCountAdvancedAsync(string searchTerm = "", string category = "", string brand = "", 
            string status = "", string material = "", string color = "", string eanCode = "",
            decimal? minWeight = null, decimal? maxWeight = null, decimal? minDesi = null, decimal? maxDesi = null,
            int? minWarranty = null, int? maxWarranty = null,
            bool? hasImage = null, bool? hasEan = null, bool? hasBarcode = null, string barcodeType = "")
        {
            var query = _context.Products.AsQueryable();

            // Apply the same filters as in SearchProductsAdvancedAsync using helper methods
            query = ApplyActiveFilter(query, includeArchived: false);
            query = ApplySearchTermFilter(query, searchTerm);
            query = ApplyCategoryFilter(query, category);
            query = ApplyBrandFilter(query, brand);

            // Advanced status filter override (if specified)
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                {
                    query = query.Where(p => !p.IsArchived);
                }
                else if (status == "archived")
                {
                    query = query.Where(p => p.IsArchived);
                }
            }

            // Apply additional filters using helper methods
            query = ApplyMaterialFilter(query, material);
            query = ApplyColorFilter(query, color);
            query = ApplyEanCodeFilter(query, eanCode);
            query = ApplyWeightRangeFilter(query, minWeight, maxWeight);
            query = ApplyDesiRangeFilter(query, minDesi, maxDesi);
            query = ApplyWarrantyRangeFilter(query, minWarranty, maxWarranty);
            query = ApplyHasImageFilter(query, hasImage);
            query = ApplyHasEanFilter(query, hasEan);
            query = ApplyHasBarcodeFilter(query, hasBarcode);
            query = ApplyBarcodeTypeFilter(query, barcodeType);

            return await query.CountAsync();
        }

        // Async versions for better performance (excludes archived)
        public async Task<List<Product>> SearchProductsAsync(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50)
        {
            _logger.LogInformation("SearchProductsAsync called with: searchTerm='{SearchTerm}', category='{Category}', brand='{Brand}', page={Page}, pageSize={PageSize}", 
                searchTerm, category, brand, page, pageSize);

            var query = _context.Products.AsQueryable();

            // Helper method'ları kullanarak DRY prensibine uygun kod
            query = ApplyActiveFilter(query);
            query = ApplySearchTermFilter(query, searchTerm);
            query = ApplyCategoryFilter(query, category);
            query = ApplyBrandFilter(query, brand);

            // Order by Name for consistent pagination
            query = query.OrderBy(p => p.Name);

            var results = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            _logger.LogInformation("SearchProductsAsync returned {Count} results", results.Count);

            return results;
        }

        public async Task<int> GetProductCountAsync(string searchTerm = "", string category = "", string brand = "")
        {
            var query = _context.Products.AsQueryable();

            // Helper method'ları kullanarak DRY prensibine uygun kod
            query = ApplyActiveFilter(query);
            query = ApplySearchTermFilter(query, searchTerm);
            query = ApplyCategoryFilter(query, category);
            query = ApplyBrandFilter(query, brand);

            return await query.CountAsync();
        }

        // Bulk operations for better performance
        public async Task BulkUpdateProductsAsync(List<Product> products)
        {
            // UpdatedDate'i tüm products için güncelle
            foreach (var product in products)
            {
                product.UpdatedDate = DateTime.UtcNow;
            }
            
            _context.Products.UpdateRange(products);
            await _context.SaveChangesAsync();
        }

        public async Task BulkDeleteProductsAsync(List<int> productIds)
        {
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();
        }

        // Archive-specific methods
        public List<Product> GetArchivedProducts(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50)
        {
            var query = _context.Products.Where(p => p.IsArchived == true).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    (p.Name != null && p.Name.Contains(searchTerm)) || 
                    (p.SKU != null && p.SKU.Contains(searchTerm)) || 
                    (p.Brand != null && p.Brand.Contains(searchTerm)) ||
                    (p.EanCode != null && p.EanCode.Contains(searchTerm)) ||
                    (p.TrendyolBarcode != null && p.TrendyolBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaBarcode != null && p.HepsiburadaBarcode.Contains(searchTerm)) ||
                    (p.KoctasBarcode != null && p.KoctasBarcode.Contains(searchTerm)) ||
                    (p.KoctasIstanbulBarcode != null && p.KoctasIstanbulBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaTedarikBarcode != null && p.HepsiburadaTedarikBarcode.Contains(searchTerm)) ||
                    (p.PttAvmBarcode != null && p.PttAvmBarcode.Contains(searchTerm)) ||
                    (p.PazaramaBarcode != null && p.PazaramaBarcode.Contains(searchTerm)) ||
                    (p.HaceyapiBarcode != null && p.HaceyapiBarcode.Contains(searchTerm)) ||
                    (p.AmazonBarcode != null && p.AmazonBarcode.Contains(searchTerm)) ||
                    (p.SpareBarcode1 != null && p.SpareBarcode1.Contains(searchTerm)) ||
                    (p.SpareBarcode2 != null && p.SpareBarcode2.Contains(searchTerm)) ||
                    (p.SpareBarcode3 != null && p.SpareBarcode3.Contains(searchTerm)) ||
                    (p.SpareBarcode4 != null && p.SpareBarcode4.Contains(searchTerm)) ||
                    (p.LogoBarcodes != null && p.LogoBarcodes.Contains(searchTerm)) ||
                    (p.KoctasEanBarcode != null && p.KoctasEanBarcode.Contains(searchTerm)) ||
                    (p.KoctasEanIstanbulBarcode != null && p.KoctasEanIstanbulBarcode.Contains(searchTerm)) ||
                    (p.PttUrunStokKodu != null && p.PttUrunStokKodu.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => (p.Category != null && p.Category == category));

            if (!string.IsNullOrEmpty(brand))
                query = query.Where(p => (p.Brand != null && p.Brand == brand));

            query = query.OrderBy(p => p.Name);

            return query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        public int GetArchivedProductCount(string searchTerm = "", string category = "", string brand = "")
        {
            var query = _context.Products.Where(p => p.IsArchived == true).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    (p.Name != null && p.Name.Contains(searchTerm)) || 
                    (p.SKU != null && p.SKU.Contains(searchTerm)) || 
                    (p.Brand != null && p.Brand.Contains(searchTerm)) ||
                    (p.EanCode != null && p.EanCode.Contains(searchTerm)) ||
                    (p.TrendyolBarcode != null && p.TrendyolBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaBarcode != null && p.HepsiburadaBarcode.Contains(searchTerm)) ||
                    (p.KoctasBarcode != null && p.KoctasBarcode.Contains(searchTerm)) ||
                    (p.KoctasIstanbulBarcode != null && p.KoctasIstanbulBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaTedarikBarcode != null && p.HepsiburadaTedarikBarcode.Contains(searchTerm)) ||
                    (p.PttAvmBarcode != null && p.PttAvmBarcode.Contains(searchTerm)) ||
                    (p.PazaramaBarcode != null && p.PazaramaBarcode.Contains(searchTerm)) ||
                    (p.HaceyapiBarcode != null && p.HaceyapiBarcode.Contains(searchTerm)) ||
                    (p.AmazonBarcode != null && p.AmazonBarcode.Contains(searchTerm)) ||
                    (p.SpareBarcode1 != null && p.SpareBarcode1.Contains(searchTerm)) ||
                    (p.SpareBarcode2 != null && p.SpareBarcode2.Contains(searchTerm)) ||
                    (p.SpareBarcode3 != null && p.SpareBarcode3.Contains(searchTerm)) ||
                    (p.SpareBarcode4 != null && p.SpareBarcode4.Contains(searchTerm)) ||
                    (p.LogoBarcodes != null && p.LogoBarcodes.Contains(searchTerm)) ||
                    (p.KoctasEanBarcode != null && p.KoctasEanBarcode.Contains(searchTerm)) ||
                    (p.KoctasEanIstanbulBarcode != null && p.KoctasEanIstanbulBarcode.Contains(searchTerm)) ||
                    (p.PttUrunStokKodu != null && p.PttUrunStokKodu.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => (p.Category != null && p.Category == category));

            if (!string.IsNullOrEmpty(brand))
                query = query.Where(p => (p.Brand != null && p.Brand == brand));

            return query.Count();
        }

        // Async archive methods
        public async Task<List<Product>> GetArchivedProductsAsync(string searchTerm = "", string category = "", string brand = "", int page = 1, int pageSize = 50)
        {
            var query = _context.Products.Where(p => p.IsArchived == true).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    (p.Name != null && p.Name.Contains(searchTerm)) || 
                    (p.SKU != null && p.SKU.Contains(searchTerm)) || 
                    (p.Brand != null && p.Brand.Contains(searchTerm)) ||
                    (p.EanCode != null && p.EanCode.Contains(searchTerm)) ||
                    (p.TrendyolBarcode != null && p.TrendyolBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaBarcode != null && p.HepsiburadaBarcode.Contains(searchTerm)) ||
                    (p.KoctasBarcode != null && p.KoctasBarcode.Contains(searchTerm)) ||
                    (p.KoctasIstanbulBarcode != null && p.KoctasIstanbulBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaTedarikBarcode != null && p.HepsiburadaTedarikBarcode.Contains(searchTerm)) ||
                    (p.PttAvmBarcode != null && p.PttAvmBarcode.Contains(searchTerm)) ||
                    (p.PazaramaBarcode != null && p.PazaramaBarcode.Contains(searchTerm)) ||
                    (p.HaceyapiBarcode != null && p.HaceyapiBarcode.Contains(searchTerm)) ||
                    (p.AmazonBarcode != null && p.AmazonBarcode.Contains(searchTerm)) ||
                    (p.SpareBarcode1 != null && p.SpareBarcode1.Contains(searchTerm)) ||
                    (p.SpareBarcode2 != null && p.SpareBarcode2.Contains(searchTerm)) ||
                    (p.SpareBarcode3 != null && p.SpareBarcode3.Contains(searchTerm)) ||
                    (p.SpareBarcode4 != null && p.SpareBarcode4.Contains(searchTerm)) ||
                    (p.LogoBarcodes != null && p.LogoBarcodes.Contains(searchTerm)) ||
                    (p.KoctasEanBarcode != null && p.KoctasEanBarcode.Contains(searchTerm)) ||
                    (p.KoctasEanIstanbulBarcode != null && p.KoctasEanIstanbulBarcode.Contains(searchTerm)) ||
                    (p.PttUrunStokKodu != null && p.PttUrunStokKodu.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => (p.Category != null && p.Category == category));

            if (!string.IsNullOrEmpty(brand))
                query = query.Where(p => (p.Brand != null && p.Brand == brand));

            query = query.OrderBy(p => p.Name);

            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<int> GetArchivedProductCountAsync(string searchTerm = "", string category = "", string brand = "")
        {
            var query = _context.Products.Where(p => p.IsArchived == true).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    (p.Name != null && p.Name.Contains(searchTerm)) || 
                    (p.SKU != null && p.SKU.Contains(searchTerm)) || 
                    (p.Brand != null && p.Brand.Contains(searchTerm)) ||
                    (p.EanCode != null && p.EanCode.Contains(searchTerm)) ||
                    (p.TrendyolBarcode != null && p.TrendyolBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaBarcode != null && p.HepsiburadaBarcode.Contains(searchTerm)) ||
                    (p.KoctasBarcode != null && p.KoctasBarcode.Contains(searchTerm)) ||
                    (p.KoctasIstanbulBarcode != null && p.KoctasIstanbulBarcode.Contains(searchTerm)) ||
                    (p.HepsiburadaTedarikBarcode != null && p.HepsiburadaTedarikBarcode.Contains(searchTerm)) ||
                    (p.PttAvmBarcode != null && p.PttAvmBarcode.Contains(searchTerm)) ||
                    (p.PazaramaBarcode != null && p.PazaramaBarcode.Contains(searchTerm)) ||
                    (p.HaceyapiBarcode != null && p.HaceyapiBarcode.Contains(searchTerm)) ||
                    (p.AmazonBarcode != null && p.AmazonBarcode.Contains(searchTerm)) ||
                    (p.SpareBarcode1 != null && p.SpareBarcode1.Contains(searchTerm)) ||
                    (p.SpareBarcode2 != null && p.SpareBarcode2.Contains(searchTerm)) ||
                    (p.SpareBarcode3 != null && p.SpareBarcode3.Contains(searchTerm)) ||
                    (p.SpareBarcode4 != null && p.SpareBarcode4.Contains(searchTerm)) ||
                    (p.LogoBarcodes != null && p.LogoBarcodes.Contains(searchTerm)) ||
                    (p.KoctasEanBarcode != null && p.KoctasEanBarcode.Contains(searchTerm)) ||
                    (p.KoctasEanIstanbulBarcode != null && p.KoctasEanIstanbulBarcode.Contains(searchTerm)) ||
                    (p.PttUrunStokKodu != null && p.PttUrunStokKodu.Contains(searchTerm))
                );
            }

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => (p.Category != null && p.Category == category));

            if (!string.IsNullOrEmpty(brand))
                query = query.Where(p => (p.Brand != null && p.Brand == brand));

            return await query.CountAsync();
        }

        // XML Methods - Product to ProductXml conversion and vice versa
        public List<ProductXml> GetAllXmlProducts()
        {
            var products = _context.Products.ToList();
            return products.Select(ConvertToProductXml).ToList();
        }

        public List<ProductXml> GetActiveXmlProducts()
        {
            var products = _context.Products.Where(p => !p.IsArchived).ToList(); // Sadece arşivde olmayanlar = aktif
            return products.Select(ConvertToProductXml).ToList();
        }

        public List<ProductXml> GetArchivedXmlProducts()
        {
            var products = _context.Products.Where(p => p.IsArchived).ToList();
            return products.Select(ConvertToProductXml).ToList();
        }

        public void AddXmlProduct(ProductXml xmlProduct)
        {
            var product = ConvertFromProductXml(xmlProduct);
            AddProduct(product);
        }

        public void ImportXmlProducts(List<ProductXml> xmlProducts)
        {
            foreach (var xmlProduct in xmlProducts)
            {
                var product = ConvertFromProductXml(xmlProduct);
                AddProduct(product);
            }
        }

        // Product to ProductXml conversion
        private ProductXml ConvertToProductXml(Product product)
        {
            var xmlProduct = new ProductXml
            {
                Id = product.Id,
                Name = product.Name ?? string.Empty,
                Description = product.Description ?? string.Empty,
                DescriptionHtml = product.Description ?? string.Empty, // HTML version
                DescriptionPlain = StripHtmlTags(product.Description ?? string.Empty), // Plain text version
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
                KoctasBarcode = product.KoctasBarcode ?? string.Empty,
                KoctasIstanbulBarcode = product.KoctasIstanbulBarcode ?? string.Empty,
                HepsiburadaTedarikBarcode = product.HepsiburadaTedarikBarcode ?? string.Empty,
                PttAvmBarcode = product.PttAvmBarcode ?? string.Empty,
                PazaramaBarcode = product.PazaramaBarcode ?? string.Empty,
                HaceyapiBarcode = product.HaceyapiBarcode ?? string.Empty,
                AmazonBarcode = product.AmazonBarcode ?? string.Empty,
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

            // MarketplaceImageUrls listesini ayrı ayrı URL'lere dönüştür
            SetMarketplaceImageUrlsFromList(xmlProduct, product.MarketplaceImageUrls);

            return xmlProduct;
        }

        // ProductXml to Product conversion
        private Product ConvertFromProductXml(ProductXml xmlProduct)
        {
            var product = new Product
            {
                // Id = 0, // Let database auto-generate for new products
                Name = xmlProduct.Name ?? string.Empty,
                Description = xmlProduct.Description ?? string.Empty,
                Category = xmlProduct.Category ?? string.Empty,
                Brand = xmlProduct.Brand ?? string.Empty,
                SKU = xmlProduct.SKU ?? string.Empty,
                Weight = xmlProduct.Weight,
                Desi = xmlProduct.Desi,
                Width = xmlProduct.Width,
                Height = xmlProduct.Height,
                Depth = xmlProduct.Depth,
                WarrantyMonths = xmlProduct.WarrantyMonths,
                Material = xmlProduct.Material ?? string.Empty,
                Color = xmlProduct.Color ?? string.Empty,
                EanCode = xmlProduct.EanCode ?? string.Empty,
                Features = xmlProduct.Features ?? string.Empty,
                Notes = xmlProduct.Notes ?? string.Empty,
                TrendyolBarcode = xmlProduct.TrendyolBarcode ?? string.Empty,
                HepsiburadaBarcode = xmlProduct.HepsiburadaBarcode ?? string.Empty,
                HepsiburadaSellerStockCode = xmlProduct.HepsiburadaSellerStockCode ?? string.Empty,
                KoctasBarcode = xmlProduct.KoctasBarcode ?? string.Empty,
                KoctasIstanbulBarcode = xmlProduct.KoctasIstanbulBarcode ?? string.Empty,
                HepsiburadaTedarikBarcode = xmlProduct.HepsiburadaTedarikBarcode ?? string.Empty,
                PttAvmBarcode = xmlProduct.PttAvmBarcode ?? string.Empty,
                PazaramaBarcode = xmlProduct.PazaramaBarcode ?? string.Empty,
                HaceyapiBarcode = xmlProduct.HaceyapiBarcode ?? string.Empty,
                AmazonBarcode = xmlProduct.AmazonBarcode ?? string.Empty,
                N11CatalogId = xmlProduct.N11CatalogId ?? string.Empty,
                N11ProductCode = xmlProduct.N11ProductCode ?? string.Empty,
                SpareBarcode1 = xmlProduct.SpareBarcode1 ?? string.Empty,
                SpareBarcode2 = xmlProduct.SpareBarcode2 ?? string.Empty,
                SpareBarcode3 = xmlProduct.SpareBarcode3 ?? string.Empty,
                SpareBarcode4 = xmlProduct.SpareBarcode4 ?? string.Empty,
                LogoBarcodes = xmlProduct.LogoBarcodes ?? string.Empty,
                KoctasEanBarcode = xmlProduct.KoctasEanBarcode ?? string.Empty,
                KoctasEanIstanbulBarcode = xmlProduct.KoctasEanIstanbulBarcode ?? string.Empty,
                PttUrunStokKodu = xmlProduct.PttUrunStokKodu ?? string.Empty,
                IsArchived = xmlProduct.IsArchived, // IsActive kaldırıldı
                CreatedDate = xmlProduct.CreatedDate,
                UpdatedDate = xmlProduct.UpdatedDate,
                ImageUrls = new List<string>(),
                MarketplaceImageUrls = new List<string>(),
                VideoUrls = new List<string>()
            };

            // Ayrı ayrı URL'leri tekrar listeye dönüştür
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

            var marketplaceImageFields = new[] {
                xmlProduct.MarketplaceImageUrl1, xmlProduct.MarketplaceImageUrl2, xmlProduct.MarketplaceImageUrl3,
                xmlProduct.MarketplaceImageUrl4, xmlProduct.MarketplaceImageUrl5, xmlProduct.MarketplaceImageUrl6,
                xmlProduct.MarketplaceImageUrl7, xmlProduct.MarketplaceImageUrl8, xmlProduct.MarketplaceImageUrl9,
                xmlProduct.MarketplaceImageUrl10
            };

            foreach (var imageUrl in marketplaceImageFields)
            {
                if (!string.IsNullOrWhiteSpace(imageUrl))
                    product.MarketplaceImageUrls.Add(imageUrl);
            }

            // Video URL'leri işle
            var videoFields = new[] {
                xmlProduct.VideoUrl1, xmlProduct.VideoUrl2, xmlProduct.VideoUrl3,
                xmlProduct.VideoUrl4, xmlProduct.VideoUrl5
            };

            foreach (var videoUrl in videoFields)
            {
                if (!string.IsNullOrWhiteSpace(videoUrl))
                    product.VideoUrls.Add(videoUrl);
            }

            // İlk görsel URL'yi ana görsel olarak ayarla
            product.ImageUrl = product.ImageUrls.FirstOrDefault() ?? string.Empty;

            return product;
        }

        /// <summary>
        /// HTML etiketlerini temizler, sadece düz metin döndürür
        /// </summary>
        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Basic regex to remove HTML tags
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }

        private void SetImageUrlsFromList(ProductXml xmlProduct, List<string> imageUrls)
        {
            if (imageUrls != null && imageUrls.Count > 0)
            {
                var imageProperties = new Action<string>[]
                {
                    url => xmlProduct.ImageUrl1 = url,
                    url => xmlProduct.ImageUrl2 = url,
                    url => xmlProduct.ImageUrl3 = url,
                    url => xmlProduct.ImageUrl4 = url,
                    url => xmlProduct.ImageUrl5 = url,
                    url => xmlProduct.ImageUrl6 = url,
                    url => xmlProduct.ImageUrl7 = url,
                    url => xmlProduct.ImageUrl8 = url,
                    url => xmlProduct.ImageUrl9 = url,
                    url => xmlProduct.ImageUrl10 = url
                };

                for (int i = 0; i < Math.Min(imageUrls.Count, imageProperties.Length); i++)
                {
                    imageProperties[i](imageUrls[i]);
                }
            }
        }

        private void SetMarketplaceImageUrlsFromList(ProductXml xmlProduct, List<string> marketplaceImageUrls)
        {
            if (marketplaceImageUrls != null && marketplaceImageUrls.Count > 0)
            {
                var marketplaceImageProperties = new Action<string>[]
                {
                    url => xmlProduct.MarketplaceImageUrl1 = url,
                    url => xmlProduct.MarketplaceImageUrl2 = url,
                    url => xmlProduct.MarketplaceImageUrl3 = url,
                    url => xmlProduct.MarketplaceImageUrl4 = url,
                    url => xmlProduct.MarketplaceImageUrl5 = url,
                    url => xmlProduct.MarketplaceImageUrl6 = url,
                    url => xmlProduct.MarketplaceImageUrl7 = url,
                    url => xmlProduct.MarketplaceImageUrl8 = url,
                    url => xmlProduct.MarketplaceImageUrl9 = url,
                    url => xmlProduct.MarketplaceImageUrl10 = url
                };

                for (int i = 0; i < Math.Min(marketplaceImageUrls.Count, marketplaceImageProperties.Length); i++)
                {
                    marketplaceImageProperties[i](marketplaceImageUrls[i]);
                }
            }
        }

        // Bulk Archive/Unarchive Operations
        public async Task BulkArchiveProductsAsync(List<int> productIds)
        {
            _logger.LogInformation("BulkArchiveProductsAsync called with {Count} product IDs", productIds.Count);

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsArchived = true;
                product.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully archived {Count} products", products.Count);
        }

        public async Task BulkUnarchiveProductsAsync(List<int> productIds)
        {
            _logger.LogInformation("BulkUnarchiveProductsAsync called with {Count} product IDs", productIds.Count);

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsArchived = false;
                product.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully unarchived {Count} products", products.Count);
        }

        // Utility methods for dropdowns and filters
        public async Task<List<string>> GetDistinctCategoriesAsync()
        {
            var directCategories = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .ToListAsync();

            var categoryRelationCategories = await _context.Products
                .Where(p => p.CategoryId.HasValue)
                .Join(_context.Categories, p => p.CategoryId, c => c.Id, (p, c) => c.Name)
                .Distinct()
                .ToListAsync();

            return directCategories.Union(categoryRelationCategories).OrderBy(c => c).ToList();
        }

        public async Task<List<string>> GetDistinctBrandsAsync()
        {
            return await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctMaterialsAsync()
        {
            return await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Material))
                .Select(p => p.Material!)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctColorsAsync()
        {
            return await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Color))
                .Select(p => p.Color!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctSpecialFeaturesAsync()
        {
            var features = new List<string>();

            // Collect all special features from different fields
            var allFeatures = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Features))
                .Select(p => p.Features!)
                .ToListAsync();

            // Split and flatten features
            foreach (var feature in allFeatures)
            {
                if (!string.IsNullOrEmpty(feature))
                {
                    features.AddRange(feature.Split(',', ';', '|')
                        .Select(f => f.Trim())
                        .Where(f => !string.IsNullOrEmpty(f)));
                }
            }

            return features.Distinct().OrderBy(f => f).ToList();
        }

        // XML Operations
        public async Task<List<ProductXml>> GetAllXmlProductsAsync()
        {
            var products = await GetAllProductsAsync();
            return products.Select(ConvertToXml).ToList();
        }

        public async Task<List<ProductXml>> GetActiveXmlProductsAsync()
        {
            var products = await _context.Products
                .Where(p => !p.IsArchived)
                .ToListAsync();
            return products.Select(ConvertToXml).ToList();
        }

        public async Task<List<ProductXml>> GetArchivedXmlProductsAsync()
        {
            var products = await _context.Products
                .Where(p => p.IsArchived)
                .ToListAsync();
            return products.Select(ConvertToXml).ToList();
        }

        public async Task ImportXmlProductsAsync(List<ProductXml> xmlProducts)
        {
            _logger.LogInformation("ImportXmlProductsAsync called with {Count} XML products", xmlProducts.Count);

            foreach (var xmlProduct in xmlProducts)
            {
                var product = ConvertFromXml(xmlProduct);
                
                // Check if product exists by SKU or Name
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.SKU == product.SKU || p.Name == product.Name);

                if (existingProduct != null)
                {
                    // Update existing product
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Brand = product.Brand;
                    existingProduct.Category = product.Category;
                    existingProduct.Material = product.Material;
                    existingProduct.Color = product.Color;
                    existingProduct.Weight = product.Weight;
                    existingProduct.Desi = product.Desi;
                    existingProduct.WarrantyMonths = product.WarrantyMonths;
                    existingProduct.UpdatedDate = DateTime.UtcNow;
                }
                else
                {
                    // Add new product
                    product.CreatedDate = DateTime.UtcNow;
                    product.UpdatedDate = DateTime.UtcNow;
                    await _context.Products.AddAsync(product);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully imported {Count} XML products", xmlProducts.Count);
        }

        // Health and statistics
        public async Task<Dictionary<string, object>> GetHealthStatsAsync()
        {
            var stats = new Dictionary<string, object>();

            try
            {
                stats["TotalProducts"] = await _context.Products.CountAsync();
                stats["ActiveProducts"] = await _context.Products.CountAsync(p => !p.IsArchived);
                stats["ArchivedProducts"] = await _context.Products.CountAsync(p => p.IsArchived);
                stats["TotalCategories"] = await _context.Categories.CountAsync();
                stats["DatabaseStatus"] = "Healthy";
                stats["LastChecked"] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                stats["DatabaseStatus"] = "Error: " + ex.Message;
                stats["LastChecked"] = DateTime.UtcNow;
            }

            return stats;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        // Helper methods for XML conversion
        private ProductXml ConvertToXml(Product product)
        {
            return new ProductXml
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Brand = product.Brand,
                Category = product.Category,
                Description = product.Description,
                Features = product.Features,
                ImageUrl1 = product.ImageUrl, // Map main ImageUrl to ImageUrl1
                Weight = product.Weight,
                Desi = product.Desi,
                Width = product.Width,
                Height = product.Height,
                Depth = product.Depth,
                WarrantyMonths = product.WarrantyMonths,
                Material = product.Material,
                Color = product.Color,
                EanCode = product.EanCode,
                Notes = product.Notes,
                CreatedDate = product.CreatedDate,
                UpdatedDate = product.UpdatedDate,
                IsArchived = product.IsArchived
            };
        }

        private Product ConvertFromXml(ProductXml xmlProduct)
        {
            return new Product
            {
                Name = xmlProduct.Name ?? "",
                SKU = xmlProduct.SKU,
                Brand = xmlProduct.Brand,
                Category = xmlProduct.Category,
                Description = xmlProduct.Description,
                Features = xmlProduct.Features,
                ImageUrl = xmlProduct.ImageUrl1, // Map ImageUrl1 to main ImageUrl
                Weight = xmlProduct.Weight,
                Desi = xmlProduct.Desi,
                Width = xmlProduct.Width,
                Height = xmlProduct.Height,
                Depth = xmlProduct.Depth,
                WarrantyMonths = xmlProduct.WarrantyMonths,
                Material = xmlProduct.Material,
                Color = xmlProduct.Color,
                EanCode = xmlProduct.EanCode,
                Notes = xmlProduct.Notes,
                IsArchived = xmlProduct.IsArchived
            };
        }

        #region Benzersizlik Kontrolleri

        public async Task<bool> IsSkuUniqueAsync(string sku, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(sku)) return true;

            var query = _context.Products.Where(p => p.SKU == sku);
            
            if (excludeProductId.HasValue)
            {
                query = query.Where(p => p.Id != excludeProductId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsEanCodeUniqueAsync(string eanCode, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(eanCode)) return true;

            var query = _context.Products.Where(p => p.EanCode == eanCode);
            
            if (excludeProductId.HasValue)
            {
                query = query.Where(p => p.Id != excludeProductId.Value);
            }

            return !await query.AnyAsync();
        }

        #endregion
    }
}
