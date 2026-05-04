using DenisCuciuc.Platform.Postgres;

namespace DenisCuciuc.Platform.Postgres.Tests;

public class PostgresNamingConventionTests
{
    [Fact]
    public void AllValues_AreDefined()
    {
        var values = Enum.GetValues<PostgresNamingConvention>();
        Assert.Contains(PostgresNamingConvention.None, values);
        Assert.Contains(PostgresNamingConvention.SnakeCase, values);
        Assert.Contains(PostgresNamingConvention.LowerCase, values);
        Assert.Contains(PostgresNamingConvention.CamelCase, values);
    }
}
