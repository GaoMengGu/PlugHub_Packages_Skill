# PlugHub Packages Skill

本仓库提供用于编写 PlugHub 外部插件包的技能资源，包含中文和英文两个版本。技能会指导 agent 创建能被 PlugHub 发现、安装和加载的 Revit 2020 插件包。

## 可安装路径

| 语言 | Skill 名称 | 路径 |
| --- | --- | --- |
| 中文 | `plughub-package-authoring` | `plughub-package-authoring` |
| English | `plughub-package-authoring-en` | `plughub-package-authoring-en` |

Agent 拿到仓库地址后，应优先读取 [skills.json](skills.json)，再用自身支持的 skill 安装、导入或仓库子路径加载机制安装以上路径。不要假设用户机器上的本地安装目录。

## Agent 入口

跨 agent 入口文件集中在 [agent-entries](agent-entries/README.md)：

- `agent-entries/core/`：通用身份、SOUL 和 AGENTS，默认中文并提供英文版。
- `agent-entries/hermes/`：Hermes 专用入口。
- `agent-entries/openclaw/`：OpenClaw 专用入口。
- `agent-entries/trae/`：Trae 专用入口。
- `agent-entries/codebuddy/`：CodeBuddy 专用入口。

## 使用边界

该技能用于创建、审查或修复 PlugHub 插件包，重点覆盖：

- `packages.json` / `*.packages.json` 清单。
- `dist/*.dll` 包内相对载荷。
- `Autodesk.Revit.UI.IExternalCommand` 命令类型。
- `PlugHub.Contracts` 模块描述。
- 图标、构建脚本、包源配置和验证证据。
- Codex、Hermes、OpenClaw、Trae、CodeBuddy 等 agent 的通用使用方式。

## 验证

仓库级静态验证使用 C#：

```powershell
dotnet build src\PlugHub.PackagesSkill.StaticValidation\PlugHub.PackagesSkill.StaticValidation.csproj -c Release
dotnet run --project src\PlugHub.PackagesSkill.StaticValidation\PlugHub.PackagesSkill.StaticValidation.csproj -- .
```

技能随包携带的 PlugHub 包验证器同样使用 C#：

```powershell
dotnet run --project plughub-package-authoring\tools\PlugHub.PackageValidator\PlugHub.PackageValidator.csproj -- <package-root>
```

运行时行为仍需要在 Windows + Revit 2020 中用安全测试模型做 smoke test；静态验证和编译通过不能替代 Revit 运行验证。
