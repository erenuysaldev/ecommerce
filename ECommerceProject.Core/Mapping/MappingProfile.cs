using AutoMapper;
using ECommerceProject.Core.DTOs;
using ECommerceProject.Core.Entities;

namespace ECommerceProject.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Product mappings
            CreateMap<Product, ProductDto>();
            CreateMap<CreateProductDto, Product>();
            CreateMap<UpdateProductDto, Product>();

            // Category mappings
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryDto, Category>();
            CreateMap<UpdateCategoryDto, Category>();

            // Seller mappings
            CreateMap<Seller, SellerDto>();
            CreateMap<CreateSellerDto, Seller>();
            CreateMap<UpdateSellerDto, Seller>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<SellerReview, SellerReviewDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName));

            CreateMap<CreateSellerReviewDto, SellerReview>();

            CreateMap<Order, OrderDetailsDto>();
            CreateMap<OrderItem, OrderItemDetailsDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.SellerStoreName, opt => opt.MapFrom(src => src.Seller.StoreName));
        }
    }
} 