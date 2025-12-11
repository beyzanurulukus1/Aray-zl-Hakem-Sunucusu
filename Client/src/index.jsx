// Dosya: Client/src/index.jsx

import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App.jsx';
// Harita stillerini en üste import ediyoruz
import 'leaflet/dist/leaflet.css'; 

// 1. ADIM: DOM'a (index.html) bağlanacak ana "root" değişkenini TANIMLA.
const root = ReactDOM.createRoot(document.getElementById('root'));

// 2. ADIM: Tanımlanan "root" değişkeni içine React bileşenini YERLEŞTİR (render et).
root.render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
);