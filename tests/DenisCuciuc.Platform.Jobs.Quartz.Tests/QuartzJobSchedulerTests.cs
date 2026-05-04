using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;

namespace DenisCuciuc.Platform.Jobs.Quartz.Tests;

public class QuartzJobSchedulerTests
{
    private readonly IScheduler _scheduler = Substitute.For<IScheduler>();
    private readonly ISchedulerFactory _schedulerFactory = Substitute.For<ISchedulerFactory>();
    private readonly QuartzJobScheduler _sut;

    public QuartzJobSchedulerTests()
    {
        _schedulerFactory.GetScheduler(Arg.Any<CancellationToken>()).Returns(_scheduler);
        _sut = new QuartzJobScheduler(_schedulerFactory, NullLogger<QuartzJobScheduler>.Instance);
    }

    [Fact]
    public async Task EnqueueAsync_SchedulesJobWithScheduler()
    {
        await _sut.EnqueueAsync<QuartzSimpleJob>(CancellationToken.None);

        await _scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnqueueAsync_TriggerStartsNow()
    {
        ITrigger? capturedTrigger = null;
        await _scheduler.ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Do<ITrigger>(t => capturedTrigger = t),
            Arg.Any<CancellationToken>());

        await _sut.EnqueueAsync<QuartzSimpleJob>(CancellationToken.None);

        Assert.NotNull(capturedTrigger);
    }

    [Fact]
    public async Task EnqueueAsync_WithArgs_SchedulesJobWithScheduler()
    {
        await _sut.EnqueueAsync<QuartzSimpleJob>("my-args", CancellationToken.None);

        await _scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleAsync_SchedulesJobWithFutureStart()
    {
        var delay = TimeSpan.FromMinutes(10);
        var before = DateTimeOffset.UtcNow.Add(delay).AddSeconds(-2);

        ITrigger? capturedTrigger = null;
        await _scheduler.ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Do<ITrigger>(t => capturedTrigger = t),
            Arg.Any<CancellationToken>());

        await _sut.ScheduleAsync<QuartzSimpleJob>(delay, CancellationToken.None);

        Assert.NotNull(capturedTrigger);
        Assert.True(capturedTrigger.StartTimeUtc >= before);
    }

    [Fact]
    public async Task ScheduleAsync_WithArgs_SchedulesJob()
    {
        await _sut.ScheduleAsync<QuartzSimpleJob>("args", TimeSpan.FromSeconds(30), CancellationToken.None);

        await _scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrUpdateRecurringAsync_WhenJobDoesNotExist_SchedulesJob()
    {
        _scheduler.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(false);

        await _sut.AddOrUpdateRecurringAsync<QuartzSimpleJob>("job-1", "0 * * * *", CancellationToken.None);

        await _scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
        await _scheduler.DidNotReceive().DeleteJob(Arg.Any<JobKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrUpdateRecurringAsync_WhenJobExists_DeletesThenReschedules()
    {
        _scheduler.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(true);

        await _sut.AddOrUpdateRecurringAsync<QuartzSimpleJob>("job-2", "0 0 * * *", CancellationToken.None);

        await _scheduler.Received(1).DeleteJob(
            Arg.Is<JobKey>(k => k.Name == "job-2" && k.Group == "platform"),
            Arg.Any<CancellationToken>());
        await _scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrUpdateRecurringAsync_ConvertsUnixCronToQuartzFormat()
    {
        _scheduler.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(false);

        ITrigger? capturedTrigger = null;
        await _scheduler.ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Do<ITrigger>(t => capturedTrigger = t),
            Arg.Any<CancellationToken>());

        // Unix "0 0 * * *" → Quartz "0 0 0 * * ?"
        await _sut.AddOrUpdateRecurringAsync<QuartzSimpleJob>("cron-job", "0 0 * * *", CancellationToken.None);

        Assert.NotNull(capturedTrigger);
        var cronTrigger = Assert.IsAssignableFrom<ICronTrigger>(capturedTrigger);
        Assert.Equal("0 0 0 * * ?", cronTrigger.CronExpressionString);
    }

    [Fact]
    public async Task AddOrUpdateRecurringAsync_PassesThroughExistingQuartzCron()
    {
        _scheduler.CheckExists(Arg.Any<JobKey>(), Arg.Any<CancellationToken>()).Returns(false);

        ITrigger? capturedTrigger = null;
        await _scheduler.ScheduleJob(
            Arg.Any<IJobDetail>(),
            Arg.Do<ITrigger>(t => capturedTrigger = t),
            Arg.Any<CancellationToken>());

        // Already 6-field Quartz cron — pass through unchanged
        await _sut.AddOrUpdateRecurringAsync<QuartzSimpleJob>("quartz-job", "0 0 12 * * ?", CancellationToken.None);

        Assert.NotNull(capturedTrigger);
        var cronTrigger = Assert.IsAssignableFrom<ICronTrigger>(capturedTrigger);
        Assert.Equal("0 0 12 * * ?", cronTrigger.CronExpressionString);
    }

    [Fact]
    public async Task RemoveRecurringAsync_DeletesJobByIdInPlatformGroup()
    {
        await _sut.RemoveRecurringAsync("remove-me", CancellationToken.None);

        await _scheduler.Received(1).DeleteJob(
            Arg.Is<JobKey>(k => k.Name == "remove-me" && k.Group == "platform"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerRecurringAsync_TriggersJobByIdInPlatformGroup()
    {
        await _sut.TriggerRecurringAsync("trigger-me", CancellationToken.None);

        await _scheduler.Received(1).TriggerJob(
            Arg.Is<JobKey>(k => k.Name == "trigger-me" && k.Group == "platform"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerRecurringAsync_PassesCorrectJobKey()
    {
        var jobId = "special-job";
        await _sut.TriggerRecurringAsync(jobId, CancellationToken.None);

        await _scheduler.Received(1).TriggerJob(
            Arg.Is<JobKey>(k => k.Name == jobId && k.Group == "platform"),
            Arg.Any<CancellationToken>());
    }
}
