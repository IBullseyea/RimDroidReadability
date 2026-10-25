# RimDroid Readability — 0.2.4

**[Mod ZIP’ini indir](https://github.com/IBullseyea/RimDroidReadability/releases/download/v0.2.4/RimDroidReadability-0.2.4.zip)** · [Tüm sürümler](https://github.com/IBullseyea/RimDroidReadability/releases) · [English](README.md)

**RimDroid Native Android üzerinde RimWorld 1.6** için büyük harita göstergeleri, eşya filtreleri ve dokunmatik hedef bulucu. Kamera yakınlığını koruyarak kolonistleri ve yerdeki eşya yığınlarını bulmayı kolaylaştırır.

MIT lisanslı · Deneysel ön sürüm

## Ekran görüntüleri

Samsung S25 FE / RimDroid 0.3.0v2 üzerinde kullanıcının paylaştığı güncel İngilizce arayüz: filtre kısayolu, büyük etiketler ve yığın toplamları.

![Kaynak filtresi ve yığın toplamları](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/resource-filter.jpg)

<details>
<summary>Fotoğrafların tamamı — 10 ek görüntü</summary>

| | |
| --- | --- |
| **Kolonistler / Eşya kapalı**<br>![Kolonistler / Eşya kapalı](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/colonist-indicators.jpg) | **Duraklatma ipucu ve kolonistler**<br>![Duraklatma ipucu ve kolonistler](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/pause-tooltip.jpg) |
| **Tüm eşyalar**<br>![Tüm eşyalar](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/all-items.jpg) | **İlaç kısayolu ipucu**<br>![İlaç kısayolu ipucu](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/medicine-tooltip.jpg) |
| **İlaç filtresi**<br>![İlaç filtresi](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/medicine-filter.jpg) | **Odun filtresi**<br>![Odun filtresi](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/wood-filter.jpg) |
| **Silah filtresi**<br>![Silah filtresi](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/weapon-filter.jpg) | **Yiyecek filtresi**<br>![Yiyecek filtresi](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/food-filter.jpg) |
| **Giysi filtresi**<br>![Giysi filtresi](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/apparel-filter.jpg) | **Eşya kapalı durumuna dönüş**<br>![Eşya kapalı durumuna dönüş](https://raw.githubusercontent.com/IBullseyea/RimDroidReadability/main/assets/images/items-off-return.jpg) |

</details>

## Özellikler

- Bağımsız yazı (18/22/26/32) ve işaret (8/16/24 arayüz birimi) boyutları.
- Kolonistler için artı, eşyalar için kare işaret; yüksek karşıtlık ve yakın eşya yığınlarını gruplama.
- **Bul** içinde kolonistler, eşya kategorileri, tür adına arama, toplam adet/yığın bilgileri ve yakınlığı koruyan **Git** eylemi.
- Sağ altta, zaman kontrollerinin alt bölgesinde **Eşya: …** kısayolu: Kapalı → Tümü → İlaç → Odun → Silah → Yiyecek → Kaynak → Giysi.
- Oyunun dilini otomatik izleyen 29 çeviri; Türkçe, İngilizce ve iki Çince dahil. Ayrı mod dil ayarı yoktur. Diğer topluluk dillerinde oyunun İngilizce geri dönüşü kullanılır.

Kapsam mevcut haritadaki serbest kolonistler ve yerdeki eşyalardır. Sisli hedefler, envanter, hayvanlar, düşmanlar, binalar ve dünya haritası hedefleri dahil değildir.

## Kurulum

1. [Releases](https://github.com/IBullseyea/RimDroidReadability/releases) bölümünden **RimDroidReadability-0.2.4.zip** dosyasını indirin. Otomatik **Source code** arşivleri derlenmiş mod DLL’sini içermez.
2. ZIP içindeki `RimDroidReadability` klasörünü ilgili RimDroid instance’ının `Mods` klasörüne çıkarın veya mod içe aktarmayı kullanın. Güncellerken **tüm klasörü**, `1.6/Languages` dahil güncelleyin.
3. Mod listesinde etkinleştirip oyunu tamamen yeniden başlatın. Mod, RimDroid’in zaten yüklediği Harmony’yi kullanır; pakette ikinci Harmony yoktur.
4. **Seçenekler → Mod Ayarları → RimDroid Readability** içinde **İşaretleri aç** seçeneğini etkinleştirin. Mod ilk kullanımda **kapalıdır**.

**Bul → Kolonistler → Git** ile kolonisti seçip merkezleyin. **Eşyalar** bölümünde kategori ve aramayı birlikte kullanın; tür satırını **Aç** ile genişletip bir yığına **Git** deyin. Yakınlık korunur; kamera hedefi takip etmez. Sağ alt kısayol aynı kategori tercihini kaydeder. Kapatmak için **İşaretleri aç** seçeneğini kaldırın veya mod listesinden devre dışı bırakıp yeniden başlatın.

## Uyumluluk ve sınırlar

**Native Unity Android** motoru, **RimWorld 1.6.4633 rev1254** ve önceden yüklenmiş tek Harmony 2.x gerekir. Linux masaüstü, Box64 ve alternatif Native Mono yolları destek kapsamı dışındadır. Tam DLL/API kimliği uyuşmazsa mod kapanır ve Mod Ayarları’nda nedenini gösterir. Doğrulanmış DLL hash/MVID bilgileri [İngilizce README](README.md) içindedir.

Kullanıcı **Samsung S25 FE / RimDroid 0.3.0v2** üzerinde çalışan İngilizce arayüz örnekleri paylaştı. Yerel kontroller derleme, mantık, çeviri, API ve paket bütünlüğünü kapsar; bütün dillerin fontları ve kapsamlı cihaz/performans kabulü henüz tamamlanmadı.

Haritada en fazla 120 işaret ve 40 metin çizilir; Bul bütün uygun hedefleri listeler. Eşya taraması 250 ms aralıkla yenilenir; yoğun depoda maliyet eşya sayısına bağlıdır. Arama `OrdinalIgnoreCase` kullanır; Türkçe noktalı İ ile i farklıdır.

## Kaynak ve katkı

Derleme için Python 3, kurulu .NET SDK/runtime ve yukarıdaki kimliğe uyan kendi oyun referanslarınız gerekir. Araçlar Linux üzerinde doğrulanmıştır; bağımlılık kurulumu veya NuGet restore yapmaz.

```sh
python3 tools/build.py --msbuild --game-managed-dir "/tam/yol/RimWorldLinux_Data/Managed"
python3 tools/validate.py --game-managed-dir "/tam/yol/RimWorldLinux_Data/Managed"
```

Statik denetim kurulu `ilspycmd` gerektirir; gerekirse `ILSPYCMD` ile tam yolunu belirtin. Çıktılar `dist/RimDroidReadability-0.2.4.zip` ve `.sha256` dosyasıdır. Oyun referansları salt okunur; raporlar ve önbellekler yalnız yerel `.build/` klasöründe tutulur.

[Katkı rehberi](CONTRIBUTING.md) ek bilgi içerir. Hata bildirirken sürümleri, motoru, telefonu, UI ölçeğini ve Mod Ayarları durumunu/ilgili logu ekleyin.

Özgün mod kodu, çevirileri ve metin belgeleri [MIT lisanslıdır](LICENSE). Bu resmî olmayan bir moddur. RimWorld, RimDroid, Unity ve Harmony kendi hak sahiplerine ve lisanslarına tabidir. Ekran görüntülerindeki oyun görselleri modun MIT lisansına dahil değildir. Oyun dosyaları ve üçüncü taraf bağımlılıkları dağıtılmaz.
