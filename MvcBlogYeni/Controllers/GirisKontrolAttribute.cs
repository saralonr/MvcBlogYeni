using System;
using System.Web.Mvc;

namespace MvcBlogYeni.Controllers
{
    internal class GirisKontrol : ActionFilterAttribute
    {
            public string YonlendirilecekAdres { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Helper.ActiveUser == null)
                YonlendirilecekAdres = "/";

            if (Helper.ActiveUser.YetkiID != 1)
                YonlendirilecekAdres = "/";
            filterContext.Result = new RedirectResult(YonlendirilecekAdres);
        }
    }

}