# 0.2.1 yayın hazırlığı

24 Ekim 2026. Bu paket yerel geliştirme çıktısıdır. **Gerçek telefon kabulü, lisans ve GitHub depo kararı beklemededir.** Kamuya yayın yapılmadı; yayına hazır olduğu iddia edilmez.

## 0.2.1 başlangıç düzeltmesi

Samsung S25 FE / RimDroid 0.3.0v2 ile paylaşılan ayar görüntüsünde 0.2.0’ın ilk runtime kapısında durduğu görüldü. RimDroid 0.3.0 kaynak referansı, Native köprünün Android’de kasıtlı olarak `LinuxPlayer` döndürdüğünü doğruluyor. 0.2.0’ın Android enum’una bağlı kontrolü bu yolu hatalı reddediyordu.

0.2.1, Native işaretine ek olarak gerçek Android oyuncusunu veya LinuxPlayer + Android işletim sistemi bilgisini kabul eder. Diğer platformlar, eksik Native işareti ve masaüstü Linux reddedilir. Runtime hatasında platform/işletim sistemi/Native işareti tanısı görünür. Sürüm/DLL/MVID/API/Harmony kapıları korunur. Yeni paketin telefonda başarılı başlatılması henüz doğrulanmadı; eski görüntüler göstergelerin kabul kanıtı değildir.

## Değişiklikler

- Kameradan bağımsız 18/22/26/32 yazı ve 8/16/24 işaret; karşıt arka plan, K/artı ve E/kare tür ayrımı.
- Büyük Bul kontrolü, Kolonistler/Eşyalar bölümleri, kategori ve tür adına arama.
- Haritada 64 birim hücre grupları; bulucuda harita çapında tür toplamı ve ayrı yığınlar. Adet/yığın ayrımı, kimlik ve konum gösterimi.
- Git ile tek seferlik seçim/merkezleme ve yakınlığı koruma; eylem anında hedef denetimi. Otomatik takip yok.
- Eski ayar anahtarları ve 0/1/2 filtre değerlerinin korunması; eksik yeni alanlara 16/açık varsayılanı.
- İki kancanın tek uyumluluk kapısıyla kurulması, kısmi kurulum geri alma, hata/assembly değişiminde kapanma ve eski pencere/katalog temizliği.

Kapsam yalnız mevcut haritadaki serbest kolonistler ve yerdeki eşyalardır. Hayvan, düşman, bina, envanter ve dünya hedefleri eklenmedi. Oyun, APK veya Unity dosyaları değiştirilmedi.

## Yerel kanıt ve sınırı

Yerel Roslyn derlemesi, saf C# mantık kontrolleri, Python araç/negatif paket testleri ve statik API/paket denetimi geçti. Kontroller; ayar geçişi, görünürlük/uygunluk kombinasyonları, kategori/arama, gruplama/toplamlar, 120’den fazla hedefe eksiksiz sayfalama, boyut/yerleşim, öncelik/bütçe ve yaşam döngüsü mantığını kapsar. Negatif paket denetimleri eski/ek/eksik/tekrarlı içerik, yanlış sürüm yönlendirmesi, yabancı DLL ve optimize Python altında reddi sınar.

Komşu oyun klasörü olmayan `/tmp` altındaki bağımsız kaynak kopyasında `--msbuild --game-managed-dir` ve `validate.py --game-managed-dir` geçti; aynı kaynaklar aynı DLL’yi üretti. Referans DLL’ler kopyalanmadı. Derleme 102 salt okunur referansın hash ve zaman damgalarının değişmediğini denetler. Statik denetim gerçek API imzalarını, assembly sürümünü, AnyCPU/framework referanslarını ve ZIP’in her payload baytını doğrular.

Son entegrasyon komutları:

```sh
python3 tools/build.py --msbuild
python3 tools/validate.py
openspec validate improve-mobile-map-readability --strict
git diff --check
```

Makineye özel kanıt dosyaları `docs/validation.json`, `docs/static-validation.json` ve `.build/portable-check.json` içindedir; Git/paket dışında tutulur. **Bu testler gerçek Harmony kurulumu, Unity GUI/kamera, Android dokunma, okunabilirlik veya FPS kabulü değildir.**

## Telefon kabulü — yapılmadı

OpenSpec görevleri **6.3, 6.4, 6.5 açık** kalır. Telefon modeli S25 FE ve RimDroid sürümü 0.3.0v2 kullanıcı tarafından bildirildi; gerçek oyun sürümü, motor, UI ölçeği ve DLL/Harmony kimliği henüz doğrulanmadı; oyun/APK başlatma veya cihaza dağıtım yapılmadı.

1. Model, çözünürlük, UI ölçeği, APK sürümü/Native rotası, oyun tam sürümü, DLL SHA/MVID ve yüklü Harmony kimliğini kaydedin. Başarılı mod başlatma logunu alın. Uyuşmazlıkta kapıyı gevşetmeyin; build ayrı doğrulanmalıdır.
2. Aynı sahnede iki kamera uzaklığında boyutları, ekran dışı koloniste Git ve yakınlığın korunmasını; kategorileri/arama/temizlemeyi; yoğun depoda adet/yığın ve tek yığına Git akışını kontrol edin. Ekran görüntülerini ve sonuçları kaydedin.
3. Sis, yok edilmiş hedef, harita/kayıt değişimi, kapatma/tekrar açma ve tercihlerin korunmasını kontrol edin. Bulucu içindeki dokunuşların haritaya geçmediğini, öndeki oyun pencerelerinin önceliğini; normal sürükleme/yakınlaştırma/seçim/uzun basmayı deneyin.
4. Aynı kayıt, kamera, oyun hızı ve cihaz ayarlarında mod kapalı/açık, bulucu kapalı/açık ve yoğun depo koşullarını karşılaştırın. Süre, kare süresi/FPS ve dokunma gecikmesini kaydedin; okunabilirlik ve hedef bulma kabulünü kullanıcı ayrıca değerlendirsin.

## Kurulum ve geri dönüş

README’deki Native/DLL/Harmony kimlik kontrolünü tamamlayın. `dist/RimDroidReadability-0.2.1.zip` içindeki tek mod klasörünü ilgili instance’a kurup etkinleştirin; yeniden başlatınca Mod Ayarları’ndaki durumu okuyun. İlk kullanımda kapalıdır. İkinci Harmony veya oyun DLL’si eklemeyin.

Kapatmak için **İşaretleri aç** seçeneğini kaldırın veya mod listesinden devre dışı bırakın. Göstergeler ve mod pencereleri kaldırılır; tercihler korunur. Gerekirse eski 0.1.0 paketine dönülebilir. Oyun dosyaları ve kayıtları dönüştürülmez.

## Bilinen sınırlar ve yayın kararları

- Referans SHA/MVID farklıysa mod devreye girmez; telefon build’i henüz doğrulanmadı.
- 48 arayüz birimi fiziksel Android dp değildir; gerçek UI ölçeğinde denenmelidir.
- Eşya tarama maliyeti çizim bütçesiyle sınırlanmaz; yoğun depo performansı ölçülmedi. Üyelik yenilemesi 250 ms gecikebilir.
- Harita hücre sınırında aynı tür iki grup kalabilir. Çizim sınırı 120 işaret/40 metindir; Bul (+N) tam etiketi çizilemeyen görsel grupları sayar.
- Arama `OrdinalIgnoreCase` kullanır; noktalı İ ve i farklıdır. Uzun adın tamamı bulucuda kaydırılarak okunabilir.
- Lisans **seçilmedi**. Lisans dosyası eklenmedi ve açık kaynak lisansı iddiası yoktur.
- GitHub depo adı/hesabı **seçilmedi**. Remote/push/yayın yapılmadı. Bunlar cihaz kanıtıyla birlikte son yayın kararlarıdır.

Paket izin listesi yalnız özgün mod DLL’si, About/LoadFolders XML, README ve bu belgedir. Kaynak/test/OpenSpec planı Git deposundadır; oyun/Unity/Harmony DLL’leri, görseller, kimlik bilgileri, üretilmiş raporlar ve önbellekler dağıtılmaz.
