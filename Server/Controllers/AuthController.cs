using Microsoft.AspNetCore.Mvc;
using Koustec.Api.Models; 

namespace Koustec.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpPost("giris")] // POST /api/giris adresini karşılar
        public IActionResult OturumAc([FromBody] GirisRequest request)
        {
            // Basit bir kimlik doğrulama simülasyonu yapalım.
            if (request.kadi == "deneme_takim" && request.sifre == "gizli_sifre")
            {
                int takimNumarasi = 1; 
                
                // Girişin başarılı olması durumda 200 OK durum kodu ile birlikte içerik olarak takım numarası alınır.
                return Ok(takimNumarasi); 
            }
            else
            {
                // Kullanıcı adı veya şifrenin geçersiz olması durumunda 400 durum kodu cevap olarak alınır.
                return BadRequest("Kullanıcı adı veya şifre geçersiz.");
            }
        }
    }
}