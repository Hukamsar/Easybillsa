using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class VerifyOtpRequest
    {
        public string MobileNumber { get; set; }
        public string Otp { get; set; }
    }
}
