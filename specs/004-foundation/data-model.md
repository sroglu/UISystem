# Data Model: UISystem Foundation Layer (UI Toolkit)

**Branch**: `004-uisystem-foundation` | **Updated**: 2026-04-01

---

## Entity Overview

```
ThemeData (ScriptableObject)
  ├── ColorPalette (struct, 17 Color fields)
  ├── ElevationPreset[6] (struct array)
  ├── ShapePresets (struct, 7 named float fields)
  └── MotionPreset[4] (struct array)

TypographyConfig (ScriptableObject)
  └── TextStyle[6] indexed by TextRole (enum)

SDFRectElement (VisualElement — custom element)
  └── Visual params exposed as UxmlAttribute + USS custom properties → Shader Graph material

ThemeManager (MonoBehaviour, singleton)
  ├── ref ThemeData activeTheme
  ├── List<UIDocument> managedPanels
  └── event Action<ThemeData> OnThemeChanged

TypographyResolver (MonoBehaviour — optional helper)
  ├── TextRole role
  └── ref VisualElement target
```

---

## ThemeData

```csharp
namespace mehmetsrl.UISystem
{
    [CreateAssetMenu(menuName = "UISystem/Theme Data")]
    public class ThemeData : ScriptableObject
    {
        [BoxGroup("Color Palette")]
        public ColorPalette Colors;

        [BoxGroup("Elevation")]
        public ElevationPreset[] ElevationPresets; // length 6, index 0–5

        [BoxGroup("Shape")]
        public ShapePresets Shapes;

        [BoxGroup("Motion")]
        public MotionPreset[] MotionPresets; // length 4, indexed by MotionPresetType
    }
}
```

### ColorPalette (struct)

| Field | Type | M3 Role | Default Light |
|-------|------|---------|---------------|
| Primary | Color | Primary actions, key components | #6750A4 |
| OnPrimary | Color | Text/icons on Primary | #FFFFFF |
| PrimaryContainer | Color | Lower-emphasis Primary container | #EADDFF |
| OnPrimaryContainer | Color | Text on PrimaryContainer | #21005D |
| Secondary | Color | Secondary actions | #625B71 |
| OnSecondary | Color | Text/icons on Secondary | #FFFFFF |
| SecondaryContainer | Color | Secondary container | #E8DEF8 |
| OnSecondaryContainer | Color | Text on SecondaryContainer | #1D192B |
| Surface | Color | Default background | #FFFBFE |
| OnSurface | Color | Text/icons on Surface | #1C1B1F |
| SurfaceVariant | Color | Alternative surface | #E7E0EC |
| OnSurfaceVariant | Color | Text on SurfaceVariant | #49454F |
| Error | Color | Error states | #B3261E |
| OnError | Color | Text on Error | #FFFFFF |
| Outline | Color | Borders, dividers | #79747E |
| OutlineVariant | Color | Subtle borders | #CAC4D0 |
| Background | Color | Overall background | #FFFBFE |

### ElevationPreset (struct)

```csharp
[Serializable]
public struct ElevationPreset
{
    public Vector2 ShadowOffset;       // CSS px in reference resolution
    public float   ShadowBlur;         // CSS px, controls smoothstep spread
    public Color   ShadowColor;        // typically black with low alpha
    public float   TonalOverlayAlpha;  // 0–1, tints surface with Primary color
}
```

**Elevation levels (M3-inspired):**

| Level | ShadowBlur | ShadowOffset | ShadowColor | TonalOverlay |
|-------|-----------|--------------|-------------|--------------|
| 0 | 0 | (0,0) | transparent | 0% |
| 1 | 4px | (0,1px) | rgba(0,0,0,0.20) | 5% |
| 2 | 8px | (0,2px) | rgba(0,0,0,0.20) | 8% |
| 3 | 12px | (0,4px) | rgba(0,0,0,0.20) | 11% |
| 4 | 16px | (0,6px) | rgba(0,0,0,0.20) | 12% |
| 5 | 24px | (0,8px) | rgba(0,0,0,0.20) | 14% |

### ShapePresets (struct)

```csharp
[Serializable]
public struct ShapePresets
{
    public float None;         // 0 px
    public float ExtraSmall;   // 4 px  (M3: 4dp)
    public float Small;        // 8 px  (M3: 8dp)
    public float Medium;       // 12 px (M3: 12dp)
    public float Large;        // 16 px (M3: 16dp)
    public float ExtraLarge;   // 28 px (M3: 28dp)
    public float Full;         // 9999  (pill shape)
}
```

*Note: In UI Toolkit with PanelSettings Scale With Screen Size (1080×1920 reference),
1px in USS ≈ 1dp at reference resolution. These values are stored as reference px.*

### MotionPreset (struct)

```csharp
[Serializable]
public struct MotionPreset
{
    public AnimationCurve Curve;
    public float          DurationMs; // milliseconds
}
```

**Named presets (indexed by MotionPresetType enum):**
- `Emphasized` (0): EaseInOut, 300ms
- `Standard` (1): EaseOut, 200ms
- `EmphasizedDecelerate` (2): Steep EaseOut, 400ms
- `StandardDecelerate` (3): Linear→EaseOut, 250ms

---

## TypographyConfig

```csharp
namespace mehmetsrl.UISystem
{
    [CreateAssetMenu(menuName = "UISystem/Typography Config")]
    public class TypographyConfig : ScriptableObject
    {
        [BoxGroup("Display")]  public TextStyle Display;
        [BoxGroup("Headline")] public TextStyle Headline;
        [BoxGroup("Title")]    public TextStyle Title;
        [BoxGroup("Body")]     public TextStyle Body;
        [BoxGroup("Label")]    public TextStyle Label;
        [BoxGroup("Caption")]  public TextStyle Caption;

        public TextStyle GetStyle(TextRole role) => role switch {
            TextRole.Display  => Display,
            TextRole.Headline => Headline,
            TextRole.Title    => Title,
            TextRole.Body     => Body,
            TextRole.Label    => Label,
            TextRole.Caption  => Caption,
            _                 => Body
        };
    }
}
```

### TextStyle (struct)

```csharp
[Serializable]
public struct TextStyle
{
    public TMP_FontAsset FontAsset;   // Roboto-Regular or Roboto-Medium SDF asset
    public float         FontSize;    // in CSS px (reference resolution)
    public FontStyles    FontStyle;   // TMP enum: Normal, Bold...
    public float         LineSpacing; // TMP line spacing units
    public float         CharSpacing; // TMP character spacing units
    public string        UssClassName; // e.g., "m3-title" — used by TypographyResolver
}
```

### TextRole (enum)

```csharp
namespace mehmetsrl.UISystem.Enums
{
    public enum TextRole
    {
        Display  = 0,
        Headline = 1,
        Title    = 2,
        Body     = 3,
        Label    = 4,
        Caption  = 5
    }
}
```

**Typography scale (M3-inspired, reference px = USS px at 1080-wide reference):**

| Role | FontSize (USS px) | Weight | USS Class |
|------|------------------|--------|-----------|
| Display | 36 | Regular | `.m3-display` |
| Headline | 28 | Regular | `.m3-headline` |
| Title | 22 | Medium | `.m3-title` |
| Body | 16 | Regular | `.m3-body` |
| Label | 14 | Medium | `.m3-label` |
| Caption | 12 | Regular | `.m3-caption` |

---

## SDFRectElement

```csharp
namespace mehmetsrl.UISystem
{
    [UxmlElement]
    public partial class SDFRectElement : VisualElement
    {
        // --- Visual Parameters (exposed as UXML attributes + USS custom properties) ---
        [UxmlAttribute] public float CornerRadius { get; set; } = 12f;
        [UxmlAttribute] public float CornerRadiusTL { get; set; } = -1f; // -1 = use CornerRadius
        [UxmlAttribute] public float CornerRadiusTR { get; set; } = -1f;
        [UxmlAttribute] public float CornerRadiusBR { get; set; } = -1f;
        [UxmlAttribute] public float CornerRadiusBL { get; set; } = -1f;

        [UxmlAttribute] public float ShadowBlur     { get; set; } = 0f;
        [UxmlAttribute] public float ShadowOffsetX  { get; set; } = 0f;
        [UxmlAttribute] public float ShadowOffsetY  { get; set; } = 0f;
        [UxmlAttribute] public Color ShadowColor    { get; set; } = new Color(0, 0, 0, 0.2f);

        [UxmlAttribute] public float OutlineThickness { get; set; } = 0f;
        [UxmlAttribute] public Color OutlineColor     { get; set; } = Color.black;

        [UxmlAttribute] public float StateOverlayOpacity { get; set; } = 0f;
        [UxmlAttribute] public Color StateOverlayColor   { get; set; } = Color.white;

        // Ripple — driven from C# (not UXML)
        public Vector2 RippleCenter { get; set; }
        public float   RippleRadius { get; set; } = 0f;
        public float   RippleAlpha  { get; set; } = 0f;

        // Material reference (shared Shader Graph instance)
        private static Material _sharedMaterial;
    }
}
```

**USS custom property mapping:**

| USS Property | Shader Graph Property | Description |
|---|---|---|
| `--corner-radius` | `_CornerRadius` | Uniform corner radius |
| `--shadow-blur` | `_ShadowBlur` | Shadow blur spread |
| `--shadow-offset` | `_ShadowOffset` | Shadow X/Y offset |
| `--shadow-color` | `_ShadowColor` | Shadow RGBA |
| `--outline-thickness` | `_OutlineThickness` | Border width |
| `--outline-color` | `_OutlineColor` | Border color |
| `--state-overlay-opacity` | `_StateOverlayOpacity` | State layer opacity |
| `--state-overlay-color` | `_StateOverlayColor` | State layer color |

---

## ThemeManager

```csharp
namespace mehmetsrl.UISystem.Core
{
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        [SerializeField] private ThemeData _activeTheme;
        [SerializeField] private ThemeData _lightTheme;
        [SerializeField] private ThemeData _darkTheme;
        [SerializeField] private TypographyConfig _typographyConfig;

        public ThemeData ActiveTheme => _activeTheme;
        public TypographyConfig Typography => _typographyConfig;
        public event Action<ThemeData> OnThemeChanged;

        // Syncs all ThemeData SO values to USS custom properties on all managed UIDocument roots
        public void SetTheme(ThemeData theme);
        public void ToggleLightDark();

        private void SyncToPanel(UIDocument doc);
        private void SyncAllPanels();
    }
}
```

**USS variable naming convention** (set on panel root VisualElement):

| ThemeData Field | USS Variable Name |
|---|---|
| Colors.Primary | `--m3-primary` |
| Colors.OnPrimary | `--m3-on-primary` |
| Colors.PrimaryContainer | `--m3-primary-container` |
| Colors.OnPrimaryContainer | `--m3-on-primary-container` |
| Colors.Secondary | `--m3-secondary` |
| Colors.Surface | `--m3-surface` |
| Colors.OnSurface | `--m3-on-surface` |
| Colors.SurfaceVariant | `--m3-surface-variant` |
| Colors.OnSurfaceVariant | `--m3-on-surface-variant` |
| Colors.Outline | `--m3-outline` |
| Colors.OutlineVariant | `--m3-outline-variant` |
| ElevationPresets[N].ShadowBlur | `--m3-elevation-N-shadow-blur` |
| ElevationPresets[N].ShadowOffset | `--m3-elevation-N-shadow-offset-x`, `--m3-elevation-N-shadow-offset-y` |
| Shapes.Medium | `--m3-shape-medium` |
| Shapes.Full | `--m3-shape-full` |
| MotionPresets[0].DurationMs | `--m3-motion-emphasized-duration` |

---

## TypographyResolver (optional C# helper)

```csharp
namespace mehmetsrl.UISystem.Core
{
    public class TypographyResolver : MonoBehaviour
    {
        [SerializeField] private TextRole _role = TextRole.Body;
        [SerializeField] private UIDocument _document; // which UIDocument to target
        [SerializeField] private string _elementName;  // element name in UXML hierarchy

        // Adds the correct .m3-* USS class to the target VisualElement on Start
        // Subscribes to ThemeManager.OnThemeChanged to re-apply if needed
        public void ApplyRole(TextRole role);
    }
}
```

---

## Enums

```csharp
public enum MotionPresetType
{
    Emphasized           = 0,
    Standard             = 1,
    EmphasizedDecelerate = 2,
    StandardDecelerate   = 3
}

public enum ColorRole
{
    Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer,
    Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer,
    Surface, OnSurface, SurfaceVariant, OnSurfaceVariant,
    Error, OnError, Outline, OutlineVariant, Background
}
```

---

## Relationships

```
ThemeManager
  └─ holds ─► ThemeData (active)
       ├─ ColorPalette ──── synced to ──► USS variables (--m3-primary etc.) ──► all VisualElements
       ├─ ElevationPreset ─ synced to ──► USS variables (--m3-elevation-N-*)
       └─ MotionPreset ──── synced to ──► USS variables (--m3-motion-*-duration)

SDFRectElement
  └─ reads ──► Shader Graph material (SDFRect.shadergraph)
       └─ parameters from ──► CSS custom properties OR direct C# property setters

TypographyConfig
  └─ held by ─► ThemeManager (optional ref) OR TypographyResolver
       └─ TextStyle per role ─► UssClassName ─► applied as CSS class ─► Label/TextElement
```

---

## USS File Structure

```
Styles/
├── Themes/
│   ├── light.uss      -- :root { --m3-primary: #6750A4; ... } (all variables)
│   └── dark.uss       -- :root { --m3-primary: #D0BCFF; ... } (dark overrides)
├── typography.uss     -- .m3-display { font-size: 36px; -unity-font-definition: ...; }
├── state-layer.uss    -- .m3-state-layer { position: absolute; ... }
└── Components/        -- per-component styles (WP-5+)
```
