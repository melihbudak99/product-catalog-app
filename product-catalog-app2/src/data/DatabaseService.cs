using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace product_catalog_app.src.data
{
    /// <summary>
    /// Production-ready database service with safety mechanisms
    /// </summary>
    public class DatabaseService
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<DatabaseService> _logger;
        private readonly string _connectionString;

        public DatabaseService(ProductDbContext context, ILogger<DatabaseService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               "Data Source=data/products.db;Cache=Shared;Pooling=true;";
        }

        /// <summary>
        /// Safely initialize database with proper migration strategy
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // Check connection - if it fails, try to create database
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection status: {CanConnect}", canConnect);

                if (!canConnect)
                {
                    _logger.LogWarning("Cannot connect to database, attempting to create...");
                    
                    // Try to ensure database directory exists
                    try
                    {
                        var dbPath = Path.GetDirectoryName(_connectionString.Split("Data Source=")[1].Split(";")[0]);
                        if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
                        {
                            Directory.CreateDirectory(dbPath);
                            _logger.LogInformation("Created database directory: {DbPath}", dbPath);
                        }
                    }
                    catch (Exception dirEx)
                    {
                        _logger.LogWarning(dirEx, "Could not create database directory");
                    }

                    // Try to create the database
                    try
                    {
                        await _context.Database.EnsureCreatedAsync();
                        _logger.LogInformation("Database created successfully");
                        canConnect = true;
                    }
                    catch (Exception createEx)
                    {
                        _logger.LogError(createEx, "Failed to create database");
                        return false;
                    }
                }

                // Double check connection after potential creation
                if (!canConnect && !await _context.Database.CanConnectAsync())
                {
                    _logger.LogError("Still cannot connect to database after creation attempt");
                    return false;
                }

                // Check if database exists and has data
                var hasExistingData = await HasExistingDataAsync();
                _logger.LogInformation("Database has existing data: {HasData}", hasExistingData);

                if (hasExistingData)
                {
                    // Production mode: Use migrations only
                    await ApplyMigrationsAsync();
                }
                else
                {
                    // New database: Use EnsureCreated safely
                    _logger.LogInformation("New database detected, ensuring schema...");
                    await _context.Database.EnsureCreatedAsync();
                }

                // Verify critical columns exist
                await VerifyCriticalColumnsAsync();

                var productCount = await _context.Products.CountAsync();
                _logger.LogInformation("Database initialization completed. Product count: {Count}", productCount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed");
                return false;
            }
        }

        /// <summary>
        /// Check if database has existing data
        /// </summary>
        private async Task<bool> HasExistingDataAsync()
        {
            try
            {
                // Check if tables exist and have data
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM sqlite_master 
                    WHERE type='table' AND name='Products'";

                var tableExists = (long)(await command.ExecuteScalarAsync() ?? 0L) > 0;
                
                if (!tableExists) return false;

                command.CommandText = "SELECT COUNT(*) FROM Products";
                var productCount = (long)(await command.ExecuteScalarAsync() ?? 0L);

                return productCount > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Apply migrations safely
        /// </summary>
        private async Task ApplyMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Applying database migrations...");
                
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var pendingCount = pendingMigrations.Count();
                
                if (pendingCount > 0)
                {
                    _logger.LogInformation("Found {Count} pending migrations", pendingCount);
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("No pending migrations found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed, checking if we can continue safely...");
                
                // Check if we can continue with current schema
                var canContinue = await VerifyCriticalColumnsAsync();
                if (!canContinue)
                {
                    throw new InvalidOperationException("Critical database schema issues detected", ex);
                }
            }
        }

        /// <summary>
        /// Verify all critical columns exist
        /// </summary>
        private async Task<bool> VerifyCriticalColumnsAsync()
        {
            try
            {
                _logger.LogInformation("Verifying critical columns...");

                var criticalColumns = new[]
                {
                    "KoctasEanBarcode",
                    "KoctasEanIstanbulBarcode", 
                    "PttUrunStokKodu",
                    "N11CatalogId",
                    "N11ProductCode",
                    "HepsiburadaSellerStockCode",
                    "ImageUrl1",
                    "ImageUrls",
                    "MarketplaceImageUrls",
                    "EntegraUrunId",
                    "EntegraUrunKodu",
                    "EntegraBarkod"
                };

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA table_info(Products)";
                
                var existingColumns = new List<string>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    existingColumns.Add(reader.GetString(1)); // Column name is at index 1
                }

                var missingColumns = criticalColumns.Except(existingColumns).ToList();
                
                if (missingColumns.Any())
                {
                    _logger.LogWarning("Missing critical columns: {Columns}", string.Join(", ", missingColumns));
                    await AddMissingColumnsAsync(missingColumns);
                }
                else
                {
                    _logger.LogInformation("All critical columns verified successfully");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify critical columns");
                return false;
            }
        }

        /// <summary>
        /// Add missing columns safely
        /// </summary>
        private async Task AddMissingColumnsAsync(List<string> missingColumns)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                foreach (var column in missingColumns)
                {
                    try
                    {
                        using var command = connection.CreateCommand();
                        command.CommandText = $"ALTER TABLE Products ADD COLUMN {column} TEXT DEFAULT ''";
                        await command.ExecuteNonQueryAsync();
                        
                        _logger.LogInformation("Added missing column: {Column}", column);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to add column {Column}", column);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add missing columns");
                throw;
            }
        }

        /// <summary>
        /// Create emergency backup
        /// </summary>
        public string CreateEmergencyBackup()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = $"data/emergency_backup_{timestamp}.db";
                
                if (File.Exists("data/products.db"))
                {
                    File.Copy("data/products.db", backupPath);
                    _logger.LogInformation("Emergency backup created: {Path}", backupPath);
                    return backupPath;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create emergency backup");
                return string.Empty;
            }
        }
    }
}
