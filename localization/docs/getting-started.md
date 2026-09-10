# Getting Started
## Installation

This repository is not published to NuGet. Add it as a submodule and reference the projects
by relative path:

```bash
git submodule add https://github.com/deniscuciuc/dotnet-libs libs/dotnet-libs
```

```xml
<ItemGroup>
  <ProjectReference Include="libs/dotnet-libs/localization/src/CoreLibs.Localization.Runtime/CoreLibs.Localization.Runtime.csproj" />
  <ProjectReference Include="libs/dotnet-libs/localization/src/CoreLibs.Localization.LiveConfig/CoreLibs.Localization.LiveConfig.csproj" />
</ItemGroup>
```
## Minimal setup
```csharp
// Program.cs
builder.Services.AddCoreLocalization(opts =>
{
    opts.DefaultCulture = "en";
    opts.FallbackChain  = ["ru", "en"];
});
// Connect to LiveConfig (single source of truth)
builder.Services.AddCoreLocalizationLiveConfig(["ru", "en"]);
```
## Usage
```csharp
public class MyService(ILocalizer localizer)
{
    public string GetWelcome(string culture, string name)
        => localizer.Get("offers.welcome.title", culture, new { name });
}
```
## LiveConfig data format
Config type: `"localization:{culture}"` (e.g. `"localization:ru"`)
```json
{
  "culture": "ru",
  "entries": {
    "offers.welcome.title": { "value": "Добро пожаловать, {name}!" },
    "game.items.count": {
      "value": "{count} предметов",
      "plurals": { "one": "1 предмет", "few": "{count} предмета", "many": "{count} предметов" }
    }
  }
}
```
