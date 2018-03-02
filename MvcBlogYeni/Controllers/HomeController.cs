using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcBlogYeni.Models.ORM;
using MvcBlogYeni.Models.DTO;
using PagedList;
using PagedList.Mvc;
using System.Web.Helpers;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using SecurityCryptoLibrary;

namespace MvcBlogYeni.Controllers
{
    public class HomeController : Controller
    {
        mvcblogEntities db = new mvcblogEntities();
        // GET: Home
        public ActionResult Index(int Page=1)
        {
            if (Session["uyeid"] == null && Request.Cookies["membid"] != null)
            {
                Session["uyeid"] = AESCryptoSec.Decrypt(Request.Cookies["membid"].Value,Helper.AesKey);
                Session["kullaniciadi"] = AESCryptoSec.Decrypt(Request.Cookies["membad"].Value,Helper.AesKey);
                Helper.ActiveUser = db.Uye.Find(Convert.ToInt32(Session["uyeid"]));
            }
            var makale = db.Makale.Where(x => x.OnayDurum == true && x.Durum == true).OrderByDescending(x=> x.Tarih).ToPagedList(Page, 3);
            return View(makale);
        }
        
        public ActionResult MakaleDetay(int id)
        {
            ViewBag.Yorumlar = db.Yorum.Where(x => x.OnayDurum == true && x.Durum == true).OrderByDescending(z => z.Tarih).Where(x => x.MakaleID == id).ToList();
            ViewBag.YorumlarForAdmin = db.Yorum.Where(x => x.Durum == true).OrderByDescending(z => z.Tarih).Where(x => x.MakaleID == id).ToList();
            ViewBag.Makaleler = db.Makale.Where(x => x.OnayDurum == true && x.Durum == true).OrderByDescending(x=>x.MakaleEtiket.Count).Take(6).ToList();
            Makale makale = db.Makale.Where(x => x.MakaleID == id && x.Durum==true && x.OnayDurum==true).SingleOrDefault();
            makale.Okunma += 1;
            db.SaveChanges();
            if (makale == null)
            {
                return RedirectToAction("Hata404", "Home");
            }
            return View(makale);
        }

        public ActionResult EnPopulerMakaleler()
        {
            List<Makale> makale = db.Makale.Where(x=>x.OnayDurum==true && x.Durum==true).OrderByDescending(z => z.Okunma).Take(8).ToList();
            return View(makale);
        }

        public ActionResult EnPopulerKategoriler()
        {
            List<Kategori> kat = db.Kategori.OrderByDescending(z => z.Makale.Count).Take(6).ToList();
            return View(kat);
        }

        public ActionResult EditorunOnerileri()
        {
            List<Makale> makale = db.Makale.Where(x => x.OnayDurum == true && x.Durum == true).OrderByDescending(x => x.Yorum.Count).Take(3).ToList();
            return View(makale);
        }
        public ActionResult EnPopulerEtiketler()
        {
            var etm = db.Etiket.GroupBy(s => s.EtiketAdi).Where(w=>w.Count()>1).Select(x => new DTOEtiket
            {
                _EtiketAdi = x.Key
            }).Take(10).ToList();
            return View(etm);
        }
        public ActionResult Hata404()
        {
            return View();
        }

        [HttpPost]
        public ActionResult YorumYap(Yorum yrm)
        {
            if (yrm.Icerik == null || Session["uyeid"] == null)
                return View();

            Yorum nYorum;
            if (Convert.ToInt32(Session["yetkiid"]) == 1)
            {
                db.Yorum.Add( nYorum = new Yorum
                {
                    UyeID = Convert.ToInt32(Session["uyeid"]),
                    MakaleID = yrm.MakaleID,
                    Icerik = yrm.Icerik,
                    Uye = db.Uye.Find(Convert.ToInt32(Session["uyeid"])),
                Makale = db.Makale.Find(yrm.MakaleID),
                    Tarih = DateTime.Now,
                    OnayDurum = true,
                    Durum = true

                });
            }
            else
            {
                db.Yorum.Add(nYorum = new Yorum
                {
                    UyeID = Convert.ToInt32(Session["uyeid"]),
                    MakaleID = yrm.MakaleID,
                    Icerik = yrm.Icerik,
                    Uye = db.Uye.Find(Convert.ToInt32(Session["uyeid"])),
                    Makale = db.Makale.Find(yrm.MakaleID),
                    Tarih = DateTime.Now,
                    OnayDurum = false,
                    Durum = true

                });
            }

            Bildirim bild = new Bildirim();
            bild.BildirimTuruID = 3;
            bild.BildirimIcerik = "<b><a href='/Uye/Index/" + nYorum.Uye.UyeID + "'>" + nYorum.Uye.KullaniciAdi + "</a></b>" + "isimli kullanıcı, <a href='/Home/MakaleDetay/" + nYorum.MakaleID + "'>" + nYorum.Makale.Baslik + "</a>" + " başlıklı makalenize yorum yaptı.";
            bild.Tarih = DateTime.Now;
            bild.UyeID = nYorum.Makale.Uye.UyeID;
            // Bildirimi makaleyi yazan kişi adına kaydettik.
            db.Bildirim.Add(bild);

            db.SaveChanges();
            return Json("Basarili");
        }

        public ActionResult YorumSil(int? id)
        {
            if (id==null)
                return RedirectToAction("Hata404", "Home");

            Yorum yrm = db.Yorum.FirstOrDefault(x=>x.YorumID==id);
            if (yrm == null || Convert.ToInt32(Session["yetkiid"]) != 1)
                return RedirectToAction("Hata404", "Home");

            yrm.Durum = false;
            yrm.OnayDurum = false;
            db.SaveChanges();
            return Redirect("/Home/MakaleDetay/" + yrm.MakaleID);
        }

        public ActionResult YorumOnay(int? id)
        {
            if (id == null)
                return RedirectToAction("Hata404", "Home");

            Yorum yrm = db.Yorum.FirstOrDefault(x => x.YorumID == id);
            if (yrm == null || Convert.ToInt32(Session["yetkiid"]) != 1)
                return RedirectToAction("Hata404", "Home");
            
            yrm.OnayDurum = true;
            db.SaveChanges();
            return Redirect("/Home/MakaleDetay/" + yrm.MakaleID);
        }

        public ActionResult BlogAra(string Aranan)
        {
            var arananMakale = db.Makale.Where(x => x.Icerik.Contains(Aranan) || x.Baslik.Contains(Aranan)).ToList();
            return View(arananMakale.OrderByDescending(x=>x.Tarih));
        }

        public ActionResult YeniMakale()
        {
            if (Session["uyeid"] == null)
                return RedirectToAction("Login", "Uye");
            
            List<SelectListItem> ktgl = db.Kategori.Select(x => new SelectListItem
            {
                 Value = x.KategoriID.ToString(),
                 Text = x.KategoriAdi
            }).ToList();

            ViewBag.Kategoriler = ktgl;

            return View();
        }

        [HttpPost]
        public ActionResult YeniMakale(Makale mk, string etiketler, HttpPostedFileBase Fotograf)
        {
            if (ModelState.IsValid)
            {
                if (mk == null || mk.Icerik.Length < 100 || mk.Icerik.Length > 1000 || mk.Baslik.Length < 5 || mk.Baslik.Length > 500)
                    return RedirectToAction("Hata404", "Home");
                else if(Session["uyeid"] == null)
                    return RedirectToAction("Hata404", "Home");
                else
                {
                    if (Fotograf == null)
                        mk.Foto = "/Uploads/MakaleFoto/avatar.png";
                    else
                    {
                        WebImage img = new WebImage(Fotograf.InputStream);
                        byte[] boyut = img.GetBytes();
                        if (boyut.Length> 2097152)
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
                        mk.OnayDurum = false;
                        mk.Tarih = DateTime.Now;
                    db.Makale.Add(mk);
                    db.SaveChanges();
                    if (etiketler!= null)
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
                        return RedirectToAction("Index", "Home");
                    //}
                }
            }
            return View(mk);
        }
    }
}