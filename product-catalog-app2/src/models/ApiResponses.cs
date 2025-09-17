namespace product_catalog_app.src.models
{
    /// <summary>
    /// Request model for bulk operations
    /// </summary>
    public class BulkOperationRequest
    {
        public string Action { get; set; } = string.Empty;
        public List<int> ProductIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// Standardized API response wrapper for consistent response format
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Creates a successful response without data
        /// </summary>
        public static ApiResponse<T> SuccessResult(string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message
            };
        }

        /// <summary>
        /// Creates an error response with single error message
        /// </summary>
        public static ApiResponse<T> ErrorResult(string error)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = "Operation failed",
                Errors = new List<string> { error }
            };
        }

        /// <summary>
        /// Creates an error response with multiple error messages
        /// </summary>
        public static ApiResponse<T> ErrorResult(List<string> errors, string message = "Operation failed")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }

        /// <summary>
        /// Creates an error response with validation errors
        /// </summary>
        public static ApiResponse<T> ValidationErrorResult(Dictionary<string, List<string>> validationErrors)
        {
            var errors = validationErrors.SelectMany(kvp => 
                kvp.Value.Select(error => $"{kvp.Key}: {error}")).ToList();

            return new ApiResponse<T>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Paginated response wrapper for list data
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Creates a paginated response
        /// </summary>
        public static PaginatedResponse<T> Create(List<T> items, int totalCount, int page, int pageSize)
        {
            return new PaginatedResponse<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    /// <summary>
    /// Bulk operation result for tracking success/failure of multiple operations
    /// </summary>
    public class BulkOperationResult
    {
        public int TotalItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<int> FailedItemIds { get; set; } = new List<int>();

        public bool HasErrors => Errors.Any() || FailedItems > 0;
        public double SuccessRate => TotalItems > 0 ? (double)SuccessfulItems / TotalItems * 100 : 0;

        /// <summary>
        /// Creates a successful bulk operation result
        /// </summary>
        public static BulkOperationResult Success(int totalItems)
        {
            return new BulkOperationResult
            {
                TotalItems = totalItems,
                SuccessfulItems = totalItems,
                FailedItems = 0
            };
        }

        /// <summary>
        /// Creates a partial success bulk operation result
        /// </summary>
        public static BulkOperationResult PartialSuccess(int totalItems, int successfulItems, List<string> errors, List<int> failedIds)
        {
            return new BulkOperationResult
            {
                TotalItems = totalItems,
                SuccessfulItems = successfulItems,
                FailedItems = totalItems - successfulItems,
                Errors = errors,
                FailedItemIds = failedIds
            };
        }
    }
}
