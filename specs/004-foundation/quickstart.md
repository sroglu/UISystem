# Quickstart: UISystem Foundation Layer (UI Toolkit)

**Branch**: `004-uisystem-foundation` | **Updated**: 2026-04-01

---

## Prerequisites

- Unity 6.3+ (6000.3)
- Universal Render Pipeline (URP) package installed
- TextMeshPro package installed
- Odin Inspector installed
- UISystem submodule at `Assets/UISystem/` with `mehmetsrl.UISystem` asmdef available

---

## Step 1: PanelSettings Setup

Every UIDocument using UISystem needs a configured `PanelSettings` asset.

1. `Create > UI Toolkit > Panel Settings` — create a `DefaultPanelSettings.asset`
2. In the Inspector:
   - `Scale Mode`: **Scale With Screen Size**
   - `Reference Resolution`: **1080 × 1920**
   - `Screen Match Mode`: **Match Width Or Height**
   - `Match`: **0.5**
3. Add the provided `light.uss` and `dark.uss` stylesheets under **Theme Style Sheets**
4. Add `typography.uss` and `state-layer.uss` under **Theme Style Sheets**

> The pre-configured `DefaultPanelSettings.asset` in `Assets/UISystem/Assets/PanelSettings/` is
> ready to use — skip creation if using the default.

---

## Step 2: Create a UIDocument

1. Create a GameObject in the scene
2. Add `UIDocument` component
3. Set `Panel Settings` → assign `DefaultPanelSettings.asset`
4. Set `Source Asset` → assign your `.uxml` file (or leave empty for script-driven UI)

---

## Step 3: Create ThemeData Assets

1. `Create > UISystem > Theme Data` — create `DefaultLight.asset`
2. In the Inspector:
   - **Primary**: `#6750A4`
   - **Surface**: `#FFFBFE`
   - **Background**: `#FFFBFE`
   - Fill remaining 14 color roles (see data-model.md for M3 defaults)
3. Duplicate → rename `DefaultDark.asset` → change to dark palette values

The `DefaultLight.asset` and `DefaultDark.asset` in `Assets/UISystem/Assets/Themes/` are
pre-configured with M3 baseline colors — use them directly.

---

## Step 4: Add ThemeManager to Scene

1. Create empty GameObject → name it `[UISystem]`
2. Add `ThemeManager` component
3. Assign fields:
   - **Active Theme** → `DefaultLight.asset`
   - **Light Theme** → `DefaultLight.asset`
   - **Dark Theme** → `DefaultDark.asset`
   - **Typography Config** → `DefaultTypography.asset`
4. Assign any `UIDocument` components in the scene to **Managed Panels** list

`ThemeManager` calls `DontDestroyOnLoad` in `Awake` and syncs `ThemeData` values to USS
custom properties on all managed panel roots immediately.

---

## Step 5: Add an SDFRectElement

**Method A — via UXML:**
```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:m3="mehmetsrl.UISystem">
    <m3:SDFRectElement corner-radius="12" shadow-blur="8" shadow-offset-y="4"
                       style="background-color: var(--m3-surface); width: 300px; height: 160px;" />
</ui:UXML>
```

**Method B — via C#:**
```csharp
var card = new SDFRectElement
{
    CornerRadius = 12f,
    ShadowBlur = 8f,
    ShadowOffsetY = 4f
};
card.style.backgroundColor = new StyleColor(Color.white); // overridden by USS
rootVisualElement.Add(card);
```

**Configuration options:**

| Property | Default | Description |
|----------|---------|-------------|
| `CornerRadius` | 12 | Uniform corner radius in CSS px |
| `ShadowBlur` | 0 | Shadow blur spread |
| `ShadowOffsetY` | 0 | Shadow vertical offset |
| `ShadowColor` | rgba(0,0,0,0.2) | Shadow color |
| `OutlineThickness` | 0 | Border width (0 = no outline) |
| `OutlineColor` | black | Border color |

---

## Step 6: Add Typography

**Method A — USS class (recommended):**
```xml
<ui:Label class="m3-title" text="Card Title" />
<ui:Label class="m3-body" text="This is body text content." />
```

**Method B — TypographyResolver (Inspector-driven):**
1. Add `TypographyResolver` MonoBehaviour to a GameObject
2. Set **Role** → `Title`
3. Set **Document** → your `UIDocument`
4. Set **Element Name** → the name of the target `Label` in UXML
5. On Start, the resolver adds `.m3-title` class to the element

**Available classes**: `.m3-display`, `.m3-headline`, `.m3-title`, `.m3-body`, `.m3-label`, `.m3-caption`

---

## Step 7: Runtime Theme Switching

```csharp
// Switch to dark theme (all USS variables update on all managed panels)
ThemeManager.Instance.SetTheme(darkThemeData);

// Or use the built-in toggle
ThemeManager.Instance.ToggleLightDark();
```

All VisualElements using `var(--m3-*)` USS variables update automatically in the same frame.
No per-component callbacks needed.

---

## Step 8: Using Elevation Presets

To apply an elevation level to an `SDFRectElement`:

```csharp
// Read elevation values from active theme
var elev = ThemeManager.Instance.ActiveTheme.ElevationPresets[2];
var sdf = rootVisualElement.Q<SDFRectElement>("my-card");
sdf.ShadowBlur = elev.ShadowBlur;
sdf.ShadowOffsetY = elev.ShadowOffset.y;
sdf.ShadowColor = elev.ShadowColor;
sdf.MarkDirtyRepaint();
```

Or in USS using the pre-synced variables:
```css
.my-card {
    --shadow-blur: var(--m3-elevation-2-shadow-blur);
    --shadow-offset-y: var(--m3-elevation-2-shadow-offset-y);
}
```

---

## Sample Scene

Open `Assets/UISystem/Samples~/Foundation/FoundationDemo.unity` for a complete demo of:
- `SDFRectElement` instances with rounded corners and shadows
- All elements themed via `ThemeManager` (colors from `ThemeData`)
- Typography roles via USS classes (`.m3-title`, `.m3-body`, etc.)
- Runtime "Switch Theme" button calling `ThemeManager.ToggleLightDark()`

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Corners rendered as squares | Shader Graph material not assigned | Check `SDFRectElement._sharedMaterial` initialization |
| USS variables not resolving | ThemeManager not synced yet | Ensure `ThemeManager.Awake()` runs before first render |
| Wrong font size | `typography.uss` not in PanelSettings | Add `typography.uss` to PanelSettings Theme Style Sheets |
| NullReferenceException on ThemeManager | No ThemeManager in scene | Add ThemeManager to scene or open FoundationDemo |
| Theme switch doesn't update visuals | UIDocument not in managed panels list | Assign UIDocument to ThemeManager.ManagedPanels |
| Ripple not visible | RippleRadius = 0 or RippleAlpha = 0 | Drive from C# and call MarkDirtyRepaint() |
