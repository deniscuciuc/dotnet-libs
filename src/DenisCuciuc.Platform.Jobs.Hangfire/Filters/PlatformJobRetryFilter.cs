using System.Reflection;
using Hangfire.Common;
using Hangfire.States;

namespace DenisCuciuc.Platform.Jobs.Hangfire.Filters;

/// <summary>
/// Applies retry count from <see cref="PlatformJobAttribute"/> or <see cref="PlatformJobsOptions"/> defaults.
/// </summary>
internal sealed class PlatformJobRetryFilter(PlatformJobsOptions options) : JobFilterAttribute, IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState)
            return;

        var retryCount = options.MaxRetries;

        var jobType = GetJobType(context.BackgroundJob.Job);
        var attr = jobType?.GetCustomAttribute<PlatformJobAttribute>();
        if (attr is { Retries: >= 0 })
            retryCount = attr.Retries;

        var retryAttempt = context.GetJobParameter<int>("RetryCount");
        if (retryAttempt >= retryCount) return;

        context.SetJobParameter("RetryCount", retryAttempt + 1);

        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        context.CandidateState = new ScheduledState(delay)
        {
            Reason = $"Retry attempt {retryAttempt + 1} of {retryCount}"
        };
    }

    private static Type? GetJobType(Job? job)
    {
        if (job?.Type is null) return null;

        // The job type for ExecuteJob<TJob> is HangfireJobScheduler
        // We need to extract the generic type argument
        if (!job.Method.IsGenericMethod) return job.Type;

        var genericArgs = job.Method.GetGenericArguments();
        return genericArgs.Length > 0 ? genericArgs[0] : job.Type;
    }
}
