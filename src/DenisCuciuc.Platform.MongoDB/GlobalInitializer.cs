using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace DenisCuciuc.Platform.MongoDB;

internal static class GlobalInitializer
{
    private static volatile bool _initialized;
    private static readonly object Lock = new();

    public static void Setup()
    {
        if (_initialized) return;

        lock (Lock)
        {
            if (_initialized) return;

            var conventionPack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true)
            };

            ConventionRegistry.Register("DenisCuciuc.PlatformConventions", conventionPack, _ => true);

            try
            {
                BsonSerializer.RegisterSerializer(typeof(DateTime), DateTimeSerializer.UtcInstance);
            }
            catch (Exception ex) when (ex.GetType().Name == "BsonSerializationException")
            {
                // Already registered â€” safe to ignore
            }

            _initialized = true;
        }
    }
}
