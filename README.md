# RimDroid Readability — 0.2.3

RimWorld 1.6’yı **RimDroid Native Unity** üzerinden telefonda oynamak için özgün, isteğe bağlı mod: kamera yakınlığından bağımsız büyük göstergeler, eşya gruplama ve dokunmatik hedef bulucu. **Gerçek telefon uyumluluğu, okunabilirlik, dokunma ve performans kabulü beklemededir.** Yerel derleme bu kabulün yerine geçmez.

## 0.2.3 — Haritada eşya filtresi kısayolu

Sağ altta, zaman durdurma/hız kontrollerinin alt bölgesinde **Eşya: <kategori>** düğmesi vardır. Mevcut görünüm simgelerinin solundaki boşlukta, alt menü çubuğunun hemen üstündedir. Her dokunuş **Kapalı → Tümü → İlaç → Odun → Silah → Yiyecek → Kaynak → Giysi → Kapalı** sırasını bir adım ilerletir. Seçim kaydedilir; harita, Bul ve mod ayarları aynı filtreyi kullanır. Bulucuda arama varsa kategoriyle birlikte uygulanmaya devam eder. Oyun zamanı veya kamera bu düğmeyle değişmez.

Düğme 192 × 48 arayüz birimidir. Öğretici/tooltip HUD öğeleri erişimi kapatmaz; ayarlar gibi oyun diyalogları öndeyken, mod kapalıyken ve dünya görünümünde gizlenir. UI ölçeği/ekran değişiminde konumu yenilenir; sığmayan ölçüde mevcut kontrollere binmek yerine gizlenir. Etiket ve işaretler iki mod düğmesinin alanından da kaçınır. Gerçek Android kontrol katmanıyla konumu ve tek dokunuş davranışı yeni telefon testinde doğrulanmalıdır.

## 0.2.2 düzeltmesi

Kolonist sorgusunun oyuna ait geçici listesini dolaşırken yeniden sorgulaması, `Collection was modified` çizim hatasına ve modun kendini kapatmasına yol açıyordu. Katalog yenilemesi artık kolonist/eşya sorgularının sahip olduğu dizi kopyaları üzerinde dolaşır; hedefin güncel uygunluğu yine çizim ve Git öncesinde denetlenir. Hata aynı katalog kaynak koduyla, oyun/Unity yerine özgün API fikstürleri kullanılarak yeniden üretildi ve düzeltmeyle geçti. Başka bir çalışma hatası olursa tek mod loguna tam exception/çağrı izi eklenir; ayar ekranındaki kısa durum mesajı korunur.

## Kullanım ve ayarlar

İlk kullanımda mod **kapalıdır**. Seçenekler → Mod Ayarları → **RimDroid Readability** içindeki başlangıç durumunu okuyup **İşaretleri aç** seçeneğini etkinleştirin.

- Kolonist göstergeleri ayrı kapatılabilir. Kolonistler artı şekli ve **K**, eşyalar kare ve **E** ile ayrılır; yalnız renge bağlı değildir.
- Yazı boyutu **18 / 22 / 26 / 32**, işaret boyutu **8 / 16 / 24** arayüz birimidir. İkisi bağımsızdır; kamera yakınlığı ve nesnelerin gerçek boyutu değişmez. Arayüz ölçeği fiziksel boyutu etkiler.
- Varsayılan yazı **22**, işaret **16**, yakın yığınları gruplama **açık**, eşya filtresi **Kapalı**, kolonistler **açık** durumdadır.
- Eski `enabled`, `colonists`, `itemFilter`, `fontSize` anahtarları ve Kapalı=0/İlaç=1/Odun=2 sayıları korunur. Eski Odun/26 tercihi değişmez. Eksik `markerSize`/`groupItems` alanları 16/açık olur; bozuk boyut veya filtre yalnız kendi varsayılanına döner. Oyun kayıtları dönüştürülmez.

Haritada büyük **Bul** kontrolü görünür. **Kolonistler** sekmesinde ekran dışındaki uygun kolonisti de görebilir, **Git** ile seçip merkezleyebilirsiniz. Önceki yakınlık korunur; kamera daha sonra hedefi takip etmez. Başarılı Git pencereyi kapatır. Kaybolmuş/sisli/başka haritadaki hedefte kamera ve seçim değişmez; bilgi gösterilip liste yenilenir.

**Eşyalar** sekmesinde kategori düğmesi ve tür adına arama birlikte çalışır. Örneğin **Odun** seçin → tür satırındaki **Aç** → miktarı, hücresi ve kimliği görünen yığındaki **Git**. Tür satırı toplam adet ve yığın sayısını ayırır. **20 + 30 = 50 adet / 2 yığın**. Bulucudaki tür toplamı mevcut haritanın tamamındaki uygun yığınları kapsar; ekrandaki işaret sayısıyla sınırlanmaz.

**Temizle**, **Geri**, **Önceki/Sonraki** ve **Kapat** kontrolleri vardır. En fazla 12 satırlık sayfalar ekran yüksekliğine göre azalır. Eylem alanları en az **48 × 48 arayüz birimidir**; içerik kısa ekranlarda dikey, çok dar ekranlarda yatay kaydırılır. Uzun adlar satır yüksekliği artırılarak tam gösterilir. Arama, sekme, sayfa ve hedef başvuruları diske kaydedilmez. Arama pencere kapalıyken de harita filtresine uygulanır; **Temizle** ile kaldırılabilir. Harita/kayıt değişimi veya modun kapatılması aramayı temizler.

## Kategori kapsamı

Tek etkin kategori vardır; özellikleri örtüşen bir yığın sorguda yalnız bir kez sayılır.

| Kategori | Dahil etme kuralı |
| --- | --- |
| Kapalı | Eşya işareti ve sonucu yok |
| Tümü | Bütün uygun yerdeki taşınabilir eşyalar |
| İlaç | `IsMedicine` |
| Odun | `WoodLog` |
| Silah | `IsWeapon` |
| Yiyecek | `ingestible` ve `CachedNutrition > 0` |
| Kaynak | `CountAsResource` veya odun |
| Giysi | `IsApparel` |

Arama oyun dilindeki tür adına, uç boşlukları temizlenerek `OrdinalIgnoreCase` alt metin eşlemesi uygular. Boş arama tüm kategoriyi gösterir. Türkçe ç/Ç eşleşir; **İ ile i aynı sayılmaz**. Noktalı İ içeren adlarda oyunun gösterdiği harfi kullanın. Tam uzun ad aranabilir.

Yalnız mevcut haritada doğmuş, yok edilmemiş, sis dışında olan **serbest kolonistler ve yerdeki eşyalar** alınır. Serbest kolonist kapsamı oyunun `FreeColonistsSpawned` listesidir. Hayvan, düşman, bina, bitki, envanter, başka harita ve dünya hedefleri kapsam dışındadır. Sisli hedefler toplam ve sonuç sayılarına da katılmaz.

## Harita gösterimi ve sınırlar

Harita grupları aynı türün aynı **64 × 64 arayüz birimlik hücresindeki** ekran içi yığınlarını birleştirir. Hücre sınırı ayrı grup oluşturabilir. Farklı tür, ekran dışı veya sisli üyeler aynı görsel toplama katılmaz. Gruplama kapatılabilir.

Seçilmiş uygun hedef, ardından kolonistler ve kararlı kimlik sırası önceliklidir. En fazla **120 işaret ve 40 metin** çizilir. İşaretlerin gerçek boyutu, diğer metinler, ekran sınırları ve Bul kontrolü yerleşimde hesaba katılır. Sığmayan metin atlanır; işareti kalabilir. **Bul (+N)** içindeki N, tam metin etiketi çizilemeyen görsel grup sayısıdır; ham yığın sayısı değildir. Bulucunun sonuçları bu çizim sınırıyla kesilmez.

Katalog üyeliği **250 ms** arayla; harita, kayıt, kategori ve arama değişiminde hemen yenilenir. Sis, miktar ve hedef uygunluğu her çizim/eylem öncesinde tekrar okunur. Yeni doğan/keşfedilen hedefler en fazla 250 ms gecikebilir. Eşya filtresi Kapalı iken eşya taraması yapılmaz. **Tam tarama maliyeti eşya sayısına bağlıdır; 120 işaret sınırı tarama maliyetini sınırlamaz.** Yoğun depo performansı telefonda ölçülmelidir.

Harita işaretleri girdi almaz. Bulucu oyun pencere sırasına katılır; öndeki oyun penceresi varken küçük Bul kontrolü etkinleşmez. Bulucu içindeki dokunuşun haritaya geçmemesi ve mevcut sürükleme/yakınlaştırma/uzun basma davranışları cihaz kabulünde kontrol edilmelidir.

## Uyumluluk kapısı

Başlangıçta Android üzerinde Native Unity, `RIMDROID_IL2MONO_MANAGED`, RimWorld 1.6, tam API imzaları, tek oyun assembly’si ve **önceden yüklenmiş tek Harmony 2.x** denetlenir. Yalnız doğrulanmış referans kabul edilir:

**0.2.1 başlangıç düzeltmesi:** RimDroid 0.3.0 Native köprüsü Android’de `Application.platform` değerini `LinuxPlayer` olarak bildirir. Bu nedenle AndroidPlayer veya **LinuxPlayer + Android işletim sistemi bilgisi** kabul edilir; her iki yolda da Native motorunun `RIMDROID_IL2MONO_MANAGED` işareti zorunludur. Salt Linux/Box64 veya `box64 + native Mono` desteği eklenmez. Başarısızlık mesajı platformu, işletim sistemini ve Native işaretinin var/yok durumunu gösterir. DLL/MVID, RimWorld 1.6, API ve tek Harmony denetimleri aynen devam eder.


- Oyun bildirimi: `1.6.4633 rev1254` (kurulum paketi revizyonu farklı görünebilir).
- `Assembly-CSharp.dll` SHA-256: `39c5532c8f9fd8b31cdc9450d3148bc2f372f03f030dc5cddd170c9a08774076`.
- MVID: `c337411a-5cb6-49b6-8f4c-0609acf16d96`.
- Unity referansı: `2022.3.35f1`.

Telefonun DLL kimliği ayrıca ölçülmelidir; PC referansıyla derlenmesi Android’de eşleştiği anlamına gelmez. Farklı build için kapı gevşetilmez, ayrı API/kimlik doğrulaması gerekir. Native dışı motorlar, Prepatcher/BepInEx ve oyun assembly’sini değiştiren kurulumlar bu kabul kapsamına alınmaz.

İki Harmony kancası tek kurulum olarak yönetilir. Kısmi hata bu modun kancalarını geri alır. Uyumsuzluk, assembly değişimi veya mod hatası bütün özellikleri kapatır; geçici pencereler/katalog temizlenir ve bir mod tanısı üretilir. Çizim yalnız Repaint kullanır; etkileşim ayrı `Verse.Window` yolundadır. Değiştirilen GUI durumu hata halinde de geri yüklenir. Kaynak depodaki `docs/api-adaptation.md` uyarlama notu erişim ve imza kararlarını açıklar.

RimDroid Native kaynak referansı Harmony 2.4.1, komşu indirilen Harmony modu 2.4.3 kullanmaktadır. Mod mevcut runtime’a reflection ile bağlanır; derleme bağımlılığı veya dağıtılan ikinci Harmony yoktur. Telefonun gerçek Harmony kimliği ve kanca çalışması henüz doğrulanmadı.

## Oyun dosyalarını değiştirmeden derleme

Kurulu .NET SDK, Python 3 ve statik denetim için mevcut `ilspycmd` gerekir. Araçlar bağımlılık kurmaz, NuGet restore yapmaz, oyunu veya APK’yı başlatmaz. Oyun/Unity/framework DLL’leri yalnız kullanıcının referans klasöründen okunur.

Modun Git kökünden, aynı doğrulanmış referansla:

```sh
python3 tools/build.py --msbuild --game-managed-dir "/tam/yol/RimWorldLinux_Data/Managed"
python3 tools/validate.py --game-managed-dir "/tam/yol/RimWorldLinux_Data/Managed"
```

Doğrudan çevrimdışı Roslyn yolu:

```sh
python3 tools/build.py --game-managed-dir "/tam/yol/RimWorldLinux_Data/Managed"
```

`ILSPYCMD` ortam değişkeniyle kurulu `ilspycmd` dosyasının tam yolu verilebilir. Yerel çalışma alanında isteğe bağlı varsayılan, mod kökünün iki üstündeki `RimWorld/game/RimWorldLinux_Data/Managed` konumudur; bağımsız klonda **`--game-managed-dir` kullanın**.

MSBuild’i Python başlatıcısıyla çalıştırın; CLI/runtime geçici dosyaları, önbellekler ve test çıktıları `.build/` altında kalır. C# 7.3, AnyCPU, .NET Framework 4.7.2 hedeflenir; modern .NET runtime mod DLL’sine bağlanmaz. Saf mantık ve katalog uyarlayıcı testleri yalnız özgün kodu/API fikstürlerini ve kurulu uyumlu .NET runtime’ını kullanır; oyun/Unity kodu çalıştırmaz. Referans DLL hash ve değişiklik zamanları derleme öncesi/sonrası karşılaştırılır.

Çıktılar:

- `1.6/Assemblies/RimDroidReadability.dll`
- `dist/RimDroidReadability-0.2.3.zip`
- Yerel kanıt: `docs/validation.json`, `docs/static-validation.json` (Git’e alınmaz).

ZIP izin listesi: yalnız `About/About.xml`, `LoadFolders.xml`, özgün mod DLL’si, `README.md`, `RELEASE.md`. Oyun, Unity, Harmony DLL’leri, oyun görselleri, kaynak referanslar, önbellekler ve raporlar pakete konmaz. Kaynak/test/OpenSpec belgeleri Git deposundadır. Lisans seçilmediğinden açık kaynak lisansı varmış gibi dağıtım yapılmaz.

## Kurulum, kapatma ve kalan kabul

1. Gerçek telefonun Native rotasını, APK ve oyun sürümünü, orijinal oyun DLL SHA/MVID’sini ve yüklü Harmony kimliğini doğrulayın.
2. ZIP’i RimDroid’in mod içe aktarma yöntemiyle kurun veya tek `RimDroidReadability` klasörünü ilgili instance’ın `Mods` klasörüne yerleştirin. Modu etkinleştirip oyunu normal biçimde yeniden başlatın. İkinci Harmony veya oyun DLL’si kopyalamayın.
3. Mod Ayarları’nda başarılı başlangıç durumunu okuyup modu açın; boyutları seçin ve Bul akışını deneyin.
4. Kapatmak için **İşaretleri aç** seçeneğini kaldırın veya mod listesinden devre dışı bırakın. Mod göstergeleri ve pencereleri kaldırılır; tercihler korunur. Oyun kayıtları ve tanımları dönüştürülmez. Gerekirse önceki 0.1.0 paketine dönülebilir.

Yerel mantık/araç/API/paket testleri gerçek Harmony kancası, Unity kamera hareketi, dokunma veya FPS kabulü değildir. Cihaz senaryoları ve kanıt sınırları [RELEASE.md](RELEASE.md) içindedir. Lisans, GitHub depo adı ve kamuya yayın kararı kullanıcıya aittir; yayın henüz yapılmadı.
