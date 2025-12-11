using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR; 
using Koustec.Api.Models;
using Koustec.Api.Services; 
using Koustec.Api.Hubs; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent; // Hafıza yönetimi için gerekli
using System.Linq; // Listeleme işlemleri için

namespace Koustec.Api.Controllers
{
    [Route("api")] 
    [ApiController]
    public class TelemetriController : ControllerBase
    {
        private readonly ValidationService _validationService;
        private readonly IHubContext<TelemetriHub> _hubContext; 

        // --- ÖNEMLİ: SERVER HAFIZASI ---
        // Tüm takımların en son gönderdiği konumu burada saklarız.
        // ConcurrentDictionary, aynı anda birden fazla takım veri gönderirse hata çıkmasını engeller.
        // Anahtar: Takım Numarası, Değer: Konum Bilgisi
        private static readonly ConcurrentDictionary<int, KonumBilgisi> _takimKonumlari = new ConcurrentDictionary<int, KonumBilgisi>();

        public TelemetriController(ValidationService validationService, IHubContext<TelemetriHub> hubContext) 
        {
            _validationService = validationService;
            _hubContext = hubContext;
        }

        [HttpPost("telemetri_gonder")] 
        public async Task<IActionResult> TelemetriGonder([FromBody] TelemetriGonderRequest request) 
        {
            // 1. Doğrulama
            string validationError = _validationService.ValidateTelemetry(request);
            if (validationError != null)
            {
                return BadRequest(new { HataKodu = validationError });
            }
            
            // 2. Gelen Veriyi Hafızaya Kaydet (Diğer takımlara göndermek için)
            var guncelKonum = new KonumBilgisi
            {
                takim_numarasi = request.takim_numarasi,
                iha_enlem = request.iha_enlem,
                iha_boylam = request.iha_boylam,
                iha_irtifa = request.iha_irtifa,
                iha_dikilme = request.iha_dikilme,
                iha_yonelme = request.iha_yonelme,
                iha_yatis = request.iha_yatis,
                iha_hizi = request.iha_hiz,
                zaman_farki = 0 // Anlık veri olduğu için fark 0 kabul edilir
            };

            // Listeyi güncelle veya ekle
            _takimKonumlari.AddOrUpdate(request.takim_numarasi, guncelKonum, (key, oldValue) => guncelKonum);

            // 3. Hakem Ekranına (Client) Canlı Yayın Yap (SignalR)
            await _hubContext.Clients.All.SendAsync("ReceiveTelemetry", request); 
            
            // 4. TAKIMA CEVAP OLUŞTUR (GERÇEK VERİ PAYLAŞIMI)
            // Hafızadaki tüm uçakları al, ama isteği gönderen takımın kendisini listeden çıkar.
            var digerUcaklar = _takimKonumlari.Values
                .Where(k => k.takim_numarasi != request.takim_numarasi)
                .ToList();

            var response = new TelemetriGonderResponse
            {
                sunucusaati = GetCurrentSunucuSaati(),
                konumBilgileri = digerUcaklar // Artık gerçek veri dönüyor!
            };

            return Ok(response);
        }
        
        // Sunucu Saati Yardımcısı
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