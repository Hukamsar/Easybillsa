using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.UI.Service.Loyalty
{
    public class CustomerLoyaltyService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ISalesRepository _salesRepository;
        private readonly IOfferRepository _offerRepository;
        private readonly IPointSettingRepository _pointSettingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CustomerLoyaltyService(
            ICustomerRepository customerRepository,
            ISalesRepository salesRepository,
            IOfferRepository offerRepository,
            IPointSettingRepository pointSettingRepository,
            IUnitOfWork unitOfWork)
        {
            _customerRepository = customerRepository;
            _salesRepository = salesRepository;
            _offerRepository = offerRepository;
            _pointSettingRepository = pointSettingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CustomerLoyaltyReportVM?> GetCustomerLoyaltyReportAsync(int customerId)
        {
            var customer = await _customerRepository.GetById(customerId);
            if (customer == null)
            {
                return null;
            }

            var sales = (await _salesRepository.GetByCustomerId(customerId))
                .OrderBy(x => x.BillDate ?? x.Created ?? DateTime.Now)
                .ToList();

            var offers = await _offerRepository.GetAll();
            var offerMap = offers.ToDictionary(x => x.Id, x => x);
            var saleById = sales.ToDictionary(x => x.Id, x => x);

            var cashbackEarnedLedger = new List<CashbackLedgerItemVM>();
            var cashbackUsedLedger = new List<CashbackLedgerItemVM>();

            foreach (var sale in sales)
            {
                if (sale.OfferId.HasValue &&
                    offerMap.TryGetValue(sale.OfferId.Value, out var offer) &&
                    IsCashbackOffer(offer))
                {
                    var cashbackAmount = CalculateCashbackAmount(sale, offer);
                    if (cashbackAmount > 0)
                    {
                        cashbackEarnedLedger.Add(new CashbackLedgerItemVM
                        {
                            TransactionDate = sale.BillDate ?? sale.Created ?? DateTime.Now,
                            TransactionType = "Earned",
                            Amount = cashbackAmount,
                            SaleId = sale.Id,
                            BillNo = sale.BillNo,
                            ExpiryDate = null,
                            NeverExpires = true,
                            Remarks = $"Cashback earned from bill {sale.BillNo}"
                        });
                    }
                }

                var advanceAdjustments = sale.SalsePaymentDetails
                    .Where(x =>
                        x.Amount > 0 &&
                        !string.IsNullOrWhiteSpace(x.Description) &&
                        x.Description.Contains("Adjusted from customer advance", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var payment in advanceAdjustments)
                {
                    cashbackUsedLedger.Add(new CashbackLedgerItemVM
                    {
                        TransactionDate = payment.Date ?? sale.BillDate ?? sale.Created ?? DateTime.Now,
                        TransactionType = "Used",
                        Amount = payment.Amount,
                        SaleId = sale.Id,
                        BillNo = sale.BillNo,
                        ExpiryDate = null,
                        NeverExpires = true,
                        Remarks = "Cashback/Advance adjusted in sale"
                    });
                }
            }

            var pointRepository = _unitOfWork.GetRepository<PointTransaction>();
            var pointTransactions = await pointRepository.Query()
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();

            var pointSettings = await _pointSettingRepository.GetAll();
            var pointSetting = GetCurrentPointSetting(pointSettings);
            var pointValueInRs = pointSetting?.PointValueInRs ?? 0m;

            var pointLedger = pointTransactions
                .Select(tx =>
                {
                    var netPoints = tx.EarnedPoints - tx.RedeemedPoints;
                    return new PointLedgerItemVM
                    {
                        TransactionDate = tx.TransactionDate,
                        EarnedPoints = tx.EarnedPoints,
                        RedeemedPoints = tx.RedeemedPoints,
                        NetPoints = netPoints,
                        PointValueInRs = pointValueInRs,
                        NetValueInRs = netPoints * pointValueInRs,
                        SaleId = tx.SaleId,
                        BillNo = tx.SaleId.HasValue && saleById.TryGetValue(tx.SaleId.Value, out var sale)
                            ? sale.BillNo
                            : null
                    };
                })
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            var cashbackEarned = cashbackEarnedLedger.Sum(x => x.Amount);
            var cashbackUsed = cashbackUsedLedger.Sum(x => x.Amount);
            var cashbackBalance = Math.Max(cashbackEarned - cashbackUsed, 0m);
            var cashbackOverUsed = Math.Max(cashbackUsed - cashbackEarned, 0m);

            var pointsEarned = pointTransactions.Sum(x => x.EarnedPoints);
            var pointsRedeemed = pointTransactions.Sum(x => x.RedeemedPoints);
            var pointsBalance = pointsEarned - pointsRedeemed;

            var cashbackLedger = cashbackEarnedLedger
                .Concat(cashbackUsedLedger)
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            return new CustomerLoyaltyReportVM
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                MobileNo = customer.PhoneNo,
                Email = customer.Email,
                CashbackEarned = cashbackEarned,
                CashbackUsed = cashbackUsed,
                CashbackBalance = cashbackBalance,
                CashbackOverUsed = cashbackOverUsed,
                CashbackUsageStatus = GetCashbackUsageStatus(cashbackEarned, cashbackUsed),
                CashbackNeverExpires = true,
                PointsEarned = pointsEarned,
                PointsRedeemed = pointsRedeemed,
                PointsBalance = pointsBalance,
                PointValueInRs = pointValueInRs,
                PointsBalanceValueInRs = pointsBalance * pointValueInRs,
                IsPointProgramActive = pointSetting != null,
                AllowPointRedemption = pointSetting?.AllowRedemption ?? false,
                IsCustomerGettingPoints = pointsEarned > 0,
                MinPointsToRedeem = pointSetting?.MinPointsToRedeem ?? 0,
                CashbackLedger = cashbackLedger,
                PointLedger = pointLedger
            };
        }

        public async Task<CustomerPointSummaryVM> GetCustomerPointSummaryAsync(int customerId)
        {
            var pointRepo = _unitOfWork.GetRepository<PointTransaction>();
            var pointsBalance = await pointRepo.Query()
                .Where(x => x.CustomerId == customerId)
                .SumAsync(x => x.EarnedPoints - x.RedeemedPoints);

            var settings = await _pointSettingRepository.GetAll();
            var pointSetting = GetApplicablePointSetting(settings, DateTime.Today);

            var pointValueInRs = pointSetting?.PointValueInRs ?? 0m;
            return new CustomerPointSummaryVM
            {
                CustomerId = customerId,
                PointsBalance = pointsBalance,
                PointValueInRs = pointValueInRs,
                PointsBalanceValueInRs = pointsBalance * pointValueInRs,
                IsPointProgramActive = pointSetting != null,
                AllowPointRedemption = pointSetting?.AllowRedemption ?? false,
                MinPointsToRedeem = pointSetting?.MinPointsToRedeem ?? 0
            };
        }

        public async Task<PointApplicationResultVM> ApplyPointsForSaleAsync(
            Sales sale,
            int requestedRedeemPoints = 0)
        {
            var result = new PointApplicationResultVM
            {
                Success = false,
                Message = string.Empty
            };

            try
            {
                if (sale == null || sale.TotalPayable <= 0)
                {
                    result.Message = "Loyalty skipped: invalid sale.";
                    return result;
                }

                // If customer is null, clean up any existing loyalty data for this sale
                if (!sale.CustomerId.HasValue)
                {
                    var pointRepo = _unitOfWork.GetRepository<PointTransaction>();
                    var existing = await pointRepo.Query()
                        .FirstOrDefaultAsync(x => x.SaleId == sale.Id);
                    if (existing != null)
                    {
                        pointRepo.Delete(existing);
                        await _unitOfWork.SaveAsync();
                    }
                    result.Success = true;
                    result.Message = "Loyalty cleared: walk-in sale.";
                    return result;
                }

                var settings = await _pointSettingRepository.GetAll();
                var billDate = (sale.BillDate ?? DateTime.Now).Date;
                var pointSetting = GetApplicablePointSetting(settings, billDate);

                // Exempt historical sales created before the loyalty program activation
                var tenantSettings = settings.Where(x => x.TenantId == sale.TenantId).ToList();
                if (tenantSettings.Count > 0)
                {
                    var earliestCreated = tenantSettings.Min(x => x.Created) ?? DateTime.MaxValue;
                    var saleCompareDate = sale.Created ?? sale.BillDate ?? DateTime.Now;
                    if (saleCompareDate < earliestCreated)
                    {
                        // Exclude historical sale from loyalty points processing
                        pointSetting = null;
                        requestedRedeemPoints = 0;
                    }
                }

                var earnedPoints = 0;
                if (pointSetting != null &&
                    pointSetting.EarnPerAmount > 0 &&
                    sale.TotalPayable >= pointSetting.MinAmountToEarn)
                {
                    var multiplier = pointSetting.EarningMultiplier > 0m ? pointSetting.EarningMultiplier : 1.0m;
                    earnedPoints = (int)Math.Floor((sale.TotalPayable / pointSetting.EarnPerAmount) * multiplier);
                }

                var redeemedPoints = 0;
                var redeemedAmount = 0m;
                var saleUpdated = false;
                var redeemMessage = string.Empty;

                var existingPayment = sale.SalsePaymentDetails?.FirstOrDefault(x =>
                    x.Description != null &&
                    x.Description.Contains("Adjusted from loyalty points", StringComparison.OrdinalIgnoreCase));

                if (requestedRedeemPoints > 0)
                {
                    if (pointSetting == null)
                    {
                        redeemMessage = "Point program is not active for this bill date.";
                    }
                    else if (!pointSetting.AllowRedemption || pointSetting.PointValueInRs <= 0)
                    {
                        redeemMessage = "Point redemption is disabled in current setting.";
                    }
                    else
                    {
                        var pointRepo = _unitOfWork.GetRepository<PointTransaction>();
                        
                        // For edit/update logic: fetch existing transaction to calculate effective balance correctly
                        var existingTx = await pointRepo.Query()
                            .FirstOrDefaultAsync(x => x.SaleId == sale.Id && x.CustomerId == sale.CustomerId.Value);
                        var currentRedeemedOnThisSale = existingTx?.RedeemedPoints ?? 0;

                        var availablePoints = await pointRepo.Query()
                            .Where(x => x.CustomerId == sale.CustomerId.Value && x.SaleId != sale.Id)
                            .SumAsync(x => x.EarnedPoints - x.RedeemedPoints);

                        var effectivePointsBalance = availablePoints + currentRedeemedOnThisSale;

                        if (effectivePointsBalance < pointSetting.MinPointsToRedeem)
                        {
                            redeemMessage = $"Minimum {pointSetting.MinPointsToRedeem} points are required to redeem (Current balance: {effectivePointsBalance}).";
                        }
                        else
                        {
                            var currentBalance = Math.Max(sale.Balance, 0m);
                            
                            // If this payment already exists, add its amount back to the current balance to get the actual outstanding balance before point redemption
                            if (existingPayment != null)
                            {
                                currentBalance += existingPayment.Amount;
                            }
                            else if (currentBalance <= 0m)
                            {
                                currentBalance = Math.Max(sale.TotalPayable - sale.PaidAmount, 0m);
                            }

                            var maxByBalance = (int)Math.Floor(currentBalance / pointSetting.PointValueInRs);
                            var maxRedeemablePoints = Math.Max(
                                0,
                                Math.Min(requestedRedeemPoints, Math.Min(effectivePointsBalance, maxByBalance)));

                            if (maxRedeemablePoints <= 0)
                            {
                                redeemMessage = "No redeemable points available for this bill.";
                            }
                            else
                            {
                                redeemedPoints = maxRedeemablePoints;
                                redeemedAmount = Math.Round(
                                    redeemedPoints * pointSetting.PointValueInRs,
                                    2,
                                    MidpointRounding.AwayFromZero);

                                if (existingPayment != null)
                                {
                                    var diff = redeemedAmount - existingPayment.Amount;
                                    existingPayment.Amount = redeemedAmount;
                                    existingPayment.Description = $"Adjusted from loyalty points ({redeemedPoints} pts)";
                                    existingPayment.Date = DateTime.Now;

                                    if (diff != 0m)
                                    {
                                        sale.PaidAmount += diff;
                                        sale.Balance = Math.Max(sale.TotalPayable - sale.PaidAmount, 0m);
                                        sale.PaymentStatus = sale.Balance <= 0
                                            ? "Paid"
                                            : sale.PaidAmount > 0 ? "Partial" : "Unpaid";
                                        saleUpdated = true;
                                    }
                                }
                                else
                                {
                                    sale.SalsePaymentDetails ??= new List<SalsePaymentDetails>();
                                    sale.SalsePaymentDetails.Add(new SalsePaymentDetails
                                    {
                                        PaymentModeId = 1,
                                        Amount = redeemedAmount,
                                        Description = $"Adjusted from loyalty points ({redeemedPoints} pts)",
                                        CustomerId = sale.CustomerId,
                                        Date = DateTime.Now
                                    });

                                    sale.PaidAmount += redeemedAmount;
                                    sale.Balance = Math.Max(sale.TotalPayable - sale.PaidAmount, 0m);
                                    sale.PaymentStatus = sale.Balance <= 0
                                        ? "Paid"
                                        : sale.PaidAmount > 0 ? "Partial" : "Unpaid";
                                    saleUpdated = true;
                                }
                            }
                        }
                    }
                }

                // If points redeemed is 0, but a loyalty payment was previously added, clean it up
                if (redeemedPoints <= 0 && existingPayment != null)
                {
                    sale.SalsePaymentDetails?.Remove(existingPayment);
                    sale.PaidAmount -= existingPayment.Amount;
                    sale.Balance = Math.Max(sale.TotalPayable - sale.PaidAmount, 0m);
                    sale.PaymentStatus = sale.Balance <= 0
                        ? "Paid"
                        : sale.PaidAmount > 0 ? "Partial" : "Unpaid";
                    saleUpdated = true;
                }

                await UpsertPointTransactionAsync(
                    sale,
                    earnedPoints,
                    redeemedPoints);

                if (saleUpdated)
                {
                    await _salesRepository.Update(sale);
                }

                await _unitOfWork.SaveAsync();

                result.Success = true;
                result.EarnedPoints = earnedPoints;
                result.RedeemedPoints = redeemedPoints;
                result.RedeemedAmount = redeemedAmount;
                result.SaleUpdated = saleUpdated;
                result.Message = BuildPointMessage(
                    earnedPoints,
                    redeemedPoints,
                    redeemedAmount,
                    redeemMessage);
                return result;
            }
            catch
            {
                // Never break the core sale flow because of loyalty logging.
                result.Success = false;
                result.Message = "Loyalty processing failed.";
                return result;
            }
        }

        public async Task<bool> TryRecordPointTransactionForSaleAsync(Sales sale)
        {
            if (sale == null || !sale.CustomerId.HasValue)
            {
                return false;
            }

            var pointRepo = _unitOfWork.GetRepository<PointTransaction>();
            var alreadyExists = await pointRepo.Query()
                .AnyAsync(x => x.SaleId == sale.Id && x.CustomerId == sale.CustomerId.Value);

            if (alreadyExists)
            {
                return false;
            }

            var pointResult = await ApplyPointsForSaleAsync(sale, 0);
            return pointResult.Success && pointResult.EarnedPoints > 0;
        }

        private static bool IsCashbackOffer(Offer offer)
        {
            return offer.OfferType == OfferType.CashbackAmount ||
                   offer.OfferType == OfferType.CashbackPercent;
        }

        private static decimal CalculateCashbackAmount(Sales sale, Offer offer)
        {
            if (sale.TotalPayable < (offer.MinAmount ?? 0))
            {
                return 0m;
            }

            if (offer.OfferType == OfferType.CashbackAmount)
            {
                return Math.Max(offer.DiscountValue, 0m);
            }

            if (offer.OfferType == OfferType.CashbackPercent)
            {
                return Math.Max(sale.TotalPayable * offer.DiscountValue / 100m, 0m);
            }

            return 0m;
        }

        private static PointSetting? GetCurrentPointSetting(IList<PointSetting> settings)
        {
            if (settings == null || settings.Count == 0)
            {
                return null;
            }

            var today = DateTime.Today;
            var active = settings
                .Where(x => x.IsActive && x.StartDate.Date <= today && x.EndDate.Date >= today)
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefault();

            if (active != null)
            {
                return active;
            }

            return settings
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefault();
        }

        private static PointSetting? GetApplicablePointSetting(
            IList<PointSetting> settings,
            DateTime billDate)
        {
            if (settings == null || settings.Count == 0)
            {
                return null;
            }

            return settings
                .Where(x =>
                    x.IsActive &&
                    x.StartDate.Date <= billDate &&
                    x.EndDate.Date >= billDate)
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefault();
        }

        private async Task UpsertPointTransactionAsync(
            Sales sale,
            int earnedPoints,
            int redeemedPoints)
        {
            if (sale == null || !sale.CustomerId.HasValue)
            {
                return;
            }

            var pointRepo = _unitOfWork.GetRepository<PointTransaction>();
            var existing = await pointRepo.Query()
                .FirstOrDefaultAsync(x => x.SaleId == sale.Id);

            if (existing == null)
            {
                if (earnedPoints > 0 || redeemedPoints > 0)
                {
                    pointRepo.Add(new PointTransaction
                    {
                        CustomerId = sale.CustomerId.Value,
                        EarnedPoints = earnedPoints,
                        RedeemedPoints = redeemedPoints,
                        SaleAmount = sale.TotalPayable,
                        SaleId = sale.Id,
                        TransactionDate = sale.BillDate ?? DateTime.Now
                    });
                }
                return;
            }

            if (earnedPoints <= 0 && redeemedPoints <= 0)
            {
                pointRepo.Delete(existing);
                return;
            }

            var hasChanges = false;
            if (existing.CustomerId != sale.CustomerId.Value)
            {
                existing.CustomerId = sale.CustomerId.Value;
                hasChanges = true;
            }

            if (existing.EarnedPoints != earnedPoints)
            {
                existing.EarnedPoints = earnedPoints;
                hasChanges = true;
            }

            if (existing.RedeemedPoints != redeemedPoints)
            {
                existing.RedeemedPoints = redeemedPoints;
                hasChanges = true;
            }

            if (existing.SaleAmount != sale.TotalPayable)
            {
                existing.SaleAmount = sale.TotalPayable;
                hasChanges = true;
            }

            var txDate = sale.BillDate ?? DateTime.Now;
            if (existing.TransactionDate != txDate)
            {
                existing.TransactionDate = txDate;
                hasChanges = true;
            }

            if (hasChanges)
            {
                pointRepo.Update(existing);
            }
        }

        private static string BuildPointMessage(
            int earnedPoints,
            int redeemedPoints,
            decimal redeemedAmount,
            string redeemMessage)
        {
            if (redeemedPoints > 0 && earnedPoints > 0)
            {
                return $"Redeemed {redeemedPoints} points (Rs. {redeemedAmount:F2}) and earned {earnedPoints} points.";
            }

            if (redeemedPoints > 0)
            {
                return $"Redeemed {redeemedPoints} points (Rs. {redeemedAmount:F2}).";
            }

            if (!string.IsNullOrWhiteSpace(redeemMessage))
            {
                if (earnedPoints > 0)
                {
                    return $"{redeemMessage} Earned {earnedPoints} points on this sale.";
                }

                return redeemMessage;
            }

            if (earnedPoints > 0)
            {
                return $"Earned {earnedPoints} points on this sale.";
            }

            return string.Empty;
        }

        private static string GetCashbackUsageStatus(decimal earned, decimal used)
        {
            if (earned <= 0)
            {
                return "No cashback earned yet.";
            }

            if (used <= 0)
            {
                return "Cashback earned but not used yet.";
            }

            if (used < earned)
            {
                return "Partially used cashback.";
            }

            if (used == earned)
            {
                return "Fully used cashback.";
            }

            return "Used more than earned cashback (additional advance adjusted).";
        }
    }
}
