# Implementation Plan: UISystem Page Builder

**Branch**: `007-page-builder` | **Date**: 2026-04-11 | **Spec**: spec.md

---

## Summary

Editor-only tool for visually composing UI pages from existing M3 components. Three main deliverables:
1. **PageBuilderWindow** — EditorWindow with component palette, hierarchy tree, property inspector, and export/import buttons
2. **PageBuilder.unity** — Dedicated editor-only builder scene with UIDocument + ThemeBootstrapper
3. **UxmlExporter / UxmlImporter** — Utilities that serialize/deserialize a VisualElement tree to/from UXML

---

## Constitution Check

| Principle | Status | Evidence |
|-----------|--------|---------|
| I. Zero Dependencies | ✅ PASS | Tüm kod `mehmetsrl.UISystem.Editor` asmdef'te; yeni assembly ref yok; `Game Tools/` menu path string-only, assembly coupling değil |
| II. SO Configuration | ✅ PASS | ComponentRegistry static data olarak component metadata tutar; hardcoded color/float constant yok |
| III. Unity Conventions | ✅ PASS | `mehmetsrl.UISystem.Editor` namespace; Odin attributes kullanılacak; `Editor/` folder yapısı |
| IV. Mobile-First | N/A | Editor-only tool, runtime performans etkisi yok |
| V. Incremental Delivery | ✅ PASS | 3 faz (P1: core, P2: polish + import, P3: presets), her biri bağımsız test edilebilir |

---

## Technical Context

- **Language/Version**: C# (Unity 6.3+, 6000.3)
- **Primary Dependencies**: UI Toolkit (Editor), Odin Inspector (editor attributes)
- **Storage**: Generated UXML files on disk
- **Testing**: Manual Unity Editor verification
- **Constraints**: Editor-only; zero cross-assembly deps; `mehmetsrl.UISystem.Editor` asmdef only

---

## Architecture

```
┌─────────────────────────────────────────────────┐
│  PageBuilderWindow (EditorWindow)                │
│  ┌────────────────────────────────────────────┐  │
│  │ Toolbar: [Theme Toggle] [Export] [Import]  │  │
│  ├───────────────────────────────���────────────┤  │
│  │ ComponentPalette (kategori foldout'ları)    │  │
│  ├────────────────────────────────────────────┤  │
│  │ HierarchyTreeView (IMGUI TreeView)         │  │
│  ├────────────────────────────────────────────┤  │
│  │ PropertyInspector (reflection-based) [P2]  │  │
│  └─────���──────────────────────────���───────────┘  │
└──────────────────┬��─────────────────────────────┘
                   │ FindObjectOfType<UIDocument>()
                   ▼
┌─────────────────────────────────────────────────┐
│  PageBuilder.unity (Editor/Scenes/)              │
│  ├── Main Camera                                 │
│  ├── UIDocument (PanelSettings + UXML source)    │
│  └── ThemeBootstrapper                           │
└──────────────────┬──────────────────────────────┘
                   │ UxmlExporter / UxmlImporter
                   ▼
              .uxml files on disk
```

---

## Project Structure

```
Assets/UISystem/Editor/
├── PageBuilder/
│   ├── PageBuilderWindow.cs          # Ana EditorWindow, IMGUI OnGUI
│   ├── PageBuilderSceneManager.cs    # Scene load/create/connect
│   ├── ComponentPalette.cs           # Kategori foldout'ları ile component listesi
│   ├── ComponentRegistry.cs          # M3 component metadata (type, category, factory)
│   ├── HierarchyTreeView.cs          # UnityEditor.IMGUI.Controls.TreeView
│   ├── PropertyInspector.cs          # Reflection-based property editor [P2]
│   ├─��� UxmlExporter.cs              # VisualElement tree → UXML serialization
│   ├─�� UxmlImporter.cs             # UXML → VisualElement tree deserialization [P2]
│   └── LayoutPresets.cs              # Hazır layout şablonları [P3]
├── Scenes/
│   └── PageBuilder.unity             # Editor-only builder scene
```

---

## Key Technical Decisions

### 1. Menu Location

`[MenuItem("Game Tools/Page Builder")]` — Parent repo'daki AnimationSystem ile aynı pattern. Assembly dependency oluşturmaz, sadece MenuItem string path.

### 2. EditorWindow ↔ Scene Communication

EditorWindow, builder scene'deki UIDocument'ı bulur:
```csharp
var docs = Object.FindObjectsOfType<UIDocument>();
var builderDoc = docs.FirstOrDefault(d => d.gameObject.scene.name == "PageBuilder");
```

Cache'lenmiş referans, `EditorSceneManager.sceneOpened`/`sceneClosing` callback'leri ile yenilenir.

### 3. Component Registry

Static class, her M3 component için metadata tutar:

```csharp
internal struct ComponentInfo
{
    public string DisplayName;     // "M3Button (Filled)"
    public string Category;        // "Actions"
    public Type ComponentType;     // typeof(M3Button)
    public Func<VisualElement> Factory;  // () => new M3Button { Variant = ButtonVariant.Filled }
}
```

Palette bu registry'yi kullanarak component listesini çizer. Her component sensible default'larla instantiate edilir.

### 4. UXML Export Strategy

`UxmlExporter` tree'yi depth-first yürür:
- **M3 component** (is M3ComponentBase) → `<components:M3[Name] attribute="value" />`
  - `[UxmlAttribute]` property'leri reflection ile okunur
  - Default instance ile karşılaştırılır, sadece non-default değerler serialize edilir
- **Layout container** (plain VisualElement) → `<ui:VisualElement style="flex-direction: row; ...">`
  - Inline style'dan flex-direction, padding, gap, justify-content serialize edilir
- Gerekli `<ui:Style>` referansları otomatik toplanır (component USS dosya yolları)
- Sadece primitive types serialize edilir: string, int, float, bool, enum

### 5. UXML Import Strategy (P2)

`UxmlImporter` XML parser kullanarak UXML dosyasını okur:
- `<components:M3[Name]>` tag'leri → ComponentRegistry'den Factory ile instantiate
- XML attribute'ları → reflection ile `[UxmlAttribute]` property'lere set
- `<ui:VisualElement>` tag'leri → layout container olarak oluştur, inline style parse et
- Child elementler recursive olarak process edilir

### 6. Property Inspector (P2)

Seçili element için reflection ile `[UxmlAttribute]` property'leri bulur:
- `string` → `EditorGUILayout.TextField`
- `bool` → `EditorGUILayout.Toggle`
- `enum` → `EditorGUILayout.EnumPopup`
- `int` / `float` → `EditorGUILayout.IntField` / `FloatField`

Layout container'lar için:
- `flex-direction` → `EditorGUILayout.EnumPopup`
- `justify-content` → `EditorGUILayout.EnumPopup`
- `align-items` → `EditorGUILayout.EnumPopup`
- `padding` → 4x float field (top/right/bottom/left)

### 7. Builder Scene

Editor-only scene (`Assets/UISystem/Editor/Scenes/PageBuilder.unity`):
- Main Camera
- UIDocument GameObject:
  - `UIDocument` component with PanelSettings
  - Root UXML source: minimal UXML with `page-root` VisualElement
- ThemeBootstrapper: `ThemeManager.Initialize()` çağırır, light/dark theme SO'ları referans eder

---

## Phased Delivery

### Phase 1 (P1) — Core
| Task | Deliverable |
|------|-------------|
| PageBuilderWindow + MenuItem | Window açılır, toolbar görünür |
| PageBuilder.unity scene | Scene yüklenir, UIDocument canvas görünür |
| ComponentRegistry | 27 M3 component metadata |
| ComponentPalette | Kategorili palette, tıkla-ekle |
| HierarchyTreeView | Tree görünümü, seçim, silme |
| UxmlExporter | Visual tree → valid UXML |
| **Gate** | Component ekle → export → yeni UIDocument'ta aynı sonuç |

### Phase 2 (P2) — Polish + Import
| Task | Deliverable |
|------|-------------|
| PropertyInspector | Variant/size/text/disabled düzenleme |
| Theme toggle | Light/dark preview |
| Hierarchy drag-and-drop | Element reorder |
| Add Container | Layout wrapper ekleme |
| UxmlImporter | Mevcut UXML'i yükleyip düzenleyebilme |
| **Gate** | Property değiştir → anında güncelleme; import → edit → re-export round-trip |

### Phase 3 (P3) — Presets
| Task | Deliverable |
|------|-------------|
| LayoutPresets | Button Row, Card Grid, Form Layout vb. |
| Palette "Layouts" kategorisi | Preset'leri tek tıkla ekleme |
| **Gate** | Preset ile tek tıkla multi-component layout |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| UxmlAttribute reflection karmaşıklığı | Export/import hatalı olabilir | Sadece primitive types; unit test ile doğrulama |
| Visual tree ↔ hierarchy sync kaybı | Stale UI | OnFocus + scene callback'lerinde rebuild |
| Undo desteği (VisualElement in-memory) | Ctrl+Z çalışmaz | P1: undo yok; P2: araştır |
| Scene kapatılırsa kayıp | İş kaybı | Export etmeden kapatmaya uyarı dialog |
| UXML import parse hataları | Corrupt state | Try-catch + validation; hata durumunda clear |

---

## Verification Protocol

1. `Unity_GetConsoleLogs` (logTypes: "error,warning") — compile error kontrolü
2. `Game Tools > Page Builder` → window açılır
3. "Load Builder Scene" → scene yüklenir, canvas görünür
4. Palette'ten 3-4 component ekle → Scene View'da görünür
5. Hierarchy'de doğru tree yapısı → seçim ve silme çalışır
6. "Export UXML" → kaydet → yeni UIDocument'ta yükle → aynı sonuç
7. Theme toggle → light/dark geçişi çalışır (P2)
8. UXML import → round-trip düzenleme çalışır (P2)

---

## Reference Files

| File | Purpose |
|------|---------|
| `Assets/UISystem/Editor/MenuItems/UISystemMenuItems.cs` | UXML generation pattern, context menu pattern |
| `Assets/UISystem/Editor/UISystemEditorWindow.cs` | EditorWindow pattern |
| `Assets/UISystem/Runtime/Core/M3ComponentBase.cs` | Base class, [UxmlAttribute] pattern |
| `Assets/UISystem/Runtime/Core/ThemeManager.cs` | Theme toggle API |
| `Assets/Scripts/Reusable/AnimationSystem/Editor/AnimationEditorTools.cs` | "Game Tools/" menu pattern |
