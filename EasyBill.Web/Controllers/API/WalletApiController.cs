using AOne.DataAccess.ProfileService;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletApiController : ControllerBase
    {
        private readonly ITenantRegistrationRepository _tenantRepo;
        private readonly IProfileService _profileService;
        private readonly IUnitOfWork _unitOfWork;

        public WalletApiController(
            ITenantRegistrationRepository tenantRepo,
            IProfileService profileService,
            IUnitOfWork unitOfWork)
        {
            _tenantRepo = tenantRepo;
            _profileService = profileService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet("GetWallet")]
        public async Task<IActionResult> GetWallet()
        {
            await _profileService.Set(User);
            var tenantId = _profileService.Profile.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Unauthorized(new { success = false, message = "Tenant not found." });
            }

            var tenant = await _tenantRepo.GetById(tenantId);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Tenant not found." });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    tenantId = tenant.Id,
                    tenantName = tenant.Name,
                    walletBalance = tenant.WalletBalance,
                    whatsAppMessageCharge = tenant.WhatsAppMessageCharge,
                    smsMessageCharge = tenant.SmsMessageCharge,
                    emailMessageCharge = tenant.EmailMessageCharge,
                    paymentGatewayProvider = tenant.PaymentGatewayProvider,
                    paymentGatewayKey = tenant.PaymentGatewayKey,
                    paymentGatewaySecret = tenant.PaymentGatewaySecret,
                    isPaymentGatewayActive = tenant.IsPaymentGatewayActive,
                    isWalletActive = tenant.IsWalletActive
                }
            });
        }

        [HttpPost("SaveWallet")]
        public async Task<IActionResult> SaveWallet([FromBody] WalletSettingsRequest request)
        {
            await _profileService.Set(User);
            var tenantId = _profileService.Profile.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Unauthorized(new { success = false, message = "Tenant not found." });
            }

            var tenant = await _tenantRepo.GetById(tenantId);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Tenant not found." });
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

            if (request.PaymentGatewayProvider != null)
            {
                tenant.PaymentGatewayProvider = request.PaymentGatewayProvider.Trim();
            }

            if (request.PaymentGatewayKey != null)
            {
                tenant.PaymentGatewayKey = request.PaymentGatewayKey.Trim();
            }

            if (request.PaymentGatewaySecret != null)
            {
                tenant.PaymentGatewaySecret = request.PaymentGatewaySecret.Trim();
            }

            if (request.IsPaymentGatewayActive.HasValue)
            {
                tenant.IsPaymentGatewayActive = request.IsPaymentGatewayActive.Value;
            }

            if (request.IsWalletActive.HasValue)
            {
                tenant.IsWalletActive = request.IsWalletActive.Value;
            }

            await _tenantRepo.Update(tenant);

            return Ok(new
            {
                success = true,
                message = "Wallet settings saved successfully.",
                walletBalance = tenant.WalletBalance
            });
        }

        [HttpPut("UpdateWallet")]
        public async Task<IActionResult> UpdateWallet([FromBody] WalletSettingsRequest request)
        {
            return await SaveWallet(request);
        }

        [HttpPost("AdminRecharge")]
        public async Task<IActionResult> AdminRecharge([FromBody] WalletRechargeRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Amount should be greater than 0." });
            }

            await _profileService.Set(User);
            var tenantId = _profileService.Profile.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Unauthorized(new { success = false, message = "Tenant not found." });
            }

            var tenant = await _tenantRepo.GetById(tenantId);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Tenant not found." });
            }

            if (!tenant.IsWalletActive)
            {
                return BadRequest(new { success = false, message = "Wallet is inactive. Please activate wallet first." });
            }

            tenant.WalletBalance += request.Amount;
            await _tenantRepo.Update(tenant);

            await LogTenantWalletHistoryAsync(
                tenant.Id,
                request.Amount,
                isDebit: false,
                paymentMode: "Manual",
                referenceNo: request.ReferenceNo,
                gatewayTransactionId: null,
                serviceType: "WhatsApp",
                serviceCharge: 0m,
                remarks: request.Remarks,
                closingBalance: tenant.WalletBalance,
                referenceSaleId: null);

            return Ok(new
            {
                success = true,
                message = "Wallet recharged successfully.",
                rechargeAmount = request.Amount,
                balance = tenant.WalletBalance,
                referenceNo = request.ReferenceNo,
                remarks = request.Remarks
            });
        }

        [HttpPost("CreateGatewayRechargeOrder")]
        public async Task<IActionResult> CreateGatewayRechargeOrder([FromBody] WalletRechargeRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Amount should be greater than 0." });
            }

            await _profileService.Set(User);
            var tenantId = _profileService.Profile.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Unauthorized(new { success = false, message = "Tenant not found." });
            }

            var tenant = await _tenantRepo.GetById(tenantId);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Tenant not found." });
            }

            if (!tenant.IsPaymentGatewayActive ||
                string.IsNullOrWhiteSpace(tenant.PaymentGatewayProvider) ||
                string.IsNullOrWhiteSpace(tenant.PaymentGatewayKey))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Payment gateway is not configured."
                });
            }

            if (!tenant.IsWalletActive)
            {
                return BadRequest(new { success = false, message = "Wallet is inactive. Please activate wallet first." });
            }

            return Ok(new
            {
                success = true,
                message = "Gateway recharge order created.",
                data = new
                {
                    provider = tenant.PaymentGatewayProvider,
                    key = tenant.PaymentGatewayKey,
                    orderId = $"WL-{DateTime.Now:yyyyMMddHHmmssfff}",
                    amount = request.Amount
                }
            });
        }

        [HttpPost("ConfirmGatewayRecharge")]
        public async Task<IActionResult> ConfirmGatewayRecharge([FromBody] GatewayRechargeConfirmRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Amount should be greater than 0." });
            }

            if (string.IsNullOrWhiteSpace(request.GatewayTransactionId))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Gateway transaction id is required."
                });
            }

            await _profileService.Set(User);
            var tenantId = _profileService.Profile.TenantId;

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Unauthorized(new { success = false, message = "Tenant not found." });
            }

            var tenant = await _tenantRepo.GetById(tenantId);
            if (tenant == null)
            {
                return NotFound(new { success = false, message = "Tenant not found." });
            }

            if (!tenant.IsWalletActive)
            {
                return BadRequest(new { success = false, message = "Wallet is inactive. Please activate wallet first." });
            }

            tenant.WalletBalance += request.Amount;
            await _tenantRepo.Update(tenant);

            await LogTenantWalletHistoryAsync(
                tenant.Id,
                request.Amount,
                isDebit: false,
                paymentMode: "Gateway",
                referenceNo: request.GatewayTransactionId,
                gatewayTransactionId: request.GatewayTransactionId,
                serviceType: "WhatsApp",
                serviceCharge: 0m,
                remarks: request.Remarks,
                closingBalance: tenant.WalletBalance,
                referenceSaleId: null);

            return Ok(new
            {
                success = true,
                message = "Gateway recharge confirmed successfully.",
                gatewayTransactionId = request.GatewayTransactionId,
                balance = tenant.WalletBalance
            });
        }

        public class WalletSettingsRequest
        {
            public decimal? WalletBalance { get; set; }
            public decimal? WhatsAppMessageCharge { get; set; }
            public decimal? SmsMessageCharge { get; set; }
            public decimal? EmailMessageCharge { get; set; }
            public string? PaymentGatewayProvider { get; set; }
            public string? PaymentGatewayKey { get; set; }
            public string? PaymentGatewaySecret { get; set; }
            public bool? IsPaymentGatewayActive { get; set; }
            public bool? IsWalletActive { get; set; }
        }

        public class WalletRechargeRequest
        {
            public decimal Amount { get; set; }
            public string? ReferenceNo { get; set; }
            public string? Remarks { get; set; }
        }

        public class GatewayRechargeConfirmRequest
        {
            public decimal Amount { get; set; }
            public string? GatewayTransactionId { get; set; }
            public string? Remarks { get; set; }
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

            var historyRepo = _unitOfWork.GetRepository<TenantWalletHistory>();
            historyRepo.Add(new TenantWalletHistory
            {
                TenantId = tenantId,
                TransactionDateTime = DateTime.Now,
                Credit = isDebit ? 0m : amount,
                Debit = isDebit ? amount : 0m,
                PaymentMode = paymentMode,
                ReferenceNo = string.IsNullOrWhiteSpace(referenceNo) ? null : referenceNo.Trim(),
                GatewayTransactionId = string.IsNullOrWhiteSpace(gatewayTransactionId) ? null : gatewayTransactionId.Trim(),
                ReferenceSaleId = referenceSaleId,
                ServiceType = string.IsNullOrWhiteSpace(serviceType) ? "WhatsApp" : serviceType,
                ServiceCharge = serviceCharge.GetValueOrDefault() < 0 ? 0m : serviceCharge.GetValueOrDefault(),
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
                ClosingBalance = closingBalance < 0 ? 0m : closingBalance
            });

            using var transaction = historyRepo.BeginTransaction();
            await historyRepo.SaveChangesAsync();
            transaction.Commit();
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
    }
}
