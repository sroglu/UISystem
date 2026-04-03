# Public API Contract: UISystem Button — WP-4 + WP-5

**Branch**: `005-uisystem-button` | **Updated**: 2026-04-01

---

## M3Button (mehmetsrl.UISystem.Components)

### UXML Usage

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:m3="mehmetsrl.UISystem.Components">

    <!-- Filled button (default) -->
    <m3:M3Button text="Save" variant="Filled" />

    <!-- Outlined button -->
    <m3:M3Button text="Cancel" variant="Outlined" />

    <!-- Text button -->
    <m3:M3Button text="Learn more" variant="Text" />

    <!-- Tonal button -->
    <m3:M3Button text="Add to cart" variant="Tonal" />

    <!-- Disabled state -->
    <m3:M3Button text="Submit" variant="Filled" disabled="true" />
</ui:UXML>
```

### C# Usage

```csharp
// Construction
var btn = new M3Button("Save", ButtonVariant.Filled);
rootVisualElement.Add(btn);

// Event subscription
btn.OnClick += () => Debug.Log("Clicked!");

// Runtime property changes
btn.Text = "Saving...";
btn.Disabled = true;
btn.Variant = ButtonVariant.Tonal;
```

### Properties

| Property | Type | UXML Attribute | Description |
|----------|------|----------------|-------------|
| `Text` | `string` | `text` | Button label text. Empty string hides the label. |
| `Variant` | `ButtonVariant` | `variant` | Visual style. One of: `Filled`, `Outlined`, `Text`, `Tonal`. |
| `Disabled` | `bool` | `disabled` | When `true`: no interaction, 38% opacity. |

### Events

| Event | Type | Fired When |
|-------|------|------------|
| `OnClick` | `event Action` | User clicks or taps the button while it is enabled. |

### USS Names (for external styling)

| USS Name | Element | Purpose |
|----------|---------|---------|
| `m3-button__root` | SDFRectElement container | Shape, background, shadow |
| `m3-button__label` | Label | Text content |
| `m3-button__icon` | VisualElement | Leading icon slot (hidden by default) |
| `m3-button--filled` | Root | Filled variant class |
| `m3-button--outlined` | Root | Outlined variant class |
| `m3-button--text` | Root | Text variant class |
| `m3-button--tonal` | Root | Tonal variant class |

---

## StateLayerController (mehmetsrl.UISystem.Core)

### C# Usage

```csharp
// Attach to any VisualElement
var controller = new StateLayerController(myElement);
controller.Attach();

// With ripple
var ripple = new RippleElement();
myElement.Add(ripple);
var controller = new StateLayerController(myElement, ripple);
controller.Attach();

// Disable
controller.Disabled = true;

// Cleanup
controller.Detach();
```

### Constructor

```csharp
public StateLayerController(VisualElement target, RippleElement ripple = null)
```

| Parameter | Description |
|-----------|-------------|
| `target` | The VisualElement that receives pointer/focus events. If it is an `SDFRectElement`, overlay opacity is set directly. |
| `ripple` | Optional `RippleElement` child. `StartRipple()` is called on `PointerDownEvent`. Pass `null` to skip ripple. |

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Disabled` | `bool` | `false` | When `true`, ignores all events. Adds `.m3-disabled` USS class to target. |
| `OverlayColor` | `Color` | `Color.white` | Tint color of the state overlay (passed to `SDFRectElement.StateOverlayColor`). |

### Methods

| Method | Description |
|--------|-------------|
| `Attach()` | Registers 6 event callbacks on the target element. Safe to call multiple times (idempotent). |
| `Detach()` | Unregisters all 6 callbacks. Call before the target element is removed or the button is disposed. |

---

## ButtonVariant (mehmetsrl.UISystem.Enums)

```csharp
public enum ButtonVariant
{
    Filled   = 0,
    Outlined = 1,
    Text     = 2,
    Tonal    = 3
}
```

---

## USS Token Reference

All color values in `button.uss` use USS custom properties from `light.uss` / `dark.uss`:

| Token | Used In |
|-------|---------|
| `var(--m3-primary)` | Filled background, Outlined/Text label |
| `var(--m3-on-primary)` | Filled label |
| `var(--m3-secondary-container)` | Tonal background |
| `var(--m3-on-secondary-container)` | Tonal label |
| `var(--m3-outline)` | Outlined border |
| `var(--m3-elevation-2-shadow-blur)` | Filled shadow blur |
| `var(--m3-elevation-2-shadow-offset-y)` | Filled shadow offset |

---

## state-layer.uss Classes

| Class | Applied By | Effect |
|-------|------------|--------|
| `.m3-disabled` | `StateLayerController` when `Disabled = true` | `opacity: 0.38` on element and all children |
