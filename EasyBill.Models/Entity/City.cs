using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class City : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CountryId {  get; set; }
        [ForeignKey(nameof(CountryId))]
        public Country Country { get; set; }
        public int StateId {  get; set; }
        [ForeignKey(nameof(StateId))]
        public State State { get; set; }
      
    }
}
