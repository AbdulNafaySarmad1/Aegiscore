# AegisCore Plugin Ecosystem

## SDK

`AegisCore.PluginSdk` exposes `IAegisCorePlugin`, `IAegisCorePluginContext`, and `AegisCorePluginManifest`.

## CLI

Install locally with:

`dotnet pack AegisCore.PluginCli/AegisCore.PluginCli.csproj -c Release`

Then install the generated tool package and run:

`aegiscore-plugin init fee-reconciliation developer@example.com`

## Marketplace

Existing endpoints cover catalog, publish, install, uninstall, upgrade, reviews, and installations.

Revenue sharing is recorded through:

`POST /api/marketplace/revenue`

Split: 30% AegisCore, 70% developer.

## Sample

`sample-plugins/fee-reconciliation/aegiscore-plugin.json` is a reference plugin manifest.
