using Mapster;
using OTel.Application.DTO.Order;
using OTel.Application.DTO.Product;
using OTel.Domain.Models;

namespace OTel.Application.Mapper;

public static class MapsterConfig
{
    public static TypeAdapterConfig GetConfiguredMappingConfig()
    {
        var config = new TypeAdapterConfig();

        config.NewConfig<Product, ProductDetailsDto>();
        config.NewConfig<CreateProductDto, Product>();
        config.NewConfig<UpdateProductDto, Product>();

        config.NewConfig<Order, OrderDetailsDto>()
              .Map(dest => dest.ProductName, src => src.Product.Name);
        config.NewConfig<CreateOrderDto, Order>();

        return config;
    }
}
