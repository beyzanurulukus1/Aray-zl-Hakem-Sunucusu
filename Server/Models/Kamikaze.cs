using Koustec.Api.Models; 

namespace Koustec.Api.Models
{
    // POST /api/kamikaze_bilgisi ile gelen veri.
    public class KamikazeRequest
    {
        // Kamikaze başlangıç zamanı (Sunucu Saati türünde).
        public SunucuSaati kamikazeBaslangicZamani { get; set; }

        // Kamikaze bitiş zamanı (Sunucu Saati türünde).
        public SunucuSaati kamikazeBitisZamani { get; set; }

        // QR kodun okunması sonucu elde edilen metin bilgisi.
        public string qrMetni { get; set; } 
    }
}