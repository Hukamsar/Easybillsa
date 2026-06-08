using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class PointSettingVm
    {
        public int Id { get; set; }
        public decimal MinAmountToEarn { get; set; }
        public decimal EarnPerAmount { get; set; }
        public decimal PointValueInRs { get; set; }
        public bool AllowRedemption { get; set; }
        public int MinPointsToRedeem { get; set; }
        public decimal EarningMultiplier { get; set; } = 1.0m;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
