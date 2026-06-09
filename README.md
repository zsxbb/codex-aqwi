# Codex Trainer - AdventureQuest Worlds Infinity

Proyek ini berisi *source code* dan *release* untuk **Codex Trainer v2.0** pada game AdventureQuest Worlds Infinity. Trainer ini diinjeksikan secara dinamis ke dalam file Assembly game menggunakan pustaka Mono.Cecil dan Harmony.

---

## 📂 Struktur Proyek

Repositori ini terdiri dari beberapa proyek C# berikut:

1. **`AutoAttackTrainer`**: Proyek utama Class Library (.NET Standard 2.1) yang berisi logika trainer, antarmuka GUI dalam game (Unity IMGUI), auto attack, infinite range, bypass respawn, bypass anti-cheat obrolan, dan lain-lain.
2. **`Patcher`**: Aplikasi Console (.NET 8.0) yang digunakan untuk mendeteksi instalasi game, membuat cadangan (`.bak`), dan secara dinamis menyuntikkan (*injecting*) pemanggilan inisialisasi Codex ke dalam file `Assembly-CSharp.dll` game.
3. **`0Harmony`**: Pustaka Harmony yang digunakan untuk memotong (*hooking*) fungsi-fungsi internal game secara *runtime*.
4. **`Publish`**: Folder yang berisi hasil kompilasi siap pakai.

---

## 🚀 Panduan Penggunaan (Untuk Pemain)

Jika Anda hanya ingin menggunakan trainer ini tanpa melakukan kompilasi ulang, terdapat dua cara:

### Metode A: Menggunakan Installer Otomatis (Direkomendasikan)
1. Buka folder **`Publish`** (atau unduh rilisan terbaru).
2. Pastikan game AdventureQuest Worlds Infinity di Steam dalam keadaan **tertutup / tidak berjalan**.
3. Jalankan **`CodexSetup.exe`**.
4. Ikuti panduan instalasi di layar. Installer akan secara otomatis mendeteksi lokasi game Anda melalui Registry Steam.
5. Klik **Install**, dan installer akan menyalin file serta menambal (patch) game secara otomatis di latar belakang.
6. Untuk menghapus trainer, Anda cukup melakukan uninstall melalui **Windows Settings > Apps** atau **Control Panel** seperti aplikasi biasa. Game Anda akan otomatis pulih ke keadaan semula tanpa mod.

### Metode B: Menggunakan Patcher Manual (Cadangan)
1. Buka folder **`Publish`** (atau unduh rilisan terbaru).
2. Pastikan game AdventureQuest Worlds Infinity di Steam dalam keadaan **tertutup / tidak berjalan**.
3. Jalankan **`Patcher.exe`**.
4. Patcher akan mencoba mencari folder `Managed` game secara otomatis di direktori instalasi Steam standar:
   `C:\Program Files (x86)\Steam\steamapps\common\AdventureQuest Worlds Unity Playtest\AdventureQuest Worlds Infinity_Data\Managed`
5. Jika folder game berada di lokasi lain, salin alamat folder `Managed` game Anda dan tempel (*paste*) ke jendela Patcher, lalu tekan **Enter**.
6. Patcher akan menyalin file, membuat cadangan `Assembly-CSharp.dll.bak`, dan memodifikasi file `Assembly-CSharp.dll` yang asli.

---

### Cara Mengaktifkan Trainer (Setelah Pemasangan)
1. Buka game, login ke karakter Anda, masuk ke dalam room, lalu ketik **`/codex`** pada kolom obrolan (chat) untuk membuka menu.
2. Pada pembukaan pertama, Anda akan melihat status "Activation Required" beserta Username akun Anda.
3. Kirimkan Username tersebut kepada pembuat/penyedia mod untuk mendapatkan 5-Digit Activation Key Anda.
4. Masukkan Activation Key di kotak input yang tersedia, dan menu trainer akan langsung terbuka.


---

## 🛠️ Informasi untuk Pengembang (Developer Guide)

Jika Anda ingin memodifikasi atau mengembangkan proyek ini:

### Prasyarat
- **Visual Studio 2022** atau **JetBrains Rider**.
- **.NET SDK 8.0** (untuk Patcher) dan **.NET Standard 2.1** support (untuk AutoAttackTrainer).
- File game AdventureQuest Worlds Infinity yang asli dari Steam (karena proyek ini bergantung pada referensi DLL game).

### Membuka Solusi
Anda dapat membuka file solusi berikut untuk mulai mengedit kode:
- **`AutoAttackTrainer.sln`** (Solusi utama yang berisi proyek Trainer).
- **`0Harmony.sln`** (Solusi untuk pustaka Harmony jika diperlukan).

### Konfigurasi Referensi (References)
Proyek `AutoAttackTrainer` merujuk langsung ke file DLL Unity game di folder instalasi default Steam. Jika Anda menginstal game di direktori kustom, Anda perlu memperbarui `<HintPath>` di file `AutoAttackTrainer/AutoAttackTrainer.csproj` agar menunjuk ke folder game Anda:
- `Assembly-CSharp.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.InputLegacyModule.dll`
- `UnityEngine.UI.dll`
- `Unity.TextMeshPro.dll`

### Cara Build Ulang
Untuk melakukan build ulang melalui terminal di folder solusi:
```powershell
dotnet build
```
Setelah build berhasil, DLL hasil kompilasi akan berada di `AutoAttackTrainer/bin/Debug/netstandard2.1/AutoAttackTrainer.dll`. Anda dapat menyalinnya ke folder game `Managed` Anda atau menaruhnya bersama `Patcher.exe` agar di-deploy oleh patcher.

---

## 🔄 Pemeliharaan Saat Game Mengalami Pembaruan (Steam Update)
Jika game diperbarui oleh developer melalui Steam, file game yang telah di-patch (`Assembly-CSharp.dll`) akan tertimpa dengan versi yang baru dan bersih.

Patcher v2.0 memiliki sistem **Self-Healing Backup**:
- Cukup jalankan kembali `Patcher.exe` setelah game selesai di-update.
- Patcher akan mendeteksi file game yang baru, memperbarui cadangan `.bak` ke versi game terbaru, dan menyuntikkan kembali hook Codex.
- Baca detail lebih lengkap di [PATCH_GUIDE.md](PATCH_GUIDE.md).
