# CoreLibs Localization — Example Worker

Demonstrates the full localization runtime pipeline:
in-memory cache, JSON file warmup, plural forms, interpolation,
LiveConfig bridge, Telegram adapter, MongoDB persistent store, and Redis distributed cache.

---

## Quick start (no external services)

```bash
dotnet run
```

The app starts with **SourceMode = json** — locale files in `locales/` are loaded
into the in-memory cache at startup. No MongoDB or Redis needed.

---

## How it works

1. `AddCoreLocalization()` registers the lock-free in-memory cache and `ILocalizer`.
2. `LocalizationWarmupService` reads `locales/ru.json` + `locales/en.json` and seeds the cache.
3. `AddCoreLocalizationLiveConfig(["ru","en"])` registers `IConfigApplier` per culture.
   When LiveConfig pushes `"localization:ru"` with a `LocalizationSnapshot`, the cache updates atomically.
4. `ILocalizer.Get(key, culture)` resolves strings with fallback + interpolation.
5. `ILocalizer.GetPlural(key, culture, count)` applies CLDR plural rules (Slavic + English).
6. `TelegramLocalizer.ForUser(userId)` auto-resolves culture from per-user preferences.

---

## Feature flags (`appsettings.yml` / env vars)

| Flag                        | Default    | Description                                  |
|-----------------------------|------------|----------------------------------------------|
| `Example:SourceMode`        | `json`     | `json` \| `store` \| `none`                  |
| `Example:StoreEnabled`      | `false`    | Enable MongoDB store + `/store/*` endpoints  |
| `Example:RedisCacheEnabled` | `false`    | Enable Redis distributed cache               |
| `Example:Cultures`          | `[ru, en]` | Cultures to register LiveConfig appliers for |

### With MongoDB + Redis (full mode)

```bash
docker-compose up -d        # start MongoDB + Redis
Example__StoreEnabled=true Example__RedisCacheEnabled=true dotnet run
```

### Seed MongoDB from JSON files

```bash
# 1. Start with store enabled
Example__StoreEnabled=true dotnet run &

# 2. Import Russian locale into MongoDB
curl -X POST http://localhost:5000/store/import \
  -H "Content-Type: application/json" \
  -d @configs/localization-ru.json

# 3. Switch to store mode and restart
Example__StoreEnabled=true Example__SourceMode=store dotnet run
```

---

## Endpoints

### `/localize` — string resolution

```bash
# Simple lookup
GET /localize?key=app.welcome&culture=ru
GET /localize/app.greeting/en

# Plural forms (CLDR: zero/one/few/many/other)
GET /localize/plural?key=game.items.count&culture=ru&count=1   # → 1 предмет
GET /localize/plural?key=game.items.count&culture=ru&count=3   # → 3 предмета
GET /localize/plural?key=game.items.count&culture=ru&count=11  # → 11 предметов

# Named interpolation — extra query params become template args
GET /localize/interpolate?key=app.greeting&culture=ru&name=Alex
GET /localize/interpolate?key=offers.bonus.received&culture=en&amount=100&currency=USD

# Dump all keys for a culture
GET /localize/all/ru
```

### `/cache` — cache management

```bash
# Current state: loaded cultures + entry counts
GET /cache/status

# Re-read locale JSON files without restarting
POST /cache/reload

# Simulate a LiveConfig push (body = LocalizationSnapshot JSON)
POST /cache/apply
Content-Type: application/json
< configs/localization-ru.json
```

### `/telegram` — Telegram adapter

```bash
# List all users with configured cultures
GET /telegram/users

# Get a user's current culture
GET /telegram/users/42

# Set language preference for user 42
PUT /telegram/users/42/culture
{"culture": "ru"}

# Reset to default culture
DELETE /telegram/users/42/culture

# Resolve a key with auto-detected user culture
GET /telegram/users/42/localize?key=app.greeting
GET /telegram/users/42/localize/telegram.bot.welcome
```

### `/store` — MongoDB store (requires `Example:StoreEnabled = true`)

```bash
# Read all entries for a culture from MongoDB
GET /store/entries/ru

# Upsert a single entry
POST /store/entries
{"key":"app.new_key","culture":"ru","value":{"value":"Новый ключ"}}

# Delete an entry
DELETE /store/entries/ru/app.new_key

# Bulk-import a full LocalizationSnapshot (e.g. produced by LocalizationJsonLoader)
POST /store/import
< configs/localization-ru.json
```

---

## Project structure

```
locales/
  ru.json                   ← Russian translations (plural forms + interpolation)
  en.json                   ← English translations
configs/
  localization-ru.json      ← Example LiveConfig push payload for "localization:ru"
  localization-en.json      ← Example LiveConfig push payload for "localization:en"
Services/
  LocalizationWarmupService.cs       ← IHostedService: seeds cache from JSON files
  LocalizationStoreWarmupService.cs  ← IHostedService: seeds cache from MongoDB
Entities/
  InMemoryTelegramUserContext.cs     ← ITelegramUserContext backed by ConcurrentDictionary
Importers/
  SnapshotStoreImporter.cs           ← Bulk-imports a LocalizationSnapshot into ILocalizationStore
docker-compose.yml          ← MongoDB + Redis for local development
```

    "game.items.count": {
      "value": "{count} предметов",
      "plurals": { "one": "1 предмет", "few": "{count} предмета", "many": "{count} предметов" }
    }

}
}

```
