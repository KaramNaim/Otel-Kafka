using Mapster;
using MapsterMapper;
using OTel.Application.Mapper;

namespace OTel.Api.Common;

public static class MapperRegistration
{
    public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        var config = MapsterConfig.GetConfiguredMappingConfig();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
