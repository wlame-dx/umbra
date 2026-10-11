# vdriver — sanal ekran yardımcısı

Umbra **imzalı bir kernel sürücüsü içermez**. İmzasız sürücü dağıtmak hem
Windows tarafından engellenir hem de güvenli değildir. Bunun yerine Umbra,
açık kaynak bir **IDD (Indirect Display Driver)** ile birlikte çalışır:

- `create.cmd` — yayına başlarken sanal ekranı kurar/etkinleştirir
- `remove.cmd` — yayın durunca sanal ekranı devre dışı bırakır/kaldırır

Umbra, "Sanal ekranı otomatik oluştur/kapat" anahtarı açıkken bu betikleri
**yönetici haklarıyla** çalıştırır (UAC istemi görünebilir). Betikler yoksa
uyarı verir ve mevcut ekran yayınlanır; hiçbir şey bozulmaz.

## Kurulum

1. Açık kaynak IDD sürücülerinden birini indir:
   - [usbmmidd_v2](https://www.usbmmidd.com/) / topluluk kopyası
   - [Virtual-Display-Driver](https://github.com/itsmikethetech/Virtual-Display-Driver)
   - [IddSampleDriver](https://github.com/ge9/IddSampleDriver)
2. Bu klasörün altında `bin` klasörü oluştur ve sürücü dosyalarını içine kopyala:

   ```
   vdriver/
     bin/
       deviceinstaller64.exe
       deviceinstaller32.exe
       usbmmidd.inf
       usbmmidd.sys
       ... (sürücünün diğer dosyaları)
     create.cmd
     remove.cmd
     README.md
   ```

3. Umbra'yı aç, **Sanal ekranı otomatik oluştur/kapat** anahtarını açık bırak
   ve yayını başlat. Ekran listesinde yeni bir monitör belirir.

> `create.cmd` / `remove.cmd` içindeki komutları kullandığın sürücüye göre
> düzenleyebilirsin. Çıkış kodu `0` başarıyı, diğer değerler hatayı belirtir.
