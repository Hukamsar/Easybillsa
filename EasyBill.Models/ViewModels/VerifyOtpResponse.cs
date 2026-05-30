using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class VerifyOtpResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool IsNewCustomer { get; set; }
        public CustomerResponse Customer { get; set; }
        public string Token { get; set; }
    }


}
