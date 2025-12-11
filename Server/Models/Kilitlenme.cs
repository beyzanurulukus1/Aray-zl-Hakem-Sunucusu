using Koustec.Api.Models; 

namespace Koustec.Api.Models
{
    // POST /api/kilitlenme_bilgisi ile gelen veri.
    public class KilitlenmeRequest
    {
        // Kilitlenmenin bittiği zamanı Sunucu Saati formatında.
        public SunucuSaati kilitlenmeBitisZamani { get; set; } 

        // Kilitlenmenin otonom olup olmadığı bilgisi (otonomsa 1, değilse 0).
        public int otonom_kilitlenme { get; set; } 
    }
    
    // Not: Bu API'nin başarılı cevabı (200 OK) sadece durumu bildirmek içindir, özel bir içerik dönmez.
}