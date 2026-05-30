using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPaymentVoucherCategoryRepository
    {
        Task<IList<PaymentVoucherCategory>> GetAll();
        Task<PaymentVoucherCategory> Create(PaymentVoucherCategory model);
        Task<PaymentVoucherCategory> GetById(int? Id);
        Task<PaymentVoucherCategory> Update(PaymentVoucherCategory model);
        Task Delete(PaymentVoucherCategory model);
    }
}
