using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IOpticalRepository
    {
        Task<IList<Optical>> GetAll();
        Task<Optical> Create(Optical model);
        Task<Optical> GetById(int? Id);
        Task<Optical> Update(Optical model);
        Task Delete(Optical model);
        Task<IList<Optical>> GetBySalesOrder(int salesOrderId, int itemmasterId);
    }
}
