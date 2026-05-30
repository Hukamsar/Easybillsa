using AOne.Models.Entity;
using AOne.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Utility.Enums;

namespace EasyBill.Models.ViewModels
{
    public class PurchaseSettingVM
    {
        public int Id { get; set; }
        public string? ApplicationUserId { get; set; }
        public int MinPurchaseExpiryDays { get; set; }
        public PurchaseTax PurchaseTax { get; set; }
    }
}
