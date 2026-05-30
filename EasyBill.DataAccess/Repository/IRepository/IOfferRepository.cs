using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IOfferRepository
    {
        Task<IList<Offer>> GetAll();
        Task<Offer> Create(Offer model);
        Task<Offer> GetById(int? Id);
        Task<Offer> Update(Offer model);
        Task Delete(Offer model);
        Task<bool> CheckDuplicateAsync(string OfferName, int id);
    }
}
