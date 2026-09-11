using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CoreLibs.Jobs.Hangfire.Tests;

public class HangfireJobSchedulerTests
{
    private readonly IBackgroundJobClient _jobClient = Substitute.For<IBackgroundJobClient>();
    private readonly IRecurringJobManager _recurringJobManager = Substitute.For<IRecurringJobManager>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly HangfireJobScheduler _scheduler;

    public HangfireJobSchedulerTests()
    {
        _scheduler = new HangfireJobScheduler(
            _jobClient,
            _recurringJobManager,
            _scopeFactory,
            NullLogger<HangfireJobScheduler>.Instance);
    }

    [Fact]
    public async Task EnqueueAsync_CallsClientCreate_WithEnqueuedState()
    {
        await _scheduler.EnqueueAsync<SimpleTestJob>(CancellationToken.None);

        _jobClient.Received(1).Create(
            Arg.Any<global::Hangfire.Common.Job>(),
            Arg.Is<IState>(s => s is EnqueuedState));
    }

    [Fact]
    public async Task EnqueueAsync_WithArgs_CallsClientCreate_WithEnqueuedState()
    {
        await _scheduler.EnqueueAsync<ArgsTestJob>("some-args", CancellationToken.None);

        _jobClient.Received(1).Create(
            Arg.Any<global::Hangfire.Common.Job>(),
            Arg.Is<IState>(s => s is EnqueuedState));
    }

    [Fact]
    public async Task ScheduleAsync_CallsClientCreate_WithScheduledState()
    {
        var delay = TimeSpan.FromMinutes(5);
        await _scheduler.ScheduleAsync<SimpleTestJob>(delay, CancellationToken.None);

        _jobClient.Received(1).Create(
            Arg.Any<global::Hangfire.Common.Job>(),
            Arg.Is<IState>(s => s is ScheduledState));
    }

    [Fact]
    public async Task ScheduleAsync_WithArgs_CallsClientCreate_WithScheduledState()
    {
        await _scheduler.ScheduleAsync<ArgsTestJob>("args", TimeSpan.FromMinutes(1), CancellationToken.None);

        _jobClient.Received(1).Create(
            Arg.Any<global::Hangfire.Common.Job>(),
            Arg.Is<IState>(s => s is ScheduledState));
    }

    [Fact]
    public async Task AddOrUpdateRecurringAsync_CallsRecurringJobManager()
    {
        await _scheduler.AddOrUpdateRecurringAsync<SimpleTestJob>("my-job", "0 * * * *", CancellationToken.None);

        _recurringJobManager.Received(1).AddOrUpdate(
            "my-job",
            Arg.Any<Job>(),
            "0 * * * *",
            Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public async Task RemoveRecurringAsync_CallsRemoveIfExists()
    {
        await _scheduler.RemoveRecurringAsync("job-to-remove", CancellationToken.None);

        _recurringJobManager.Received(1).RemoveIfExists("job-to-remove");
    }

    [Fact]
    public async Task RemoveRecurringAsync_PassesJobId_Correctly()
    {
        var jobId = "specific-job-id";
        await _scheduler.RemoveRecurringAsync(jobId, CancellationToken.None);

        _recurringJobManager.Received(1).RemoveIfExists(jobId);
    }

    [Fact]
    public async Task TriggerRecurringAsync_CallsManagerTrigger()
    {
        await _scheduler.TriggerRecurringAsync("trigger-me", CancellationToken.None);

        _recurringJobManager.Received(1).Trigger("trigger-me");
    }

    [Fact]
    public async Task ExecuteJob_ResolvesAndExecutesJob_FromDI()
    {
        var job = new SimpleTestJob();
        var scope = Substitute.For<IServiceScope>();
        var scopeProvider = Substitute.For<IServiceProvider>();

        scope.ServiceProvider.Returns(scopeProvider);
        _scopeFactory.CreateScope().Returns(scope);
        scopeProvider.GetService(typeof(SimpleTestJob)).Returns(job);

        await _scheduler.ExecuteJob(typeof(SimpleTestJob).AssemblyQualifiedName!, CancellationToken.None);

        Assert.True(job.Executed);
    }

    [Fact]
    public async Task ExecuteJob_ThrowsInvalidOperationException_ForUnknownType()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _scheduler.ExecuteJob("Unknown.Type.Name, FakeAssembly", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteJobWithArgs_SetsArgsOnJob_BeforeExecuting()
    {
        var job = new ObjectArgsTestJob();
        var scope = Substitute.For<IServiceScope>();
        var scopeProvider = Substitute.For<IServiceProvider>();

        scope.ServiceProvider.Returns(scopeProvider);
        _scopeFactory.CreateScope().Returns(scope);
        scopeProvider.GetService(typeof(ObjectArgsTestJob)).Returns(job);

        await _scheduler.ExecuteJobWithArgs(
            typeof(ObjectArgsTestJob).AssemblyQualifiedName!,
            "test-argument",
            CancellationToken.None);

        Assert.True(job.Executed);
        Assert.Equal("test-argument", job.Args);
    }

    [Fact]
    public async Task ExecuteJobWithArgs_NullArgs_DoesNotSetArgs()
    {
        var job = new ArgsTestJob();
        var scope = Substitute.For<IServiceScope>();
        var scopeProvider = Substitute.For<IServiceProvider>();

        scope.ServiceProvider.Returns(scopeProvider);
        _scopeFactory.CreateScope().Returns(scope);
        scopeProvider.GetService(typeof(ArgsTestJob)).Returns(job);

        await _scheduler.ExecuteJobWithArgs(
            typeof(ArgsTestJob).AssemblyQualifiedName!,
            null,
            CancellationToken.None);

        Assert.True(job.Executed);
        Assert.Null(job.Args);
    }
}
