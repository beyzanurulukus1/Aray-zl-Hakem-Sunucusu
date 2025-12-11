namespace Koustec.Api.Models
{
    public class SunucuSaati
    {
        // Alınan ve gönderilen tüm sunucu saatleri bu formattadır.
        public int gun { get; set; }
        public int saat { get; set; }
        public int dakika { get; set; }
        public int saniye { get; set; }
        public int milisaniye { get; set; }
    }
}