namespace CoreLibs.Jobs.Tests;

public class InMemoryJobSchedulerTests
{
    private readonly InMemoryJobScheduler _scheduler = new();

    // â”€â”€ Enqueue â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Fact]
    public async Task EnqueueAsync_WithoutArgs_RecordsJobType()
    {
        await _scheduler.EnqueueAsync<TestJob>();

        Assert.Single(_scheduler.EnqueuedJobs);
        Assert.True(_scheduler.EnqueuedJobs.TryPeek(out var record));
        Assert.Equal(typeof(TestJob), record.JobType);
        Assert.Null(record.Args);
    }

    [Fact]
    public async Task EnqueueAsync_WithArgs_RecordsJobTypeAndArgs()
    {
        var args = new { Email = "test@example.com" };

        await _scheduler.EnqueueAsync<TestJob>(args);

        Assert.Single(_scheduler.EnqueuedJobs);
        Assert.True(_scheduler.EnqueuedJobs.TryPeek(out var record));
        Assert.Equal(typeof(TestJob), record.JobType);
        Assert.Same(args, record.Args);
    }

    [Fact]
    public async Task EnqueueAsync_MultipleCalls_RecordsAllInOrder()
    {
        await _scheduler.EnqueueAsync<TestJob>();
        await _scheduler.EnqueueAsync<TestJobWithArgs>();
        await _scheduler.EnqueueAsync<TestJob>();

        Assert.Equal(3, _scheduler.EnqueuedJobs.Count);
    }

    // â”€â”€ Schedule â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Fact]
    public async Task ScheduleAsync_WithoutArgs_RecordsJobTypeAndDelay()
    {
        var delay = TimeSpan.FromMinutes(10);

        await _scheduler.ScheduleAsync<TestJob>(delay);

        Assert.Single(_scheduler.ScheduledJobs);
        Assert.True(_scheduler.ScheduledJobs.TryPeek(out var record));
        Assert.Equal(typeof(TestJob), record.JobType);
        Assert.Equal(delay, record.Delay);
        Assert.Null(record.Args);
    }

    [Fact]
    public async Task ScheduleAsync_WithArgs_RecordsAll()
    {
        var args = new { UserId = 42 };
        var delay = TimeSpan.FromHours(1);

        await _scheduler.ScheduleAsync<TestJob>(args, delay);

        Assert.Single(_scheduler.ScheduledJobs);
        Assert.True(_scheduler.ScheduledJobs.TryPeek(out var record));
        Assert.Equal(typeof(TestJob), record.JobType);
        Assert.Same(args, record.Args);
        Assert.Equal(delay, record.Delay);
    }

    // â”€â”€ Recurring â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Fact]
    public async Task AddOrUpdateRecurringAsync_NewJob_AddsRecord()
    {
        await _scheduler.AddOrUpdateRecurringAsync<TestJob>("cleanup", "0 0 * * *");

        Assert.Single(_scheduler.RecurringJobs);
        Assert.True(_scheduler.RecurringJobs.TryGetValue("cleanup", out var record));
        Assert.Equal(typeof(TestJob), record.JobType);
        Assert.Equal("cleanup", record.JobId);
        Assert.Equal("0 0 * * *", record.Cron);
    }

    [Fact]
    public async Task AddOrUpdateRecurringAsync_ExistingJob_UpdatesRecord()
    {
        await _scheduler.AddOrUpdateRecurringAsync<TestJob>("cleanup", "0 0 * * *");
        await _scheduler.AddOrUpdateRecurringAsync<TestJob>("cleanup", "*/5 * * * *");

        Assert.Single(_scheduler.RecurringJobs);
        Assert.Equal("*/5 * * * *", _scheduler.RecurringJobs["cleanup"].Cron);
    }

    [Fact]
    public async Task RemoveRecurringAsync_ExistingJob_RemovesRecord()
    {
        await _scheduler.AddOrUpdateRecurringAsync<TestJob>("cleanup", "0 0 * * *");

        await _scheduler.RemoveRecurringAsync("cleanup");

        Assert.Empty(_scheduler.RecurringJobs);
    }

    [Fact]
    public async Task RemoveRecurringAsync_NonExistentJob_DoesNotThrow()
    {
        await _scheduler.RemoveRecurringAsync("non-existent");

        Assert.Empty(_scheduler.RecurringJobs);
    }

    [Fact]
    public async Task TriggerRecurringAsync_DoesNotThrow()
    {
        await _scheduler.AddOrUpdateRecurringAsync<TestJob>("cleanup", "0 0 * * *");

        await _scheduler.TriggerRecurringAsync("cleanup");
    }

    // â”€â”€ Reset â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    [Fact]
    public async Task Reset_ClearsAllCollections()
    {
        await _scheduler.EnqueueAsync<TestJob>();
        await _scheduler.ScheduleAsync<TestJob>(TimeSpan.FromMinutes(1));
        await _scheduler.AddOrUpdateRecurringAsync<TestJob>("job", "* * * * *");

        _scheduler.Reset();

        Assert.Empty(_scheduler.EnqueuedJobs);
        Assert.Empty(_scheduler.ScheduledJobs);
        Assert.Empty(_scheduler.RecurringJobs);
    }

    // â”€â”€ Test doubles â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private sealed class TestJob : ICoreJob
    {
        public Task ExecuteAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class TestJobWithArgs : ICoreJob<string>
    {
        public string? Args { get; set; }
        public Task ExecuteAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
