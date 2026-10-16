# Architecture

## Projects and dependencies

App depends on Core and Data. Data depends on Core. Core depends on nothing.

- **__Product__.Core** (net10.0): models, interfaces, text helpers, caching helper, layout logic. Never WPF or SQL.
- **__Product__.Data** (net10.0): SQLite + Dapper repositories, migrations, backups. Never WPF.
- **__Product__.App** (net10.0-windows): views, view models, app services, startup, resources.

## Startup flow

Settings, culture, logging, single instance, host, backup and migrate, then the main window. Pages load their own data when navigated to.

## Data flow

ViewModel, then repository (Task.Run + Dapper), then SQLite. Writes evict cache keys and send `DataChanged`; open pages reload.

## Loading states

Every page ViewModel derives from `PageViewModel` (Idle, Loading, Loaded, Empty, Error) and shows `LoadStateOverlay`.

## Caching layers

Checked in this order: ViewModel state, identity maps, IMemoryCache (`GetOrLoadAsync`), disk cache (`cache\`), SQLite page cache.

## Feature map

| Feature | Folder | Notes |
|---|---|---|
