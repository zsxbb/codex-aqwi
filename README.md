# ⚔️ Codex Trainer — AdventureQuest Worlds: Infinity

**Codex Trainer** adalah modifikasi canggih dan *trainer in-game* mandiri untuk **AdventureQuest Worlds Infinity**. Dibangun menggunakan arsitektur dynamic injection (.NET & Harmony), mod ini menghadirkan beragam utilitas pertarungan otomatis, manipulasi mekanik permainan, serta fitur **WebRTC Proximity Voice Chat** terintegrasi secara langsung di dalam game.

---

## 🌟 Fitur Unggulan

### 🎙️ 1. WebRTC In-Game Voice Chat
* **Komunikasi Suara Real-Time:** Ngobrol langsung dengan pemain sesama pengguna Codex di room yang sama tanpa perlu aplikasi pihak ketiga seperti Discord.
* **Kompresi Audio Opus:** Menggunakan codec Opus standar industri (`Concentus`) untuk kualitas suara jernih dengan latensi ultra-rendah dan hemat kuota data.
* **Pengaturan Audio Lengkap:**
  * Dropdown pemilihan mikrofon perangkat keras secara langsung di dalam game.
  * **Input Boost** (0% s/d 300%) untuk memperjelas suara mic yang pelan.
  * **Output Volume Boost** (0% s/d 300%) serta indikator VU meter visual real-time.
* **Koneksi Terenkripsi:** Paket audio dan signaling dilindungi dengan enkripsi data.

### ⚔️ 2. Modifikasi Pertarungan (Combat & Farming)
* **Auto Attack Otomatis:** Menyerang monster terdekat secara otomatis tanpa klik berulang, sangat optimal untuk grinding dan farming monster berjam-jam.
* **Infinite Range (Bypass Jarak Serang):** Karakter dapat menyerang target dari mana saja di seluruh area layar tanpa batasan jarak (*melee to screen-wide reach*).
* **Auto Target & Skill Rotation:** Otomatis mengunci target baru saat monster mati dan mengeksekusi rotasi skill.

### 🏃 3. Movement & Respawn Tweaks
* **Keep Cell on Death (Respawn di Tempat Mati):** Jika karakter Anda gugur saat boss raid atau farming, karakter akan langsung bangkit kembali di koordinat tempat Anda mati tanpa terlempar ke spawn cell awal.
* **Speed Hack / Movement Tweaks:** Mempercepat mobilitas perpindahan karakter di area peta.

### 💻 4. Antarmuka In-Game & Injeksi Mandiri
* **Menu Tersembunyi (`/codex`):** Antarmuka GUI tampil elegan di dalam game hanya saat dipanggil lewat chat, tidak mengotori layar permainan utama.
* **Single-File Dynamic Patcher:** Executable mandiri `Patcher.exe` mengemas seluruh library dan dependensi di dalamnya tanpa membutuhkan DLL tercecer.
* **Sistem Lisensi Aman:** Proteksi aktivasi berbasis key authorization yang unik untuk setiap nama karakter pemain.

---

## 📖 Tutorial Penggunaan

### Langkah 1: Pemasangan (Instalasi)

Pemasangan dilakukan dengan sangat mudah menggunakan installer wizard otomatis:
1. Pastikan game **AdventureQuest Worlds Infinity** di Steam dalam keadaan **tertutup** (tidak sedang berjalan).
2. Unduh berkas **`CodexSetup.rar`** dari rilis resmi, lalu ekstrak berkas `CodexSetup.exe`.
3. Jalankan **`CodexSetup.exe`**. Installer akan secara otomatis mendeteksi folder instalasi game Anda di Steam.
4. Klik **Next** hingga selesai. Installer akan menyuntikkan patch secara instan dalam waktu < 2 detik.

---

### Langkah 2: Cara Aktivasi Pertama Kali

1. Buka game melalui Steam atau pintasan desktop **Play AQW Infinity with Codex**.
2. Masuk ke karakter Anda dan kunjungi sembarang room (misal `/join battleon`).
3. Pada kolom obrolan (chat), ketik:
   ```text
   /codex
   ```
4. Menu aktivasi Codex Trainer akan muncul di layar. Anda akan melihat status **"Activation Required"** beserta username karakter Anda.
5. Salin username tersebut dan dapatkan **5-Digit Activation Key** dari penyedia mod/developer.
6. Masukkan kode tersebut pada kotak input lisensi, lalu klik **Activate**.
7. Menu fitur trainer akan langsung terbuka dan siap digunakan!

---

### Langkah 3: Menggunakan Fitur di Dalam Game

* **Membuka/Menutup Menu:** Ketik `/codex` di chat kapan saja saat bermain.
* **Menjalankan Auto Attack:** Buka tab **Combat** pada GUI Codex, lalu centang kotak **Auto Attack**. Karakter Anda akan otomatis mengunci target dan menyerang.
* **Mengaktifkan Voice Chat:** 
  1. Masuk ke tab **Voice Chat**.
  2. Klik tombol **Connect**.
  3. Pilih input microphone Anda pada menu dropdown dan sesuaikan slider **Input Boost** serta **Output Volume**.
  4. Suara Anda akan otomatis terdengar oleh pemain lain di room yang sama yang juga terhubung ke voice chat.

---

### Langkah 4: Cara Menghapus (Uninstall Bersih)

Jika Anda ingin bermain game dalam kondisi bersih tanpa mod:
1. Buka **Windows Settings > Apps > Installed Apps** (atau Control Panel > Programs and Features).
2. Cari **Codex Trainer**, lalu klik **Uninstall**.
3. Uninstaller akan secara otomatis memulihkan berkas asli `Assembly-CSharp.dll` dari cadangan cadangan (`.bak`). Game Anda 100% kembali normal seperti semula.

---

## 🛡️ Keamanan & Privasi (Security & Anti-Cheat)

### 1. Penjelasan Peringatan Antivirus (False Positive)
Saat mengunduh atau menjalankan installer/patcher, Windows Defender atau antivirus pihak ketiga mungkin menampilkan peringatan:
> **Mengapa ini terjadi?**  
> Modifikasi game PC bekerja dengan metode **Dynamic Assembly / Memory Injection** (menggunakan pustaka *Mono.Cecil* dan *Harmony*) untuk menyuntikkan instruksi ke dalam berkas biner game. Pola injeksi biner ini memiliki tanda perilaku yang sama persis dengan cara kerja mod game, reverse-engineering tools, dan cheat engine, sehingga sering memicu alarm *False Positive (Heuristic)* pada antivirus.

* **Jaminan Keamanan:** Berkas installer dan biner Codex dibangun langsung dari kode sumber terbuka di repositori ini, tidak memuat adware, cryptominer, trojan, atau spyware apa pun.
* **Solusi:** Tambahkan berkas `CodexSetup.exe` atau folder game ke dalam daftar pengecualian (*Exclusion / Whitelist*) antivirus Anda jika terblokir.

---

### 2. Stealth Chat Interception (Privasi Akun)
* Perintah `/codex` diintercept langsung pada level client-side sebelum pesan teks dikirimkan ke server game Artix Entertainment.
* Pesan perintah aktivasi atau pembukaan menu Anda **tidak akan pernah terkirim** ke server umum atau terbaca oleh pemain lain/game master (GM).

---

### 3. Ketahanan Terhadap Update Game (Update-Persistence)
* Bila Steam memperbarui game AQW Infinity, file biner game asli akan tertimpa versi baru.
* Codex dirancang dengan sistem **Dynamic Hooking**: Patcher tidak menimpa file serialisasi Unity yang rentan membuat game crash. Anda cukup menjalankan kembali installer atau patcher satu kali setelah update Steam untuk mengaktifkan kembali mod tanpa perlu konfigurasi ulang.

---

## ⚖️ Penafian (Disclaimer)
* Perangkat lunak ini dibuat untuk tujuan edukasi, penelitian modifikasi Unity engine, dan hiburan komunitas.
* Penggunaan modifikasi dalam game daring selalu memiliki risiko regulasi akun dari pihak pengembang game. Gunakan fitur dengan bijak dan bertanggung jawab.
