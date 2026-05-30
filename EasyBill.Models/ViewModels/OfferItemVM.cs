using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class OfferItemVM
    {
        public int Id { get; set; }
        public int OfferId { get; set; } 
        public int ItemId { get; set; } 
        public int? FreeItemId { get; set; } 
        public int BuyQty { get; set; }
        public int FreeQty { get; set; }
        public decimal fixedPrice { get; set; }
        public string? ComboGroupId { get; set; }
    }
}
