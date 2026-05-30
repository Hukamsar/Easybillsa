using Microsoft.AspNetCore.Mvc;

namespace AOneWeb.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult UsersList()
        {
            return View();
        }
        public IActionResult RoleList()
        {
            return View();
        }
        public IActionResult UserProfile()
        {
            return View();
        }
    }
}
