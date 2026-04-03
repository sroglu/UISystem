# Quickstart: UISystem Button — WP-4 + WP-5

**Branch**: `005-uisystem-button` | **Updated**: 2026-04-01

---

## Scenario 1: Basic Button in UXML (Smoke Test)

**Goal**: Verify M3Button renders in Play mode with correct M3 colors and shape.

### Setup

1. Open `Assets/UISystem/Scenes/ButtonDemo.unity`
2. Ensure the scene has: `[UISystem]` GameObject (ThemeManager + DefaultLight/Dark) + `UIDocument` GameObject (ThemeSwitchButton + UIDocument)
3. Enter Play mode

### Expected Result

- Four buttons visible: Filled (purple pill), Outlined (transparent + border), Text (label only), Tonal (secondary-container pill)
- All buttons show correct M3 baseline colors
- Zero console errors

---

## Scenario 2: State Layer Feedback (Interactive Test)

**Goal**: Verify hover/press/focus overlays appear and disappear correctly.

### Steps

1. Open ButtonDemo scene, enter Play mode
2. Move mouse over the **Filled** button
3. Click and hold the button
4. Release the mouse
5. Click then quickly move the mouse away while holding

### Expected Results

| Action | Expected |
|--------|----------|
| Hover | Subtle tint overlay (8% opacity) visible over the button |
| Press down | Overlay brightens (10% opacity) + ripple starts from click point |
| Release | Overlay returns to hover (8%) or clears if mouse left |
| Mouse leave while pressed | Overlay clears, ripple fades out |

---

## Scenario 3: Theme Switch (Integration Test)

**Goal**: Verify all button variants update colors when theme is toggled.

### Steps

1. Enter Play mode in ButtonDemo scene
2. Note the Filled button's purple background
3. Click the **Switch Theme** button
4. Observe all buttons

### Expected Results

- Filled button background changes from M3 light primary (#6750A4) to M3 dark primary (#D0BCFF)
- Tonal button background changes from light secondary-container to dark secondary-container
- Outlined button border color changes to dark outline value
- Zero per-button C# callbacks needed — change propagates automatically via USS

---

## Scenario 4: Disabled Button (Functional Test)

**Goal**: Verify disabled button rejects interaction and renders at 38% opacity.

### Steps

1. Enter Play mode
2. Observe the disabled button (one of the four should be pre-disabled in the demo)
3. Click it 10 times rapidly
4. Check console

### Expected Results

- Button renders at 38% opacity (visibly dimmed)
- No `OnClick` events fire (nothing logged to console)
- No hover/press overlay appears on hover/click attempts

---

## Scenario 5: C# Instantiation (Code Test)

**Goal**: Verify M3Button can be created from C# and wired to an OnClick handler.

```csharp
// In a MonoBehaviour.Start() or VisualElement OnEnable()
var btn = new M3Button("Confirm", ButtonVariant.Filled);
btn.OnClick += () => Debug.Log("[Test] Button clicked!");
rootVisualElement.Add(btn);
```

### Expected Results

- Button renders with correct filled style
- Clicking logs "[Test] Button clicked!" to console
- No errors

---

## Scenario 6: Editor Context Menu (WP-10 Test)

**Goal**: Verify context menu creates valid UXML files.

### Steps

1. In the Unity Project window, right-click any folder
2. Select `Assets > Create > UISystem > Button (Filled)`
3. Open the created `.uxml` file in a text editor

### Expected Results

- File created in the selected folder
- Content includes `<m3:M3Button variant="filled" text="Button" />`
- Opening the file in UI Builder shows a button element

---

## Integration Notes

- **ThemeManager must be in scene**: `[UISystem]` GameObject with ThemeManager component, both `DefaultLight.asset` and `DefaultDark.asset` assigned
- **UIDocument must call RegisterPanel**: `ThemeSwitchButton` component handles this automatically in `Start()`
- **Required stylesheets on UIDocument** (via PanelSettings or direct): `light.uss`, `typography.uss`, `state-layer.uss`, `button.uss`
- **PanelSettings**: Reuse `DefaultPanelSettings.asset` from 004-foundation
