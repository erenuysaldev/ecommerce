using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerceProject.Core.DTOs;
using ECommerceProject.Core.Entities;
using ECommerceProject.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ECommerceProject.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, IMapper mapper, ILogger<AdminController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Bekleyen satıcı onaylarını listele
        /// </summary>
        [HttpGet("pending-sellers")]
        public async Task<ActionResult<ApiResponse<IEnumerable<SellerDto>>>> GetPendingSellers()
        {
            var sellers = await _context.Sellers
                .Where(s => !s.IsApproved)
                .ToListAsync();

            return Ok(new ApiResponse<IEnumerable<SellerDto>>
            {
                Data = _mapper.Map<IEnumerable<SellerDto>>(sellers)
            });
        }

        /// <summary>
        /// Bekleyen değerlendirmeleri listele
        /// </summary>
        [HttpGet("pending-reviews")]
        public async Task<ActionResult<ApiResponse<IEnumerable<SellerReviewDto>>>> GetPendingReviews()
        {
            var reviews = await _context.SellerReviews
                .Include(r => r.User)
                .Include(r => r.Seller)
                .Where(r => !r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(new ApiResponse<IEnumerable<SellerReviewDto>>
            {
                Data = _mapper.Map<IEnumerable<SellerReviewDto>>(reviews)
            });
        }

        /// <summary>
        /// Değerlendirmeyi onayla/reddet
        /// </summary>
        [HttpPut("reviews/{reviewId}/approve")]
        public async Task<ActionResult<ApiResponse<SellerReviewDto>>> ApproveReview(int reviewId, [FromBody] ApproveReviewDto dto)
        {
            var review = await _context.SellerReviews
                .Include(r => r.User)
                .Include(r => r.Seller)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return NotFound(new ApiResponse<SellerReviewDto> { Error = "Değerlendirme bulunamadı" });
            }

            review.IsApproved = dto.IsApproved;
            
            if (review.IsApproved)
            {
                // Satıcının ortalama puanını güncelle
                var averageRating = await _context.SellerReviews
                    .Where(r => r.SellerId == review.SellerId && r.IsApproved)
                    .AverageAsync(r => r.Rating);

                review.Seller.Rating = (decimal)averageRating;
            }

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<SellerReviewDto>
            {
                Data = _mapper.Map<SellerReviewDto>(review)
            });
        }

        /// <summary>
        /// Sistem istatistiklerini getir
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
        {
            try
            {
                var stats = new DashboardStatsDto
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalSellers = await _context.Sellers.CountAsync(),
                    TotalProducts = await _context.Products.CountAsync(),
                    TotalOrders = await _context.Orders.CountAsync(),
                    PendingSellers = await _context.Sellers.CountAsync(s => !s.IsApproved),
                    PendingReviews = await _context.SellerReviews.CountAsync(r => !r.IsApproved),
                    TotalRevenue = await _context.Orders
                        .Where(o => o.PaymentStatus == "Completed")
                        .SumAsync(o => o.TotalAmount),
                    RecentOrders = await _context.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .Take(5)
                        .Select(o => new RecentOrderDto
                        {
                            OrderId = o.Id,
                            OrderDate = o.OrderDate,
                            TotalAmount = o.TotalAmount,
                            Status = o.Status
                        })
                        .ToListAsync()
                };

                return Ok(new ApiResponse<DashboardStatsDto> { Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard istatistikleri alınırken hata oluştu");
                return StatusCode(500, new ApiResponse<DashboardStatsDto> 
                { 
                    Error = "Dashboard istatistikleri alınırken bir hata oluştu" 
                });
            }
        }
    }
} 