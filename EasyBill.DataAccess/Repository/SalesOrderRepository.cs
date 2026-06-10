using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class SalesOrderRepository : ISalesOrderRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public SalesOrderRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<SalesOrder>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesOrder>();
                IList<SalesOrder> results = await repository.Query().Include(x => x.Customers).Include(x => x.salesOrderItems).ThenInclude(x => x.ItemMaster).ThenInclude(x => x.Company).Include(x => x.SalsePaymentDetails).ThenInclude(x => x.ModeOfPayment).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<SalesOrder>> GetByCustomerId(int? customerId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesOrder>();
                IList<SalesOrder> result = await repository.Query().Include(x => x.Customers).Include(x => x.PharmacyDoctor).Include(x => x.salesOrderItems).ThenInclude(x => x.ItemMaster).Where(l => l.CustomerId == customerId).ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SalesOrder> Create(SalesOrder model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesOrder>();
                repository.Add(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SalesOrder> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesOrder>();
                var result = await repository.Query().Include(x => x.Customers).Include(x => x.PharmacyDoctor).Include(x => x.salesOrderItems).ThenInclude(x => x.ItemMaster).Include(x => x.SalsePaymentDetails).Where(l => l.Id == Id).Include(x => x.Opticals).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<SalesOrder> Update(SalesOrder model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesOrder>();
                repository.Update(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }

                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task Delete(SalesOrder model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<SalesOrder>();
                assetGroupRepository.Delete(model);
                using (var transaction = assetGroupRepository.BeginTransaction())
                {
                    await assetGroupRepository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IList<SalesOrder>> GetByCustomerIdWithPayments(int? customerId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesOrder>();
                IList<SalesOrder> result = await repository.Query()
                    .Include(x => x.Customers)
                    .Include(x => x.PharmacyDoctor)
                    .Include(x => x.salesOrderItems)
                        .ThenInclude(x => x.ItemMaster)
                    .Include(x => x.SalsePaymentDetails)
                    .Where(l => l.CustomerId == customerId)
                    .ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// CancelOrder — Soft Delete approach
        ///
        /// PURPOSE: Customer apna order cancel kar sake.
        ///          Hard delete nahi karte — admin audit ke liye record DB mein rehta hai.
        ///          BaseEntity ke Deleted (DateTime?) aur DeletedBy (string) fields set karte hain.
        ///
        /// SAFETY:  Sirf "Unpaid" ya "Partial" orders cancel ho sakte hain.
        ///          Fully paid orders cancel nahi ho sakte (refund logic alag hai).
        ///
        /// RETURNS: true agar cancel hua, false agar order nahi mila ya already paid/cancelled hai.
        /// </summary>
        public async Task<bool> CancelOrder(int orderId, string cancelledBy, string reason)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesOrder>();

                // Fetch the order
                var order = await repository.Query()
                    .Where(o => o.Id == orderId && o.Deleted == null)  // Only non-deleted orders
                    .FirstOrDefaultAsync();

                if (order == null)
                    return false;  // Order nahi mila ya pehle se cancelled hai

                // Safety check: Fully paid order cancel nahi hoga
                if (order.PaidAmount > 0 && order.PaidAmount >= order.TotalPayable)
                    return false;  // Fully paid — refund required, cancel blocked

                // Soft delete — record DB mein rehta hai for admin audit
                order.Deleted    = DateTime.UtcNow;
                order.DeletedBy  = $"CANCELLED_BY:{cancelledBy} | REASON:{reason}";

                repository.Update(order);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
