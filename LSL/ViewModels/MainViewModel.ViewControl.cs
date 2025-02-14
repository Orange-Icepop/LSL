using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    // 顾名思义，这一部分的MainViewModel是用于控制控件可用性与特殊内容操作的
    // 主要成员是get-set访问器，用于控制控件可用性与特殊内容
    public partial class MainViewModel
    {
        private void InitViewControl()
        {
            LaunchServerButtonContext = "启动服务器";
            LaunchServerButtonEnabled = false;
        }
        public string LaunchServerButtonContext { get; set; }
        public bool LaunchServerButtonEnabled { get; set; }
    }
}
