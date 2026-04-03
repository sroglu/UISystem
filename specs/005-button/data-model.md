# Data Model: UISystem Button — WP-4 + WP-5 + WP-10

**Branch**: `005-uisystem-button` | **Updated**: 2026-04-01

---

## Entities

### M3Button

**Type**: `VisualElement` (partial class, `[UxmlElement]`)
**Namespace**: `mehmetsrl.UISystem.Components`
**File**: `Runtime/Components/M3Button.cs`

| Property | Type | UxmlAttribute | Default | Description |
|----------|------|---------------|---------|-------------|
| `Text` | `string` | `text` | `"Button"` | Button label text |
| `Variant` | `ButtonVariant` | `variant` | `Filled` | Visual style variant |
| `Disabled` | `bool` | `disabled` | `false` | Disables interaction and dims to 38% opacity |

**Events**:
| Event | Signature | Description |
|-------|-----------|-------------|
| `OnClick` | `public event Action OnClick` | Fires on click when not disabled |

**Internal composition** (constructed in constructor, not serialized):
| Child | Type | USS Name | Role |
|-------|------|----------|------|
| `_root` | `SDFRectElement` | `m3-button__root` | Rounded rect background + shadow |
| `_ripple` | `RippleElement` | — | Touch ripple animation (child of _root) |
| `_label` | `Label` | `m3-button__label` | Text content |
| `_iconSlot` | `VisualElement` | `m3-button__icon` | Leading icon placeholder (hidden) |
| `_stateLayer` | `StateLayerController` | — | Interaction feedback controller |

**State transitions**:
```
Idle  →  Hovered (StateOverlayOpacity 0.08)
Idle  →  Focused (StateOverlayOpacity 0.10)
Hovered → Pressed (StateOverlayOpacity 0.10 + RippleElement.StartRipple)
Pressed → Hovered (StateOverlayOpacity 0.08)
Pressed → Idle    (pointer left during press)
Any    → Disabled (element opacity 0.38, no events)
```

---

### ButtonVariant

**Type**: `enum`
**Namespace**: `mehmetsrl.UISystem.Enums`
**File**: `Runtime/Enums/ButtonVariant.cs`

| Value | Integer | USS class suffix | Background | Label color |
|-------|---------|-----------------|------------|-------------|
| `Filled` | `0` | `--filled` | `var(--m3-primary)` | `var(--m3-on-primary)` |
| `Outlined` | `1` | `--outlined` | transparent | `var(--m3-primary)` |
| `Text` | `2` | `--text` | transparent | `var(--m3-primary)` |
| `Tonal` | `3` | `--tonal` | `var(--m3-secondary-container)` | `var(--m3-on-secondary-container)` |

Explicit integer values prevent serialization breakage on enum reorder.

---

### StateLayerController

**Type**: Plain C# class (NOT MonoBehaviour)
**Namespace**: `mehmetsrl.UISystem.Core`
**File**: `Runtime/Core/StateLayerController.cs`

| Field | Type | Description |
|-------|------|-------------|
| `_target` | `VisualElement` | The element receiving state callbacks |
| `_sdfTarget` | `SDFRectElement?` | Nullable cast of `_target`; used to set `StateOverlayOpacity` |
| `_ripple` | `RippleElement?` | Optional ripple child; `StartRipple()` called on press |
| `Disabled` | `bool` | When true, events are ignored and `.m3-disabled` class applied |
| `OverlayColor` | `Color` | Tint of the state overlay (default: `Color.white`) |

**Public API**:
| Method | Description |
|--------|-------------|
| `StateLayerController(VisualElement target, RippleElement ripple = null)` | Constructor; stores references, does NOT attach yet |
| `Attach()` | Registers all 6 pointer/focus callbacks on `_target` |
| `Detach()` | Unregisters all 6 callbacks |

**State machine** (internal):
```
_isHovered: bool
_isPressed: bool
_isFocused: bool

PointerEnter → _isHovered=true  → UpdateOverlay()
PointerLeave → _isHovered=false, _isPressed=false → UpdateOverlay()
PointerDown  → _isPressed=true  → UpdateOverlay() + StartRipple()
PointerUp    → _isPressed=false → UpdateOverlay()
FocusIn      → _isFocused=true  → UpdateOverlay()
FocusOut     → _isFocused=false → UpdateOverlay()

UpdateOverlay():
  if Disabled: return
  opacity = _isPressed || _isFocused ? 0.10 : _isHovered ? 0.08 : 0.0
  _sdfTarget?.StateOverlayOpacity = opacity
```

---

## File Layout

```
Assets/UISystem/
├── Runtime/
│   ├── Components/
│   │   └── M3Button.cs                    # [UxmlElement] partial class
│   ├── Core/
│   │   └── StateLayerController.cs        # Plain C# state controller
│   └── Enums/
│       └── ButtonVariant.cs               # Filled=0, Outlined=1, Text=2, Tonal=3
├── Styles/
│   ├── state-layer.uss                    # .m3-disabled rule (placeholder → real)
│   └── Components/
│       └── button.uss                     # 4 variant + label USS classes
├── UXML/
│   └── M3Button.uxml                      # Reusable UXML template
├── Editor/
│   └── MenuItems/
│       └── UISystemMenuItems.cs           # Assets > Create > UISystem context menu
└── Samples~/
    └── Button/
        ├── ButtonDemo.uxml                # 4 variant layout
        └── ButtonDemo.unity              # Test scene (NOT in Samples~ → Scenes/)
```

**Note on scene path**: Per 004 findings, `Samples~/` is excluded from Unity AssetDatabase — MonoBehaviour GUIDs are unresolvable. **ButtonDemo.unity MUST be placed in `Assets/UISystem/Scenes/`** (same as FoundationDemo.unity), not in `Samples~/Button/`.
