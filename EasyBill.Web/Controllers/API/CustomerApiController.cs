using AOneWeb.Service.AuthService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.Loyalty;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerApiController : ControllerBase
    {
        private readonly ICustomerRepository _customerservice;
        private readonly AuthService _authService;
        private readonly HttpClient _httpClient;
        private readonly CustomerLoyaltyService _customerLoyaltyService;

        public CustomerApiController(
            ICustomerRepository customerservice,
            AuthService authService,
            IHttpClientFactory httpClientFactory,
            CustomerLoyaltyService customerLoyaltyService)
        {
            _customerservice = customerservice;
            _authService = authService;
            _httpClient = httpClientFactory.CreateClient();
            _customerLoyaltyService = customerLoyaltyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _customerservice.GetAll();
            return Ok(new { success = true, data });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var model = await _customerservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Customer not found" });

            return Ok(new { success = true, data = model });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerVM Vm)
        {
            if (Vm == null)
                return BadRequest(new { success = false, message = "Invalid data." });

            var model = new Customer
            {
                Name = Vm.Name,
                Address = Vm.Address,
                PhoneNo = Vm.PhoneNo,
                Email = Vm.Email,
            };

            await _customerservice.Create(model);
            return Ok(new { success = true, message = "Customer created successfully.", data = model });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] CustomerVM Vm)
        {
            var model = await _customerservice.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Customer not found" });

            model.Name = Vm.Name;
            model.Address = Vm.Address;
            model.PhoneNo = Vm.PhoneNo;
            model.Email = Vm.Email;

            await _customerservice.Update(model);
            return Ok(new { success = true, message = "Customer updated successfully.", data = model });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { success = false, message = "Invalid Id for deletion." });

                var model = await _customerservice.GetById(id);
                if (model == null)
                    return NotFound(new { success = false, message = "Customer not found." });

                await _customerservice.Delete(model);
                return Ok(new { success = true, message = "Customer deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("{id}/Loyalty")]
        public async Task<IActionResult> GetCustomerLoyalty(int id)
        {
            var report = await _customerLoyaltyService.GetCustomerLoyaltyReportAsync(id);
            if (report == null)
            {
                return NotFound(new { success = false, message = "Customer not found." });
            }

            return Ok(new { success = true, data = report });
        }

        [HttpGet("MyLoyalty")]
        public async Task<IActionResult> GetMyLoyalty()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (!int.TryParse(customerIdClaim, out var customerId) || customerId <= 0)
            {
                return Unauthorized(new { success = false, message = "Customer token is invalid." });
            }

            var report = await _customerLoyaltyService.GetCustomerLoyaltyReportAsync(customerId);
            if (report == null)
            {
                return NotFound(new { success = false, message = "Customer not found." });
            }

            return Ok(new { success = true, data = report });
        }


        //============for app================

        [HttpPost("SendOTP")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            if (string.IsNullOrEmpty(request.MobileNumber) || request.MobileNumber.Length != 10)
                return BadRequest(new { success = false, message = "Invalid mobile number" });
            try
            {
                var otpModel = await _customerservice.SendOTP(request);
                if (otpModel == null)
                    return BadRequest(new { success = false, message = "OTP not sent" });
                return Ok(new
                {
                    success = true,
                    message = "OTP sent successfully.",
                    otp = otpModel.OtpCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("VerifyOTP")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOtpRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request" });

            if (string.IsNullOrEmpty(request.MobileNumber) || request.MobileNumber.Length != 10)
                return BadRequest(new { success = false, message = "Invalid mobile number" });

            if (string.IsNullOrEmpty(request.Otp) || request.Otp.Length != 4)
                return BadRequest(new { success = false, message = "Invalid OTP" });

            try
            {
                var result = await _customerservice.VerifyOTP(request);

                if (!result.Success)
                {
                    return Ok(new VerifyOtpResponse
                    {
                        Success = false,
                        Message = result.Message,
                        IsNewCustomer = false,
                        Customer = result.Customer
                    });
                    //return Ok(new
                    //{
                    //    success = false,
                    //    message = result.Message,
                    //    isNewCustomer = false
                    //});
                }
                if(result.Success && result.Customer!=null)
                {
                    var tokenResponse = _authService.GenerateCustomerToken(result.Customer);
                    result.Token = tokenResponse.Token;
                }

                return Ok(new VerifyOtpResponse
                {
                    Success = true,
                    Message = result.Message,
                    IsNewCustomer = result.IsNewCustomer,
                    Token = result.Token,
                    Customer = result.Customer
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Something went wrong. Please try again."
                });
            }
        }

        [HttpPost("CreateCustomer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.MobileNumber))
                    return BadRequest(new CreateCustomerResponse
                    {
                        Success = false,
                        Message = "Mobile number required"
                    });

                if (string.IsNullOrEmpty(request.FullName))
                    return BadRequest(new CreateCustomerResponse
                    {
                        Success = false,
                        Message = "Full name required"
                    });

                var result = await _customerservice.CreateCustomer(request);
                if (result.Success)
                {
                    var tokenResponse = _authService.GenerateCustomerToken(result.Customer);
                    result.Token = tokenResponse.Token;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Something went wrong. Please try again."
                });
            }
        }
        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var result = await _customerservice.UpdateProfileAsync(request);
                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    customer = result.Customer == null ? null : new
                    {
                        id = result.Customer.Id,
                        fullName = result.Customer.FullName,
                        email = result.Customer.Email,
                        mobileNumber = result.Customer.MobileNumber,
                        address = result.Customer.Address
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Something went wrong. Please try again."
                });
            }
        }
        [HttpGet("reverse")]
        public async Task<IActionResult> ReverseGeocode(double lat, double lng)
        {
            var url =
                $"https://nominatim.openstreetmap.org/reverse" +
                $"?format=json&addressdetails=1&lat={lat}&lon={lng}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "EasyBill-WebApp (support@easybill.com)");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return BadRequest("Unable to fetch address");

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

    }
}
