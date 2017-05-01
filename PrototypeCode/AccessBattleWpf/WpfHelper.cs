using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AccessBattleWpf
{
    public static class WpfHelper
    {
        public static  bool IsInDesignerMode
        {
            get
            {
                return (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue);
            }
        }
    }
}
