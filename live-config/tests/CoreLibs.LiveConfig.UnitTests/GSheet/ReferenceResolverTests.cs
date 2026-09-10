using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Pipeline;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class ReferenceResolverTests
{
    // --- Domain types ---
    public class CurrencyConfig
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
    }

    // --- Row type with reference ---
    public class RewardRow
    {
        [GSheetColumn("Currency")]
        [GSheetRef(typeof(CurrencyConfig), nameof(CurrencyConfig.Code))]
        public string Currency { get; set; } = "";

        [GSheetColumn("Amount")] public int Amount { get; set; }
    }

    // --- Row type without references ---
    public class SimpleRow
    {
        [GSheetColumn("Name")] public string Name { get; set; } = "";
    }

    [Fact]
    public void Validate_AllReferencesExist_ReturnsOk()
    {
        var context = new GSheetImportContext();
        context.SetDomain<CurrencyConfig>(new List<CurrencyConfig>
        {
            new() { Code = "USD", Name = "Dollar" },
            new() { Code = "EUR", Name = "Euro" }
        });

        var rows = new List<RewardRow>
        {
            new() { Currency = "USD", Amount = 100 },
            new() { Currency = "EUR", Amount = 50 }
        };

        var result = ReferenceResolver.Validate(rows, context, "Rewards");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_MissingReference_ReturnsError()
    {
        var context = new GSheetImportContext();
        context.SetDomain<CurrencyConfig>(new List<CurrencyConfig>
        {
            new() { Code = "USD", Name = "Dollar" }
        });

        var rows = new List<RewardRow>
        {
            new() { Currency = "USD", Amount = 100 },
            new() { Currency = "INVALID", Amount = 50 }
        };

        var result = ReferenceResolver.Validate(rows, context, "Rewards");

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Contains("INVALID", result.Errors[0].Reason);
        Assert.Contains("CurrencyConfig", result.Errors[0].Reason);
    }

    [Fact]
    public void Validate_NoDomainData_ReturnsErrors()
    {
        var context = new GSheetImportContext();
        // No currency domain set

        var rows = new List<RewardRow>
        {
            new() { Currency = "USD", Amount = 100 }
        };

        var result = ReferenceResolver.Validate(rows, context, "Rewards");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Validate_CaseInsensitiveLookup_Passes()
    {
        var context = new GSheetImportContext();
        context.SetDomain<CurrencyConfig>(new List<CurrencyConfig>
        {
            new() { Code = "USD", Name = "Dollar" }
        });

        var rows = new List<RewardRow>
        {
            new() { Currency = "usd", Amount = 100 }
        };

        var result = ReferenceResolver.Validate(rows, context, "Rewards");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_NoRefAttributes_ReturnsOk()
    {
        var context = new GSheetImportContext();
        var rows = new List<SimpleRow> { new() { Name = "Test" } };

        var result = ReferenceResolver.Validate(rows, context, "Simple");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GetReferencedDomainTypes_ReturnsCorrectTypes()
    {
        var types = ReferenceResolver.GetReferencedDomainTypes(typeof(RewardRow));

        Assert.Single(types);
        Assert.Equal(typeof(CurrencyConfig), types[0]);
    }

    [Fact]
    public void GetReferencedDomainTypes_NoRefs_ReturnsEmpty()
    {
        var types = ReferenceResolver.GetReferencedDomainTypes(typeof(SimpleRow));

        Assert.Empty(types);
    }
}
