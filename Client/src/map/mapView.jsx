import React, { useState, useEffect, useRef } from 'react';
import { MapContainer, TileLayer, Marker, Circle, Popup } from 'react-leaflet';
import 'leaflet/dist/leaflet.css'; 
import startSignalR from '../signalr/telemetriClient'; 


import L from 'leaflet';
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

// Sabitler
const DEFAULT_CENTER = [40.78, 29.95];
const SERVER_BASE_URL = 'http://localhost:8000/api';

// İHA konumlarını Server'dan aldıkça tutmak için global depo
const allUAVs = {}; 


const MapView = ({ onUAVUpdate }) => { 
    
    const [uavPositions, setUAVPositions] = useState([]); // Canlı İHA konumları (Harita için)
    const [qrPosition, setQrPosition] = useState(null); 
    const [hssZones, setHssZones] = useState([]); 
    const [center, setCenter] = useState(DEFAULT_CENTER);
    const connectionRef = useRef(null); 

    // --- 1. Başlangıçta Statik Konumları (QR ve HSS) Çekme ---
    useEffect(() => {
        // QR Koordinatını Çekme (Harita merkezini belirler)
        fetch(`${SERVER_BASE_URL}/qr_koordinati`)
            .then(res => res.json())
            .then(data => {
                if (data.qrEnlem && data.qrBoylam) {
                    const pos = [data.qrEnlem, data.qrBoylam];
                    setQrPosition(pos);
                    setCenter(pos); 
                }
            })
            .catch(error => console.error("QR çekilemedi:", error));

        // HSS Koordinatlarını Çekme
        fetch(`${SERVER_BASE_URL}/hss_koordinatlari`)
            .then(res => res.json())
            .then(data => {
                if (data && data.hss_koordinat_bilgileri) {
                    setHssZones(data.hss_koordinat_bilgileri);
                }
            })
            .catch(error => console.error("HSS çekilemedi:", error));
    }, []);

    // --- 2. SignalR İstemcisine Bağlanma ve Canlı Veri Akışı ---
    useEffect(() => {
        // Sunucudan (Hub'dan) "ReceiveTelemetry" metoduyla gelen veriyi işler
        const handleTelemetry = (telemetryData) => {
            // Gelen JSON datasından sadece gerekli alanları çıkar
            const { takim_numarasi, iha_enlem, iha_boylam, iha_irtifa } = telemetryData;

            
            allUAVs[takim_numarasi] = {
                num: takim_numarasi, 
                lat: iha_enlem, 
                lng: iha_boylam, 
                irtifa: iha_irtifa,
            };
            
            
            const currentPositions = Object.values(allUAVs);
            setUAVPositions(currentPositions);

            
            if (onUAVUpdate) {
                onUAVUpdate(currentPositions); 
            }
        };

        // SignalR bağlantısını başlat ve geri çağırma fonksiyonunu tanımla
        connectionRef.current = startSignalR(handleTelemetry); 

        // Temizlik: Komponent kapanınca bağlantıyı durdur
        return () => {
            if (connectionRef.current) {
                connectionRef.current.stop();
            }
        };
    }, [onUAVUpdate]); 

    return (
        <MapContainer center={center} zoom={15} style={{ height: '100%', width: '100%' }}>
            <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution='&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
            />

            {/* İHA Konumları */}
            {uavPositions.map(uav => (
                <Marker key={uav.num} position={[uav.lat, uav.lng]}>
                    <Popup>
                        İHA {uav.num} <br/>
                        İrtifa: {uav.irtifa}m
                    </Popup>
                </Marker>
            ))}

            {/* QR Kod Konumu (Hedef) */}
            {qrPosition && (
                <Marker position={qrPosition} icon={L.divIcon({className: 'qr-icon', html: '<div style="font-size: 24px;">🎯</div>'})}>
                    <Popup>QR Kodu Hedefi</Popup>
                </Marker>
            )}

            {/* HSS Yasak Bölgeleri (Çemberler) */}
            {hssZones.map(hss => (
                <Circle 
                    key={hss.id} 
                    center={[hss.hssEnlem, hss.hssBoylam]} 
                    radius={hss.hssYaricap} 
                    pathOptions={{ color: 'red', fillColor: '#f03', fillOpacity: 0.3 }}
                >
                    <Popup>HSS {hss.id} - Yasak Bölge (Yarıçap: {hss.hssYaricap}m)</Popup>
                </Circle>
            ))}
        </MapContainer>
    );
};

export default MapView;