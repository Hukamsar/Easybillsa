using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IpaymentVoucherRepository
    {
        Task<IList<PaymentVoucher>> GetAll();
        Task<PaymentVoucher> Create(PaymentVoucher model);
        Task<PaymentVoucher> GetById(int? Id);
        Task<PaymentVoucher> Update(PaymentVoucher model);
        Task Delete(PaymentVoucher model);
    }
}
