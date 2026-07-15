using AOneWeb.Service.AuthService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using EasyBill.UI.Service.Loyalty;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using AOne.Utility.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Services.TestResults.WebApi;

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
        private readonly ISalesOrderRepository _salesOrderRepo;
        private readonly IModeOfPaymentRepository _modeOfPaymentRepo;
        private readonly IItemMasterRepository _itemMasterRepo;
        private readonly AOne.DataAccess.Data.ApplicationDbContext _db;

        public CustomerApiController(
            ICustomerRepository customerservice,
            AuthService authService,
            IHttpClientFactory httpClientFactory,
            CustomerLoyaltyService customerLoyaltyService,
            ISalesOrderRepository salesOrderRepo,
            IModeOfPaymentRepository modeOfPaymentRepo,
            IItemMasterRepository itemMasterRepo,
            AOne.DataAccess.Data.ApplicationDbContext db)
        {
            _customerservice = customerservice;
            _authService = authService;
            _httpClient = httpClientFactory.CreateClient();
            _customerLoyaltyService = customerLoyaltyService;
            _salesOrderRepo = salesOrderRepo;
            _modeOfPaymentRepo = modeOfPaymentRepo;
            _itemMasterRepo = itemMasterRepo;
            _db = db;
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

        [AllowAnonymous]
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

        [AllowAnonymous]
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
                if (result.Success && result.Customer != null)
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

        [AllowAnonymous]
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
        [HttpPut("MyProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request data." });

            request.Id = customerId;

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

        [HttpGet("search")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> SearchAddress(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Query is required");

            var url =
                $"https://nominatim.openstreetmap.org/search" +
                $"?format=json&addressdetails=1&limit=5&q={Uri.EscapeDataString(q)}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "EasyBill-WebApp (support@easybill.com)");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return BadRequest("Unable to fetch address");

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }

        // ============================================================================================
        // HELPERS
        // ============================================================================================
        private bool TryGetCustomerId(out int customerId)
        {
            customerId = 0;
            var claim = User.FindFirst("CustomerId")?.Value;
            return int.TryParse(claim, out customerId) && customerId > 0;
        }

        private bool TryGetCustomerPhoneNo(out string phoneNo)
        {
            phoneNo = User.FindFirst("PhoneNumber")?.Value ?? string.Empty;
            return !string.IsNullOrEmpty(phoneNo);
        }

        private static List<object> MapItems(IList<SalesOrderItem>? items)
        {
            if (items == null || !items.Any())
                return new List<object>();

            return items.Select(i => (object)new
            {
                itemId = i.ItemMasterId,
                itemName = i.ItemMaster?.Name ?? string.Empty,
                batch = i.Batch ?? string.Empty,
                qty = i.Qty,
                rate = i.Rate,
                mrp = i.Mrp,
                gst = i.Gst,
                discount = i.Discount,
                amount = i.Amount,
                expiryDate = i.Expirydate
            }).ToList();
        }

        private static List<object> MapPayments(IList<SalsePaymentDetails>? payments)
        {
            if (payments == null || !payments.Any())
                return new List<object>();

            return payments.Select(p => (object)new
            {
                paymentId = p.Id,
                mode = p.ModeOfPayment?.Name ?? "N/A",
                amount = p.Amount,
                referenceNo = p.ReferenceNo ?? string.Empty,
                description = p.Description ?? string.Empty,
                date = p.Date
            }).ToList();
        }

        private static string GetPaymentStatus(decimal paid, decimal total)
        {
            if (paid == 0) return "Unpaid";
            if (paid < total) return "Partial";
            return "Paid";
        }

        // ============================================================================================
        // MERGED APIS FOR MOBILE/WEB CUSTOMERS
        // ============================================================================================

        [HttpGet("MyOrders")]
        public async Task<IActionResult> GetMyOrders()
        {
            if (!TryGetCustomerPhoneNo(out string phoneNo))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            var orders = await _salesOrderRepo.GetByCustomerPhoneNumber(phoneNo);

            if (orders == null || !orders.Any())
                return Ok(new { success = true, message = "No orders found.", count = 0, data = new List<object>() });

            var result = new List<object>();
            foreach (var o in orders.OrderByDescending(o => o.BillDate))
            {
                result.Add(new
                {
                    orderId = o.Id,
                    billNo = o.BillNo,
                    billDate = o.BillDate,
                    address = o.Address ?? string.Empty,
                    mobileNo = o.MobileNo ?? string.Empty,
                    tenantId = o.TenantId ?? string.Empty,
                    subTotal = o.Total,
                    gstAmount = o.TotalGstAmt,
                    discount = o.Totaldiscount,
                    totalPayable = o.TotalPayable,
                    paidAmount = o.PaidAmount,
                    balanceDue = o.Balance,
                    returnAmount = o.ReturnAmount,
                    paymentStatus = GetPaymentStatus(o.PaidAmount, o.TotalPayable),
                    status = o.OrderStatus.ToString(),
                    items = MapItems(o.salesOrderItems),
                    payments = MapPayments(o.SalsePaymentDetails)
                });
            }

            return Ok(new
            {
                success = true,
                phoneNo = phoneNo,
                count = result.Count,
                data = result
            });
        }

        [HttpGet("MyOrders/{orderId}")]
        public async Task<IActionResult> GetMyOrderDetail(int orderId)
        {
            if (!TryGetCustomerPhoneNo(out string phoneNo))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            if (orderId <= 0)
                return BadRequest(new { success = false, message = "Invalid Order ID." });

            var order = await _salesOrderRepo.GetById(orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Order not found." });

            //if (order.Customers == null || order.Customers.PhoneNo != phoneNo)
            //    return Forbid();

            return Ok(new
            {
                success = true,
                data = new
                {
                    orderId = order.Id,
                    billNo = order.BillNo,
                    billDate = order.BillDate,
                    address = order.Address ?? string.Empty,
                    mobileNo = order.MobileNo ?? string.Empty,
                    tenantId = order.TenantId ?? string.Empty,
                    subTotal = order.Total,
                    gstAmount = order.TotalGstAmt,
                    discount = order.Totaldiscount,
                    discountPercent = order.discountPercent,
                    discountAmount = order.discountAmount,
                    totalPayable = order.TotalPayable,
                    paidAmount = order.PaidAmount,
                    balanceDue = order.Balance,
                    returnAmount = order.ReturnAmount,
                    roundOff = order.RoundOffAmount,
                    cessAmount = order.TotalCessAmt,
                    orderStatus = order.OrderStatus.ToString(),
                    paymentStatus = GetPaymentStatus(order.PaidAmount, order.TotalPayable),
                    doctorMobile = order.DoctorMobileNumber ?? string.Empty,
                    doctorRegNo = order.DoctorRegNumber ?? string.Empty,
                    items = MapItems(order.salesOrderItems),
                    payments = MapPayments(order.SalsePaymentDetails)
                }
            });
        }

        [HttpGet("MyProfile")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            var customer = await _customerservice.GetById(customerId);

            if (customer == null)
                return NotFound(new { success = false, message = "Customer profile not found." });

            var addresses = customer.Addresses?.Select(a => (object)new
            {
                addressId = a.Id,
                title = a.Title,
                addressLine = a.AddressLine,
                city = a.City,
                state = a.State,
                pincode = a.Pincode,
                isDefault = a.IsDefault
            }).ToList() ?? new List<object>();

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = customer.Id,
                    name = customer.Name,
                    email = customer.Email ?? string.Empty,
                    phone = customer.PhoneNo ?? string.Empty,
                    address = customer.Address ?? string.Empty,
                    address2 = customer.Address2 ?? string.Empty,
                    country = customer.Country ?? string.Empty,
                    state = customer.State ?? string.Empty,
                    city = customer.City ?? string.Empty,
                    pin = customer.PinCode ?? string.Empty,
                    gstNo = customer.GSTNo ?? string.Empty,
                    status = customer.Status.ToString(),
                    isMobileVerified = customer.IsMobileVerified,
                    lastLogin = customer.LastLogin,
                    memberSince = customer.Created,
                    addresses = addresses
                }
            });
        }

        [HttpGet("MyPayments")]
        public async Task<IActionResult> GetMyPayments()
        {
            if (!TryGetCustomerPhoneNo(out string phoneNo))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            var orders = await _salesOrderRepo.GetByCustomerPhoneNumberWithPayments(phoneNo);

            if (orders == null || !orders.Any())
                return Ok(new { success = true, message = "No payment records found.", count = 0, data = new List<object>() });

            var result = new List<object>();
            foreach (var o in orders.OrderByDescending(o => o.BillDate))
            {
                var paymentDetails = (o.SalsePaymentDetails ?? new List<SalsePaymentDetails>())
                    .Select(p => new
                    {
                        paymentId = p.Id,
                        modeName = p.ModeOfPayment?.Name ?? "N/A",
                        amount = p.Amount,
                        referenceNo = p.ReferenceNo ?? string.Empty,
                        description = p.Description ?? string.Empty,
                        paymentDate = p.Date
                    }).ToList();

                result.Add(new
                {
                    orderId = o.Id,
                    billNo = o.BillNo,
                    billDate = o.BillDate,
                    tenantId = o.TenantId ?? string.Empty,
                    totalPayable = o.TotalPayable,
                    totalPaid = o.PaidAmount,
                    balanceDue = o.Balance,
                    paymentStatus = GetPaymentStatus(o.PaidAmount, o.TotalPayable),
                    paymentDetails = paymentDetails
                });
            }

            var totalPaidOverall = orders.Sum(o => o.PaidAmount);
            var totalBalanceOverall = orders.Sum(o => o.Balance);

            return Ok(new
            {
                success = true,
                phoneNo = phoneNo,
                summary = new
                {
                    totalOrders = orders.Count,
                    totalPaid = totalPaidOverall,
                    totalBalance = totalBalanceOverall,
                    fullyPaidOrders = orders.Count(o => o.Balance == 0 && o.PaidAmount > 0),
                    pendingOrders = orders.Count(o => o.Balance > 0)
                },
                data = result
            });
        }

        [HttpPost("CancelOrder/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest? request)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            if (orderId <= 0)
                return BadRequest(new { success = false, message = "Invalid Order ID." });

            var order = await _salesOrderRepo.GetById(orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Order not found." });

            if (order.CustomerId != customerId)
                return Forbid();

            if (order.Deleted != null)
                return BadRequest(new { success = false, message = "This order is already cancelled." });

            if (order.OrderStatus != OrderStatus.Ordered)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Order cannot be cancelled because it is already {order.OrderStatus}."
                });
            }

            if (order.PaidAmount > 0 && order.PaidAmount >= order.TotalPayable)
                return BadRequest(new
                {
                    success = false,
                    message = "Fully paid orders cannot be cancelled. Please contact support for refund."
                });

            var reason = request?.Reason ?? "No reason provided";

            var cancelled = await _salesOrderRepo.CancelOrder(orderId, customerId.ToString(), reason);

            if (!cancelled)
                return BadRequest(new { success = false, message = "Order could not be cancelled. Please try again." });

            return Ok(new
            {
                success = true,
                message = "Order cancelled successfully.",
                orderId = orderId,
                billNo = order.BillNo,
                cancelledAt = DateTime.UtcNow
            });
        }

        [HttpPost("AddAddress")]
        public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request)
        {
            if (!TryGetCustomerPhoneNo(out string phoneNo))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            if (request == null || string.IsNullOrWhiteSpace(request.AddressLine))
                return BadRequest(new { success = false, message = "Address line is required." });

            // Add address to the primary/first shadow account to avoid DB duplication
            var primaryCustomer = await _customerservice.GetCustomerByMobileno(phoneNo, true);
            if (primaryCustomer == null)
                return BadRequest(new { success = false, message = "Customer profile not initialized." });

            var newAddress = new CustomerAddress
            {
                CustomerId = primaryCustomer.Id,
                Title = request.Title,
                AddressLine = request.AddressLine,
                City = request.City,
                State = request.State,
                Pincode = request.Pincode,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsDefault = request.IsDefault
            };

            await _customerservice.AddAddressAsync(newAddress);

            return Ok(new
            {
                success = true,
                message = "Address added successfully.",
                addressId = newAddress.Id
            });
        }

        [HttpGet("MyAddresses")]
        public async Task<IActionResult> GetMyAddresses()
        {
            if (!TryGetCustomerPhoneNo(out string phoneNo))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            var addresses = await _customerservice.GetAddressesByCustomerPhoneAsync(phoneNo);

            var result = addresses.Select(a => new
            {
                addressId = a.Id,
                title = a.Title,
                addressLine = a.AddressLine,
                city = a.City,
                state = a.State,
                pincode = a.Pincode,
                latitude = a.Latitude,
                longitude = a.Longitude,
                isDefault = a.IsDefault
            }).ToList();

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }

        [HttpPut("UpdateAddress")]
        public async Task<IActionResult> UpdateAddress([FromBody] UpdateAddressRequest request)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            if (request == null || request.Id <= 0 || string.IsNullOrWhiteSpace(request.AddressLine))
                return BadRequest(new { success = false, message = "Address ID and Address Line are required." });

            var address = await _customerservice.GetAddressByIdAsync(request.Id);
            if (address == null || address.CustomerId != customerId)
                return NotFound(new { success = false, message = "Address not found." });

            address.Title = request.Title;
            address.AddressLine = request.AddressLine;
            address.City = request.City;
            address.State = request.State;
            address.Pincode = request.Pincode;
            address.IsDefault = request.IsDefault;

            await _customerservice.UpdateAddressAsync(address);

            return Ok(new
            {
                success = true,
                message = "Address updated successfully.",
                data = new
                {
                    addressId = address.Id,
                    title = address.Title,
                    addressLine = address.AddressLine,
                    city = address.City,
                    state = address.State,
                    pincode = address.Pincode,
                    isDefault = address.IsDefault
                }
            });
        }

        [HttpDelete("DeleteAddress/{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid Address ID." });

            var deleted = await _customerservice.DeleteAddressAsync(id, customerId);
            if (!deleted)
                return NotFound(new { success = false, message = "Address not found or access denied." });

            return Ok(new
            {
                success = true,
                message = "Address deleted successfully."
            });
        }

        [HttpPost("SetDefaultAddress/{id}")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid Address ID." });

            var success = await _customerservice.SetDefaultAddressAsync(id, customerId);
            if (!success)
                return NotFound(new { success = false, message = "Address not found or access denied." });

            return Ok(new
            {
                success = true,
                message = "Default address set successfully."
            });
        }

        [HttpGet("TrackOrder/{orderId}")]
        public async Task<IActionResult> TrackOrder(int orderId)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            var order = await _salesOrderRepo.GetById(orderId);
            if (order == null || order.CustomerId != customerId)
                return NotFound(new { success = false, message = "Order not found or access denied." });

            var currentStatus = order.Deleted != null ? OrderStatus.Cancelled.ToString() : order.OrderStatus.ToString();

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                billNo = order.BillNo ?? "Pending",
                status = currentStatus,
                orderedOn = order.Created ?? order.BillDate,
                expectedDelivery = order.BillDate?.AddDays(3)
            });
        }

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!TryGetCustomerPhoneNo(out string phoneNo))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            if (string.IsNullOrWhiteSpace(request.TenantId))
                return BadRequest(new { success = false, message = "TenantId is required to place an order." });

            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest(new { success = false, message = "Order must contain at least one item." });

            if (string.IsNullOrWhiteSpace(request.OrderAddress))
                return BadRequest(new { success = false, message = "Order address is required." });

            decimal totalAmount = 0;
            decimal totalGstAmount = 0;
            decimal totalDiscount = 0;
            var orderItems = new List<SalesOrderItem>();

            foreach (var reqItem in request.Items)
            {
                var itemMaster = await _itemMasterRepo.GetByItemMasterId(reqItem.ItemId);
                if (itemMaster == null)
                    return BadRequest(new { success = false, message = $"Item with ID {reqItem.ItemId} not found." });

                decimal rate = reqItem.Rate;
                decimal qty = reqItem.Qty;
                decimal amount = rate * qty;
                decimal gstPercent = reqItem.GstPercent;
                decimal gstAmount = reqItem.GstAmount;
                decimal discountPercent = reqItem.Discount;
                decimal discountAmount = reqItem.DiscountAmount;

                totalAmount += amount;
                totalGstAmount += gstAmount;
                totalDiscount += discountAmount;

                orderItems.Add(new SalesOrderItem
                {
                    ItemMasterId = itemMaster.Id,
                    Qty = qty,
                    Rate = rate,
                    Gst = gstPercent,
                    Discount = discountPercent,
                    Amount = amount
                });
            }

            var shadowCustomer = await _customerservice.GetOrCreateShadowCustomerAsync(phoneNo, request.TenantId);
            if (shadowCustomer == null)
                return StatusCode(500, new { success = false, message = "Failed to synchronize customer identity." });

            int customerId = shadowCustomer.Id;

            var paymentDetails = new List<SalsePaymentDetails>();
            decimal paidAmount = 0;
            if (request.PaymentDetails != null && request.PaymentDetails.Any())
            {
                foreach (var payment in request.PaymentDetails)
                {
                    paymentDetails.Add(new SalsePaymentDetails
                    {
                        PaymentModeId = payment.PaymentModeId,
                        Amount = payment.Amount,
                        Description = payment.Description,
                        Created = DateTime.UtcNow,
                        CreatedBy = customerId.ToString()
                    });
                    paidAmount += payment.Amount;
                }
            }

            var newOrder = new SalesOrder
            {
                CustomerId = customerId,
                TenantId = request.TenantId,
                BillNo = request.OrderNumber,
                BillDate = request.OrderDate ?? DateTime.UtcNow,
                Address = request.OrderAddress,
                MobileNo = request.MobileNo,
                Total = totalAmount,
                TotalGstAmt = totalGstAmount,
                Totaldiscount = totalDiscount,
                discountAmount = totalDiscount,
                TotalPayable = request.TotalPayableAmount,
                Balance = request.TotalPayableAmount - paidAmount,
                PaidAmount = paidAmount,
                OrderStatus = OrderStatus.Ordered,
                salesOrderItems = orderItems,
                SalsePaymentDetails = paymentDetails.Any() ? paymentDetails : null,
                Created = DateTime.UtcNow,
                CreatedBy = customerId.ToString()
            };

            var createdOrder = await _salesOrderRepo.Create(newOrder);

            return Ok(new
            {
                success = true,
                message = "Order placed successfully.",
                orderId = createdOrder.Id,
                totalPayable = createdOrder.TotalPayable,
                status = createdOrder.OrderStatus.ToString()
            });
        }

        // ============================================================================================
        // SHOPPING: TENANTS & STOCK
        // ============================================================================================

        [HttpGet("nearby-tenants")]
        public async Task<IActionResult> GetNearbyTenants(double customerLat, double customerLng, double radiusInKm = 10)
        {
            var tenants = await _db.Tenants
                .Where(t => t.Latitude != null && t.Longitude != null)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Address1,
                    t.Address2,
                    t.Location,
                    t.Latitude,
                    t.Longitude,
                    t.Logo
                })
                .ToListAsync();

            var nearbyTenants = new List<NearbyTenantResponse>();
            foreach (var t in tenants)
            {
                double distance = CalculateDistance(customerLat, customerLng, (double)t.Latitude, (double)t.Longitude);
                if (distance <= radiusInKm)
                {
                    nearbyTenants.Add(new NearbyTenantResponse
                    {
                        TenantId = t.Id,
                        Name = t.Name ?? "Unknown Shop",
                        Address = string.Join(", ", new[] { t.Address1, t.Address2 }.Where(x => !string.IsNullOrEmpty(x))),
                        Location = t.Location,
                        Latitude = t.Latitude,
                        Longitude = t.Longitude,
                        Logo = t.Logo,
                        DistanceMeters = distance * 1000,
                        DistanceText = (distance < 1) ? $"{Math.Round(distance * 1000)} m away" : $"{Math.Round(distance, 1)} km away"
                    });
                }
            }

            return Ok(new { success = true, data = nearbyTenants.OrderBy(x => x.DistanceMeters) });
        }

        [HttpGet("tenants/{tenantId}/stock")]
        public async Task<IActionResult> GetTenantStock(string tenantId)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Fetch items that belong to the tenant
            var items = await _db.ItemMasters
                .Include(i => i.Category)
                .Include(i => i.SubCategory)
                .Include(i => i.ItemImages)
                .Where(i => i.TenantId == tenantId && i.IsActive)
                .ToListAsync();

            // Fetch Current Stock for this tenant
            var stocks = await _db.CurrentStocks
                .Where(s => s.TenantId == tenantId)
                .ToListAsync();

            var groupedResult = items
                .GroupBy(i => i.Category)
                .Select(catGroup => new TenantStockCategoryResponse
                {
                    CategoryId = catGroup.Key?.Id ?? 0,
                    CategoryName = catGroup.Key?.CategoryName ?? "Uncategorized",

                    SubCategories = catGroup
                        .GroupBy(i => i.SubCategory)
                        .Select(subGroup => new TenantStockSubCategoryResponse
                        {
                            SubCategoryId = subGroup.Key?.Id ?? 0,
                            SubCategoryName = subGroup.Key?.Name ?? "General",

                            Items = subGroup.Select(i =>
                            {
                                var stock = stocks
                                    .Where(s => s.ItemId == i.Id)
                                    .OrderBy(s => s.ExpiryDate)
                                    .FirstOrDefault();

                                return new TenantStockItemResponse
                                {
                                    ItemId = i.Id,
                                    ItemName = i.Name,
                                    ItemCode = i.Code,

                                    // Stock Details
                                    Qty = stock?.Qty ?? 0,
                                    TotalStockQuantity = stocks
                                        .Where(s => s.ItemId == i.Id)
                                        .Sum(s => s.Qty),
                                    Batch = stock?.Batch,
                                    ExpiryDate = stock?.ExpiryDate,
                                    Barcode = stock?.Barcode,
                                    Mrp = stock?.Mrp ?? 0,
                                    PurchaseRate = stock?.PurchaseRate ?? 0,

                                    // Item Details
                                    SalesRate = i.SalesRate1,
                                    SalesRate1 = i.SalesRate1,
                                    SalesRate2 = i.SalesRate2,
                                    Unit = i.Unit1,
                                    MinimumQty = i.MinimumQty,
                                    MaximumQty = i.MaximumQty,
                                    MaximumDiscount = i.MaximumDiscount,
                                    DecemalAllowed = i.DecemalAllowed,
                                    Conversion = i.Conversion,
                                    Hsn = i.Hsn,

                                    // Images
                                    ImageUrl = i.ItemImages
                                        .Where(x => !x.IsDeleted)
                                        .OrderBy(x => x.SortOrder)
                                        .Select(x => $"{baseUrl}{x.ImagePath}")
                                        .ToList()
                                };
                            }).ToList()
                        }).ToList()
                }).ToList();

            return Ok(new
            {
                success = true,
                data = groupedResult
            });
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Radius of the earth in km
            var dLat = Deg2Rad(lat2 - lat1);
            var dLon = Deg2Rad(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; 
        }

        private double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}
