using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class StockApiController : ControllerBase
    {
        private readonly IStockRepository _stockservice;

        public StockApiController(IStockRepository stockservice)
        {
            _stockservice = stockservice;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _stockservice.GetAll();
            return Ok(data);
        }
    }
}
