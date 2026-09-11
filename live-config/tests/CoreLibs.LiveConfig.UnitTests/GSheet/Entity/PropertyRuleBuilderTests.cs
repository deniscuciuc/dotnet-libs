using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.GSheet.Entity;
using CoreLibs.LiveConfig.GSheet.Schema;

namespace CoreLibs.LiveConfig.UnitTests.GSheet.Entity;

public record PriceRow(
    [property: GSheetColumn] string ItemId,
    [property: GSheetColumn] decimal Price,
    [property: GSheetColumn] decimal? DiscountPrice);

public class PropertyRuleBuilderTests
{
    [Fact]
    public void Must_PropertyOnly_ValidatesEachRow()
    {
        var builder = new GSheetEntityBuilder<PriceRow>();
        builder.Rule(x => x.Price).Must(p => p > 0).WithMessage("Price must be positive");

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<PriceRow>
        {
            new("a", 10, null),
            new("b", 0, null),
            new("c", -5, null)
        };
        var context = new GSheetImportContext();

        var result = adapter.RowValidator!.Validate(rows, context);

        Assert.True(result.HasErrors);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Contains("Price must be positive", e.Reason));
    }

    [Fact]
    public void Must_RowContextPredicate_ValidatesWithRowContext()
    {
        var builder = new GSheetEntityBuilder<PriceRow>();
        builder.Rule(x => x.DiscountPrice)
            .Must((row, dp) => dp is null || dp < row.Price)
            .WithMessage("Discount must be less than price");

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<PriceRow>
        {
            new("a", 10, 5), // OK
            new("b", 10, 15), // FAIL — discount > price
            new("c", 10, null) // OK — no discount
        };
        var context = new GSheetImportContext();

        var result = adapter.RowValidator!.Validate(rows, context);

        Assert.True(result.HasErrors);
        Assert.Single(result.Errors);
        Assert.Equal(1, result.Errors[0].RowIndex); // row index 1
    }

    [Fact]
    public void WithMessage_DynamicFactory_IncludesValueInMessage()
    {
        var builder = new GSheetEntityBuilder<PriceRow>();
        builder.Rule(x => x.Price)
            .Must(p => p > 0)
            .WithMessage(p => $"Invalid price: {p}");

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<PriceRow> { new("a", -1, null) };
        var context = new GSheetImportContext();

        var result = adapter.RowValidator!.Validate(rows, context);

        Assert.Contains("Invalid price: -1", result.Errors[0].Reason);
    }

    [Fact]
    public void Rule_WithSheetName_IncludesSheetInError()
    {
        var builder = new GSheetEntityBuilder<PriceRow>();
        builder.Rule(x => x.Price).Must(p => p > 0).WithMessage("Bad price");

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<PriceRow> { new("a", 0, null) };
        var context = new GSheetImportContext { SheetName = "Prices" };

        var result = adapter.RowValidator!.Validate(rows, context);

        Assert.Equal("Prices", result.Errors[0].SheetName);
    }

    [Fact]
    public void MultipleRules_AllValidated()
    {
        var builder = new GSheetEntityBuilder<PriceRow>();
        builder.Rule(x => x.Price).Must(p => p > 0).WithMessage("Price positive");
        builder.Rule(x => x.ItemId).Must(id => !string.IsNullOrEmpty(id)).WithMessage("ItemId required");

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<PriceRow> { new("", -1, null) };
        var context = new GSheetImportContext();

        var result = adapter.RowValidator!.Validate(rows, context);

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void AllRowsValid_ReturnsOk()
    {
        var builder = new GSheetEntityBuilder<PriceRow>();
        builder.Rule(x => x.Price).Must(p => p > 0).WithMessage("Must be positive");

        var adapter = builder.Build("TestSheet", "A1:C");
        var rows = new List<PriceRow>
        {
            new("a", 10, null),
            new("b", 20, null)
        };

        var result = adapter.RowValidator!.Validate(rows, new GSheetImportContext());

        Assert.True(result.IsSuccess);
    }
}
