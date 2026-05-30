using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        //T-Department
        IQueryable<T> GetAll();
        T GetSingle(Expression<Func<T, bool>> predicate); 
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);

        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);


        void Delete(T entity);
        IQueryable<T> Query();
        void DeleteWhere(Expression<Func<T, bool>> predicate);
        IDbContextTransaction BeginTransaction();
        Task SaveChangesAsync();

    }
}
