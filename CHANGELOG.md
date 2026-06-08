# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-06-08

### Added

- 系统托盘：关闭窗口时最小化到托盘，双击图标恢复窗口
- 托盘右键菜单：打开主界面 / 退出
- 开机自启：界面勾选即写入注册表，开机以 `--minimized` 参数后台启动
- 支持 `--minimized` 启动参数，启动后自动隐藏到托盘

### Fixed

- 修复按钮点击无响应的问题（移除 CanExecute 谓词）
- 修复 MSI 安装报错 2819（添加 WIXUI_INSTALLDIR 属性）
- 修复 WiX 构建扩展安装和编码问题

## [0.1.0] - 2026-06-08

### Added

- 实时查看系统中所有运行进程及其内存占用
- 按进程名搜索过滤进程列表
- 为指定进程配置内存上限（快速预设：256MB / 512MB / 1GB / 2GB / 4GB / 8GB）
- 规则启用/禁用/删除（右键菜单操作）
- 后台自动监控：每 3 秒扫描新进程并自动应用匹配的内存限制规则
- 配置持久化：规则保存至 `%APPDATA%\ProcessLimit\rules.json`
- 基于 Windows Job Object API 的进程内存限制（`JOB_OBJECT_LIMIT_PROCESS_MEMORY`）
- WiX v4 构建的 MSI 安装包（含桌面快捷方式、开始菜单入口）
- GitHub Actions CI：创建 Release 时自动构建并上传 MSI

[1.1.0]: https://github.com/sun-praise/process-limit/releases/tag/v1.1.0
[0.1.0]: https://github.com/sun-praise/process-limit/releases/tag/v0.1.0
