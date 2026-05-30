using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IModeOfPaymentRepository
    {
        Task<IList<ModeOfPayment>> GetAll();
        Task<ModeOfPayment> Create(ModeOfPayment model);
        Task<ModeOfPayment> GetById(int? Id);
        Task<ModeOfPayment> Update(ModeOfPayment model);
        Task Delete(ModeOfPayment model);
        Task<bool> CheckDuplicateAsync(string Name, int id);
    }
}
