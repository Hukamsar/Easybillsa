using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class StockController : Controller
    {
        private readonly IStockRepository _stockservice;
        public StockController(IStockRepository stockservice) { _stockservice = stockservice; }
        public async Task<IActionResult> Index()
        {
            var data = await _stockservice.GetAll(); 
            return View(data);
        }
    }
}
