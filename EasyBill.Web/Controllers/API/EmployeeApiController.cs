using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using EasyBill.Models;
using System.Text.Json;

namespace EasyBill.Web.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeApiController : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly ITenantRepository _tenantRepo;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUsers> _userManager;

        public EmployeeApiController(
            IEmployeeRepository employeeRepo, 
            ITenantRepository tenantRepo, 
            IMemoryCache cache,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUsers> userManager)
        {
            _employeeRepo = employeeRepo;
            _tenantRepo = tenantRepo;
            _cache = cache;
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(string? hoTenantId)
        {
            var cacheKey = $"StaffList_v2_{hoTenantId ?? "all"}";

            if (!_cache.TryGetValue(cacheKey, out var result))
            {
                var tenants = await _tenantRepo.GetAll();
                var validTenantIds = string.IsNullOrEmpty(hoTenantId) 
                    ? tenants.Select(t => t.Id).ToHashSet()
                    : tenants.Where(t => t.Id == hoTenantId || t.ParentTenantId == hoTenantId).Select(t => t.Id).ToHashSet();

                var allEmployees = await _employeeRepo.GetAll();
                var filteredEmployees = string.IsNullOrEmpty(hoTenantId) 
                    ? allEmployees.ToList()
                    : allEmployees.Where(e => validTenantIds.Contains(e.TenantId)).ToList();

                result = filteredEmployees.Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Email,
                    e.Phone,
                    DepartmentName = e.Departments?.Name ?? "N/A",
                    DesignationName = e.Designation?.Name ?? "N/A",
                    TenantId = e.TenantId,
                    TenantName = tenants.FirstOrDefault(t => t.Id == e.TenantId)?.Name ?? "N/A"
                }).ToList();

                // Elite strategy: Short TTL (Absolute) for real-time dashboard feel, while protecting DB from burst polling.
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(30)); 

                _cache.Set(cacheKey, result, cacheOptions);
            }

            return Ok(new { success = true, data = result });
        }

        [HttpGet("GetTenants")]
        public async Task<IActionResult> GetTenants(string? hoTenantId)
        {
            var tenants = await _tenantRepo.GetAll();
            if (!string.IsNullOrEmpty(hoTenantId))
            {
                tenants = tenants.Where(t => t.ParentTenantId == hoTenantId || t.Id == hoTenantId).ToList();
            }
            var result = tenants.Select(t => new
            {
                t.Id,
                t.Name
            });
            return Ok(new { success = true, data = result });
        }

        [HttpPost("Transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            if (request == null || request.EmployeeId <= 0 || string.IsNullOrEmpty(request.ToTenantId))
                return BadRequest(new { success = false, message = "Invalid transfer request parameters." });

            var employee = await _dbContext.Employees.FindAsync(request.EmployeeId);
            if (employee == null)
                return NotFound(new { success = false, message = "Employee not found." });

            var toTenant = await _dbContext.Tenants.FindAsync(request.ToTenantId);
            if (toTenant == null)
                return NotFound(new { success = false, message = "Destination Tenant not found." });

            var fromTenantId = employee.TenantId;

            // Create transfer log
            var log = new EmployeeTransferLog
            {
                EmployeeId = employee.Id,
                FromTenantId = fromTenantId,
                ToTenantId = request.ToTenantId,
                ToDepartmentId = request.ToDepartmentId,
                ToDesignationId = request.ToDesignationId,
                TransferDate = DateTime.UtcNow,
                TransferReason = request.TransferReason,
                IsReadByDestination = false
            };

            _dbContext.EmployeeTransferLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = $"Employee '{employee.Name}' transfer initiated to '{toTenant.Name}'. Waiting for destination to accept." });
        }

        [HttpGet("GetTransferLogs")]
        public async Task<IActionResult> GetTransferLogs(string? hoTenantId)
        {
            var query = _dbContext.EmployeeTransferLogs
                .Include(l => l.Employee)
                .AsQueryable();

            if (!string.IsNullOrEmpty(hoTenantId))
            {
                query = query.Where(l => l.FromTenantId == hoTenantId || l.ToTenantId == hoTenantId || 
                                         _dbContext.Tenants.Any(t => t.Id == l.FromTenantId && t.ParentTenantId == hoTenantId) ||
                                         _dbContext.Tenants.Any(t => t.Id == l.ToTenantId && t.ParentTenantId == hoTenantId));
            }

            var logs = await query.OrderByDescending(l => l.TransferDate).ToListAsync();
            var tenants = await _tenantRepo.GetAll();

            var result = logs.Select(l => new
            {
                l.Id,
                EmployeeName = l.Employee?.Name ?? "Unknown",
                l.TransferDate,
                l.TransferReason,
                FromTenantId = l.FromTenantId,
                FromTenantName = tenants.FirstOrDefault(t => t.Id == l.FromTenantId)?.Name ?? "N/A",
                ToTenantId = l.ToTenantId,
                ToTenantName = tenants.FirstOrDefault(t => t.Id == l.ToTenantId)?.Name ?? "N/A"
            });

            return Ok(new { success = true, data = result });
        }

        [HttpGet("GetUnreadTransferAlerts")]
        public async Task<IActionResult> GetUnreadTransferAlerts()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            var unreadLogs = await _dbContext.EmployeeTransferLogs
                .Include(l => l.Employee)
                .ThenInclude(e => e.Departments)
                .Include(l => l.Employee)
                .ThenInclude(e => e.Designation)
                .Where(l => l.ToTenantId == tenantId && !l.IsReadByDestination)
                .OrderByDescending(l => l.TransferDate)
                .ToListAsync();

            var tenants = await _tenantRepo.GetAll();

            var result = unreadLogs.Select(l => new
            {
                l.Id,
                EmployeeName = l.Employee?.Name ?? "Unknown",
                Department = l.Employee?.Departments?.Name ?? "N/A",
                Designation = l.Employee?.Designation?.Name ?? "N/A",
                l.TransferDate,
                l.TransferReason,
                FromTenantName = tenants.FirstOrDefault(t => t.Id == l.FromTenantId)?.Name ?? "N/A"
            });

            return Ok(new { success = true, data = result });
        }

        [HttpPost("AcknowledgeTransferAlert/{id}")]
        public async Task<IActionResult> AcknowledgeTransferAlert(int id)
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(tenantId)) return Unauthorized();

            var log = await _dbContext.EmployeeTransferLogs.FirstOrDefaultAsync(l => l.Id == id && l.ToTenantId == tenantId);
            if (log == null) return NotFound(new { success = false, message = "Alert not found." });

            var originalEmployee = await _dbContext.Employees.FindAsync(log.EmployeeId);
            if (originalEmployee != null)
            {
                // Check if employee already exists in destination branch by phone or email
                var existingEmp = await _dbContext.Employees.FirstOrDefaultAsync(e => e.TenantId == tenantId && 
                    ((!string.IsNullOrEmpty(e.Phone) && e.Phone == originalEmployee.Phone) || 
                     (!string.IsNullOrEmpty(e.Email) && e.Email == originalEmployee.Email)));
                
                int targetEmployeeId;
                if (existingEmp != null)
                {
                    // Reactivate if deleted
                    existingEmp.Deleted = null;
                    existingEmp.DeletedBy = null;
                    targetEmployeeId = existingEmp.Id;
                }
                else
                {
                    // Create new employee record for destination branch
                    var newEmp = new Employee
                    {
                        Name = originalEmployee.Name,
                        Email = originalEmployee.Email,
                        Phone = originalEmployee.Phone,
                        RequiredCreditional = originalEmployee.RequiredCreditional,
                        Initials = originalEmployee.Initials,
                        IsHoAdmin = originalEmployee.IsHoAdmin,
                        TenantId = tenantId,
                        DepartMentId = log.ToDepartmentId, 
                        DesignationId = log.ToDesignationId, 
                        AccountGroupId = originalEmployee.AccountGroupId,
                        Created = DateTime.UtcNow,
                        CreatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    };
                    _dbContext.Employees.Add(newEmp);
                    await _dbContext.SaveChangesAsync(); // Save to get the new ID
                    targetEmployeeId = newEmp.Id;
                }

                // Move login account to new employee
                var appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == originalEmployee.Id);
                if (appUser != null)
                {
                    appUser.TenantId = tenantId;
                    appUser.EmployeeId = targetEmployeeId;
                    await _userManager.UpdateAsync(appUser);
                }

                // Soft-delete the original employee so it disappears from source branch's active list
                originalEmployee.Deleted = DateTime.UtcNow;
                originalEmployee.DeletedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }

            log.IsReadByDestination = true;
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("Inspect")]
        public async Task<IActionResult> Inspect()
        {
            var user = await _userManager.Users.OrderByDescending(u => u.Id).FirstOrDefaultAsync();
            if (user == null) return NotFound();
            var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId);
            return Ok(new {
                user.Id,
                user.UserName,
                user.TenantId,
                user.AllowedBranches,
                EmployeeTenantId = employee?.TenantId
            });
        }

        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments()
        {
            var tenantId = User.FindFirst("TenantId")?.Value ?? Request.Query["hoTenantId"].ToString();
            var departments = await _dbContext.Departments.Where(d => d.TenantId == tenantId || d.TenantId == null).Select(d => new { d.Id, d.Name }).ToListAsync();
            return Ok(new { success = true, data = departments });
        }

        [HttpGet("GetDesignations")]
        public async Task<IActionResult> GetDesignations()
        {
            var tenantId = User.FindFirst("TenantId")?.Value ?? Request.Query["hoTenantId"].ToString();
            var designations = await _dbContext.designations.Where(d => d.TenantId == tenantId || d.TenantId == null).Select(d => new { d.Id, d.Name }).ToListAsync();
            return Ok(new { success = true, data = designations });
        }

        public class MasterCreationDto
        {
            public string Name { get; set; }
            public string HoTenantId { get; set; }
        }

        [HttpPost("CreateDepartment")]
        public async Task<IActionResult> CreateDepartment([FromBody] MasterCreationDto model)
        {
            if (string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.HoTenantId))
                return BadRequest(new { success = false, message = "Name and HO TenantId are required." });

            var dept = new EasyBill.Models.Entity.Department
            {
                Name = model.Name,
                TenantId = model.HoTenantId
            };
            
            _dbContext.Departments.Add(dept);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, data = new { dept.Id, dept.Name } });
        }

        [HttpPost("CreateDesignation")]
        public async Task<IActionResult> CreateDesignation([FromBody] MasterCreationDto model)
        {
            if (string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.HoTenantId))
                return BadRequest(new { success = false, message = "Name and HO TenantId are required." });

            var desig = new EasyBill.Models.Entity.Designation
            {
                Name = model.Name,
                TenantId = model.HoTenantId
            };
            
            _dbContext.designations.Add(desig);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, data = new { desig.Id, desig.Name } });
        }

        [HttpGet("GetHoUsers")]
        public async Task<IActionResult> GetHoUsers(string hoTenantId)
        {
            if (string.IsNullOrEmpty(hoTenantId))
                return BadRequest("hoTenantId is required");

            var users = await _dbContext.Employees
                .Where(e => e.TenantId == hoTenantId && e.IsHoAdmin)
                .Select(e => new
                {
                    Id = _dbContext.Users.Where(u => u.EmployeeId == e.Id).Select(u => u.Id).FirstOrDefault() ?? e.Id.ToString(),
                    EmployeeId = e.Id,
                    Name = e.Name ?? "",
                    Email = e.Email,
                    PhoneNumber = e.Phone,
                    AllowedBranches = _dbContext.Users.Where(u => u.EmployeeId == e.Id).Select(u => u.AllowedBranches).FirstOrDefault(),
                    DepartMentId = e.DepartMentId,
                    DesignationId = e.DesignationId,
                    Initials = (int?)e.Initials,
                    RequiredCreditional = e.RequiredCreditional
                })
                .ToListAsync();

            return Ok(new { success = true, data = users });
        }

        [HttpPost("CreateHoUser")]
        public async Task<IActionResult> CreateHoUser([FromBody] JsonElement requestBody)
        {
            try
            {
                var rawJson = requestBody.GetRawText();
                
                using (JsonDocument document = JsonDocument.Parse(rawJson))
                {
                    var root = document.RootElement;
                    
                    string email = root.TryGetProperty("email", out var el) ? el.GetString() : null;
                    string name = root.TryGetProperty("name", out var nl) ? nl.GetString() : 
                                  (root.TryGetProperty("fullName", out var fn) ? fn.GetString() : "");
                    string phone = root.TryGetProperty("phone", out var pl) ? pl.GetString() : 
                                   (root.TryGetProperty("phoneNumber", out var pn) ? pn.GetString() : null);
                    string password = root.TryGetProperty("password", out var pwl) ? pwl.GetString() : null;
                    
                    int? departmentId = null;
                    if (root.TryGetProperty("departMentId", out var deptProp) || root.TryGetProperty("departmentId", out deptProp)) {
                        if (deptProp.ValueKind == JsonValueKind.Number) departmentId = deptProp.GetInt32();
                        else if (deptProp.ValueKind == JsonValueKind.String && int.TryParse(deptProp.GetString(), out int dId)) departmentId = dId;
                    }

                    int? designationId = null;
                    if (root.TryGetProperty("designationId", out var desigProp)) {
                        if (desigProp.ValueKind == JsonValueKind.Number) designationId = desigProp.GetInt32();
                        else if (desigProp.ValueKind == JsonValueKind.String && int.TryParse(desigProp.GetString(), out int dId2)) designationId = dId2;
                    }

                    int? initialsVal = null;
                    if (root.TryGetProperty("initials", out var initProp)) {
                        if (initProp.ValueKind == JsonValueKind.Number) initialsVal = initProp.GetInt32();
                        else if (initProp.ValueKind == JsonValueKind.String && int.TryParse(initProp.GetString(), out int iVal)) initialsVal = iVal;
                    }

                    bool reqCred = false;
                    if (root.TryGetProperty("requiredCreditional", out var reqProp)) {
                        if (reqProp.ValueKind == JsonValueKind.True) reqCred = true;
                        else if (reqProp.ValueKind == JsonValueKind.String && bool.TryParse(reqProp.GetString(), out bool bVal)) reqCred = bVal;
                    }

                    // Try multiple names for TenantId
                    string tenantId = root.TryGetProperty("tenantId", out var tl) ? tl.GetString() : 
                                      (root.TryGetProperty("hoTenantId", out var htl) ? htl.GetString() : null);

                    if (string.IsNullOrEmpty(tenantId))
                    {
                        tenantId = Request.Query["hoTenantId"].ToString();
                    }
                    if (string.IsNullOrEmpty(tenantId))
                    {
                        tenantId = Request.Query["tenantId"].ToString();
                    }

                    // If TenantId is still null, fallback to the logged-in user's TenantId
                    if (string.IsNullOrEmpty(tenantId) && User.Identity.IsAuthenticated)
                    {
                        tenantId = User.FindFirst("TenantId")?.Value;
                    }

                    if (reqCred)
                    {
                        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                            return BadRequest(new { success = false, message = "Email and Password are required when 'Required Credential' is checked." });

                        var existingUser = await _userManager.FindByEmailAsync(email);
                        if (existingUser != null)
                            return BadRequest(new { success = false, message = "Email is already registered." });
                    }

                    var newEmployee = new Employee
                    {
                        Name = name ?? "",
                        Email = email,
                        Phone = phone,
                        TenantId = tenantId,
                        DepartMentId = departmentId,
                        DesignationId = designationId,
                        Initials = initialsVal.HasValue ? (AOne.Utility.Enums.Initials)initialsVal.Value : null,
                        RequiredCreditional = reqCred,
                        IsHoAdmin = true
                    };

                    _dbContext.Employees.Add(newEmployee);
                    await _dbContext.SaveChangesAsync();



                    // Robustly parse AllowedBranches (handles strings or objects)
                    List<string> allowedBranches = new List<string>();
                    JsonElement branchesElement;
                    if (root.TryGetProperty("allowedBranches", out branchesElement) || 
                        root.TryGetProperty("branches", out branchesElement) ||
                        root.TryGetProperty("branchAccess", out branchesElement) ||
                        root.TryGetProperty("selectedBranches", out branchesElement))
                    {
                        if (branchesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in branchesElement.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                {
                                    allowedBranches.Add(item.GetString());
                                }
                                else if (item.ValueKind == JsonValueKind.Object)
                                {
                                    if (item.TryGetProperty("id", out var idProp))
                                        allowedBranches.Add(idProp.GetString());
                                    else if (item.TryGetProperty("tenantId", out var tIdProp))
                                        allowedBranches.Add(tIdProp.GetString());
                                }
                            }
                        }
                    }
                    if (reqCred)
                    {
                        var user = new ApplicationUsers
                        {
                            UserName = email,
                            Email = email,
                            PhoneNumber = phone,
                            EmployeeId = newEmployee.Id,
                            TenantId = tenantId,
                            EmailConfirmed = true,
                            PhoneNumberConfirmed = true
                        };

                        if (allowedBranches.Any())
                        {
                            user.AllowedBranches = System.Text.Json.JsonSerializer.Serialize(allowedBranches);
                        }

                        var result = await _userManager.CreateAsync(user, password);

                        if (result.Succeeded)
                        {
                            string roleId = root.TryGetProperty("roleId", out var rl) ? rl.GetString() : null;
                            if (!string.IsNullOrEmpty(roleId))
                            {
                                var role = await _dbContext.Roles.FindAsync(roleId);
                                if (role != null)
                                {
                                    await _userManager.AddToRoleAsync(user, role.Name);
                                }
                            }
                            return Ok(new { success = true, message = "HO User created successfully." });
                        }
                        return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                    }
                    else
                    {
                        return Ok(new { success = true, message = "HO User created successfully (without login credentials)." });
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error processing request: " + ex.Message });
            }
        }

        [HttpPost("EditHoUser")]
        public async Task<IActionResult> EditHoUser([FromBody] JsonElement requestBody)
        {
            try
            {
                var root = requestBody;
                
                string userId = root.TryGetProperty("id", out var idl) ? idl.GetString() : null;
                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "User ID is required for editing." });

                var existingUser = await _userManager.FindByIdAsync(userId);
                int? employeeId = null;

                if (existingUser != null)
                {
                    employeeId = existingUser.EmployeeId;
                }
                else
                {
                    // If no ApplicationUser found, maybe it's just an Employee ID passed (because reqCred was false)
                    if (int.TryParse(userId, out int empIdFallback))
                    {
                        employeeId = empIdFallback;
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = "User not found." });
                    }
                }

                string name = root.TryGetProperty("name", out var nl) ? nl.GetString() : "";
                string phone = root.TryGetProperty("phone", out var pl) ? pl.GetString() : null;
                
                int? departmentId = null;
                if (root.TryGetProperty("departMentId", out var deptProp) || root.TryGetProperty("departmentId", out deptProp)) {
                    if (deptProp.ValueKind == JsonValueKind.Number) departmentId = deptProp.GetInt32();
                    else if (deptProp.ValueKind == JsonValueKind.String && int.TryParse(deptProp.GetString(), out int dId)) departmentId = dId;
                }

                int? designationId = null;
                if (root.TryGetProperty("designationId", out var desigProp)) {
                    if (desigProp.ValueKind == JsonValueKind.Number) designationId = desigProp.GetInt32();
                    else if (desigProp.ValueKind == JsonValueKind.String && int.TryParse(desigProp.GetString(), out int dId2)) designationId = dId2;
                }

                int? initialsVal = null;
                if (root.TryGetProperty("initials", out var initProp)) {
                    if (initProp.ValueKind == JsonValueKind.Number) initialsVal = initProp.GetInt32();
                    else if (initProp.ValueKind == JsonValueKind.String && int.TryParse(initProp.GetString(), out int iVal)) initialsVal = iVal;
                }

                bool reqCred = false;
                if (root.TryGetProperty("requiredCreditional", out var reqProp)) {
                    if (reqProp.ValueKind == JsonValueKind.True) reqCred = true;
                    else if (reqProp.ValueKind == JsonValueKind.String && bool.TryParse(reqProp.GetString(), out bool bVal)) reqCred = bVal;
                }

                // Parse allowed branches
                List<string> allowedBranches = new List<string>();
                JsonElement branchesElement;
                if (root.TryGetProperty("tenantIds", out branchesElement) || 
                    root.TryGetProperty("allowedBranches", out branchesElement) || 
                    root.TryGetProperty("branches", out branchesElement) ||
                    root.TryGetProperty("selectedBranches", out branchesElement))
                {
                    if (branchesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in branchesElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String) allowedBranches.Add(item.GetString());
                            else if (item.ValueKind == JsonValueKind.Object)
                            {
                                if (item.TryGetProperty("id", out var idProp)) allowedBranches.Add(idProp.GetString());
                                else if (item.TryGetProperty("tenantId", out var tIdProp)) allowedBranches.Add(tIdProp.GetString());
                            }
                        }
                    }
                }

                if (existingUser != null)
                {
                    existingUser.PhoneNumber = phone;
                    existingUser.AllowedBranches = allowedBranches.Any() ? System.Text.Json.JsonSerializer.Serialize(allowedBranches) : null;
                    await _userManager.UpdateAsync(existingUser);
                }

                // Update Employee table if exists
                if (employeeId.HasValue)
                {
                    var emp = await _dbContext.Employees.FindAsync(employeeId.Value);
                    if (emp != null)
                    {
                        emp.Name = name;
                        emp.Phone = phone;
                        emp.DepartMentId = departmentId;
                        emp.DesignationId = designationId;
                        if (initialsVal.HasValue) emp.Initials = (AOne.Utility.Enums.Initials)initialsVal.Value;
                        emp.RequiredCreditional = reqCred;
                        
                        // If they now want credentials but didn't have them before
                        if (reqCred && existingUser == null)
                        {
                            string email = root.TryGetProperty("email", out var el) ? el.GetString() : null;
                            string password = root.TryGetProperty("password", out var pwl) ? pwl.GetString() : null;
                            
                            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                                return BadRequest(new { success = false, message = "Email and Password are required to create new login credentials." });
                                
                            var newUser = new ApplicationUsers
                            {
                                UserName = email,
                                Email = email,
                                PhoneNumber = phone,
                                EmployeeId = emp.Id,
                                TenantId = emp.TenantId,
                                EmailConfirmed = true,
                                PhoneNumberConfirmed = true,
                                AllowedBranches = allowedBranches.Any() ? System.Text.Json.JsonSerializer.Serialize(allowedBranches) : null
                            };
                            
                            var result = await _userManager.CreateAsync(newUser, password);
                            if (!result.Succeeded)
                                return BadRequest(new { success = false, message = "Failed to create credentials: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
                        }

                        _dbContext.Employees.Update(emp);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                return Ok(new { success = true, message = "HO User updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "Error processing request: " + ex.Message });
            }
        }

        [HttpGet("GetEmployeeBranches")]
        public async Task<IActionResult> GetEmployeeBranches(int employeeId)
        {
            try
            {
                var employee = await _dbContext.Employees.FindAsync(employeeId);
                if (employee == null) return NotFound("Employee not found");

                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
                
                var hoTenantId = employee.TenantId;
                if (string.IsNullOrEmpty(hoTenantId)) return BadRequest("Employee tenant missing");
                
                var allBranches = await _dbContext.Tenants
                    .Where(t => t.Id == hoTenantId || t.ParentTenantId == hoTenantId)
                    .Select(t => new { t.Id, t.Name })
                    .ToListAsync();

                List<string> allowedBranches = new List<string>();
                if (user != null && !string.IsNullOrEmpty(user.AllowedBranches))
                {
                    try { allowedBranches = System.Text.Json.JsonSerializer.Deserialize<List<string>>(user.AllowedBranches) ?? new List<string>(); }
                    catch { }
                }

                return Ok(new
                {
                    hasLogin = user != null,
                    employeeName = employee.Name,
                    userId = user?.Id,
                    allBranches,
                    allowedBranches
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AssignBranches")]
        public async Task<IActionResult> AssignBranches([FromBody] AssignBranchesRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId)) return BadRequest(new { success = false, message = "Invalid user ID" });

                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null) return NotFound(new { success = false, message = "User not found" });

                user.AllowedBranches = System.Text.Json.JsonSerializer.Serialize(request.BranchIds ?? new List<string>());
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new { success = true, message = "Branch access updated successfully" });
                }
                
                return BadRequest(new { success = false, message = "Failed to update user" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }

    public class TransferRequest
    {
        public int EmployeeId { get; set; }
        public string ToTenantId { get; set; }
        public string? TransferReason { get; set; }
        public int? ToDepartmentId { get; set; }
        public int? ToDesignationId { get; set; }
    }

    public class CreateHoUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string? TenantId { get; set; }
        public string? RoleId { get; set; }
        public List<string>? AllowedBranches { get; set; }
    }

    public class AssignBranchesRequest
    {
        public string? UserId { get; set; }
        public List<string>? BranchIds { get; set; }
    }
}
