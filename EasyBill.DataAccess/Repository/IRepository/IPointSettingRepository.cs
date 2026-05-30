using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPointSettingRepository
    {
        Task<IList<PointSetting>> GetAll();
        Task<PointSetting> Create(PointSetting model);
        Task<PointSetting> GetById(int? Id);
        Task<PointSetting> Update(PointSetting model);
        Task Delete(PointSetting model);
    }
}
