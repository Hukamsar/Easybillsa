using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.Models.Entity
{
    public class SubscriptionPlan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string PlanName { get; set; }

        public decimal MonthlyPrice { get; set; }

        public decimal YearlyPrice { get; set; }

        public int DailyCustomerLimit { get; set; }

        public int MaxDesktopLogins { get; set; }

        public int MaxMobileLogins { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<PlanFeature>? PlanFeatures { get; set; }
    }
}
