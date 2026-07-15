using AOne.DataAccess.Data;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace EasyBill.DataAccess.Repository
{
    public class BranchItemMappingRepository : TenantRepository<BranchItemMapping>, IBranchItemMappingRepository
    {
        private readonly ApplicationDbContext _context;

        public BranchItemMappingRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ITenantAccessor tenantAccess) 
            : base(context, tenantAccess, httpContextAccessor)
        {
            _context = context;
        }
    }
}
