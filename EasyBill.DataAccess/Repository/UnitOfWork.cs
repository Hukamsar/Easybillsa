using AOne.DataAccess.Data;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models;
using DocumentFormat.OpenXml.InkML;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantAccessor _tenantAccessor;
        private Dictionary<Type, object> repositories;
        public UnitOfWork(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, ITenantAccessor tenantAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _tenantAccessor = tenantAccessor;
        }
        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
        public void Save()
        {
           _db.SaveChanges();
        }
        public IRepository<T> GetRepository<T>()
           where T : class
        {
            if (this.repositories == null)
            {
                this.repositories = new Dictionary<Type, object>();
            }

            var type = typeof(T);
            if (!this.repositories.ContainsKey(type))
            {
                this.repositories[type] = new TenantRepository<T>(this._db, this._tenantAccessor, this._httpContextAccessor);
            }

            return (IRepository<T>)this.repositories[type];
        }
    }
}
