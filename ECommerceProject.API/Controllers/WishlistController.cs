using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerceProject.Core.DTOs;
using ECommerceProject.Core.Entities;
using ECommerceProject.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceProject.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(AppDbContext context, IMapper mapper, ILogger<WishlistController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<WishlistItemDto>>>> GetWishlist()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var items = await _context.WishlistItems
                    .Include(w => w.Product)
                        .ThenInclude(p => p.Seller)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.AddedAt)
                    .Select(w => new WishlistItemDto
                    {
                        ProductId = w.ProductId,
                        ProductName = w.Product.Name,
                        ProductImage = w.Product.ImageUrl,
                        Price = w.Product.Price,
                        AddedAt = w.AddedAt,
                        SellerStoreName = w.Product.Seller.StoreName
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<WishlistItemDto>> { Data = items });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Favori listesi getirilirken hata oluştu");
                return StatusCode(500, new ApiResponse<IEnumerable<WishlistItemDto>> 
                { 
                    Error = "Favori listesi getirilirken bir hata oluştu" 
                });
            }
        }

        [HttpPost("{productId}")]
        public async Task<ActionResult<ApiResponse<WishlistItemDto>>> AddToWishlist(int productId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var product = await _context.Products
                    .Include(p => p.Seller)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return NotFound(new ApiResponse<WishlistItemDto> { Error = "Ürün bulunamadı" });
                }

                var existingItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existingItem != null)
                {
                    return BadRequest(new ApiResponse<WishlistItemDto> 
                    { 
                        Error = "Bu ürün zaten favorilerinizde" 
                    });
                }

                var wishlistItem = new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                };

                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync();

                var itemDto = new WishlistItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product.ImageUrl,
                    Price = product.Price,
                    AddedAt = wishlistItem.AddedAt,
                    SellerStoreName = product.Seller?.StoreName
                };

                return Ok(new ApiResponse<WishlistItemDto> { Data = itemDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün favorilere eklenirken hata oluştu");
                return StatusCode(500, new ApiResponse<WishlistItemDto> 
                { 
                    Error = "Ürün favorilere eklenirken bir hata oluştu" 
                });
            }
        }

        [HttpDelete("{productId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveFromWishlist(int productId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem == null)
                {
                    return NotFound(new ApiResponse<bool> { Error = "Ürün favorilerde bulunamadı" });
                }

                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool> { Data = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün favorilerden silinirken hata oluştu");
                return StatusCode(500, new ApiResponse<bool> 
                { 
                    Error = "Ürün favorilerden silinirken bir hata oluştu" 
                });
            }
        }
    }
} 