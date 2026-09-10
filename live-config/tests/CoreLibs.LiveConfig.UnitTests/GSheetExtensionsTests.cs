using System.Text;
using CoreLibs.LiveConfig.GSheet;
using CoreLibs.LiveConfig.Startup;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CoreLibs.LiveConfig.UnitTests;

public class GSheetExtensionsTests
{
    [Fact]
    public void AddLiveConfigGSheet_RegistersConfigSourceAndSharedContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IConfigStore>());
        services.AddLiveConfig(_ => { });
        services.AddLiveConfigGSheet(options =>
        {
            options.ApplicationName = "test-app";
            options.SpreadsheetId = "spreadsheet-id";
            options.Credentials = new GSheetCredentials
            {
                Method = GSheetCredentialsMethod.Base64JsonFromConfiguration,
                Value = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))
            };
        });

        services.AddSingleton(new SheetsService(new BaseClientService.Initializer
        {
            ApplicationName = "test-app",
            HttpClientInitializer = GoogleCredential.FromAccessToken("test-token")
        }));

        using var provider = services.BuildServiceProvider();

        var source = provider.GetRequiredService<IConfigSource>();
        var registry = provider.GetRequiredService<ConfigSourceRegistry>();
        var context1 = provider.GetRequiredService<CoreLibs.LiveConfig.GSheet.GSheetImportContext>();
        var context2 = provider.GetRequiredService<CoreLibs.LiveConfig.GSheet.GSheetImportContext>();

        Assert.IsType<CoreLibs.LiveConfig.GSheet.GSheetConfigSource>(source);
        Assert.Same(source, registry.Resolve("gsheet"));
        Assert.Same(context1, context2);
    }
}
