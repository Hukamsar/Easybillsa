using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ModeOfPaymentVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public IEnumerable<ModeOfPayment> ModeOfPayments { get; set; }
        public modeofpayment? PaymentType { get; set; }
        public int? BankId { get; set; }
    }
}
