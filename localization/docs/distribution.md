# Distribution & LiveConfig Integration
## Flow
```
Source (GSheet / JSON)
    ↓  import via live-config pipeline
LiveConfig Store (MongoDB / Postgres)
    ↓  distribute via Redis pub/sub
LocalizationConfigApplier.ApplyAsync(json, version)
    ↓  atomic cache update
ILocalizationCache (in-memory)
    ↓  O(1) read
ILocalizer.Get(key, culture)
```
## DI Registration
```csharp
// Register appliers for all supported cultures
services.AddCoreLocalizationLiveConfig(["ru", "en", "de"]);
```
Each culture gets its own `LocalizationConfigApplier` bound to config type `"localization:{culture}"`.
## Atomic update
`LocalizationCache` uses `ImmutableDictionary` + `Interlocked.CompareExchange`.  
In-flight reads of the old snapshot complete safely while the new one is installed.
