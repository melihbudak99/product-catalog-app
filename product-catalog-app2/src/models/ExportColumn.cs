using System.ComponentModel.DataAnnotations;

namespace product_catalog_app.src.models
{
    /// <summary>
    /// Export/Import için sütun tanımları
    /// </summary>
    public class ExportColumn
    {
        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
        public bool IsRequired { get; set; } = false;
        public string DataType { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; } = 0;
    }

    /// <summary>
    /// Export için sütun seçim filtreleri
    /// </summary>
    public class ExportColumnFilter
    {
        public string? Status { get; set; } // "all", "active", "archived"
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? SearchTerm { get; set; }
        public List<string> SelectedColumns { get; set; } = new List<string>();
        public bool IncludeHtmlDescription { get; set; } = true;
        public bool IncludePlainTextDescription { get; set; } = true;
        public bool IncludeImageUrls { get; set; } = true;
        public bool IncludeVideoUrls { get; set; } = true;
        public bool IncludeMarketplaceBarcodes { get; set; } = true;
        public bool IncludeSpecialFeatures { get; set; } = true;
        public string ExportFormat { get; set; } = "xlsx"; // xlsx, json, xml, csv
        public List<int>? SelectedProductIds { get; set; } = null; // Seçili ürün ID'leri (bulk export için)
    }

    /// <summary>
    /// HTML/Text açıklama seçenekleri
    /// </summary>
    public class DescriptionExportOptions
    {
        public bool IncludeHtml { get; set; } = true;
        public bool IncludePlainText { get; set; } = true;
        public string HtmlColumnName { get; set; } = "Description_HTML";
        public string PlainTextColumnName { get; set; } = "Description_PlainText";
    }

    /// <summary>
    /// Export edilen ürün verileri (dinamik sütunlar için)
    /// </summary>
    public class ExportProductData
    {
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
        
        public T? GetValue<T>(string propertyName)
        {
            if (Properties.TryGetValue(propertyName, out var value))
            {
                if (value is T typedValue)
                    return typedValue;
                
                try
                {
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }
            return default(T);
        }

        public void SetValue(string propertyName, object? value)
        {
            Properties[propertyName] = value;
        }
    }
}
