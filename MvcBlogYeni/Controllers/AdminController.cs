using MvcBlogYeni.Models.ORM;
using MvcBlogYeni.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Web.Helpers;

namespace MvcBlogYeni.Controllers
{
    public class AdminController : Controller
    {
        mvcblogEntities db = new mvcblogEntities();
        // GET: Admin
        [AdminKontrol]
        public ActionResult Index()
        {
            if (Session["yetkiid"] == null && Request.Cookies["membid"] != null)
                return Redirect("/Uye/Login/");
            else if (Convert.ToInt32(Session["yetkiid"]) != 1)
                return Redirect("/Home/Index/");
            
            DTOSonHareketler son;
            List<DTOSonHareketler> hareketList = new List<DTOSonHareketler>();
            List<Yorum> yrm = db.Yorum.OrderByDescending(x => x.Tarih).Take(10).ToList();
            List<Makale> mk = db.Makale.OrderByDescending(x => x.Tarih).Take(10).ToList();
            foreach (var item in yrm)
            {
                son = new DTOSonHareketler();
                son.HareketAdi = item.Uye.KullaniciAdi + " isimli kullanıcı, \"" + item.Makale.Baslik + "\" makalesine yorum yaptı.";
                son.Tarih = (DateTime)item.Tarih;
                hareketList.Add(son);
            }
            foreach (var item in mk)
            {
                son = new DTOSonHareketler();
                son.HareketAdi = item.Uye.KullaniciAdi + " isimli kullanıcı, \"" + item.Baslik + "\" başlıklı bir makale yazdı.";
                son.Tarih = (DateTime)item.Tarih;
                hareketList.Add(son);
            }

            int[] adminIndexIstatistik = { db.Yorum.Count(), db.Makale.Count(), db.Uye.Count(), db.Kategori.Count() };
            ViewBag.Istatistikler = adminIndexIstatistik;
            ViewBag.YeniUyelikler = db.Uye.OrderByDescending(x => x.UyeID).Take(10).ToList();
            ViewBag.SonHareketler = hareketList.OrderByDescending(x => x.Tarih).Take(10).ToList();
            return View();
        }


        //KATEGORİ
        [AdminKontrol]
        public ActionResult Kategoriler()
        {
            var kategoriler = db.Kategori.OrderBy(x => x.KategoriID).ToList();
            return View(kategoriler);
        }

        [AdminKontrol]
        public ActionResult KategoriDetay(int? id)
        {
            if (id == null)
                return Redirect("/Admin/Kategoriler/");
            ViewBag.KategoriMakaleleri = db.Kategori.SingleOrDefault(x => x.KategoriID == id);
            var kategoriMakaleleri = db.Makale.Where(x => x.KategoriID == id).ToList();
            return View(kategoriMakaleleri);
        }
        [AdminKontrol]
        public ActionResult KategoriSil(int? id)
        {
            if (id == null)
            {
                TempData["kategorisilmehatasi1"] = "Bir hata oluştu.";
                return Redirect("/Admin/Kategoriler/");
            }

            Kategori ktg = db.Kategori.Find(id);
            if (ktg != null)
            {
                var makaleler = db.Makale.Where(x => x.KategoriID == ktg.KategoriID).ToList();
                if (makaleler != null)
                {
                    foreach (Makale item in makaleler)
                    {
                        var makaleEtiket = db.MakaleEtiket.Where(x=>x.MakaleID==item.MakaleID).ToList();
                        foreach (MakaleEtiket mkt in makaleEtiket)
                        {
                            db.MakaleEtiket.Remove(mkt);
                        }
                        db.Makale.Remove(item);
                    }
                }
                db.Kategori.Remove(ktg);
                db.SaveChanges();
                TempData["kategorisilindi"] = "Kategori başarıyla silindi.";
            }
            else
                TempData["kategorisilmehatasi2"] = "Böyle bir kategori yok.";
            return Redirect("/Admin/Kategoriler/");
        }

        [AdminKontrol]
        public ActionResult KategoriYeni()
        {
            return View();
        }
        [AdminKontrol]
        [HttpPost]
        public ActionResult KategoriYeni(Kategori ktg)
        {
            if (ktg != null)
            {
                if (ktg.KategoriAdi.Length<51)
                {
                    db.Kategori.Add(ktg);
                    db.SaveChanges();
                    TempData["kategorieklemebasarili"] = ktg.KategoriAdi+" kategorisi başarıyla eklendi.";
                }
                else
                {
                    TempData["kategorieklemehatasi1"] = "Kategori adı 50 karakterden fazla olamaz.";
                    return View();
                }
            }
            else
            {
                TempData["kategorieklemehatasi2"] = "Kategori adı boş bırakılamaz.";
                return View();
            }
            return Redirect("/Admin/Kategoriler/");
        }
        [AdminKontrol]
        public ActionResult KategoriDuzenle(int? id)
        {
            if (id==null)
                return Redirect("/Admin/Kategoriler/");

            var kategori = db.Kategori.FirstOrDefault(x=>x.KategoriID==id);
            if (kategori== null)
                return Redirect("/Admin/Kategoriler/");
            
            return View(kategori);
        }
        [AdminKontrol]
        [HttpPost]
        public ActionResult KategoriDuzenle(Kategori ktg,int? id)
        {
            if (id== null)
                return Redirect("/Admin/Kategoriler/");

            var kat = db.Kategori.Find(id);
            if (kat== null)
                return Redirect("/Admin/Kategoriler/");

            kat.KategoriAdi = ktg.KategoriAdi;
            db.SaveChanges();
            TempData["kategoriduzenlebasarili"] = "Kategori başarıyla güncellendi.";
            return Redirect("/Admin/Kategoriler/");

        }


        //MAKALE
        [AdminKontrol]
        public ActionResult Makaleler()
        {
            var makaleler = db.Makale.OrderBy(x => x.Tarih).ToList();
            return View(makaleler);
        }
        [AdminKontrol]
        public ActionResult MakaleYeni()
        {
            List<SelectListItem> ktgl = db.Kategori.Select(x => new SelectListItem
            {
                Value = x.KategoriID.ToString(),
                Text = x.KategoriAdi
            }).ToList();

            ViewBag.Kategoriler = ktgl;

            return View();
        }

        [AdminKontrol]
        [HttpPost]
        public ActionResult MakaleYeni(Makale mk, string etiketler, HttpPostedFileBase Fotograf)
        {
            if (ModelState.IsValid)
            {
                if (mk == null || mk.Icerik.Length < 100 || mk.Icerik.Length > 1000 || mk.Baslik.Length < 5 || mk.Baslik.Length > 500)
                    return RedirectToAction("Hata404", "Home");
                else
                {
                    if (Fotograf == null)
                        mk.Foto = "/Uploads/MakaleFoto/avatar.png";
                    else
                    {
                        WebImage img = new WebImage(Fotograf.InputStream);
                        byte[] boyut = img.GetBytes();
                        if (boyut.Length > 2097152)
                        {
                            TempData["fotoboyutbuyuk"] = "Yüklediğiniz fotoğraf boyutu 2MB'tan fazla olamaz.";
                            return View();
                        }
                        //getbytes ile boyutu büyük dosyada hata verdir.
                        FileInfo fotoinfo = new FileInfo(Fotograf.FileName);

                        string newfoto = Guid.NewGuid().ToString() + fotoinfo.Extension;
                        //Resim boyutlandırma problemli.
                        img.Resize(700, 350);
                        img.Save("~/Uploads/MakaleFoto/" + newfoto);
                        mk.Foto = "/Uploads/MakaleFoto/" + newfoto;
                    }

                    //var captcha = Request.Form["g-recaptcha-response"];

                    //const string secret = "{SECRETKEY}";

                    //var restUrl = string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secret, captcha);

                    //WebRequest req = WebRequest.Create(restUrl);
                    //HttpWebResponse resp = req.GetResponse() as HttpWebResponse;

                    //JsonSerializer serializer = new JsonSerializer();

                    //CaptchaResult result = null;

                    //using (var reader = new StreamReader(resp.GetResponseStream()))
                    //{
                    //    string resultObject = reader.ReadToEnd();
                    //    result = JsonConvert.DeserializeObject<CaptchaResult>(resultObject);
                    //}
                    //if (!result.Success)
                    //{
                    //    TempData["captchahata"] = "Lütfen bir robot olmadığınızı doğrulayın.";
                    //}
                    //else
                    //{
                    mk.Okunma = 0;
                    mk.UyeID = Convert.ToInt32(Session["uyeid"]);
                    mk.Durum = true;
                    mk.OnayDurum = true;
                    mk.Tarih = DateTime.Now;
                    db.Makale.Add(mk);
                    db.SaveChanges();
                    if (etiketler != null)
                    {
                        string[] etiketdizi = etiketler.Split(',');
                        foreach (var i in etiketdizi)
                        {
                            var yenietiket = new Etiket { EtiketAdi = i };
                            db.Etiket.Add(yenietiket);
                            db.SaveChanges();
                            MakaleEtiket me = new MakaleEtiket();
                            me.MakaleID = mk.MakaleID;
                            me.EtiketID = yenietiket.EtiketID;
                            mk.MakaleEtiket.Add(me);
                        }
                    }

                    db.SaveChanges();
                    TempData["makaleeklemebasarili"] = "Makaleniz başarıyla oluşturuldu.";
                    return RedirectToAction("Makaleler", "Admin");
                    //}
                }
            }
            return View(mk);
        }

        [AdminKontrol]
        public ActionResult MakaleSil(int? id)
        {
            //fotograf da silinsin.
            if (id == null)
            {
                TempData["makalesilmehatasi1"] = "Bir hata oluştu.";
                return Redirect("/Admin/Makaleler/");
            }

            Makale makale = db.Makale.Find(id);
            if (makale != null)
            {
                var makaleEtiket = db.MakaleEtiket.Where(x => x.MakaleID == makale.MakaleID).ToList();
                foreach (MakaleEtiket mkt in makaleEtiket)
                {
                    db.MakaleEtiket.Remove(mkt);
                }
                if (makale.Foto != "/Uploads/MakaleFoto/avatar.png")
                    System.IO.File.Delete(Server.MapPath(makale.Foto));

                db.Makale.Remove(makale);
                db.SaveChanges();
                
                
                TempData["makalesilindi"] = "Makale başarıyla silindi.";
            }
            else
                TempData["makalesilmehatasi2"] = "Böyle bir makale yok.";
            return Redirect("/Admin/Makaleler/");
        }

        [AdminKontrol]
        public ActionResult MakaleDetay(int? id)
        {
            if (id==null)
                return Redirect("/Admin/Makaleler/");

            var makale = db.Makale.FirstOrDefault(x=>x.MakaleID==id);
            if (makale == null)
                return Redirect("/Admin/Makaleler/");
            
            return View(makale);
        }

        [AdminKontrol]
        public ActionResult MakaleOnayBekleyen()
        {
            var makaleler = db.Makale.Where(x=>x.OnayDurum==false && x.Durum==true).ToList();
            return View(makaleler);
        }

        [AdminKontrol]
        public ActionResult MakaleOnay(int? id)
        {
            if (id == null)
            {
                TempData["makaleonayhatasi1"] = "Bir hata oluştu.";
                return Redirect("/Admin/MakaleOnayBekleyen/");
            }
            Makale makale = db.Makale.Find(id);
            if (makale != null)
            {
                makale.OnayDurum = true;
                db.SaveChanges();
                
                TempData["makaleonaylandi"] = "Makale başarıyla onaylandı.";
            }
            else
                TempData["makaleonayhatasi2"] = "Böyle bir makale yok.";
            return Redirect("/Admin/MakaleOnayBekleyen/");
        }
        [AdminKontrol]
        public ActionResult MakaleDuzenle(int? id)
        {
            if (id==null)
                return Redirect("/Admin/Makaleler/");

            var makale = db.Makale.FirstOrDefault(x=>x.MakaleID==id);

            if (makale == null)
                return Redirect("/Admin/Makaleler/");

            List<SelectListItem> ktgl = db.Kategori.Select(x => new SelectListItem
            {
                Value = x.KategoriID.ToString(),
                Text = x.KategoriAdi
            }).ToList();

            ViewBag.Kategoriler = ktgl;

            return View(makale);
        }

        [AdminKontrol]
        [HttpPost]
        public ActionResult MakaleDuzenle(Makale mk,int? id,string etiketler,HttpPostedFileBase Foto)
        {
            if (ModelState.IsValid)
            {
                if (mk == null || mk.Icerik.Length < 100 || mk.Icerik.Length > 1000 || mk.Baslik.Length < 5 || mk.Baslik.Length > 500)
                {
                    TempData["fotoboyutbuyuk"] = "İçerik 100-1000,başlık 5-500 karakter arası olmalı...";
                    return View();
                }
                else
                {
                    if (id==null)
                        return RedirectToAction("Hata404", "Home");

                    var makale = db.Makale.FirstOrDefault(x=>x.MakaleID==id);
                    if (makale == null)
                        return RedirectToAction("Hata404", "Home");

                    
                    if(Foto!= null)
                    {
                        System.IO.File.Delete(Server.MapPath(makale.Foto));

                        WebImage img = new WebImage(Foto.InputStream);
                        byte[] boyut = img.GetBytes();
                        if (boyut.Length > 2097152)
                        {
                            TempData["fotoboyutbuyuk"] = "Yüklediğiniz fotoğraf boyutu 2MB'tan fazla olamaz.";
                            return View();
                        }
                        FileInfo fotoinfo = new FileInfo(Foto.FileName);

                        string newfoto = Guid.NewGuid().ToString() + fotoinfo.Extension;
                        
                        img.Resize(700, 350);
                        img.Save("~/Uploads/MakaleFoto/" + newfoto);
                        makale.Foto = "/Uploads/MakaleFoto/" + newfoto;
                    }
                    makale.Icerik = mk.Icerik;
                    makale.Baslik = mk.Baslik;
                    makale.KategoriID = mk.KategoriID;
                   makale.Tarih = mk.Tarih;
                    db.SaveChanges();
                    if (etiketler != null)
                    {
                        var etikets = db.MakaleEtiket.Where(x=>x.MakaleID==makale.MakaleID).ToList();
                        foreach (MakaleEtiket item in etikets)
                        {
                            db.MakaleEtiket.Remove(item);
                        }
                        string[] etiketdizi = etiketler.Split(',');
                        foreach (var i in etiketdizi)
                        {
                            var yenietiket = new Etiket { EtiketAdi = i };
                            db.Etiket.Add(yenietiket);
                            db.SaveChanges();
                            MakaleEtiket me = new MakaleEtiket();
                            me.MakaleID = makale.MakaleID;
                            me.EtiketID = yenietiket.EtiketID;
                            makale.MakaleEtiket.Add(me);
                        }
                    }

                    db.SaveChanges();
                    TempData["makaleduzenlemebasarili"] = "Makaleniz başarıyla düzenlendi.";
                    
                }
            }
            return RedirectToAction("Makaleler", "Admin");
        }
    }
}