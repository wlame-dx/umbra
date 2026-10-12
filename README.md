# Umbra

Bu sistem aynı zamanda vibecoding aracılığıylada desteklenmiştir.

Windows bilgisayarındaki ekranı, yerel ağ üzerinden telefonuna ya da tabletine akıtan
küçük bir program. spacedesk'in **filigransız** alternatifi olarak yazıldı:
tek dosya, filigran yok, açık kaynak.

- İstemci için **web sayfası** yeterli — Android/iOS tarayıcıda tam ekran çalışır.
- Sanal (ikinci) ekranı Umbra yayın sırasında **kendi oluşturup** yayın bitince kapatır
  (yardımcı sürücü kuruluysa); kurulu değilse mevcut ekranı yayınlar.
- **Türkçe / İngilizce** ara yüz: uygulama, web istemcisi, tanıtım sitesi ve kurulum
  aracı — hepsi tek tuşla dil değiştirir.

```
┌────────────┐   WebSocket (JPEG kareler)   ┌──────────────┐
│  Windows   │ ────────────────────────────► │ telefon/tablet │
│  Umbra.exe │ ◄──────────────────────────── │ tarayıcı     │
└────────────┘     dokunma / fare / tuş      └──────────────┘
```

## Özellikler

- Tek ekrana bağlanan her istemci gönderilen her kareyi alır (kalite/fps/ölçek host'tan ayarlanır).
- Kare hızı, JPEG kalitesi ve ölçek hem host arayüzünden hem tarayıcıdaki ayar panelinden canlı değiştirilebilir.
- **Fare imleci görüntüye işlenir** — uzaktaki imleci tarayıcıda görürsün (host'taki fare de dahil).
- Dokunmatik girdi Windows `InjectTouchInput` ile **gerçek dokunma** olarak işlenir (çoklu parmak dahil).
  Dokunma enjeksiyonu kullanılamazsa olaylar otomatik olarak fareye düşer; istemci panelinden
  **Kontrol → Dokunmatik / Fare** ile kipi elle seçebilirsin.
- **Yayın öncesi uyarılar:** uzaktan kontrol kapalı, dokunma yok, sanal ekran sürücüsü yok,
  port dolu ya da ölçek 1'den küçükse yayın başlamadan önce uyarı penceresi çıkar.
  Durdururken onay istenir.
- Pencereyi büyüt/küçült/kapla: başlık çubuğundan sürükle, kenarlardan boyutlandır, çift tıkla büyüt.
- **Dil seçimi:** başlık çubuğundaki `TR / EN` açılır menüsü tüm arayüzü çevirir.
- Tam ekran, sığdır / doldur / 1:1 ölçek, bağlantı istatistikleri tarayıcıda mevcut.
- GUI: WPF, koyu gri–siyah tema. XAML çalışma zamanında yüklenir (BAML derlemesine gerek yok),
  bu yüzden `ui/MainWindow.xaml` dosyasını düzenleyip yeniden derlemeden de deneyebilirsin.

## Gereksinimler

- Windows 8.1 / 10 / 11
- .NET Framework 4.8 (Windows 10/11'de hazır gelir)
- Derlemek istersen ekstra kurulum **gerekmez**: `csc.exe` (C# 5) Windows'ta hazırdır.

## Kurulum yolları

Üç yol var, üçü de aynı uygulamayı verir:

1. **Kurulum dosyası** — `Umbra-Setup-<sürüm>.exe`'yi çalıştır. Yönetici hakkı istemez;
   `%LOCALAPPDATA%\Programs\Umbra` altına kurar, Başlat menüsü (+ istersen masaüstü)
   kısayolu ekler ve Kayıt Defteri'ne kaldırma girdisi yazar. Kaldırmak için
   **Ayarlar → Uygulamalar → Umbra → Kaldır**.
2. **Tasinabilir (portable)** — `Umbra-Portable-<sürüm>.zip`'i istediğin yere çıkar,
   içindeki `Umbra\Umbra.exe`'yi çalıştır. Kurulum yok, iz bırakmaz.
3. **Kaynaktan derle** — aşağıdaki "Hızlı başlangıç" bölümü.

## Hızlı başlangıç

```powershell
# derle
.\build.ps1          # dist\Umbra.exe üretir

# çalıştır
.\run.ps1            # ya da dist\Umbra.exe

# yayını başlat, çıkan adresi telefonunda aç
```

`build.ps1` sistemdeki `csc.exe`'yi bulur, WPF referanslarını ekler, arayüzü, web
dosyalarını ve simgeyi exe içine gömer (`/resource:`), ayrıca `dist\` altına
düzenlenebilir kopyalarını da koyar. Exe, `dist\ui` ve `dist\wwwroot` klasörlerini
bulamazsa gömülü kopyalardan okur — yani sadece `Umbra.exe`'yi kopyalasan da çalışır.

## Paketleme (kurulum + portable)

```powershell
.\package.ps1                 # sürüm 1.0.0
.\package.ps1 -Version 1.0.1  # sürüm belirt
```

`package.ps1` önce derler, sonra:

- `dist\Umbra-Portable-<sürüm>.zip` — `Umbra\` klasörü içinde taşınabilir sürüm.
- `dist\Umbra-Setup-<sürüm>.exe` — yuku (payload) gömülü, temalı, TR/EN kurulum aracı.

Kurulum aracı ayrıca `--silent` (sessiz kur), `--uninstall` ve `--uninstall --silent`
bayraklarını kabul eder.

## Simge (U logosu)

Simge `tools\make-icon.ps1` ile üretilir; `assets\umbra.ico` (16–256, çok boyutlu) ve
`assets\umbra.png` (pencere simgesi) dosyalarını yazar. Simgeyi değiştirmek istersen
betiği düzenleyip tekrar çalıştır, sonra yeniden derle:

```powershell
.\tools\make-icon.ps1
.\build.ps1
```

## Sanal ikinci ekran

Umbra kernel sürücüsü içermez (imzasız sürücü kurulumu Windows'ta hem engellenir hem
riskli). Bunun yerine açık kaynak bir **IDD (Indirect Display Driver)** ile birlikte
çalışır ve onu senin yerine yönetir.

### En kolay yol: uygulamadan indir

Umbra'yı aç → **UZAKTAN KONTROL** kartındaki **"Sanal ekran sürücüsünü indir"** düğmesine
bas → çıkan onayı kabul et. Açılan PowerShell penceresi
[Virtual-Display-Driver](https://github.com/itsmikethetech/Virtual-Display-Driver)
sürümünü indirip `vdriver\bin\` içine koyar. Pencere kapanınca **"Sanal ekranı otomatik
oluştur/kapat"** anahtarı açıkken yayını başlat; Umbra sürücüyü kurar (UAC onayı isteyebilir)
ve ekran listesinde ikinci monitör belirir.

### Elle indirme

Kurulum sürümünde `vdriver` klasörü şurada olur:

```
%LOCALAPPDATA%\Programs\Umbra\vdriver
```

Taşınabilir (zip) sürümde ise zip'i açtığın `Umbra\vdriver` klasöründe. PowerShell aç ve
şunu çalıştır (kurulum sürümü için):

```powershell
powershell -ExecutionPolicy Bypass -File "$env:LOCALAPPDATA\Programs\Umbra\vdriver\get-driver.ps1"
```

Bu betik sürücüyü indirip `vdriver\bin\` içine koyar. İstersen açık kaynak sürücülerden
birini (Virtual-Display-Driver veya [usbmmidd](https://github.com/ge9/IddSampleDriver))
indirip `vdriver\bin\` klasörüne elle de kopyalayabilirsin (ayrıntı: `vdriver\README.md`).

### Kurulum nasıl işliyor

Yayını başlatınca Umbra `vdriver\create.cmd`'yi (yönetici hakkıyla, UAC onayı isteyebilir)
arka planda çalıştırır, sanal ekranı kurar ve monitör listesini tazeler; arayüz donmaz.
Yayın durunca (ya da uygulama kapanınca) `vdriver\remove.cmd` ile kaldırır. Sürücü dosyaları
yoksa boşuna UAC açmaz; günlüğe bilgi yazar ve mevcut ekranı yayınlar.

## Tarayıcı istemcisi

Host çalışırken adresi aç (`http://<ip>:8777/`). Web istemcisi:

- `ws://` üzerinden bağlanır; JPEG kareleri ikili mesaj olarak alır.
- Dokunma/fare/tekerlek olaylarını JSON mesajlarla host'a gönderir.
- Araç çubuğu: **görüntü** (sığdır/doldur/1:1), **tam ekran**, **bilgi**, **klavye aç/kapat**, **ayarlar** ve **TR/EN**.
- Ayar panelinde ayrıca **Kontrol** kipi (Dokunmatik / Fare) seçilir; seçim host'a iletilir.
- Ayarlar (kalite, fps, ölçek, klavye, kontrol kipi) oturum boyunca korunur; dil tercihi
  tarayıcıda `localStorage`'a yazılır.
- "Ana ekrana ekle" ile Android/iOS'te tam ekranlı uygulama gibi kullanılabilir.

## Protokol (kısa özet)

| Yön | Tür | İçerik |
|-----|-----|--------|
| sunucu → istemci | text | `{"t":"hello","w":..,"h":..}` bağlanınca |
| sunucu → istemci | binary | JPEG kare |
| sunucu → istemci | text | `{"t":"pong","ts":..}` ping yanıtı |
| istemci → sunucu | text | `{"t":"p","a":"down|move|up","x":..,"y":..,"b":..,"id":..}` |
| istemci → sunucu | text | `{"t":"wheel","d":..,"x":..,"y":..}` |
| istemci → sunucu | text | `{"t":"key","a":"down|up","code":..,"key":..}` |
| istemci → sunucu | text | `{"t":"mode","m":"touch|mouse"}` |
| istemci → sunucu | text | `{"t":"ping","ts":..}` |
| istemci → sunucu | text | `{"t":"set","quality":..,"fps":..,"scale":..}` |

## Proje yapısı

```
umbra/
  host/                 Windows host uygulaması (C# / .NET Framework 4.8)
    src/                kaynak kod (C# 5, eski dil sürümüne uyumlu)
    ui/MainWindow.xaml  WPF arayüzü (çalışma zamanında yüklenir)
    wwwroot/            web istemcisi (index.html, style.css, app.js, favicon)
    assets/             U simgesi (umbra.ico, umbra.png)
    vdriver/            sanal ekran yardımcı betikleri (create.cmd, remove.cmd, get-driver.ps1)
    build.ps1           tek komutla derleme
    package.ps1         kurulum + portable paketleme
    run.ps1             çalıştırma
    tools/              doğrulama araçları (UI önizleme, ağ/test istemcileri, simge üretimi)
  setup/                kurulum aracının kaynağı (C# / WPF)
  site/                 tanıtım sitesi (GitHub Pages, TR/EN)
```

## Yol haritası

- [ ] Kendi imzalı IDD sürücüsü (sanal monitörü Umbra'nın kendisi sürücü olarak oluştursun)
- [ ] Android APK istemcisi
- [ ] Ses aktarımı
- [ ] Çoklu istemci için oturum/kimlik doğrulama

## Lisans

MIT. Bakınız [LICENSE](LICENSE).

---

Umbra — *gölge gibi yanında.*
