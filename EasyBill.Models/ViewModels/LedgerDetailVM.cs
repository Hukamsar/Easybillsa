using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class LedgerDetailVM
    {

        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Particular { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }   // ✅ ADD
    }
}
