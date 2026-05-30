using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models.SideMenu
{
    public class MenuSectionItemModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Href { get; set; }
        public string? ControllerName { get; set; }  // ✅ Add this
        public string? ActionName { get; set; }
        public string[]? Roles { get; set; }
        public PageStatus PageStatus { get; set; } = PageStatus.Completed;
        public bool IsParent { get; set; }
        public List<MenuSectionSubItemModel>? MenuItems { get; set; }
    }
}
