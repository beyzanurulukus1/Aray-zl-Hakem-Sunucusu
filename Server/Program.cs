using Koustec.Api.Services; 
using Koustec.Api.Hubs; 
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Servislerin Tanıtılması (Dependency Injection) ---

builder.Services.AddControllers();

// SignalR Servisini ekle
builder.Services.AddSignalR(); 

// CORS Servisini ekle (Sadece Client'ın çalıştığı 3000 portuna izin verir)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.WithOrigins("http://localhost:3000","http://localhost:3001") 
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()); 
});

builder.Services.AddSingleton<ValidationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 


var app = builder.Build();

// --- 2. Uygulama Yapılandırması (Middleware) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy"); 

app.UseAuthorization();
app.MapControllers(); 

// Hub'ı rotaya eşle: Client bu adrese bağlanacak.
app.MapHub<TelemetriHub>("/telemetryHub"); 

app.Run();