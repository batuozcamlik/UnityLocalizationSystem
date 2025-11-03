# Unity Localization Grid (Index-Based JSON Localization)

Küçük/orta ölçekli Unity projeleri için **index tabanlı**, **JSON** dosyasıyla çalışan, hem **runtime API** hem de **Editor penceresi** sunan hafif bir lokalizasyon sistemi.

> 🔧 Öne Çıkanlar
- Index bazlı dil sistemi (0 = Default)
- JSON tabanlı veri kaydı
- Kolon / satır otomatik hizalama
- Editor grid arayüzü
- Dil & kelime ekleme / silme
- Runtime API
- Eksik çeviriler için fallback
- Build’de `Application.persistentDataPath` desteği

---

## Kurulum

1) Aşağıdaki scriptleri projeye ekleyin:

- `LocalizationManager.cs`
- `LocalizationTypes.cs`
- `LocalizationEditorWindow.cs`

2) Unity Hierarchy → GameObject → `LocalizationManager` ekleyin  
   (Singleton + DontDestroyOnLoad)

3) Inspector:
- `relativeJsonPath` – JSON yolu (`Localization/localization.json`)
- `createSampleIfMissing` – yoksa örnek JSON oluştur
- `defaultLanguageName` – index 0 dil ismi

---

## Hızlı Başlangıç

```csharp
void Start()
{
    LocalizationManager.Instance.SetLanguage(1); // İngilizce örneği
    string hello = LocalizationManager.Instance.Get("Merhaba");
    Debug.Log(hello);
}
```

> `Get()` → default dilde arar → seçili dildeki karşılığı döner → yoksa default döner

---

## Editor Penceresi

Menü:
```
Window → Localization → Editor
```

Özellikler:
- Yatay: diller
- Dikey: kelimeler
- JSON path seçimi
- Kaydet / Yükle
- Dil seçimi
- Kelime filtreleme
- Yeni kelime ekle
- Yeni dil ekle
- Kelime sil
- Dil sil (default silinemez)
- Kapanırken kaydedilmemiş değişiklik uyarısı

Yeni kelime eklediğinizde diğer diller otomatik olarak `__MISSING__` değerini alır.

---

## JSON Model

```json
{
  "languages": [
    { "name": "Türkçe" },
    { "name": "English" }
  ],
  "words": [
    ["Merhaba", "Hello"],
    ["Elma", "Apple"]
  ],
  "selectedLanguageIndex": 0
}
```

- `languages[i].name` → Dil adı
- `words[row][column]` → Kelime
- `selectedLanguageIndex` → Aktif dil

---

## Runtime API

### Dil Seç
```csharp
void SetLanguage(int languageIndex)
```

### Dil Ekle
```csharp
void AddLanguage(string name)
```

### Default Kolona Kelime Ekle
```csharp
int AddWordToDefault(string newWord)
```

### Belirli Hücreye Çeviri Yaz
```csharp
void SetWordAt(int languageIndex, int wordIndex, string value)
```

### Get (default kelime ile)
```csharp
string Get(string defaultWord)
```

### GetByIndex
```csharp
string GetByIndex(int wordIndex)
```

### Default Dil içinde Arama
```csharp
List<(int index, string word)> SearchInDefault(string contains, bool caseSensitive=false)
```

---

## ✅ Örnek Kullanım

```csharp
// Çeviri al
string translateWord = LocalizationManager.Instance.Get("YeniKelime");

// Index üzerinden çeviri al
string translated = LocalizationManager.Instance.GetByIndex(index);

Debug.Log("Kelime: " + translateWord);
Debug.Log("Index: " + index);
Debug.Log("Index üzerinden: " + translated);
```

---

## Dosya Yapısı & Dağıtım

- **Unity Editor**  
  JSON → proje kökünde çözülür

- **Build Ortamı**  
  JSON → `Application.persistentDataPath`

JSON dosyanızı repository içinde saklamanız önerilir.

---

## SSS

✅ Yeni kelime ekledim → Diğer diller boş  
→ `__MISSING__` otomatik yerleştirilir

✅ Default dil adını değiştirmek  
→ Editor üst toolbar

✅ Yanlış dil ekledim  
→ Dil sütununu sil

✅ Kelime index’ini almak  
→ `SearchInDefault()` kullanılabilir

---

## Lisans & Atıf

Inspector ve Editor UI üzerinde imza yer alır:  
**Created by Batu Özçamlık — www.batuozcamlik.com**

Kullanımda atıf memnuniyetle karşılanır.

---
