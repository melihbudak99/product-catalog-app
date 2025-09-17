using System.Xml.Serialization;

namespace product_catalog_app.src.models
{
    [XmlRoot("ProductCatalog")]
    public class ProductCatalog
    {
        [XmlElement("Product")]
        public List<ProductXml> Products { get; set; } = new List<ProductXml>();
    }

    public class ProductXml
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DescriptionHtml { get; set; } = string.Empty; // HTML etiketli açıklama
        public string DescriptionPlain { get; set; } = string.Empty; // Düz metin açıklama
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;

        // Boyut ve Ağırlık Bilgileri
        public decimal Weight { get; set; } // Ağırlık (kg)
        public decimal Desi { get; set; } // Desi
        public decimal Width { get; set; } // Genişlik (cm)
        public decimal Height { get; set; } // Yükseklik (cm)
        public decimal Depth { get; set; } // En (cm)
        public int WarrantyMonths { get; set; } // Garanti Süresi (Ay)

        // Materyal ve Renk
        public string Material { get; set; } = string.Empty; // Materyal
        public string Color { get; set; } = string.Empty; // Ürün Rengi
        public string EanCode { get; set; } = string.Empty; // EAN Kodu

        // Özellikler ve Notlar
        public string Features { get; set; } = string.Empty;
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

        // Yedek Pazaryeri Barkodları
        public string SpareBarcode1 { get; set; } = string.Empty;
        public string SpareBarcode2 { get; set; } = string.Empty;
        public string SpareBarcode3 { get; set; } = string.Empty;
        public string SpareBarcode4 { get; set; } = string.Empty;

        // Logo Barkodları (JSON olarak)
        public string LogoBarcodes { get; set; } = string.Empty;

        // Yeni Pazaryeri Barkodları
        public string KoctasEanBarcode { get; set; } = string.Empty;
        public string KoctasEanIstanbulBarcode { get; set; } = string.Empty;
        public string PttUrunStokKodu { get; set; } = string.Empty;

        // Tarih Bilgileri
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        // Görsel URL'leri - Gerçek Görseller
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

        // Pazaryeri Görselleri
        public string MarketplaceImageUrl1 { get; set; } = string.Empty;
        public string MarketplaceImageUrl2 { get; set; } = string.Empty;
        public string MarketplaceImageUrl3 { get; set; } = string.Empty;
        public string MarketplaceImageUrl4 { get; set; } = string.Empty;
        public string MarketplaceImageUrl5 { get; set; } = string.Empty;
        public string MarketplaceImageUrl6 { get; set; } = string.Empty;
        public string MarketplaceImageUrl7 { get; set; } = string.Empty;
        public string MarketplaceImageUrl8 { get; set; } = string.Empty;
        public string MarketplaceImageUrl9 { get; set; } = string.Empty;
        public string MarketplaceImageUrl10 { get; set; } = string.Empty;

        // Video URL'leri - Ürün Videoları
        public string VideoUrl1 { get; set; } = string.Empty;
        public string VideoUrl2 { get; set; } = string.Empty;
        public string VideoUrl3 { get; set; } = string.Empty;
        public string VideoUrl4 { get; set; } = string.Empty;
        public string VideoUrl5 { get; set; } = string.Empty;

        // Pazaryeri Video URL'leri - KALDIRILDI
        // Bu alanlar artık kullanılmıyor ve XML şablonlarından kaldırıldı

        // Archive status
        public bool IsArchived { get; set; } = false;
    }
}