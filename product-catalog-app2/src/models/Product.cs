using System.ComponentModel.DataAnnotations;

namespace product_catalog_app.src.models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string SKU { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string Brand { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string Category { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(2000)]
        public string Features { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string ImageUrl { get; set; } = string.Empty;
        
        public decimal Weight { get; set; }
        public decimal Desi { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Depth { get; set; }
        public decimal? Length { get; set; }
        public int WarrantyMonths { get; set; }
        
        public string Material { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string EanCode { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        
        // Pazaryeri Barkodları
        public string TrendyolBarcode { get; set; } = string.Empty;
        public string HepsiburadaBarcode { get; set; } = string.Empty;
        public string HepsiburadaSellerStockCode { get; set; } = string.Empty;
        public string KoctasBarcode { get; set; } = string.Empty;
        public string KoctasIstanbulBarcode { get; set; } = string.Empty;
        public string HepsiburadaTedarikBarcode { get; set; } = string.Empty;
        public string PttAvmBarcode { get; set; } = string.Empty;
        public string PazaramaBarcode { get; set; } = string.Empty;
        public string HaceyapiBarcode { get; set; } = string.Empty;
        public string AmazonBarcode { get; set; } = string.Empty;
        public string N11CatalogId { get; set; } = string.Empty;
        public string N11ProductCode { get; set; } = string.Empty;
        public string SpareBarcode1 { get; set; } = string.Empty;
        public string SpareBarcode2 { get; set; } = string.Empty;
        public string SpareBarcode3 { get; set; } = string.Empty;
        public string SpareBarcode4 { get; set; } = string.Empty;
        public string LogoBarcodes { get; set; } = string.Empty;
        
        // Yeni Pazaryeri Barkodları
        public string KoctasEanBarcode { get; set; } = string.Empty;
        public string KoctasEanIstanbulBarcode { get; set; } = string.Empty;
        public string PttUrunStokKodu { get; set; } = string.Empty;
        
        // Entegra Barkodları
        public string EntegraUrunId { get; set; } = string.Empty;
        public string EntegraUrunKodu { get; set; } = string.Empty;
        public string EntegraBarkod { get; set; } = string.Empty;
        
        // Özel Ürün Özellikleri
        public string KlozetKanalYapisi { get; set; } = string.Empty;
        public string KlozetTipi { get; set; } = string.Empty;
        public string KlozetKapakCinsi { get; set; } = string.Empty;
        public string KlozetMontajTipi { get; set; } = string.Empty;
        public string LawaboSuTasmaDeligi { get; set; } = string.Empty;
        public string LawaboArmaturDeligi { get; set; } = string.Empty;
        public string LawaboTipi { get; set; } = string.Empty;
        public string LawaboOzelligi { get; set; } = string.Empty;
        
        // Batarya Özellikleri
        public string BataryaCikisUcuUzunlugu { get; set; } = string.Empty;
        public string BataryaYuksekligi { get; set; } = string.Empty;
        public string KabinTipi { get; set; } = string.Empty;
        
        // Görsel URL'leri
        public string ImageUrl1 { get; set; } = string.Empty;
        public string ImageUrl2 { get; set; } = string.Empty;
        public string ImageUrl3 { get; set; } = string.Empty;
        public string ImageUrl4 { get; set; } = string.Empty;
        public string ImageUrl5 { get; set; } = string.Empty;
        public string ImageUrl6 { get; set; } = string.Empty;
        public string ImageUrl7 { get; set; } = string.Empty;
        public string ImageUrl8 { get; set; } = string.Empty;
        public string ImageUrl9 { get; set; } = string.Empty;
        public string ImageUrl10 { get; set; } = string.Empty;
        
        // Medya URL Listeleri
        public List<string> ImageUrls { get; set; } = new List<string>();
        public List<string> MarketplaceImageUrls { get; set; } = new List<string>();
        public List<string> VideoUrls { get; set; } = new List<string>();
        // MarketplaceVideoUrls - KALDIRILDI
        
        // Zaman Bilgileri
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; } = DateTime.UtcNow;
        
        // Durum Bilgileri - Sadece Aktif/Arşiv yapısı
        public bool IsArchived { get; set; } = false;
        
        // IsActive property'si kaldırıldı - sadece IsArchived kullanılacak
        // Aktif ürün: IsArchived = false
        // Arşiv ürün: IsArchived = true
        
        // Kategori İlişkisi
        public int? CategoryId { get; set; }
        public virtual Category? CategoryEntity { get; set; }
        
        /// <summary>
        /// Debug: Tüm alan değerlerini kontrol et
        /// </summary>
        public string GetDebugFieldSummary()
        {
            var fields = new List<string>();
            
            // Temel alanlar
            if (!string.IsNullOrEmpty(Name)) fields.Add($"Name: {Name}");
            if (!string.IsNullOrEmpty(SKU)) fields.Add($"SKU: {SKU}");
            if (!string.IsNullOrEmpty(Brand)) fields.Add($"Brand: {Brand}");
            if (!string.IsNullOrEmpty(Category)) fields.Add($"Category: {Category}");
            if (!string.IsNullOrEmpty(Description)) fields.Add($"Description: {Description[..Math.Min(50, Description.Length)]}...");
            
            // Ölçüler
            if (Weight > 0) fields.Add($"Weight: {Weight}");
            if (Desi > 0) fields.Add($"Desi: {Desi}");
            if (Width > 0) fields.Add($"Width: {Width}");
            if (Height > 0) fields.Add($"Height: {Height}");
            if (Depth > 0) fields.Add($"Depth: {Depth}");
            if (Length > 0) fields.Add($"Length: {Length}");
            
            // Barkodlar
            if (!string.IsNullOrEmpty(TrendyolBarcode)) fields.Add($"TrendyolBarcode: {TrendyolBarcode}");
            if (!string.IsNullOrEmpty(HepsiburadaBarcode)) fields.Add($"HepsiburadaBarcode: {HepsiburadaBarcode}");
            if (!string.IsNullOrEmpty(AmazonBarcode)) fields.Add($"AmazonBarcode: {AmazonBarcode}");
            if (!string.IsNullOrEmpty(KoctasBarcode)) fields.Add($"KoctasBarcode: {KoctasBarcode}");
            if (!string.IsNullOrEmpty(LogoBarcodes)) fields.Add($"LogoBarcodes: {LogoBarcodes}");
            if (!string.IsNullOrEmpty(EntegraUrunId)) fields.Add($"EntegraUrunId: {EntegraUrunId}");
            if (!string.IsNullOrEmpty(EntegraUrunKodu)) fields.Add($"EntegraUrunKodu: {EntegraUrunKodu}");
            if (!string.IsNullOrEmpty(EntegraBarkod)) fields.Add($"EntegraBarkod: {EntegraBarkod}");
            
            // Görseller
            if (ImageUrls?.Any() == true) fields.Add($"ImageUrls: {ImageUrls.Count} items");
            if (MarketplaceImageUrls?.Any() == true) fields.Add($"MarketplaceImageUrls: {MarketplaceImageUrls.Count} items");
            if (VideoUrls?.Any() == true) fields.Add($"VideoUrls: {VideoUrls.Count} items");
            
            // Özellikler
            if (!string.IsNullOrEmpty(KlozetKanalYapisi)) fields.Add($"KlozetKanalYapisi: {KlozetKanalYapisi}");
            if (!string.IsNullOrEmpty(Material)) fields.Add($"Material: {Material}");
            if (!string.IsNullOrEmpty(Color)) fields.Add($"Color: {Color}");
            
            return $"Product Fields ({fields.Count} set): " + string.Join(", ", fields);
        }
    }
}
