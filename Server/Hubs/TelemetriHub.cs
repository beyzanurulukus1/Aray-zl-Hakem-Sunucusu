using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Koustec.Api.Models;


namespace Koustec.Api.Hubs
{
    // Bu Hub, Client (Arayüz) ile Server arasındaki canlı bağlantı noktasıdır.
    public class TelemetriHub : Hub
    {
        // Client'tan (Arayüz) bir şey geldiğinde bu metot kullanılabilir.
        // Ancak bu projede, Server Client'a bilgi göndereceği için bu kısmı boş bırakıyoruz.
        
        // Örnek: Arayüzden bir mesaj göndermek istenseydi:
        // public async Task SendMessage(string user, string message)
        // {
        //     await Clients.All.SendAsync("ReceiveMessage", user, message);
        // }
    }
}