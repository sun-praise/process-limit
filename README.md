# ProcessLimit

一个 Windows 桌面工具，通过图形界面为指定进程设置最大内存限制。基于 **WPF + .NET 8**，使用 Windows **Job Object** API 实现进程级内存限制。

## 为什么需要它

现代 Web 应用（如 Electron 应用、浏览器标签页、基于 Web 技术的笔记/聊天工具等）往往会大量占用内存，导致 Windows 系统卡顿甚至无响应。Windows 虽然提供了任务管理器，但只能查看和结束进程，并没有提供一个合适的操作界面来限制进程的资源使用量。ProcessLimit 填补了这个空白——让你通过简洁的图形界面为任意进程设置内存上限，超出限制时系统会自动终止该进程，保护系统流畅运行。

## 功能

- 实时查看系统中所有运行进程及其内存占用
- 按进程名搜索过滤
- 为指定进程配置内存上限（支持快速预设：256MB / 512MB / 1GB / 2GB / 4GB / 8GB）
- 规则启用/禁用/删除
- 后台自动监控：每 3 秒扫描新启动的进程，自动应用匹配的内存限制规则
- 配置持久化：规则保存在 `%APPDATA%\ProcessLimit\rules.json`

## 原理

使用 Windows [Job Object](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects) API 的 `JOB_OBJECT_LIMIT_PROCESS_MEMORY` 限制。将目标进程分配到带有内存限制的 Job Object 中，当进程内存使用超出限制时，系统会自动终止该进程。

## 运行要求

- Windows 10 1809+ / Windows 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- **管理员权限**（操作其他进程所需）

## 使用

```bash
# 克隆
git clone https://github.com/sun-praise/process-limit.git
cd process-limit

# 运行（需要管理员权限）
dotnet run
```

也可以直接构建发布版本：

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

生成的可执行文件在 `bin\Release\net8.0-windows\win-x64\publish\` 目录下。

## 操作说明

1. 左侧面板显示所有运行中的进程，可搜索过滤
2. 选中一个进程，点击右侧「从选中进程」自动填入进程名
3. 设置内存上限（MB），或使用底部的快速预设按钮
4. 点击「添加」创建限制规则
5. 右键规则可以启用/禁用/删除

## 项目结构

```
ProcessLimit/
├── Models/
│   └── ProcessRule.cs            # 限制规则模型
├── Helpers/
│   └── NativeMethods.cs          # Windows API P/Invoke 声明
├── Services/
│   ├── JobObjectService.cs       # Job Object 核心封装
│   ├── ConfigService.cs          # JSON 配置持久化
│   └── ProcessMonitorService.cs  # 后台进程监控服务
├── MainWindow.xaml / .cs         # WPF 主界面
├── MainViewModel.cs              # MVVM ViewModel
└── ProcessLimit.csproj
```

## 技术栈

- **UI 框架**: WPF (.NET 8)
- **架构模式**: MVVM
- **核心 API**: Windows Job Object (kernel32.dll)
- **配置存储**: JSON 文件

## 许可证

[MIT](LICENSE)
