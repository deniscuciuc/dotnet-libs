namespace CoreLibs.Jobs.Hangfire.Tests;

public class JobKindEnumTests
{
    [Fact]
    public void JobProviderKind_AllValues_Defined()
    {
        var values = Enum.GetValues<JobProviderKind>();
        Assert.Contains(JobProviderKind.Hangfire, values);
        Assert.Contains(JobProviderKind.Quartz, values);
    }

    [Fact]
    public void JobStoreKind_AllValues_Defined()
    {
        var values = Enum.GetValues<JobStoreKind>();
        Assert.Contains(JobStoreKind.Postgres, values);
        Assert.Contains(JobStoreKind.MongoDB, values);
    }
}
