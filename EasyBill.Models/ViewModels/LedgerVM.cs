using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class LedgerVM
    {
        public int Id { get; set; }
        public string PartyName { get; set; }
        public string Type { get; set; }

        public decimal Debit { get; set; }   // ✅ ADD
        public decimal Credit { get; set; }  // ✅ ADD
        public string? Remarks { get; set; }
    }
}
