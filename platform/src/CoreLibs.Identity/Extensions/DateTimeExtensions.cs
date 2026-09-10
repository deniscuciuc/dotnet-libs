namespace CoreLibs.Identity.Extensions;

public static class DateTimeExtensions
{
    public static long DateTimeToUnix(DateTime? time)
    {
        return time?.ToUnixTimeSpan() ?? 0;
    }

    public static long ToUnixTimeSpan(this DateTime? time)
    {
        return time?.ToUnixTimeSpan() ?? 0;
    }

    public static long ToUnixTimeSpan(this DateTime time)
    {
        return (long)(time - DateTimeOffset.UnixEpoch.UtcDateTime).TotalSeconds;
    }

    public static long TimeSpanToUnix(TimeSpan? time)
    {
        return time?.ToUnixTimeSpan() ?? 0;
    }

    public static long ToUnixTimeSpan(this TimeSpan time)
    {
        return (long)time.TotalSeconds;
    }

    public static DateTime UnixToDateTime(this long time)
    {
        return DateTimeOffset.FromUnixTimeSeconds(time).DateTime;
    }
}
