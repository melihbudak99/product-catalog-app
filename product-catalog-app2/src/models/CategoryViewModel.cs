using product_catalog_app.src.models;

namespace product_catalog_app.src.models
{
    public class CategoryViewModel
    {
        public List<Category> Categories { get; set; } = new List<Category>();
        public Dictionary<int, int> ProductCounts { get; set; } = new Dictionary<int, int>();

        // For pagination if needed
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        // For search/filter
        public string SearchTerm { get; set; } = string.Empty;
    }
}
