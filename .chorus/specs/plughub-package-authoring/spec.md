---
slug: plughub-package-authoring
title: PlugHub 插件包本地编写与交付
status: done
created: 2026-10-31
---

## Intent

让使用中文或英文 Skill 的 agent 根据用户需求，在目标机器检查或安装 PlugHub，编写并本地编译完整插件包，验证后复制到 PlugHub 实际使用的包目录。

## Requirements

- [x] 默认流程先识别目标操作系统、Revit 环境、PlugHub 安装状态、版本、实际安装及包目录；源码目录存在不等于已安装，多候选目录不猜测。
- [x] PlugHub 缺失时从可核实的 GitHub 官方仓库获取执行时最新版本，按对应版本安装说明安装并复查；来源不明、不兼容、权限不足或失败时报告阻塞，不声明成功。
- [x] 本地编译支持本机 Revit API DLL 和 NuGet 编译引用两种路径，沿用 Revit 2020/net48 与 PlugHub.Contracts 契约。
- [x] 清单、DLL 和图标等载荷验证通过后，复制完整包到已确认目录；保护已有包及用户配置，复制后核查文件，刷新由用户操作。
- [x] 默认不要求 GitHub 分支、同步 main、上传、推送或跟踪远程 workflow；保留下载 PlugHub 所需的 GitHub 访问和本地改动保护规则。
- [x] 双语 Skill、参考文档、相关 README、agent 入口和发现元数据一致；仓库静态校验验证新流程并保留现有包契约和图标约束。
- [x] 交付分别报告环境与安装证据、编译结果、清单与载荷验证、投放目录与文件、待刷新事项和 Revit 运行验证状态。

## Non-goals

- 不开发业务插件或额外样例包，不改变 PlugHub 框架和包清单格式。
- 不增加自动刷新、重启 Revit、自动升级已安装 PlugHub或 GitHub 发布功能。
- 本次变更只交付 Skill 工作流和相关静态校验，不在本仓库开发环境执行目标机器安装或业务插件投放。
