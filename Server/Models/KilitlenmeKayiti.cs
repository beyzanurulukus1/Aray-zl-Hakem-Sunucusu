using Koustec.Api.Models;

namespace Koustec.Api.Models
{
    // Birleştirilmiş:  Hem Kilitlenme hem Kamikaze olaylarını burada saklarız
    public class KilitlenmeKayiti
    {
        // Hangi takım kilitlenmiş / çarpışmış
        public int kilitlenenTakimNumarasi { get; set; }

        // Hangi takım kilitlemişse / çarpışmışsa
        public int kilitleyenTakimNumarasi { get; set; }

        // Kilitlenme başlangıç zamanı
        public SunucuSaati kilitlenmeBaslangicZamani { get; set; }

        // Kilitlenme bitiş zamanı
        public SunucuSaati kilitlenmeBitisZamani { get; set; }

        // Olay türü:  0 = KİLİTLENME (Yaklaşma), 1 = KAMİKAZE (Çarpışma)
        public int kilitlenmeTuru { get; set; }
    }
}