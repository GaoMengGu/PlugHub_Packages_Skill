# Agent 兼容性

此技能刻意采用 Markdown 优先结构，使 Codex、Hermes、OpenClaw、Trae、CodeBuddy 和类似编码 agent 能使用同一份技能目录。

## 通用规则

- 先加载 `SKILL.md`，再按任务只加载需要的参考文件。
- 使用当前 agent 原生的文件读取、补丁、shell 和验证工具；不要假设存在 Codex 专用工具名。
- 命令保持明确、可复制。PlugHub/Revit 包工作面向 Windows，优先给 PowerShell 示例。
- 保留根目录 `SKILL.md` 文件名；YAML frontmatter 只保留 `name` 和 `description`。
- 参考文件保持在技能根目录下一层的 `references/` 中。
- 辅助脚本放在 `scripts/`，通过路径调用脚本，不要重复改写脚本逻辑。

## 平台说明

- Codex：使用当前 workspace，做小范围补丁，并报告验证命令与结果。
- Hermes：如果可用，使用 `skills_list` / `skill_view`；否则直接读取技能目录。写清单前先加载 `references/plughub-package-contract.md`。
- OpenClaw：把此技能视为仓库本地指令。从包根目录运行 shell 验证，避免隐藏的全局假设。
- Trae 和 CodeBuddy：读取 `SKILL.md` 与所需参考文件，然后按 IDE agent 的常规编辑和终端流程执行。

## 跨 Agent 输出契约

完成插件包任务时，报告：

- 包根目录和清单路径
- 创建或修改的项目文件
- 修改后的 manifest module id 和 feature id
- 构建命令和结果
- 清单验证器命令和结果
- 是否已执行 Revit 2020 运行时 smoke test，或仍待执行

不要仅凭静态验证就声称 PlugHub 运行时加载成功。

## 本地交付与无人值守

先读取 authoring-playbook.md，核实目标机器/系统/Revit、PlugHub 安装与版本，缺失才按官方最新版说明安装复查；本地构建与验证通过后才复制完整包到实际扫描目录，刷新由用户操作。遇到权限、多目录、来源或文件冲突时，通过当前平台评论或问题面板澄清；headless 时不要使用交互终端提示。

跨 Agent 报告还必须逐项列出安装来源/版本与结果、构建结果、包验证结果、实际目的目录、复制后文件/哈希核对和备份恢复状态、待用户刷新步骤及 Revit runtime 未测状态。无法访问目标机器时，安装和复制保持待完成；无 GitHub 分支不是本地开发阻塞。
