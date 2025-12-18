import React, { useState, useEffect } from 'react';

// UTC zaman objesini HH:MM:SS formatına çevirir
const formatTime = (timeObj) => {
    if (!timeObj) return "--:--:--";
    const pad = (num) => String(num).padStart(2, '0');
    return `${pad(timeObj.saat)}:${pad(timeObj.dakika)}:${pad(timeObj.saniye)}`;
};

const SERVER_BASE_URL = 'http://localhost:8000/api';

const ControlPanel = ({ uavPositions }) => {
    // Güvenli varsayılan değer
    const activeUAVCount = uavPositions && Array.isArray(uavPositions) ? uavPositions.length : 0;

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
                    const sortedData = Array.isArray(data) ? data.reverse() : [];
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
            <h3
                style={{
                    borderBottom: '1px solid #34495e',
                    paddingBottom: '5px',
                    color: '#2ecc71',
                    fontSize: '16px'
                }}
            >
                Canlı Telemetri ({activeUAVCount} İHA)
            </h3>

            <ul style={{ listStyle: 'none', padding: 0, fontSize: '13px' }}>
                {uavPositions && Array.isArray(uavPositions) && uavPositions.length > 0 ? (
                    uavPositions.map((uav, index) => {
                        // Her property için güvenli erişim sağla
                        const takimNum = uav?.num ?? uav?.takim_numarasi ?? 'N/A';
                        const enlem = uav?.lat ?? uav?.iha_enlem ?? 0;
                        const boylam = uav?.lng ?? uav?.iha_boylam ?? 0;
                        const irtifa = uav?.irtifa ?? uav?.iha_irtifa ?? 0;
                        const batarya = uav?.iha_batarya ?? 0;
                        const kilitlenme = uav?.iha_kilitlenme ?? 0;

                        return (
                            <li
                                key={index}
                                style={{
                                    marginBottom: '8px',
                                    padding: '8px',
                                    backgroundColor: '#34495e',
                                    borderRadius: '4px',
                                    borderLeft: `4px solid ${kilitlenme === 1 ? '#e74c3c' : '#f1c40f'}`
                                }}
                            >
                                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                                    <strong style={{ color: '#f1c40f' }}>Takım {takimNum}</strong>
                                    <span style={{ fontSize: '11px', color: '#bdc3c7' }}>Batarya: %{batarya}</span>
                                </div>
                                <div style={{ marginTop: '4px' }}>
                                    Konum: {typeof enlem === 'number' ? enlem.toFixed(5) : enlem}, {typeof boylam === 'number' ? boylam.toFixed(5) : boylam}
                                    <br />
                                    İrtifa: <span style={{ color: '#e74c3c', fontWeight: 'bold' }}>{typeof irtifa === 'number' ? irtifa.toFixed(1) : irtifa}m</span>
                                </div>
                            </li>
                        );
                    })
                ) : (
                    <li style={{ color: '#95a5a6', fontStyle: 'italic', marginTop: '10px' }}>Veri bekleniyor...</li>
                )}
            </ul>

            {/* 2. SAVAŞ KAYITLARI (LOGLAR) */}
            <h3
                style={{
                    marginTop: '25px',
                    borderBottom: '1px solid #34495e',
                    paddingBottom: '5px',
                    color: '#e74c3c',
                    fontSize: '16px'
                }}
            >
                Savaş Kayıtları (Log)
            </h3>

            <p style={{ fontSize: '12px', color: '#bdc3c7', marginBottom: '10px' }}>
                *Toplam Olay: {logs.length}*
            </p>

            <ul style={{ listStyle: 'none', padding: 0, fontSize: '12px', maxHeight: '400px', overflowY: 'auto' }}>
                {Array.isArray(logs) && logs.length > 0 ? (
                    logs.map((log, index) => {
                        // Log verisi için güvenli erişim
                        const olaySaat = log?.kilitlenmeBaslangicZamani;
                        const olayTuru = log?.kilitlenmeTuru ?? 0;
                        const kilitleyenTakim = log?.kilitleyenTakimNumarasi ?? 'N/A';
                        const kilitlenenTakim = log?.kilitlenenTakimNumarasi ?? 'N/A';

                        return (
                            <li
                                key={index}
                                style={{
                                    padding: '8px',
                                    marginBottom: '6px',
                                    backgroundColor: '#2c3e50',
                                    borderRadius: '4px',
                                    borderLeft: olayTuru === 1 ? '4px solid #e74c3c' : '4px solid #3498db'
                                }}
                            >
                                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '4px' }}>
                                    <strong style={{ color: olayTuru === 1 ? '#e74c3c' : '#3498db' }}>
                                        {olayTuru === 1 ? '[KAMİKAZE 💥]' : '[KİLİTLENME 🎯]'}
                                    </strong>
                                    <span style={{ color: '#95a5a6' }}>{formatTime(olaySaat)}</span>
                                </div>

                                <div style={{ color: '#ecf0f1' }}>
                                    <span style={{ color: '#f1c40f' }}>Takım {kilitleyenTakim}</span>
                                    {' --> '}
                                    <span style={{ color: '#e67e22' }}>Takım {kilitlenenTakim}</span>
                                </div>
                            </li>
                        );
                    })
                ) : (
                    <li style={{ color: '#95a5a6', fontStyle: 'italic', marginTop: '10px' }}>Henüz kilitlenme veya çarpışma yok.</li>
                )}
            </ul>

        </div>
    );
};

export default ControlPanel;