using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ICustomerRepository
    {
        Task<IList<Customer>> GetAll();
        Task<Customer> Create(Customer model);
        Task<Customer> GetById(int? Id);
        Task<Customer> GetCustomerByMobileno(string? phoneNo);
        Task<Customer> Update(Customer model);
        Task Delete(Customer model);
        Task<bool> CheckDuplicateAsync(string name, int id);

        Task<CustomerOtp> SendOTP(SendOtpRequest sendOtpRequest);
        Task<VerifyOtpResponse> VerifyOTP(VerifyOtpRequest verifyOtpRequest);
        Task<CreateCustomerResponse> CreateCustomer(CreateCustomerRequest request);
        Task<CreateCustomerResponse> UpdateProfileAsync(UpdateProfileRequest request);
        Task<CustomerAddress> AddAddressAsync(CustomerAddress address);
    }
}
