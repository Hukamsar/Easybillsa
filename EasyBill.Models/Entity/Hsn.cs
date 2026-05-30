using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Utility.Enums;

namespace AOne.Models.Entity
{
    public class Hsn:BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string HsnCode {  get; set; }
        public decimal SGST {  get; set; }
        public decimal CGST { get; set; }
        public decimal IGST { get; set; }
        public decimal Cess { get; set; }
        public HsnType? HsnType { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
