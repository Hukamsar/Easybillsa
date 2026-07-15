using AOne.DataAccess.Repository;
using AOne.DataAccess.Repository.IRepository;
using Azure.Core;
using DocumentFormat.OpenXml.Office2016.Excel;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using AOne.Models;

namespace EasyBill.DataAccess.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IUnitOfWork _unitofwork;
        private readonly UserManager<ApplicationUsers> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CustomerRepository(IUnitOfWork unitofwork, UserManager<ApplicationUsers> userManager, RoleManager<IdentityRole> roleManager)
        {
            _unitofwork = unitofwork;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<Customer> Create(Customer model)
        { 
            try
            {
                var repository = _unitofwork.GetRepository<Customer>();
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

        public async Task Delete(Customer model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Customer>();
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

        public async Task<IList<Customer>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Customer>();
                IList<Customer> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Customer> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Customer>();
                var result = await repository.Query()
                    .Include(c => c.Addresses)
                    .Where(l => l.Id == Id)
                    .FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Customer> GetCustomerByMobileno(string? phoneno, bool ignoreTenantFilter = false)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Customer>();
                var query = repository.Query();
                
                if (ignoreTenantFilter)
                    query = query.IgnoreQueryFilters();

                var result = await query.Where(l => l.PhoneNo == phoneno).OrderBy(c => c.Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Customer> Update(Customer model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Customer>();
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
        public async Task<bool> CheckDuplicateAsync(string name, int id)
        {
            var repo = _unitofwork.GetRepository<Customer>();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            string cleanedName = name.Trim().ToLower();


            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.Name != null &&

                    x.Name.ToLower() == cleanedName &&
                    x.Id != id

                );

            return alreadyExists;
        }

        public async Task<CustomerOtp> SendOTP(SendOtpRequest sendOtpRequest)
        {
            try
            {
                var otp = new Random().Next(1000, 9999).ToString("D4");
                var model = new CustomerOtp
                {
                    MobileNumber = sendOtpRequest.MobileNumber,
                    OtpCode = otp.ToString(),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false,
                    Created = DateTime.UtcNow

                };
                var repository = _unitofwork.GetRepository<CustomerOtp>();
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
                throw;
            }
        }

        public async Task<VerifyOtpResponse> VerifyOTP(VerifyOtpRequest verifyOtpRequest)
        {
            try
            {
                var otpRepo = _unitofwork.GetRepository<CustomerOtp>();
                var customerRepo = _unitofwork.GetRepository<Customer>();

                var otpRecord = await otpRepo.GetAll().Where(o => o.MobileNumber == verifyOtpRequest.MobileNumber && o.OtpCode == verifyOtpRequest.Otp && o.IsUsed == false).OrderByDescending(x => x.Created).FirstOrDefaultAsync();

                if (otpRecord == null)
                {
                    return new VerifyOtpResponse
                    {
                        Success = false,
                        Message = "OTP is invalid or already used"
                    };
                }

                if (otpRecord.ExpiresAt < DateTime.UtcNow)
                {
                    return new VerifyOtpResponse
                    {
                        Success = false,
                        Message = "OTP has expired"
                    };
                }

                // Mark OTP as used
                otpRecord.IsUsed = true;
                otpRepo.Update(otpRecord);

                using (var transaction = otpRepo.BeginTransaction())
                {
                    await otpRepo.SaveChangesAsync();
                    transaction.Commit();
                }

                // Check existing customer (using GetAll to bypass tenant filters)
                var existingCustomer = await customerRepo.GetAll().FirstOrDefaultAsync(x => x.PhoneNo == verifyOtpRequest.MobileNumber);

                if (existingCustomer == null)
                {
                    // Create new Customer record with default name
                    existingCustomer = new Customer
                    {
                        Name = "New Customer",
                        PhoneNo = verifyOtpRequest.MobileNumber,
                        IsMobileVerified = true,
                        LastLogin = DateTime.UtcNow,
                        LastOtp = otpRecord.OtpCode,
                        OtpExpiresAt = otpRecord.ExpiresAt
                    };
                    customerRepo.Add(existingCustomer);
                    await customerRepo.SaveChangesAsync();
                }
                else
                {
                    existingCustomer.LastOtp = otpRecord.OtpCode;
                    existingCustomer.OtpExpiresAt = otpRecord.ExpiresAt;
                    existingCustomer.LastLogin = DateTime.UtcNow;
                    existingCustomer.IsMobileVerified = true;

                    customerRepo.Update(existingCustomer);
                    await customerRepo.SaveChangesAsync();
                }

                // Find or create ApplicationUsers record for this Customer
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.CustomerId == existingCustomer.Id || u.PhoneNumber == existingCustomer.PhoneNo || u.UserName == existingCustomer.PhoneNo);
                if (user == null)
                {
                    user = new ApplicationUsers
                    {
                        UserName = existingCustomer.PhoneNo,
                        PhoneNumber = existingCustomer.PhoneNo,
                        CustomerId = existingCustomer.Id,
                        Email = string.IsNullOrEmpty(existingCustomer.Email) ? $"{existingCustomer.PhoneNo}@easybill.com" : existingCustomer.Email,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true
                    };
                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create Identity User: {errors}");
                    }

                    if (!await _roleManager.RoleExistsAsync("Customer"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));
                    }
                    var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                    if (!roleResult.Succeeded)
                    {
                        var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to assign role Customer: {errors}");
                    }
                }
                else if (user.CustomerId == null)
                {
                    user.CustomerId = existingCustomer.Id;
                    await _userManager.UpdateAsync(user);
                }

                bool isNew = string.IsNullOrEmpty(existingCustomer.Name) || existingCustomer.Name == "New Customer";

                return new VerifyOtpResponse
                {
                    Success = true,
                    IsNewCustomer = isNew,
                    Message = isNew ? "New customer. Please complete registration." : "Verification successful",
                    Customer = new CustomerResponse
                    {
                        Id = existingCustomer.Id,
                        FullName = existingCustomer.Name,
                        MobileNumber = existingCustomer.PhoneNo,
                        Email = existingCustomer.Email,
                        Address = existingCustomer.Address,
                        IsMobileVerified = existingCustomer.IsMobileVerified,
                        IdentityUserId = user?.Id
                    }
                };
            }
            catch (Exception ex)
            {
                return new VerifyOtpResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<CreateCustomerResponse> CreateCustomer(CreateCustomerRequest request)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Customer>();
                var existingCustomer = await repository.GetAll().FirstOrDefaultAsync(x => x.PhoneNo == request.MobileNumber);

                if (existingCustomer != null)
                {
                    // Update details for the customer created during VerifyOTP
                    existingCustomer.Name = request.FullName;
                    existingCustomer.Address = request.Address;
                    existingCustomer.IsMobileVerified = true;
                    existingCustomer.LastLogin = DateTime.UtcNow;

                    repository.Update(existingCustomer);
                    await repository.SaveChangesAsync();

                    // Find linked user or create if not found
                    var user = await _userManager.Users.FirstOrDefaultAsync(u => u.CustomerId == existingCustomer.Id || u.PhoneNumber == existingCustomer.PhoneNo || u.UserName == existingCustomer.PhoneNo);
                    if (user == null)
                    {
                        user = new ApplicationUsers
                        {
                            UserName = existingCustomer.PhoneNo,
                            PhoneNumber = existingCustomer.PhoneNo,
                            CustomerId = existingCustomer.Id,
                            Email = string.IsNullOrEmpty(existingCustomer.Email) ? $"{existingCustomer.PhoneNo}@easybill.com" : existingCustomer.Email,
                            EmailConfirmed = true,
                            PhoneNumberConfirmed = true
                        };
                        var createResult = await _userManager.CreateAsync(user);
                        if (!createResult.Succeeded)
                        {
                            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                            throw new Exception($"Failed to create Identity User: {errors}");
                        }

                        if (!await _roleManager.RoleExistsAsync("Customer"))
                        {
                            await _roleManager.CreateAsync(new IdentityRole("Customer"));
                        }
                        var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
                        if (!roleResult.Succeeded)
                        {
                            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                            throw new Exception($"Failed to assign role Customer: {errors}");
                        }
                    }
                    else if (user.CustomerId == null)
                    {
                        user.CustomerId = existingCustomer.Id;
                        await _userManager.UpdateAsync(user);
                    }

                    return new CreateCustomerResponse
                    {
                        Success = true,
                        Message = "Customer registered successfully",
                        Customer = new CustomerResponse
                        {
                            Id = existingCustomer.Id,
                            FullName = existingCustomer.Name,
                            MobileNumber = existingCustomer.PhoneNo,
                            Email = existingCustomer.Email,
                            Address = existingCustomer.Address,
                            IsMobileVerified = existingCustomer.IsMobileVerified,
                            IdentityUserId = user?.Id
                        }
                    };
                }

                var otpRepo = _unitofwork.GetRepository<CustomerOtp>();
                var otpRecord = await otpRepo.GetAll().Where(o => o.MobileNumber == request.MobileNumber && o.IsUsed == true).OrderByDescending(x => x.Created).FirstOrDefaultAsync();

                var newCustomer = new Customer
                {
                    Name = request.FullName,
                    Address = request.Address,
                    PhoneNo = request.MobileNumber,
                    IsMobileVerified = true,
                    LastLogin = DateTime.UtcNow
                };
                if (otpRecord != null)
                {
                    newCustomer.LastOtp = otpRecord.OtpCode;
                    newCustomer.OtpExpiresAt = otpRecord.ExpiresAt;
                }
                repository.Add(newCustomer);
                await repository.SaveChangesAsync();

                // Create ApplicationUsers record
                var newUser = new ApplicationUsers
                {
                    UserName = newCustomer.PhoneNo,
                    PhoneNumber = newCustomer.PhoneNo,
                    CustomerId = newCustomer.Id,
                    Email = string.IsNullOrEmpty(newCustomer.Email) ? $"{newCustomer.PhoneNo}@easybill.com" : newCustomer.Email,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true
                };
                var createRes = await _userManager.CreateAsync(newUser);
                if (!createRes.Succeeded)
                {
                    var errors = string.Join(", ", createRes.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create Identity User: {errors}");
                }

                if (!await _roleManager.RoleExistsAsync("Customer"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Customer"));
                }
                var roleRes = await _userManager.AddToRoleAsync(newUser, "Customer");
                if (!roleRes.Succeeded)
                {
                    var errors = string.Join(", ", roleRes.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to assign role Customer: {errors}");
                }

                return new CreateCustomerResponse
                {
                    Success = true,
                    Message = "Customer created successfully",
                    Customer = new CustomerResponse
                    {
                        Id = newCustomer.Id,
                        FullName = newCustomer.Name,
                        MobileNumber = newCustomer.PhoneNo,
                        Email = newCustomer.Email,
                        Address = newCustomer.Address,
                        IsMobileVerified = newCustomer.IsMobileVerified,
                        IdentityUserId = newUser.Id
                    }
                };
            }
            catch (Exception ex)
            {
                return new CreateCustomerResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<CreateCustomerResponse> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var repository = _unitofwork.GetRepository<Customer>();
            
            // We use the ID passed in to find the phone number (or we could use the phone from request)
            var referenceCustomer = await repository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == request.Id);

            if (referenceCustomer == null)
            {
                return new CreateCustomerResponse
                {
                    Success = false,
                    Message = "Customer Details not exists"
                };
            }

            var phoneToSync = referenceCustomer.PhoneNo;
            if (string.IsNullOrEmpty(phoneToSync))
                phoneToSync = request.Phone;

            var allShadowAccounts = await repository.Query()
                .IgnoreQueryFilters()
                .Where(x => x.PhoneNo == phoneToSync)
                .ToListAsync();

            foreach (var customer in allShadowAccounts)
            {
                customer.Name = request.Name ?? customer.Name;
                customer.Address = request.Address ?? customer.Address;
                customer.Address2 = request.Address2 ?? customer.Address2;
                customer.Country = request.Country ?? customer.Country;
                customer.State = request.State ?? customer.State;
                customer.City = request.City ?? customer.City;
                customer.PinCode = request.Pin ?? customer.PinCode;
                customer.GSTNo = request.GstNumber ?? customer.GSTNo;
                customer.PhoneNo = request.Phone ?? customer.PhoneNo;
                customer.Email = request.Email ?? customer.Email;
                customer.LastModified = DateTime.UtcNow;
                customer.LastModifiedBy = request.Id.ToString();
                
                repository.Update(customer);
            }

            using (var transaction = repository.BeginTransaction())
            {
                await repository.SaveChangesAsync();
                transaction.Commit();
            }

            return new CreateCustomerResponse
            {
                Success = true,
                Message = "Customer Details Updated successfully across all shops",
                Customer = new CustomerResponse
                {
                    Id = referenceCustomer.Id,
                    FullName = referenceCustomer.Name,
                    MobileNumber = referenceCustomer.PhoneNo,
                    Email = referenceCustomer.Email,
                    Address = referenceCustomer.Address,
                    IsMobileVerified = referenceCustomer.IsMobileVerified
                }
            };
        }

        public async Task<CustomerAddress> AddAddressAsync(CustomerAddress address)
        {
            try
            {
                // If this is the first address or marked as default, we might want to unset other defaults
                var addressRepo = _unitofwork.GetRepository<CustomerAddress>();
                
                if (address.IsDefault)
                {
                    var existingAddresses = await addressRepo.Query()
                        .Where(a => a.CustomerId == address.CustomerId && a.IsDefault)
                        .ToListAsync();
                        
                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                        addressRepo.Update(addr);
                    }
                }

                addressRepo.Add(address);
                using (var transaction = addressRepo.BeginTransaction())
                {
                    await addressRepo.SaveChangesAsync();
                    transaction.Commit();
                }

                return address;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IList<CustomerAddress>> GetAddressesByCustomerIdAsync(int customerId)
        {
            try
            {
                var addressRepo = _unitofwork.GetRepository<CustomerAddress>();
                return await addressRepo.Query()
                    .Where(a => a.CustomerId == customerId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.Created)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<CustomerAddress?> GetAddressByIdAsync(int addressId)
        {
            try
            {
                var addressRepo = _unitofwork.GetRepository<CustomerAddress>();
                return await addressRepo.Query()
                    .FirstOrDefaultAsync(a => a.Id == addressId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<CustomerAddress> UpdateAddressAsync(CustomerAddress address)
        {
            try
            {
                var addressRepo = _unitofwork.GetRepository<CustomerAddress>();
                
                if (address.IsDefault)
                {
                    var existingAddresses = await addressRepo.Query()
                        .Where(a => a.CustomerId == address.CustomerId && a.Id != address.Id && a.IsDefault)
                        .ToListAsync();
                        
                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                        addressRepo.Update(addr);
                    }
                }

                addressRepo.Update(address);
                using (var transaction = addressRepo.BeginTransaction())
                {
                    await addressRepo.SaveChangesAsync();
                    transaction.Commit();
                }
                return address;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> DeleteAddressAsync(int addressId, int customerId)
        {
            try
            {
                var addressRepo = _unitofwork.GetRepository<CustomerAddress>();
                var address = await addressRepo.Query()
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);
                    
                if (address == null) return false;

                addressRepo.Delete(address);
                using (var transaction = addressRepo.BeginTransaction())
                {
                    await addressRepo.SaveChangesAsync();
                    transaction.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> SetDefaultAddressAsync(int addressId, int customerId)
        {
            try
            {
                var addressRepo = _unitofwork.GetRepository<CustomerAddress>();
                var addresses = await addressRepo.Query()
                    .Where(a => a.CustomerId == customerId)
                    .ToListAsync();

                var targetAddress = addresses.FirstOrDefault(a => a.Id == addressId);
                if (targetAddress == null) return false;

                foreach (var addr in addresses)
                {
                    addr.IsDefault = (addr.Id == addressId);
                    addressRepo.Update(addr);
                }

                using (var transaction = addressRepo.BeginTransaction())
                {
                    await addressRepo.SaveChangesAsync();
                    transaction.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Customer> GetOrCreateShadowCustomerAsync(string phoneNumber, string tenantId)
        {
            try
            {
                var repo = _unitofwork.GetRepository<Customer>();
                
                // 1. Try to find if they already exist in the target Tenant
                var existingCustomer = await repo.Query()
                    .IgnoreQueryFilters()
                    .Where(c => c.PhoneNo == phoneNumber && c.TenantId == tenantId)
                    .FirstOrDefaultAsync();

                if (existingCustomer != null)
                    return existingCustomer;

                // 2. They don't exist in this tenant. Find their profile from any other tenant to clone it.
                var anyOtherProfile = await GetCustomerByMobileno(phoneNumber, true);

                var newShadowCustomer = new Customer
                {
                    TenantId = tenantId,
                    PhoneNo = phoneNumber,
                    Name = anyOtherProfile?.Name ?? "App User",
                    Email = anyOtherProfile?.Email,
                    Address = anyOtherProfile?.Address,
                    Address2 = anyOtherProfile?.Address2,
                    City = anyOtherProfile?.City,
                    State = anyOtherProfile?.State,
                    PinCode = anyOtherProfile?.PinCode,
                    Country = anyOtherProfile?.Country,
                    GSTNo = anyOtherProfile?.GSTNo,
                    IsMobileVerified = true,
                    LastLogin = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    CreatedBy = "System_Shadow_Sync"
                };

                return await Create(newShadowCustomer);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<IList<CustomerAddress>> GetAddressesByCustomerPhoneAsync(string phoneNumber)
        {
            try
            {
                var customerRepo = _unitofwork.GetRepository<Customer>();
                var customerIds = await customerRepo.Query()
                    .IgnoreQueryFilters()
                    .Where(c => c.PhoneNo == phoneNumber)
                    .Select(c => c.Id)
                    .ToListAsync();

                var addressRepo = _unitofwork.GetRepository<CustomerAddress>();
                return await addressRepo.Query()
                    .IgnoreQueryFilters()
                    .Where(a => customerIds.Contains(a.CustomerId))
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.Created)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}