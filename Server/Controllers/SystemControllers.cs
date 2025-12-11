using Microsoft.AspNetCore.Mvc;
using Koustec.Api.Models; 
using System; 

namespace Koustec.Api.Controllers
{
    [Route("api")] // API rotasını /api olarak belirledik
    [ApiController]
    public class SystemController : ControllerBase
    {
        [HttpGet("sunucusaati")] // GET /api/sunucusaati adresini karşılar
        public IActionResult SunucuSaatiniAl()
        {
            // Tüm verilerin eşzaman olması için UTC saatini kullanıyoruz.
            DateTime now = DateTime.UtcNow; 
            
            var sunucuSaati = new SunucuSaati
            {
                gun = now.Day,
                saat = now.Hour,
                dakika = now.Minute,
                saniye = now.Second,
                milisaniye = now.Millisecond
            };

            // Başarılı istek için 200 OK durum kodu döner.
            return Ok(sunucuSaati);
        }
    }
}