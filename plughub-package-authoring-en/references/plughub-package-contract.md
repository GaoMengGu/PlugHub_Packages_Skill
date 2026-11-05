# PlugHub Package Contract

This reference describes the PlugHub package contract. Synchronous startup and legacy state removal use the 2026-11-04 [framework commit dbb5ca2](https://github.com/GaoMengGu/PlugHub/commit/dbb5ca27c1aa1d9f86b6a8c5ae889ad9c48b6c3d) as their baseline; this commit does not establish release availability. Verify the target installation and matching Contracts before delivery; neither an existing installation nor the latest Release is proof of this contract.

The skill repository package validator enforces a deliverable package contract: module `version`, distributable `assembly`, a `features` array, and `commandType` for every command feature. Ordinary modules need a non-empty feature array; explicit startup modules may have an empty array. It checks JSON and payload files without reflecting over DLLs, so it cannot establish startup interface, constructor, or Revit behavior.

## Discovery

- PlugHub package manifests are named `packages.json` or `*.packages.json`.
- A source root is scanned by checking the root `packages.json` first, then recursively finding other `packages.json` and `*.packages.json` files.
- Paths under `.git` are ignored.
- A manifest is readable only when the JSON object contains both `schemaVersion` and `modules`.
- A repository package uses `module.id` as both the package id and module id in repository browsing.

## Runtime Compatibility

- PlugHub Revit runtime currently targets Revit `2020`.
- If a module declares `revitVersions`, the list must include `"2020"` or PlugHub skips that module with a compatibility diagnostic.
- `frameworkVersionRange` is preserved as metadata and is not strictly evaluated by the current runtime. Package repositories keep `">=1.3.0"`, but it cannot prevent mixing incompatible Contracts. Verify actual framework/Contracts builds or DLL hashes and coordinate matching versions before delivery.
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
- `module.autoStart`: optional JSON boolean; omitted or false does not load startup entry points. Strings, numbers, and null are invalid.
- `module.startupType`: optional JSON string; when startup is true, it must be the full CLR type name in the same record's `assembly`. See the interface and lifecycle rules below.
- `module.features`: an array; non-empty for ordinary command packages. A valid startup-only package may use `[]` without buttons or icons.
- `module.displayName`: module grouping label. Existing packages often share display names across related modules, such as view tools.
- `module.category` / `module.tags`: repository filtering, default layout, and display metadata; features inherit these values when omitted.
- `feature.group`: fallback Ribbon panel name when the workspace has no explicit group layout.
- `feature.commandAssembly`: optional package-relative DLL path. Only set it when a feature uses a different DLL; otherwise omit it and inherit `module.assembly`.
- `feature.commandType`: full CLR type name of an `Autodesk.Revit.UI.IExternalCommand`.
- `feature.iconPath`: package-relative icon path. Prefer a generated or supplied PNG file named `icons/<feature>.png`; the file must exist and ship as package payload. PlugHub/Revit icons use a 32x32 transparent-background PNG, with the main glyph preferably inside a 24x24 safe area and a 4px margin; Revit handles high-DPI scaling, so no extra multi-scale assets are needed.

For external package repositories, avoid root `packageDirectories`, `moduleSources`, `repositories`, and `conflictPolicy` unless editing framework configuration. Module `enabled` / `visible` and feature `defaultState` have been removed and cannot control activation or hiding. Layout fields such as `buttonSize` / `order` remain framework configuration; the package validator still rejects them.

## Synchronous Startup

Only for an explicit event/resource registration requirement, declare this record inside `modules[]`, retaining the root envelope above:

```json
{
  "id": "plughub.modules.example-events",
  "version": "V1.0.0",
  "assembly": "dist/PlugHub.ExampleEvents.dll",
  "autoStart": true,
  "startupType": "PlugHub.ExampleEvents.Startup",
  "features": []
}
```

- `startupType` must be a public, non-abstract `Autodesk.Revit.UI.IExternalApplication` in that DLL, with a public parameterless constructor. Ordinary packages omit both fields; false never instantiates the type. Legacy `type`, `IPlugHubModule.Initialize/Shutdown`, and feature `commandType` do not replace the startup entry point.
- The framework calls it synchronously on the Revit main thread, before framework `OnStartup` returns and before Ribbon construction. Do not defer to `ApplicationInitialized`, assume an active document, call Revit API via `Task.Run`, or register a separate package `.addin`. Keep startup short; a blocked main thread cannot be safely killed.
- Startup is independent of Ribbon presentation: `features: []` and layout exclusions do not prevent it. Candidates come from installed/effective local sources with compatibility and duplicate ID checks; remote repository caches do not start. Framework Order/ID controls ordering; `dependsOn` currently provides no startup scheduling.
- Each module gets at most one attempt per session. Construction/startup exceptions and Failed/Cancelled results produce diagnostics and allow other modules to continue. Once `OnStartup` has been entered, a failed instance gets one best-effort `OnShutdown`; failures before construction have no instance to clean up. Undo partial registrations and make cleanup safe after partial initialization.
- Successful instances live until exit and receive `OnShutdown` in reverse startup order; one cleanup failure does not block the others. A later framework Ribbon construction failure also triggers best-effort cleanup.
- Refresh, button clicks, and packages added during a session do not retry initialization. Startup package installation, update, removal, and declaration changes take effect next Revit session; current successful instances remain until shutdown. Require a user restart for acceptance; do not rely on net48 hot unloading.

## Legacy State Migration and Rollback

- Remove source references to `ModuleDescriptor.State` / `ModuleState` and `FeatureDescriptor.DefaultState` / `FeatureState`. Keep the remaining descriptor interface and existing empty Initialize/Shutdown methods where needed, without promising framework calls. Use diagnostics for failures rather than replacement activation states.
- New manifests, generators, and templates omit `modules[].enabled` / `visible` and `features[].defaultState`. In presets, remove only `moduleOverrides[].enabled` / `visible`, preserving ordering, views, and combinations. The framework ignores old values and restores normal eligibility for qualified installed features; normal targeted saves remove obsolete keys without rewriting unrelated files in bulk.
- Preserve source `moduleSources[].enabled`, repository `enabled` / public/private, WPF `Visibility`, installation states and diagnostics, plus compatibility, stable IDs, registration, and workspace checks. Never delete globally by State/Enabled/Visible names.
- Preserve feature IDs, `commandKey`, `commandType`, and business behavior. Inventory source, templates, tests, bundled Contracts, and actual reference paths. Remove obsolete state references and rebuild every affected package against matching new Contracts; old DLLs may fail to load after public members are removed.
- Rollback requires closing Revit and restoring the complete framework, matching Contracts, package DLLs/dependencies, and manifest/configuration backups together. JSON-only rollback cannot repair a binary mismatch.

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

User drop-in layout (relative to the verified scanned root only):

```text
packages/dropins/PlugHub_Packages/
  packages.json
  dist/
    PlugHub.ExampleTool.dll
  icons/
    example-tool.png
```

## Local Delivery Boundary

`packages/dropins/PlugHub_Packages/` illustrates a relative layout, not a fixed absolute installation path. Resolve the real scanned root from the effective addin Assembly location and module configuration; the default is packages under the installation directory. Source checkouts, repository caches, and runtime-cache are not evidence of an installed directory. See the [installation and copy workflow](authoring-playbook.md).

For manual local delivery, copy the validated complete package with its manifest-relative dist, icons, and required dependencies. The repository-install payload list above describes the framework's own install service, not a substitute for complete manual copying. Keep backups/staging outside all scanned roots to avoid duplicate module/feature ids, preserve other packages/config, compare hashes, and revalidate the destination. The user refreshes manually; this does not prove Revit command execution.
