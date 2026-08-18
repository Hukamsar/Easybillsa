using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EasyBill.UI.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private const string PendingOrdersSessionKey = "Wallet.PendingGatewayOrders";
        private const string StaticGatewayProvider = "Razorpay";
        private const string StaticGatewayKey = "YOUR_KEY_ID";
        private const string StaticGatewaySecret = "YOUR_KEY_SECRET";
        private const bool StaticGatewayActive = true;

        private readonly ITenantRegistrationRepository _tenantRepository;
        private readonly IProfileService _profileService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUnitOfWork _unitOfWork;

        public WalletController(
            ITenantRegistrationRepository tenantRepository,
            IProfileService profileService,
            IHttpClientFactory httpClientFactory,
            IUnitOfWork unitOfWork)
        {
            _tenantRepository = tenantRepository;
            _profileService = profileService;
            _httpClientFactory = httpClientFactory;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Recharge));
        }

        [HttpGet]
        public IActionResult Recharge()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetWallet()
        {
            var tenant = await GetCurrentTenantAsync();
            if (tenant == null)
            {
                return Json(new { success = false, message = "Tenant not found." });
            }

            var gateway = ResolveGatewayConfig(tenant);
            var balance = await GetDisplayWalletBalanceAsync(tenant);
            var history = await GetTenantWalletHistoryAsync(tenant.Id, 20);

            return Json(new
            {
                success = true,
                data = new
                {
                    tenantId = tenant.Id,
                    tenantName = string.IsNullOrWhiteSpace(tenant.Name) ? "Company" : tenant.Name,
                    tenantEmail = tenant.Email,
                    tenantMobileNo = tenant.MobileNo,
                    walletBalance = balance,
                    whatsAppMessageCharge = tenant.WhatsAppMessageCharge,
                    smsMessageCharge = tenant.SmsMessageCharge,
                    emailMessageCharge = tenant.EmailMessageCharge,
                    paymentGatewayProvider = gateway.Provider,
                    paymentGatewayKey = gateway.Key,
                    paymentGatewaySecret = string.Empty,
                    isPaymentGatewayActive = gateway.IsConfigured,
                    isWalletActive = tenant.IsWalletActive,
                    isSmsChargeActive = tenant.IsSmsChargeActive,
                    isEmailChargeActive = tenant.IsEmailChargeActive,
                    isWhatsAppChargeActive = tenant.IsWhatsAppChargeActive,
                    rechargeHistory = history
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSettings([FromBody] WalletSettingsRequest request)
        {
            var tenant = await GetCurrentTenantAsync();
            if (tenant == null)
            {
                return Json(new { success = false, message = "Tenant not found." });
            }

            if (request.WhatsAppMessageCharge.HasValue && request.WhatsAppMessageCharge.Value < 0)
            {
                return Json(new { success = false, message = "WhatsApp charge cannot be negative." });
            }

            if (request.SmsMessageCharge.HasValue && request.SmsMessageCharge.Value < 0)
            {
                return Json(new { success = false, message = "SMS charge cannot be negative." });
            }

            if (request.EmailMessageCharge.HasValue && request.EmailMessageCharge.Value < 0)
            {
                return Json(new { success = false, message = "Email charge cannot be negative." });
            }

            if (request.WalletBalance.HasValue && request.WalletBalance.Value < 0)
            {
                return Json(new { success = false, message = "Wallet balance cannot be negative." });
            }

            if (request.WalletBalance.HasValue)
            {
                tenant.WalletBalance = request.WalletBalance.Value;
            }

            if (request.WhatsAppMessageCharge.HasValue)
            {
                tenant.WhatsAppMessageCharge = request.WhatsAppMessageCharge.Value;
            }

            if (request.SmsMessageCharge.HasValue)
            {
                tenant.SmsMessageCharge = request.SmsMessageCharge.Value;
            }

            if (request.EmailMessageCharge.HasValue)
            {
                tenant.EmailMessageCharge = request.EmailMessageCharge.Value;
            }

            if (request.IsPaymentGatewayActive.HasValue)
            {
                tenant.IsPaymentGatewayActive = request.IsPaymentGatewayActive.Value;
            }

            if (request.IsWalletActive.HasValue)
            {
                tenant.IsWalletActive = request.IsWalletActive.Value;
            }
            
                tenant.IsSmsChargeActive = request.IsSmsChargeActive; 
                tenant.IsEmailChargeActive = request.IsEmailChargeActive; 
                tenant.IsWhatsAppChargeActive = request.IsWhatsAppChargeActive;
           
            await _tenantRepository.Update(tenant);

            return Json(new
            {
                success = true,
                message = "Wallet settings saved successfully.",
                walletBalance = tenant.WalletBalance
            });
        }

        [HttpPost]
        public async Task<IActionResult> AdminRecharge([FromBody] WalletRechargeRequest request)
        {
            if (request.Amount <= 0)
            {
                return Json(new { success = false, message = "Amount should be greater than 0." });
            }

            var tenant = await GetCurrentTenantAsync();
            if (tenant == null)
            {
                return Json(new { success = false, message = "Tenant not found." });
            }

            if (!tenant.IsWalletActive)
            {
                return Json(new { success = false, message = "Wallet is inactive. Please activate wallet first." });
            }

            var generatedReferenceNo = string.IsNullOrWhiteSpace(request.ReferenceNo)
                ? GenerateRechargeReference()
                : request.ReferenceNo.Trim();

            tenant.WalletBalance += request.Amount;
            await _tenantRepository.Update(tenant);

            string? historyWarning = null;
            try
            {
                await LogTenantWalletHistoryAsync(
                    tenant.Id,
                    request.Amount,
                    isDebit: false,
                    "Manual",
                    generatedReferenceNo,
                    null,
                    request.ServiceType,
                    request.ServiceCharge,
                    request.Remarks,
                    tenant.WalletBalance,
                    null);
            }
            catch
            {
                historyWarning = "Recharge completed, but history logging failed.";
            }

            return Json(new
            {
                success = true,
                message = "Wallet recharged successfully.",
                rechargeAmount = request.Amount,
                balance = tenant.WalletBalance,
                referenceNo = generatedReferenceNo,
                remarks = request.Remarks,
                historyWarning
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateGatewayOrder([FromBody] WalletRechargeRequest request)
        {
            if (request.Amount <= 0)
            {
                return Json(new { success = false, message = "Amount should be greater than 0." });
            }

            var tenant = await GetCurrentTenantAsync();
            if (tenant == null)
            {
                return Json(new { success = false, message = "Tenant not found." });
            }

            if (!tenant.IsWalletActive)
            {
                return Json(new { success = false, message = "Wallet is inactive. Please activate wallet first." });
            }

            var gateway = ResolveGatewayConfig(tenant);
            if (!gateway.IsConfigured)
            {
                return Json(new
                {
                    success = false,
                    message = "Payment gateway is not configured.",
                    details = "Set valid Razorpay Key/Secret in backend static config or tenant gateway settings."
                });
            }

            var amountInPaise = (long)Math.Round(request.Amount * 100M, MidpointRounding.AwayFromZero);
            var receipt = $"wl_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var credentialBytes = Encoding.UTF8.GetBytes($"{gateway.Key}:{gateway.Secret}");
                var authHeader = Convert.ToBase64String(credentialBytes);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

                var payload = new
                {
                    amount = amountInPaise,
                    currency = "INR",
                    receipt,
                    payment_capture = 1
                };

                httpRequest.Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                using var response = await httpClient.SendAsync(httpRequest);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unable to create payment order from gateway.",
                        details = responseText
                    });
                }

                using var orderJson = JsonDocument.Parse(responseText);
                var root = orderJson.RootElement;

                var orderId = root.GetProperty("id").GetString();
                var orderAmount = root.GetProperty("amount").GetInt64();
                var currency = root.GetProperty("currency").GetString() ?? "INR";

                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Payment order id was not returned by gateway."
                    });
                }

                var pendingOrders = GetPendingOrders();
                var generatedReferenceNo = GenerateRechargeReference();
                pendingOrders[orderId] = new PendingGatewayOrder
                {
                    Amount = request.Amount,
                    CreatedOnUtc = DateTime.UtcNow,
                    ServiceType = NormalizeServiceType(request.ServiceType),
                    ServiceCharge = NormalizeServiceCharge(request.ServiceCharge),
                    Remarks = request.Remarks,
                    ReferenceNo = generatedReferenceNo
                };
                SavePendingOrders(pendingOrders);

                return Json(new
                {
                    success = true,
                    message = "Payment order created successfully.",
                    data = new
                    {
                        provider = StaticGatewayProvider,
                        key = gateway.Key,
                        orderId,
                        amountInPaise = orderAmount,
                        amount = request.Amount,
                        currency,
                        referenceNo = generatedReferenceNo,
                        tenantName = tenant.Name,
                        tenantEmail = tenant.Email,
                        tenantMobileNo = tenant.MobileNo
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to create payment order.",
                    details = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmGatewayRecharge([FromBody] GatewayRechargeConfirmRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.GatewayOrderId) ||
                string.IsNullOrWhiteSpace(request.GatewayTransactionId) ||
                string.IsNullOrWhiteSpace(request.GatewaySignature))
            {
                return Json(new
                {
                    success = false,
                    message = "Payment verification data is missing."
                });
            }

            var tenant = await GetCurrentTenantAsync();
            if (tenant == null)
            {
                return Json(new { success = false, message = "Tenant not found." });
            }

            var gateway = ResolveGatewayConfig(tenant);
            if (!gateway.IsConfigured)
            {
                return Json(new { success = false, message = "Payment gateway is not configured properly." });
            }

            var pendingOrders = GetPendingOrders();
            if (!pendingOrders.TryGetValue(request.GatewayOrderId, out var pendingOrder))
            {
                return Json(new
                {
                    success = false,
                    message = "Payment order is not pending for confirmation. Please start recharge again."
                });
            }

            if (pendingOrder.Amount <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid pending payment amount."
                });
            }

            var payload = $"{request.GatewayOrderId}|{request.GatewayTransactionId}";
            var expectedSignature = ComputeHmacSha256(payload, gateway.Secret);

            if (!SecureEquals(expectedSignature, request.GatewaySignature))
            {
                return Json(new
                {
                    success = false,
                    message = "Payment signature verification failed."
                });
            }

            try
            {
                var gatewayValidation = await ValidateRazorpayPaymentAsync(
                    gateway.Key,
                    gateway.Secret,
                    request.GatewayTransactionId,
                    request.GatewayOrderId,
                    pendingOrder.Amount);

                if (!gatewayValidation.IsValid)
                {
                    return Json(new
                    {
                        success = false,
                        message = gatewayValidation.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unable to validate payment status from gateway.",
                    details = ex.Message
                });
            }

            tenant.WalletBalance += pendingOrder.Amount;
            await _tenantRepository.Update(tenant);

            string? historyWarning = null;

            try
            {
                await LogTenantWalletHistoryAsync(
                    tenant.Id,
                    pendingOrder.Amount,
                    isDebit: false,
                    "Gateway",
                    pendingOrder.ReferenceNo,
                    request.GatewayTransactionId,
                    pendingOrder.ServiceType,
                    pendingOrder.ServiceCharge,
                    pendingOrder.Remarks,
                    tenant.WalletBalance,
                    null);
            }
            catch
            {
                historyWarning = "Recharge completed, but history logging failed.";
            }

            pendingOrders.Remove(request.GatewayOrderId);
            SavePendingOrders(pendingOrders);

            return Json(new
            {
                success = true,
                message = "Wallet recharged successfully via payment gateway.",
                balance = tenant.WalletBalance,
                rechargeAmount = pendingOrder.Amount,
                referenceNo = pendingOrder.ReferenceNo,
                gatewayTransactionId = request.GatewayTransactionId,
                historyWarning
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRechargeHistory(int take = 30)
        {
            if (take <= 0)
            {
                take = 30;
            }

            if (take > 200)
            {
                take = 200;
            }

            var tenant = await GetCurrentTenantAsync();
            if (tenant == null)
            {
                return Json(new { success = false, message = "Tenant not found." });
            }

            var data = await GetTenantWalletHistoryAsync(tenant.Id, take);

            return Json(new { success = true, data });
        }

        private async Task<(bool IsValid, string Message)> ValidateRazorpayPaymentAsync(
            string key,
            string secret,
            string paymentId,
            string orderId,
            decimal expectedAmount)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var credentialBytes = Encoding.UTF8.GetBytes($"{key}:{secret}");
            var authHeader = Convert.ToBase64String(credentialBytes);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.razorpay.com/v1/payments/{paymentId}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            using var response = await httpClient.SendAsync(httpRequest);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (false, "Failed to verify payment at gateway.");
            }

            using var paymentJson = JsonDocument.Parse(responseText);
            var root = paymentJson.RootElement;

            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : null;

            var apiOrderId = root.TryGetProperty("order_id", out var orderIdElement)
                ? orderIdElement.GetString()
                : null;

            var apiAmountInPaise = root.TryGetProperty("amount", out var amountElement)
                ? amountElement.GetInt64()
                : 0;

            var expectedAmountInPaise = (long)Math.Round(expectedAmount * 100M, MidpointRounding.AwayFromZero);

            if (!string.Equals(apiOrderId, orderId, StringComparison.Ordinal))
            {
                return (false, "Gateway order id mismatch.");
            }

            if (apiAmountInPaise != expectedAmountInPaise)
            {
                return (false, "Gateway amount mismatch.");
            }

            if (!string.Equals(status, "captured", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, "authorized", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Payment is not captured yet.");
            }

            return (true, "Payment verified.");
        }

        private async Task<AOne.Models.Entity.Tenant?> GetCurrentTenantAsync()
        {
            var tenantId = await GetCurrentTenantIdAsync();

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return null;
            }

            return await _tenantRepository.GetById(tenantId);
        }

        private async Task<string?> GetCurrentTenantIdAsync()
        {
            var tenantId = User.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }

            await _profileService.Set(User);
            return _profileService?.Profile?.TenantId;
        }

        private async Task<IList<RechargeHistoryRow>> GetTenantWalletHistoryAsync(
            string tenantId,
            int take)
        {
            var historyRepo = _unitOfWork.GetRepository<TenantWalletHistory>();
            var rows = await historyRepo.Query()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.TransactionDateTime)
                .Take(take)
                .ToListAsync();

            return rows.Select(x => new RechargeHistoryRow
            {
                Id = x.Id,
                TransactionDateTime = x.TransactionDateTime,
                Amount = x.Credit > 0 ? x.Credit : -x.Debit,
                IsDebit = x.Debit > 0,
                PaymentMode = x.PaymentMode,
                ReferenceNo = x.ReferenceNo,
                ServiceType = x.ServiceType,
                ServiceCharge = x.ServiceCharge,
                Note = x.Remarks
            }).ToList();
        }

        private async Task LogTenantWalletHistoryAsync(
            string tenantId,
            decimal amount,
            bool isDebit,
            string paymentMode,
            string? referenceNo,
            string? gatewayTransactionId,
            string? serviceType,
            decimal? serviceCharge,
            string? remarks,
            decimal closingBalance,
            int? referenceSaleId)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || amount <= 0)
            {
                return;
            }

            var serviceName = NormalizeServiceType(serviceType);
            var sanitizedCharge = NormalizeServiceCharge(serviceCharge);
            var sanitizedNote = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
            var reference = !string.IsNullOrWhiteSpace(referenceNo)
                ? referenceNo.Trim()
                : !string.IsNullOrWhiteSpace(gatewayTransactionId)
                    ? gatewayTransactionId.Trim()
                    : GenerateRechargeReference();

            var historyRepo = _unitOfWork.GetRepository<TenantWalletHistory>();
            historyRepo.Add(new TenantWalletHistory
            {
                TenantId = tenantId,
                TransactionDateTime = DateTime.Now,
                Credit = isDebit ? 0m : amount,
                Debit = isDebit ? amount : 0m,
                PaymentMode = paymentMode,
                ReferenceNo = reference,
                GatewayTransactionId = string.IsNullOrWhiteSpace(gatewayTransactionId) ? null : gatewayTransactionId.Trim(),
                ReferenceSaleId = referenceSaleId,
                ServiceType = serviceName,
                ServiceCharge = sanitizedCharge,
                Remarks = sanitizedNote,
                ClosingBalance = decimal.Round(Math.Max(closingBalance, 0m), 2, MidpointRounding.AwayFromZero)
            });

            using var transaction = historyRepo.BeginTransaction();
            await historyRepo.SaveChangesAsync();
            transaction.Commit();
        }

        private static string NormalizeServiceType(string? serviceType)
        {
            if (string.IsNullOrWhiteSpace(serviceType))
            {
                return "WhatsApp";
            }

            var tokens = serviceType.Split(
                new[] { ',', '+', '|', '/', ';' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                tokens = new[] { serviceType };
            }

            var services = new List<string>();
            foreach (var token in tokens)
            {
                var normalized = token.Trim().ToLowerInvariant();
                if (normalized == "both")
                {
                    if (!services.Contains("SMS"))
                    {
                        services.Add("SMS");
                    }

                    if (!services.Contains("WhatsApp"))
                    {
                        services.Add("WhatsApp");
                    }

                    continue;
                }

                if (normalized == "sms" && !services.Contains("SMS"))
                {
                    services.Add("SMS");
                    continue;
                }

                if (normalized == "email" && !services.Contains("Email"))
                {
                    services.Add("Email");
                    continue;
                }

                if (normalized == "whatsapp" && !services.Contains("WhatsApp"))
                {
                    services.Add("WhatsApp");
                }
            }

            return services.Count > 0 ? string.Join(" + ", services) : "WhatsApp";
        }

        private static decimal NormalizeServiceCharge(decimal? serviceCharge)
        {
            if (!serviceCharge.HasValue || serviceCharge.Value < 0)
            {
                return 0m;
            }

            return decimal.Round(serviceCharge.Value, 2, MidpointRounding.AwayFromZero);
        }

        private static string GenerateRechargeReference()
        {
            var randomSuffix = RandomNumberGenerator.GetInt32(1000, 9999);
            return $"RCG-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{randomSuffix}";
        }

        private GatewayConfig ResolveGatewayConfig(AOne.Models.Entity.Tenant tenant)
        {
            var provider = string.IsNullOrWhiteSpace(StaticGatewayProvider)
                ? "Razorpay"
                : StaticGatewayProvider.Trim();

            var key = StaticGatewayKey?.Trim() ?? string.Empty;
            var secret = StaticGatewaySecret?.Trim() ?? string.Empty;

            if (IsPlaceholderGatewayValue(key) || IsPlaceholderGatewayValue(secret))
            {
                if (!string.IsNullOrWhiteSpace(tenant.PaymentGatewayProvider))
                {
                    provider = tenant.PaymentGatewayProvider.Trim();
                }

                if (!string.IsNullOrWhiteSpace(tenant.PaymentGatewayKey))
                {
                    key = tenant.PaymentGatewayKey.Trim();
                }

                if (!string.IsNullOrWhiteSpace(tenant.PaymentGatewaySecret))
                {
                    secret = tenant.PaymentGatewaySecret.Trim();
                }
            }

            var isConfigured = StaticGatewayActive &&
                provider.Equals("Razorpay", StringComparison.OrdinalIgnoreCase) &&
                !IsPlaceholderGatewayValue(key) &&
                !IsPlaceholderGatewayValue(secret);

            return new GatewayConfig
            {
                Provider = provider,
                Key = key,
                Secret = secret,
                IsConfigured = isConfigured
            };
        }

        private async Task<decimal> GetDisplayWalletBalanceAsync(AOne.Models.Entity.Tenant tenant)
        {
            await Task.CompletedTask;
            return decimal.Round(Math.Max(tenant.WalletBalance, 0m), 2, MidpointRounding.AwayFromZero);
        }

        private static bool IsPlaceholderGatewayValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var normalized = value.Trim();
            return normalized.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
        }

        private Dictionary<string, PendingGatewayOrder> GetPendingOrders()
        {
            var raw = HttpContext.Session.GetString(PendingOrdersSessionKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new Dictionary<string, PendingGatewayOrder>(StringComparer.Ordinal);
            }

            try
            {
                var value = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, PendingGatewayOrder>>(raw);
                return value ?? new Dictionary<string, PendingGatewayOrder>(StringComparer.Ordinal);
            }
            catch
            {
                return new Dictionary<string, PendingGatewayOrder>(StringComparer.Ordinal);
            }
        }

        private void SavePendingOrders(Dictionary<string, PendingGatewayOrder> orders)
        {
            var cleaned = orders
                .Where(x => x.Value.CreatedOnUtc >= DateTime.UtcNow.AddHours(-6))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);

            HttpContext.Session.SetString(
                PendingOrdersSessionKey,
                System.Text.Json.JsonSerializer.Serialize(cleaned));
        }

        private static string ComputeHmacSha256(string payload, string secret)
        {
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(secretBytes);
            var hash = hmac.ComputeHash(payloadBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool SecureEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);

            if (aBytes.Length != bBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

    }
}
