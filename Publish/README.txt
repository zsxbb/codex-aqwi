=================================================
             Codex Trainer v2.0
=================================================

Cara Penggunaan:
1. Ekstrak seluruh isi folder ini ke komputer Anda.
2. Jalankan `Patcher.exe`.
3. Patcher akan otomatis mendeteksi lokasi game AdventureQuest Worlds Infinity di folder Steam standar.
4. Jika game tidak ditemukan otomatis, silakan copy dan paste lokasi folder 'Managed' game Anda ke dalam console Patcher.
   (Contoh: C:\Program Files (x86)\Steam\steamapps\common\AdventureQuest Worlds Unity Playtest\AdventureQuest Worlds Infinity_Data\Managed)
5. Tekan Enter, dan Patcher akan memasang trainer secara otomatis.
6. Buka game, login ke karakter Anda, masuk ke room, lalu ketik `/codex` di chat untuk membuka menu.
7. Di menu pertama, Anda akan melihat status "Activation Required" beserta Username akun Anda.
8. Kirimkan Username tersebut kepada pembuat/penyedia mod untuk mendapatkan 5-Digit Activation Key Anda.
9. Masukkan Activation Key di kotak input yang tersedia, dan menu trainer akan langsung terbuka.

Catatan Penting:
- Pastikan game tidak sedang berjalan saat menjalankan Patcher.
- Jika game melakukan update di Steam, cukup jalankan kembali `Patcher.exe` ini.
- Kunci aktivasi bersifat permanen untuk username tersebut di komputer Anda (hanya perlu dimasukkan sekali).

-------------------------------------------------
🛡️ INFORMASI DETEKSI ANTIVIRUS (FALSE POSITIVE)
-------------------------------------------------
Beberapa program Antivirus (terutama Windows Defender) mungkin mendeteksi `Patcher.exe` atau `AutoAttackTrainer.dll` sebagai ancaman (misalnya: Trojan/Injector).

Mengapa ini terjadi?
Patcher menggunakan pustaka Mono.Cecil untuk memodifikasi file `Assembly-CSharp.dll` bawaan game secara langsung guna menyuntikkan menu Codex. Aktivitas modifikasi/injeksi file binary game seperti ini dinilai mencurigakan oleh sistem keamanan antivirus, sehingga memicu peringatan bahaya (False Positive / Deteksi Palsu).

Jaminan Keamanan:
- Proyek ini 100% aman untuk digunakan.
- Seluruh kode sumber (source code) proyek ini bersifat terbuka (Open Source) dan dapat Anda periksa secara transparan di repositori GitHub kami: https://github.com/zsxbb/codex-aqwi.
- Tidak ada modul berbahaya, spyware, pencurian data, atau malware di dalamnya.

Cara Mengatasinya:
1. Windows Defender / Antivirus Lain:
   Buka Windows Security -> Virus & threat protection -> Protection history. Jika file diblokir, klik file tersebut dan pilih "Allow on device" atau "Restore".
2. Menambahkan Exclusion (Pengecualian):
   Agar file tidak terhapus otomatis di kemudian hari, tambahkan folder tempat Anda menyimpan file ini ke dalam daftar pengecualian antivirus Anda:
   - Buka Windows Security -> Virus & threat protection -> Manage settings (di bawah Virus & threat protection settings) -> Add or remove exclusions -> Add an exclusion -> Folder (pilih folder tempat file ini diekstrak).
