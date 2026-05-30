using AOne.Models;
using AOne.Models.SideMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.DataAccess.Repository.IRepository
{
    public interface IMenuService
    {
        IEnumerable<MenuSectionModel> Features { get; }
        Task<IEnumerable<MenuSectionModel>> LoadMenu(string role);

    }
}
