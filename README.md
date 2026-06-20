# PlugHub Packages Skill

本仓库提供用于编写 PlugHub 外部插件包的技能资源，帮助 agent 创建能被 PlugHub 发现、安装和加载的 Revit 2020 插件包。

英文说明：[README.en-US.md](README.en-US.md)

## 可安装路径

| 语言 | Skill 名称 | 路径 |
| --- | --- | --- |
| 中文 | `plughub-package-authoring` | `plughub-package-authoring` |
| English | `plughub-package-authoring-en` | `plughub-package-authoring-en` |

Agent 拿到仓库地址后，应优先读取 [skills.json](skills.json)，再用自身支持的 skill 安装、导入或仓库子路径加载机制安装以上路径。不要假设用户机器上的本地安装目录。

## Agent 入口

跨 agent 入口文件集中在 [agent-entries](agent-entries/README.md)：

- 通用身份、SOUL 和 AGENTS：`agent-entries/core/`
- Hermes：`agent-entries/hermes/`
- OpenClaw：`agent-entries/openclaw/`
- Trae：`agent-entries/trae/`
- CodeBuddy：`agent-entries/codebuddy/`

## 适用范围

该技能用于创建、审查或修复 PlugHub 插件包，重点覆盖：

- `packages.json` / `*.packages.json` 清单。
- `dist/*.dll` 包内相对载荷。
- `Autodesk.Revit.UI.IExternalCommand` 命令类型。
- `PlugHub.Contracts` 模块描述。
- 图标、构建脚本、包源配置和交付证据。
- Codex、Hermes、OpenClaw、Trae、CodeBuddy 等 agent 的通用使用方式。
