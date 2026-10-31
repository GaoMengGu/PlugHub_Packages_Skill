# CodeBuddy Entry Point

Use root `skills.json` to discover installable paths, then load one skill directory:

- `plughub-package-authoring` for Chinese workflows.
- `plughub-package-authoring-en` for English workflows.

Use the selected `SKILL.md` as the primary instruction file. Load `references/authoring-playbook.md` before scaffolding a new PlugHub package and `references/plughub-package-contract.md` before changing manifests.

Run package validation with the bundled C# project:

```powershell
dotnet run --project <skill-dir>\tools\PlugHub.PackageValidator\PlugHub.PackageValidator.csproj -- <package-root>
```

Local delivery follows the selected SKILL and authoring-playbook: check/install PlugHub, compile and validate locally, then copy the complete package to the verified scanned directory; the user refreshes manually. Protect existing work, other packages, and config. Report installation/build/validation/copy/runtime states separately; clarify headless blockers through platform comments or panels, not terminal prompts.
