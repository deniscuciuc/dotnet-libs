# Localization Stores (optional)
> ⚠️ Stores are **optional**. The primary source is LiveConfig.  
> Use stores only for cold-start pre-loading or auditing.
## MongoDB (`CoreLibs.Localization.Store.Mongo`)
```csharp
services.AddCoreLocalizationMongo();
```
Collection: `Localizations`  
Document key: `"{culture}:{key}"` (e.g. `"ru:offers.welcome.title"`)
## PostgreSQL (`CoreLibs.Localization.Store.Postgres`)
```csharp
services.AddCoreLocalizationPostgres(connectionString);
```
Table: `localization_entries`  
Primary key: `(culture, key)`  
Value stored as `jsonb`
