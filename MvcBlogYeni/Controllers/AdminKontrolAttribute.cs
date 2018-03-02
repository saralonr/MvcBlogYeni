using System;
using System.Web.Mvc;

namespace MvcBlogYeni.Controllers
{
    internal class AdminKontrol : ActionFilterAttribute
    {
        public string YonlendirilecekAdres { get; set; }
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if ( Helper.ActiveUser == null)
                YonlendirilecekAdres = "/Home/Index/";
            else if (Helper.ActiveUser.YetkiID != 1)
                YonlendirilecekAdres = "/Home/Index/";
            else
                return;
            filterContext.Result = new RedirectResult(YonlendirilecekAdres);
        }
    }
}