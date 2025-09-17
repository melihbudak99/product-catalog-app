using Microsoft.EntityFrameworkCore;
using product_catalog_app.src.models;

namespace product_catalog_app.src.data
{
    /// <summary>
    /// Optimized DbContext for handling 1000+ products efficiently
    /// </summary>
    public class ProductDbContextOptimized : ProductDbContext
    {
        public ProductDbContextOptimized(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Optimize for large datasets
                optionsBuilder.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.FirstWithoutOrderByAndFilterWarning));
            }
            
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Additional performance indexes for large datasets
            modelBuilder.Entity<Product>(entity =>
            {
                // Covering indexes for search operations
                entity.HasIndex(e => new { e.IsArchived, e.Name, e.Brand, e.Category })
                      .HasDatabaseName("IX_Products_Search_Covering");

                // Index for pagination with filters - IsActive removed since we only use IsArchived
                entity.HasIndex(e => new { e.IsArchived, e.CreatedDate })
                      .HasDatabaseName("IX_Products_Status_Date");

                // Full-text search simulation index
                entity.HasIndex(e => new { e.Name, e.SKU, e.EanCode })
                      .HasDatabaseName("IX_Products_TextSearch");

                // Barcode search optimization
                entity.HasIndex(e => new { e.TrendyolBarcode, e.HepsiburadaBarcode, e.KoctasBarcode })
                      .HasDatabaseName("IX_Products_Barcodes");
            });

            // Category optimization
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(e => new { e.IsActive, e.Name })
                      .HasDatabaseName("IX_Categories_Active_Name");
            });
        }
    }
}
