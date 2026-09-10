using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace CoreLibs.Jobs.Quartz.Tests;

public class ToQuartzCronTests
{
    private static (QuartzJobScheduler scheduler, IScheduler inner) CreateScheduler()
    {
        var inner = Substitute.For<IScheduler>();
        inner.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(false);
        var factory = Substitute.For<ISchedulerFactory>();
        factory.GetScheduler(Arg.Any<CancellationToken>()).Returns(inner);
        var sut = new QuartzJobScheduler(factory, NullLogger<QuartzJobScheduler>.Instance);
        return (sut, inner);
    }

    private static async Task<string?> GetCronExpression(string inputCron)
    {
        var (sut, inner) = CreateScheduler();
        ITrigger? capturedTrigger = null;
        await inner.ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Do<ITrigger>(t => capturedTrigger = t),
            Arg.Any<CancellationToken>());

        await sut.AddOrUpdateRecurringAsync<QuartzSimpleJob>("job", inputCron, CancellationToken.None);
        return (capturedTrigger as ICronTrigger)?.CronExpressionString;
    }

    [Theory]
    [InlineData("0 * * * *", "0 0 * * * ?")]    // every hour — DOM=* DOW=* → DOW=?
    [InlineData("0 0 * * *", "0 0 0 * * ?")]    // midnight daily — DOM=* DOW=* → DOW=?
    [InlineData("30 9 * * 1", "0 30 9 ? * 1")]  // Monday 9:30 — DOM=* DOW=1 → DOM=?
    [InlineData("* * * * *", "0 * * * * ?")]    // every minute — DOM=* DOW=* → DOW=?
    public async Task UnixCron_ConvertedToQuartzCron(string unixCron, string expectedQuartzCron)
    {
        var result = await GetCronExpression(unixCron);
        Assert.Equal(expectedQuartzCron, result);
    }

    [Theory]
    [InlineData("0 0 0 * * ?")]     // already 6-field, valid
    [InlineData("0 30 9 ? * MON")]  // already 6-field, DOM=? DOW=MON, valid
    public async Task AlreadyQuartzCron_PassedThrough(string quartzCron)
    {
        var result = await GetCronExpression(quartzCron);
        Assert.Equal(quartzCron, result);
    }
}
