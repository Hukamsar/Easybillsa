using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class Gstr1DetailVM
    {
        public int SrNo { get; set; }
        public string GroupName { get; set; }

        public string CustomerName { get; set; }
        public string GSTIN { get; set; }
        public DateTime? BillDate { get; set; }
        public string BillNo { get; set; }

        public decimal InvoiceValue { get; set; }
        public string SupplyType { get; set; } // Local / Central

        public string HSN { get; set; }
        public string QtyDisplay { get; set; }

        public decimal Amount { get; set; }
        public decimal Taxable { get; set; }

        public decimal SGSTPer { get; set; }
        public decimal SGST { get; set; }

        public decimal CGSTPer { get; set; }
        public decimal CGST { get; set; }

        public decimal IGSTPer { get; set; }
        public decimal IGST { get; set; }

        public decimal Cess { get; set; }
        public decimal TotalGST { get; set; }
    }
}
