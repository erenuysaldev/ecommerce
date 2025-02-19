using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerceProject.Core.DTOs;
using ECommerceProject.Core.Entities;
using ECommerceProject.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ECommerceProject.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SellersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<SellersController> _logger;

        public SellersController(AppDbContext context, IMapper mapper, ILogger<SellersController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<SellerDto>>>> GetSellers()
        {
            var sellers = await _context.Sellers.ToListAsync();
            return Ok(new ApiResponse<IEnumerable<SellerDto>>
            {
                Data = _mapper.Map<IEnumerable<SellerDto>>(sellers)
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SellerDto>>> GetSeller(int id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                return NotFound(new ApiResponse<SellerDto> { Error = "Satıcı bulunamadı" });
            }

            return Ok(new ApiResponse<SellerDto>
            {
                Data = _mapper.Map<SellerDto>(seller)
            });
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SellerDto>>> CreateSeller(CreateSellerDto dto)
        {
            var seller = _mapper.Map<Seller>(dto);
            seller.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            seller.CreatedAt = DateTime.UtcNow;
            seller.IsApproved = false; // Admin onayı gerekiyor
            
            _context.Sellers.Add(seller);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSeller), new { id = seller.Id }, 
                new ApiResponse<SellerDto> { Data = _mapper.Map<SellerDto>(seller) });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<SellerDto>>> UpdateSeller(int id, UpdateSellerDto dto)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                return NotFound(new ApiResponse<SellerDto> { Error = "Satıcı bulunamadı" });
            }

            // Sadece kendi mağazasını güncelleyebilir
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (seller.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _mapper.Map(dto, seller);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<SellerDto>
            {
                Data = _mapper.Map<SellerDto>(seller)
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/approve")]
        public async Task<ActionResult<ApiResponse<SellerDto>>> ApproveSeller(int id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                return NotFound(new ApiResponse<SellerDto> { Error = "Satıcı bulunamadı" });
            }

            seller.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<SellerDto>
            {
                Data = _mapper.Map<SellerDto>(seller)
            });
        }

        // Satıcının kendi ürünlerini listeleyen endpoint
        [HttpGet("my-products")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetMyProducts()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null)
                {
                    return NotFound(new ApiResponse<IEnumerable<ProductDto>> 
                    { 
                        Error = "Satıcı profili bulunamadı" 
                    });
                }

                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.SellerId == seller.Id)
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<ProductDto>>
                {
                    Data = _mapper.Map<IEnumerable<ProductDto>>(products)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satıcı ürünleri listelenirken hata oluştu");
                return StatusCode(500, new ApiResponse<IEnumerable<ProductDto>> 
                { 
                    Error = "Satıcı ürünleri listelenirken bir hata oluştu" 
                });
            }
        }

        // Satıcının satış istatistiklerini getiren endpoint
        [HttpGet("my-stats")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<SellerStatsDto>>> GetMyStats()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var seller = await _context.Sellers
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null)
                {
                    return NotFound(new ApiResponse<SellerStatsDto> 
                    { 
                        Error = "Satıcı profili bulunamadı" 
                    });
                }

                var stats = new SellerStatsDto
                {
                    TotalProducts = seller.Products.Count,
                    TotalSales = seller.TotalSales,
                    Rating = seller.Rating,
                    IsApproved = seller.IsApproved,
                    StoreName = seller.StoreName,
                    JoinDate = seller.CreatedAt
                };

                return Ok(new ApiResponse<SellerStatsDto> { Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satıcı istatistikleri alınırken hata oluştu");
                return StatusCode(500, new ApiResponse<SellerStatsDto> 
                { 
                    Error = "Satıcı istatistikleri alınırken bir hata oluştu" 
                });
            }
        }

        /// <summary>
        /// Toplu ürün ekleme işlemi yapar
        /// </summary>
        /// <param name="dto">Eklenecek ürünlerin listesi</param>
        /// <returns>Eklenen ürünlerin listesi</returns>
        [HttpPost("bulk-create-products")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> BulkCreateProducts(BulkCreateProductDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsApproved);

                if (seller == null)
                {
                    return BadRequest(new ApiResponse<IEnumerable<ProductDto>>
                    {
                        Error = "Onaylanmış bir satıcı hesabınız olmalıdır"
                    });
                }

                var products = dto.Products.Select(p =>
                {
                    var product = _mapper.Map<Product>(p);
                    product.SellerId = seller.Id;
                    return product;
                }).ToList();

                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();

                var createdProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => products.Select(x => x.Id).Contains(p.Id))
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<ProductDto>>
                {
                    Data = _mapper.Map<IEnumerable<ProductDto>>(createdProducts)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu ürün eklenirken hata oluştu");
                return StatusCode(500, new ApiResponse<IEnumerable<ProductDto>>
                {
                    Error = "Toplu ürün eklenirken bir hata oluştu"
                });
            }
        }

        /// <summary>
        /// Toplu stok güncelleme işlemi yapar
        /// </summary>
        /// <param name="dto">Güncellenecek stok bilgileri</param>
        /// <returns>İşlem başarı durumu</returns>
        [HttpPut("bulk-update-stock")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<bool>>> BulkUpdateStock(BulkUpdateStockDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Error = "Satıcı profili bulunamadı"
                    });
                }

                var productIds = dto.Products.Select(p => p.ProductId).ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id) && p.SellerId == seller.Id)
                    .ToListAsync();

                foreach (var product in products)
                {
                    var update = dto.Products.First(p => p.ProductId == product.Id);
                    product.Stock = update.NewStock;
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool> { Data = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu stok güncellenirken hata oluştu");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Error = "Toplu stok güncellenirken bir hata oluştu"
                });
            }
        }
    }
} 