using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ISalseSettingRepository
    {
        Task<IList<SalseSetting>> GetAll();
        Task<SalseSetting> Create(SalseSetting model);
        Task<SalseSetting> GetById(int? Id);
        Task<SalseSetting> GetByUserId(string? userid);
        Task<SalseSetting> Update(SalseSetting model);
        Task Delete(SalseSetting model);
    }
}
