using MvcBlogYeni.Models.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MvcBlogYeni
{
    public class Helper
    {
        public const string AesKey = @"58J2XM(OQL91GR39";
        public static Uye ActiveUser
        {
            get
            {
                return HttpContext.Current.Session["ActiveUser"] as Uye;
            }
            set
            {
                HttpContext.Current.Session["ActiveUser"] = value;
            }
        }
        
       
    }
}