using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        void Save();
        Task SaveAsync();
        IRepository<T> GetRepository<T>() where T : class;
        Task<bool> IsRecordReferencedAsync<T>(int id) where T : class;
    }
}
