using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models.SideMenu
{
    public class MenuSectionSubItemModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Href { get; set; }
        public string? ControllerName { get; set; }  // ✅ Add this
        public string? ActionName { get; set; }
        public string[]? Roles { get; set; }
        public bool IsParent { get; set; } = false;
        public PageStatus PageStatus { get; set; } = PageStatus.Completed;
        public List<MenuSectionSubItemModel> SubItems { get; set; } = new();
    }
}
