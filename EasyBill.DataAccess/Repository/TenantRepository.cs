using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using AOne.DataAccess.Data;
using AOne.Models.Entity;
using AOne.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace EasyBill.DataAccess.Repository
{
    public class TenantRepository<T> : IRepository<T>
        where T : class
    {
        private readonly ApplicationDbContext _context; 
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantAccessor _tenantAccess;
        public string Name => throw new NotImplementedException();
        protected DbSet<T> DbSet { get; }

        public TenantRepository(ApplicationDbContext context, ITenantAccessor tenantAccess, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            DbSet = _context.Set<T>();
            _tenantAccess = tenantAccess;
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public IQueryable<T> Query()
        {
           // return DbSet;
            var query = DbSet.AsQueryable();
            return ApplyTenantFilter(query);
        }

        public virtual void Add(T entity)
        {
            try
            {
                EntityEntry dbEntityEntry = _context.Entry(entity);
                _context.Set<T>().Add(entity);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
                
            }
        }
        public virtual void AddRange(IEnumerable<T> entities)
        {
            try
            {
                _context.Set<T>().AddRange(entities);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception("Error during bulk insert: " + ex.Message);
            }
        }

        public virtual int Count()
        {
            try
            {
                return _context.Set<T>().AsNoTracking().Count();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public int Count(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return _context.Set<T>().AsNoTracking().Where(predicate).Count();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);

            }
        }

        public IQueryable<T> GetAll()
        {
            try
            {
                return _context.Set<T>().AsNoTracking().AsQueryable();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public T GetSingle(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return _context.Set<T>().AsNoTracking().Where(predicate).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
         
        public T GetSingle(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
        {
            try
            {
                IQueryable<T> query = _context.Set<T>();
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }

                return query.AsNoTracking().Where(predicate).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public virtual void Update(T entity)
        {
            try
            {
                EntityEntry dbEntityEntry = _context.Entry(entity);
                dbEntityEntry.State = EntityState.Modified;
                _context.SaveChanges();
                // _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            try
            {
                foreach (var entity in entities)
                {
                    _context.Entry(entity).State = EntityState.Modified;
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception("Error during bulk update: " + ex.Message);
            }
        }

        public virtual void Delete(T entity)
        {
            try
            {
                EntityEntry dbEntityEntry = _context.Entry<T>(entity);
                dbEntityEntry.State = EntityState.Deleted;
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public virtual void DeleteWhere(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var entites = _context.Set<T>().Where(predicate);
                foreach (var entity in entites)
                {
                    _context.Entry<T>(entity).State = EntityState.Deleted;
                   
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public IQueryable<T> GetAll(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return _context.Set<T>().AsNoTracking().Where(predicate).AsQueryable();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public IQueryable<T> GetAllIncluding(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
        {
            try
            {
                IQueryable<T> query = _context.Set<T>();
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }

                return query.AsNoTracking().Where(predicate).AsQueryable();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public IQueryable<T> GetAll(int count)
        {
            try
            {
                return _context.Set<T>().AsNoTracking().AsQueryable().Take(count);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private IQueryable<T> ApplyTenantFilter<T>(IQueryable<T> query) where T : class
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return query;

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return query;

            if (user.IsInRole("SuperAdmin"))
                return query;

            bool hasTenant = typeof(IMayHaveTenant).IsAssignableFrom(typeof(T));
            bool hasCreatedBy = typeof(T).GetProperty("CreatedBy") != null;
            bool isTenantMasterTable = typeof(IMasterEntity).IsAssignableFrom(typeof(T));
            bool isGMasterTable = typeof(IGMasterEntity).IsAssignableFrom(typeof(T));

            var tenantId = _tenantAccess.GetCurrentTenantId();
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? filterExpression = null;

            if (isGMasterTable)
            {
                return query;
            }
            else if (isTenantMasterTable)
            {
                if (hasTenant && !string.IsNullOrEmpty(tenantId))
                {
                    var tenantProp = Expression.Property(parameter, "TenantId");
                    var tenantConst = Expression.Constant(tenantId);
                    filterExpression = Expression.Equal(tenantProp, tenantConst);
                }
            }
            else if (typeof(T) == typeof(Tenant))
            {
                if (!string.IsNullOrEmpty(tenantId))
                {
                    var idProp = Expression.Property(parameter, "Id");
                    var tenantConst = Expression.Constant(tenantId);
                    filterExpression = Expression.Equal(idProp, tenantConst);
                }
            }
            else
            {
                if (user.IsInRole("Admin") || user.IsInRole("Doctor"))
                {
                    if (hasTenant && !string.IsNullOrEmpty(tenantId))
                    {
                        var tenantProp = Expression.Property(parameter, "TenantId");
                        var tenantConst = Expression.Constant(tenantId);
                        filterExpression = Expression.Equal(tenantProp, tenantConst);
                    }
                }
                else
                {
                    if (hasCreatedBy)
                    {
                        var createdByProp = Expression.Property(parameter, "CreatedBy");
                        var createdByConst = Expression.Constant(userId);
                        filterExpression = Expression.Equal(createdByProp, createdByConst);
                    }
                }
            }

            if (filterExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(filterExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }
    }
}
