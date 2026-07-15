using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ContraVM
    {
        public int Id { get; set; }
        public string VouncherNo { get; set; }
        public DateTime? Date { get; set; }
        public ContraCategory? Category { get; set; }
        public CashAndBank? CashAndBank { get; set; }
        public int? BankId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? Attachments { get; set; }
        public IFormFile? UploadAttachments { get; set; }
    }
}
