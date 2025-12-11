import requests
import json
import time
import sys
import math
from datetime import datetime
from pymavlink import mavutil 

# --- RENKLİ KONSOL ÇIKTILARI ---
class Renk:
    YESIL = '\033[92m'
    MAVI = '\033[94m'
    SARI = '\033[93m'
    KIRMIZI = '\033[91m' # Kilitlenme Rengi
    MOR = '\033[95m'     # Çarpışma Rengi
    RESET = '\033[0m'

print(f"{Renk.MAVI}=== KOUSTECH SAVAŞAN İHA KONTROL MERKEZİ (LOGLU SİSTEM) ==={Renk.RESET}")

# --- 1. KİMLİK AYARI ---
try:
    girdi = input("Hangi Takımı Yöneteceksin? (1, 2, 3...): ")
    TAKIM_NUMARASI = int(girdi)
except:
    print("Hatalı giriş, Takım 1 varsayıldı.")
    TAKIM_NUMARASI = 1

# --- 2. SUNUCU AYARI ---
print(f"Varsayılan Sunucu: http://localhost:8000")
girdi_ip = input("Sunucu IP'si (Aynı bilgisayardaysan Enter'a bas): ")

if girdi_ip.strip() == "":
    BASE_URL = "http://localhost:8000"
else:
    BASE_URL = f"http://{girdi_ip}:8000"

# İKİ FARKLI ADRES TANIMLIYORUZ:
TELEMETRI_URL = f"{BASE_URL}/api/telemetri_gonder"     # Harita için
LOG_URL = f"{BASE_URL}/api/kilitlenme_bilgisi"         # Tabloyu doldurmak için

# --- 3. OTOMATİK PORT MATEMATİĞİ ---
MAV_PORT = 14551 + ((TAKIM_NUMARASI - 1) * 10)
MAV_CONNECTION = f"udp:127.0.0.1:{MAV_PORT}"

# Kocaeli Merkez (Ofsetsiz - Tam Merkez)
HEDEF_LAT = 40.78000 
HEDEF_LON = 29.95000 

# Simülasyonun (SITL) Referans Noktası (Avustralya Pisti)
SITL_MERKEZ_LAT = -35.363261
SITL_MERKEZ_LON = 149.165230

# --- 4. MATEMATİKSEL MESAFE HESAPLAMA ---
def mesafe_hesapla(lat1, lon1, lat2, lon2):
    R = 6371000 
    phi1 = math.radians(lat1)
    phi2 = math.radians(lat2)
    delta_phi = math.radians(lat2 - lat1)
    delta_lambda = math.radians(lon2 - lon1)
    a = math.sin(delta_phi / 2)**2 + math.cos(phi1) * math.cos(phi2) * math.sin(delta_lambda / 2)**2
    c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a))
    return R * c 

def get_time():
    n = datetime.utcnow()
    return {"gun":n.day, "saat":n.hour, "dakika":n.minute, "saniye":n.second, "milisaniye":n.microsecond//1000}

def main():
    print(f"\n{Renk.YESIL}✅ TAKIM {TAKIM_NUMARASI} SAVAŞA HAZIR!{Renk.RESET}")
    print(f"   -> Telemetri: {TELEMETRI_URL}")
    print(f"   -> Loglama:   {LOG_URL}")
    print(f"   -> Port:      {MAV_PORT}")
    
    try:
        master = mavutil.mavlink_connection(MAV_CONNECTION)
        print(f"📡 Uçak aranıyor...")
        master.wait_heartbeat()
        print(f"{Renk.YESIL}✅ UÇAK BAĞLANDI!{Renk.RESET}")
        master.mav.request_data_stream_send(master.target_system, master.target_component, mavutil.mavlink.MAV_DATA_STREAM_ALL, 4, 1)
    except Exception as e:
        print(f"{Renk.KIRMIZI}❌ Hata: Bağlantı kurulamadı.{Renk.RESET}")
        return

    # Savaş Durumu Değişkenleri
    kamikaze_yapildi = False 
    son_log_zamani = 0 # Spam engellemek için zaman sayacı

    while True:
        # VERİ OKU
        msg = master.recv_match(type=['GLOBAL_POSITION_INT'], blocking=True)
        if not msg: continue

        raw_lat = msg.lat / 1e7
        raw_lon = msg.lon / 1e7
        alt = msg.relative_alt / 1000.0
        
        if alt == 0.0: continue 

        # --- IŞINLAMA (MUTLAK HESAP) ---
        benim_lat = HEDEF_LAT + (raw_lat - SITL_MERKEZ_LAT)
        benim_lon = HEDEF_LON + (raw_lon - SITL_MERKEZ_LON)
        
        # --- SAVAŞ VE LOGLAMA MANTIĞI ---
        kilitlenme_aktif = 0
        en_yakin_mesafe = 99999
        en_yakin_rakip_no = 0

        try:
            resp = requests.get(TELEMETRI_URL, timeout=0.1)
            if resp.status_code == 200:
                rakipler = resp.json()
                
                for rakip in rakipler:
                    r_no = rakip.get('takim_numarasi')
                    if r_no == TAKIM_NUMARASI: continue 
                    
                    dist = mesafe_hesapla(benim_lat, benim_lon, rakip.get('iha_enlem'), rakip.get('iha_boylam'))
                    
                    if dist < en_yakin_mesafe:
                        en_yakin_mesafe = dist
                        en_yakin_rakip_no = r_no

                # --- KARAR MEKANİZMASI VE LOG GÖNDERME ---
                
                # A. KAMİKAZE (Limit 5m - Kesin Çarpışma)
                if en_yakin_mesafe < 5.0 and not kamikaze_yapildi:
                    print(f"{Renk.MOR}💥 ÇARPIŞMA GERÇEKLEŞTİ! Hedef: Takım {en_yakin_rakip_no} (Mesafe: {en_yakin_mesafe:.1f}m){Renk.RESET}")
                    
                    # 1. TABLOYU DOLDUR (LOG PAKETİ)
                    log_verisi = {
                        "kilitlenenTakimNumarasi": TAKIM_NUMARASI,
                        "kilitleyenTakimNumarasi": en_yakin_rakip_no,
                        "kilitlenmeBaslangicZamani": get_time(),
                        "kilitlenmeBitisZamani": get_time(),
                        "kilitlenmeTuru": 1 # 1 = KAMİKAZE
                    }
                    requests.post(LOG_URL, json=log_verisi, timeout=0.1)
                    print(f"📝 Web Sitesi Tablosuna Kayıt Gönderildi!")

                    # 2. MOTORLARI KAPAT
                    master.mav.command_long_send(
                        master.target_system, master.target_component,
                        mavutil.mavlink.MAV_CMD_COMPONENT_ARM_DISARM, 0,
                        0, 0, 0, 0, 0, 0, 0
                    )
                    kamikaze_yapildi = True 

                # B. KİLİTLENME (Limit 50m - Yaklaşma)
                elif en_yakin_mesafe < 50.0 and not kamikaze_yapildi:
                    kilitlenme_aktif = 1
                    print(f"{Renk.KIRMIZI}🎯 KİLİTLENDİM! -> Takım {en_yakin_rakip_no} (Mesafe: {en_yakin_mesafe:.1f}m){Renk.RESET}")
                    
                    # Log tablosunu şişirmemek için 3 saniyede bir log atıyoruz
                    if time.time() - son_log_zamani > 3:
                        log_verisi = {
                            "kilitlenenTakimNumarasi": TAKIM_NUMARASI,
                            "kilitleyenTakimNumarasi": en_yakin_rakip_no,
                            "kilitlenmeBaslangicZamani": get_time(),
                            "kilitlenmeBitisZamani": get_time(),
                            "kilitlenmeTuru": 0 # 0 = KİLİTLENME
                        }
                        requests.post(LOG_URL, json=log_verisi, timeout=0.1)
                        print(f"📝 Kilitlenme Logu Gönderildi.")
                        son_log_zamani = time.time()
                
                # C. NORMAL SEYİR
                elif not kamikaze_yapildi:
                    if en_yakin_rakip_no != 0:
                        print(f"📤 Takım {TAKIM_NUMARASI} | İrtifa: {alt:.1f}m | {Renk.SARI}👀 En Yakın: Takım {en_yakin_rakip_no} ({en_yakin_mesafe:.1f}m){Renk.RESET}")
                    else:
                        print(f"📤 Takım {TAKIM_NUMARASI} | İrtifa: {alt:.1f}m | 🔭 Etraf Temiz")

        except Exception as e:
            pass

        # VERİ PAKETİ (Harita Güncellemesi)
        # Eğer kamikaze yaptıysak kilitlenme bilgisini sürekli 1 gönderiyoruz ki ekranda kırmızı kalsın
        final_kilit = 1 if kamikaze_yapildi else kilitlenme_aktif

        telemetry = {
            "takim_numarasi": TAKIM_NUMARASI,
            "iha_enlem": benim_lat, "iha_boylam": benim_lon, "iha_irtifa": alt,
            "iha_dikilme": 0, "iha_yonelme": 0, "iha_yatis": 0,
            "iha_hiz": 0 if kamikaze_yapildi else 25,
            "iha_batarya": 0 if kamikaze_yapildi else 80, 
            "iha_otonom": 1, 
            "iha_kilitlenme": final_kilit,
            "hedef_merkez_X": 0, "hedef_merkez_Y": 0, "hedef_genislik": 0, "hedef_yukseklik": 0,
            "gps_saati": get_time()
        }
        
        try:
            requests.post(TELEMETRI_URL, json=telemetry, timeout=0.1)
        except:
            pass

if __name__ == "__main__":
    main()