using CoreLibs.LiveConfig.GSheet;

namespace CoreLibs.LiveConfig.UnitTests.GSheet;

public class GSheetImportReportTests
{
    [Fact]
    public void NewReport_HasNoImporters()
    {
        var report = new GSheetImportReport();

        Assert.Empty(report.ImporterReports);
        Assert.Equal(0, report.TotalRows);
        Assert.Equal(0, report.TotalValidRows);
    }

    [Fact]
    public void AddImporter_TracksMetrics()
    {
        var report = new GSheetImportReport();
        var importerReport = new GSheetImportReport.ImporterReport
        {
            ImporterName = "TestImporter",
            SheetName = "Items",
            Range = "A:Z",
            TotalRows = 100,
            ValidRows = 95,
            UsedPipelineApi = true,
            Duration = TimeSpan.FromMilliseconds(250),
            Errors = [new GSheetValidationError(0, "Name", "required")],
            Warnings = []
        };

        report.Add(importerReport);

        Assert.Single(report.ImporterReports);
        Assert.Equal(100, report.TotalRows);
        Assert.Equal(95, report.TotalValidRows);
        Assert.True(report.HasErrors);
    }

    [Fact]
    public void MultipleImporters_AggregatesMetrics()
    {
        var report = new GSheetImportReport();
        report.Add(new GSheetImportReport.ImporterReport
        {
            ImporterName = "A",
            SheetName = "SheetA",
            Range = "A:Z",
            TotalRows = 50,
            ValidRows = 50,
            UsedPipelineApi = false,
            Duration = TimeSpan.FromMilliseconds(100),
            Errors = [],
            Warnings = []
        });
        report.Add(new GSheetImportReport.ImporterReport
        {
            ImporterName = "B",
            SheetName = "SheetB",
            Range = "A:Z",
            TotalRows = 30,
            ValidRows = 25,
            UsedPipelineApi = true,
            Duration = TimeSpan.FromMilliseconds(200),
            Errors = [],
            Warnings = []
        });

        Assert.Equal(80, report.TotalRows);
        Assert.Equal(75, report.TotalValidRows);
        Assert.False(report.HasErrors);
    }
}
