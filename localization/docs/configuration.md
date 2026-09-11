# Configuration Reference
## `Localization` section
```yaml
localization:
  defaultCulture: en            # Ultimate fallback culture
  fallbackChain:                # Ordered fallback list
    - ru
    - en
  logMissingKeys: true          # Log warning on missing key
  returnKeyOnMissing: true      # Return key itself instead of ""
```
## `Localization:Cache` section (optional Redis)
```yaml
localization:
  cache:
    keyPrefix: localization     # Redis key prefix
    expiry: "01:00:00"          # TTL (TimeSpan format)
```
## LiveConfig config types
| Config type | Content |
|-------------|---------|
| `localization:ru` | `LocalizationSnapshot` for Russian |
| `localization:en` | `LocalizationSnapshot` for English |
| `localization:{culture}` | Any culture by code |
