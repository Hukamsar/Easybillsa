using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class PlanFeature
    {
        public int PlanId { get; set; }

        [ForeignKey(nameof(PlanId))]
        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public int FeatureId { get; set; }

        [ForeignKey(nameof(FeatureId))]
        public Feature? Feature { get; set; }
    }
}
