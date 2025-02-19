using ECommerceProject.Core.Entities;
using ECommerceProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ECommerceProject.Core.DTOs;
using AutoMapper;

namespace ECommerceProject.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly IMapper _mapper;

        public ProductsController(
            AppDbContext context, 
            ILogger<ProductsController> logger,
            IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetProducts()
        {
            try
            {
                _logger.LogInformation("Ürünler listeleniyor");
                var products = await _context.Products.Include(p => p.Category).ToListAsync();
                
                _logger.LogInformation($"{products.Count} adet ürün listelendi");
                
                var response = new ApiResponse<IEnumerable<ProductDto>>
                {
                    Data = _mapper.Map<IEnumerable<ProductDto>>(products)
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürünler listelenirken hata oluştu");
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new ApiResponse<ProductDto> { Error = "Ürün bulunamadı" });
            }

            var response = new ApiResponse<ProductDto>
            {
                Data = _mapper.Map<ProductDto>(product)
            };

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(CreateProductDto productDto)
        {
            try
            {
                _logger.LogInformation("Yeni ürün ekleniyor: {@ProductDto}", productDto);
                
                var product = _mapper.Map<Product>(productDto);
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Yeni ürün eklendi. ID: {ProductId}", product.Id);

                var response = new ApiResponse<ProductDto>
                {
                    Data = _mapper.Map<ProductDto>(product)
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün eklenirken hata oluştu: {@ProductDto}", productDto);
                throw;
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, UpdateProductDto productDto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new ApiResponse<ProductDto> { Error = "Ürün bulunamadı" });
            }

            _mapper.Map(productDto, product);

            try
            {
                await _context.SaveChangesAsync();
                var response = new ApiResponse<ProductDto>
                {
                    Data = _mapper.Map<ProductDto>(product)
                };
                return Ok(response);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound(new ApiResponse<ProductDto> { Error = "Ürün bulunamadı" });
                }
                throw;
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
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