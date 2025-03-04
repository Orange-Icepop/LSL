﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSL.ViewModels
{
    // 顾名思义，这一部分的MainViewModel是用于控制控件可用性与特殊内容操作的
    // 主要成员是get-set访问器，用于控制控件可用性与特殊内容
    public partial class MainViewModel
    {
        public string LaunchServerButtonContext { get => CurrentServerRunning ? "关闭服务器" : "启动服务器"; }// TODO:配置ICommand的修改
        public ICommand LaunchServerButtonCommand { get => CurrentServerRunning ? StopServerCmd : StartServerCmd; }
        public bool LaunchServerButtonEnabled { get => !CurrentServerRunning || CurrentServerOnline; }
    }
}
