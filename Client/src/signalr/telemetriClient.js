import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

const startSignalR = (onTelemetryReceived) => {
    // Server'da tanımladığımız Hub adresi: http://localhost:5000/telemetryHub
    const connection = new HubConnectionBuilder()
        .withUrl("http://localhost:8000/telemetryHub")
        .configureLogging(LogLevel.Information)
        .build();

    // Sunucudan "ReceiveTelemetry" metodu çağrıldığında bu fonksiyon çalışır.
    connection.on("ReceiveTelemetry", (telemetryData) => {
        // Gelen veriyi (telemetryData) MapView bileşenine iletiyoruz
        onTelemetryReceived(telemetryData);
    });

    // Bağlantıyı başlat
    connection.start()
        .then(() => console.log("SignalR Connected!"))
        .catch(err => console.error("SignalR Bağlantı Hatası: ", err));
        
    return connection;
};

export default startSignalR;