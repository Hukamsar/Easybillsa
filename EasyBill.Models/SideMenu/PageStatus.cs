using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models.SideMenu
{
    public enum PageStatus
    {
        [Description("Coming Soon")] ComingSoon,
        [Description("WIP")] Wip,
        [Description("New")] New,
        [Description("Completed")] Completed
    }
}
