# LSL Development Info

Master Priority：重新设计ViewModel的模式

在github发行版中附带index.txt（或者json？）指明各个平台对应的文件名称

在服务器添加页面的选择Java栏目填写路径，而不只是ComboBox

自动检测目录下的未注册服务器

Starred: 增加服务端进程性能监控

## SubProject

1、整合导航逻辑
 - [x] 完整适配string参数的导航命令与enum参数的导航命令
 - [x] 完善反向导航命令于AppState
 - [ ] 创建视图缓存策略
 - [ ] 使用编译时绑定（在完成所有绑定修复后进行，因为需要编译）
 - [ ] 修复所有绑定（包括命令和字段）
 - [ ] 服务器的在线玩家数量、已经添加的服务器数量、正在运行的服务器数量
 - [x] 修复弹窗，并同步修复打开网页的错误处理
 - [x] 修复左栏状态响应

2、解决配置迁移
 - [x] 使用WhenAnyValue监听ServerConfig和JavaList的变化，并更新对应的ObservableCollection

3、跨组件协作
 - [x] 将EventBus迁移到MessageBus
 - [x] 新建跨组件有返回值无耦合的通信方式

4、解决UI问题
 - [x] 修复右栏的溢出问题
 - [x] 将JavaInfo改用TreeDataGrid以优化卡顿，并添加样式
 - [x] 为Popup配置边框样式
 - [ ] 修复删除服务器后无法自动修正右侧视图的问题
 - [ ] 修复删除服务器后无法启动服务器的问题

5、功能修复
 - [x] 修复服务器添加逻辑
 - [x] 修复服务器删除逻辑
 - [x] 松弛正则表达式的匹配限制
 - [ ] 修复服务器修改逻辑
