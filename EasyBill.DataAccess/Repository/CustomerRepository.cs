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

namespace EasyBill.DataAccess.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public CustomerRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
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
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Customer> GetCustomerByMobileno(string? phoneno)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Customer>();
                var result = await repository.Query().Where(l => l.PhoneNo == phoneno).FirstOrDefaultAsync();
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

                var otpRecord = await otpRepo.Query().Where(o => o.MobileNumber == verifyOtpRequest.MobileNumber && o.OtpCode == verifyOtpRequest.Otp && o.IsUsed == false).OrderByDescending(x => x.Created).FirstOrDefaultAsync();

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

                // Check existing customer
                // var existingCustomer = await customerRepo.Query().FirstOrDefaultAsync(x => x.PhoneNo == verifyOtpRequest.MobileNumber);
                var existingCustomer = await customerRepo.Query().Where(x => x.PhoneNo == verifyOtpRequest.MobileNumber)
                    .Select(x => new Customer
                    {
                        Id = x.Id,
                        Name = x.Name, // safe, never null
                        PhoneNo = x.PhoneNo,
                        Address = x.Address,
                        Email = x.Email,
                        //LastOtp = x.LastOtp,
                        OtpExpiresAt = x.OtpExpiresAt,
                        IsMobileVerified = x.IsMobileVerified
                    })
                    .FirstOrDefaultAsync();


                if (existingCustomer != null)
                {
                    existingCustomer.LastOtp = otpRecord.OtpCode;
                    existingCustomer.OtpExpiresAt = otpRecord.ExpiresAt;
                    existingCustomer.LastLogin = DateTime.UtcNow;
                    existingCustomer.IsMobileVerified = true;

                    customerRepo.Update(existingCustomer);
                    await customerRepo.SaveChangesAsync();


                    return new VerifyOtpResponse
                    {
                        Success = true,
                        IsNewCustomer = false,
                        Customer = new CustomerResponse
                        {
                            Id = existingCustomer.Id,
                            FullName = existingCustomer.Name,
                            MobileNumber = existingCustomer.PhoneNo,
                            Email = existingCustomer.Email,
                            Address = existingCustomer.Address,
                            IsMobileVerified = existingCustomer.IsMobileVerified
                        }
                    };
                }

                // New user
                return new VerifyOtpResponse
                {
                    Success = true,
                    IsNewCustomer = true,
                    Message = "New customer. Please complete registration."
                };
            }
            catch (Exception ex)
            {
                return new VerifyOtpResponse
                {
                    Success = false,
                    Message = "Something went wrong"
                };
            }
        }

        public async Task<CreateCustomerResponse> CreateCustomer(CreateCustomerRequest request)
        {
            try
            {

                var repository = _unitofwork.GetRepository<Customer>();

                var existingCustomer = await repository.Query().FirstOrDefaultAsync(x => x.PhoneNo == request.MobileNumber);

                if (existingCustomer != null)
                {
                    return new CreateCustomerResponse
                    {
                        Success = false,
                        Message = "Customer already exists"
                    };
                }
                var otpRepo = _unitofwork.GetRepository<CustomerOtp>();
                var otpRecord = await otpRepo.Query().Where(o => o.MobileNumber == request.MobileNumber && o.IsUsed == true).OrderByDescending(x => x.Created).FirstOrDefaultAsync();

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
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
                //await repository.SaveChangesAsync();

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
                        IsMobileVerified = newCustomer.IsMobileVerified
                    }
                };
            }
            catch (Exception ex)
            {
                return new CreateCustomerResponse
                {
                    Success = false,
                    Message = "Something went wrong"
                };
            }
        }

        public async Task<CreateCustomerResponse> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var repository = _unitofwork.GetRepository<Customer>();
            var existingCustomer = await repository.Query().FirstOrDefaultAsync(x => x.Id == request.Id);

            if (existingCustomer == null)
            {
                return new CreateCustomerResponse
                {
                    Success = false,
                    Message = "Customer Details not exists"
                };
            }

            existingCustomer.Name = request.Name;
            existingCustomer.Address = request.Address;
            existingCustomer.PhoneNo = request.Phone;
            existingCustomer.Email = request.Email;
            existingCustomer.LastModified = DateTime.UtcNow;
            existingCustomer.LastModifiedBy = request.Id.ToString();
            

            repository.Update(existingCustomer);
            using (var transaction = repository.BeginTransaction())
            {
                await repository.SaveChangesAsync();
                transaction.Commit();
            }
            //await repository.SaveChangesAsync();

            return new CreateCustomerResponse
            {
                Success = true,
                Message = "Customer Details Updated successfully",
                Customer = new CustomerResponse
                {
                    Id = existingCustomer.Id,
                    FullName = existingCustomer.Name,
                    MobileNumber = existingCustomer.PhoneNo,
                    Email = existingCustomer.Email,
                    Address = existingCustomer.Address,
                    IsMobileVerified = existingCustomer.IsMobileVerified
                }
            };
        }

    }


}