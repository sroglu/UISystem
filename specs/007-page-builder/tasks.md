# Tasks: UISystem Page Builder

**Branch**: `007-page-builder` | **Date**: 2026-04-11

---

## Phase 1 — Core (P1)

### T001: ComponentRegistry oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/ComponentRegistry.cs`

Static class — 27 M3 component için metadata:
- `ComponentInfo` struct: DisplayName, Category, ComponentType, Factory delegate
- Kategori grupları: Actions, Selection, Input, Containment, Communication, Navigation
- Her component sensible default'larla Factory delegate'e sahip
  - Örn: `M3Button` → `() => new M3Button { Variant = ButtonVariant.Filled, Text = "Button" }`

**Checkpoint**: ComponentRegistry compile olur, tüm 27 component type resolve eder.

---

### T002: Builder Scene oluştur
**File**: `Assets/UISystem/Editor/Scenes/PageBuilder.unity`

Scene içeriği:
- Main Camera
- `PageBuilder` empty GameObject
- UIDocument GameObject:
  - `UIDocument` component (PanelSettings referansı)
  - Root visual tree: tek `VisualElement` named "page-root" (`flex-grow: 1; background-color: var(--m3-surface);`)
- ThemeBootstrapper: ThemeManager initialization

**Checkpoint**: Scene yüklenir, Scene View'da surface-colored boş canvas görünür.

---

### T003: PageBuilderSceneManager oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/PageBuilderSceneManager.cs`

Static helper class:
- `LoadBuilderScene()` — `EditorSceneManager.OpenScene` ile scene yükler
- `IsBuilderSceneLoaded()` — yüklü scene'ler arasında "PageBuilder" arar
- `GetBuilderUIDocument()` — builder scene'deki UIDocument'ı bulur
- `GetBuilderScenePath()` — scene asset yolunu döner
- `CreateBuilderScene()` — scene yoksa sıfırdan oluşturur (fallback)

**Checkpoint**: `LoadBuilderScene()` çağrısı scene'i yükler, `GetBuilderUIDocument()` doğru UIDocument'ı döner.

---

### T004: PageBuilderWindow — temel yapı
**File**: `Assets/UISystem/Editor/PageBuilder/PageBuilderWindow.cs`

EditorWindow:
- `[MenuItem("Game Tools/Page Builder")]` ile açılır
- `OnEnable`: builder scene bağlantısını kontrol et, scene event callback'lerini kaydet
- `OnGUI` layout:
  - Toolbar (üst): Export UXML butonu
  - Component palette (sol/üst bölüm) — ComponentPalette helper
  - Hierarchy tree (orta bölüm) — HierarchyTreeView helper
- Scene bağlı değilse: "Load Builder Scene" butonu göster
- UIDocument referansını cache'le, scene callback'lerinde yenile

**Checkpoint**: `Game Tools > Page Builder` → window açılır, "Load Builder Scene" butonu çalışır, bağlantı kurulur.

---

### T005: ComponentPalette oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/ComponentPalette.cs`

IMGUI helper class:
- `ComponentRegistry`'den component listesini çeker
- Kategori başına `EditorGUILayout.Foldout` grubu
- Her component bir buton: tıklandığında `OnComponentSelected(ComponentInfo)` callback tetiklenir
- Scroll view ile uzun liste desteği
- Palette layout: kompakt grid veya dikey liste

**Checkpoint**: Palette tüm kategorileri ve componentleri gösterir, tıklama callback tetikler.

---

### T006: HierarchyTreeView oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/HierarchyTreeView.cs`

`UnityEditor.IMGUI.Controls.TreeView` extension:
- `BuildRoot()`: UIDocument'ın visual tree'sini yürüyerek `TreeViewItem` oluşturur
- Her item: element type + name/text gösterir (ör. "M3Button (Filled) — Save")
- `SelectionChanged`: seçili `VisualElement` referansını saklar, Scene View highlight
- Sağ tık context menu: "Delete" seçeneği
- `Refresh()`: tree'yi yeniden build eder (component ekle/sil sonrası)

**Checkpoint**: Tree doğru visual tree yapısını gösterir, seçim ve silme çalışır.

---

### T007: UxmlExporter oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/UxmlExporter.cs`

Static class:
- `ExportToUxml(VisualElement root, string filePath)` — ana method
- Depth-first tree walk:
  - M3ComponentBase subclass → `<components:M3[Name]>` tag, [UxmlAttribute] reflection
  - VisualElement (container) → `<ui:VisualElement style="...">` inline flex properties
- Non-default attribute detection: default instance ile karşılaştırma
- USS style reference toplama: component type → USS dosya yolu mapping
- XML header: `xmlns:ui`, `xmlns:components` declarations
- `<ui:Style src="...">` tag'leri otomatik eklenir

**Checkpoint**: 3-4 component ekle → export → oluşan UXML valid ve yeni UIDocument'ta aynı sonuç.

---

### T008: Export butonunu wire'la
**File**: `PageBuilderWindow.cs` (güncelleme)

- Toolbar'a "Export UXML" butonu ekle
- Tıklandığında `EditorUtility.SaveFilePanel` aç
- `UxmlExporter.ExportToUxml(pageRoot, path)` çağır
- `AssetDatabase.Refresh()` ve asset'i ping et

**Checkpoint**: End-to-end: component ekle → Export UXML → dosya oluşur → yeni UIDocument'ta doğru render.

---

## Phase 2 — Polish + Import (P2)

### T009: PropertyInspector oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/PropertyInspector.cs`

IMGUI helper class:
- Input: seçili `VisualElement` referansı
- M3ComponentBase subclass ise:
  - Reflection ile `[UxmlAttribute]` property'leri bul
  - Type'a göre IMGUI field: string→TextField, bool→Toggle, enum→EnumPopup, int/float→number
  - Property değişikliği → component'e set → `Repaint()` tetikle
- Layout container ise:
  - flex-direction, justify-content, align-items → EnumPopup
  - padding (4x float) → FloatField
- Tüm değişiklikler anında Scene View'a yansır

**Checkpoint**: Button seç → variant değiştir → Scene View güncellenir.

---

### T010: Theme toggle ekle
**File**: `PageBuilderWindow.cs` (güncelleme)

- Toolbar'a light/dark toggle butonu ekle
- Tıklandığında `ThemeManager.ToggleLightDark()` çağır
- Buton ikonu/text'i mevcut temayı yansıtsın

**Checkpoint**: Toggle → tüm componentler tema değiştirir.

---

### T011: Hierarchy drag-and-drop
**File**: `HierarchyTreeView.cs` (güncelleme)

TreeView override'ları:
- `CanStartDrag` → true
- `SetupDragAndDrop` → drag data hazırla
- `HandleDragAndDrop` → hedef parent/index belirle, `parent.Insert(index, element)` çağır
- Drop sonrası `Reload()` ile tree rebuild

**Checkpoint**: Element'i tree'de sürükle → visual tree'de sıra değişir.

---

### T012: Add Container komutu
**File**: `ComponentPalette.cs` (güncelleme) + `PageBuilderWindow.cs`

- Palette'in en üstüne "Add Container" butonu ekle
- Tıklandığında `new VisualElement()` oluştur:
  - `flex-direction: column`, `padding: 8px`
  - Hierarchy'de "Container" olarak görünsün
- Seçili element'in child'ı olarak ekle

**Checkpoint**: Container ekle → child component ekle → nesting çalışır.

---

### T013: UxmlImporter oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/UxmlImporter.cs`

Static class:
- `ImportFromUxml(string filePath, VisualElement pageRoot)` — ana method
- XML parser (`XDocument` veya `XmlDocument`) ile UXML dosyasını oku
- `<components:M3[Name]>` tag'leri → ComponentRegistry'den type lookup → Factory ile instantiate
- XML attribute'ları → reflection ile `[UxmlAttribute]` property'lere set
- `<ui:VisualElement>` tag'leri → layout container, inline `style` attribute parse
- Child elementler recursive
- Hata durumunda: try-catch + `Debug.LogWarning` + mevcut tree temizleme
- pageRoot'u clear edip import edilen tree ile değiştir

**Checkpoint**: Export → Import → aynı tree yapısı reconstruct edilir.

---

### T014: Import butonunu wire'la
**File**: `PageBuilderWindow.cs` (güncelleme)

- Toolbar'a "Import UXML" butonu ekle
- Tıklandığında `EditorUtility.OpenFilePanel` aç (.uxml filtre)
- Mevcut tree boş değilse "Overwrite?" uyarısı göster
- `UxmlImporter.ImportFromUxml(path, pageRoot)` çağır
- Hierarchy rebuild

**Checkpoint**: UXML import → tree yüklenir → düzenle → re-export → round-trip çalışır.

---

## Phase 3 — Presets (P3)

### T015: LayoutPresets oluştur
**File**: `Assets/UISystem/Editor/PageBuilder/LayoutPresets.cs`

Static class — preset tanımları:
- `ButtonRow`: horizontal flex, 3x M3Button (Filled/Outlined/Text)
- `CardList`: vertical flex, 3x M3Card (Elevated) with title
- `CardGrid`: 2-column wrap layout, 4x M3Card (Filled)
- `HeaderContentFooter`: vertical flex with M3TopAppBar + scroll area + M3NavigationBar
- `FormLayout`: vertical flex with 3x M3TextField + M3Toggle + M3Button

Her preset `Func<VisualElement>` factory method.

**Checkpoint**: Her preset doğru component hierarchy oluşturur.

---

### T016: Palette'e Layouts kategorisi ekle
**File**: `ComponentPalette.cs` (güncelleme)

- Component kategorilerinin üstüne "Layouts" foldout ekle
- Preset'leri buton olarak listele
- Tıklandığında preset factory çağır → pageRoot'a ekle
- Hierarchy rebuild

**Checkpoint**: "Card Grid" preset → 2x2 card layout tek tıkla oluşur.
