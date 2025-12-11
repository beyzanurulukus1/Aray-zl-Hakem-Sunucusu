using System.Collections.Generic; // List<T> kullanımı için eklenmeli
using Koustec.Api.Models;

namespace Koustec.Api.Models
{
    // GET /api/qr_koordinati cevabı.
    public class QrKoordinatiResponse
    {
        public double qrEnlem { get; set; } // QR kodun enlem bilgisi.
        public double qrBoylam { get; set; } // QR kodun boylam bilgisi.
    }
    
    // HSS koordinatları için model
    public class HssKoordinatBilgisi
    {
        public int id { get; set; } // Hava savunma sisteminin numarası.
        public double hssEnlem { get; set; } // HSS'nin enlemi.
        public double hssBoylam { get; set; } // HSS'nin boylamı.
        public int hssYaricap { get; set; } // Kaçınılması gereken dairenin yarıçapı.
    }

    // GET /api/hss_koordinatlari cevabı.
    public class HssKoordinatlariResponse
    {
        public SunucuSaati sunucusaati { get; set; } // Sunucu Saati.
        public List<HssKoordinatBilgisi> hss_koordinat_bilgileri { get; set; } // HSS'lerin listesi.
    }
}