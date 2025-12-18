using Microsoft.AspNetCore. SignalR;
using System.Threading.Tasks;
using System.Collections. Concurrent;
using System.Collections.Generic;
using System.Linq;
using System;
using Koustec.Api.Models;
using Koustec.Api.Services;

namespace Koustec.Api. Hubs
{
    public class TelemetriHub :  Hub
    {
        private readonly JsonLogService _jsonLogService;
        
        // Tüm takımların verileri (hafızada)
        private static readonly ConcurrentDictionary<int, TakimVerisi> _takimVerileri = new();
        
        // Bağlı takımların connection ID'leri
        private static readonly ConcurrentDictionary<string, int> _bagliTakimlar = new();

        public TelemetriHub(JsonLogService jsonLogService)
        {
            _jsonLogService = jsonLogService;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🚁 PYTHON'DAN TELEMETRİ AL
        // ═══════════════════════════════════════════════════════════════
        public async Task<object> TelemetriGonder(TelemetriGonderRequest request)
        {
            DateTime simdikiZaman = DateTime. UtcNow. AddHours(3);

            // 1. JSON'a kaydet
            _jsonLogService.TelemetriKaydet(request);

            // 2. Takımı kaydet
            _bagliTakimlar[Context.ConnectionId] = request. takim_numarasi;

            // 3. Veriyi hafızaya al
            var yeniVeri = new TakimVerisi
            {
                Konum = new KonumBilgisi
                {
                    takim_numarasi = request.takim_numarasi,
                    iha_enlem = request.iha_enlem,
                    iha_boylam = request.iha_boylam,
                    iha_irtifa = request.iha_irtifa,
                    iha_dikilme = request. iha_dikilme,
                    iha_yonelme = request. iha_yonelme,
                    iha_yatis = request.iha_yatis,
                    iha_hizi = request.iha_hiz,
                    zaman_farki = 0
                },
                SonGuncellemeZamani = simdikiZaman
            };

            _takimVerileri. AddOrUpdate(request.takim_numarasi, yeniVeri, (key, old) => yeniVeri);

            // 4. Tüm takımların konumlarını hazırla
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
                zaman_farki = t.Key == request.takim_numarasi ? 0 
                    : (int)(simdikiZaman - t.Value.SonGuncellemeZamani).TotalMilliseconds
            }).ToList();

            // 5. Cevap oluştur
            var response = new
            {
                sunucusaati = new
                {
                    gun = simdikiZaman. Day,
                    saat = simdikiZaman.Hour,
                    dakika = simdikiZaman.Minute,
                    saniye = simdikiZaman.Second,
                    milisaniye = simdikiZaman. Millisecond
                },
                konumBilgileri = tumKonumlar
            };

            // 6. React'a da gönder (hakem ekranı)
            await Clients.All.SendAsync("ReceiveTelemetry", request);
            await Clients.All.SendAsync("ReceiveAllTeams", response);
            // 7. Log
            Console.WriteLine($"🔌 [WebSocket] T{request.takim_numarasi} veri gönderdi | " +
                              $"📤 {tumKonumlar. Count} takım | " +
                              $"[{string.Join(", ", tumKonumlar.Select(r => $"T{r.takim_numarasi}"))}]");

            // 8. Python'a cevap döndür
            return response;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔌 BAĞLANTI OLAYLARI
        // ═══════════════════════════════════════════════════════════════
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"🟢 Yeni bağlantı:  {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_bagliTakimlar.TryRemove(Context.ConnectionId, out int takimNo))
            {
                Console.WriteLine($"🔴 Takım {takimNo} bağlantısı koptu:  {Context.ConnectionId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }

    public class TakimVerisi
    {
        public KonumBilgisi Konum { get; set; }
        public DateTime SonGuncellemeZamani { get; set; }
    }
}