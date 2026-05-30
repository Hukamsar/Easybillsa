using Microsoft.AspNetCore.Mvc;
namespace EasyBill.UI.ViewComponents
{

    public class CategoryFormViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View();
        }
    }

}
