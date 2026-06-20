using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;
return RepositoryValidator.Run(args);

internal static class RepositoryValidator
{
    private static readonly string[] SkillDirectories = ["plughub-package-authoring", "plughub-package-authoring-en"];
    private static readonly string[] AgentEntryFiles =
    [
        "agent-entries/README.md",
        "agent-entries/core/SOUL.md",
        "agent-entries/core/SOUL.en-US.md",
        "agent-entries/core/AGENTS.md",
        "agent-entries/core/AGENTS.en-US.md",
        "agent-entries/core/IDENTITY.md",
        "agent-entries/core/IDENTITY.en-US.md",
        "agent-entries/hermes/HERMES.md",
        "agent-entries/openclaw/OPENCLAW.md",
        "agent-entries/trae/TRAE.md",
        "agent-entries/codebuddy/CODEBUDDY.md"
    ];
    private static readonly string[] SensitiveFragments =
    [
        string.Concat("C:", "\\", "Users"),
        string.Concat("C:", "/", "Users"),
        string.Concat("Y", "ilan"),
        string.Concat(".codex", "\\", "skills", "\\", ".system"),
        string.Concat(".codex", "/", "skills", "/", ".system"),
        string.Concat("D:", "\\", "AI", "\\", "code"),
        string.Concat("D:", "/", "AI", "/", "code")
    ];

    private static readonly string LegacyScriptName = string.Concat("validate_plughub", "_package.py");
    private static readonly string LegacyRuntimeName = string.Concat("py", "thon");

    public static int Run(string[] args)
    {
        var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
        var errors = new List<string>();

        if (!Directory.Exists(root))
        {
            errors.Add($"Repository root does not exist: {root}");
            return Finish(errors);
        }

        ValidateRequiredFiles(root, errors);
        ValidateReadmes(root, errors);
        ValidateAgentEntries(root, errors);
        ValidateSkills(root, errors);
        ValidateSkillsJson(root, errors);
        ValidateWorkflow(root, errors);
        ValidateGitAttributes(root, errors);
        ValidateNoLegacyScripts(root, errors);
        ValidateNoTextLeaks(root, errors);

        return Finish(errors);
    }

    private static void ValidateRequiredFiles(string root, List<string> errors)
    {
        foreach (var relativePath in new[]
        {
            "README.md",
            "README.zh-CN.md",
            "README.en-US.md",
            "skills.json",
            ".gitattributes",
            ".github/workflows/sync-gitee.yml"
        }.Concat(AgentEntryFiles))
        {
            RequireFile(root, relativePath, errors);
        }
    }

    private static void ValidateReadmes(string root, List<string> errors)
    {
        var rootReadme = ReadRequiredText(root, "README.md", errors);
        if (rootReadme.Length > 0)
        {
            RequireContains(rootReadme, "README.en-US.md", "README.md must link to the English README.", errors);
            RequireContains(rootReadme, "skills.json", "README.md must mention skills.json discovery.", errors);
            RequireContains(rootReadme, "agent-entries", "README.md must mention the agent entry directory.", errors);
            RejectContains(rootReadme, "dotnet run", "README.md must not document validation commands.", errors);
            RejectContains(rootReadme, "PlugHub.PackageValidator", "README.md must not document validator project usage.", errors);
        }

        var chinese = ReadRequiredText(root, "README.zh-CN.md", errors);
        if (chinese.Length > 0)
        {
            RequireContains(chinese, "dotnet run --project", "Chinese README must document C# validation.", errors);
            RequireContains(chinese, "plughub-package-authoring", "Chinese README must list the Chinese skill path.", errors);
            RequireContains(chinese, "plughub-package-authoring-en", "Chinese README must list the English skill path.", errors);
        }

        var english = ReadRequiredText(root, "README.en-US.md", errors);
        if (english.Length > 0)
        {
            RequireContains(english, "dotnet run --project", "English README must document C# validation.", errors);
            RequireContains(english, "plughub-package-authoring", "English README must list the Chinese skill path.", errors);
            RequireContains(english, "plughub-package-authoring-en", "English README must list the English skill path.", errors);
        }
    }

    private static void ValidateAgentEntries(string root, List<string> errors)
    {
        foreach (var oldRootEntry in new[] { "SOUL.md", "SOUL.en-US.md", "AGENTS.md", "AGENTS.en-US.md", "IDENTITY.md", "IDENTITY.en-US.md", "HERMES.md", "OPENCLAW.md", "TRAE.md", "CODEBUDDY.md" })
        {
            if (File.Exists(Path.Combine(root, oldRootEntry)))
            {
                errors.Add($"Move root agent entry into agent-entries/: {oldRootEntry}");
            }
        }

        var index = ReadRequiredText(root, "agent-entries/README.md", errors);
        if (index.Length > 0)
        {
            RequireContains(index, "core/", "agent-entries/README.md must list the core directory.", errors);
            RequireContains(index, "hermes/", "agent-entries/README.md must list the Hermes directory.", errors);
            RequireContains(index, "openclaw/", "agent-entries/README.md must list the OpenClaw directory.", errors);
            RequireContains(index, "trae/", "agent-entries/README.md must list the Trae directory.", errors);
            RequireContains(index, "codebuddy/", "agent-entries/README.md must list the CodeBuddy directory.", errors);
        }

        var soul = ReadRequiredText(root, "agent-entries/core/SOUL.md", errors);
        if (soul.Length > 0)
        {
            RequireContains(soul, "身份", "agent-entries/core/SOUL.md must include the 身份 keyword.", errors);
            RequireContains(soul, "记忆", "agent-entries/core/SOUL.md must include the 记忆 keyword.", errors);
            RequireContains(soul, "identity", "agent-entries/core/SOUL.md must include the identity keyword.", errors);
            RequireContains(soul, "communication", "agent-entries/core/SOUL.md must include the communication keyword.", errors);
            RequireContains(soul, "style", "agent-entries/core/SOUL.md must include the style keyword.", errors);
            RequireContains(soul, "规则", "agent-entries/core/SOUL.md must include the 规则 keyword.", errors);
            RequireContains(soul, "rules", "agent-entries/core/SOUL.md must include the rules keyword.", errors);
        }

        var soulEnglish = ReadRequiredText(root, "agent-entries/core/SOUL.en-US.md", errors);
        if (soulEnglish.Length > 0)
        {
            RequireContains(soulEnglish, "Identity", "agent-entries/core/SOUL.en-US.md must include the Identity keyword.", errors);
            RequireContains(soulEnglish, "Memory", "agent-entries/core/SOUL.en-US.md must include the Memory keyword.", errors);
            RequireContains(soulEnglish, "Communication", "agent-entries/core/SOUL.en-US.md must include the Communication keyword.", errors);
            RequireContains(soulEnglish, "Style", "agent-entries/core/SOUL.en-US.md must include the Style keyword.", errors);
            RequireContains(soulEnglish, "Rules", "agent-entries/core/SOUL.en-US.md must include the Rules keyword.", errors);
        }

        var identity = ReadRequiredText(root, "agent-entries/core/IDENTITY.md", errors);
        if (identity.Length > 0)
        {
            RequireSingleSentence(identity, "agent-entries/core/IDENTITY.md", errors);
            RequireContains(identity, "PlugHub", "agent-entries/core/IDENTITY.md must identify PlugHub.", errors);
            RequireContains(identity, "Revit 2020", "agent-entries/core/IDENTITY.md must preserve the Revit 2020 boundary.", errors);
        }

        var identityEnglish = ReadRequiredText(root, "agent-entries/core/IDENTITY.en-US.md", errors);
        if (identityEnglish.Length > 0)
        {
            RequireSingleSentence(identityEnglish, "agent-entries/core/IDENTITY.en-US.md", errors);
            RequireContains(identityEnglish, "PlugHub", "agent-entries/core/IDENTITY.en-US.md must identify PlugHub.", errors);
            RequireContains(identityEnglish, "Revit 2020", "agent-entries/core/IDENTITY.en-US.md must preserve the Revit 2020 boundary.", errors);
        }

        var agents = ReadRequiredText(root, "agent-entries/core/AGENTS.md", errors);
        if (agents.Length > 0)
        {
            RequireContains(agents, "使命", "agent-entries/core/AGENTS.md must include the 使命 keyword.", errors);
            RequireContains(agents, "mission", "agent-entries/core/AGENTS.md must include the mission keyword.", errors);
            RequireContains(agents, "交付", "agent-entries/core/AGENTS.md must include the 交付 keyword.", errors);
            RequireContains(agents, "workflow", "agent-entries/core/AGENTS.md must include the workflow keyword.", errors);
            RequireContains(agents, "agent-entries/core/IDENTITY.md", "agent-entries/core/AGENTS.md must load IDENTITY.md.", errors);
            RequireContains(agents, "agent-entries/core/SOUL.md", "agent-entries/core/AGENTS.md must load SOUL.md.", errors);
            RequireContains(agents, "skills.json", "agent-entries/core/AGENTS.md must use skills.json discovery.", errors);
            RequireContains(agents, "Hermes", "agent-entries/core/AGENTS.md must mention Hermes.", errors);
            RequireContains(agents, "OpenClaw", "agent-entries/core/AGENTS.md must mention OpenClaw.", errors);
            RequireContains(agents, "用户指定的工作分支", "agent-entries/core/AGENTS.md must require a user-specified work branch.", errors);
            RequireContains(agents, "不要默认使用 `codex`", "agent-entries/core/AGENTS.md must forbid defaulting to codex.", errors);
            RequireContains(agents, "同步最新 `main`", "agent-entries/core/AGENTS.md must require syncing latest main.", errors);
            RequireContains(agents, "跟踪 GitHub workflow", "agent-entries/core/AGENTS.md must require tracking GitHub workflow results.", errors);
            RequireContains(agents, "推送目标分支", "agent-entries/core/AGENTS.md delivery must report the push target branch.", errors);
        }

        var agentsEnglish = ReadRequiredText(root, "agent-entries/core/AGENTS.en-US.md", errors);
        if (agentsEnglish.Length > 0)
        {
            RequireContains(agentsEnglish, "Mission", "agent-entries/core/AGENTS.en-US.md must include the Mission keyword.", errors);
            RequireContains(agentsEnglish, "Workflow", "agent-entries/core/AGENTS.en-US.md must include the Workflow keyword.", errors);
            RequireContains(agentsEnglish, "Delivery", "agent-entries/core/AGENTS.en-US.md must include the Delivery keyword.", errors);
            RequireContains(agentsEnglish, "agent-entries/core/IDENTITY.en-US.md", "agent-entries/core/AGENTS.en-US.md must load IDENTITY.en-US.md.", errors);
            RequireContains(agentsEnglish, "agent-entries/core/SOUL.en-US.md", "agent-entries/core/AGENTS.en-US.md must load SOUL.en-US.md.", errors);
            RequireContains(agentsEnglish, "skills.json", "agent-entries/core/AGENTS.en-US.md must use skills.json discovery.", errors);
            RequireContains(agentsEnglish, "user-specified work branch", "agent-entries/core/AGENTS.en-US.md must require a user-specified work branch.", errors);
            RequireContains(agentsEnglish, "do not default to `codex`", "agent-entries/core/AGENTS.en-US.md must forbid defaulting to codex.", errors);
            RequireContains(agentsEnglish, "Sync latest `main`", "agent-entries/core/AGENTS.en-US.md must require syncing latest main.", errors);
            RequireContains(agentsEnglish, "track GitHub workflow", "agent-entries/core/AGENTS.en-US.md must require tracking GitHub workflow results.", errors);
            RequireContains(agentsEnglish, "Push target branch", "agent-entries/core/AGENTS.en-US.md delivery must report the push target branch.", errors);
        }

        foreach (var platformEntry in new Dictionary<string, string>
        {
            ["Hermes"] = "agent-entries/hermes/HERMES.md",
            ["OpenClaw"] = "agent-entries/openclaw/OPENCLAW.md",
            ["Trae"] = "agent-entries/trae/TRAE.md",
            ["CodeBuddy"] = "agent-entries/codebuddy/CODEBUDDY.md"
        })
        {
            var text = ReadRequiredText(root, platformEntry.Value, errors);
            if (text.Length > 0)
            {
                RequireContains(text, "skills.json", $"{platformEntry.Value} must use skills.json discovery.", errors);
                RequireContains(text, "plughub-package-authoring", $"{platformEntry.Value} must list the Chinese skill path.", errors);
                RequireContains(text, "plughub-package-authoring-en", $"{platformEntry.Value} must list the English skill path.", errors);
            }
        }
    }

    private static void ValidateSkills(string root, List<string> errors)
    {
        foreach (var skillDirectory in SkillDirectories)
        {
            var skillRoot = Path.Combine(root, skillDirectory);
            if (!Directory.Exists(skillRoot))
            {
                errors.Add($"Missing skill directory: {skillDirectory}");
                continue;
            }

            ValidateSkillFrontmatter(root, skillDirectory, errors);
            RequireFile(root, $"{skillDirectory}/agents/openai.yaml", errors);
            RequireFile(root, $"{skillDirectory}/references/plughub-package-contract.md", errors);
            RequireFile(root, $"{skillDirectory}/references/authoring-playbook.md", errors);
            RequireFile(root, $"{skillDirectory}/references/agent-compatibility.md", errors);
            RequireFile(root, $"{skillDirectory}/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj", errors);
            RequireFile(root, $"{skillDirectory}/tools/PlugHub.PackageValidator/Program.cs", errors);

            var skillText = ReadRequiredText(root, $"{skillDirectory}/SKILL.md", errors);
            RequireContains(skillText, "tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj", $"{skillDirectory}/SKILL.md must reference the C# package validator.", errors);

            var contract = ReadRequiredText(root, $"{skillDirectory}/references/plughub-package-contract.md", errors);
            var playbook = ReadRequiredText(root, $"{skillDirectory}/references/authoring-playbook.md", errors);
            RequireContains(playbook, "tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj", $"{skillDirectory}/authoring-playbook.md must reference the C# package validator.", errors);
            ValidatePackageManifestContract(root, skillDirectory, skillText, contract, playbook, errors);
            ValidateGitHubWorkflow(skillDirectory, skillText, playbook, errors);
            ValidateIconDesignLanguage(skillDirectory, skillText, contract, playbook, errors);

            var compatibility = ReadRequiredText(root, $"{skillDirectory}/references/agent-compatibility.md", errors);
            foreach (var agent in new[] { "Codex", "Hermes", "OpenClaw", "Trae", "CodeBuddy" })
            {
                RequireContains(compatibility, agent, $"{skillDirectory}/agent-compatibility.md must mention {agent}.", errors);
            }
        }
    }

    private static void ValidatePackageManifestContract(string root, string skillDirectory, string skillText, string contract, string playbook, List<string> errors)
    {
        var validator = ReadRequiredText(root, $"{skillDirectory}/tools/PlugHub.PackageValidator/Program.cs", errors);
        var combinedDocs = string.Join("\n", skillText, contract, playbook);

        RequireContains(combinedDocs, "packages.json", $"{skillDirectory} docs must use the current packages.json manifest name.", errors);
        RequireContains(combinedDocs, "*.packages.json", $"{skillDirectory} docs must mention adjacent *.packages.json manifests.", errors);
        RequireContains(combinedDocs, "indexVersion", $"{skillDirectory} docs must document repository indexVersion.", errors);
        RequireAnyContains(combinedDocs, ["module `version`", "module.version", "每个 module 包含 `id`、`version`"], $"{skillDirectory} docs must document module-level version.", errors);
        RequireContains(combinedDocs, skillDirectory.EndsWith("-en", StringComparison.Ordinal) ? "publishable clickable package" : "可发布、可点击插件包", $"{skillDirectory} docs must distinguish skill publish constraints from the PlugHub runtime minimum.", errors);

        RejectContains(combinedDocs, "`package.json`", $"{skillDirectory} docs must not document the legacy package.json manifest name.", errors);
        RejectContains(combinedDocs, "`*.package.json`", $"{skillDirectory} docs must not document the legacy *.package.json manifest pattern.", errors);
        RejectContains(combinedDocs, "\"schemaVersion\": \"1.0\",\n  \"version\": \"V1.0.0\"", $"{skillDirectory} docs must not show a legacy root version sample.", errors);
        RejectContains(combinedDocs, "\"enabled\": true,\n      \"visible\": true", $"{skillDirectory} docs must not tell authors to write enabled/visible package state fields.", errors);
        RejectContains(combinedDocs, "\"visible\": true", $"{skillDirectory} docs must not tell authors to write visible state fields.", errors);
        RejectContains(combinedDocs, "\"defaultState\"", $"{skillDirectory} docs must not tell authors to write defaultState layout fields.", errors);
        RejectContains(combinedDocs, "\"buttonSize\"", $"{skillDirectory} docs must not tell authors to write buttonSize layout fields.", errors);
        RejectContains(combinedDocs, "\"commandAssembly\": \"dist/PlugHub.ExampleTool.dll\"", $"{skillDirectory} docs must not repeat commandAssembly when module assembly is sufficient.", errors);
        RejectContains(playbook, "DefaultState = FeatureState.Visible", $"{skillDirectory}/authoring-playbook.md descriptor example must not include manifest-forbidden layout state fields.", errors);
        RejectContains(playbook, "ButtonSize = \"large\"", $"{skillDirectory}/authoring-playbook.md descriptor example must not include manifest-forbidden layout state fields.", errors);
        RejectContains(playbook, "CommandAssembly = \"dist/PlugHub.ExampleTool.dll\"", $"{skillDirectory}/authoring-playbook.md descriptor example should inherit module assembly instead of repeating command assembly.", errors);
        RejectContains(playbook, "Order = 500", $"{skillDirectory}/authoring-playbook.md descriptor example must not include manifest-forbidden ordering fields.", errors);
        RejectContains(playbook, "Order = 510", $"{skillDirectory}/authoring-playbook.md descriptor example must not include manifest-forbidden ordering fields.", errors);

        RequireContains(validator, "var manifest = \"packages.json\"", $"{skillDirectory} package validator must default to packages.json.", errors);
        RequireContains(validator, "indexVersion", $"{skillDirectory} package validator must validate root indexVersion.", errors);
        RequireContains(validator, "moduleVersion", $"{skillDirectory} package validator must validate module-level version.", errors);
        RequireContains(validator, "DisallowedModuleKeys", $"{skillDirectory} package validator must reject module runtime state fields.", errors);
        RequireContains(validator, "DisallowedFeatureKeys", $"{skillDirectory} package validator must reject feature layout state fields.", errors);
        RejectContains(validator, "var manifest = \"package.json\"", $"{skillDirectory} package validator must not default to legacy package.json.", errors);
        RejectContains(validator, "Root version", $"{skillDirectory} package validator must not require legacy root version.", errors);
    }

    private static void ValidateGitHubWorkflow(string skillDirectory, string skillText, string playbook, List<string> errors)
    {
        var combinedDocs = string.Join("\n", skillText, playbook);
        if (skillDirectory.EndsWith("-en", StringComparison.Ordinal))
        {
            RequireContains(combinedDocs, "user-specified work branch", $"{skillDirectory} docs must require a user-specified work branch.", errors);
            RequireContains(combinedDocs, "ask the user for the branch", $"{skillDirectory} docs must ask for a branch when none is specified.", errors);
            RequireContains(combinedDocs, "Do not default to `codex`", $"{skillDirectory} docs must forbid defaulting to codex.", errors);
            RequireContains(combinedDocs, "sync latest `main`", $"{skillDirectory} docs must require syncing latest main before work.", errors);
            RequireContains(combinedDocs, "track GitHub workflow", $"{skillDirectory} docs must require tracking GitHub workflow results after push.", errors);
            RequireContains(playbook, "Branch State Decision Table", $"{skillDirectory}/authoring-playbook.md must define branch state handling decisions.", errors);
            RequireContains(playbook, "remote branch is ahead or diverged", $"{skillDirectory}/authoring-playbook.md must stop on ahead/diverged branches instead of force-updating.", errors);
            return;
        }

        RequireContains(combinedDocs, "用户指定的工作分支", $"{skillDirectory} docs must require a user-specified work branch.", errors);
        RequireContains(combinedDocs, "先询问用户分支", $"{skillDirectory} docs must ask for a branch when none is specified.", errors);
        RequireContains(combinedDocs, "不要默认使用 `codex`", $"{skillDirectory} docs must forbid defaulting to codex.", errors);
        RequireContains(combinedDocs, "同步最新 `main`", $"{skillDirectory} docs must require syncing latest main before work.", errors);
        RequireContains(combinedDocs, "跟踪 GitHub workflow", $"{skillDirectory} docs must require tracking GitHub workflow results after push.", errors);
        RequireContains(playbook, "分支状态决策表", $"{skillDirectory}/authoring-playbook.md must define branch state handling decisions.", errors);
        RequireContains(playbook, "远端分支领先或已分叉", $"{skillDirectory}/authoring-playbook.md must stop on ahead/diverged branches instead of force-updating.", errors);
    }

    private static void ValidateIconDesignLanguage(string skillDirectory, string skillText, string contract, string playbook, List<string> errors)
    {
        if (skillDirectory.EndsWith("-en", StringComparison.Ordinal))
        {
            RequireContains(skillText, "generate an icon PNG", $"{skillDirectory}/SKILL.md must tell agents to generate icon PNG files.", errors);
            RequireContains(skillText, "text-to-image", $"{skillDirectory}/SKILL.md must require text-to-image icon generation.", errors);
            RequireContains(skillText, "save it to `icons/<feature>.png`", $"{skillDirectory}/SKILL.md must tell agents where to save generated icons.", errors);
            RequireContains(skillText, "update `feature.iconPath`", $"{skillDirectory}/SKILL.md must tell agents to wire generated icons into the manifest.", errors);
            RequireContains(skillText, "icon generation prompt", $"{skillDirectory}/SKILL.md must include an icon generation prompt.", errors);
            RequireContains(skillText, "Create a flat, solid glyph icon", $"{skillDirectory}/SKILL.md must include the concrete PlugHub icon prompt wording.", errors);
            RequireContains(skillText, "Only skip icon generation", $"{skillDirectory}/SKILL.md must limit when icon generation may be skipped.", errors);
            RequireContains(skillText, "no usable text-to-image capability", $"{skillDirectory}/SKILL.md must only allow skipping when text-to-image capability is unavailable.", errors);
            RequireContains(skillText, "32x32 canvas", $"{skillDirectory}/SKILL.md must require a 32x32 icon canvas.", errors);
            RequireContains(skillText, "24x24 safe area", $"{skillDirectory}/SKILL.md must require a 24x24 icon safe area.", errors);
            RequireContains(skillText, "4px margin", $"{skillDirectory}/SKILL.md must require a 4px icon margin.", errors);
            RequireContains(skillText, "transparent-background PNG", $"{skillDirectory}/SKILL.md must require transparent-background PNG icons.", errors);
            RequireContains(skillText, "Do not generate @2x", $"{skillDirectory}/SKILL.md must say not to generate multi-scale icon assets.", errors);

            RequireContains(contract, "generated or supplied PNG file", $"{skillDirectory}/plughub-package-contract.md must describe iconPath as a generated or supplied PNG asset.", errors);
            RequireContains(contract, "icons/<feature>.png", $"{skillDirectory}/plughub-package-contract.md must preserve the icon asset convention.", errors);
            RequireContains(contract, "32x32", $"{skillDirectory}/plughub-package-contract.md must document the 32x32 icon size.", errors);
            RequireContains(contract, "transparent-background PNG", $"{skillDirectory}/plughub-package-contract.md must document transparent-background PNG icons.", errors);

            RequireContains(playbook, "Generate the feature icon", $"{skillDirectory}/authoring-playbook.md must include a feature icon generation step.", errors);
            RequireContains(playbook, "text-to-image", $"{skillDirectory}/authoring-playbook.md must require text-to-image icon generation.", errors);
            RequireContains(playbook, "Use this prompt", $"{skillDirectory}/authoring-playbook.md must provide the generation prompt.", errors);
            RequireContains(playbook, "Save the generated PNG", $"{skillDirectory}/authoring-playbook.md must tell authors to save the generated PNG.", errors);
            RequireContains(playbook, "Set `feature.iconPath`", $"{skillDirectory}/authoring-playbook.md must tell authors to update the manifest icon path.", errors);
            RequireContains(playbook, "32x32 canvas", $"{skillDirectory}/authoring-playbook.md must include the 32x32 icon canvas in the prompt.", errors);
            RequireContains(playbook, "24x24 safe area", $"{skillDirectory}/authoring-playbook.md must include the 24x24 icon safe area in the prompt.", errors);
            RequireContains(playbook, "4px margin", $"{skillDirectory}/authoring-playbook.md must include the 4px icon margin in the prompt.", errors);
            RequireContains(playbook, "transparent background", $"{skillDirectory}/authoring-playbook.md must require transparent icon backgrounds.", errors);
            RequireContains(playbook, "Do not generate @2x", $"{skillDirectory}/authoring-playbook.md must say not to generate multi-scale icon assets.", errors);
            return;
        }

        RequireContains(skillText, "生成图标 PNG", $"{skillDirectory}/SKILL.md must tell agents to generate icon PNG files.", errors);
        RequireContains(skillText, "文生图", $"{skillDirectory}/SKILL.md must require text-to-image icon generation.", errors);
        RequireContains(skillText, "保存到 `icons/<feature>.png`", $"{skillDirectory}/SKILL.md must tell agents where to save generated icons.", errors);
        RequireContains(skillText, "更新 `feature.iconPath`", $"{skillDirectory}/SKILL.md must tell agents to wire generated icons into the manifest.", errors);
        RequireContains(skillText, "图标生成提示词", $"{skillDirectory}/SKILL.md must include an icon generation prompt.", errors);
        RequireContains(skillText, "Create a flat, solid glyph icon", $"{skillDirectory}/SKILL.md must include the concrete PlugHub icon prompt wording.", errors);
        RequireContains(skillText, "只有用户明确提供图标", $"{skillDirectory}/SKILL.md must limit when icon generation may be skipped.", errors);
        RequireContains(skillText, "没有可用文生图能力", $"{skillDirectory}/SKILL.md must only allow skipping when text-to-image capability is unavailable.", errors);
        RequireContains(skillText, "32×32 画布", $"{skillDirectory}/SKILL.md must require a 32x32 icon canvas.", errors);
        RequireContains(skillText, "24×24 安全区", $"{skillDirectory}/SKILL.md must require a 24x24 icon safe area.", errors);
        RequireContains(skillText, "4px 留白", $"{skillDirectory}/SKILL.md must require a 4px icon margin.", errors);
        RequireContains(skillText, "透明底 PNG", $"{skillDirectory}/SKILL.md must require transparent-background PNG icons.", errors);
        RequireContains(skillText, "不用额外做多倍图", $"{skillDirectory}/SKILL.md must say not to generate multi-scale icon assets.", errors);

        RequireContains(contract, "生成或提供的 PNG 文件", $"{skillDirectory}/plughub-package-contract.md must describe iconPath as a generated or supplied PNG asset.", errors);
        RequireContains(contract, "icons/<feature>.png", $"{skillDirectory}/plughub-package-contract.md must preserve the icon asset convention.", errors);
        RequireContains(contract, "32×32", $"{skillDirectory}/plughub-package-contract.md must document the 32x32 icon size.", errors);
        RequireContains(contract, "透明底 PNG", $"{skillDirectory}/plughub-package-contract.md must document transparent-background PNG icons.", errors);

        RequireContains(playbook, "生成 feature 图标", $"{skillDirectory}/authoring-playbook.md must include a feature icon generation step.", errors);
        RequireContains(playbook, "文生图", $"{skillDirectory}/authoring-playbook.md must require text-to-image icon generation.", errors);
        RequireContains(playbook, "使用这个提示词", $"{skillDirectory}/authoring-playbook.md must provide the generation prompt.", errors);
        RequireContains(playbook, "保存生成的 PNG", $"{skillDirectory}/authoring-playbook.md must tell authors to save the generated PNG.", errors);
        RequireContains(playbook, "设置 `feature.iconPath`", $"{skillDirectory}/authoring-playbook.md must tell authors to update the manifest icon path.", errors);
        RequireContains(playbook, "32×32 画布", $"{skillDirectory}/authoring-playbook.md must include the 32x32 icon canvas in the prompt.", errors);
        RequireContains(playbook, "24×24 安全区", $"{skillDirectory}/authoring-playbook.md must include the 24x24 icon safe area in the prompt.", errors);
        RequireContains(playbook, "4px 留白", $"{skillDirectory}/authoring-playbook.md must include the 4px icon margin in the prompt.", errors);
        RequireContains(playbook, "透明背景", $"{skillDirectory}/authoring-playbook.md must require transparent icon backgrounds.", errors);
        RequireContains(playbook, "不用额外做多倍图", $"{skillDirectory}/authoring-playbook.md must say not to generate multi-scale icon assets.", errors);
    }

    private static void ValidateSkillFrontmatter(string root, string skillDirectory, List<string> errors)
    {
        var text = ReadRequiredText(root, $"{skillDirectory}/SKILL.md", errors);
        if (text.Length == 0)
        {
            return;
        }

        var lines = text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 4 || lines[0] != "---")
        {
            errors.Add($"{skillDirectory}/SKILL.md must start with YAML frontmatter.");
            return;
        }

        var end = Array.FindIndex(lines, 1, line => line == "---");
        if (end < 0)
        {
            errors.Add($"{skillDirectory}/SKILL.md frontmatter is not closed.");
            return;
        }

        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 1; index < end; index++)
        {
            var line = lines[index];
            var separator = line.IndexOf(':', StringComparison.Ordinal);
            if (separator <= 0)
            {
                errors.Add($"{skillDirectory}/SKILL.md frontmatter line {index + 1} is not a key/value pair.");
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            keys[key] = value;
        }

        foreach (var key in keys.Keys.Where(key => key is not "name" and not "description"))
        {
            errors.Add($"{skillDirectory}/SKILL.md frontmatter must only use name and description, found: {key}");
        }

        if (!keys.TryGetValue("name", out var name) || name != skillDirectory)
        {
            errors.Add($"{skillDirectory}/SKILL.md frontmatter name must match the directory.");
        }

        if (!keys.TryGetValue("description", out var description) || description.Length == 0)
        {
            errors.Add($"{skillDirectory}/SKILL.md frontmatter description is required.");
        }
    }

    private static void ValidateSkillsJson(string root, List<string> errors)
    {
        var path = Path.Combine(root, "skills.json");
        if (!File.Exists(path))
        {
            return;
        }

        using var document = ReadJson(path, errors);
        if (document is null)
        {
            return;
        }

        var rootElement = document.RootElement;
        RequireJsonString(rootElement, "repository", "GaoMengGu/PlugHub_Packages_skill", "skills.json repository must match this repository.", errors);

        if (!rootElement.TryGetProperty("skills", out var skills) || skills.ValueKind != JsonValueKind.Array)
        {
            errors.Add("skills.json must contain a skills array.");
            return;
        }

        foreach (var skillDirectory in SkillDirectories)
        {
            var found = skills.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("name", out var name) &&
                item.TryGetProperty("path", out var pathValue) &&
                name.GetString() == skillDirectory &&
                pathValue.GetString() == skillDirectory);

            if (!found)
            {
                errors.Add($"skills.json must list {skillDirectory} with matching name and path.");
            }
        }

        ValidateAgentEntriesJson(rootElement, errors);
    }

    private static void ValidateAgentEntriesJson(JsonElement rootElement, List<string> errors)
    {
        if (!rootElement.TryGetProperty("agentEntries", out var agentEntries) || agentEntries.ValueKind != JsonValueKind.Object)
        {
            errors.Add("skills.json must contain an agentEntries object.");
            return;
        }

        var expectedPaths = new Dictionary<string, string>
        {
            ["core.zh-CN.identity"] = "agent-entries/core/IDENTITY.md",
            ["core.zh-CN.soul"] = "agent-entries/core/SOUL.md",
            ["core.zh-CN.agents"] = "agent-entries/core/AGENTS.md",
            ["core.en-US.identity"] = "agent-entries/core/IDENTITY.en-US.md",
            ["core.en-US.soul"] = "agent-entries/core/SOUL.en-US.md",
            ["core.en-US.agents"] = "agent-entries/core/AGENTS.en-US.md",
            ["platforms.Hermes"] = "agent-entries/hermes/HERMES.md",
            ["platforms.OpenClaw"] = "agent-entries/openclaw/OPENCLAW.md",
            ["platforms.Trae"] = "agent-entries/trae/TRAE.md",
            ["platforms.CodeBuddy"] = "agent-entries/codebuddy/CODEBUDDY.md"
        };

        foreach (var expected in expectedPaths)
        {
            if (!TryGetNestedString(agentEntries, expected.Key, out var actual) || actual != expected.Value)
            {
                errors.Add($"skills.json agentEntries.{expected.Key} must be {expected.Value}.");
            }
        }
    }

    private static void ValidateWorkflow(string root, List<string> errors)
    {
        var workflow = ReadRequiredText(root, ".github/workflows/sync-gitee.yml", errors);
        if (workflow.Length == 0)
        {
            return;
        }

        RequireContains(workflow, "push:", "Gitee sync workflow must run on push.", errors);
        RequireContains(workflow, "main", "Gitee sync workflow must target main.", errors);
        RequireContains(workflow, "workflow_dispatch:", "Gitee sync workflow must support manual dispatch.", errors);
        RequireContains(workflow, "gitee.com:GaoMengGu/PlugHub_Packages_skill.git", "Gitee sync workflow must push to the expected Gitee repository.", errors);
        RequireContains(workflow, "GITEE_PRIVATE_KEY", "Gitee sync workflow must use the GITEE_PRIVATE_KEY secret.", errors);
    }

    private static void ValidateGitAttributes(string root, List<string> errors)
    {
        var gitAttributes = ReadRequiredText(root, ".gitattributes", errors);
        if (gitAttributes.Length == 0)
        {
            return;
        }

        RequireContains(gitAttributes, "* text=auto", ".gitattributes must enable text normalization.", errors);
        foreach (var pattern in new[] { "*.md text eol=lf", "*.yaml text eol=lf", "*.yml text eol=lf", "*.json text eol=lf", "*.cs text eol=lf", "*.csproj text eol=lf" })
        {
            RequireContains(gitAttributes, pattern, $".gitattributes must pin LF endings for {pattern.Split(' ')[0]}.", errors);
        }
    }

    private static void ValidateNoLegacyScripts(string root, List<string> errors)
    {
        var legacyFiles = Directory.EnumerateFiles(root, "*.py", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(root, path))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var file in legacyFiles)
        {
            errors.Add($"Remove legacy .py file: {file}");
        }
    }

    private static void ValidateNoTextLeaks(string root, List<string> errors)
    {
        foreach (var file in EnumeratePolicyTextFiles(root))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var text = File.ReadAllText(file, Encoding.UTF8);

            foreach (var fragment in SensitiveFragments)
            {
                if (text.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"{relative} contains a machine-specific path or personal fragment.");
                }
            }

            if (text.Contains(LegacyScriptName, StringComparison.OrdinalIgnoreCase) ||
                text.Contains(LegacyRuntimeName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{relative} still references the legacy script runtime.");
            }
        }
    }

    private static IEnumerable<string> EnumeratePolicyTextFiles(string root)
    {
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".md",
            ".json",
            ".yaml",
            ".yml",
            ".csproj"
        };

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(root, path))
            .Where(path => allowedExtensions.Contains(Path.GetExtension(path)));
    }

    private static JsonDocument? ReadJson(string path, List<string> errors)
    {
        try
        {
            return JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        }
        catch (JsonException ex)
        {
            errors.Add($"{Path.GetFileName(path)} is not valid JSON: {ex.Message}");
            return null;
        }
    }

    private static void RequireJsonString(JsonElement root, string propertyName, string expected, string error, List<string> errors)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String || value.GetString() != expected)
        {
            errors.Add(error);
        }
    }

    private static bool TryGetNestedString(JsonElement root, string dottedPath, out string? value)
    {
        var current = root;
        foreach (var segment in dottedPath.Split('.'))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                value = null;
                return false;
            }
        }

        if (current.ValueKind == JsonValueKind.String)
        {
            value = current.GetString();
            return true;
        }

        value = null;
        return false;
    }

    private static void RequireFile(string root, string relativePath, List<string> errors)
    {
        if (!File.Exists(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))))
        {
            errors.Add($"Missing required file: {relativePath}");
        }
    }

    private static string ReadRequiredText(string root, string relativePath, List<string> errors)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            errors.Add($"Missing required file: {relativePath}");
            return string.Empty;
        }

        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static void RequireContains(string text, string expected, string error, List<string> errors)
    {
        if (!text.Contains(expected, StringComparison.Ordinal))
        {
            errors.Add(error);
        }
    }

    private static void RequireAnyContains(string text, string[] expectedValues, string error, List<string> errors)
    {
        if (!expectedValues.Any(expected => text.Contains(expected, StringComparison.Ordinal)))
        {
            errors.Add(error);
        }
    }

    private static void RejectContains(string text, string forbidden, string error, List<string> errors)
    {
        if (text.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(error);
        }
    }

    private static void RequireSingleSentence(string text, string relativePath, List<string> errors)
    {
        var normalized = text.Trim();
        var nonEmptyLines = normalized.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (nonEmptyLines.Length != 1)
        {
            errors.Add($"{relativePath} must be a single non-empty line.");
        }

        var sentenceTerminators = normalized.Count(ch => ch is '.' or '。' or '!' or '！' or '?' or '？');
        if (sentenceTerminators != 1)
        {
            errors.Add($"{relativePath} must contain exactly one sentence terminator.");
        }
    }

    private static bool IsIgnoredPath(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return relative == ".git" ||
            relative.StartsWith(".git/", StringComparison.Ordinal) ||
            parts.Contains("bin", StringComparer.OrdinalIgnoreCase) ||
            parts.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    private static int Finish(List<string> errors)
    {
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Console.WriteLine($"ERROR: {error}");
            }

            Console.WriteLine($"PlugHub_Packages_skill static validation failed with {errors.Count} error(s).");
            return 1;
        }

        Console.WriteLine("PlugHub_Packages_skill static validation passed.");
        return 0;
    }
}
