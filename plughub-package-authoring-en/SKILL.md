---
name: plughub-package-authoring-en
description: Use when creating, reviewing, or repairing Revit 2020 PlugHub external plugin packages that PlugHub must discover, install, and load. Applies to packages.json or *.packages.json manifests, dist/*.dll payloads, PlugHub.Contracts modules, Autodesk.Revit.UI.IExternalCommand commands, icons, build.ps1, local/GitHub/Gitee package sources, and plugin package authoring across Codex, Hermes, OpenClaw, Trae, and CodeBuddy.
---

# PlugHub Package Authoring

Use this skill to produce a complete PlugHub-loadable package, not just a Revit command class. A valid package has a discoverable manifest, package-relative payload paths, a `net48` command assembly, and verification evidence.

This skill and its package validator enforce a publishable clickable package contract: every module must have a distributable DLL, a version, and at least one clickable feature, and every command feature must declare `commandType`. This is stricter than PlugHub's minimum runtime JSON-read contract so agents do not deliver half-finished packages that can be scanned but not installed, clicked, or diagnosed reliably.

## Requirement Handling

- Treat the user's request as a plugin feature specification, not as a documentation question. Unless the user explicitly asks only for a plan, actually create or modify package files.
- First map the request to one user-clickable Revit feature: module id, feature id, Ribbon group, command type, input method, transaction boundary, failure messages, and validation approach.
- If the feature changes the model, the command must use an explicit Revit API transaction. If it only reads state or asks for selection, still handle no active document, no active view, cancelled selection, and related failure paths.
- Default to producing a complete PlugHub-discoverable package: `packages.json` / `*.packages.json`, `dist/*.dll` build path, icon path, project file, build script registration, and validation evidence.
- If the current environment cannot run Revit 2020, report only static validation and build results, and explicitly mark the Revit runtime smoke test as pending. Do not equate static validation with runtime success.

## Icon Generation

- When authoring or repairing a plugin feature, use text-to-image to generate an icon PNG for every user-clickable feature, save it to `icons/<feature>.png`, and update `feature.iconPath` to that package-relative path.
- Generate the icon with text-to-image from the feature `displayName`, `description`, and core action, then apply the PlugHub icon design language: minimal geometric abstraction, 100% flat, solid dark charcoal `#1A1A1A`, transparent background, solid glyph geometry, micro-rounded corners, negative space, and recognizable at 16x16.
- The icon generation prompt must include the core phrase `Create a flat, solid glyph icon` and ask for a strict 32x32 canvas, the main glyph inside a 24x24 safe area, a 4px margin on every side, a transparent-background PNG, no text, no extra background fill, no frame, no gradients, no shadows, no outline strokes, and no 3D perspective; use the template in `references/authoring-playbook.md`.
- Revit scales the 32x32 source icon automatically on high-DPI displays. Do not generate @2x, @3x, or other multi-scale variants.
- Only skip icon generation when the user explicitly supplies an icon, explicitly asks not to generate one, or the current agent environment has no usable text-to-image capability. If skipped, mark the icon asset as pending in the delivery result and do not fake an `iconPath` to a missing file.

## Workflow

1. Confirm the GitHub work branch and sync the baseline.
   - The work branch must be a user-specified work branch; if the user has not specified one, ask the user for the branch before editing code, committing, or pushing.
   - Do not default to `codex`; multiple agents sharing one branch makes commits, pushes, and workflow results ambiguous.
   - Before work, inspect the worktree, protect existing uncommitted changes, sync latest `main`, then create or update the user-specified branch from latest `main`.
   - Do not work directly on `main`, and do not use destructive git operations unless the user explicitly asks for them.

2. Locate the target package root and nearby examples.
   - Prefer existing `PlugHub_Packages` patterns when present.
   - If the package format is uncertain, read `references/plughub-package-contract.md`.
   - If implementing a new package, read `references/authoring-playbook.md`.
   - If operating in Hermes, OpenClaw, Trae, CodeBuddy, or another non-Codex agent, read `references/agent-compatibility.md`.

3. Make the package discoverable.
   - Put a valid `packages.json` in the package root, or use a colocated `<DllName>.packages.json` for a flat single-DLL drop-in.
   - Keep payload paths relative to the manifest directory.
   - Use `dist/<AssemblyName>.dll` for module assemblies, let features inherit module `assembly` by default, and use `icons/<feature>.png` for feature icons.

4. Implement the Revit command assembly.
   - Target `.NET Framework 4.8` / `net48`.
   - Reference `RevitAPI.dll`, `RevitAPIUI.dll`, and `PlugHub.Contracts`.
   - Add one `IExternalCommand` class per user-triggered feature.
   - Add an `IPlugHubModule` class whose `Describe()` mirrors the manifest metadata, but do not rely on this class alone; PlugHub package discovery reads JSON manifests.

5. Update repository integration files.
   - Add the project to the solution and `build.ps1` if the repo uses them.
   - Add or update the root manifest module and feature records.
   - Ensure `dist/*.dll` is produced by the build and retained for package distribution.
   - Generate an icon PNG for each feature, save it to `icons/<feature>.png`, update `feature.iconPath`, and ensure icons are real package files, not `builtin:` references or absolute paths.

6. Validate before completion.
   - Run `dotnet run --project <skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj -- <package-root>` for manifest and payload checks.
   - Run the repository validator when present, usually `.\tests\Validate-Package.ps1`.
   - Run `.\build.ps1 -UseRevitApiNuGet` for CI-style compile checks, or `.\build.ps1 -RevitApiDir "<Revit 2020 install dir>"` for installed Revit API references.
   - For behavior, smoke-test in Windows + Revit 2020 with a safe test model or family file.

7. Commit, push, and track GitHub workflow results.
   - Before committing, inspect `git status` and include only files related to the current task.
   - Push to the user-specified work branch, not directly to `main`.
   - After pushing, track GitHub workflow results; when `gh` is available, prefer `gh run list`, `gh run watch`, and `gh run view --log-failed`.
   - If GitHub workflow status cannot be accessed, mark workflow verification as unconfirmed in the delivery result; if it fails, read failed logs and distinguish code, environment, permission, or release-configuration issues.

## Hard Rules

- Do not create only a `.addin` file. PlugHub loads package features from `packages.json` / `*.packages.json`.
- Do not edit code, commit, or push before obtaining the user-specified work branch; do not default to `codex`.
- Do not leave `commandAssembly`, `assembly`, or `iconPath` absolute. PlugHub resolves relative paths from the manifest directory and rejects install payloads outside that directory.
- Prefer feature inheritance from module `assembly`; do not repeat `commandAssembly` in repository manifests unless a feature really uses a different DLL.
- Do not actively write `enabled`, `visible`, `defaultState`, `buttonSize`, or other runtime state/layout fields; framework defaults and user configuration own those values.
- Do not omit `commandType`; it must be the full type name of an `Autodesk.Revit.UI.IExternalCommand`.
- Do not put business logic in the module descriptor class. Keep behavior in command classes.
- Do not claim the package is loadable until manifest validation, build validation, and relevant Revit smoke testing are reported.

## Reference Map

- `references/plughub-package-contract.md`: PlugHub discovery, manifest, install, and runtime loading rules.
- `references/authoring-playbook.md`: implementation templates, package layout, and validation commands.
- `references/agent-compatibility.md`: portable operating rules for Codex, Hermes, OpenClaw, Trae, and CodeBuddy.
