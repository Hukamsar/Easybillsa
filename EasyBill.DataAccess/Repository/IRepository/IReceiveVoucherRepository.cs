using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IReceiveVoucherRepository
    {
        Task<IList<ReceiveVoucher>> GetAll();
        Task<ReceiveVoucher> Create(ReceiveVoucher model);
        Task<ReceiveVoucher> GetById(int? Id);
        Task<ReceiveVoucher> Update(ReceiveVoucher model); 
        Task Delete(ReceiveVoucher model);
    }
}
