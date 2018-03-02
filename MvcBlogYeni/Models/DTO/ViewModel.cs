using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MvcBlogYeni.Models.ORM;

namespace MvcBlogYeni.Models.DTO
{
    public class ViewModel
    {
        public List<Makale> _Makale { get; set; }
        public List<Kategori> _Kategori { get; set; }
        public List<Etiket> _Etiket { get; set; }
        public List<Mesaj> _Mesaj { get; set; }
        public List<Uye> _Uye { get; set; }
        public List<Yorum> _Yorum { get; set; }
    }

    public class DTOEtiket
    {
        public int _EtiketID { get; set; }
        public string _EtiketAdi { get; set; }
    }

    public class DTOSonHareketler
    {
        public string HareketAdi { get; set; }
        public DateTime Tarih { get; set; }
    }
}