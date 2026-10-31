# Agent Entries

本目录收纳跨 agent 入口文件，避免仓库根目录过于拥挤。

- `core/`：通用身份、SOUL 和 AGENTS 入口，默认中文，同时提供英文版。
- `hermes/`：Hermes 专用入口。
- `openclaw/`：OpenClaw 专用入口。
- `trae/`：Trae 专用入口。
- `codebuddy/`：CodeBuddy 专用入口。

本地交付按所选 SKILL 和 authoring-playbook 执行：检查或安装 PlugHub，本地编译验证后将完整包复制到已核实的扫描目录，刷新由用户操作。保护已有改动、其他包和 config，分别报告安装、构建、验证、复制与运行状态；无人值守的阻塞通过平台评论或问题面板澄清，不使用终端交互提示。
