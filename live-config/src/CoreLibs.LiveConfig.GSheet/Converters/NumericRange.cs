namespace CoreLibs.LiveConfig.GSheet.Converters;

public sealed record NumericRange(double Min, double Max)
{
    public bool Contains(double value)
    {
        return value >= Min && value <= Max;
    }

    public override string ToString()
    {
        return $"{Min}-{Max}";
    }
}
