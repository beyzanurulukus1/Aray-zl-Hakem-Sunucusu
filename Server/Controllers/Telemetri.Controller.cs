using Microsoft.AspNetCore. Mvc;
using Microsoft.AspNetCore.SignalR;
using Koustec.Api.Models;
using Koustec. Api.Services;
using Koustec.Api.Hubs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace Koustec.Api. Controllers
{
    [Route("api")]
    [ApiController]
    public class TelemetriController : ControllerBase
    {
        private readonly ValidationService _validationService;
        private readonly IHubContext<TelemetriHub> _hubContext;
        private readonly JsonLogService _jsonLogService;

        private static readonly ConcurrentDictionary<int, TakimVerisi> _takimVerileri = new ConcurrentDictionary<int, TakimVerisi>();

        public TelemetriController(
            ValidationService validationService,
            IHubContext<TelemetriHub> hubContext,
            JsonLogService jsonLogService)
        {
            _validationService = validationService;
            _hubContext = hubContext;
            _jsonLogService = jsonLogService;
        }

        [HttpPost("telemetri_gonder")]
        public async Task<IActionResult> TelemetriGonder([FromBody] TelemetriGonderRequest request)
        {
            // 1. DOĞRULAMA
            string validationError = _validationService.ValidateTelemetry(request);
            if (validationError != null)
            {
                return BadRequest(new { HataKodu = validationError });
            }

            // 2. JSON'A KAYDET
            _jsonLogService.TelemetriKaydet(request);

            // 3. ŞU ANKİ ZAMAN
            DateTime simdikiZaman = DateTime.UtcNow. AddHours(3); // Türkiye saati

            // 4. GELEN VERİYİ SUNUCU HAFIZASINA KAYDET
            var yeniVeri = new TakimVerisi
            {
                Konum = new KonumBilgisi
                {
                    takim_numarasi = request.takim_numarasi,
                    iha_enlem = request.iha_enlem,
                    iha_boylam = request. iha_boylam,
                    iha_irtifa = request.iha_irtifa,
                    iha_dikilme = request.iha_dikilme,
                    iha_yonelme = request.iha_yonelme,
                    iha_yatis = request.iha_yatis,
                    iha_hizi = request.iha_hiz,
                    zaman_farki = 0
                },
                SonGuncellemeZamani = simdikiZaman
            };

            _takimVerileri. AddOrUpdate(request.takim_numarasi, yeniVeri, (key, oldValue) => yeniVeri);

            // 5. HAKEM EKRANINA CANLI YAYIN
            await _hubContext.Clients. All.SendAsync("ReceiveTelemetry", request);

            // 6. TÜM TAKIMLARIN VERİLERİNİ HAZIRLA (KENDİSİ DAHİL)
            var tumKonumlar = new List<KonumBilgisi>();

            foreach (var takimKaydi in _takimVerileri)
            {
                var takimVerisi = takimKaydi.Value;

                int zamanFarki = (int)(simdikiZaman - takimVerisi. SonGuncellemeZamani).TotalMilliseconds;
                
                if (takimKaydi.Key == request. takim_numarasi)
                    zamanFarki = 0;

                var konum = new KonumBilgisi
                {
                    takim_numarasi = takimVerisi.Konum. takim_numarasi,
                    iha_enlem = takimVerisi.Konum. iha_enlem,
                    iha_boylam = takimVerisi.Konum. iha_boylam,
                    iha_irtifa = takimVerisi.Konum.iha_irtifa,
                    iha_dikilme = takimVerisi.Konum.iha_dikilme,
                    iha_yonelme = takimVerisi. Konum.iha_yonelme,
                    iha_yatis = takimVerisi.Konum. iha_yatis,
                    iha_hizi = takimVerisi. Konum.iha_hizi,
                    zaman_farki = zamanFarki
                };

                tumKonumlar. Add(konum);
            }

            // 7. CEVAP OLUŞTUR
            var response = new TelemetriGonderResponse
            {
                sunucusaati = GetCurrentSunucuSaati(),
                konumBilgileri = tumKonumlar
            };

            // 8. DEBUG LOG
            Console.WriteLine($"📥 T{request.takim_numarasi} veri gönderdi | " +
                              $"📤 {tumKonumlar. Count} takım verisi döndürüldü | " +
                              $"[{string.Join(", ", tumKonumlar.Select(r => $"T{r.takim_numarasi}"))}]");

            return Ok(response);
        }

        [HttpPost("temizle")]
        public IActionResult EskiVerileriTemizle()
        {
            var simdikiZaman = DateTime.UtcNow.AddHours(3);
            var silinecekler = _takimVerileri
                .Where(kvp => (simdikiZaman - kvp.Value. SonGuncellemeZamani).TotalSeconds > 10)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var takim in silinecekler)
            {
                _takimVerileri. TryRemove(takim, out _);
            }

            return Ok(new { SilinenTakimlar = silinecekler. Count });
        }

        // TÜM TAKIMLARI GETİR (GET endpoint - opsiyonel)
        [HttpGet("tum_takimlar")]
        public IActionResult TumTakimlariGetir()
        {
            var simdikiZaman = DateTime.UtcNow.AddHours(3);
            
            var tumKonumlar = _takimVerileri. Select(t => new KonumBilgisi
            {
                takim_numarasi = t.Value. Konum.takim_numarasi,
                iha_enlem = t. Value.Konum.iha_enlem,
                iha_boylam = t.Value. Konum.iha_boylam,
                iha_irtifa = t.Value. Konum.iha_irtifa,
                iha_dikilme = t.Value.Konum.iha_dikilme,
                iha_yonelme = t.Value.Konum.iha_yonelme,
                iha_yatis = t. Value.Konum.iha_yatis,
                iha_hizi = t.Value. Konum.iha_hizi,
                zaman_farki = (int)(simdikiZaman - t.Value. SonGuncellemeZamani).TotalMilliseconds
            }).ToList();

            return Ok(new
            {
                sunucusaati = GetCurrentSunucuSaati(),
                toplam_takim = tumKonumlar.Count,
                konumBilgileri = tumKonumlar
            });
        }

        private SunucuSaati GetCurrentSunucuSaati()
        {
            DateTime now = DateTime. UtcNow. AddHours(3); // Türkiye saati
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

    public class TakimVerisi
    {
        public KonumBilgisi Konum { get; set; }
        public DateTime SonGuncellemeZamani { get; set; }
    }
}