using AOne.Models;
using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class Stock : BaseEntity
    {
        public int Id { get; set; }
        public int ItemMasterId { get; set; }
        [ForeignKey(nameof(ItemMasterId))]
        public ItemMaster ItemMaster { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public CategoryMaster Category { get; set; }
        public string ItemCode {  get; set; }
        public decimal Stocks { get; set; } 
        public string? Unit1 { get; set; }
        public string? Unit2 { get; set; }
    }
}
