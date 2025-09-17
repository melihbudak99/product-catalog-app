using product_catalog_app.src.models;

namespace product_catalog_app.src.common
{
    /// <summary>
    /// Null Safety Utility for Product entities
    /// Eliminates 30+ duplicate null checks from Program.cs
    /// </summary>
    public static class NullSafetyUtils
    {
        /// <summary>
        /// Ensures basic product fields are not null
        /// Returns true if any updates were made
        /// </summary>
        public static bool EnsureBasicFieldsNotNull(Product product)
        {
            if (product == null) return false;
            
            bool hasUpdates = false;
            
            // Basic fields
            if (product.Description == null) { product.Description = ""; hasUpdates = true; }
            if (product.Features == null) { product.Features = ""; hasUpdates = true; }
            if (product.Notes == null) { product.Notes = ""; hasUpdates = true; }
            if (product.Brand == null) { product.Brand = ""; hasUpdates = true; }
            if (product.Category == null) { product.Category = ""; hasUpdates = true; }
            if (product.SKU == null) { product.SKU = ""; hasUpdates = true; }
            if (product.EanCode == null) { product.EanCode = ""; hasUpdates = true; }
            if (product.Material == null) { product.Material = ""; hasUpdates = true; }
            if (product.Color == null) { product.Color = ""; hasUpdates = true; }
            if (product.ImageUrl == null) { product.ImageUrl = ""; hasUpdates = true; }
            
            return hasUpdates;
        }
        
        /// <summary>
        /// Ensures all string properties are not null
        /// Returns true if any updates were made
        /// </summary>
        public static bool EnsureStringPropertiesNotNull(Product product)
        {
            if (product == null) return false;
            
            bool hasUpdates = false;
            
            // Use reflection to handle all string properties at once
            var stringProperties = typeof(Product)
                .GetProperties()
                .Where(prop => prop.PropertyType == typeof(string) && prop.CanWrite);
                
            foreach (var prop in stringProperties)
            {
                var value = prop.GetValue(product) as string;
                if (value == null)
                {
                    prop.SetValue(product, "");
                    hasUpdates = true;
                }
            }
            
            return hasUpdates;
        }
        
        /// <summary>
        /// Specific method for barcode fields - more performance optimized
        /// </summary>
        public static bool EnsureBarcodeFieldsNotNull(Product product)
        {
            if (product == null) return false;
            
            bool hasUpdates = false;
            
            // Barcode fields
            if (product.AmazonBarcode == null) { product.AmazonBarcode = ""; hasUpdates = true; }
            if (product.HaceyapiBarcode == null) { product.HaceyapiBarcode = ""; hasUpdates = true; }
            if (product.HepsiburadaBarcode == null) { product.HepsiburadaBarcode = ""; hasUpdates = true; }
            if (product.HepsiburadaSellerStockCode == null) { product.HepsiburadaSellerStockCode = ""; hasUpdates = true; }
            if (product.HepsiburadaTedarikBarcode == null) { product.HepsiburadaTedarikBarcode = ""; hasUpdates = true; }
            if (product.KoctasBarcode == null) { product.KoctasBarcode = ""; hasUpdates = true; }
            if (product.KoctasIstanbulBarcode == null) { product.KoctasIstanbulBarcode = ""; hasUpdates = true; }
            if (product.N11CatalogId == null) { product.N11CatalogId = ""; hasUpdates = true; }
            if (product.N11ProductCode == null) { product.N11ProductCode = ""; hasUpdates = true; }
            if (product.PazaramaBarcode == null) { product.PazaramaBarcode = ""; hasUpdates = true; }
            if (product.PttAvmBarcode == null) { product.PttAvmBarcode = ""; hasUpdates = true; }
            if (product.TrendyolBarcode == null) { product.TrendyolBarcode = ""; hasUpdates = true; }
            if (product.SpareBarcode1 == null) { product.SpareBarcode1 = ""; hasUpdates = true; }
            if (product.SpareBarcode2 == null) { product.SpareBarcode2 = ""; hasUpdates = true; }
            if (product.SpareBarcode3 == null) { product.SpareBarcode3 = ""; hasUpdates = true; }
            if (product.SpareBarcode4 == null) { product.SpareBarcode4 = ""; hasUpdates = true; }
            if (product.LogoBarcodes == null) { product.LogoBarcodes = ""; hasUpdates = true; }
            
            return hasUpdates;
        }
        
        /// <summary>
        /// Special features null safety
        /// </summary>
        public static bool EnsureSpecialFeaturesNotNull(Product product)
        {
            if (product == null) return false;
            
            bool hasUpdates = false;
            
            // Klozet özellikleri
            if (product.KlozetKanalYapisi == null) { product.KlozetKanalYapisi = ""; hasUpdates = true; }
            if (product.KlozetTipi == null) { product.KlozetTipi = ""; hasUpdates = true; }
            if (product.KlozetKapakCinsi == null) { product.KlozetKapakCinsi = ""; hasUpdates = true; }
            if (product.KlozetMontajTipi == null) { product.KlozetMontajTipi = ""; hasUpdates = true; }
            
            // Lavabo özellikleri
            if (product.LawaboSuTasmaDeligi == null) { product.LawaboSuTasmaDeligi = ""; hasUpdates = true; }
            if (product.LawaboArmaturDeligi == null) { product.LawaboArmaturDeligi = ""; hasUpdates = true; }
            if (product.LawaboTipi == null) { product.LawaboTipi = ""; hasUpdates = true; }
            if (product.LawaboOzelligi == null) { product.LawaboOzelligi = ""; hasUpdates = true; }
            
            // Batarya özellikleri
            if (product.BataryaCikisUcuUzunlugu == null) { product.BataryaCikisUcuUzunlugu = ""; hasUpdates = true; }
            if (product.BataryaYuksekligi == null) { product.BataryaYuksekligi = ""; hasUpdates = true; }
            
            return hasUpdates;
        }
        
        /// <summary>
        /// Complete null safety check - combines all methods
        /// </summary>
        public static bool EnsureProductNotNull(Product product)
        {
            if (product == null) return false;
            
            bool hasUpdates = false;
            
            hasUpdates |= EnsureBarcodeFieldsNotNull(product);
            hasUpdates |= EnsureSpecialFeaturesNotNull(product);
            
            // Basic fields
            if (product.EanCode == null) { product.EanCode = ""; hasUpdates = true; }
            if (product.Material == null) { product.Material = ""; hasUpdates = true; }
            if (product.Color == null) { product.Color = ""; hasUpdates = true; }
            if (product.ImageUrl == null) { product.ImageUrl = ""; hasUpdates = true; }
            
            return hasUpdates;
        }
        
        /// <summary>
        /// Batch process multiple products
        /// </summary>
        public static int EnsureProductsNotNull(IEnumerable<Product> products)
        {
            int updatedCount = 0;
            
            foreach (var product in products)
            {
                if (EnsureProductNotNull(product))
                {
                    updatedCount++;
                }
            }
            
            return updatedCount;
        }
    }
}
