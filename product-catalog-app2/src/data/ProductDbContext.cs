using Microsoft.EntityFrameworkCore;
using product_catalog_app.src.models;

namespace product_catalog_app.src.data
{
    public class ProductDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductXml> XmlProducts { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;

        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Product tablosu için
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.SKU).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.Brand).HasMaxLength(200).IsRequired(false);
                entity.Property(e => e.Category).HasMaxLength(200).IsRequired(false);
                entity.Property(e => e.Description).HasMaxLength(2000).IsRequired(false); // Allow null/empty
                entity.Property(e => e.Features).HasMaxLength(2000).IsRequired(false);
                entity.Property(e => e.ImageUrl).HasMaxLength(1000).IsRequired(false);
                entity.Property(e => e.Weight).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Length).HasColumnType("decimal(18,2)").IsRequired(false);

                // Make all barcode fields nullable
                entity.Property(e => e.TrendyolBarcode).IsRequired(false);
                entity.Property(e => e.HepsiburadaBarcode).IsRequired(false);
                entity.Property(e => e.HepsiburadaSellerStockCode).IsRequired(false);
                entity.Property(e => e.KoctasBarcode).IsRequired(false);
                entity.Property(e => e.KoctasIstanbulBarcode).IsRequired(false);
                entity.Property(e => e.HepsiburadaTedarikBarcode).IsRequired(false);
                entity.Property(e => e.PttAvmBarcode).IsRequired(false);
                entity.Property(e => e.PazaramaBarcode).IsRequired(false);
                entity.Property(e => e.HaceyapiBarcode).IsRequired(false);
                entity.Property(e => e.AmazonBarcode).IsRequired(false);
                entity.Property(e => e.N11CatalogId).IsRequired(false);
                entity.Property(e => e.N11ProductCode).IsRequired(false);
                entity.Property(e => e.SpareBarcode1).IsRequired(false);
                entity.Property(e => e.SpareBarcode2).IsRequired(false);
                entity.Property(e => e.SpareBarcode3).IsRequired(false);
                entity.Property(e => e.SpareBarcode4).IsRequired(false);
                entity.Property(e => e.LogoBarcodes).IsRequired(false);
                
                // Entegra barcode fields
                entity.Property(e => e.EntegraUrunId).IsRequired(false);
                entity.Property(e => e.EntegraUrunKodu).IsRequired(false);
                entity.Property(e => e.EntegraBarkod).IsRequired(false);
                
                entity.Property(e => e.Material).IsRequired(false);
                entity.Property(e => e.Color).IsRequired(false);
                entity.Property(e => e.EanCode).IsRequired(false);
                entity.Property(e => e.Notes).IsRequired(false);

                // Special product features - make all optional
                entity.Property(e => e.KlozetKanalYapisi).IsRequired(false);
                entity.Property(e => e.KlozetTipi).IsRequired(false);
                entity.Property(e => e.KlozetKapakCinsi).IsRequired(false);
                entity.Property(e => e.KlozetMontajTipi).IsRequired(false);
                entity.Property(e => e.LawaboSuTasmaDeligi).IsRequired(false);
                entity.Property(e => e.LawaboArmaturDeligi).IsRequired(false);
                entity.Property(e => e.LawaboTipi).IsRequired(false);
                entity.Property(e => e.LawaboOzelligi).IsRequired(false);
                
                // Battery features - make all optional
                entity.Property(e => e.BataryaCikisUcuUzunlugu).IsRequired(false);
                entity.Property(e => e.BataryaYuksekligi).IsRequired(false);

                // Performance indexes for frequently searched columns
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.SKU);
                entity.HasIndex(e => e.Brand);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => new { e.Category, e.Brand }); // Composite index for filtering
                // entity.HasIndex(e => e.IsActive); // Removed - using only IsArchived
                entity.HasIndex(e => e.IsArchived); // Archive index
                // entity.HasIndex(e => new { e.IsActive, e.IsArchived }); // Removed - using only IsArchived
                entity.HasIndex(e => e.CreatedDate);

                // Additional performance indexes for search operations
                entity.HasIndex(e => new { e.IsArchived, e.Name }); // For search with archive filter
                entity.HasIndex(e => new { e.IsArchived, e.Category, e.Brand }); // For filtered searches
                // entity.HasIndex(e => new { e.IsArchived, e.IsActive }); // Removed - using only IsArchived

                // List<string> ImageUrls'yi string olarak kaydet - WARNING düzeltildi
                entity.Property(e => e.ImageUrls)
                    .HasConversion(
                        v => string.Join('|', v ?? new List<string>()),
                        v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()
                    )
                    .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => (c1 ?? new List<string>()).SequenceEqual(c2 ?? new List<string>()),
                        c => (c ?? new List<string>()).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => (c ?? new List<string>()).ToList()));

                // List<string> MarketplaceImageUrls'yi string olarak kaydet
                entity.Property(e => e.MarketplaceImageUrls)
                    .HasConversion(
                        v => string.Join('|', v ?? new List<string>()),
                        v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()
                    )
                    .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => (c1 ?? new List<string>()).SequenceEqual(c2 ?? new List<string>()),
                        c => (c ?? new List<string>()).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => (c ?? new List<string>()).ToList()));

                // List<string> VideoUrls'yi string olarak kaydet
                entity.Property(e => e.VideoUrls)
                    .IsRequired(false)
                    .HasConversion(
                        v => string.Join('|', v ?? new List<string>()),
                        v => v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()
                    )
                    .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                        (c1, c2) => (c1 ?? new List<string>()).SequenceEqual(c2 ?? new List<string>()),
                        c => (c ?? new List<string>()).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => (c ?? new List<string>()).ToList()));
            });

            // ProductXml tablosu için
            modelBuilder.Entity<ProductXml>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Category).HasMaxLength(200);
                entity.Property(e => e.Brand).HasMaxLength(200);
                entity.Property(e => e.SKU).HasMaxLength(100);
                entity.Property(e => e.Features).HasMaxLength(2000);

                // Görsel URL'leri için ayrı sütunlar
                entity.Property(e => e.ImageUrl1).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl2).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl3).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl4).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl5).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl6).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl7).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl8).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl9).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl10).HasMaxLength(1000);
            });

            // Category tablosu için
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500).IsRequired(false);
                entity.HasIndex(e => e.Name).IsUnique(); // Unique kategori adları
                entity.HasIndex(e => e.IsActive);
            });

            // Product-Category relationship
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.CategoryEntity)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Category backward compatibility index
                entity.HasIndex(e => e.Category);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}