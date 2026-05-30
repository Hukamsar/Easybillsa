using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Model
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public DateTime TokenExpiry { get; set; }
    }
}
