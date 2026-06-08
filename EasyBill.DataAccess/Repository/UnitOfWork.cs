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
using System.Linq.Expressions;
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

        public async Task<bool> IsRecordReferencedAsync<T>(int id) where T : class
        {
            var entityType = _db.Model.FindEntityType(typeof(T));
            var referencingFKs = entityType.GetReferencingForeignKeys();

            foreach (var fk in referencingFKs)
            {
                var dependentType = fk.DeclaringEntityType.ClrType;
                var fkColumn = fk.Properties.First().Name;

                var dbSet = GetDbSetForType(dependentType);

                // x => x.FKColumn == id
                var parameter = Expression.Parameter(dependentType, "x");
                var property = Expression.Property(parameter, fkColumn);

                Expression constant;

                // If FK is nullable: convert id to int?
                if (property.Type.IsGenericType &&
                    property.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlying = Nullable.GetUnderlyingType(property.Type);
                    var nullableValue = Convert.ChangeType(id, underlying);
                    constant = Expression.Constant(nullableValue, property.Type);
                }
                else
                {
                    constant = Expression.Constant(id, property.Type);
                }

                var equal = Expression.Equal(property, constant);
                var lambda = Expression.Lambda(equal, parameter);

                // Get AnyAsync(source, predicate, cancellationToken)
                var method = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .Where(m => m.Name == "AnyAsync"
                        && m.GetParameters().Length == 3)   // FIXED: must have 3 parameters
                    .Single()
                    .MakeGenericMethod(dependentType);

                // Call AnyAsync
                var task = (Task<bool>)method.Invoke(
                    null,
                    new object[] { dbSet, lambda, CancellationToken.None }   // FIXED: 3 parameters passed
                );

                bool exists = await task;

                if (exists)
                    return true;
            }

            return false;
        }
        private IQueryable<object> GetDbSetForType(Type type)
        {
            var method = typeof(DbContext).GetMethod("Set", Type.EmptyTypes);
            var generic = method.MakeGenericMethod(type);
            return (IQueryable<object>)generic.Invoke(_db, null);
        }
    }
}
