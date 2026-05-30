using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPharmacyDoctorRepository
    {
        Task<IList<PharmacyDoctor>> GetAll();
        Task<PharmacyDoctor> Create(PharmacyDoctor model);
        Task<PharmacyDoctor> GetById(int? Id);
        Task<PharmacyDoctor> GetDoctorByMobileno(string? phoneNo);
        Task<PharmacyDoctor> Update(PharmacyDoctor model);
        Task Delete(PharmacyDoctor model);
    }
}
