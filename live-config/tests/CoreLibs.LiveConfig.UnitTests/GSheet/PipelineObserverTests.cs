using CoreLibs.LiveConfig.GSheet.Pipeline;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class PipelineObserverTests
{
    [Fact]
    public void DefaultMethods_DoNotThrow()
    {
        IGSheetPipelineObserver observer = new NoOpObserver();

        observer.OnBeforeParse("Test", "Sheet1", "A1:Z");
        observer.OnAfterParse("Test", 10);
        observer.OnBeforeValidation("Test", 10);
        observer.OnAfterValidation("Test", 0);
        observer.OnBeforeMapping("Test", 10);
        observer.OnAfterMapping("Test", 5);
        observer.OnCompleted("Test", TimeSpan.FromMilliseconds(100), true);
    }

    [Fact]
    public void TrackingObserver_RecordsAllCalls()
    {
        var observer = new TrackingObserver();

        observer.OnBeforeParse("P1", "Sheet", "A1:Z");
        observer.OnAfterParse("P1", 10);
        observer.OnBeforeValidation("P1", 10);
        observer.OnAfterValidation("P1", 2);
        observer.OnBeforeMapping("P1", 8);
        observer.OnAfterMapping("P1", 4);
        observer.OnCompleted("P1", TimeSpan.FromMilliseconds(50), true);

        Assert.Equal(7, observer.Calls.Count);
        Assert.Equal("BeforeParse:P1:Sheet:A1:Z", observer.Calls[0]);
        Assert.Equal("AfterParse:P1:10", observer.Calls[1]);
        Assert.Equal("BeforeValidation:P1:10", observer.Calls[2]);
        Assert.Equal("AfterValidation:P1:2", observer.Calls[3]);
        Assert.Equal("BeforeMapping:P1:8", observer.Calls[4]);
        Assert.Equal("AfterMapping:P1:4", observer.Calls[5]);
        Assert.Equal("Completed:P1:True", observer.Calls[6]);
    }

    [Fact]
    public void TrackingObserver_RecordsFailure()
    {
        var observer = new TrackingObserver();

        observer.OnCompleted("P1", TimeSpan.Zero, false);

        Assert.Single(observer.Calls);
        Assert.Equal("Completed:P1:False", observer.Calls[0]);
    }

    // ── Helpers ──

    private sealed class NoOpObserver : IGSheetPipelineObserver;

    private sealed class TrackingObserver : IGSheetPipelineObserver
    {
        public List<string> Calls { get; } = [];

        public void OnBeforeParse(string pipelineName, string sheet, string range)
        {
            Calls.Add($"BeforeParse:{pipelineName}:{sheet}:{range}");
        }

        public void OnAfterParse(string pipelineName, int rowCount)
        {
            Calls.Add($"AfterParse:{pipelineName}:{rowCount}");
        }

        public void OnBeforeValidation(string pipelineName, int rowCount)
        {
            Calls.Add($"BeforeValidation:{pipelineName}:{rowCount}");
        }

        public void OnAfterValidation(string pipelineName, int errorCount)
        {
            Calls.Add($"AfterValidation:{pipelineName}:{errorCount}");
        }

        public void OnBeforeMapping(string pipelineName, int rowCount)
        {
            Calls.Add($"BeforeMapping:{pipelineName}:{rowCount}");
        }

        public void OnAfterMapping(string pipelineName, int domainCount)
        {
            Calls.Add($"AfterMapping:{pipelineName}:{domainCount}");
        }

        public void OnCompleted(string pipelineName, TimeSpan elapsed, bool success)
        {
            Calls.Add($"Completed:{pipelineName}:{success}");
        }
    }
}
