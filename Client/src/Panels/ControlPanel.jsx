import React, { useState, useEffect } from 'react';

// UTC zaman objesini HH:MM:SS formatına çevirir
const formatTime = (timeObj) => {
    if (!timeObj) return "--:--:--";
    const pad = (num) => String(num).padStart(2, '0');
    return `${pad(timeObj.saat)}:${pad(timeObj.dakika)}:${pad(timeObj.saniye)}`;
};

const SERVER_BASE_URL = 'http://localhost:8000/api'; 

const ControlPanel = ({ uavPositions }) => { 
    const activeUAVCount = uavPositions ? uavPositions.length : 0;
    
    // Tek bir liste tutuyoruz, çünkü sunucudan hepsi beraber geliyor
    const [logs, setLogs] = useState([]);

    // --- POLLING MEKANİZMASI ---
    useEffect(() => {
        const fetchRecords = async () => {
            try {
                // Python'un gönderdiği tek adrese istek atıyoruz
                const response = await fetch(`${SERVER_BASE_URL}/kilitlenme_bilgisi`);
                if (response.ok) {
                    const data = await response.json();
                    
                    // Veri varsa ters çevir (En yeni en üstte), yoksa boş dizi
                    const sortedData = data ? data.reverse() : [];
                    setLogs(sortedData);
                }
            } catch (err) {
                console.error("Loglar çekilemedi:", err);
            }
        };

        fetchRecords(); 
        const interval = setInterval(fetchRecords, 1000); // Her 1 saniyede bir güncelle (Canlı hissi için)

        return () => clearInterval(interval);
    }, []); 

    return (
        <div style={{ padding: '10px', color: '#ecf0f1' }}>
            
            {/* 1. CANLI TELEMETRİ ÖZETİ */}
            <h3 style={{ borderBottom: '1px solid #34495e', paddingBottom: '5px', color: '#2ecc71', fontSize: '16px' }}>
                Canlı Telemetri ({activeUAVCount} İHA)
            </h3>
            
            <ul style={{ listStyle: 'none', padding: 0, fontSize: '13px' }}>
                {uavPositions && uavPositions.map((uav, index) => (
                    <li key={index} style={{ marginBottom: '8px', padding: '8px', backgroundColor: '#34495e', borderRadius: '4px', borderLeft: `4px solid ${uav.iha_kilitlenme === 1 ? '#e74c3c' : '#f1c40f'}` }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                            <strong style={{ color: '#f1c40f' }}>Takım {uav.takim_numarasi}</strong>
                            <span style={{ fontSize: '11px', color:'#bdc3c7' }}>Batarya: %{uav.iha_batarya}</span>
                        </div>
                        <div style={{ marginTop: '4px' }}>
                            Konum: {uav.iha_enlem.toFixed(5)}, {uav.iha_boylam.toFixed(5)}<br/>
                            İrtifa: <span style={{ color: '#e74c3c', fontWeight: 'bold' }}>{uav.iha_irtifa.toFixed(1)}m</span>
                        </div>
                    </li>
                ))}
                {activeUAVCount === 0 && <li style={{ color: '#95a5a6', fontStyle: 'italic', marginTop: '10px' }}>Veri bekleniyor...</li>}
            </ul>

            {/* 2. SAVAŞ KAYITLARI (LOGLAR) */}
            <h3 style={{ marginTop: '25px', borderBottom: '1px solid #34495e', paddingBottom: '5px', color: '#e74c3c', fontSize: '16px' }}>
                Savaş Kayıtları (Log)
            </h3>
            
            <p style={{ fontSize: '12px', color: '#bdc3c7', marginBottom: '10px' }}>
                *Toplam Olay: {logs.length}*
            </p>

            <ul style={{ listStyle: 'none', padding: 0, fontSize: '12px', maxHeight: '400px', overflowY: 'auto' }}>
                {logs.map((log, index) => (
                    <li key={index} style={{ 
                        padding: '8px', 
                        marginBottom: '6px', 
                        backgroundColor: '#2c3e50', 
                        borderRadius: '4px',
                        borderLeft: log.kilitlenmeTuru === 1 ? '4px solid #e74c3c' : '4px solid #3498db' // Kırmızı (Kamikaze) veya Mavi (Kilit)
                    }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                            <strong style={{ color: log.kilitlenmeTuru === 1 ? '#e74c3c' : '#3498db' }}>
                                {log.kilitlenmeTuru === 1 ? '[KAMİKAZE 💥]' : '[KİLİTLENME 🎯]'}
                            </strong>
                            <span style={{ color: '#95a5a6' }}>{formatTime(log.kilitlenmeBaslangicZamani)}</span>
                        </div>
                        
                        <div style={{ color: '#ecf0f1' }}>
                            <span style={{ color: '#f1c40f' }}>Takım {log.kilitleyenTakimNumarasi}</span> 
                            {' --> '} 
                            <span style={{ color: '#e67e22' }}>Takım {log.kilitlenenTakimNumarasi}</span>
                        </div>
                    </li>
                ))}

                {logs.length === 0 && <li style={{ color: '#95a5a6', fontStyle: 'italic', marginTop: '10px' }}>Henüz kilitlenme veya çarpışma yok.</li>}
            </ul>

        </div>
    );
};

export default ControlPanel;