# PlugHub Package Authoring AGENTS

The default entry files are Chinese: `agent-entries/core/IDENTITY.md`, `agent-entries/core/SOUL.md`, and `agent-entries/core/AGENTS.md`. English entry files are: `agent-entries/core/IDENTITY.en-US.md`, `agent-entries/core/SOUL.en-US.md`, and `agent-entries/core/AGENTS.en-US.md`.

## Mission

Deliver Revit 2020 feature requests as complete PlugHub external packages that can be discovered from repository sources, installed into PlugHub, shown on the PlugHub Ribbon, and executed in Revit 2020.

## Load Order

1. Read `agent-entries/core/IDENTITY.en-US.md` for the one-sentence public identity.
2. Read `agent-entries/core/SOUL.en-US.md` for identity, memory, communication style, and rules.
3. Read root `skills.json` to discover installable skill paths.
4. Load one skill directory by language:
   - Chinese: `plughub-package-authoring`
   - English: `plughub-package-authoring-en`
5. Read the selected `SKILL.md`; load reference files only when the task requires them:
   - Read `references/plughub-package-contract.md` before writing manifests.
   - Read `references/authoring-playbook.md` before creating a new package.
   - Read `references/agent-compatibility.md` in Hermes, OpenClaw, Trae, CodeBuddy, or other non-Codex environments.

## Workflow

1. Confirm the user-specified work branch; when none is specified, ask the user for the branch and do not default to `codex`.
2. Sync latest `main`, then create or update the user-specified branch from latest `main`.
3. Locate the package root, current manifest, and nearby examples.
4. Map the request to a module id, feature id, Ribbon group, icon path, command assembly, and command type.
5. Maintain `packages.json` or `*.packages.json` with complete `schemaVersion`, `indexVersion`, `revitVersions`, `frameworkVersionRange`, module `version`, and `modules`.
6. Implement or repair the `net48` command assembly; command types must implement `Autodesk.Revit.UI.IExternalCommand`.
7. Keep payloads package-relative: `dist/*.dll` and `icons/*.png`.
8. Update build entry points, solution files, or package-repository integration files.
9. Run the C# package validator, repository validator, and compile checks; run a Revit 2020 runtime smoke test when available.
10. After committing and pushing to the user-specified branch, track GitHub workflow results.

## Delivery

The final delivery must report:

- Added or changed manifest files.
- Package-relative payloads for `assembly`, optional `commandAssembly`, and `iconPath`.
- Added or repaired command types.
- Build integration locations.
- C# static validation and compile results.
- Revit 2020 runtime smoke-test result; if unavailable, mark it as pending.
- Push target branch, commit, and GitHub workflow result; if workflow access is unavailable, mark it as unconfirmed.

## Hermes / OpenClaw

- Hermes: prefer native skill discovery; if unavailable, read repository-local instructions in this file's load order.
- OpenClaw: use `agent-entries/core/IDENTITY.en-US.md`, `agent-entries/core/SOUL.en-US.md`, and `agent-entries/core/AGENTS.en-US.md` as the root instruction layer, then load the selected skill directory.
- In both environments, do not assume Codex-specific tool names, local install paths, or hidden global state.
