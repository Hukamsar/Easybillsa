using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    /// <summary>
    /// ✅ CUSTOMER-FACING API CONTROLLER
    /// ====================================
    /// Ye controller sirf mobile/web app ke customer users ke liye hai.
    /// Har endpoint Customer JWT token se secure hai.
    /// Admin/Tenant APIs alag controllers mein hain — ye controller unhe touch nahi karta.
    ///
    /// Included APIs:
    /// 1. GET  /api/CustomerOrderApi/MyOrders        → Customer ki poori order history (sab tenants se)
    /// 2. GET  /api/CustomerOrderApi/MyOrders/{id}   → Kisi ek order ki detail
    /// 3. GET  /api/CustomerOrderApi/MyProfile       → Customer ka apna profile (token se)
    /// 4. PUT  /api/CustomerOrderApi/MyProfile       → Customer apna profile update kare
    /// 5. GET  /api/CustomerOrderApi/MyPayments      → Customer ke sab payments ka detail (mode of payment + amounts)
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerOrderApiController : ControllerBase
    {
        private readonly ISalesOrderRepository _salesOrderRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IModeOfPaymentRepository _modeOfPaymentRepo;
        private readonly IItemMasterRepository _itemMasterRepo;

        public CustomerOrderApiController(
            ISalesOrderRepository salesOrderRepo,
            ICustomerRepository customerRepo,
            IModeOfPaymentRepository modeOfPaymentRepo,
            IItemMasterRepository itemMasterRepo)
        {
            _salesOrderRepo = salesOrderRepo;
            _customerRepo = customerRepo;
            _modeOfPaymentRepo = modeOfPaymentRepo;
            _itemMasterRepo = itemMasterRepo;
        }

        // ============================================================================================
        // HELPER: JWT Token se CustomerId nikalna
        // Customer token mein "CustomerId" claim hota hai (AuthService.GenerateCustomerToken se set)
        // ============================================================================================
        private bool TryGetCustomerId(out int customerId)
        {
            customerId = 0;
            var claim = User.FindFirst("CustomerId")?.Value;
            return int.TryParse(claim, out customerId) && customerId > 0;
        }

        // ============================================================================================
        // HELPER: SalesOrderItem list → clean response list
        // ============================================================================================
        private static List<object> MapItems(IList<SalesOrderItem>? items)
        {
            if (items == null || !items.Any())
                return new List<object>();

            return items.Select(i => (object)new
            {
                itemId     = i.ItemMasterId,
                itemName   = i.ItemMaster?.Name ?? string.Empty,
                batch      = i.Batch ?? string.Empty,
                qty        = i.Qty,
                rate       = i.Rate,
                mrp        = i.Mrp,
                gst        = i.Gst,
                discount   = i.Discount,
                amount     = i.Amount,
                expiryDate = i.Expirydate
            }).ToList();
        }

        // ============================================================================================
        // HELPER: SalsePaymentDetails list → clean response list
        // ============================================================================================
        private static List<object> MapPayments(IList<SalsePaymentDetails>? payments)
        {
            if (payments == null || !payments.Any())
                return new List<object>();

            return payments.Select(p => (object)new
            {
                paymentId   = p.Id,
                mode        = p.ModeOfPayment?.Name ?? "N/A",
                amount      = p.Amount,
                referenceNo = p.ReferenceNo ?? string.Empty,
                description = p.Description ?? string.Empty,
                date        = p.Date
            }).ToList();
        }

        // ============================================================================================
        // HELPER: Payment status string
        // ============================================================================================
        private static string GetPaymentStatus(decimal paid, decimal total)
        {
            if (paid == 0) return "Unpaid";
            if (paid < total) return "Partial";
            return "Paid";
        }

        // ============================================================================================
        // API 1: MY ORDERS — Customer ki poori order history
        // ============================================================================================
        /// <summary>
        /// GET /api/CustomerOrderApi/MyOrders
        ///
        /// PURPOSE: Customer apni poori order history dekh sakta hai.
        ///          Ek customer kai alag-alag companies/tenants se order kar sakta hai,
        ///          isliye response mein TenantId bhi include kiya gaya hai taaki
        ///          frontend group kar sake "kis company se kya order kiya".
        ///
        /// AUTH: Customer JWT Token required (Role = "Customer")
        /// EXISTING IMPACT: SalesOrderApiController ka koi bhi endpoint NAHI chhua.
        /// </summary>
        [HttpGet("MyOrders")]
        public async Task<IActionResult> GetMyOrders()
        {
            // Step 1: Token se CustomerId nikalo
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            // Step 2: Is customer ke sab orders laao (ISalesOrderRepository.GetByCustomerId — already exists)
            var orders = await _salesOrderRepo.GetByCustomerId(customerId);

            if (orders == null || !orders.Any())
                return Ok(new { success = true, message = "No orders found.", count = 0, data = new List<object>() });

            // Step 3: Response shape karo — TenantId include hai taaki customer dekh sake
            //         "kis tenant (company) se ye order place ki thi"
            var result = new List<object>();
            foreach (var o in orders.OrderByDescending(o => o.BillDate))
            {
                result.Add(new
                {
                    // Order Basic Info
                    orderId       = o.Id,
                    billNo        = o.BillNo,
                    billDate      = o.BillDate,
                    address       = o.Address ?? string.Empty,
                    mobileNo      = o.MobileNo ?? string.Empty,

                    // 🏢 Tenant Info — Kis company se order tha (multi-tenant support)
                    tenantId      = o.TenantId ?? string.Empty,

                    // Amount Breakdown
                    subTotal      = o.Total,
                    gstAmount     = o.TotalGstAmt,
                    discount      = o.Totaldiscount,
                    totalPayable  = o.TotalPayable,
                    paidAmount    = o.PaidAmount,
                    balanceDue    = o.Balance,
                    returnAmount  = o.ReturnAmount,

                    // Order Status (based on payment)
                    paymentStatus = GetPaymentStatus(o.PaidAmount, o.TotalPayable),

                    // Ordered Items
                    items         = MapItems(o.salesOrderItems),

                    // Payment Details
                    payments      = MapPayments(o.SalsePaymentDetails)
                });
            }

            return Ok(new
            {
                success    = true,
                customerId = customerId,
                count      = result.Count,
                data       = result
            });
        }

        // ============================================================================================
        // API 2: MY ORDER DETAIL — Kisi ek order ki poori detail
        // ============================================================================================
        /// <summary>
        /// GET /api/CustomerOrderApi/MyOrders/{orderId}
        ///
        /// PURPOSE: Ek specific order ki detail dekhna (items, payments, amount breakdown).
        ///          Security check: customer sirf apna order dekh sakta hai, doosre ka nahi.
        ///
        /// AUTH: Customer JWT Token required
        /// EXISTING IMPACT: Kuch nahi chhua.
        /// </summary>
        [HttpGet("MyOrders/{orderId}")]
        public async Task<IActionResult> GetMyOrderDetail(int orderId)
        {
            // Step 1: Token validate
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            if (orderId <= 0)
                return BadRequest(new { success = false, message = "Invalid Order ID." });

            // Step 2: Order fetch karo
            var order = await _salesOrderRepo.GetById(orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Order not found." });

            // Step 3: Security — Customer sirf apna hi order dekh sakta hai
            if (order.CustomerId != customerId)
                return Forbid(); // 403 — doosre customer ka order access karne ki koshish

            // Step 4: Detailed response
            return Ok(new
            {
                success = true,
                data = new
                {
                    orderId         = order.Id,
                    billNo          = order.BillNo,
                    billDate        = order.BillDate,
                    address         = order.Address ?? string.Empty,
                    mobileNo        = order.MobileNo ?? string.Empty,

                    // 🏢 Tenant Info
                    tenantId        = order.TenantId ?? string.Empty,

                    // Amount breakdown
                    subTotal        = order.Total,
                    gstAmount       = order.TotalGstAmt,
                    discount        = order.Totaldiscount,
                    discountPercent = order.discountPercent,
                    discountAmount  = order.discountAmount,
                    totalPayable    = order.TotalPayable,
                    paidAmount      = order.PaidAmount,
                    balanceDue      = order.Balance,
                    returnAmount    = order.ReturnAmount,
                    roundOff        = order.RoundOffAmount,
                    cessAmount      = order.TotalCessAmt,

                    paymentStatus   = GetPaymentStatus(order.PaidAmount, order.TotalPayable),

                    // Doctor info (for pharmacy orders)
                    doctorMobile    = order.DoctorMobileNumber ?? string.Empty,
                    doctorRegNo     = order.DoctorRegNumber ?? string.Empty,

                    // Items
                    items           = MapItems(order.salesOrderItems),

                    // Payment breakdown
                    payments        = MapPayments(order.SalsePaymentDetails)
                }
            });
        }

        // ============================================================================================
        // API 3: MY PROFILE — Customer apna profile dekhe (token se, Id ki zaroorat nahi)
        // ============================================================================================
        /// <summary>
        /// GET /api/CustomerOrderApi/MyProfile
        ///
        /// PURPOSE: Customer apna profile dekh sakta hai sirf apne JWT token se.
        ///          UserId/CustomerId alag se bhejne ki zaroorat nahi — token se automatically milta hai.
        ///
        /// AUTH: Customer JWT Token required
        /// EXISTING IMPACT: CustomerApiController ka GetById endpoint alag hai — ye naya hai sirf customer ke liye.
        /// </summary>
        [HttpGet("MyProfile")]
        public async Task<IActionResult> GetMyProfile()
        {
            // Step 1: Token se CustomerId nikalo
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            // Step 2: Customer fetch karo DB se
            var customer = await _customerRepo.GetById(customerId);

            if (customer == null)
                return NotFound(new { success = false, message = "Customer profile not found." });

            // Step 3: Clean response — sensitive fields (LastOtp, OtpExpiresAt) expose nahi hote
            return Ok(new
            {
                success = true,
                data = new
                {
                    id               = customer.Id,
                    name             = customer.Name,
                    email            = customer.Email ?? string.Empty,
                    phone            = customer.PhoneNo ?? string.Empty,
                    address          = customer.Address ?? string.Empty,
                    address2         = customer.Address2 ?? string.Empty,
                    country          = customer.Country ?? string.Empty,
                    state            = customer.State ?? string.Empty,
                    city             = customer.City ?? string.Empty,
                    pin              = customer.PinCode ?? string.Empty,
                    gstNo            = customer.GSTNo ?? string.Empty,
                    status           = customer.Status.ToString(),
                    isMobileVerified = customer.IsMobileVerified,
                    lastLogin        = customer.LastLogin,
                    memberSince      = customer.Created  // BaseEntity se aata hai
                }
            });
        }

        // ============================================================================================
        // API 4: UPDATE MY PROFILE — Customer apna profile update kare
        // ============================================================================================
        /// <summary>
        /// PUT /api/CustomerOrderApi/MyProfile
        ///
        /// PURPOSE: Customer apna naam, email, phone, address update kare.
        ///          CustomerId request body se nahi, token se liya jata hai — security ke liye.
        ///
        /// AUTH: Customer JWT Token required
        /// EXISTING IMPACT: CustomerApiController ka UpdateProfile admin ke liye hai.
        ///                  Ye customer ke liye alag secure version hai.
        /// </summary>
        [HttpPut("MyProfile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            // Step 1: Token se CustomerId nikalo
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            if (request == null)
                return BadRequest(new { success = false, message = "Invalid request data." });

            // Step 2: Body ka Id override karo token se — customer sirf apna data update kar sake
            request.Id = customerId;

            // Step 3: Existing repository method use karo (CustomerRepository.UpdateProfileAsync)
            var result = await _customerRepo.UpdateProfileAsync(request);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Customer == null ? null : new
                {
                    id      = result.Customer.Id,
                    name    = result.Customer.FullName,
                    email   = result.Customer.Email,
                    phone   = result.Customer.MobileNumber,
                    address = result.Customer.Address
                }
            });
        }

        // ============================================================================================
        // API 5: MY PAYMENTS — Customer ke sab payments ki detail
        // ============================================================================================
        /// <summary>
        /// GET /api/CustomerOrderApi/MyPayments
        ///
        /// PURPOSE: Customer dekh sake ki usne kaunse orders ke liye kitna payment kiya,
        ///          kis mode (Cash/UPI/Card/etc.) se kiya, aur koi balance remaining hai kya.
        ///          Ye payment ki READ-ONLY history hai — koi financial change nahi karta.
        ///
        /// AUTH: Customer JWT Token required
        /// EXISTING IMPACT: Kuch nahi chhua. ReceivePaymentApiController/PaymentVoucherApiController untouched.
        /// </summary>
        [HttpGet("MyPayments")]
        public async Task<IActionResult> GetMyPayments()
        {
            // Step 1: Token validate
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            // Step 2: Customer ke sab orders laao (payments include hain — GetByCustomerIdWithPayments)
            var orders = await _salesOrderRepo.GetByCustomerIdWithPayments(customerId);

            if (orders == null || !orders.Any())
                return Ok(new { success = true, message = "No payment records found.", count = 0, data = new List<object>() });

            // Step 3: Har order ke payments compile karke dikhao
            var result = new List<object>();
            foreach (var o in orders.OrderByDescending(o => o.BillDate))
            {
                // Payment details — Mode, amount, date
                var paymentDetails = (o.SalsePaymentDetails ?? new List<SalsePaymentDetails>())
                    .Select(p => new
                    {
                        paymentId   = p.Id,
                        modeName    = p.ModeOfPayment?.Name ?? "N/A",  // Cash / UPI / Card etc.
                        amount      = p.Amount,
                        referenceNo = p.ReferenceNo ?? string.Empty,
                        description = p.Description ?? string.Empty,
                        paymentDate = p.Date
                    }).ToList();

                result.Add(new
                {
                    // Order reference
                    orderId       = o.Id,
                    billNo        = o.BillNo,
                    billDate      = o.BillDate,

                    // 🏢 Kis company ka order (multi-tenant)
                    tenantId      = o.TenantId ?? string.Empty,

                    // Amount summary
                    totalPayable  = o.TotalPayable,
                    totalPaid     = o.PaidAmount,
                    balanceDue    = o.Balance,
                    paymentStatus = GetPaymentStatus(o.PaidAmount, o.TotalPayable),

                    // Payment breakdown per order
                    paymentDetails = paymentDetails
                });
            }

            // Step 4: Summary bhi do — Total paid, total balance
            var totalPaidOverall    = orders.Sum(o => o.PaidAmount);
            var totalBalanceOverall = orders.Sum(o => o.Balance);

            return Ok(new
            {
                success    = true,
                customerId = customerId,
                summary = new
                {
                    totalOrders     = orders.Count,
                    totalPaid       = totalPaidOverall,
                    totalBalance    = totalBalanceOverall,
                    fullyPaidOrders = orders.Count(o => o.Balance == 0 && o.PaidAmount > 0),
                    pendingOrders   = orders.Count(o => o.Balance > 0)
                },
                data = result
            });
        }
        
        // ============================================================================================
        // API 6: CANCEL ORDER — Customer apna order cancel kare
        // ============================================================================================
        /// <summary>
        /// POST /api/CustomerOrderApi/CancelOrder/{orderId}
        ///
        /// PURPOSE: Customer apna order cancel kar sake (agar paid nahi hua).
        ///          Soft Delete use kiya gaya hai:
        ///            - Order DB se delete NAHI hota — admin audit trail rehti hai
        ///            - BaseEntity.Deleted = DateTime.UtcNow set hota hai
        ///            - BaseEntity.DeletedBy = "CANCELLED_BY:customerId | REASON:..."
        ///
        /// RULES:
        ///   ✅ Sirf customer ka apna order cancel ho sakta hai
        ///   ✅ Sirf "Unpaid" ya "Partial" orders cancel ho sakte hain
        ///   ❌ Fully paid orders cancel nahi ho sakte (refund process alag hai)
        ///   ❌ Already cancelled orders dobara cancel nahi ho sakte
        ///
        /// AUTH: Customer JWT Token required
        /// EXISTING IMPACT: Koi bhi admin/existing functionality break nahi hoti.
        ///                  Soft delete hone se order admin panel mein filtered out ho jayega
        ///                  (agar admin panel Deleted==null filter use karta hai).
        /// </summary>
        [HttpPost("CancelOrder/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest? request)
        {
            // Step 1: Token se CustomerId nikalo
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid or expired customer token." });

            if (orderId <= 0)
                return BadRequest(new { success = false, message = "Invalid Order ID." });

            // Step 2: Order fetch karo aur ownership verify karo
            var order = await _salesOrderRepo.GetById(orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Order not found." });

            // Security: Customer sirf apna order cancel kar sakta hai
            if (order.CustomerId != customerId)
                return Forbid(); // 403

            // Check: Pehle se cancel hua?
            if (order.Deleted != null)
                return BadRequest(new { success = false, message = "This order is already cancelled." });

            // Check: Fully paid order cancel nahi hoga
            if (order.PaidAmount > 0 && order.PaidAmount >= order.TotalPayable)
                return BadRequest(new
                {
                    success = false,
                    message = "Fully paid orders cannot be cancelled. Please contact support for refund."
                });

            // Step 3: Cancel reason (optional)
            var reason = request?.Reason ?? "No reason provided";

            // Step 4: Soft cancel via repository
            var cancelled = await _salesOrderRepo.CancelOrder(orderId, customerId.ToString(), reason);

            if (!cancelled)
                return BadRequest(new { success = false, message = "Order could not be cancelled. Please try again." });

            return Ok(new
            {
                success  = true,
                message  = "Order cancelled successfully.",
                orderId  = orderId,
                billNo   = order.BillNo,
                cancelledAt = DateTime.UtcNow
            });
        }
        // ============================================================================================
        // API 7: ADD ADDRESS — Customer apni nayi delivery address save kare
        // ============================================================================================
        /// <summary>
        /// POST /api/CustomerOrderApi/AddAddress
        ///
        /// PURPOSE: Customer ek nayi address (Home/Work) save kar sake.
        /// </summary>
        [HttpPost("AddAddress")]
        public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            if (request == null || string.IsNullOrWhiteSpace(request.AddressLine))
                return BadRequest(new { success = false, message = "Address line is required." });

            var newAddress = new CustomerAddress
            {
                CustomerId = customerId,
                Title = request.Title,
                AddressLine = request.AddressLine,
                City = request.City,
                State = request.State,
                Pincode = request.Pincode,
                IsDefault = request.IsDefault
            };

            await _customerRepo.AddAddressAsync(newAddress);

            return Ok(new
            {
                success = true,
                message = "Address added successfully.",
                addressId = newAddress.Id
            });
        }

        // ============================================================================================
        // API 8: TRACK ORDER — Customer apne order ka current status dekhe
        // ============================================================================================
        /// <summary>
        /// GET /api/CustomerOrderApi/TrackOrder/{orderId}
        ///
        /// PURPOSE: Return order status (Ordered, Billed, Dispatched, Delivered, Cancelled).
        /// </summary>
        [HttpGet("TrackOrder/{orderId}")]
        public async Task<IActionResult> TrackOrder(int orderId)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            var order = await _salesOrderRepo.GetById(orderId);
            if (order == null || order.CustomerId != customerId)
                return NotFound(new { success = false, message = "Order not found or access denied." });

            // If it was cancelled using the CancelOrder API (soft delete)
            var currentStatus = order.Deleted != null ? AOne.Utility.Enums.OrderStatus.Cancelled.ToString() : order.OrderStatus.ToString();

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                billNo = order.BillNo ?? "Pending",
                status = currentStatus,
                orderedOn = order.Created ?? order.BillDate,
                expectedDelivery = order.BillDate?.AddDays(3) // Example dummy logic
            });
        }

        // ============================================================================================
        // API 9: CREATE ORDER — Customer naya order place kare (Server-side Pricing)
        // ============================================================================================
        /// <summary>
        /// POST /api/CustomerOrderApi/CreateOrder
        ///
        /// PURPOSE: Customer cart se order banaye.
        /// SECURITY: Item rate aur GST server-side calculate hota hai (DB se) taaki user app se 
        ///           price manipulate na kar sake. Total amount khud calculate hota hai.
        /// </summary>
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!TryGetCustomerId(out int customerId))
                return Unauthorized(new { success = false, message = "Invalid customer token." });

            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest(new { success = false, message = "Order must contain at least one item." });

            if (string.IsNullOrWhiteSpace(request.OrderAddress))
                return BadRequest(new { success = false, message = "Order address is required." });

            decimal totalAmount = 0;
            decimal totalGstAmount = 0;
            decimal totalDiscount = 0;
            var orderItems = new List<SalesOrderItem>();

            // Accept values from frontend as requested
            foreach (var reqItem in request.Items)
            {
                var itemMaster = await _itemMasterRepo.GetByItemMasterId(reqItem.ItemId);
                if (itemMaster == null)
                    return BadRequest(new { success = false, message = $"Item with ID {reqItem.ItemId} not found." });

                // Values provided by frontend
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

            // Payment Details
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
                OrderStatus = AOne.Utility.Enums.OrderStatus.Ordered,
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
    }
}

