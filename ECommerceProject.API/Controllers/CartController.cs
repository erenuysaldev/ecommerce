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
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CartController> _logger;

        public CartController(AppDbContext context, IMapper mapper, ILogger<CartController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<CartDto>>> GetCart()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var cart = await GetOrCreateCart(userId);

                var cartDto = new CartDto
                {
                    Id = cart.Id,
                    Items = cart.Items.Select(i => new CartItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        ProductImage = i.Product.ImageUrl,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.UnitPrice * i.Quantity,
                        SellerStoreName = i.Product.Seller?.StoreName
                    }).ToList(),
                    TotalAmount = cart.Items.Sum(i => i.UnitPrice * i.Quantity)
                };

                return Ok(new ApiResponse<CartDto> { Data = cartDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet getirilirken hata oluştu");
                return StatusCode(500, new ApiResponse<CartDto> 
                { 
                    Error = "Sepet getirilirken bir hata oluştu" 
                });
            }
        }

        [HttpPost("items")]
        public async Task<ActionResult<ApiResponse<CartDto>>> AddToCart(AddToCartDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var cart = await GetOrCreateCart(userId);

                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    return NotFound(new ApiResponse<CartDto> { Error = "Ürün bulunamadı" });
                }

                if (product.Stock < dto.Quantity)
                {
                    return BadRequest(new ApiResponse<CartDto> { Error = "Yeterli stok yok" });
                }

                var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
                if (existingItem != null)
                {
                    existingItem.Quantity += dto.Quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UnitPrice = product.Price
                    });
                }

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await GetCart();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün sepete eklenirken hata oluştu");
                return StatusCode(500, new ApiResponse<CartDto> 
                { 
                    Error = "Ürün sepete eklenirken bir hata oluştu" 
                });
            }
        }

        [HttpPut("items/{productId}")]
        public async Task<ActionResult<ApiResponse<CartDto>>> UpdateCartItem(int productId, UpdateCartItemDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var cart = await GetOrCreateCart(userId);

                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (cartItem == null)
                {
                    return NotFound(new ApiResponse<CartDto> { Error = "Ürün sepette bulunamadı" });
                }

                var product = await _context.Products.FindAsync(productId);
                if (product.Stock < dto.Quantity)
                {
                    return BadRequest(new ApiResponse<CartDto> { Error = "Yeterli stok yok" });
                }

                cartItem.Quantity = dto.Quantity;
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await GetCart();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sepet güncellenirken hata oluştu");
                return StatusCode(500, new ApiResponse<CartDto> 
                { 
                    Error = "Sepet güncellenirken bir hata oluştu" 
                });
            }
        }

        [HttpDelete("items/{productId}")]
        public async Task<ActionResult<ApiResponse<CartDto>>> RemoveFromCart(int productId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var cart = await GetOrCreateCart(userId);

                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (cartItem == null)
                {
                    return NotFound(new ApiResponse<CartDto> { Error = "Ürün sepette bulunamadı" });
                }

                _context.CartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return await GetCart();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün sepetten silinirken hata oluştu");
                return StatusCode(500, new ApiResponse<CartDto> 
                { 
                    Error = "Ürün sepetten silinirken bir hata oluştu" 
                });
            }
        }

        private async Task<Cart> GetOrCreateCart(string userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = new List<CartItem>()
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }
    }
} 