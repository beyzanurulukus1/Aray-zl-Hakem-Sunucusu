using Microsoft.AspNetCore. Mvc;
using Koustec.Api.Models;
using Koustec. Api.Services;
using System. Collections.Generic; 
using System; 
using System.Linq;

namespace Koustec. Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class GorevController : ControllerBase
    {
        private readonly JsonLogService _jsonLogService;

        // ✅ BİRLEŞTİRİLMİŞ:  Tüm kilitlenme ve kamikaze kayıtlarını tek listede tutuyoruz
        private static readonly List<KilitlenmeKayiti> KilitlenmeKayitlari = new List<KilitlenmeKayiti>();

        public GorevController(JsonLogService jsonLogService)
        {
            _jsonLogService = jsonLogService;
        }

        // --- GÜNCELLENMIŞ:  Kilitlenme ve Kamikaze Bilgisi Gönderme (POST) ---
        // Python'un kontrol_merkezi. py'den gelen POST isteğini karşılıyor
        [HttpPost("kilitlenme_bilgisi")] 
        public IActionResult KilitlenmeBilgisiGonder([FromBody] KilitlenmeKayiti request)
        {
            if (request == null)
            {
                return BadRequest("Geçersiz istek.");
            }

            // Gelen veriyi listeye ekle (Hem kilitlenme hem kamikaze)
            KilitlenmeKayitlari.Add(request);
            
            // JSON'a kaydet
            _jsonLogService.OlayKaydet(request);
            
            return Ok(); 
        }

        // --- GÜNCELLENMIŞ:  Tüm Kilitlenme ve Kamikaze Kayıtlarını Alma (GET) ---
        // React ControlPanel'in istediği endpoint
        [HttpGet("kilitlenme_bilgisi")]
        public IActionResult GetKilitlenmeKayitlari()
        {
            return Ok(KilitlenmeKayitlari);
        }

        // --- (İSTEĞE BAĞLI) Sadece Kamikaze Olaylarını Filtrele ---
        [HttpGet("kamikaze_kayitlari")]
        public IActionResult GetKamikazeKayitlari()
        {
            // kilitlenmeTuru == 1 olan kayıtları filtrele
            var kamikazeKayitlari = KilitlenmeKayitlari.Where(k => k.kilitlenmeTuru == 1).ToList();
            return Ok(kamikazeKayitlari);
        }

        // --- (İSTEĞE BAĞLI) Sadece Kilitlenme Olaylarını Filtrele ---
        [HttpGet("kilitlenme_kayitlari")]
        public IActionResult GetOnlyKilitlenmeOlaylari()
        {
            // kilitlenmeTuru == 0 olan kayıtları filtrele
            var kilitlenmeKayitlari = KilitlenmeKayitlari.Where(k => k.kilitlenmeTuru == 0).ToList();
            return Ok(kilitlenmeKayitlari);
        }
        
        // --- QR Koordinatı Alma (GET /api/qr_koordinati) ---
        [HttpGet("qr_koordinati")]
        public IActionResult QrKoordinatiniAl()
        {
            var qrKonumu = new QrKoordinatiResponse
            {
                qrEnlem = 40.78,
                qrBoylam = 29.95 
            };

            return Ok(qrKonumu); 
        }

        // --- Hava Savunma Sistemi Koordinatları Alma (GET /api/hss_koordinatlari) ---
        [HttpGet("hss_koordinatlari")]
        public IActionResult HssKoordinatlariniAl()
        {
            bool hssAktifMi = true; 

            if (! hssAktifMi)
            {
                var bosCevap = new HssKoordinatlariResponse
                {
                    sunucusaati = GetCurrentSunucuSaati(),
                    hss_koordinat_bilgileri = new List<HssKoordinatBilgisi>()
                };
                return Ok(bosCevap);
            }

            var hssListesi = new List<HssKoordinatBilgisi>
            {
                new HssKoordinatBilgisi { id = 0, hssEnlem = 40.7810, hssBoylam = 29.9520, hssYaricap = 50 }, 
                new HssKoordinatBilgisi { id = 1, hssEnlem = 40.7790, hssBoylam = 29.9480, hssYaricap = 50 }, 
                new HssKoordinatBilgisi { id = 2, hssEnlem = 40.7830, hssBoylam = 29.9500, hssYaricap = 75 }, 
                new HssKoordinatBilgisi { id = 3, hssEnlem = 40.7770, hssBoylam = 29.9530, hssYaricap = 150 } 
            };

            var cevap = new HssKoordinatlariResponse
            {
                sunucusaati = GetCurrentSunucuSaati(),
                hss_koordinat_bilgileri = hssListesi
            };
            
            return Ok(cevap);
        }

        // --- Yardımcı Metot: Sunucu Saati ---
        private SunucuSaati GetCurrentSunucuSaati()
{
    // UTC+3 (Türkiye saati)
    DateTime now = DateTime.UtcNow. AddHours(3);
    return new SunucuSaati
    {
        gun = now.Day, 
        saat = now.Hour, 
        dakika = now.Minute, 
        saniye = now.Second, 
        milisaniye = now. Millisecond
    };
}
    }
}