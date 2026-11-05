# PlugHub 插件包契约

此参考描述 PlugHub 插件包契约。同步自启与旧状态清理基线为 2026-11-04 的[框架提交 dbb5ca2](https://github.com/GaoMengGu/PlugHub/commit/dbb5ca27c1aa1d9f86b6a8c5ae889ad9c48b6c3d)；该提交不代表功能已发布。投放前核实目标安装及匹配 Contracts，不能假定已有安装或最新 Release 包含此契约。

技能仓库中的包验证器执行可交付插件包约束：要求 module `version`、可分发 `assembly`、`features` 数组和每个命令 feature 的 `commandType`；普通模块的功能数组非空，显式自启模块允许空数组。它检查 JSON 与载荷文件，不反射加载 DLL，不能证明启动接口、构造函数或实际 Revit 行为。

## 发现规则

- PlugHub 插件包清单文件名为 `packages.json` 或 `*.packages.json`。
- 扫描来源根目录时，先检查根目录 `packages.json`，再递归查找其他 `packages.json` 和 `*.packages.json`。
- `.git` 下的路径会被忽略。
- 只有 JSON 根对象同时包含 `schemaVersion` 和 `modules` 时，清单才会被读取。
- 仓库浏览中，`module.id` 同时作为 package id 和 module id。

## 运行时兼容性

- PlugHub Revit 运行时当前面向 Revit `2020`。
- 如果模块声明了 `revitVersions`，列表必须包含 `"2020"`；否则 PlugHub 会用兼容性诊断跳过该模块。
- `frameworkVersionRange` 当前作为元数据保留，运行时尚未严格计算范围；插件包仓库保持 `">=1.3.0"`，但不能靠它阻止新旧 Contracts 混装。核对实际框架/Contracts 构建或 DLL 哈希，协调匹配版本后投放。
- 插件命令程序集应面向 `net48`。

## 清单形态

默认使用以下清单骨架：

```json
{
  "schemaVersion": "1.1",
  "indexVersion": "V1.0.0",
  "revitVersions": ["2020"],
  "frameworkVersionRange": ">=1.3.0",
  "modules": [
    {
      "id": "plughub.modules.example-tool",
      "version": "V1.0.0",
      "author": "GAOMENGGU",
      "assembly": "dist/PlugHub.ExampleTool.dll",
      "displayName": "示例工具",
      "description": "示例 PlugHub 插件包。",
      "category": "example",
      "tags": ["example", "revit-api"],
      "features": [
        {
          "id": "plughub.modules.example-tool.run",
          "displayName": "运行示例",
          "description": "运行示例命令。",
          "iconPath": "icons/example-tool.png",
          "commandType": "PlugHub.ExampleTool.RunExampleCommand"
        }
      ]
    }
  ]
}
```

## 字段规则

- `indexVersion`：仓库索引快照版本，不参与单个插件更新判断。
- `module.id` 和 `feature.id`：必须全局唯一；使用稳定的小写 dotted/kebab id，例如 `plughub.modules.level-visibility.toggle`。
- `module.version`：模块版本，仓库页展示和更新判断使用这个字段。
- `module.author`：模块作者元数据。
- `module.assembly`：包内相对 DLL 路径。某个功能省略 `commandAssembly` 时，它也是 fallback；新包通常让功能继承该值。
- `module.autoStart`：可选 JSON boolean，缺省或 false 不加载启动入口；字符串、数字、null 均非法。
- `module.startupType`：可选 JSON string；自启为 true 时必须为同记录 `assembly` 内的完整 CLR 类型名，具体接口与生命周期见下节。
- `module.features`：数组；普通命令包非空，合法纯自启包可用 `[]`，无需按钮或图标。
- `module.displayName`：模块分组名称。已有包会让相关模块共享名称，例如视图工具。
- `module.category` / `module.tags`：仓库筛选、默认布局和展示元数据；feature 未设置时会继承这些分类信息。
- `feature.group`：workspace 没有显式 group 布局时的 fallback Ribbon panel 名称。
- `feature.commandAssembly`：可选包内相对 DLL 路径。只有功能使用不同 DLL 时才写；省略时继承 `module.assembly`。
- `feature.commandType`：实现 `Autodesk.Revit.UI.IExternalCommand` 的完整 CLR 类型名。
- `feature.iconPath`：包内相对图标路径。优先指向生成或提供的 PNG 文件，命名为 `icons/<feature>.png`；该文件必须真实存在并随包载荷分发。PlugHub/Revit 图标使用 32×32 透明底 PNG，主体建议在 24×24 安全区内并保留 4px 留白；高分屏由 Revit 自动缩放，不需要额外多倍图。

对外部插件包仓库，除非正在编辑 PlugHub 框架配置，否则不要在包清单根节点放 `packageDirectories`、`moduleSources`、`repositories`、`conflictPolicy`。模块 `enabled` / `visible`、功能 `defaultState` 已移除，不能再用作启停/隐藏开关；`buttonSize` / `order` 等布局字段仍由框架配置管理，包验证器继续拒绝这些字段。

## 同步启动

仅对明确需要事件/资源注册的模块，在 `modules[]` 中声明以下记录；它仍需要前述根对象外壳：

```json
{
  "id": "plughub.modules.example-events",
  "version": "V1.0.0",
  "assembly": "dist/PlugHub.ExampleEvents.dll",
  "autoStart": true,
  "startupType": "PlugHub.ExampleEvents.Startup",
  "features": []
}
```

- `startupType` 必须为该 DLL 内公开、非抽象、可无参构造的 `Autodesk.Revit.UI.IExternalApplication` 类型。普通包省略两字段；false 即使带类型也不实例化。旧 `type`、`IPlugHubModule.Initialize/Shutdown` 和功能 `commandType` 不替代启动入口。
- 框架在 Revit 主线程、框架 `OnStartup` 返回前同步调用入口，先于 Ribbon 构建。不延后到 `ApplicationInitialized`；不能假设有活动文档，不通过 `Task.Run` 调用 Revit API，也不为包另注册 `.addin`。入口应短小，阻塞主线程无法被安全强杀。
- 自启与 Ribbon 呈现无关，`features: []` 或功能被布局排除都不影响自启。候选来自已安装/有效本地来源，通过兼容性和重复 ID 检查；远程仓库缓存不参与启动。顺序由框架的 Order/ID 决定，`dependsOn` 目前没有启动调度语义。
- 同一会话每个模块至多尝试一次。构造/初始化失败或返回 Failed/Cancelled 会记录诊断并继续其他模块；已进入 `OnStartup` 的失败实例尽力调用一次 `OnShutdown`，构造前失败没有实例可清理。包须撤销失败前的局部注册，并让清理对部分初始化安全。
- 成功实例保持到退出，按成功启动的逆序调用 `OnShutdown`；一个清理失败不阻断其他实例。框架后续 Ribbon 构建失败也会尽力清理。
- 刷新、点击按钮和会话内新增包不重试初始化；自启包安装、更新、卸载或声明变更在下次 Revit 会话生效，当前成功实例保持到关闭。用户须重启验收，不能依赖 net48 热卸载。

## 旧状态迁移与回退

- 删除源码中 `ModuleDescriptor.State` / `ModuleState`、`FeatureDescriptor.DefaultState` / `FeatureState` 引用。描述接口其余成员及现有空 Initialize/Shutdown 可保留，不能承诺框架调用它们；失败转诊断，不另造启停状态。
- 新清单、生成器和模板不输出 `modules[].enabled` / `visible`、`features[].defaultState`；预设只去除 `moduleOverrides[].enabled` / `visible`，保留排序、视图和组合。框架忽略旧值，合格已安装功能恢复正常资格，正常定向保存去旧键；不批量重写无关配置。
- 保留来源 `moduleSources[].enabled`、仓库 `enabled` / public/private、WPF `Visibility`、安装状态和诊断，以及兼容性、稳定 ID、注册/工作区校验。禁止全局按 State/Enabled/Visible 名称删除。
- 保留 feature ID、`commandKey`、`commandType` 和业务行为。盘点源码、模板、测试、随包 Contracts 及实际引用路径，删除失效状态引用后用匹配新 Contracts 重编译所有受影响包；旧 DLL 可能因公开成员删除而加载失败。
- 回退须先关闭 Revit，同时恢复完整框架、匹配 Contracts、包 DLL/依赖及清单/配置备份；只回滚 JSON 不能修复二进制不匹配。

## 安装载荷

安装仓库插件包时，PlugHub 会写入单模块 `packages.json`，并且只复制这些包内相对载荷：

- `module.assembly`
- 每个 `feature.commandAssembly`，如果存在且不同于 `module.assembly`
- 每个 `feature.iconPath`

绝对载荷路径不会被复制，也不适合作为插件包实践。相对载荷文件缺失会导致安装失败。

## 运行时加载

- PlugHub 会相对模块 resolved base directory 解析 `feature.commandAssembly`，通常也就是清单目录。
- 运行时会先把包文件 shadow copy 到 `runtime-cache`，再加载命令程序集。
- `commandType` 从缓存程序集加载，且必须能赋值给 `Autodesk.Revit.UI.IExternalCommand`。
- 命令实例通过无参构造函数创建。

## 来源配置示例

本地开发来源：

```json
{
  "id": "local-plughub-packages",
  "type": "localFolder",
  "path": "D:/path/to/PlugHub_Packages",
  "manifestPath": "packages.json",
  "enabled": true,
  "autoUpdate": false
}
```

GitHub 来源：

```json
{
  "id": "plughub-packages",
  "type": "github",
  "repository": "GaoMengGu/PlugHub_Packages",
  "ref": "main",
  "path": "packages/github/GaoMengGu_PlugHub_Packages",
  "manifestPath": "packages.json",
  "enabled": true,
  "autoUpdate": true
}
```

用户投放目录（仅为已确认扫描根目录下的相对结构示例）：

```text
packages/dropins/PlugHub_Packages/
  packages.json
  dist/
    PlugHub.ExampleTool.dll
  icons/
    example-tool.png
```

## 本地投放边界

`packages/dropins/PlugHub_Packages/` 是结构示例，不是固定绝对安装路径；从有效 addin 的 Assembly 位置与模块配置解析真实扫描根目录，默认是安装目录下的 packages。源码路径、仓库缓存和 runtime-cache 都不能作为已安装目录的证据。详见 [本地安装及复制流程](authoring-playbook.md)。

手动本地交付复制验证过的完整包，保留清单目录内的 dist、icons 和必需依赖结构；上述“仓库安装载荷”描述框架自身的安装服务，并不代替手动完整复制。备份/暂存放在所有扫描范围之外，避免重复 module/feature id；保护其他包和 config，复制后核对哈希并重新验证目标包。刷新由用户操作，不能据此宣称 Revit 命令已执行成功。
