# 编写手册

实现或修复 PlugHub 插件包仓库时使用此手册。

## 实现步骤

1. 选择稳定命名。
   - Assembly：`PlugHub.<FeatureArea>`。
   - Namespace：与 assembly 保持一致。
   - Module id：`plughub.modules.<feature-area>`。
   - Feature id：`plughub.modules.<feature-area>.<verb-or-action>`。

2. 创建或更新项目。
   - 目标框架使用 `net48`。
   - 引用 `PlugHub.Contracts`。
   - 本地开发时通过已安装 DLL 引用 Revit API；CI 编译引用可使用 `Autodesk.Revit.SDK`，并设置 `PrivateAssets="all"`、`ExcludeAssets="runtime"`。
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
                Order = 500,
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
                        Order = 510,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandAssembly = "dist/PlugHub.ExampleTool.dll",
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
   - 在 `package.json` 中添加 module 和 feature 记录。
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

6. 验证。
   - 运行 `dotnet run --project <skill-dir>/tools/PlugHub.PackageValidator/PlugHub.PackageValidator.csproj -- <package-root>`。
   - 如果包仓库有 `.\tests\Validate-Package.ps1`，运行它。
   - 未安装 Revit 时运行 `.\build.ps1 -UseRevitApiNuGet`。
   - 本机有 Revit API DLL 时运行 `.\build.ps1 -RevitApiDir "<Revit 2020 API DLL directory>"`。
   - Revit 行为仍必须在 Windows + Revit 2020 运行时验证。

## 仓库检查清单

- `package.json` 包含 `schemaVersion`、`version`、`revitVersions`、`frameworkVersionRange` 和 `modules`。
- 每个 `module.id` 和 `feature.id` 都唯一且稳定。
- 每个命令功能都声明 `commandAssembly` 和 `commandType`。
- 每个相对 `assembly`、`commandAssembly`、`iconPath` 指向的文件都存在。
- `dist/*.dll` 包含命令类型，并作为分发包的一部分保留。
- `icons/*.png` 是生成或用户提供的真实 PNG，存在且为包内相对路径。
- 不要把 `bin/`、`obj/`、PDB、Revit API DLL 视为插件包 release 载荷。
- 最终 release 或 ZIP 只包含用户安装需要的载荷，例如 `package.json`、`dist/*.dll`、`icons/*.png`。
