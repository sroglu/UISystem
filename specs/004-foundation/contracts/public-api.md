# Public API Contract: UISystem Foundation Layer (UI Toolkit)

**Branch**: `004-uisystem-foundation` | **Updated**: 2026-04-01
**Type**: Unity C# runtime library — contracts are public class/interface signatures.

---

## ThemeManager

```csharp
namespace mehmetsrl.UISystem.Core
{
    public class ThemeManager : MonoBehaviour
    {
        // Singleton access — null if no ThemeManager in scene
        public static ThemeManager Instance { get; }

        // Currently active theme
        public ThemeData ActiveTheme { get; }

        // Active typography config
        public TypographyConfig Typography { get; }

        // Fired synchronously when SetTheme() changes the active theme.
        public event Action<ThemeData> OnThemeChanged;

        // Switch to a specific ThemeData.
        // Syncs all --m3-* USS variables on all managed panel roots. Fires OnThemeChanged.
        public void SetTheme(ThemeData newTheme);

        // Toggle between LightTheme and DarkTheme assets.
        public void ToggleLightDark();
    }
}
```

**Contract rules:**
- `SetTheme(null)` → no-op, logs error
- `ToggleLightDark()` with no dark theme assigned → no-op, logs warning
- `OnThemeChanged` is fired synchronously — all CSS variable consumers update in the same frame
- USS variables are set on the root `VisualElement` of every `UIDocument` in the managed list

---

## ThemeData

```csharp
namespace mehmetsrl.UISystem
{
    public class ThemeData : ScriptableObject
    {
        public ColorPalette    Colors;
        public ElevationPreset[] ElevationPresets; // length == 6, index 0–5
        public ShapePresets    Shapes;
        public MotionPreset[]  MotionPresets;      // length == 4

        // Get elevation preset safely (clamps level to [0,5])
        public ElevationPreset GetElevation(int level);

        // Get color by role
        public Color GetColor(ColorRole role);

        // Get motion preset by type
        public MotionPreset GetMotion(MotionPresetType type);
    }
}
```

---

## SDFRectElement

```csharp
namespace mehmetsrl.UISystem
{
    [UxmlElement]
    public partial class SDFRectElement : VisualElement
    {
        // Shape — per-corner radii (-1 = inherit from CornerRadius)
        [UxmlAttribute] public float CornerRadius   { get; set; } // uniform, default 12
        [UxmlAttribute] public float CornerRadiusTL { get; set; } // -1 = use CornerRadius
        [UxmlAttribute] public float CornerRadiusTR { get; set; }
        [UxmlAttribute] public float CornerRadiusBR { get; set; }
        [UxmlAttribute] public float CornerRadiusBL { get; set; }

        // Shadow
        [UxmlAttribute] public float ShadowBlur    { get; set; }
        [UxmlAttribute] public float ShadowOffsetX { get; set; }
        [UxmlAttribute] public float ShadowOffsetY { get; set; }
        [UxmlAttribute] public Color ShadowColor   { get; set; }

        // Outline
        [UxmlAttribute] public float OutlineThickness { get; set; }
        [UxmlAttribute] public Color OutlineColor     { get; set; }

        // State overlay (driven by WP-4 StateLayerController)
        public float StateOverlayOpacity { get; set; }
        public Color StateOverlayColor   { get; set; }

        // Ripple (driven from C# only — not UXML attributes)
        public Vector2 RippleCenter { get; set; }
        public float   RippleRadius { get; set; }
        public float   RippleAlpha  { get; set; }
    }
}
```

**Contract rules:**
- Setting any property automatically calls `MarkDirtyRepaint()`.
- `CornerRadius` values are clamped in the Shader Graph — no visual artifact if value exceeds half-size.
- `ShadowBlur = 0` → no shadow rendered (shader keyword disabled).
- `OutlineThickness = 0` → no outline rendered (shader keyword disabled).
- `RippleRadius` and `RippleAlpha` must be animated from C# code — no built-in animation.

---

## TypographyConfig

```csharp
namespace mehmetsrl.UISystem
{
    public class TypographyConfig : ScriptableObject
    {
        // Get the TextStyle for a given role
        public TextStyle GetStyle(TextRole role);
    }
}
```

---

## TypographyResolver (optional MonoBehaviour helper)

```csharp
namespace mehmetsrl.UISystem.Core
{
    public class TypographyResolver : MonoBehaviour
    {
        public TextRole Role { get; set; }

        // Adds the correct .m3-* USS class to the target VisualElement.
        // Called automatically on Start.
        public void ApplyRole(TextRole role);
    }
}
```

**Contract rules:**
- `ApplyRole()` is a no-op if no target VisualElement is found.
- Does not throw — logs warning on missing element or config.
- `Role` setter does NOT call `ApplyRole()` automatically at runtime (unlike the old
  uGUI `TypographyResolver` which called `ApplyStyle()` — call `ApplyRole()` explicitly
  if changing at runtime).

---

## USS Classes (stable public contract)

These class names MUST NOT be renamed or removed without a major version bump:

| Class | Font Size | Weight | Usage |
|-------|-----------|--------|-------|
| `.m3-display` | 36px | Regular | Splash, large numbers |
| `.m3-headline` | 28px | Regular | Section headings |
| `.m3-title` | 22px | Medium | Card/dialog titles |
| `.m3-body` | 16px | Regular | General text |
| `.m3-label` | 14px | Medium | Button labels |
| `.m3-caption` | 12px | Regular | Helper text, timestamps |

---

## USS Variables (stable public contract)

These variables are guaranteed to be set by `ThemeManager.SetTheme()`:

```
Color roles (17):
  --m3-primary, --m3-on-primary, --m3-primary-container, --m3-on-primary-container
  --m3-secondary, --m3-on-secondary, --m3-secondary-container, --m3-on-secondary-container
  --m3-surface, --m3-on-surface, --m3-surface-variant, --m3-on-surface-variant
  --m3-error, --m3-on-error, --m3-outline, --m3-outline-variant, --m3-background

Elevation (6 levels × 4 values each):
  --m3-elevation-{0-5}-shadow-blur
  --m3-elevation-{0-5}-shadow-offset-x
  --m3-elevation-{0-5}-shadow-offset-y
  --m3-elevation-{0-5}-tonal-alpha

Shape presets (7):
  --m3-shape-none, --m3-shape-extra-small, --m3-shape-small, --m3-shape-medium
  --m3-shape-large, --m3-shape-extra-large, --m3-shape-full

Motion durations (4):
  --m3-motion-emphasized-duration, --m3-motion-standard-duration
  --m3-motion-emphasized-decelerate-duration, --m3-motion-standard-decelerate-duration
```

---

## Assembly References

Projects using UISystem Foundation:
- Runtime: reference `mehmetsrl.UISystem`
- Editor tools: reference `mehmetsrl.UISystem.Editor`

UISystem MUST NOT reference any `mehmetsrl.*` Infrastructural assemblies.
