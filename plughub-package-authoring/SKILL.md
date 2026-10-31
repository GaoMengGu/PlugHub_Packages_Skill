---
name: plughub-package-authoring
description: 用于创建、审查或修复 Revit 2020 PlugHub 外部插件包，使其能被 PlugHub 发现、安装和加载。适用于 packages.json 或 *.packages.json 清单、dist/*.dll 载荷、PlugHub.Contracts 模块、Autodesk.Revit.UI.IExternalCommand 命令、icons、build.ps1、PlugHub 安装检查、本地编译与目录投放、本地/GitHub/Gitee 插件源，以及需要兼容 Codex、Hermes、OpenClaw、Trae、CodeBuddy 的插件包编写任务。
---

# PlugHub 插件包编写

使用此技能时，目标是产出一个完整的、可被 PlugHub 加载的插件包，而不是只写一个 Revit 命令类。有效插件包必须同时具备可发现清单、包内相对载荷路径、`net48` 命令程序集和验证证据。

本技能和包验证器执行的是“可发布、可点击插件包”的强约束：每个 module 必须有可分发 DLL、版本和至少一个可点击 feature，每个命令 feature 必须有 `commandType`。这比 PlugHub 运行时最低可读取 JSON 契约更严格，用来避免 agent 交付只能被扫描但不能安装、点击或诊断的半成品。

## 需求处理

- 将用户需求当作插件功能规格，而不是文档咨询。除非用户明确只要方案，否则要实际创建或修改插件包文件。
- 先把需求落到一个用户可点击的 Revit 功能：模块 id、功能 id、Ribbon 分组、命令类型、输入方式、事务边界、失败消息和验证方式。
- 如果需求涉及模型修改，命令必须使用明确的 Revit API 事务；如果只是读取或选择，也要处理无活动文档、无活动视图、取消选择等失败路径。
- 默认产出一个可被 PlugHub 发现的完整包：`packages.json` / `*.packages.json`、`dist/*.dll` 构建路径、图标路径、项目文件、构建脚本登记和验证记录。
- 如果当前环境无法运行 Revit 2020，只能声明静态验证和构建结果，并明确标记 Revit runtime smoke test 待执行；不要把静态验证等同于运行成功。

## 图标生成

- 编写或修复插件功能时，要使用文生图能力为每个用户可点击功能生成图标 PNG，保存到 `icons/<feature>.png`，并更新 `feature.iconPath` 指向这个包内相对路径。
- 文生图生成图标时根据功能 `displayName`、`description` 和核心动作提炼概念，再套用 PlugHub 图标设计语言：极简几何抽象、100% flat、纯黑/深碳灰 `#1A1A1A`、透明背景、实心块状 glyph、微圆角、负空间、16x16 小尺寸可辨。
- 图标生成提示词必须包含核心句子 `Create a flat, solid glyph icon`，并明确要求输出严格 32×32 画布、主体控制在 24×24 安全区、四周 4px 留白、透明底 PNG、无文字、无多余底色、无边框、无渐变、无阴影、无描边、无 3D 透视；可直接使用 `references/authoring-playbook.md` 中的模板。
- Revit 高分屏渲染会自动缩放，使用 32×32 原图即可，不用额外做多倍图或 `@2x`/`@3x` 变体。
- 只有用户明确提供图标、明确要求不生成图标，或当前 agent 环境没有可用文生图能力时，才跳过生成；跳过时必须在交付结果中标记图标资产待补，不要伪造 `iconPath` 指向不存在的文件。

## 工作流

1. 检查目标环境与 PlugHub 安装。
   - 检查工作区，保护已有未提交改动；本地交付不要求 GitHub 分支、同步 main、上传、推送或远程 workflow。仅在用户要求版本控制交付时执行对应 Git 操作，不做破坏性覆盖。
   - 先读取 `references/authoring-playbook.md` 的安装与目录检查；确认目标机器可访问、操作系统、Revit 2020、构建工具、PlugHub 安装状态与版本。
   - 从实际 Revit addin 的 Assembly 路径、安装文件和有效配置定位安装根目录及实际扫描的包目录，检查写权限；源码 checkout 不等于已安装。
   - 已安装则复用，不自动升级。未安装时从核实的官方 GitHub 获取执行时最新版本，按该版本说明安装并复查；官方入口为 https://github.com/GaoMengGu/PlugHub/releases/latest 。
   - 来源、兼容性、权限或多目录归属不明时停止相关写入并记录阻塞；headless 环境通过平台评论或问题面板澄清，不使用阻塞终端提示。

2. 定位目标插件包根目录和附近示例。
   - 优先沿用已有 `PlugHub_Packages` 结构。
   - 如果不确定包格式，读取 `references/plughub-package-contract.md`。
   - 如果要新增插件包，读取 `references/authoring-playbook.md`。
   - 如果运行在 Hermes、OpenClaw、Trae、CodeBuddy 或其他非 Codex agent 中，读取 `references/agent-compatibility.md`。

3. 让插件包可被发现。
   - 在包根目录放置有效 `packages.json`；平铺投放单个 DLL 时，可使用同目录 `<DllName>.packages.json`。
   - 所有载荷路径都保持为相对清单目录的路径。
   - 模块程序集使用 `dist/<AssemblyName>.dll`，功能默认继承 module `assembly`，功能图标使用 `icons/<feature>.png`。

4. 实现 Revit 命令程序集。
   - 目标框架使用 `.NET Framework 4.8` / `net48`。
   - 引用 `RevitAPI.dll`、`RevitAPIUI.dll` 和 `PlugHub.Contracts`。
   - 每个用户可触发功能对应一个 `IExternalCommand` 类。
   - 添加一个 `IPlugHubModule` 类，让 `Describe()` 与清单元数据保持一致；但不要只依赖这个类，PlugHub 插件包发现读取的是 JSON 清单。

5. 更新仓库集成文件。
   - 如果仓库使用解决方案和 `build.ps1`，把新项目加入其中。
   - 在根清单中新增或更新模块和功能记录。
   - 确保构建会产出 `dist/*.dll`，并且该 DLL 作为插件包分发载荷保留。
   - 为每个功能生成图标 PNG，保存到 `icons/<feature>.png`，更新 `feature.iconPath`，并确保图标是真实包文件，不使用 `builtin:` 引用或绝对路径。

6. 本地编译与验证。
   - 先沿用实际项目入口构建：有本机 API 时使用 `.\build.ps1 -RevitApiDir "<Revit 2020 install dir>"`；否则用 `.\build.ps1 -UseRevitApiNuGet` 获取 NuGet 编译引用并在本地编译。确认脚本支持这些参数；缺 SDK/PowerShell 时报告限制，不把本地编译换成上传 GitHub Actions。
   - 构建成功后运行 `dotnet run --project <skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj -- <package-root>` 检查清单和载荷；邻接清单加 `--manifest <DllName>.packages.json`。
   - 如果仓库有验证器，运行实际入口，通常为 `.\tests\Validate-Package.ps1`。构建或验证失败时停止投放，不使用旧 DLL 冒充新产物。

7. 完整包投放与交付。
   - 按 playbook 先核实实际目标目录、写权限和同名包冲突；更新前备份到所有扫描目录之外，遇到占用或不明文件停止替换，保护其他包和 config。
   - 仅将验证通过的完整包（清单、dist DLL、icons 和必需依赖）按相对结构复制到确认的包目录；复制后核对文件列表/哈希并重新运行目标包验证，失败时恢复原包或撤回本次新增目录。
   - 刷新由用户操作：默认只复制，不自动刷新或重启 Revit；按安装版本说明告知用户重新加载配置，必要时由用户重开 Revit。
   - 分别报告安装、构建、清单/载荷验证、复制与待刷新状态；Revit runtime smoke test 未执行就明确标记待执行。行为验证需在 Windows + Revit 2020 的安全测试模型或族文件中完成。

## 硬性规则

- 不要只创建 `.addin` 文件。PlugHub 从 `packages.json` / `*.packages.json` 加载插件包功能。
- 不要绕过安装检查、构建或包验证直接投放；不要清空 packages 根目录、覆盖 config 或强制解锁已加载 DLL。
- 不要让 `assembly`、`commandAssembly` 或 `iconPath` 保持绝对路径。PlugHub 按清单目录解析相对路径，并会拒绝复制清单目录外的安装载荷。
- 新包优先让功能继承 module `assembly`，不要在仓库清单中重复写 `commandAssembly`，除非某个功能确实使用不同 DLL。
- 不要主动写入 `enabled`、`visible`、`defaultState`、`buttonSize` 等运行时状态或布局字段；这些由框架默认值和用户配置负责。
- 不要省略 `commandType`；它必须是实现 `Autodesk.Revit.UI.IExternalCommand` 的完整类型名。
- 不要把业务逻辑写在模块描述类里。业务行为放在命令类中。
- 在报告清单验证、构建验证和相关 Revit smoke test 结果前，不要声称插件包可被 PlugHub 加载。

## 参考文件

- `references/plughub-package-contract.md`：PlugHub 的发现、清单、安装和运行时加载规则。
- `references/authoring-playbook.md`：实现模板、包结构和验证命令。
- `references/agent-compatibility.md`：Codex、Hermes、OpenClaw、Trae、CodeBuddy 的通用操作约束。
