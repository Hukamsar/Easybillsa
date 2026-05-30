using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class Country : BaseEntity
    {
        public int Id {  get; set; }
        public string Name { get; set; }
    }
}
