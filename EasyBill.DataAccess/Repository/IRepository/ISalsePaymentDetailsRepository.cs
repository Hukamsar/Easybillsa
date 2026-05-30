using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ISalsePaymentDetailsRepository
    {
        Task<IList<SalsePaymentDetails>> GetAll();
        Task<SalsePaymentDetails> Create(SalsePaymentDetails model);
        Task<SalsePaymentDetails> GetById(int? Id);
        Task<SalsePaymentDetails> Update(SalsePaymentDetails model);
        Task Delete(SalsePaymentDetails model);
    }
}
