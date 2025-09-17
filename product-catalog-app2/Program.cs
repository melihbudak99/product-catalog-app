using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using System.IO.Compression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Globalization;
using product_catalog_app.src.data;
using product_catalog_app.src.services;
using product_catalog_app.src.models;
using product_catalog_app.src.interfaces;
using ProductCatalogApp.Helpers;

// 🚀 Professional Production-Ready Configuration
var builder = WebApplication.CreateBuilder(args);

// Configure culture for decimal parsing
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Response compression for production
if (builder.Environment.IsProduction())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
            "text/plain",
            "text/css",
            "application/javascript",
            "text/html",
            "application/xml",
            "text/xml",
            "application/json",
            "text/json",
            "application/font-woff",
            "application/font-woff2"
        });
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal;
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Optimal;
    });
}

// Response caching
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1MB
    options.UseCaseSensitivePaths = false;
});

builder.Services.AddControllersWithViews();

// Database context with advanced connection pooling
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=data/products.db;Cache=Shared;Pooling=true;";

builder.Services.AddDbContextPool<ProductDbContext>(options =>
    options.UseSqlite(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableServiceProviderCaching()
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution),
    poolSize: 128);

// Register services
if (builder.Environment.IsProduction() || builder.Configuration.GetValue<bool>("UseOptimizedServices", false))
{
    builder.Services.AddScoped<ProductRepository>();
    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<IProductService>(provider => provider.GetRequiredService<ProductService>());
    builder.Services.AddScoped<IProductRepository>(provider => provider.GetRequiredService<ProductRepository>());
    
    builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
    Console.WriteLine("🚀 Using optimized services for production operations");
}
else
{
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<ProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<ProductService>();
}

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<XmlService>();

// Export/Import services
builder.Services.AddScoped<ExportColumnService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<ImportService>();

// Professional database service - NO MORE MANUAL MIGRATIONS!
builder.Services.AddScoped<DatabaseService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck("database", () => {
        try {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            connection.Open();
            return HealthCheckResult.Healthy("Database connection successful");
        } catch (Exception ex) {
            return HealthCheckResult.Unhealthy($"Database error: {ex.Message}");
        }
    })
    .AddCheck("memory", () => {
        var allocatedBytes = GC.GetTotalMemory(false);
        var maxMemoryMB = 500;
        if (allocatedBytes > maxMemoryMB * 1024 * 1024)
        {
            return HealthCheckResult.Unhealthy(
                $"Memory usage {allocatedBytes / 1024 / 1024}MB exceeds limit of {maxMemoryMB}MB");
        }
        return HealthCheckResult.Healthy(
            $"Memory usage: {allocatedBytes / 1024 / 1024}MB");
    });

var app = builder.Build();

// 🛡️ Professional database initialization - SAFE APPROACH
using (var scope = app.Services.CreateScope())
{
    var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

    try
    {
        logger.LogInformation("🚀 Starting professional database initialization...");
        
        // Use DatabaseService for safe initialization - NO MANUAL MIGRATIONS!
        var success = await databaseService.InitializeDatabaseAsync();
        
        if (success)
        {
            logger.LogInformation("✅ Database initialization completed successfully");
        }
        else
        {
            logger.LogError("❌ Database initialization failed, attempting emergency recovery...");
            
            // Emergency recovery: try to create database manually
            try
            {
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("🆘 Emergency database creation successful");
                success = true;
            }
            catch (Exception emergencyEx)
            {
                logger.LogError(emergencyEx, "❌ Emergency database creation also failed");
                success = false;
            }
        }

        if (success)
        {
            // Initialize basic categories if none exist
            try
            {
                logger.LogInformation("🔍 Getting category service...");
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                logger.LogInformation("✅ Category service obtained");
                
                logger.LogInformation("🔍 Getting existing categories...");
                var existingCategories = await categoryService.GetAllCategoriesAsync();
                logger.LogInformation("✅ Existing categories count: {Count}", existingCategories.Count);
                
                if (!existingCategories.Any())
                {
                    logger.LogInformation("🏷️ Initializing comprehensive product categories...");
                    var defaultCategories = new[]
                    {
                        new { Name = "Ara Musluk", Description = "Ara musluk ve sistemleri" },
                        new { Name = "Banyo Aksesuar", Description = "Banyo için çeşitli aksesuar ürünleri" },
                        new { Name = "Banyo Dolapları", Description = "Banyo mobilyaları ve dolap sistemleri" },
                        new { Name = "Batarya (Banyo)", Description = "Banyo için batarya ve musluk sistemleri" },
                        new { Name = "Batarya (Çanak Lavabo)", Description = "Çanak lavabo için özel batarya sistemleri" },
                        new { Name = "Batarya (Eviye)", Description = "Eviye için batarya ve musluk sistemleri" },
                        new { Name = "Batarya (Lavabo)", Description = "Lavabo için batarya ve musluk sistemleri" },
                        new { Name = "Batarya Set", Description = "Batarya set ve kombinasyonları" },
                        new { Name = "Çamaşır Musluğu", Description = "Çamaşır için özel musluk sistemleri" },
                        new { Name = "Diğer", Description = "Diğer kategorilere girmeyen ürünler" },
                        new { Name = "Duş Sistemleri", Description = "Duş kabini ve duş sistemleri" },
                        new { Name = "Gömme Rezervuar", Description = "Gömme rezervuar sistemleri" },
                        new { Name = "Gömme+Klozet Set", Description = "Gömme rezervuar ve klozet kombinasyonları" },
                        new { Name = "Hela Taşı", Description = "Hela taşı ve benzeri ürünler" },
                        new { Name = "Hırdavat ve Tesisat", Description = "Tesisat hırdavat malzemeleri" },
                        new { Name = "Klozet", Description = "Standart klozet sistemleri" },
                        new { Name = "Klozet Kapağı", Description = "Klozet kapağı ve aksesuarları" },
                        new { Name = "Lavabo", Description = "Lavabo ve lavabo sistemleri" },
                        new { Name = "Lavabo Sifonları (Pop-Up)", Description = "Pop-up lavabo sifonları" },
                        new { Name = "Mutfak Eviye Lavabosu", Description = "Mutfak eviye lavabo sistemleri" },
                        new { Name = "Pisuvar", Description = "Pisuvar ve pisuvar sistemleri" },
                        new { Name = "Rezervuar İç Takım", Description = "Rezervuar iç takım ve parçaları" },
                        new { Name = "Seramik", Description = "Seramik ürünler ve malzemeler" },
                        new { Name = "Taharet Musluğu", Description = "Taharet musluğu ve sistemleri" },
                        new { Name = "Toz Yapıştırıcı", Description = "Toz yapıştırıcı ve kimyasal malzemeler" }
                    };

                    foreach (var categoryData in defaultCategories)
                    {
                        try
                        {
                            await categoryService.AddCategoryAsync(categoryData.Name, categoryData.Description);
                            logger.LogInformation("✅ Added category: {CategoryName}", categoryData.Name);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "❌ Failed to add category: {CategoryName}", categoryData.Name);
                        }
                    }
                    
                    logger.LogInformation("✅ Professional category system initialized successfully");
                }
                else
                {
                    logger.LogInformation("✅ Categories already exist, skipping initialization");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error during category initialization");
            }
            
            // Get stats for verification
            try
            {
                var productCount = await context.Products.CountAsync();
                var categoryService2 = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                var categoryCount = await categoryService2.GetActiveCategoryCountAsync();
                
                logger.LogInformation("📊 Database stats - Products: {ProductCount}, Categories: {CategoryCount}", 
                    productCount, categoryCount);
            }
            catch (Exception statsEx)
            {
                logger.LogWarning(statsEx, "⚠️ Could not retrieve database stats");
            }
        }
        else
        {
            logger.LogError("❌ Critical database initialization failure - application may not function properly");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database initialization error - continuing with best effort");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
    app.UseResponseCompression();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseResponseCaching();

// cPanel shared hosting için HTTPS redirect kontrolü
if (!app.Environment.IsDevelopment())
{
    var enableHttpsRedirect = Environment.GetEnvironmentVariable("ENABLE_HTTPS_REDIRECT");
    if (!string.IsNullOrEmpty(enableHttpsRedirect) && enableHttpsRedirect.ToLower() == "true")
    {
        app.UseHttpsRedirection();
    }
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
        else
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
        }
    }
});

// Security headers for production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    
    app.Use(async (context, next) =>
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        
        context.Response.Headers.TryAdd("Content-Security-Policy",
            "default-src 'self'; " +
            "style-src 'self' 'unsafe-inline' cdnjs.cloudflare.com fonts.googleapis.com; " +
            "script-src 'self' 'unsafe-inline' cdnjs.cloudflare.com; " +
            "img-src 'self' data: https: blob: *.dsmcdn.com; " +
            "media-src 'self' https: data: blob: *.dsmcdn.com *.youtube.com *.ytimg.com *.vimeo.com *.vimeocdn.com; " +
            "font-src 'self' cdnjs.cloudflare.com fonts.gstatic.com; " +
            "connect-src 'self'; " +
            "frame-src 'self' https://www.youtube.com https://player.vimeo.com; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'");
        
        await next();
    });
}

app.UseRouting();

// Health checks endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Product}/{action=Index}/{id?}");

// API endpoints
app.MapGet("/api/products", async (HttpContext context, [FromServices] IProductService productService) =>
{
    int page = context.Request.Query.ContainsKey("page") ? 
        int.Parse(context.Request.Query["page"]!) : 1;
    int pageSize = context.Request.Query.ContainsKey("pageSize") ? 
        int.Parse(context.Request.Query["pageSize"]!) : 50;
        
    return await productService.SearchProductsAsync("", "", "", page, pageSize);
});

app.MapGet("/api/products/count", async ([FromServices] IProductService productService) =>
{
    return await productService.GetProductCountAsync();
});

// cPanel uyumlu port konfigürasyonu
var port = Environment.GetEnvironmentVariable("PORT") ?? 
           Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? 
           "http://0.0.0.0:5000";

if (!port.StartsWith("http"))
{
    port = $"http://0.0.0.0:{port}";
}

app.Run(port);
