# Nocturne Plugin Ecosystem

## SDK

`Nocturne.PluginSdk` exposes `INocturnePlugin`, `INocturnePluginContext`, and `NocturnePluginManifest`.

## CLI

Install locally with:

`dotnet pack Nocturne.PluginCli/Nocturne.PluginCli.csproj -c Release`

Then install the generated tool package and run:

`nocturne-plugin init fee-reconciliation developer@example.com`

## Marketplace

Existing endpoints cover catalog, publish, install, uninstall, upgrade, reviews, and installations.

Revenue sharing is recorded through:

`POST /api/marketplace/revenue`

Split: 30% Nocturne, 70% developer.

## Sample

`sample-plugins/fee-reconciliation/nocturne-plugin.json` is a reference plugin manifest.
