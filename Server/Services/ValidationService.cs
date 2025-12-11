using Koustec.Api.Models; 
using System;
using System.Collections.Generic;

namespace Koustec.Api.Services
{
    public class ValidationService
    {
        // Her takımın son telemetri gönderim zamanını tutmak için in-memory depolama.
        private static readonly Dictionary<int, DateTime> _lastTelemetryTimes = new Dictionary<int, DateTime>();

        
        public string ValidateTelemetry(TelemetriGonderRequest request)
        {
            // --- 1. Frekans Kontrolü (Max 2 Hz) ---
            if (_lastTelemetryTimes.ContainsKey(request.takim_numarasi))
            {
                TimeSpan timeDifference = DateTime.UtcNow - _lastTelemetryTimes[request.takim_numarasi];
                
                // 2 Hz demek, en az 500 milisaniyede (0.5 saniye) bir gönderim demektir.
                if (timeDifference.TotalMilliseconds < 500)
                {
                    // Hata Kodu 3: Gönderim Hızı Çok Yüksek
                    return "3: 2 Hz (500 ms) üzerinde gönderim hızına ulaşıldı."; 
                }
            }
            
            // --- 2. Aralık Kontrolleri ---
            // Dikilme: -90 ila +90 aralığında olmalıdır.
            if (request.iha_dikilme < -90 || request.iha_dikilme > 90)
            {
                return $"Aralık Hatası: iha_dikilme değeri ({request.iha_dikilme}) [-90, 90] aralığı dışındadır.";
            }

            // Yatış: -90 ila +90 aralığında olmalıdır.
            if (request.iha_yatis < -90 || request.iha_yatis > 90)
            {
                return $"Aralık Hatası: iha_yatis değeri ({request.iha_yatis}) [-90, 90] aralığı dışındadır.";
            }

            // Yönelme: 0 ila 360 aralığında olmalıdır.
            if (request.iha_yonelme < 0 || request.iha_yonelme > 360)
            {
                return $"Aralık Hatası: iha_yonelme değeri ({request.iha_yonelme}) [0, 360] aralığı dışındadır.";
            }

            // Eğer doğrulama başarılı ise, son gönderim zamanını güncelle
            _lastTelemetryTimes[request.takim_numarasi] = DateTime.UtcNow;

            // Başarılı doğrulama
            return null;
        }
    }
}