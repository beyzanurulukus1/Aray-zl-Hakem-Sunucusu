import React, { useState } from 'react';
import MapView from './map/mapView.jsx'; 
import ControlPanel from './Panels/ControlPanel.jsx'; 

function App() {
    // App bileşeni sadece canlı İHA konumlarını tutmaya devam eder
    const [liveUAVPositions, setLiveUAVPositions] = useState([]);
    
    // MapView'den gelen canlı konum verisini App bileşenine almak için bir callback fonksiyonu
    const handleUAVUpdate = (positions) => {
        setLiveUAVPositions(positions);
    };
    
    return (
        <div className="App" style={{ display: 'flex', height: '100vh', width: '100vw' }}>
            
            {/* 1. KONTROL PANELİ (Soldaki Bar) */}
            <div 
                style={{ 
                    width: '300px', 
                    backgroundColor: '#2c3e50', 
                    padding: '20px', 
                    overflowY: 'auto',
                    color: '#ecf0f1', 
                    boxShadow: '4px 0 10px rgba(0, 0, 0, 0.5)', 
                    zIndex: 100 
                }}
            >
                <h1 
                    style={{ 
                        borderBottom: '2px solid #3498db', 
                        paddingBottom: '15px', 
                        marginBottom: '25px', 
                        color: '#3498db',
                        fontSize: '24px'
                    }}
                >
                    Hakem Sunucusu Klonu
                </h1>
                <p style={{ fontSize: '14px', color: '#bdc3c7' }}>Sunucu: http://localhost:8000</p>
                
                {/* ControlPanel sadece canlı İHA konumlarını alır, kayıtları kendi çeker */}
                <ControlPanel 
                    uavPositions={liveUAVPositions} 
                />
            </div>
            
            {/* 2. HARİTA (Sağdaki Büyük Kısım) */}
            <div style={{ flexGrow: 1 }}>
                <MapView onUAVUpdate={handleUAVUpdate} />
            </div>
        </div>
    );
}

export default App;