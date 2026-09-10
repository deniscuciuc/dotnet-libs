# Architecture
```
LiveConfig (Redis + Mongo)
       ↓
CoreLibs.Localization.LiveConfig
  LocalizationConfigApplier
       ↓  (atomic Update)
CoreLibs.Localization.Runtime
  LocalizationCache  (ImmutableDictionary, lock-free reads)
       ↓
  Localizer
  ├── fallback chain (ru → en → key)
  ├── interpolation  ({name} → "Denis")
  └── pluralization  (one/few/many/other)
       ↓
Application (ILocalizer)
```
## Key design decisions
| Decision | Why |
|----------|-----|
| `ImmutableDictionary` + `Interlocked.CompareExchange` | Lock-free reads, atomic writes |
| LiveConfig as single source of truth | No direct GSheet/JSON coupling |
| `LocalizationSnapshot` per culture | One config per language, independent versioning |
| Fallback chain in options | Configurable per deployment |
| `partial class Localizer` | Plural logic separated without splitting the contract |
