using Microsoft.AspNetCore.Mvc;
using Koustec.Api.Models;
using System.Collections.Generic; 
using System; 
using System.Linq;

namespace Koustec.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class GorevController : ControllerBase
    {
        // Kilitlenme ve Kamikaze kayıtlarını Server'da tutan in-memory depolama.
        private static readonly List<KilitlenmeRequest> KilitlenmeKayitlari = new List<KilitlenmeRequest>();
        private static readonly List<KamikazeRequest> KamikazeKayitlari = new List<KamikazeRequest>();


        // --- Kilitlenme Bilgisi Gönderme (POST) ---
        [HttpPost("kilitlenme_bilgisi")] 
        public IActionResult KilitlenmeBilgisiGonder([FromBody] KilitlenmeRequest request)
        {
            KilitlenmeKayitlari.Add(request);
            return Ok(); 
        }

        // --- Kamikaze Bilgisi Gönderme (POST) ---
        [HttpPost("kamikaze_bilgisi")]
        public IActionResult KamikazeBilgisiGonder([FromBody] KamikazeRequest request)
        {
            KamikazeKayitlari.Add(request);
            return Ok();
        }

        // --- Kilitlenme Kayıtlarını Alma (GET /api/kilitlenme_kayitlari) ---
        [HttpGet("kilitlenme_kayitlari")]
        public IActionResult GetKilitlenmeKayitlari()
        {
            return Ok(KilitlenmeKayitlari);
        }

        // --- Kamikaze Kayıtlarını Alma (GET /api/kamikaze_kayitlari) ---
        [HttpGet("kamikaze_kayitlari")]
        public IActionResult GetKamikazeKayitlari()
        {
            return Ok(KamikazeKayitlari);
        }
        
        
        // --- QR Koordinatı Alma (GET /api/qr_koordinati) ---
        [HttpGet("qr_koordinati")]
        public IActionResult QrKoordinatiniAl()
        {
            var qrKonumu = new QrKoordinatiResponse
            {
                // Kocaeli/İzmit civarı
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

            if (!hssAktifMi)
            {
                var bosCevap = new HssKoordinatlariResponse
                {
                    sunucusaati = GetCurrentSunucuSaati(),
                    hss_koordinat_bilgileri = new List<HssKoordinatBilgisi>()
                };
                return Ok(bosCevap);
            }

            // HSS aktif ise, koordinatları Kocaeli merkezinin etrafına yerleştir.
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
            DateTime now = DateTime.UtcNow;
            return new SunucuSaati
            {
                gun = now.Day, saat = now.Hour, dakika = now.Minute, 
                saniye = now.Second, milisaniye = now.Millisecond
            };
        }
    }
}