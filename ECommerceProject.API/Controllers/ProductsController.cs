using ECommerceProject.Core.Entities;
using ECommerceProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ECommerceProject.Core.DTOs;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace ECommerceProject.API.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private const string ProductsCacheKey = "products_all";
        private const string ProductCacheKeyPrefix = "product_";

        public ProductsController(
            AppDbContext context, 
            ILogger<ProductsController> logger,
            IMapper mapper,
            IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _cache = cache;
        }

        /// <summary>
        /// Tüm ürünleri listeler
        /// </summary>
        /// <returns>Ürün listesi</returns>
        /// <response code="200">Ürünler başarıyla listelendi</response>
        /// <response code="401">Yetkilendirme hatası</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetProducts()
        {
            try
            {
                _logger.LogInformation("Ürünler listeleniyor");

                // Cache'den veriyi almayı dene
                if (_cache.TryGetValue(ProductsCacheKey, out IEnumerable<ProductDto> cachedProducts))
                {
                    _logger.LogInformation("Ürünler cache'den alındı");
                    return Ok(new ApiResponse<IEnumerable<ProductDto>> { Data = cachedProducts });
                }

                // Cache'de yoksa veritabanından al
                var products = await _context.Products.Include(p => p.Category).ToListAsync();
                var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

                // Cache'e kaydet (1 saat süreyle)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                
                _cache.Set(ProductsCacheKey, productDtos, cacheOptions);
                
                _logger.LogInformation($"{products.Count} adet ürün listelendi");
                
                return Ok(new ApiResponse<IEnumerable<ProductDto>> { Data = productDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürünler listelenirken hata oluştu");
                throw;
            }
        }

        /// <summary>
        /// ID'ye göre ürün getirir
        /// </summary>
        /// <param name="id">Ürün ID</param>
        /// <returns>Ürün detayı</returns>
        /// <response code="200">Ürün başarıyla getirildi</response>
        /// <response code="404">Ürün bulunamadı</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            var cacheKey = $"{ProductCacheKeyPrefix}{id}";

            // Cache'den veriyi almayı dene
            if (_cache.TryGetValue(cacheKey, out ProductDto cachedProduct))
            {
                _logger.LogInformation($"Ürün (ID: {id}) cache'den alındı");
                return Ok(new ApiResponse<ProductDto> { Data = cachedProduct });
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new ApiResponse<ProductDto> { Error = "Ürün bulunamadı" });
            }

            var productDto = _mapper.Map<ProductDto>(product);

            // Cache'e kaydet (1 saat süreyle)
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));
            
            _cache.Set(cacheKey, productDto, cacheOptions);

            return Ok(new ApiResponse<ProductDto> { Data = productDto });
        }

        /// <summary>
        /// Yeni ürün ekler
        /// </summary>
        /// <param name="productDto">Ürün bilgileri</param>
        /// <returns>Eklenen ürün</returns>
        /// <response code="201">Ürün başarıyla eklendi</response>
        /// <response code="400">Geçersiz istek</response>
        [HttpPost]
        [Authorize(Roles = "Seller,Admin")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(CreateProductDto productDto)
        {
            try
            {
                // Satıcı kontrolü
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsApproved);

                if (seller == null && !User.IsInRole("Admin"))
                {
                    return BadRequest(new ApiResponse<ProductDto> 
                    { 
                        Error = "Onaylanmış bir satıcı hesabınız olmalıdır" 
                    });
                }

                var product = _mapper.Map<Product>(productDto);
                
                if (!User.IsInRole("Admin"))
                {
                    product.SellerId = seller.Id;
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var productToReturn = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == product.Id);

                return CreatedAtAction(nameof(GetProduct), 
                    new { id = product.Id }, 
                    new ApiResponse<ProductDto> 
                    { 
                        Data = _mapper.Map<ProductDto>(productToReturn) 
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün oluşturulurken hata oluştu");
                return StatusCode(500, new ApiResponse<ProductDto> 
                { 
                    Error = "Ürün oluşturulurken bir hata oluştu" 
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, UpdateProductDto productDto)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Seller)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto> { Error = "Ürün bulunamadı" });
                }

                // Sadece ürünün satıcısı veya admin güncelleyebilir
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (product.Seller?.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                _mapper.Map(productDto, product);
                await _context.SaveChangesAsync();

                var updatedProduct = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id);

                return Ok(new ApiResponse<ProductDto> 
                { 
                    Data = _mapper.Map<ProductDto>(updatedProduct) 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün güncellenirken hata oluştu");
                return StatusCode(500, new ApiResponse<ProductDto> 
                { 
                    Error = "Ürün güncellenirken bir hata oluştu" 
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Seller)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return NotFound(new ApiResponse<bool> { Error = "Ürün bulunamadı" });
                }

                // Sadece ürünün satıcısı veya admin silebilir
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (product.Seller?.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool> { Data = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün silinirken hata oluştu");
                return StatusCode(500, new ApiResponse<bool> 
                { 
                    Error = "Ürün silinirken bir hata oluştu" 
                });
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        [HttpGet("filter")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> FilterProducts([FromQuery] ProductFilterDto filter)
        {
            try
            {
                _logger.LogInformation("Ürünler filtreleniyor: {@Filter}", filter);

                var query = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                // Arama
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    query = query.Where(p => 
                        p.Name.Contains(filter.SearchTerm) || 
                        p.Description.Contains(filter.SearchTerm));
                }

                // Fiyat filtresi
                if (filter.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= filter.MaxPrice.Value);
                }

                // Kategori filtresi
                if (filter.CategoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
                }

                // Sıralama
                if (!string.IsNullOrWhiteSpace(filter.SortBy))
                {
                    query = filter.SortBy.ToLower() switch
                    {
                        "name" => filter.SortDirection?.ToUpper() == "DESC" 
                            ? query.OrderByDescending(p => p.Name)
                            : query.OrderBy(p => p.Name),
                        
                        "price" => filter.SortDirection?.ToUpper() == "DESC"
                            ? query.OrderByDescending(p => p.Price)
                            : query.OrderBy(p => p.Price),
                        
                        "stock" => filter.SortDirection?.ToUpper() == "DESC"
                            ? query.OrderByDescending(p => p.Stock)
                            : query.OrderBy(p => p.Stock),
                        
                        _ => query.OrderBy(p => p.Id)
                    };
                }

                // Sayfalama
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize);

                query = query.Skip((filter.PageNumber - 1) * filter.PageSize)
                            .Take(filter.PageSize);

                var products = await query.ToListAsync();

                _logger.LogInformation($"Filtreleme sonucu {products.Count} adet ürün bulundu");

                var response = new ApiResponse<IEnumerable<ProductDto>>
                {
                    Data = _mapper.Map<IEnumerable<ProductDto>>(products),
                    Meta = new
                    {
                        TotalItems = totalItems,
                        TotalPages = totalPages,
                        CurrentPage = filter.PageNumber,
                        PageSize = filter.PageSize
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürünler filtrelenirken hata oluştu: {@Filter}", filter);
                throw;
            }
        }
    }
} 