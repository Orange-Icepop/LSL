## 此项目已废弃 This project is obsolete

由于 .NET 框架有着巨大的内存开销，与 LSL 轻量化的初衷分歧越来越大，因此维护者认为没有继续基于 C# 开发的必要，此项目即日起存档。

建议改用基于Tauri和Rust的[SeaLantern](https://github.com/SeaLantern-Studio/SeaLantern)项目，有着开放的插件生态与更加庞大的维护团队，本项目的开发者也在其中，为其提供与MC服务器交互的实现指导。

此项目包含了许许多多与 Minecraft 服务器交互的技巧，希望能够为广大开服器的开发者提供帮助。

- Forge 服务器安装：`LSL.Common/Utilities/Minecraft/ForgeInstaller.cs`
- Java 查找：`LSL.Common/Utilities/Minecraft/JavaFinder.cs`
- 服务端核心类型识别（读取 jar MANIFEST.MF，识别 Vanilla/Forge/Fabric/Paper 等）：`LSL.Common/Utilities/Minecraft/CoreTypeHelper.cs`
- Forge 服务器配置解析（读取 run.sh/run.bat 中的库参数）：`LSL.Common/Utilities/Minecraft/ForgeConfigHelper.cs`
- 跨平台 Java 运行时自动发现（Windows/Linux/macOS）：`LSL.Common/Utilities/Minecraft/JavaFinder.cs`
- Minecraft 玩家/状态/性能指标模型：`LSL.Common/Models/Minecraft/`
- 有界可观察链表（线程安全，适用于性能图表数据）：`LSL.Common/Collections/RangedObservableLinkedList.cs`

除上述核心功能外，LSL 还在UI上做出了创新：

- 基于 **Avalonia UI** 的跨平台图形界面，支持 Windows/Linux/macOS
- 自定义控件库（终端模拟器、性能折线图、按钮、卡片、提示框等）
- 弹窗/对话框系统（确认、警告、错误提示）

# 柠方服务器启动器 | Lime Server Launcher

柠方服务器启动器（LSL）是一个主打轻量与用户友好的 Minecraft Java 版服务器启动器，基于 C# 与 AvaloniaUI 开发，支持 Windows/Linux/MacOS。

### 🎮 简单的 Minecraft 服务器全生命周期管理

LSL 提供了简洁而完整的 Minecraft Java 版服务器管理功能：
- **启动/停止/强制结束**服务器进程
- 向运行中的服务器发送任意控制台命令
- 通过正则解析日志实时检测服务器状态（Done 完成启动、Stopping 正在停止、进程已退出）
- 支持同时管理**多个服务器实例**
- 退出时智能选择：关闭所有服务器 / 让服务器在后台继续运行 / 取消退出

### 📊 实时性能监控

LSL 内置了全面的性能监控系统，帮助你掌握服务器资源占用情况：
- **单服务器维度**：实时 CPU 使用率、内存占用（字节 + 百分比）
- **全局维度**：所有运行中服务器的累计 CPU 消耗、总内存占用及其占系统内存的比例
- **多粒度聚合采样**：每秒采样（保留最近 60 秒）、每分钟聚合（保留最近 30 分钟）
- 基于 **Rx.NET 响应式流**实时推送到 UI 折线图，无需轮询

### 👥 玩家追踪与管理

LSL 能够自动解析服务器的控制台输出流，实现：
- 实时追踪玩家上下线，自动更新在线玩家列表（用户名 + UUID）
- 捕获并展示玩家聊天消息
- 所有状态通过响应式数据流自动同步到 UI
