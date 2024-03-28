using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PlantBasedPizza.LoyaltyPoints.Adapters;
using PlantBasedPizza.LoyaltyPoints.Core;
using PlantBasedPizza.Shared;

namespace PlantBasedPizza.LoyaltyPoints.Shared;

public static class Setup
{
    public static IServiceCollection AddLoyaltyServices(this IServiceCollection services, IConfiguration configuration)
    {
        var client = new MongoClient(configuration["DatabaseConnection"]);

        services.AddSingleton(client);

        services.AddSingleton<ICustomerLoyaltyPointsRepository, CustomerLoyaltyPointRepository>();
        services.AddSingleton<AddLoyaltyPointsCommandHandler>();
        services.AddSingleton<SpendLoyaltyPointsCommandHandler>();

        BsonClassMap.RegisterClassMap<CustomerLoyaltyPoints>(map =>
        {
            map.AutoMap();
            map.SetIgnoreExtraElements(true);
            map.SetIgnoreExtraElementsIsInherited(true);
        });

        services.AddSharedInfrastructure(configuration, "LoyaltyPoints");
        
        return services;
    }
}