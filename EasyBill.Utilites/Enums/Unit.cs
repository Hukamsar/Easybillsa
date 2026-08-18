using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Utility.Enums
{
    public enum Unit
    {
        KG = 1,
        Ltr = 2,
        Meter = 3,
        SQM = 4,
        SFT = 5,
        Pcs = 6,
        Box = 7,
        Set = 8
    }
    public enum Types
    {
        Finished,
        Consumable,
        Raw
    }
    public enum HsnType
    {
        Goods = 1,
        Service = 2
    }
    public enum GSTType
    {
        Registered = 1,
        UnRegistered = 2
    }
    public enum CompanyType
    {
        [Display(Name = "Head Office (Centralized)")]
        HeadOffice = 1,
        
        [Display(Name = "Branch (Spoke)")]
        Branch = 2,

        [Display(Name = "Standalone (Individual)")]
        Standalone = 3
    }
    public enum WorkingStyle
    {
        Text = 1,
        Text2 = 2
    }
    public enum BusinessType
    {
        [Display(Name = "Grocery Store")]
        GroceryStore = 1,

        [Display(Name = "Pharmacy")]
        Pharmacy = 2,

        [Display(Name = "Food & Beverage")]
        FoodAndBeverage = 3
    }
    public enum CalenderType
    {
        English = 1
    }
    public enum TaxType
    {
        GST = 1,

    }
    public enum Purchasereturnreason
    {
        BreakAge = 1,
        Expiry = 2,
        Purchasereturn = 3
    }
    public enum OfferType
    {
        DisCountAmount = 1,
        DiscountPercent = 2,
        BuyXGetX = 3,
        Combo = 4,
        CashbackAmount = 5,
        CashbackPercent = 6
    }
    public enum Applicable
    {
        AllProduct = 1,
        Companies = 2,
        Categories = 3,
        Selected = 4,
        SubCategories = 5
    }
    public enum Initials
    {
        Mr = 1,
        Ms = 2,
        Dr = 3,
    }

    public enum TaxStatus
    {
        Taxable = 1,
        Exempted = 2,
        TaxPaid = 3
    }
    public enum Party
    {
        Customer = 1,
        Supplier = 2,
        Staff = 3,
        Other = 4

    }


    public enum GstStateCode
    {
        JammuAndKashmir = 1,          // 01
        HimachalPradesh = 2,          // 02
        Punjab = 3,                   // 03
        Chandigarh = 4,               // 04
        Uttarakhand = 5,              // 05
        Haryana = 6,                  // 06
        Delhi = 7,                    // 07
        Rajasthan = 8,                // 08
        UttarPradesh = 9,             // 09
        Bihar = 10,                   // 10
        Sikkim = 11,                  // 11
        ArunachalPradesh = 12,        // 12
        Nagaland = 13,                // 13
        Manipur = 14,                 // 14
        Mizoram = 15,                 // 15
        Tripura = 16,                 // 16
        Meghalaya = 17,               // 17
        Assam = 18,                   // 18
        WestBengal = 19,              // 19
        Jharkhand = 20,               // 20
        Odisha = 21,                  // 21
        Chhattisgarh = 22,            // 22
        MadhyaPradesh = 23,           // 23
        Gujarat = 24,                 // 24
        DadraNagarHaveliAndDamanDiu = 26, // 26
        Maharashtra = 27,             // 27
        AndhraPradeshOld = 28,        // 28
        Karnataka = 29,               // 29
        Goa = 30,                     // 30
        Lakshadweep = 31,             // 31
        Kerala = 32,                  // 32
        TamilNadu = 33,               // 33
        Puducherry = 34,              // 34
        AndamanNicobar = 35,          // 35
        Telangana = 36,               // 36
        AndhraPradesh = 37,           // 37
        Ladakh = 38                   // 38
    }
    public enum CustomerCategory
    {
        Retailer = 1,
        Stockist = 2,
        Distributor = 3,
        Hospital = 4
    }

    public enum CustomerStatus
    {
        Active = 1,
        Inactive = 2,
        Blocked = 3,
        Suspended = 4
    }
    public enum modeofpayment
    {
        Cash = 1,
        Bank= 2,

    }
    public enum WalletOwnerType
    {
        CustomerWallet = 1,
        DeliveryBoyWallet = 2
    }

    public enum WalletStatus
    {
        Active = 1,
        Inactive = 2,
        Blocked = 3
    }

    public enum WalletTransactionType
    {
        AdvanceDeposit = 1,
        RefundToWallet = 2,
        LoyaltyCashback = 3,
        MembershipCredit = 4,
        ManualCredit = 5,
        SaleDeduction = 6,
        CODCollection = 7,
        DailySettlement = 8,
        ExpenseAdjustment = 9,
        IncentiveCredit = 10,
        AlertSmsCharge = 11,
        AlertWhatsAppCharge = 12
    }
    public enum SalesTax
    {
        Inclusive = 1,
        Exclusive = 2
    }
    public enum PurchaseTax
    {
        Inclusive = 1,
        Exclusive = 2
    }
    public enum CashAndBank
    {
        Cash = 1,
        Bank = 2
    }
    public enum ContraCategory
    {
        Deposit = 1,
        Withdraw = 2
    }
}
