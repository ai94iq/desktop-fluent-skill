# Architecture

## Projects and dependencies

Desktop depends on Presentation, Core and Data. Presentation depends on Core and Data. Data depends on Core. Core depends on nothing.

- **__Product__.Core** (net10.0): models, interfaces, text helpers, caching helper, layout logic. Never UI or SQL.
- **__Product__.Data** (net10.0): SQLite + Dapper repositories, migrations, backups. Never UI.
- **__Product__.Presentation** (net10.0): view models, settings, `Tr` and resx, portable services and the interfaces the UI implements. No Avalonia or OS types.
- **__Product__.Desktop** (net10.0): Avalonia views, `Themes/Exceptions.axaml`, the UI services (theme, dialogs, change notifications), startup and assets. OS-specific services go in `Platform/<OS>/` behind an interface, picked in `Hosting/DesktopServices.cs`.

## Startup flow

`Program.Main`: settings, culture, logging, single instance, then Avalonia. `App` then builds the host, applies the theme, backs up and migrates off the UI thread, and shows the main window. Pages load their own data when navigated to.

## Data flow

ViewModel, then repository (Task.Run + Dapper), then SQLite. Writes evict cache keys and send `DataChanged`; open pages reload.

## Loading states

Every page ViewModel derives from `PageViewModel` (Idle, Loading, Loaded, Empty, Error); views show each state through the `LoadStateIs` converter.

## Caching layers

Checked in this order: ViewModel state, identity maps, IMemoryCache (`GetOrLoadAsync`), disk cache (`cache`), SQLite page cache.

## Feature map

| Feature | Folder | Notes |
|---|---|---|
