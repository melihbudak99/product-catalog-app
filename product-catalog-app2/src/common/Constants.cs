using System;

namespace product_catalog_app.src.common
{
    /// <summary>
    /// Application-wide constants for maintainability and consistency
    /// </summary>
    public static class Constants
    {
        // Cache Keys
        public static class CacheKeys
        {
            public const string ALL_CATEGORIES = "all_categories";
            public const string ALL_BRANDS = "all_brands";
            public const string CATEGORY_COUNT_PREFIX = "category_count_";
            public const string PRODUCT_SEARCH_PREFIX = "product_search_";
        }

        // Cache Durations (in minutes)
        public static class CacheDuration
        {
            public const int CATEGORIES = 30;
            public const int BRANDS = 30;
            public const int SEARCH_RESULTS = 5;
            public const int PRODUCT_COUNT = 10;
        }

        // Pagination
        public static class Pagination
        {
            public const int DEFAULT_PAGE_SIZE = 50;
            public const int MAX_PAGE_SIZE = 200;
            public const int DEFAULT_PAGE = 1;
        }

        // File and Export
        public static class Export
        {
            public const string XML_CONTENT_TYPE = "application/xml";
            public const string CSV_CONTENT_TYPE = "text/csv";
            public const string EXCEL_CONTENT_TYPE = "application/vnd.ms-excel";
        }

        // Performance Thresholds - Optimized for 1000+ products
        public static class Performance
        {
            public const int SLOW_REQUEST_THRESHOLD_MS = 2000; // Increased for large datasets
            public const int BULK_OPERATION_BATCH_SIZE = 500; // Increased batch size
            public const int MAX_CONCURRENT_QUERIES = 10; // For parallel processing
            public const int QUERY_TIMEOUT_SECONDS = 60; // For complex queries
        }

        // Validation
        public static class Validation
        {
            public const int MAX_NAME_LENGTH = 500;
            public const int MAX_NAME_WORDS = 200; // Ürün adı maksimum kelime sayısı
            public const int MAX_SKU_LENGTH = 100;
            public const int MAX_BRAND_LENGTH = 200;
            public const int MAX_CATEGORY_LENGTH = 200;
            public const int MAX_DESCRIPTION_LENGTH = 2000;
            public const int MAX_URL_LENGTH = 1000;
            public const decimal MIN_WEIGHT = 0.01m;
            public const decimal MAX_WEIGHT = 9999.99m;
        }

        // Database
        public static class Database
        {
            public const string CONNECTION_STRING_KEY = "DefaultConnection";
            public const string SQLITE_DEFAULT = "Data Source=products.db";
        }

        // Image Management
        public static class Images
        {
            public const int MAX_IMAGES_PER_PRODUCT = 10;
            public const int MAX_MARKETPLACE_IMAGES = 10;
        }
    }
}
