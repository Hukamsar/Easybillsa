using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class PharmacyDoctorVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNo { get; set; }
        public string? Address { get; set; }
        public string? RegistrationNo { get; set; }
        public string? Specialized { get; set; }
        public decimal Commission { get; set; } 
    }
}
