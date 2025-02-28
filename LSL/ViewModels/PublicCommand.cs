using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    // 用于放置公共命令（仍然属于视图模型）
    // 主要成员为杂项ICommand
    public class PublicCommand : RegionalVMBase
    {
        public PublicCommand(AppStateLayer appState, ServiceConnector serveCon) : base(appState, serveCon)
        {
        }
    }
}
