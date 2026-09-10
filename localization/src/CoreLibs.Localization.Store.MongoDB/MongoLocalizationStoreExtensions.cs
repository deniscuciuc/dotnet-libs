using CoreLibs.Localization;
using CoreLibs.Localization.Store.MongoDB.Serializers;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace CoreLibs.Localization.Store.MongoDB;

public static class MongoLocalizationStoreExtensions
{
    public static IServiceCollection AddCoreLocalizationMongo(this IServiceCollection services)
    {
        BsonSerializer.TryRegisterSerializer(LanguageCodeSerializer.Default);
        services.AddSingleton<ILocalizationStore, MongoLocalizationStore>();
        return services;
    }
}
