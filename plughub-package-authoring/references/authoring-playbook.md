# 编写手册

实现或修复 PlugHub 插件包仓库时使用此手册。

## 安装与目录检查

1. 检查工作区并保护已有未提交改动；无 GitHub 分支也可开始本地任务。不要自动 stash、切分支、同步 main、上传或推送；遇到同文件改动先保留并确认边界。
2. 确认安装状态、实际目标机器及操作系统、Revit 2020、.NET SDK 和 PowerShell。Linux/macOS 或不可访问的 Windows 目标只能记录可完成的静态工作，不能声称已安装或投放。
3. 检查目标机器有效的 Revit 2020 addin（机器级及当前用户级），读取 PlugHub.addin 中的 Assembly，核实对应 `PlugHub.Revit2020.dll`、`PlugHub.Contracts.dll`、配置和文件版本；目录名或源码 checkout 不能证明安装。多个 addin/安装候选时列出路径及证据，请用户确认正在使用的实例，不猜测。
4. 已安装则复用并核对兼容性，不自动升级。未安装时从核实的官方 GitHub 获取执行时最新版本，按对应 tag 的说明安装并复查 addin、DLL、版本与目录。入口为 [GaoMengGu/PlugHub](https://github.com/GaoMengGu/PlugHub)、[最新 Release](https://github.com/GaoMengGu/PlugHub/releases/latest)；可用下列 PowerShell 查询元数据，选择该 Release 的官方安装资产，记录 tag、下载 URL 和安装结果：

```powershell
$Release = Invoke-RestMethod 'https://api.github.com/repos/GaoMengGu/PlugHub/releases/latest'
$Release | Select-Object tag_name, html_url
$Release.assets | Select-Object name, browser_download_url
```

优先使用官方 `PlugHub-Setup-<tag>.exe`，按该 tag 安装说明执行，不猜静默参数。若只提供源码，则按对应 commit 的构建安装说明操作，记录 commit；没有可用发布/说明、网络受阻、来源不明、版本不兼容时停止安装并报告。不得静默换旧版或预发布版。安装需要 UAC 或组织管理员权限而当前无交互能力时，交给用户/IT 执行，复查前保持“安装待完成”，不绕过提权。

5. 根据实际 addin Assembly 目录定位运行根目录，读取其有效 `config` 下的模块配置，解析 `packageDirectories` 及启用的 `localFolder` 来源；相对路径以运行根目录解析。官方默认投放在安装目录下的 `packages`，但目录示例不能代替实际配置。用户指定目的地也必须确认被当前实例扫描；不能复制到仓库缓存或 `runtime-cache`。
6. 检查目标目录的写权限及替换/恢复所需权限；不要擅改 ACL、取得所有权或清理其他包。来源、版本、目标目录或权限不明时停止相关写操作，记录阻塞并澄清。headless 环境用平台评论/问题面板请求处理，不运行阻塞终端提问。

核查基线（2026-10-31）：官方 API 返回 `V1.5.7`，它只是本次核查快照，不是固定安装版本。[官方用户指南](https://github.com/GaoMengGu/PlugHub/blob/cacc665b64be92cb115b58b8a3db1caac1706067/README.md)说明安装器写入机器级 Revit addin、安装目录可变且需要 UAC；[运行入口](https://github.com/GaoMengGu/PlugHub/blob/cacc665b64be92cb115b58b8a3db1caac1706067/src/PlugHub.Revit2020/ExternalApplicationEntry.cs)以程序集目录为根，[扫描实现](https://github.com/GaoMengGu/PlugHub/blob/cacc665b64be92cb115b58b8a3db1caac1706067/src/PlugHub.Framework/Sources/ModuleSourceResolver.cs)解析配置中的本地路径并递归发现清单。每次执行重新核实安装版本及这些契约。

### 本地交付决策表

| 状态 | 下一步及报告 |
| --- | --- |
| 已安装且兼容 | 记录版本及有效目录，复用安装，进入开发。 |
| 未安装 | 查询官方最新版，按对应说明安装并复查；未复查不记为成功。 |
| 目标不可访问或非 Windows | 停止安装/投放，记录目标访问阻塞；可做的静态工作单独报告。 |
| 来源不明、无可用发布或不兼容 | 停止安装，请用户确认；不静默换来源或版本。 |
| 多个安装或包目录 | 列出 addin/配置证据，请确认目标；停止相关写入。 |
| 无写权限或 UAC 无法完成 | 保留现有文件，请用户/IT 处理；不绕过权限。 |
| 构建失败或包验证失败 | 停止投放，报告失败输出；修复后重新构建和验证。 |
| 同名目标、重复 module/feature id 或不明文件 | 核对是否本次更新，确认后备份；其他包 id 冲突或不明内容先澄清。 |
| DLL 占用或复制中断 | 不强制解锁；恢复原包或撤回本次新增目录，报告失败，等待用户关闭相关程序。 |
| 复制核查通过 | 标记“已复制，待用户刷新”；未做 Revit smoke test 则仍为待测。 |

## 实现步骤

本技能和包验证器执行的是可发布、可点击插件包强约束：module 必须有版本、可分发程序集和非空功能列表，命令功能必须声明 `commandType`。这比 PlugHub 运行时最低 JSON 读取条件更严格，目的是避免交付无法安装或无法点击的包。

1. 选择稳定命名。
   - Assembly：`PlugHub.<FeatureArea>`。
   - Namespace：与 assembly 保持一致。
   - Module id：`plughub.modules.<feature-area>`。
   - Feature id：`plughub.modules.<feature-area>.<verb-or-action>`。

2. 创建或更新项目。
   - 目标框架使用 `net48`。
   - 引用 `PlugHub.Contracts`。
   - 本地开发时通过已安装 DLL 引用 Revit API；本地 NuGet 编译引用可使用 `Autodesk.Revit.SDK`，并设置 `PrivateAssets="all"`、`ExcludeAssets="runtime"`。
   - 不要把 Revit API DLL 当作插件包载荷。

最小项目形态：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <AssemblyName>PlugHub.ExampleTool</AssemblyName>
    <RootNamespace>PlugHub.ExampleTool</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup Condition="'$(RevitApiReferenceMode)' == 'Installed'">
    <Reference Include="RevitAPI">
      <HintPath>$(RevitApiDir)\RevitAPI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="RevitAPIUI">
      <HintPath>$(RevitApiDir)\RevitAPIUI.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup Condition="'$(RevitApiReferenceMode)' == 'NuGet'">
    <PackageReference Include="Autodesk.Revit.SDK" Version="$(RevitApiNuGetVersion)" PrivateAssets="all" ExcludeAssets="runtime" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="$(PlugHubRoot)\src\PlugHub.Contracts\PlugHub.Contracts.csproj" />
  </ItemGroup>
</Project>
```

按实际 PlugHub checkout 调整 `ProjectReference` 路径。

3. 添加模块描述类。

```csharp
using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.ExampleTool
{
    public sealed class ExampleToolModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.example-tool",
                Name = "示例工具",
                Description = "示例 PlugHub 插件包。",
                State = ModuleState.Enabled,
                Tags = new[] { "example", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.example-tool.run",
                        ModuleId = "plughub.modules.example-tool",
                        Name = "运行示例",
                        Description = "运行示例命令。",
                        Category = "example",
                        Group = "示例工具",
                        Tags = new[] { "example" },
                        CommandType = "PlugHub.ExampleTool.RunExampleCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }
        public void Shutdown() { }
    }
}
```

描述器用于让程序集自描述，清单仍是 PlugHub 插件包发现的主入口。不要把 `Order`、`DefaultState`、`ButtonSize`、`CommandAssembly` 等兼容字段照搬到仓库 JSON 清单；新包让 feature 继承 module `assembly`，布局和状态交给框架默认值与用户配置。

4. 添加外部命令。

```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.ExampleTool
{
    [Transaction(TransactionMode.Manual)]
    public sealed class RunExampleCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var document = commandData?.Application?.ActiveUIDocument?.Document;
            if (document == null)
            {
                message = "未找到当前 Revit 文档。";
                return Result.Failed;
            }

            try
            {
                using (var transaction = new Transaction(document, "运行示例"))
                {
                    transaction.Start();
                    // 在这里执行 Revit API 修改。
                    transaction.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
```

5. 注册插件包。
   - 在 `packages.json` 中添加 module 和 feature 记录。
   - 如果存在 solution 文件，将项目加入 solution。
   - 如果仓库使用集中构建脚本，将项目路径加入 `build.ps1`。
   - 使用文生图生成 feature 图标：从功能名称和描述中提取核心动词/名词，剔除写实细节，把概念转为方块、阵列、粗箭头或重叠剪影，再按 PlugHub 风格化生成 PNG。
   - 使用这个提示词，把 `[Icon Concept]` 替换为功能概念：

```text
Role: Expert UI/UX Icon Designer
Task: Generate a professional app icon matching a specific strict minimalistic design system.

[Icon Concept]
Create a flat, solid glyph icon that abstractly represents: [Icon Concept].
The concept should be highly simplified into basic geometric shapes like blocks, arrows, matrices, or clean overlapping silhouettes. Do NOT draw realistic objects, interface screenshots, text, or fine lines.

[Visual Style Constraints - STRICT]
1. Style: Ultra-minimalist, 100% flat design, solid glyph icon. NO gradients, NO 3D shading, NO fine details, NO outline strokes.
2. Color: Strictly monochrome. Solid dark charcoal (#1A1A1A). Use a transparent background, not a filled white square.
3. Shape Language: Heavy visual weight. If lines or arrows are used, they must be very thick and bold. All sharp corners and edges must have a subtle, micro-rounded finish.
4. Scale & Contrast: Use positive/negative space contrast so the icon is recognizable at 16x16.
5. Revit Asset Size: Output exactly one 32×32 画布. Keep the main glyph inside a 24×24 安全区 with 4px 留白 on all sides so Revit does not crop it.
6. Output Format: 透明背景 PNG only. Revit scales 32×32 source icons automatically on high-DPI displays, so 不用额外做多倍图, @2x, @3x, or other scaled variants.

Output: Only the black and transparent solid icon PNG asset, perfectly centered, without any text, frame, outline, or extra background fill.
```

   - 保存生成的 PNG 到 `icons/<feature>.png`。
   - 设置 `feature.iconPath` 为 `icons/<feature>.png`。
   - 构建输出到 `dist/<AssemblyName>.dll`。

6. 本地编译并验证。
   - 先按本机 DLL 或 NuGet 编译引用运行实际脚本；NuGet 也是本地编译，不是远程构建。确认 SDK、PowerShell、`PlugHub.Contracts` 引用路径及脚本参数可用。
   - 本机有 API DLL 时运行 `.\build.ps1 -RevitApiDir "<Revit 2020 API DLL directory>"`；否则运行 `.\build.ps1 -UseRevitApiNuGet`。
   - 仅在构建成功后运行 `dotnet run --project <skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj -- <package-root>`；邻接清单加 `--manifest <DllName>.packages.json`。
   - 如果包仓库有 `.\tests\Validate-Package.ps1`，运行它；任何构建/验证失败都停止投放，不能交付旧 DLL。

## 仓库检查清单

- `packages.json` 包含 `schemaVersion`、`indexVersion`、`revitVersions`、`frameworkVersionRange` 和 `modules`。
- 每个 module 包含 `id`、`version`、`assembly` 和 `features`。
- 每个 `module.id` 和 `feature.id` 都唯一且稳定。
- 每个命令功能都声明 `commandType`，并通过 feature `commandAssembly` 或 module `assembly` 解析到命令 DLL。
- 每个相对 `assembly`、`commandAssembly`、`iconPath` 指向的文件都存在；新包通常省略 `commandAssembly` 并继承 module `assembly`。
- `dist/*.dll` 包含命令类型，并作为分发包的一部分保留。
- `icons/*.png` 是生成或用户提供的真实 PNG，存在且为包内相对路径。
- 不要把 `enabled`、`visible`、`defaultState`、`buttonSize` 等运行时状态或布局字段写入仓库清单。
- 不要把 `bin/`、`obj/`、PDB、Revit API DLL 视为插件包 release 载荷。
- 最终 release 或 ZIP 只包含用户安装需要的载荷，例如 `packages.json`、`dist/*.dll`、`icons/*.png`。

## 完整包复制与复查

1. 确认实际扫描根目录和本包唯一目标子目录，检查其他已扫描包的 module/feature id，避免同一功能被重复注册。源是已构建并验证的分发目录，包含清单、`dist/*.dll`、`icons/*.png` 和运行必需依赖；不复制源码、bin/obj、Revit API DLL 或用户 config。
2. 记录已有目标文件清单；同名目标必须确认属于本次更新。已有包先完整备份到所有扫描目录之外，并核对备份文件列表/哈希可恢复；不明文件或归属先澄清。不要把备份放在 packages 内，递归扫描会发现重复清单。
3. 在所有扫描目录之外创建唯一暂存目录，复制整个已验证分发包，保持相对结构；拒绝指向包外的链接/重解析点。先对暂存副本验证并逐文件比较哈希，检查是否遗漏清单、图标和依赖。原包在这一步保持原样。

```powershell
# 四个变量均来自前述检查，不可直接使用占位符；Staging 是尚不存在的唯一目录。
$PackageRoot = '<validated distribution root>'
$Staging = '<unique staging directory outside all scanned roots>'
$Destination = '<verified scanned root>/<this-package>'
$Validator = '<skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj'
Copy-Item -LiteralPath $PackageRoot -Destination $Staging -Recurse -ErrorAction Stop
dotnet run --project $Validator -- $Staging
if ($LASTEXITCODE -ne 0) { throw 'Staged package validation failed' }
```

4. 只有暂存副本完整且备份可恢复时，替换本包唯一子目录。原目标存在时先移到扫描范围外，再将暂存副本移动到目标；避免直接递归覆盖留下旧 DLL。涉及 DLL 占用时停止，不强制关闭程序或解锁。复制/移动可能跨卷且不保证原子性，记录进度并检查每步结果。
5. 发布后核对源与目标的全部相对文件列表和逐文件 SHA-256，再对目标运行相同验证器（邻接清单使用相同 `--manifest`）。校验失败或复制中断时，只撤回此次新增/替换内容并恢复原包；恢复也失败时保留备份及失败证据，停止并请用户处理，不报告完成。不要删除 packages 根目录、其他包或 config。
6. 默认只复制，刷新由用户操作；告知“已复制，待用户刷新”。按安装版本说明让用户在 PlugHub Manager 重新加载配置，必要时自行关闭并重新打开 Revit，检查启用状态和布局。刷新不保证当前 Revit 会话已热替换 DLL；不自动刷新或重启。

交付报告逐项填写：目标机器与安装版本/来源、安装检查或安装结果、构建命令/退出结果、清单与载荷验证、源/目标目录及文件哈希核对、备份/恢复状态、待用户刷新步骤、Revit runtime smoke test 结果或“待执行”。静态验证或复制通过不能证明 Revit 加载及命令执行成功。
