using EasyBill.UI.Service;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class BarcodeController : Controller
    {
        private readonly BarcodeService _barcodeService;

        public BarcodeController()
        {
            _barcodeService = new BarcodeService();
        }

       
        public IActionResult Generate(string code = "ABC123")
        {
            var imageBytes = _barcodeService.GenerateBarcode(code);
            return File(imageBytes, "image/png");
        }
    }
}
