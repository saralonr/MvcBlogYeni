using MvcBlogYeni.Models.ORM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Security;
using System.Web.Helpers;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;
using System.Text;
using SecurityCryptoLibrary;

namespace MvcBlogYeni.Controllers
{
    public class CaptchaResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("error-codes")]
        public List<string> ErrorCodes { get; set; }
    }

    public class UyeController : Controller
    {
        // GET: Uye
        mvcblogEntities db = new mvcblogEntities();
        public static string dogrulamaSifresi;
        Random rnd = new Random();
        public ActionResult Index(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Home");

            var uye = db.Uye.Where(x => x.UyeID == id).SingleOrDefault();
            if (uye == null)
                return RedirectToAction("Hata404", "Home");
            return View(uye);
        }
        //[GirisKontrol]
        public ActionResult Login()
        {
            if (Session["uyeid"] != null && Request.Cookies["membid"] != null && Session["yetkiid"] != null)
                return RedirectToAction("Index", "Home");
            else if (Session["uyeid"] == null && Request.Cookies["membid"] != null)
                return RedirectToAction("Index", "Home");
            return View();
        }
        [HttpPost]
        public ActionResult Login(Uye uye)
        {
            if (String.IsNullOrEmpty(uye.KullaniciAdi) || String.IsNullOrEmpty(uye.Sifre))
            {
                TempData["HataliGiris"] = "Kullanıcı adı ya da şifre boş bırakılamaz!";
                return View();
            }

            uye.Sifre = Sec.MD5Hex(uye.Sifre).ToLower();
            var login = db.Uye.Where(u => u.KullaniciAdi == uye.KullaniciAdi).SingleOrDefault();
            if (login == null)
            {
                TempData["HataliGiris"] = "Girdiğiniz kullanıcı veya şifre hatalı";
                return View();
            }
            else if (login.KullaniciAdi == uye.KullaniciAdi && login.Sifre == uye.Sifre && login.Durum == true)
            {
                //Beni hatırla

                HttpCookie kuki = new HttpCookie("membad");
                kuki.Value = AESCryptoSec.Encrypt(login.KullaniciAdi,Helper.AesKey);
                kuki.Expires = DateTime.Now.AddDays(10);
                Response.Cookies.Add(kuki);

                HttpCookie kuki2 = new HttpCookie("membid");
                kuki2.Value = AESCryptoSec.Encrypt(login.UyeID.ToString(), Helper.AesKey);
                kuki2.Expires = DateTime.Now.AddDays(10);
                Response.Cookies.Add(kuki2);

                Helper.ActiveUser = login;
                Session["uyeid"] = login.UyeID;
                Session["kullaniciadi"] = login.KullaniciAdi;
                Session["yetkiid"] = login.YetkiID;

                return RedirectToAction("Index", "Home");
            }
            else
            {
                TempData["HataliGiris"] = "Girdiğiniz kullanıcı veya şifre hatalı";
                return View();
            }
        }
        public ActionResult Logout()
        {
            HttpCookie kuki = Response.Cookies["membad"];
            kuki.Value = "";
            kuki.Expires = DateTime.Now.AddDays(-10);
            Response.Cookies.Add(kuki);

            HttpCookie kuki2 = Response.Cookies["membid"];
            kuki2.Value = "";
            kuki2.Expires = DateTime.Now.AddDays(-10);
            Response.Cookies.Add(kuki2);

            Session["uyeid"] = null;
            Session["kullaniciadi"] = null;
            Session["yetkiid"] = null;
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Uye uye, HttpPostedFileBase Foto)
        {
            if (ModelState.IsValid)
            {
                if (uye == null || uye.Sifre.Length < 6 || uye.Sifre.Length > 15 || uye.KullaniciAdi.Length < 6 || uye.KullaniciAdi.Length > 15)
                    return RedirectToAction("Hata404", "Home");
                else
                {
                    if (Foto == null)
                        uye.Foto = "/Uploads/UyeFoto/avatar.png";
                    else
                    {
                        WebImage img = new WebImage(Foto.InputStream);
                        FileInfo fotoinfo = new FileInfo(Foto.FileName);

                        string newfoto = Guid.NewGuid().ToString() + fotoinfo.Extension;
                        img.Resize(300, 300);
                        img.Save("~/Uploads/UyeFoto/" + newfoto);
                        uye.Foto = "/Uploads/UyeFoto/" + newfoto;
                    }
                    var kullanici = db.Uye.Any(x => x.KullaniciAdi == uye.KullaniciAdi);
                    if (kullanici)
                    {
                        TempData["kullanicivar"] = "Bu isimle bir kullanıcı zaten kayıtlı.";
                        return View();
                    }

                    var captcha = Request.Form["g-recaptcha-response"];

                    const string secret = "{SECRETKEY}";

                    var restUrl = string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secret, captcha);

                    WebRequest req = WebRequest.Create(restUrl);
                    HttpWebResponse resp = req.GetResponse() as HttpWebResponse;

                    JsonSerializer serializer = new JsonSerializer();

                    CaptchaResult result = null;

                    using (var reader = new StreamReader(resp.GetResponseStream()))
                    {
                        string resultObject = reader.ReadToEnd();
                        result = JsonConvert.DeserializeObject<CaptchaResult>(resultObject);
                    }
                    if (!result.Success)
                    {
                        TempData["captchahata"] = "Lütfen bir robot olmadığınızı doğrulayın.";
                    }
                    else
                    {
                        uye.Sifre = Sec.MD5Hex(uye.Sifre).ToLower();
                        uye.Durum = true;
                        uye.YetkiID = 2;
                        db.Uye.Add(uye);
                        db.SaveChanges();

                        HttpCookie kuki = new HttpCookie("membad");
                        kuki.Value = uye.KullaniciAdi;
                        kuki.Expires = DateTime.Now.AddDays(10);
                        Response.Cookies.Add(kuki);

                        HttpCookie kuki2 = new HttpCookie("membid");
                        kuki2.Value = uye.UyeID.ToString();
                        kuki2.Expires = DateTime.Now.AddDays(10);
                        Response.Cookies.Add(kuki2);

                        Session["uyeid"] = uye.UyeID;
                        Session["kullaniciadi"] = uye.KullaniciAdi;
                        Session["yetkiid"] = uye.YetkiID;
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            return View(uye);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Home");

            var kisi = db.Uye.Where(x => x.UyeID == id).SingleOrDefault();
            if (Convert.ToInt32(Session["uyeid"]) != kisi.UyeID)
                return RedirectToAction("Hata404", "Home");

            return View(kisi);
        }
        [HttpPost]
        public ActionResult Edit(Uye uye, int id, HttpPostedFileBase Foto)
        {
            if (ModelState.IsValid)
            {

                var kisi = db.Uye.Where(u => u.UyeID == id).SingleOrDefault();
                //string dogrulama = Sec.MD5Hex(Convert.ToString(dogrulamaSifresi));
                //if (dogrulama != kisi.Sifre)
                //{
                //    TempData["dogrulamahata"] = "Şifrenizi yanlış girdiniz.";
                //    return View();
                //}
                if (Foto != null)
                {
                    if (kisi.Foto != "/Uploads/UyeFoto/avatar.png")
                    {
                        System.IO.File.Delete(Server.MapPath(kisi.Foto));
                        WebImage img = new WebImage(Foto.InputStream);
                        FileInfo fotoinfo = new FileInfo(Foto.FileName);

                        string newfoto = Guid.NewGuid().ToString() + fotoinfo.Extension;
                        img.Resize(300, 300);
                        img.Save("~/Uploads/UyeFoto/" + newfoto);
                        kisi.Foto = "/Uploads/UyeFoto/" + newfoto;
                    }
                    else
                    {
                        WebImage img = new WebImage(Foto.InputStream);
                        FileInfo fotoinfo = new FileInfo(Foto.FileName);

                        string newfoto = Guid.NewGuid().ToString() + fotoinfo.Extension;
                        img.Resize(300, 300);
                        img.Save("~/Uploads/UyeFoto/" + newfoto);
                        kisi.Foto = "/Uploads/UyeFoto/" + newfoto;
                    }
                }
                kisi.AdSoyad = uye.AdSoyad;
                if (uye.Sifre != null)
                {
                    if (uye.Sifre.Length > 5 || uye.Sifre.Length < 16)
                        kisi.Sifre = Sec.MD5Hex(uye.Sifre).ToLower();
                }
                kisi.Email = uye.Email;
                kisi.UyeHakkinda = uye.UyeHakkinda;
                db.SaveChanges();

                return Redirect("/Uye/Index/" + Session["uyeid"].ToString());

            }
            return View();
        }

        public ActionResult SifremiUnuttum()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SifremiUnuttum(Uye uye)
        {
            int kisiid = 0;
            if (uye != null)
            {
                var kisi = db.Uye.Where(x => x.Email == uye.Email).SingleOrDefault();
                if (kisi != null)
                {
                    kisiid = kisi.UyeID;
                    SendMail(kisi);
                }

                else
                {
                    TempData["mailbasarisiz1"] = "Bu e-maile sahip bir kullanıcı yok.";
                    return View();
                }
            }
            else
            {
                TempData["mailbasarisiz2"] = "E-mailizi yazınız.";
                return View();
            }
            Session["sifresifirlayanid"] = kisiid;
            return Redirect("/Uye/SifirlamaKodu/" + kisiid);
        }

        [NonAction]
        private void SendMail(Uye uye)
        {

            try
            {
                // sifirlama kodu link sonuna eklenecek. ister tıklamaylayönlendirme ister kodu girin geçin gibi bir yönlendirme. sifirlamakodu ndan sonra yenisifre diye sayfa acilacak. o sayfanın parametresi sifirlamaKodu olacak. routueconfig i ona göre ayarla.
                string sifirlamaKodu = Guid.NewGuid().ToString();

                SmtpClient sc = new SmtpClient();
                sc.Port = 587;
                sc.Host = "smtp.gmail.com";
                sc.EnableSsl = true;
                sc.Credentials = new NetworkCredential("{MAILADRESI}", "{SIFRE}");

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("{MAILADRESI}", "MvcBlog");
                mail.To.Add(uye.Email);
                mail.Subject = "Şifre Sıfırlama MvcBlog";
                mail.IsBodyHtml = true;
                mail.Body = "Şifre sıfırlama e-maili talep ettiniz.Şifre sıfırlamak için geçici sıfırlama kodunuz : " + sifirlamaKodu + " http://localhost:64874/Uye/Index/" + uye.UyeID;
                sc.Send(mail);

                SifreSifirlama sf = new SifreSifirlama();
                sf.UyeID = uye.UyeID;
                sf.SifirlamaKodu = sifirlamaKodu;
                sf.Tarih = DateTime.Now;
                db.SifreSifirlama.Add(sf);
                db.SaveChanges();

            }
            catch (Exception)
            {
                TempData["mailbasarisiz3"] = "Şifre sıfırlama maili başarısız oldu.";
            }

        }
        public ActionResult SifirlamaKodu(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Home");
            if (id != Convert.ToInt32(Session["sifresifirlayanid"]))
                return RedirectToAction("Hata404", "Home");
            var uye = db.Uye.Where(x => x.UyeID == id).SingleOrDefault();
            return View(uye);
        }
        [HttpPost]
        public ActionResult SifirlamaKodu(SifreSifirlama sfr, Uye uye, int id)
        {
            if (sfr == null || id == 0)
            {
                TempData["kodhatali"] = "Hata var";
                return View();
            }
            var kod = db.SifreSifirlama.Where(z => z.UyeID == id).OrderByDescending(x => x.Tarih).Take(1).SingleOrDefault();
            if (kod.SifirlamaKodu != sfr.SifirlamaKodu || uye.KullaniciAdi != kod.Uye.KullaniciAdi)
            {
                TempData["hatalikod"] = "Şifre sıfırlama kodunuz yada kullanıcı adınız hatalı.";
                return View(kod.Uye);
            }
            else
                return Redirect("/Uye/YeniSifre/" + kod.UyeID);
        }
        public ActionResult YeniSifre(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Home");
            if (id != Convert.ToInt32(Session["sifresifirlayanid"]))
                return RedirectToAction("Hata404", "Home");
            var uye = db.Uye.Where(x => x.UyeID == id).SingleOrDefault();
            return View(uye);
        }
        [HttpPost]
        public ActionResult YeniSifre(Uye uye, int id)
        {
            var kisi = db.Uye.Where(x => x.UyeID == id).SingleOrDefault();
            if (uye == null || id == 0 || uye.Sifre.Length < 6)
            {
                TempData["yenisifrehata"] = "Yeni şifre belirlenirken bir hata oluştu.";
                return View(kisi);
            }
            if (kisi == null)
                return RedirectToAction("Hata404", "Home");
            kisi.Sifre = Sec.MD5Hex(uye.Sifre).ToLower(); ;
            var sifirlamaKod = db.SifreSifirlama.Where(x => x.UyeID == id).ToList();
            foreach (var item in sifirlamaKod)
            {
                db.SifreSifirlama.Remove(item);
            }

            Bildirim bild = new Bildirim();
            bild.BildirimTuruID = 4;
            bild.Tarih = DateTime.Now;
            bild.BildirimIcerik = bild.Tarih.Value.ToShortDateString()+" tarihinde şifreniz değiştirildi.";
            bild.UyeID = kisi.UyeID;
            db.Bildirim.Add(bild);


            db.SaveChanges();
            Session.Abandon();
            return RedirectToAction("Login", "Uye");
        }
        public ActionResult Mesajlar(int? id)
        {
            if (id == null)
                return RedirectToAction("Index", "Home");

            if (Convert.ToInt32(Session["uyeid"]) != id)
                return RedirectToAction("Hata404", "Home");

            var uyeMsj = db.Mesaj.Where(x => x.Uye.UyeID == id || x.Uye1.UyeID == id).ToList();
            return View(uyeMsj);
        }
        [HttpGet]
        public JsonResult GelenMesajlariGetir()
        {
            int id = Convert.ToInt32(Session["uyeid"]);
            if (Session["uyeid"] == null)
                return Json("Oturum Açmadınız");

            List<Mesaj> gelenlist = new List<Mesaj>();
            gelenlist = db.Mesaj.Where(x => x.AliciID == id).OrderByDescending(x=>x.Tarih).ToList();

            return Json(
           (from obj in gelenlist select new { Baslik = obj.Baslik, AliciID = obj.AliciID, GondericiID = obj.GondericiID, Icerik = obj.Icerik, ID = obj.ID, OkunduBilgisi = obj.OkunduBilgisi, Tarih = obj.Tarih, KullaniciAdi = obj.Uye1.KullaniciAdi })
           , JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GidenMesajlariGetir()
        {
            int id = Convert.ToInt32(Session["uyeid"]);
            if (Session["uyeid"] == null)
                return Json("Oturum Açmadınız");

            List<Mesaj> gidenlist = new List<Mesaj>();
            gidenlist = db.Mesaj.Where(x => x.GondericiID == id).OrderByDescending(x=>x.Tarih).ToList();
            return Json(
          (from obj in gidenlist select new { Baslik = obj.Baslik, AliciID = obj.AliciID, GondericiID = obj.GondericiID, Icerik = obj.Icerik, ID = obj.ID, OkunduBilgisi = obj.OkunduBilgisi, Tarih = obj.Tarih, KullaniciAdiA = obj.Uye1.KullaniciAdi, KullaniciAdiG = obj.Uye.KullaniciAdi })
          , JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult MesajArama(string mesaj)
        {
            mesaj = mesaj.Replace("\\\"", "").Replace(@"""", "");
            List<Mesaj> msjList = db.Mesaj.Where(x => x.Icerik.Contains(mesaj) || x.Baslik.Contains(mesaj) || x.Uye1.KullaniciAdi.Contains(mesaj)).ToList();
            
            return Json(from obj in msjList select new { Baslik=obj.Baslik, Icerik=obj.Icerik, KullaniciAdiG = obj.Uye1.KullaniciAdi,KullaniciAdiA=obj.Uye.KullaniciAdi,Tarih= obj.Tarih, GondericiID=obj.GondericiID },JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult YeniMesaj(Mesaj msg)
        {

            if (Session["uyeid"] == null || msg == null)
                return RedirectToAction("Hata404", "Home");
            
            msg.GondericiID = Convert.ToInt32(Session["uyeid"]);
            msg.Tarih = DateTime.Now;
            db.Mesaj.Add(msg);

            Bildirim bild = new Bildirim();
            bild.BildirimTuruID = 2;
            Uye uye = db.Uye.Find(msg.GondericiID);
            bild.BildirimIcerik = "<b><a href='/Uye/Index/"+uye.UyeID+"'>" + uye.KullaniciAdi + "</a></b>" + " size bir mesaj gönderdi.";
            bild.Tarih = DateTime.Now;
            bild.UyeID = (int)msg.AliciID;
            // ark.Uye1 -> Uye2ID  oluyor. Bu metodu çalıştıran kişi karşı tarafa bildirim göndermiş oluyor.
            db.Bildirim.Add(bild);

            db.SaveChanges();
            return Redirect("/Uye/Mesajlar/" + msg.GondericiID);
        }

        public ActionResult ArkadasEkle(int? id)
        {
            if (Session["uyeid"] == null || id == null)
                return RedirectToAction("Hata404", "Home");

            UyeArkadas ark = new UyeArkadas();
            ark.Uye = db.Uye.Find(Convert.ToInt32(Session["uyeid"]));
            ark.Uye1ID = Convert.ToInt32(Session["uyeid"]);
            ark.Uye2ID = (int)id;
            ark.Durum = false;
            db.UyeArkadas.Add(ark);

            Bildirim bild = new Bildirim();
            bild.BildirimTuruID = 1;
            bild.BildirimIcerik = "<b><a href='/Uye/Index/" + ark.Uye.UyeID + "'>" + ark.Uye.KullaniciAdi + "</a></b>" + " arkadaşlık isteği gönderdi.";
            bild.Tarih = DateTime.Now;
            bild.UyeID = ark.Uye2ID;
            // ark.Uye -> Uye1ID  oluyor. Bu metodu çalıştıran kişi karşı tarafa bildirim göndermiş oluyor.
            db.Bildirim.Add(bild);

            db.SaveChanges();
            return Redirect("/Uye/Index/" + id);
        }

        public ActionResult ArkadaslikIptal(int? id)
        {
            if (Session["uyeid"] == null || id == null)
                return RedirectToAction("Hata404", "Home");

            UyeArkadas ark = db.UyeArkadas.AsEnumerable().FirstOrDefault(x=>x.Uye1ID== Convert.ToInt32(Session["uyeid"]) && x.Uye2ID == id);
            if (ark == null)
                return RedirectToAction("Hata404", "Home");

            db.UyeArkadas.Remove(ark);
            db.SaveChanges();
            return Redirect("/Uye/Index/" + id);
        }

        public ActionResult Onayla(int? id)
        {
            if (Session["uyeid"] == null || id == null)
                return RedirectToAction("Hata404", "Home");

            UyeArkadas ark = db.UyeArkadas.AsEnumerable().FirstOrDefault(x => x.Uye2ID == Convert.ToInt32(Session["uyeid"]) && x.Uye1ID == id && x.Durum == false);
            if (ark == null)
                return RedirectToAction("Hata404", "Home");
            ark.Durum = true;

            Bildirim bild = new Bildirim();
            bild.BildirimTuruID = 1;
            bild.BildirimIcerik = "<b><a href='/Uye/Index/" + ark.Uye1.UyeID + "'>" + ark.Uye1.KullaniciAdi + "</a></b>" + " arkadaşlık isteğini onayladı.";
            bild.Tarih = DateTime.Now;
            bild.UyeID = ark.Uye1ID;
            // ark.Uye1 -> Uye2ID  oluyor. Bu metodu çalıştıran kişi karşı tarafa bildirim göndermiş oluyor.
            db.Bildirim.Add(bild);
            db.SaveChanges();
            return Redirect("/Uye/Index/" + id);
        }

        public ActionResult ArkadasCikar(int? id)
        {
            if (Session["uyeid"] == null || id == null)
                return RedirectToAction("Hata404", "Home");

            UyeArkadas ark = db.UyeArkadas.AsEnumerable().FirstOrDefault(x => x.Uye1ID == Convert.ToInt32(Session["uyeid"]) && x.Uye2ID == id && x.Durum==true);
            if (ark == null)
                return RedirectToAction("Hata404", "Home");

            db.UyeArkadas.Remove(ark);
            db.SaveChanges();
            return Redirect("/Uye/Index/" + id);
        }

        public ActionResult Bildirimler(int? id)
        {
            if (Session["uyeid"] == null || Convert.ToInt32(Session["uyeid"]) != id || id==null)
                return RedirectToAction("Hata404", "Home");
            List<Bildirim> bd = db.Bildirim.Where(x=>x.UyeID==id).OrderByDescending(x=>x.Tarih).ToList();
            return View(bd);
        }
    }
}