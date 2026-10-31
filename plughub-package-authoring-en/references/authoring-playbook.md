# Authoring Playbook

Use this playbook when implementing or repairing a PlugHub package repository.

## Installation and Directory Checks

1. Inspect the worktree and protect existing uncommitted changes; a GitHub branch is not required for local work. Do not automatically stash, switch branches, sync main, upload, or push. Preserve overlapping edits and clarify ownership first.
2. Verify the actual target machine, operating system, Revit 2020, .NET SDK, and PowerShell. On Linux/macOS or an inaccessible Windows target, report available static work separately; do not claim installation or delivery.
3. Inspect effective Revit 2020 addins at machine and current-user scope. Read the Assembly in PlugHub.addin and verify its `PlugHub.Revit2020.dll`, `PlugHub.Contracts.dll`, configuration, and file version. A directory name or source checkout does not prove installation. For multiple addin/install candidates, list paths and evidence and ask which instance is active.
4. Reuse a compatible installed version without upgrading. If missing, obtain the latest version at execution time from verified official GitHub, install using that tag's instructions, and recheck addin, DLLs, version, and directories. Official entries: [GaoMengGu/PlugHub](https://github.com/GaoMengGu/PlugHub) and [latest Release](https://github.com/GaoMengGu/PlugHub/releases/latest). Query metadata with PowerShell, select an official asset from that Release, and record the tag, download URL, and install result:

```powershell
$Release = Invoke-RestMethod 'https://api.github.com/repos/GaoMengGu/PlugHub/releases/latest'
$Release | Select-Object tag_name, html_url
$Release.assets | Select-Object name, browser_download_url
```

Prefer the official `PlugHub-Setup-<tag>.exe` and follow that tag's instructions without inventing silent switches. If only source is available, follow its build/install instructions and record the commit. Stop and report missing releases/instructions, blocked network, unknown source, or incompatibility; do not silently substitute older or prerelease versions. If UAC or administrator approval is required and unavailable in the current headless environment, hand installation to the user/IT and keep it pending until rechecked; do not bypass elevation.

5. Resolve the runtime root from the actual addin Assembly directory and read effective module configuration under its `config`. Resolve `packageDirectories` and enabled `localFolder` sources relative to the runtime root. The official default is `packages` under the installation directory; examples are not proof of the effective destination. Even a user-specified path must be confirmed as scanned by the active instance; never copy into repository caches or `runtime-cache`.
6. Verify destination write permission including replacement and recovery. Do not change ACLs, take ownership, or clean unrelated packages. Stop affected writes and clarify unknown source, version, destination, or permission. In headless mode, use platform comments/question panels without blocking terminal prompts.

Verified baseline (2026-10-31): official API returned `V1.5.7`, a dated observation rather than a fixed install version. The [official user guide](https://github.com/GaoMengGu/PlugHub/blob/cacc665b64be92cb115b58b8a3db1caac1706067/README.md) documents a variable install directory, machine-wide Revit addin registration, and UAC. The [runtime entry](https://github.com/GaoMengGu/PlugHub/blob/cacc665b64be92cb115b58b8a3db1caac1706067/src/PlugHub.Revit2020/ExternalApplicationEntry.cs) uses the assembly directory as its root; the [scanner](https://github.com/GaoMengGu/PlugHub/blob/cacc665b64be92cb115b58b8a3db1caac1706067/src/PlugHub.Framework/Sources/ModuleSourceResolver.cs) resolves configured local paths and discovers manifests recursively. Recheck the installed version and these contracts for each execution.

### Local Delivery Decision Table

| State | Next step and report |
| --- | --- |
| Installed and compatible | Record version and effective directories; reuse installation and develop. |
| Missing installation | Query official latest release, install per its instructions, and recheck before claiming success. |
| Inaccessible target or non-Windows target | Stop installation/delivery, report access blocker, and separate available static work. |
| Unknown source, missing release, or incompatibility | Stop installation and clarify; do not silently substitute sources or versions. |
| Multiple installations or package directories | List addin/configuration evidence and clarify destination before writes. |
| No write permission or unavailable UAC | Preserve files; ask user/IT to resolve without bypassing permissions. |
| Build or package validation failure | Stop deployment, report errors, fix, rebuild, and revalidate. |
| Same-name destination, duplicate module/feature id, or unexplained files | Establish update ownership and back up; clarify unrelated id conflicts or unknown content first. |
| Locked DLL or interrupted copy | Do not forcibly unlock; restore original or withdraw the new directory, report failure, and await user closure of relevant applications. |
| Copy checks passed | Report copied, awaiting user refresh; unperformed Revit smoke testing remains pending. |

## Implementation Steps

This skill and package validator enforce a publishable clickable package contract: modules need a version, distributable assembly, and non-empty feature list, and command features need `commandType`. This is stricter than PlugHub's minimum runtime JSON-read condition so delivered packages are installable and clickable.

1. Choose stable names.
   - Assembly: `PlugHub.<FeatureArea>`.
   - Namespace: match the assembly.
   - Module id: `plughub.modules.<feature-area>`.
   - Feature id: `plughub.modules.<feature-area>.<verb-or-action>`.

2. Create or update the project.
   - Target `net48`.
   - Reference `PlugHub.Contracts`.
   - Reference Revit API by installed DLLs for local development or `Autodesk.Revit.SDK` with `PrivateAssets="all"` and `ExcludeAssets="runtime"` for local NuGet compile references.
   - Keep Revit API DLLs out of the package payload.

Minimal project shape:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>PlugHub.ExampleTool</AssemblyName>
    <RootNamespace>PlugHub.ExampleTool</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup Condition="'$(RevitApiReferenceMode)' == 'Installed'">
    <Reference Include="RevitAPI">
      <HintPath>$(RevitApiDir)\RevitAPI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="RevitAPIUI">
      <HintPath>$(RevitApiDir)\RevitAPIUI.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup Condition="'$(RevitApiReferenceMode)' == 'NuGet'">
    <PackageReference Include="Autodesk.Revit.SDK" Version="$(RevitApiNuGetVersion)" PrivateAssets="all" ExcludeAssets="runtime" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="$(PlugHubRoot)\src\PlugHub.Contracts\PlugHub.Contracts.csproj" />
  </ItemGroup>
</Project>
```

Adjust the `ProjectReference` path to the actual PlugHub checkout.

3. Add a module descriptor class.

```csharp
using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.ExampleTool
{
    public sealed class ExampleToolModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.example-tool",
                Name = "Example Tools",
                Description = "Example PlugHub tool package.",
                State = ModuleState.Enabled,
                Tags = new[] { "example", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.example-tool.run",
                        ModuleId = "plughub.modules.example-tool",
                        Name = "Run Example",
                        Description = "Run the example command.",
                        Category = "example",
                        Group = "Example Tools",
                        Tags = new[] { "example" },
                        CommandType = "PlugHub.ExampleTool.RunExampleCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }
        public void Shutdown() { }
    }
}
```

The descriptor lets the assembly describe itself, but the manifest remains the primary PlugHub package discovery entry. Do not copy compatibility fields such as `Order`, `DefaultState`, `ButtonSize`, or `CommandAssembly` into repository JSON manifests; new packages should let features inherit module `assembly`, while layout and state remain owned by framework defaults and user configuration.

4. Add an external command.

```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.ExampleTool
{
    [Transaction(TransactionMode.Manual)]
    public sealed class RunExampleCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var document = commandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                message = "No active Revit document.";
                return Result.Failed;
            }

            try
            {
                using (var transaction = new Transaction(document, "Run Example"))
                {
                    transaction.Start();
                    // Perform the Revit API change here.
                    transaction.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
```

5. Register the package.
   - Add a module entry and feature entry to `packages.json`.
   - Add the project to the solution file if one exists.
   - Add the project path to `build.ps1` if the repo uses a central build script.
   - Generate the feature icon with text-to-image: extract the core verb or noun from the feature name and description, remove realism, reduce the concept to blocks, matrices, heavy arrows, or overlapping silhouettes, then generate a PNG with PlugHub styling.
   - Use this prompt, replacing `[Icon Concept]` with the feature concept:

```text
Role: Expert UI/UX Icon Designer
Task: Generate a professional app icon matching a specific strict minimalistic design system.

[Icon Concept]
Create a flat, solid glyph icon that abstractly represents: [Icon Concept].
The concept should be highly simplified into basic geometric shapes like blocks, arrows, matrices, or clean overlapping silhouettes. Do NOT draw realistic objects, interface screenshots, text, or fine lines.

[Visual Style Constraints - STRICT]
1. Style: Ultra-minimalist, 100% flat design, solid glyph icon. NO gradients, NO 3D shading, NO fine details, NO outline strokes.
2. Color: Strictly monochrome. Solid dark charcoal (#1A1A1A). Use a transparent background, not a filled white square.
3. Shape Language: Heavy visual weight. If lines or arrows are used, they must be very thick and bold. All sharp corners and edges must have a subtle, micro-rounded finish.
4. Scale & Contrast: Use positive/negative space contrast so the icon is recognizable at 16x16.
5. Revit Asset Size: Output exactly one 32x32 canvas. Keep the main glyph inside a 24x24 safe area with a 4px margin on all sides so Revit does not crop it.
6. Output Format: Transparent-background PNG only. Revit scales 32x32 source icons automatically on high-DPI displays, so Do not generate @2x, @3x, or other scaled variants.

Output: Only the black and transparent solid icon PNG asset, perfectly centered, without any text, frame, outline, or extra background fill.
```

   - Save the generated PNG to `icons/<feature>.png`.
   - Set `feature.iconPath` to `icons/<feature>.png`.
   - Build to `dist/<AssemblyName>.dll`.

6. Build locally, then validate.
   - Run the actual script with installed DLLs or NuGet compile references; both paths compile locally. Verify SDK, PowerShell, `PlugHub.Contracts` reference paths, and supported script parameters.
   - Use `.\build.ps1 -RevitApiDir "<Revit 2020 API DLL directory>"` for installed DLLs, otherwise `.\build.ps1 -UseRevitApiNuGet`.
   - Only after a successful build, run `dotnet run --project <skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj -- <package-root>`; add `--manifest <DllName>.packages.json` for an adjacent manifest.
   - Run `.\tests\Validate-Package.ps1` when present. Stop delivery on any build/validation failure; do not deliver stale DLLs.

## Repository Checklist

- `packages.json` has `schemaVersion`, `indexVersion`, `revitVersions`, `frameworkVersionRange`, and `modules`.
- Every module has `id`, `version`, `assembly`, and `features`.
- Every `module.id` and `feature.id` is unique and stable.
- Every command feature has `commandType` and resolves its command DLL through feature `commandAssembly` or module `assembly`.
- Every relative `assembly`, `commandAssembly`, and `iconPath` exists; new packages usually omit `commandAssembly` and inherit module `assembly`.
- `dist/*.dll` contains the command type and is part of the distributed package.
- `icons/*.png` is a generated or user-supplied real PNG, exists, and is package-relative.
- Do not write `enabled`, `visible`, `defaultState`, `buttonSize`, or other runtime state/layout fields into repository manifests.
- `bin/`, `obj/`, PDBs, and Revit API DLLs are not treated as package release payload.
- The final release or ZIP includes only user-needed payload such as `packages.json`, `dist/*.dll`, and `icons/*.png`.

## Complete Package Copy and Recheck

1. Confirm the actual scanned root and this package's unique destination subdirectory. Check module/feature ids in other scanned packages to avoid duplicate registration. Use a built and validated distribution directory containing the manifest, `dist/*.dll`, `icons/*.png`, and required dependencies; exclude source, bin/obj, Revit API DLLs, and user config.
2. Inventory existing destination files and establish ownership of any same-name package. Back up the complete existing package outside every scanned directory and compare file lists/hashes to establish recoverability; clarify unknown files or ownership. Backups inside packages would be discovered as duplicate manifests by recursive scanning.
3. Copy the complete package from the validated distribution into a unique staging directory outside every scanned directory, preserving relative structure; reject links/reparse points leading outside the package. Validate the staged copy and compare all file hashes, including manifests, icons, and dependencies. Leave the original untouched at this stage.

```powershell
# Resolve all four variables from prior checks; Staging is a unique directory that does not yet exist.
$PackageRoot = '<validated distribution root>'
$Staging = '<unique staging directory outside all scanned roots>'
$Destination = '<verified scanned root>/<this-package>'
$Validator = '<skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj'
Copy-Item -LiteralPath $PackageRoot -Destination $Staging -Recurse -ErrorAction Stop
dotnet run --project $Validator -- $Staging
if ($LASTEXITCODE -ne 0) { throw 'Staged package validation failed' }
```

4. Replace only this package's unique subdirectory after staging is complete and the backup is recoverable. Move any original outside scanned roots first, then move staging into the destination; avoid recursive overlays that retain obsolete DLLs. Stop on locked DLLs without closing applications or forcibly unlocking files. Copy/move can cross volumes and is not guaranteed atomic; record progress and inspect each result.
5. Compare complete source/destination relative file lists and per-file SHA-256, then run the same validator on the destination (with the same `--manifest` for adjacent manifests). On failed checks or interrupted copies, withdraw only this attempt's additions/replacement and restore the original package. If recovery fails, preserve backup and failure evidence, stop, and ask the user to intervene; never report completion. Do not delete the packages root, other packages, or config.
6. Copy only by default; the user refreshes manually. Report copied, awaiting user refresh. Per the installed version's instructions, tell the user to reload configuration in PlugHub Manager and, if necessary, close and reopen Revit and check enabled state/layout. Refresh does not prove a hot replacement of DLLs in the current Revit session; do not automatically refresh or restart.

Delivery must separately report target machine, installed version/source, installation checks or results, build command/exit result, manifest/payload validation, source/destination and hash comparison, backup/recovery state, user refresh steps, and Revit runtime smoke test result or pending status. Static validation or copying does not prove Revit loading or command execution.
