using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PlantBasedPizza.Kitchen.Core.Entities;
using PlantBasedPizza.Kitchen.Core.Services;
using PlantBasedPizza.Kitchen.Infrastructure.IntegrationEvents;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

namespace PlantBasedPizza.Kitchen.Infrastructure
{
    using MongoDB.Bson.Serialization;

    public static class Setup
    {
        public static IServiceCollection AddKitchenInfrastructure(this IServiceCollection services,
            IConfiguration configuration, string overrideDatabaseConnection = null)
        {
            var client = new MongoClient(overrideDatabaseConnection ?? configuration["DatabaseConnection"]);

            services.AddSingleton(client);
            services.Configure<ServiceEndpoints>(configuration.GetSection("Services"));
            
            BsonClassMap.RegisterClassMap<KitchenRequest>(map =>
            {
                map.AutoMap();
                map.SetIgnoreExtraElements(true);
                map.SetIgnoreExtraElementsIsInherited(true);
            });
            
            services.AddHttpClient("recipe-service")
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy());
            
            services.AddSingleton<IRecipeService, RecipeService>();
            services.AddSingleton<IKitchenRequestRepository, KitchenRequestRepository>();
            services.AddSingleton<IKitchenEventPublisher, KitchenEventPublisher>();

            return services;
        }
        
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);
            
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(delay);
        }
    }
}