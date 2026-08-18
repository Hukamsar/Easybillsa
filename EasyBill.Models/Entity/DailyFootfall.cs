using System;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.Models.Entity
{
    public class DailyFootfall
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string TenantId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        public int FootfallCount { get; set; }
    }
}
