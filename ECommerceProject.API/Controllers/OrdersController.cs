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
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(AppDbContext context, IMapper mapper, ILogger<OrdersController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Yeni sipariş oluşturma
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<OrderDetailsDto>>> CreateOrder(CreateOrderDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Ürünleri ve satıcıları kontrol et
                var productIds = dto.Items.Select(i => i.ProductId).ToList();
                var products = await _context.Products
                    .Include(p => p.Seller)
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                if (products.Count != dto.Items.Count)
                {
                    return BadRequest(new ApiResponse<OrderDetailsDto> 
                    { 
                        Error = "Bazı ürünler bulunamadı" 
                    });
                }

                // Stok kontrolü
                foreach (var item in dto.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    if (product.Stock < item.Quantity)
                    {
                        return BadRequest(new ApiResponse<OrderDetailsDto> 
                        { 
                            Error = $"{product.Name} için yeterli stok yok" 
                        });
                    }
                }

                // Siparişi oluştur
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    ShippingAddress = dto.ShippingAddress,
                    ContactPhone = dto.ContactPhone,
                    PaymentMethod = dto.PaymentMethod,
                    PaymentStatus = "Pending",
                    OrderItems = new List<OrderItem>()
                };

                decimal totalAmount = 0;

                // Sipariş kalemlerini oluştur
                foreach (var item in dto.Items)
                {
                    var product = products.First(p => p.Id == item.ProductId);
                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        SellerId = product.SellerId ?? 0,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        Status = "Pending"
                    };

                    totalAmount += orderItem.UnitPrice * orderItem.Quantity;
                    order.OrderItems.Add(orderItem);

                    // Stok güncelle
                    product.Stock -= item.Quantity;
                }

                order.TotalAmount = totalAmount;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var orderDetails = await GetOrderDetails(order.Id);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, 
                    new ApiResponse<OrderDetailsDto> { Data = orderDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş oluşturulurken hata oluştu");
                return StatusCode(500, new ApiResponse<OrderDetailsDto> 
                { 
                    Error = "Sipariş oluşturulurken bir hata oluştu" 
                });
            }
        }

        /// <summary>
        /// Sipariş detaylarını getirme
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<OrderDetailsDto>>> GetOrder(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var orderDetails = await GetOrderDetails(id);

                if (orderDetails == null)
                {
                    return NotFound(new ApiResponse<OrderDetailsDto> { Error = "Sipariş bulunamadı" });
                }

                // Sadece kendi siparişini görebilir (Admin hariç)
                var order = await _context.Orders.FindAsync(id);
                if (order.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                return Ok(new ApiResponse<OrderDetailsDto> { Data = orderDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş detayları getirilirken hata oluştu");
                return StatusCode(500, new ApiResponse<OrderDetailsDto> 
                { 
                    Error = "Sipariş detayları getirilirken bir hata oluştu" 
                });
            }
        }

        /// <summary>
        /// Satıcının kendisine ait siparişleri listeler
        /// </summary>
        [HttpGet("seller")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<IEnumerable<OrderDetailsDto>>>> GetSellerOrders([FromQuery] string status = null)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null)
                {
                    return NotFound(new ApiResponse<IEnumerable<OrderDetailsDto>> 
                    { 
                        Error = "Satıcı profili bulunamadı" 
                    });
                }

                var query = _context.OrderItems
                    .Include(i => i.Order)
                    .Include(i => i.Product)
                    .Where(i => i.SellerId == seller.Id);

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(i => i.Status == status);
                }

                var orderItems = await query.OrderByDescending(i => i.Order.OrderDate).ToListAsync();

                var orders = orderItems
                    .GroupBy(i => i.Order)
                    .Select(g => new OrderDetailsDto
                    {
                        Id = g.Key.Id,
                        OrderDate = g.Key.OrderDate,
                        Status = g.Key.Status,
                        TotalAmount = g.Sum(i => i.UnitPrice * i.Quantity),
                        ShippingAddress = g.Key.ShippingAddress,
                        ContactPhone = g.Key.ContactPhone,
                        PaymentMethod = g.Key.PaymentMethod,
                        PaymentStatus = g.Key.PaymentStatus,
                        Items = g.Select(i => new OrderItemDetailsDto
                        {
                            ProductId = i.ProductId,
                            ProductName = i.Product.Name,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            Status = i.Status
                        }).ToList()
                    });

                return Ok(new ApiResponse<IEnumerable<OrderDetailsDto>> { Data = orders });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satıcı siparişleri listelenirken hata oluştu");
                return StatusCode(500, new ApiResponse<IEnumerable<OrderDetailsDto>> 
                { 
                    Error = "Satıcı siparişleri listelenirken bir hata oluştu" 
                });
            }
        }

        /// <summary>
        /// Satıcının sipariş durumunu güncellemesi
        /// </summary>
        [HttpPut("seller/items/{orderItemId}/status")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<OrderItemDetailsDto>>> UpdateOrderItemStatus(
            int orderItemId, 
            [FromBody] UpdateOrderItemStatusDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null)
                {
                    return NotFound(new ApiResponse<OrderItemDetailsDto> 
                    { 
                        Error = "Satıcı profili bulunamadı" 
                    });
                }

                var orderItem = await _context.OrderItems
                    .Include(i => i.Product)
                    .Include(i => i.Order)
                    .FirstOrDefaultAsync(i => i.Id == orderItemId && i.SellerId == seller.Id);

                if (orderItem == null)
                {
                    return NotFound(new ApiResponse<OrderItemDetailsDto> 
                    { 
                        Error = "Sipariş kalemi bulunamadı" 
                    });
                }

                // Durumu güncelle
                orderItem.Status = dto.Status;
                await _context.SaveChangesAsync();

                // Tüm sipariş kalemlerinin durumunu kontrol et
                var allOrderItems = await _context.OrderItems
                    .Where(i => i.OrderId == orderItem.OrderId)
                    .ToListAsync();

                // Eğer tüm kalemler tamamlandıysa siparişin durumunu güncelle
                if (allOrderItems.All(i => i.Status == "Delivered"))
                {
                    orderItem.Order.Status = "Delivered";
                    await _context.SaveChangesAsync();
                }

                return Ok(new ApiResponse<OrderItemDetailsDto>
                {
                    Data = new OrderItemDetailsDto
                    {
                        ProductId = orderItem.ProductId,
                        ProductName = orderItem.Product.Name,
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.UnitPrice,
                        Status = orderItem.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş kalemi güncellenirken hata oluştu");
                return StatusCode(500, new ApiResponse<OrderItemDetailsDto> 
                { 
                    Error = "Sipariş kalemi güncellenirken bir hata oluştu" 
                });
            }
        }

        /// <summary>
        /// Sipariş istatistiklerini getir
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<OrderStatsDto>>> GetOrderStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                    .ToListAsync();

                var stats = new OrderStatsDto
                {
                    TotalOrders = orders.Count,
                    TotalRevenue = orders.Sum(o => o.TotalAmount),
                    AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                    OrdersByStatus = orders.GroupBy(o => o.Status)
                        .Select(g => new OrderStatusCountDto
                        {
                            Status = g.Key,
                            Count = g.Count()
                        }).ToList(),
                    DailyStats = orders.GroupBy(o => o.OrderDate.Date)
                        .Select(g => new DailyOrderStatsDto
                        {
                            Date = g.Key,
                            OrderCount = g.Count(),
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Date)
                        .ToList(),
                    PaymentMethodStats = orders.GroupBy(o => o.PaymentMethod)
                        .Select(g => new PaymentMethodStatsDto
                        {
                            Method = g.Key,
                            Count = g.Count(),
                            TotalAmount = g.Sum(o => o.TotalAmount)
                        }).ToList()
                };

                return Ok(new ApiResponse<OrderStatsDto> { Data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sipariş istatistikleri alınırken hata oluştu");
                return StatusCode(500, new ApiResponse<OrderStatsDto> 
                { 
                    Error = "Sipariş istatistikleri alınırken bir hata oluştu" 
                });
            }
        }

        /// <summary>
        /// Gelişmiş sipariş arama ve filtreleme
        /// </summary>
        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<OrderDetailsDto>>>> SearchOrders([FromQuery] OrderSearchDto searchDto)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Seller)
                    .AsQueryable();

                // Filtreleri uygula
                if (!string.IsNullOrEmpty(searchDto.Status))
                {
                    query = query.Where(o => o.Status == searchDto.Status);
                }

                if (!string.IsNullOrEmpty(searchDto.PaymentStatus))
                {
                    query = query.Where(o => o.PaymentStatus == searchDto.PaymentStatus);
                }

                if (searchDto.MinAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount >= searchDto.MinAmount.Value);
                }

                if (searchDto.MaxAmount.HasValue)
                {
                    query = query.Where(o => o.TotalAmount <= searchDto.MaxAmount.Value);
                }

                if (searchDto.StartDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate >= searchDto.StartDate.Value);
                }

                if (searchDto.EndDate.HasValue)
                {
                    query = query.Where(o => o.OrderDate <= searchDto.EndDate.Value);
                }

                // Sıralama
                query = searchDto.SortBy?.ToLower() switch
                {
                    "date_desc" => query.OrderByDescending(o => o.OrderDate),
                    "date_asc" => query.OrderBy(o => o.OrderDate),
                    "amount_desc" => query.OrderByDescending(o => o.TotalAmount),
                    "amount_asc" => query.OrderBy(o => o.TotalAmount),
                    _ => query.OrderByDescending(o => o.OrderDate)
                };

                // Sayfalama
                var totalItems = await query.CountAsync();
                var pageSize = searchDto.PageSize ?? 10;
                var pageNumber = searchDto.Page ?? 1;

                var orders = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var orderDtos = orders.Select(o => new OrderDetailsDto
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    ShippingAddress = o.ShippingAddress,
                    ContactPhone = o.ContactPhone,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    Items = o.OrderItems.Select(i => new OrderItemDetailsDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Status = i.Status,
                        SellerStoreName = i.Seller?.StoreName
                    }).ToList()
                });

                var result = new PagedResult<OrderDetailsDto>
                {
                    Items = orderDtos,
                    TotalItems = totalItems,
                    PageSize = pageSize,
                    CurrentPage = pageNumber,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return Ok(new ApiResponse<PagedResult<OrderDetailsDto>> { Data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Siparişler aranırken hata oluştu");
                return StatusCode(500, new ApiResponse<PagedResult<OrderDetailsDto>> 
                { 
                    Error = "Siparişler aranırken bir hata oluştu" 
                });
            }
        }

        private async Task<OrderDetailsDto> GetOrderDetails(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Seller)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;

            return new OrderDetailsDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                ShippingAddress = order.ShippingAddress,
                ContactPhone = order.ContactPhone,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                Items = order.OrderItems.Select(item => new OrderItemDetailsDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Status = item.Status,
                    SellerStoreName = item.Seller?.StoreName
                }).ToList()
            };
        }
    }
} 