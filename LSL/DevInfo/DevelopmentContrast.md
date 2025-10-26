# LSL开发规则（自用）

## 一 | 前后分离原则

LSL是一个MVVM应用程序，并且在未来版本中会出现客户端/服务端分离并使用IPC通信的情况。

LSL由四层构成：
1. UI层，包含所有UI控件和界面的设计，主要使用Avalonia XAML(.axaml)和其Code-behind文件编写。原则上不涉及任何与显示效果无关的代码。
2. VM层，负责将业务信息展示到UI层，并将用户操作提交到Service层，使用ReactiveUI框架工作。
3. ServiceConnector，VM层与Service层交流的中间桥接件，（前后端分离后将）分为GUI侧和Daemon侧的代码，使用Protobuf进行IPC通信。
4. Service层，负责具体服务器管理业务，包括但不限于服务器的注册、启动与关闭、性能监控，服务端文件、MOD与插件的下载和管理。

所有从Service层返回的内容均实现IServiceResult接口，包含ResultType成员来告知服务运行结果，以及一个nullable Exception成员告知错误信息。

## 二 | 