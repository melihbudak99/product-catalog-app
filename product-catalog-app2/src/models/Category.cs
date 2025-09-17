using System.ComponentModel.DataAnnotations;

namespace product_catalog_app.src.models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı gereklidir")]
        [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
