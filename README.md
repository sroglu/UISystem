# UISystem

A modular, reusable UI component library for Unity, inspired by Material Design 3 (M3) principles.

This is not a 1:1 implementation of the M3 specification. It leverages Google's years of UX research — sizing, spacing, state feedback, color hierarchy, typography scale — to provide a Unity-native, performant, and flexible UI foundation.

Built on **UI Toolkit** (UXML + USS + C#) with **URP Shader Graph** for custom visual effects.

## Features

**SDFRectElement** — The visual foundation of all components. A custom `VisualElement` renders rounded rectangles, soft shadows, and outlines using SDF math via `Painter2D` (CPU vector drawing). `[UxmlElement]` with `[UxmlAttribute]` properties for corner-radius, shadow-blur, shadow-offset, shadow-color, and outline-thickness. Shares the element tree with `RippleElement` for M3 ripple touch feedback.

**Theme System** — USS custom properties powered by ScriptableObject data. `ThemeData` SO holds the color palette, elevation presets, shape tokens, and motion presets. `ThemeManager` applies `light.uss` or `dark.uss` stylesheets to managed panels at runtime — all `var(--m3-*)` variables resolve automatically. Light/dark toggle via `ThemeManager.Instance.ToggleLightDark()`.

**Typography** — TextMeshPro-based type scale inspired by M3. Display, Headline, Title, Body, Label, and Caption roles defined as USS classes.

**StateLayerController** — Plain C# interaction feedback controller (not MonoBehaviour). Manages hover (0.08), pressed (0.10), focused (0.10), and disabled (0.38) state overlays by setting `SDFRectElement.StateOverlayOpacity` directly — clipped to the rounded rect boundary. Integrates with `RippleElement` for M3 press ripple. Attach/Detach lifecycle for safe callback management.

**Flexbox Layouts** — UI Toolkit's native Flexbox engine handles all layout. No manual layout groups needed — just USS flex properties.

**Component Builder Wizard** — Editor tools for rapidly creating components and layouts, with context menu shortcuts.

## Components

| Component | Styles | Status | Description |
|-----------|--------|--------|-------------|
| Button | Filled, Outlined, Text, Tonal | ✅ Implemented | Primary and secondary actions |
| Card | Elevated, Filled, Outlined | — | Content grouping |
| Toggle | — | — | Binary selection (switch) |
| TextField | Filled, Outlined | — | Text input, floating label |
| Dialog | — | — | Modal notification and confirmation |
| Snackbar | — | — | Temporary notification, queue system |
| BottomNav | — | — | Bottom navigation bar, 3–5 items |
| TabBar | Fixed, Scrollable | — | Top tab navigation |

## Requirements

- Unity 6.3+ (6000.3)
- Universal Render Pipeline (URP)
- TextMeshPro (Unity package)

## Installation

### As a Git Submodule (recommended)

```bash
git submodule add https://github.com/sroglu/UISystem.git Assets/Submodules/UISystem
```

### Via Unity Package Manager (UPM)

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.sroglu.uisystem": "https://github.com/sroglu/UISystem.git"
  }
}
```

## Quick Start

### 1. Create a Theme

Use `Assets > Create > UISystem > Theme Data` to create a new theme asset. Set the Primary, Secondary, and Surface colors along with elevation presets. Included `DefaultLight` and `DefaultDark` assets are ready to use.

### 2. Set Up ThemeManager

Add a `ThemeManager` component to a GameObject in your scene. Assign your active ThemeData reference. ThemeManager syncs ScriptableObject values to USS custom properties, making them available to all UISystem components.

### 3. Add Components

**Method A — Context Menu:**

`Assets > Create > UISystem > Button (Filled)` to generate a configured UXML template.

**Method B — Component Builder Wizard:**

Open via `Window > UISystem > Component Builder`. Select type, style, and configuration, then create.

**Method C — UXML / C#:**

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements"
         xmlns:components="mehmetsrl.UISystem.Components">
    <components:M3Button variant="Filled" text="Click Me" />
</ui:UXML>
```

```csharp
using mehmetsrl.UISystem.Components;
using mehmetsrl.UISystem.Enums;

var button = new M3Button { Text = "Click Me", Variant = ButtonVariant.Filled };
button.OnClick += () => Debug.Log("clicked");
rootVisualElement.Add(button);
```

### 4. Switch Themes

```csharp
ThemeManager.Instance.SetTheme(darkThemeData);
```

All USS custom properties update automatically. Components restyle instantly.

## Architecture

```
┌─────────────────────────────────────────────┐
│           EDITOR TOOLING                     │
│   Component Builder Wizard,                  │
│   Layout Preset Generator                    │
├─────────────────────────────────────────────┤
│              COMPONENTS                      │
│   Button, Card, Toggle, TextField,           │
│   Dialog, Snackbar, Navigation               │
│   (UXML templates + C# custom elements)      │
├─────────────────────────────────────────────┤
│              CORE SYSTEMS                    │
│   ThemeManager, StateLayerController,        │
│   TypographyResolver, MotionPresets          │
├─────────────────────────────────────────────┤
│              FOUNDATION                      │
│   UI Shader Graph, USS Theme Variables,      │
│   Design Token Definitions                   │
└─────────────────────────────────────────────┘
```

## Design Principles

UISystem is not a copy of M3, but builds on the following principles:

- **Hierarchy through elevation:** Important elements get higher elevation. Depth conveyed through shadow + tonal overlay.
- **State feedback:** Every interactive element provides visual feedback. States expressed through specific opacity overlays.
- **Typography scale:** Roles (Display, Body, Label…) instead of arbitrary font sizes. Limited set of proportional sizes.
- **Identity through shape:** Corner radius on a consistent scale. Buttons use pill shape, cards use medium radius, dialogs use large radius.
- **Context through motion:** Animations provide context, not decoration. Consistent easing curves throughout.
- **Color roles:** Colors assigned through roles (Primary, Surface, Error…), not direct hex values. Theme changes propagate automatically.

## M3 Reference

The primary design reference is [m3.material.io](https://m3.material.io). When implementing or extending components, consult the M3 site for exact specifications — dp values, color role mappings, state opacities, animation durations, and component anatomy.

UISystem does not copy M3 pixel-for-pixel. The M3 site serves as a research-backed starting point: if you don't know what value to use, what size something should be, or how a state transition should feel, pull the answer from there.

## Scope and Roadmap

For detailed work packages, technical decisions, and development order, see [SCOPE.md](SCOPE.md).

## License

MIT
