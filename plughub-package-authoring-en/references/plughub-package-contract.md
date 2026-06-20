# PlugHub Package Contract

This reference captures the package shape that PlugHub currently discovers, installs, and loads.

This contract describes fields shared by the PlugHub runtime and package repositories. The skill repository package validator additionally enforces a publishable clickable package contract, such as module `version`, non-empty `features`, distributable `assembly`, and `commandType` for command features. Those checks are publication-quality gates and can be stricter than the minimum runtime JSON-read conditions.

## Discovery

- PlugHub package manifests are named `packages.json` or `*.packages.json`.
- A source root is scanned by checking the root `packages.json` first, then recursively finding other `packages.json` and `*.packages.json` files.
- Paths under `.git` are ignored.
- A manifest is readable only when the JSON object contains both `schemaVersion` and `modules`.
- A repository package uses `module.id` as both the package id and module id in repository browsing.

## Runtime Compatibility

- PlugHub Revit runtime currently targets Revit `2020`.
- If a module declares `revitVersions`, the list must include `"2020"` or PlugHub skips that module with a compatibility diagnostic.
- `frameworkVersionRange` is preserved as metadata and is not strictly evaluated by the current runtime, but package repositories should keep `">=1.3.0"` unless the PlugHub framework contract changes.
- Package command assemblies should target `net48`.

## Manifest Shape

Use this as the default manifest skeleton:

```json
{
  "schemaVersion": "1.1",
  "indexVersion": "V1.0.0",
  "revitVersions": ["2020"],
  "frameworkVersionRange": ">=1.3.0",
  "modules": [
    {
      "id": "plughub.modules.example-tool",
      "version": "V1.0.0",
      "author": "GAOMENGGU",
      "assembly": "dist/PlugHub.ExampleTool.dll",
      "displayName": "Example Tools",
      "description": "Example PlugHub tool package.",
      "category": "example",
      "tags": ["example", "revit-api"],
      "features": [
        {
          "id": "plughub.modules.example-tool.run",
          "displayName": "Run Example",
          "description": "Run the example command.",
          "iconPath": "icons/example-tool.png",
          "commandType": "PlugHub.ExampleTool.RunExampleCommand"
        }
      ]
    }
  ]
}
```

## Field Rules

- `indexVersion`: repository index snapshot version; it does not drive per-plugin update decisions.
- `module.id` and `feature.id`: make globally unique; use stable lowercase dotted/kebab ids such as `plughub.modules.level-visibility.toggle`.
- `module.version`: module version used by the repository page and update checks.
- `module.author`: module author metadata.
- `module.assembly`: package-relative DLL path. It is also the fallback when a feature omits `commandAssembly`; new packages usually let features inherit it.
- `module.displayName`: module grouping label. Existing packages often share display names across related modules, such as view tools.
- `module.category` / `module.tags`: repository filtering, default layout, and display metadata; features inherit these values when omitted.
- `feature.group`: fallback Ribbon panel name when the workspace has no explicit group layout.
- `feature.commandAssembly`: optional package-relative DLL path. Only set it when a feature uses a different DLL; otherwise omit it and inherit `module.assembly`.
- `feature.commandType`: full CLR type name of an `Autodesk.Revit.UI.IExternalCommand`.
- `feature.iconPath`: package-relative icon path. Prefer a generated or supplied PNG file named `icons/<feature>.png`; the file must exist and ship as package payload. PlugHub/Revit icons use a 32x32 transparent-background PNG, with the main glyph preferably inside a 24x24 safe area and a 4px margin; Revit handles high-DPI scaling, so no extra multi-scale assets are needed.

For external package repositories, avoid root `packageDirectories`, `moduleSources`, `repositories`, and `conflictPolicy` unless you are editing PlugHub framework configuration. Also do not actively write `enabled`, `visible`, `defaultState`, `buttonSize`, or other runtime state/layout fields; framework defaults and user configuration own those values. The package repository validator rejects these fields for package manifests.

## Installation Payload

When installing a repository package, PlugHub writes a single-module `packages.json` and copies only these package-relative payloads:

- `module.assembly`
- each `feature.commandAssembly`, if present and different from `module.assembly`
- each `feature.iconPath`

Absolute payload paths are ignored for copying and are a bad package practice. Missing relative payload files fail installation.

## Runtime Loading

- PlugHub resolves `feature.commandAssembly` relative to the module's resolved base directory, normally the manifest directory.
- The runtime shadow-copies package files into `runtime-cache` before loading command assemblies.
- `commandType` is loaded from the cached assembly and must be assignable to `Autodesk.Revit.UI.IExternalCommand`.
- The command instance is created with a parameterless constructor.

## Source Configuration Examples

Local development source:

```json
{
  "id": "local-plughub-packages",
  "type": "localFolder",
  "path": "D:/path/to/PlugHub_Packages",
  "manifestPath": "packages.json",
  "enabled": true,
  "autoUpdate": false
}
```

GitHub source:

```json
{
  "id": "plughub-packages",
  "type": "github",
  "repository": "GaoMengGu/PlugHub_Packages",
  "ref": "main",
  "path": "packages/github/GaoMengGu_PlugHub_Packages",
  "manifestPath": "packages.json",
  "enabled": true,
  "autoUpdate": true
}
```

User drop-in layout:

```text
packages/dropins/PlugHub_Packages/
  packages.json
  dist/
    PlugHub.ExampleTool.dll
  icons/
    example-tool.png
```
