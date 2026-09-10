# GSheet Example Layout

This folder groups the Google Sheets examples by API style.

Use [prepare-example-data.gs](examples/CoreLibs.LiveConfig.Examples.Worker/Sources/GSheet/prepare-example-data.gs) to create all spreadsheet tabs and sample rows needed by these examples.

- `Legacy/` shows the original `IGSheetImporter<TRow, TDomain>` model with explicit `ClassMap`, validation, and mapping.
- `Pipeline/` shows the recommended `IGSheetPipeline<TRow, TDomain>` model with schema attributes, validators, aggregators, and dependency ordering.
- `EntityApi/` shows the higher-level `GSheetEntity` / `GSheetEntityConfig` model for the most declarative approach.

Use the same domain objects from `../../Domain/` to compare how much code each style needs for the same config shapes.
