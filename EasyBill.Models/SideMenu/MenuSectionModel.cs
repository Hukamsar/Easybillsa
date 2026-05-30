using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Models.SideMenu
{
    public class MenuSectionModel
    {
        public string Title { get; set; } = string.Empty;
        public string[]? Roles { get; set; }
        public bool IsParent { get; set; }
        public List<MenuSectionItemModel>? SectionItems { get; set; }
    }
}
