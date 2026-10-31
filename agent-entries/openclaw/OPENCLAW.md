# OpenClaw Entry Point

Default Chinese entry files:

1. `agent-entries/core/IDENTITY.md`
2. `agent-entries/core/SOUL.md`
3. `agent-entries/core/AGENTS.md`

English entry files:

1. `agent-entries/core/IDENTITY.en-US.md`
2. `agent-entries/core/SOUL.en-US.md`
3. `agent-entries/core/AGENTS.en-US.md`

Use root `skills.json` to discover installable paths, then load one skill directory:

- `plughub-package-authoring` for Chinese workflows.
- `plughub-package-authoring-en` for English workflows.

Treat the selected directory as repository-local task instructions. Read `SKILL.md`, then load the referenced files only when the task needs them.

Run package validation with the bundled C# project:

```powershell
dotnet run --project <skill-dir>\tools\PlugHub.PackageValidator\PlugHub.PackageValidator.csproj -- <package-root>
```

Local delivery follows the selected SKILL and authoring-playbook: check/install PlugHub, compile and validate locally, then copy the complete package to the verified scanned directory; the user refreshes manually. Protect existing work, other packages, and config. Report installation/build/validation/copy/runtime states separately; clarify headless blockers through platform comments or panels, not terminal prompts.
