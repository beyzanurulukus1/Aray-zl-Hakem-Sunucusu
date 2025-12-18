using System;
using System.Collections.Generic;
using System.IO;
using System.Text. Json;
using Koustec.Api.Models;

namespace Koustec.Api. Services
{
    public class JsonLogService
    {
        private readonly string _logKlasoru = "Logs";
        private readonly object _lockObject = new object();

        public JsonLogService()
        {
            if (!Directory.Exists(_logKlasoru))
            {
                Directory.CreateDirectory(_logKlasoru);
                Console.WriteLine($"📁 Log klasörü oluşturuldu: {_logKlasoru}");
            }
        }

        public void TelemetriKaydet(TelemetriGonderRequest telemetri)
        {
            var dosyaAdi = Path.Combine(_logKlasoru, $"telemetri_{DateTime. UtcNow: yyyyMMdd}. json");
            
            var kayit = new
            {
                Zaman = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd HH:mm:ss. fff"),
                TakimNumarasi = telemetri.takim_numarasi,
                Enlem = telemetri.iha_enlem,
                Boylam = telemetri. iha_boylam,
                Irtifa = telemetri.iha_irtifa,
                Hiz = telemetri. iha_hiz,
                Batarya = telemetri.iha_batarya
            };

            DosyayaEkle(dosyaAdi, kayit);
        }

        public void OlayKaydet(KilitlenmeKayiti olay)
        {
            var dosyaAdi = Path.Combine(_logKlasoru, $"olaylar_{DateTime.UtcNow: yyyyMMdd}.json");
            DosyayaEkle(dosyaAdi, olay);
            Console.WriteLine($"💾 Olay kaydedildi!");
        }

        private void DosyayaEkle<T>(string dosyaAdi, T kayit)
        {
            lock (_lockObject)
            {
                try
                {
                    List<object> loglar = new List<object>();

                    if (File.Exists(dosyaAdi))
                    {
                        var icerik = File.ReadAllText(dosyaAdi);
                        loglar = JsonSerializer.Deserialize<List<object>>(icerik) ?? new List<object>();
                    }

                    loglar.Add(kayit);
                    File.WriteAllText(dosyaAdi, JsonSerializer. Serialize(loglar, new JsonSerializerOptions { WriteIndented = true }));
                }
                catch (Exception ex)
                {
                    Console. WriteLine($"❌ Log hatası: {ex. Message}");
                }
            }
        }
    }
}