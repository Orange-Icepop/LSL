using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Windows.Input;

namespace LSL.Controls
{
    public partial class ServerTree : TreeView
    {
        // 依赖属性：用于绑定第一层的数据  
        public static readonly StyledProperty<object> ServerTreeRootItemProperty =
            AvaloniaProperty.Register<ServerTree, object>(nameof(ServerTreeRootItem));

        public object ServerTreeRootItem
        {
            get => GetValue(ServerTreeRootItemProperty);
            set => SetValue(ServerTreeRootItemProperty, value);
        }

        // 依赖属性：用于绑定按钮点击的命令  
        public static readonly StyledProperty<ICommand> OpenListProperty =
            AvaloniaProperty.Register<ServerTree, ICommand>(nameof(OpenListCmd));

        public ICommand OpenListCmd
        {
            get => GetValue(OpenListProperty);
            set => SetValue(OpenListProperty, value);
        }
        //节点声明
        public class ListTreeNode
        {
            public string Text { get; set; }
            public object CommandParameter { get; set; }
            public List<ListTreeNode> Children { get; set; }
            
            public ListTreeNode(string text, object commandParameter = null, List<ListTreeNode> children = null)
            {
                Text = text;
                CommandParameter = commandParameter;
                Children = children ?? new List<ListTreeNode>();
            }
        }

        public ServerTree()
        {
            InitializeComponent();
        }
    }
}
