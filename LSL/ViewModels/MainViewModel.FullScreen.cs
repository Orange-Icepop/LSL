﻿using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public partial class MainViewModel
    {
        #region 全屏顶栏板块
        private string _FSTitle;
        public string FSTitle
        {
            get => _FSTitle;
            set => this.RaiseAndSetIfChanged(ref _FSTitle, value);
        }
        #endregion

        #region 服务器添加板块
        // Java选择列表绑定
        public List<string> AddingJavaList
        {
            get
            {
                List<string> list = [];
                foreach (var item in JavaVersions)
                {
                    list.Add(item.Vendor + " " + item.Version);
                }
                return list;
            }
        }
        #endregion
    }
}
