using System.Collections.Generic;

namespace Koustec.Api.Models
{
    // POST /api/telemetri_gonder isteğiyle gelen veri.
    public class TelemetriGonderRequest
    {
        public int takim_numarasi { get; set; }
        public double iha_enlem { get; set; }
        public double iha_boylam { get; set; }
        public double iha_irtifa { get; set; } // Metre cinsinden yükseklik
        public double iha_dikilme { get; set; } // -90 ila +90 aralığında olmalı
        public double iha_yonelme { get; set; } // 0 ila 360 aralığında olmalı
        public double iha_yatis { get; set; } // -90 ila +90 aralığında olmalı
        public double iha_hiz { get; set; } // Metre/saniye cinsinden yer hızı
        public int iha_batarya { get; set; } // Yüzde cinsinden doluluk oranı
        public int iha_otonom { get; set; } // Otonom ise 1, değilse 0
        public int iha_kilitlenme { get; set; } // Kilitlenme varsa 1, yoksa 0

        // Kilitlenme bilgileri (iha_kilitlenme=1 ise sıfırdan farklı olmalı)
        public int hedef_merkez_X { get; set; }
        public int hedef_merkez_Y { get; set; }
        public int hedef_genislik { get; set; }
        public int hedef_yukseklik { get; set; }

        public SunucuSaati gps_saati { get; set; } // GPS saat verisi (UTC+0)
    }

    // POST /api/telemetri_gonder cevabının içindeki konum bilgileri.
    public class KonumBilgisi
    {
        public int takim_numarasi { get; set; }
        public double iha_enlem { get; set; }
        public double iha_boylam { get; set; }
        public double iha_irtifa { get; set; }
        public double iha_dikilme { get; set; }
        public double iha_yonelme { get; set; }
        public double iha_yatis { get; set; }
        public double iha_hizi { get; set; }
        public int zaman_farki { get; set; } // Konum bilgisi ile sunucu saati arasındaki fark (milisaniye)
    }

    // POST /api/telemetri_gonder cevabı.
    public class TelemetriGonderResponse
    {
        public SunucuSaati sunucusaati { get; set; }
        public List<KonumBilgisi> konumBilgileri { get; set; }
    }
}