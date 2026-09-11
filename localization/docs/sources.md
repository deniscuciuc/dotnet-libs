# Localization Sources
> ⚠️ Sources feed data **into LiveConfig**, not directly into the Localization runtime.
## Google Sheets (`CoreLibs.Localization.LiveConfig.GSheet`)
Spreadsheet format:
| Key | ru | en | de |
|-----|----|----|----|
| offers.welcome.title | Добро пожаловать! | Welcome! | Willkommen! |
| game.items.count | {count} предметов | {count} items | {count} Artikel |
Use `LocalizationGSheetConverter.Convert(rows)` to produce `LocalizationSnapshot[]`.
## JSON files (`CoreLibs.Localization.LiveConfig.Json`)
File per culture: `locales/ru.json`, `locales/en.json`
```json
{
  "offers.welcome.title": { "value": "Добро пожаловать, {name}!" },
  "game.items.count": {
    "value": "{count} предметов",
    "plurals": { "one": "1 предмет", "few": "{count} предмета", "many": "{count} предметов" }
  }
}
```
