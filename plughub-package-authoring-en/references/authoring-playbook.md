# Authoring Playbook

Use this playbook when implementing or repairing a PlugHub package repository.

## Implementation Steps

0. Prepare the GitHub work branch.
   - Use the user-specified work branch; if the user has not specified one, ask the user for the branch and do not default to `codex`.
   - Inspect `git status` and do not overwrite existing uncommitted changes.
   - Sync latest `main`: run `git fetch origin`, switch to `main`, then run `git pull --ff-only origin main`.
   - Create or update the user-specified branch from latest `main`, then continue implementation on that branch.
   - After commit and push, track GitHub workflow results: prefer `gh run list`, `gh run watch`, and `gh run view --log-failed`; if workflow status cannot be accessed, mark workflow verification as unconfirmed in the delivery result.

### Branch State Decision Table

| State | Handling |
| --- | --- |
| No user-specified branch | Stop and ask the user for the branch; do not edit code, commit, or push. |
| Worktree has uncommitted changes | Stop and describe the changes; stash, commit, or layer edits only with user consent. |
| User-specified branch does not exist | Create it from latest `origin/main`. |
| User-specified branch exists and is clean | Sync latest `main`, then update that branch from latest `main` before continuing. |
| The remote branch is ahead or diverged | Stop and report the difference; do not silently rebase, force-push, or overwrite remote state. |
| Updating the branch creates conflicts | Stop and report conflicting files; do not guess a resolution. |

This skill and package validator enforce a publishable clickable package contract: modules need a version, distributable assembly, and non-empty feature list, and command features need `commandType`. This is stricter than PlugHub's minimum runtime JSON-read condition so delivered packages are installable and clickable.

1. Choose stable names.
   - Assembly: `PlugHub.<FeatureArea>`.
   - Namespace: match the assembly.
   - Module id: `plughub.modules.<feature-area>`.
   - Feature id: `plughub.modules.<feature-area>.<verb-or-action>`.

2. Create or update the project.
   - Target `net48`.
   - Reference `PlugHub.Contracts`.
   - Reference Revit API by installed DLLs for local development or `Autodesk.Revit.SDK` with `PrivateAssets="all"` and `ExcludeAssets="runtime"` for CI compile references.
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

6. Validate.
   - Run `dotnet run --project <skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj -- <package-root>`.
   - Run `.\tests\Validate-Package.ps1` when the package repo has it.
   - Run `.\build.ps1 -UseRevitApiNuGet` when Revit is not installed.
   - Run `.\build.ps1 -RevitApiDir "<Revit 2020 API DLL directory>"` when local Revit API DLLs are available.
   - Revit behavior still requires Windows + Revit 2020 runtime validation.

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
