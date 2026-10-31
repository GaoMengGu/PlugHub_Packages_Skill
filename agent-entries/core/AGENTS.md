# PlugHub 插件包编写 AGENTS

默认入口文件为中文：`agent-entries/core/IDENTITY.md`、`agent-entries/core/SOUL.md`、`agent-entries/core/AGENTS.md`。英文入口文件为：`agent-entries/core/IDENTITY.en-US.md`、`agent-entries/core/SOUL.en-US.md`、`agent-entries/core/AGENTS.en-US.md`。

## 使命 / mission

把用户的 Revit 2020 功能需求交付为完整的 PlugHub 外部插件包，先检查或安装 PlugHub、本地编译验证并投放完整包，使其能被发现、安装到 PlugHub、显示到 PlugHub Ribbon，并在 Revit 2020 中执行。

## 加载顺序

1. 读取 `agent-entries/core/IDENTITY.md` 获取一句话对外身份。
2. 读取 `agent-entries/core/SOUL.md` 获取身份、记忆、沟通风格和规则。
3. 读取根目录 `skills.json` 发现可安装 skill 路径。
4. 根据语言加载一个 skill 目录：
   - 中文：`plughub-package-authoring`
   - English：`plughub-package-authoring-en`
5. 读取所选目录的 `SKILL.md`；仅在任务需要时加载引用文件：
   - 写清单前读取 `references/plughub-package-contract.md`。
   - 新建包前读取 `references/authoring-playbook.md`。
   - 在 Hermes、OpenClaw、Trae、CodeBuddy 等非 Codex 环境中读取 `references/agent-compatibility.md`。

## 工作流 / workflow

1. 检查目标机器、操作系统、Revit 2020 和 PlugHub 安装状态；保护已有未提交改动，无需 GitHub 分支即可进入本地任务。
2. 先读 `references/authoring-playbook.md`，从有效 addin、DLL 和配置确认版本及实际扫描目录；已安装复用，缺失时从官方 GitHub 获取执行时最新版本，按对应说明安装并复查。来源、兼容性、权限或多目录不明时停止相关写入并澄清。
3. 定位插件包根目录、现有清单和附近示例。
4. 将需求映射为模块 id、功能 id、Ribbon 分组、图标路径、命令程序集和命令类型。
5. 维护 `packages.json` 或 `*.packages.json`，确保 `schemaVersion`、`indexVersion`、`revitVersions`、`frameworkVersionRange`、module `version` 和 `modules` 完整。
6. 实现或修复 `net48` 命令程序集，命令类型必须实现 `Autodesk.Revit.UI.IExternalCommand`。
7. 保持载荷为包内相对路径：`dist/*.dll` 和 `icons/*.png`。
8. 更新构建入口、解决方案或包仓库集成文件。
9. 使用 `-RevitApiDir` 或 `-UseRevitApiNuGet` 本地构建成功后，运行 C# 包验证器及仓库验证器；失败即停止投放。
10. 按 playbook 核实目标和写权限，备份同名包到扫描目录外，复制完整包并核对文件/哈希、重新验证；保留其他包及 config，遇到占用/复制失败恢复并报告。刷新由用户操作，不自动重启；Git 提交/推送只在用户要求时执行。

## 交付 / delivery

交付结果必须说明：

- 修改或新增的清单文件。
- `assembly`、可选 `commandAssembly`、`iconPath` 对应的包内相对载荷。
- 新增或修复的命令类型。
- 构建集成位置。
- C# 静态验证和编译结果。
- Revit 2020 runtime smoke test 结果；无法执行时，明确标记待执行。
- 目标机器、安装来源/版本及安装复查结果。
- 真实目的目录、复制核对与备份/恢复状态、待用户刷新步骤；未投放明确标记原因。

## Hermes / OpenClaw

- Hermes：优先使用原生 skill 发现；不可用时按本文件的加载顺序读取仓库本地指令。
- OpenClaw：将 `agent-entries/core/IDENTITY.md`、`agent-entries/core/SOUL.md`、`agent-entries/core/AGENTS.md` 作为根指令层，再加载所选 skill 目录。
- 两者都不要假设 Codex 专用工具名、本机安装路径或隐藏全局状态。

无人值守时通过平台评论或问题面板请求澄清，不使用阻塞终端提示。
