import asyncio
import json
import time
import math
from datetime import datetime, timezone, timedelta
from pymavlink import mavutil
from signalrcore.hub_connection_builder import HubConnectionBuilder

# --- RENKLİ KONSOL ÇIKTILARI ---
class Renk: 
    YESIL = '\033[92m'
    MAVI = '\033[94m'
    SARI = '\033[93m'
    KIRMIZI = '\033[91m'
    MOR = '\033[95m'
    CYAN = '\033[96m'
    RESET = '\033[0m'

print(f"{Renk.MAVI}=== KOUSTECH SAVAŞAN İHA - WEBSOCKET ==={Renk.RESET}")

# --- 1. KİMLİK AYARI ---
try:
    girdi = input("Hangi Takımı Yöneteceksin? (1, 2, 3... ): ")
    TAKIM_NUMARASI = int(girdi)
except: 
    print("Hatalı giriş, Takım 1 varsayıldı.")
    TAKIM_NUMARASI = 1

# --- 2. SUNUCU AYARI ---
print(f"Varsayılan Sunucu:  localhost: 8000")
girdi_ip = input("Sunucu IP'si (Aynı bilgisayardaysan Enter'a bas): ")

if girdi_ip.strip() == "":
    SUNUCU_URL = "http://localhost:8000/telemetryHub"
else: 
    SUNUCU_URL = f"http://{girdi_ip}:8000/telemetryHub"

# --- 3. OTOMATİK PORT MATEMATİĞİ ---
MAV_PORT = 14550 + TAKIM_NUMARASI
MAV_CONNECTION = f"udpin:localhost:{MAV_PORT}"

# Kocaeli Merkez (Hedef konum)
HEDEF_LAT = 40.78000
HEDEF_LON = 29.95000

# SITL Referans Noktası
SITL_MERKEZ_LAT = -35.363261
SITL_MERKEZ_LON = 149.165230

# --- RAKİP VERİ DEPOSU ---
rakip_verileri = {}

# --- GLOBAL DEĞİŞKENLER ---
hub_connection = None
bagli = False

def sunucu_saati_yazdir(sunucu_saati):
    if not sunucu_saati:
        return "--: --:--"
    return f"{sunucu_saati.get('saat', 0):02d}:{sunucu_saati.get('dakika', 0):02d}:{sunucu_saati.get('saniye', 0):02d}. {sunucu_saati.get('milisaniye', 0):03d}"

def mesafe_hesapla(lat1, lon1, lat2, lon2):
    R = 6371000
    phi1 = math.radians(lat1)
    phi2 = math.radians(lat2)
    delta_phi = math.radians(lat2 - lat1)
    delta_lambda = math.radians(lon2 - lon1)
    a = math.sin(delta_phi / 2)**2 + math.cos(phi1) * math.cos(phi2) * math.sin(delta_lambda / 2)**2
    c = 2 * math. atan2(math.sqrt(a), math.sqrt(1 - a))
    return R * c

def en_yakin_rakibi_bul(benim_lat, benim_lon):
    en_yakin_mesafe = float('inf')
    en_yakin_rakip = None

    for takim_no, veri in rakip_verileri.items():
        if takim_no == TAKIM_NUMARASI:
            continue
        mesafe = mesafe_hesapla(benim_lat, benim_lon, veri['iha_enlem'], veri['iha_boylam'])
        if mesafe < en_yakin_mesafe:
            en_yakin_mesafe = mesafe
            en_yakin_rakip = {
                'takim_no':  takim_no,
                'mesafe': mesafe,
                'zaman_farki': veri['zaman_farki'],
                'irtifa': veri['iha_irtifa']
            }

    return en_yakin_rakip

def main():
    global hub_connection, bagli, rakip_verileri

    print(f"\n{Renk.YESIL}✅ TAKIM {TAKIM_NUMARASI} WEBSOCKET MODU{Renk. RESET}")
    print(f"   -> WebSocket:  {SUNUCU_URL}")
    print(f"   -> MAVLink:    {MAV_PORT}")

    # --- WEBSOCKET BAĞLANTISI ---
    hub_connection = HubConnectionBuilder()\
        .with_url(SUNUCU_URL)\
        .with_automatic_reconnect({
            "type": "raw",
            "keep_alive_interval": 10,
            "reconnect_interval": 5,
        })\
        .build()

    # --- CALLBACK FONKSİYONLARI ---
    def on_open():
        global bagli
        bagli = True
        print(f"{Renk.YESIL}🔌 WebSocket BAĞLANDI!{Renk.RESET}")

    def on_close():
        global bagli
        bagli = False
        print(f"{Renk.KIRMIZI}🔌 WebSocket KAPANDI!{Renk.RESET}")

    def on_error(error):
        print(f"{Renk.KIRMIZI}❌ WebSocket Hatası: {error}{Renk.RESET}")

    # --- RAKİP VERİLERİNİ DİNLE (SUNUCUDAN GELEN) ---
    def on_receive_all_teams(data):
        global rakip_verileri
        
        konum_bilgileri = data.get('konumBilgileri', [])
        sunucu_saati = data.get('sunucusaati', {})

        with open("sunucu_tum_telemetriler.txt", "a", encoding="utf-8") as f:
            zaman = datetime.now().strftime("%H:%M:%S. %f")[:-3]
            saat_str = f"{sunucu_saati.get('saat',0):02d}:{sunucu_saati.get('dakika',0):02d}:{sunucu_saati.get('saniye',0):02d}"
        
            f.write(f"\n[{zaman}] Sunucu Saati: {saat_str}\n")
            for t in konum_bilgileri:
                f.write(f"  T{t.get('takim_numarasi')}: "
                    f"Enlem:{t.get('iha_enlem',0):.6f} "
                    f"Boylam:{t.get('iha_boylam',0):.6f} "
                    f"İrtifa:{t.get('iha_irtifa',0):.1f}m "
                    f"Hız:{t.get('iha_hizi',0)} "
                    f"Gecikme:{t.get('zaman_farki',0)}ms\n")
                f.write(f"{'─'*60}\n")
        
        # Verileri kaydet
        for rakip in konum_bilgileri:
            takim_no = rakip.get('takim_numarasi')
            rakip_verileri[takim_no] = {
                'takim_numarasi': takim_no,
                'iha_enlem': rakip. get('iha_enlem', 0),
                'iha_boylam': rakip.get('iha_boylam', 0),
                'iha_irtifa': rakip.get('iha_irtifa', 0),
                'iha_hizi': rakip.get('iha_hizi', 0),
                'zaman_farki':  rakip.get('zaman_farki', 0),
            }
        
        # Ekrana yazdır
        if konum_bilgileri:
            rakip_ozet = []
            for r in konum_bilgileri:
                zaman_farki = r.get('zaman_farki', 0)
                takim = r.get('takim_numarasi')
                guncellik = "🟢" if zaman_farki < 500 else "🟡" if zaman_farki < 1000 else "🔴"
                rakip_ozet. append(f"T{takim}:{zaman_farki}ms{guncellik}")
            
            saat = f"{sunucu_saati.get('saat',0):02d}:{sunucu_saati.get('dakika',0):02d}:{sunucu_saati.get('saniye',0):02d}"
            print(f"{Renk.MAVI}📥 Sunucu:  {saat} | Takımlar: [{', '.join(rakip_ozet)}]{Renk. RESET}")

    # --- EVENT HANDLER'LARI KAYDET ---
    hub_connection.on_open(on_open)
    hub_connection.on_close(on_close)
    hub_connection.on_error(on_error)
    hub_connection.on("ReceiveAllTeams", lambda args: on_receive_all_teams(args[0] if isinstance(args, list) else args))

    # --- BAĞLAN ---
    print(f"📡 WebSocket bağlanıyor...")
    hub_connection.start()
    
    # Bağlantı bekle
    timeout = 10
    while not bagli and timeout > 0:
        time.sleep(0.5)
        timeout -= 0.5

    if not bagli:
        print(f"{Renk.KIRMIZI}❌ WebSocket bağlantısı kurulamadı! {Renk. RESET}")
        return

    # --- MAVLink BAĞLANTISI ---
    try:
        master = mavutil.mavlink_connection(MAV_CONNECTION)
        print(f"📡 Uçak aranıyor...")
        master.wait_heartbeat()
        print(f"{Renk. YESIL}✅ UÇAK BAĞLANDI!{Renk.RESET}")
        master.mav.request_data_stream_send(
            master.target_system, master.target_component,
            mavutil.mavlink.MAV_DATA_STREAM_ALL, 4, 1
        )
    except Exception as e: 
        print(f"{Renk.KIRMIZI}❌ MAVLink Hatası: {e}{Renk.RESET}")
        return

    kamikaze_yapildi = False
    print(f"\n{Renk.CYAN}📊 WebSocket veri akışı başlıyor...{Renk.RESET}\n")

    while True:
        if not bagli: 
            print(f"{Renk.SARI}⚠️ Bağlantı bekleniyor... {Renk.RESET}")
            time.sleep(1)
            continue

        # --- VERİ OKU ---
        msg = master.recv_match(type=['GLOBAL_POSITION_INT'], blocking=True, timeout=1)
        if not msg: 
            continue

        raw_lat = msg.lat / 1e7
        raw_lon = msg. lon / 1e7
        alt = msg.relative_alt / 1000.0

        if alt <= 0.0:
            continue

        # --- IŞINLAMA ---
        benim_lat = HEDEF_LAT + (raw_lat - SITL_MERKEZ_LAT)
        benim_lon = HEDEF_LON + (raw_lon - SITL_MERKEZ_LON)

        # --- TELEMETRİ PAKETİ ---
        telemetry = {
            "takim_numarasi": TAKIM_NUMARASI,
            "iha_enlem": benim_lat,
            "iha_boylam": benim_lon,
            "iha_irtifa": alt,
            "iha_dikilme": 0,
            "iha_yonelme": 0,
            "iha_yatis":  0,
            "iha_hiz": 0 if kamikaze_yapildi else 25,
            "iha_batarya": 0 if kamikaze_yapildi else 80,
            "iha_otonom": 1,
            "iha_kilitlenme": 0,
            "hedef_merkez_X":  0,
            "hedef_merkez_Y":  0,
            "hedef_genislik": 0,
            "hedef_yukseklik": 0,
            "gps_saati": {
                "gun":  datetime.now().day,
                "saat": datetime.now().hour,
                "dakika": datetime.now().minute,
                "saniye": datetime.now().second,
                "milisaniye":  int(datetime.now().microsecond / 1000)
            }
        }

        # --- WEBSOCKET İLE GÖNDER ---
        try: 
            hub_connection.send("TelemetriGonder", [telemetry])
            print(f"{Renk.CYAN}📤 T{TAKIM_NUMARASI} | Alt:  {alt:.1f}m | Konum: {benim_lat:.6f}, {benim_lon:.6f}{Renk. RESET}")

            # --- EN YAKIN RAKİBİ KONTROL ET ---
            en_yakin = en_yakin_rakibi_bul(benim_lat, benim_lon)
            
            if en_yakin and not kamikaze_yapildi: 
                mesafe = en_yakin['mesafe']
                hedef_takim = en_yakin['takim_no']

                if mesafe < 5.0:
                    print(f"{Renk.MOR}💥 ÇARPIŞMA!  T{hedef_takim} ({mesafe:.1f}m){Renk. RESET}")
                    kamikaze_yapildi = True
                    # Motorları kapat
                    master. mav.command_long_send(
                        master.target_system, master. target_component,
                        mavutil. mavlink.MAV_CMD_COMPONENT_ARM_DISARM, 0,
                        0, 0, 0, 0, 0, 0, 0
                    )
                elif mesafe < 50.0:
                    print(f"{Renk. KIRMIZI}🎯 KİLİTLENDİM -> T{hedef_takim} ({mesafe:.1f}m){Renk.RESET}")
                elif mesafe < 200.0:
                    print(f"{Renk. SARI}👁️ RADAR -> T{hedef_takim} ({mesafe:.1f}m){Renk.RESET}")

        except Exception as e: 
            print(f"{Renk. KIRMIZI}❌ Gönderim hatası: {e}{Renk.RESET}")

        time.sleep(0.5)

if __name__ == "__main__":
    try: 
        main()
    except KeyboardInterrupt:
        print(f"\n{Renk. SARI}👋 Çıkış yapılıyor... {Renk. RESET}")
        if hub_connection: 
            hub_connection.stop()
