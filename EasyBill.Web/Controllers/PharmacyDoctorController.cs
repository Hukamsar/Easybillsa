using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class PharmacyDoctorController : Controller
    {
        private readonly IPharmacyDoctorRepository pharmacyDoctorRepository;
        public PharmacyDoctorController(IPharmacyDoctorRepository pharmacyDoctorRepository)
        {
            this.pharmacyDoctorRepository = pharmacyDoctorRepository;
        }

        public async Task<IActionResult> Index()
        {
            var data  = await pharmacyDoctorRepository.GetAll();
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> GetDoctorByMobile(string phoneNo)
        {
            var doctor = await pharmacyDoctorRepository.GetDoctorByMobileno(phoneNo); // your service logic

            if (doctor == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                data = new
                {
                    doctor.Name,
                    doctor.Id,
                    doctor.PhoneNo,
                    doctor.RegistrationNo
                }
            });
        }
        [HttpPost]
        public async Task<IActionResult> CreateDoctor([FromBody] PharmacyDoctorVM Vm)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid doctor data");
            var Doctor = new PharmacyDoctor
            {
                Name = Vm.Name,
                PhoneNo = Vm.PhoneNo,
                Address = Vm.Address, 
                RegistrationNo = Vm.RegistrationNo,
                Specialized = Vm.Specialized,
                Commission = Vm.Commission,
            };

            var result = await pharmacyDoctorRepository.Create(Doctor);

            return Json(new
            {
                success = true,
                data = new
                {
                    result.Id,
                    result.Name,
                    result.PhoneNo,
                    result.RegistrationNo
                }
            });
        }
    }
}
