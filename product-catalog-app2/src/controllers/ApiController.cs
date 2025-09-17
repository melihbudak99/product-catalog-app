using Microsoft.AspNetCore.Mvc;
using product_catalog_app.src.models;
using product_catalog_app.src.services;
using Microsoft.Extensions.Logging;

namespace product_catalog_app.src.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts(
            [FromQuery] string search = "",
            [FromQuery] string category = "",
            [FromQuery] string brand = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var products = await _productService.SearchProductsAsync(search, category, brand, page, pageSize);
                var totalCount = await _productService.GetProductCountAsync(search, category, brand);

                var response = new
                {
                    Products = products.Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        SKU = p.SKU,
                        Brand = p.Brand,
                        Category = p.Category,
                        ImageUrl = p.ImageUrls?.FirstOrDefault() ?? "",
                        Features = p.Features,
                        IsActive = !p.IsArchived // Active = !Archived
                    }),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products via API");
                return StatusCode(500, new { Error = "An error occurred while retrieving products" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound(new { Error = "Product not found" });
                }

                var response = new
                {
                    Id = product.Id,
                    Name = product.Name,
                    SKU = product.SKU,
                    Brand = product.Brand,
                    Category = product.Category,
                    Description = product.Description,
                    Features = product.Features,
                    ImageUrls = product.ImageUrls,
                    MarketplaceImageUrls = product.MarketplaceImageUrls,
                    Weight = product.Weight,
                    Desi = product.Desi,
                    Width = product.Width,
                    Height = product.Height,
                    Depth = product.Depth,
                    WarrantyMonths = product.WarrantyMonths,
                    Material = product.Material,
                    Color = product.Color,
                    EanCode = product.EanCode,
                    IsActive = !product.IsArchived, // Active = !Archived
                    CreatedDate = product.CreatedDate,
                    UpdatedDate = product.UpdatedDate
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId} via API", id);
                return StatusCode(500, new { Error = "An error occurred while retrieving the product" });
            }
        }

        [HttpGet("categories")]
        public ActionResult<IEnumerable<string>> GetCategories()
        {
            try
            {
                var categories = _productService.GetDistinctCategories();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories via API");
                return StatusCode(500, new { Error = "An error occurred while retrieving categories" });
            }
        }

        [HttpGet("brands")]
        public ActionResult<IEnumerable<string>> GetBrands()
        {
            try
            {
                var brands = _productService.GetDistinctBrands();
                return Ok(brands);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving brands via API");
                return StatusCode(500, new { Error = "An error occurred while retrieving brands" });
            }
        }
    }
}
