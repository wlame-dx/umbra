# vdriver — sanal ekran yardımcısı

Umbra **imzalı bir kernel sürücüsü içermez**. İmzasız sürücü dağıtmak hem
Windows tarafından engellenir hem de güvenli değildir. Bunun yerine Umbra,
açık kaynak bir **IDD (Indirect Display Driver)** ile birlikte çalışır:

- `create.cmd` — yayına başlarken sanal ekranı kurar/etkinleştirir
- `remove.cmd` — yayın durunca sanal ekranı devre dışı bırakır/kaldırır
- `get-driver.ps1` — açık kaynak Virtual-Display-Driver sürücüsünü indirip
  `bin` klasörüne koyar (isteğe bağlı kolay yol)

Umbra, "Sanal ekranı otomatik oluştur/kapat" anahtarı açıkken bu betikleri
**yönetici haklarıyla** çalıştırır (UAC istemi görünebilir). Sürücü dosyaları
yoksa boşuna UAC açılmaz; günlüğe bilgi yazılır ve mevcut ekran yayınlanır.
Hiçbir şey bozulmaz.

## Kolay kurulum (önerilen)

PowerShell aç ve şunu çalıştır:

```powershell
powershell -ExecutionPolicy Bypass -File vdriver\get-driver.ps1
```

Bu betik [Virtual-Display-Driver](https://github.com/itsmikethetech/Virtual-Display-Driver)
sürümünü indirir ve `vdriver\bin` içine yerleştirir. Sonra Umbra'yı aç,
**Sanal ekranı otomatik oluştur/kapat** anahtarını açık bırak ve yayını
başlat. İlk seferde Windows sürücü için bir onay (UAC / sertifika) isteyebilir.

## Elle kurulum

1. Açık kaynak IDD sürücülerinden birini indir:
   - [Virtual-Display-Driver](https://github.com/itsmikethetech/Virtual-Display-Driver) **(önerilen)**
   - [usbmmidd_v2](https://www.usbmmidd.com/)
   - [IddSampleDriver](https://github.com/ge9/IddSampleDriver)
2. Bu klasörün altında `bin` klasörü oluştur ve sürücü dosyalarını içine kopyala.
   İki yerleşim desteklenir:

   **A) Virtual-Display-Driver / pnputil tarzı**
   ```
   vdriver/
     bin/
       *.inf
       *.sys
       *.cer   (varsa)
       *.cat
     create.cmd
     remove.cmd
     README.md
   ```

   **B) usbmmidd_v2 / deviceinstaller tarzı**
   ```
   vdriver/
     bin/
       deviceinstaller64.exe
       deviceinstaller32.exe
       usbmmidd.inf
       usbmmidd.sys
     create.cmd
     remove.cmd
     README.md
   ```

3. Umbra'yı aç, **Sanal ekranı otomatik oluştur/kapat** anahtarını açık bırak
   ve yayını başlat. Ekran listesinde yeni bir monitör belirir.

> `create.cmd` / `remove.cmd` içindeki komutları kullandığın sürücüye göre
> düzenleyebilirsin. Çıkış kodu `0` başarıyı, diğer değerler hatayı belirtir.
