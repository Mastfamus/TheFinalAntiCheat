import os
import time
import requests
import winreg

GITHUB_USER = "Mastfamus"
GITHUB_REPO = "TheFinalAntiCheat"
GITHUB_RAW_BASE = f"https://raw.githubuser.com{GITHUB_USER}/{GITHUB_REPO}_Background"

def locate_among_us_steam_dir():
    try:
        hKey = winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 945360")
        steam_path, _ = winreg.QueryValueEx(hKey, "InstallLocation")
        return steam_path
    except Exception:
        return r"C:\Program Files (x86)\Steam\steamapps\common\Among Us"

STEAM_DATA_DIR = os.path.join(locate_among_us_steam_dir(), "TheFAC_Data")
LOCAL_BAN_FILE = os.path.join(STEAM_DATA_DIR, "BanPlayers.txt")

def download_file_from_cloud(repo_path, local_destination):
    try:
        url = f"{GITHUB_RAW_BASE}/{repo_path}"
        response = requests.get(url, headers={"User-Agent": "The-FAC-Sovereign-Hook/24.0"}, timeout=15)
        if response.status_code == 200:
            os.makedirs(os.path.dirname(local_destination), exist_ok=True)
            with open(local_destination, "wb") as f: f.write(response.content)
            print(f"📡 [EAC CLOUD]: Synchronized: {repo_path}")
            return True
    except Exception: pass
    return False

def run_live_sync_bridge():
    print("📡 [THE FAC CLOUD BRIDGE]: Active network daemon initialized.")
    download_file_from_cloud("Modstamp.png", os.path.join(STEAM_DATA_DIR, "Modstamp.png"))
    download_file_from_cloud("EAC.txt", os.path.join(STEAM_DATA_DIR,"EAC.txt"))
    download_file_from_cloud("Gavelcursor.png", os.path.join(STEAM_DATA_DIR, "Gavelcursor.png"))
    download_file_from_cloud("Mountain.png", os.path.join(STEAM_DATA_DIR, "Mountain.png"))
    download_file_from_cloud("JapaneseTemple.png", os.path.join(STEAM_DATA_DIR, "JapaneseTemple.png"))
    download_file_from_cloud("Bday.png", os.path.join(STEAM_DATA_DIR, "Bday.png"))
    download_file_from_cloud("PhoneNeonBean.png", os.path.join(STEAM_DATA_DIR, "PhoneNeonBean.png"))                        
    download_file_from_cloud("RailTracks.png", os.path.join(STEAM_DATA_DIR, "RailTracks.png"))
    download_file_from_cloud("Squarles.png", os.path.join(STEAM_DATA_DIR, "Squarles.png"))
    download_file_from_cloud("Waterfall.png", os.path.join(STEAM_DATA_DIR, "Waterfall.png"))
    

    while True:
        try: download_file_from_cloud("BanPlayers.txt", LOCAL_BAN_FILE)
        except Exception: pass
        time.sleep(60)

if __name__ == "__main__":
    run_live_sync_bridge()