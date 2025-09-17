using System.ComponentModel.DataAnnotations;
using product_catalog_app.src.common;
using product_catalog_app.src.models;

namespace product_catalog_app.src.services
{
    /// <summary>
    /// Service for centralized validation logic and business rules
    /// </summary>
    public class ValidationService
    {
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validates a product entity against business rules
        /// </summary>
        public ValidationResult ValidateProduct(Product product)
        {
            var validationResult = new ValidationResult();

            // Required field validation
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                validationResult.AddError("Name", "Ürün adı zorunludur");
            }

            // Length validations using constants
            if (product.Name?.Length > Constants.Validation.MAX_NAME_LENGTH)
            {
                validationResult.AddError("Name", $"Ürün adı en fazla {Constants.Validation.MAX_NAME_LENGTH} karakter olabilir");
            }

            // Word count validation for product name
            if (!string.IsNullOrWhiteSpace(product.Name))
            {
                var wordCount = CountWords(product.Name);
                if (wordCount > Constants.Validation.MAX_NAME_WORDS)
                {
                    validationResult.AddError("Name", $"Ürün adı en fazla {Constants.Validation.MAX_NAME_WORDS} kelime olabilir. Şu anda {wordCount} kelime var.");
                }
            }

            if (product.SKU?.Length > Constants.Validation.MAX_SKU_LENGTH)
            {
                validationResult.AddError("SKU", $"SKU en fazla {Constants.Validation.MAX_SKU_LENGTH} karakter olabilir");
            }

            if (product.Brand?.Length > Constants.Validation.MAX_BRAND_LENGTH)
            {
                validationResult.AddError("Brand", $"Marka adı en fazla {Constants.Validation.MAX_BRAND_LENGTH} karakter olabilir");
            }

            if (product.Description?.Length > Constants.Validation.MAX_DESCRIPTION_LENGTH)
            {
                validationResult.AddError("Description", $"Açıklama en fazla {Constants.Validation.MAX_DESCRIPTION_LENGTH} karakter olabilir");
            }

            // Weight validation
            if (product.Weight < 0 || product.Weight > Constants.Validation.MAX_WEIGHT)
            {
                validationResult.AddError("Weight", $"Ağırlık 0 ile {Constants.Validation.MAX_WEIGHT} kg arasında olmalıdır");
            }

            // Image URL validation
            if (product.ImageUrls?.Count > Constants.Images.MAX_IMAGES_PER_PRODUCT)
            {
                validationResult.AddError("ImageUrls", $"En fazla {Constants.Images.MAX_IMAGES_PER_PRODUCT} resim eklenebilir");
            }

            if (product.MarketplaceImageUrls?.Count > Constants.Images.MAX_MARKETPLACE_IMAGES)
            {
                validationResult.AddError("MarketplaceImageUrls", $"En fazla {Constants.Images.MAX_MARKETPLACE_IMAGES} pazaryeri resmi eklenebilir");
            }

            // URL format validation
            ValidateImageUrls(product.ImageUrls, "ImageUrls", validationResult);
            ValidateImageUrls(product.MarketplaceImageUrls, "MarketplaceImageUrls", validationResult);

            return validationResult;
        }

        /// <summary>
        /// Validates that product has required fields for creation
        /// </summary>
        public ValidationResult ValidateProductForCreation(Product product)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(product.Name))
                result.AddError(nameof(product.Name), "Ürün adı zorunludur");

            // All other fields are optional - only validate ranges if they have values
            if (product.Weight < 0)
                result.AddError(nameof(product.Weight), "Ağırlık negatif olamaz");

            if (product.Desi < 0)
                result.AddError(nameof(product.Desi), "Desi negatif olamaz");

            if (product.WarrantyMonths < 0)
                result.AddError(nameof(product.WarrantyMonths), "Garanti süresi negatif olamaz");

            return result;
        }

        /// <summary>
        /// Validates product data for bulk operations
        /// </summary>
        public ValidationResult ValidateProductsForBulkOperation(List<Product> products)
        {
            var result = new ValidationResult();

            if (products == null || !products.Any())
            {
                result.AddError("Products", "En az bir ürün seçilmelidir");
                return result;
            }

            if (products.Count > Constants.Performance.BULK_OPERATION_BATCH_SIZE)
            {
                result.AddError("Products", $"Toplu işlem için en fazla {Constants.Performance.BULK_OPERATION_BATCH_SIZE} ürün seçilebilir");
            }

            return result;
        }

        /// <summary>
        /// Validates XML import data
        /// </summary>
        public ValidationResult ValidateXmlImportData(string xmlContent)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                result.AddError("XmlContent", "XML içeriği boş olamaz");
                return result;
            }

            // Basic XML validation
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
                if (doc.Root == null)
                {
                    result.AddError("XmlContent", "Geçersiz XML formatı");
                }
            }
            catch (System.Xml.XmlException ex)
            {
                result.AddError("XmlContent", $"XML parsing hatası: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        public (int page, int pageSize) ValidatePaginationParameters(int page, int pageSize)
        {
            // Ensure page is at least 1
            if (page < Constants.Pagination.DEFAULT_PAGE)
            {
                _logger.LogWarning("Invalid page number {Page}, defaulting to {DefaultPage}", page, Constants.Pagination.DEFAULT_PAGE);
                page = Constants.Pagination.DEFAULT_PAGE;
            }

            // Ensure page size is within acceptable limits
            if (pageSize <= 0)
            {
                _logger.LogWarning("Invalid page size {PageSize}, defaulting to {DefaultPageSize}", pageSize, Constants.Pagination.DEFAULT_PAGE_SIZE);
                pageSize = Constants.Pagination.DEFAULT_PAGE_SIZE;
            }
            else if (pageSize > Constants.Pagination.MAX_PAGE_SIZE)
            {
                _logger.LogWarning("Page size {PageSize} exceeds maximum {MaxPageSize}, capping to maximum", pageSize, Constants.Pagination.MAX_PAGE_SIZE);
                pageSize = Constants.Pagination.MAX_PAGE_SIZE;
            }

            return (page, pageSize);
        }

        /// <summary>
        /// Validates search term for potential security issues
        /// </summary>
        public string SanitizeSearchTerm(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return string.Empty;

            // Remove potentially dangerous characters
            var sanitized = searchTerm.Trim();
            
            // Remove SQL injection patterns (basic protection)
            var dangerousPatterns = new[] { "'", "\"", ";", "--", "/*", "*/", "xp_", "exec", "execute" };
            foreach (var pattern in dangerousPatterns)
            {
                sanitized = sanitized.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
            }

            // Limit length
            if (sanitized.Length > 200)
            {
                sanitized = sanitized.Substring(0, 200);
                _logger.LogWarning("Search term truncated from {OriginalLength} to 200 characters", searchTerm.Length);
            }

            return sanitized;
        }

        private void ValidateImageUrls(List<string>? urls, string fieldName, ValidationResult result)
        {
            if (urls == null) return;

            for (int i = 0; i < urls.Count; i++)
            {
                var url = urls[i];
                if (string.IsNullOrWhiteSpace(url)) continue;

                if (url.Length > Constants.Validation.MAX_URL_LENGTH)
                {
                    result.AddError($"{fieldName}[{i}]", $"URL uzunluğu en fazla {Constants.Validation.MAX_URL_LENGTH} karakter olabilir");
                }

                if (!IsValidUrl(url))
                {
                    result.AddError($"{fieldName}[{i}]", "Geçerli bir URL formatı değil");
                }
            }
        }

        private static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Counts the number of words in a text string
        /// </summary>
        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;
            
            // Trim the text and split by whitespace, then filter out empty entries
            var words = text.Trim().Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return words.Length;
        }
    }

    /// <summary>
    /// Custom validation result class for detailed error reporting
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public Dictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>();

        public void AddError(string field, string message)
        {
            if (!Errors.ContainsKey(field))
            {
                Errors[field] = new List<string>();
            }
            Errors[field].Add(message);
        }

        public List<string> GetAllErrors()
        {
            return Errors.SelectMany(kvp => kvp.Value).ToList();
        }

        public string GetErrorsAsString()
        {
            return string.Join("; ", GetAllErrors());
        }
    }
}
