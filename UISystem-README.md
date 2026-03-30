# UISystem

A modular, reusable UI component library for Unity, inspired by Material Design 3 (M3) principles.

This is not a 1:1 implementation of the M3 specification. It leverages Google's years of UX research — sizing, spacing, state feedback, color hierarchy, typography scale — to provide a Unity-native, performant, and flexible UI foundation.

## Features

**SDF UI Shader** — The visual foundation of all components. A single shader handles rounded corners, soft shadow, outline, state overlay, and ripple effect. One pass instead of multiple draw calls.

**Theme System** — ScriptableObject-based theming. Color palette, elevation presets, shape and motion definitions all in a single asset. Light/dark theme switching supported at runtime.

**Typography** — A TextMeshPro-based text style system inspired by M3's typography scale. Display, Headline, Title, Body, Label, and Caption roles.

**State Layers** — An interaction layer following M3's state feedback principles. Hover, pressed, focused, and disabled states are managed automatically.

**Component Builder Wizard** — Editor tools for rapidly creating components and layout structures, saving them as prefabs. Includes context menu shortcuts and layout preset templates.

## Components

| Component | Styles | Description |
|-----------|--------|-------------|
| Button | Filled, Outlined, Text, Tonal | Primary and secondary actions |
| Card | Elevated, Filled, Outlined | Content grouping |
| Toggle | — | Binary selection (switch) |
| TextField | Filled, Outlined | Text input, floating label |
| Dialog | — | Modal notification and confirmation |
| Snackbar | — | Temporary notification, queue system |
| BottomNav | — | Bottom navigation bar, 3–5 items |
| TabBar | Fixed, Scrollable | Top tab navigation |

## Requirements

- Unity 6+ (6000.x)
- TextMeshPro (Unity package)
- uGUI (Canvas-based UI)

No external dependencies.

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

Use `Assets > Create > UISystem > Theme Data` to create a new theme asset. Set the Primary, Secondary, and Surface colors along with elevation presets. You can start with the included `DefaultLight` and `DefaultDark` assets.

### 2. Set Up ThemeManager

Add an empty GameObject to your scene and attach the `ThemeManager` component. Assign your active ThemeData reference. ThemeManager distributes theme information to all UISystem components.

### 3. Add Components

**Method A — Context Menu:**

Right-click in the Hierarchy panel → `GameObject > UISystem > Button (Filled)` to add directly to the scene.

**Method B — Component Builder Wizard:**

Open the wizard via `Window > UISystem > Component Builder`. Select the component type, style, and configuration, then create.

**Method C — Manual:**

Add `SDFRectGraphic`, `StateLayerController`, and the relevant component script (e.g., `M3Button`) to a GameObject. Make sure ThemeManager is active.

### 4. Switch Themes

```csharp
// Switch between light/dark at runtime
ThemeManager.Instance.SetTheme(darkThemeData);
```

All components listen to the `OnThemeChanged` event and update automatically.

## Folder Structure

```
UISystem/
├── Runtime/
│   ├── Shaders/        SDF UI shader
│   ├── ScriptableObjects/  ThemeData, TypographyConfig
│   ├── Core/           ThemeManager, StateLayerController, TypographyResolver
│   ├── Components/     M3Button, M3Card, M3Toggle, ...
│   ├── Graphics/       SDFRectGraphic
│   ├── Enums/          ButtonStyle, CardStyle, ElevationLevel, ...
│   └── Data/           ElevationPreset, TextStyleData
├── Editor/
│   ├── Inspectors/     Custom inspectors
│   └── Wizard/         Component Builder, Layout Composer
├── Assets/
│   ├── Themes/         Default theme assets
│   ├── Typography/     Font assets and config
│   └── Prefabs/        Ready-made component prefabs
└── Samples~/           Sample scenes
```

## Architecture

```
┌─────────────────────────────────────────────┐
│           EDITOR TOOLING                     │
│   Component Builder Wizard,                  │
│   Layout Preset Generator,                   │
│   Custom Inspectors                          │
├─────────────────────────────────────────────┤
│              COMPONENTS                      │
│   Button, Card, Toggle, TextField,           │
│   Dialog, Snackbar, Navigation ...           │
├─────────────────────────────────────────────┤
│              CORE SYSTEMS                    │
│   ThemeManager, StateLayerController,        │
│   TypographyResolver, MotionPresets          │
├─────────────────────────────────────────────┤
│              FOUNDATION                      │
│   SDF UI Shader, Theme ScriptableObjects,    │
│   Design Token Definitions                   │
└─────────────────────────────────────────────┘
```

Dependencies flow top-down. Foundation depends on nothing. Editor Tooling runs only in the Editor and is excluded from runtime builds.

## Design Principles

UISystem is not a copy of M3, but builds on the following principles:

- **Hierarchy through elevation:** Important elements get higher elevation, background elements get lower. Depth is conveyed through shadow + tonal overlay.
- **State feedback:** Every interactive element provides visual feedback to the user. Hover, press, and focus states are expressed through specific opacity overlays.
- **Typography scale:** A limited number of text sizes defined in proportion to each other. Roles (Display, Body, Label…) are used instead of arbitrary font sizes.
- **Identity through shape:** Corner radius values are defined on a consistent scale. Buttons use pill shape, cards use medium radius, dialogs use large radius.
- **Context through motion:** Animations provide context, not decoration. Open, close, and transition motions use consistent easing curves.
- **Color roles:** Colors are assigned through roles (Primary, Surface, Error…), not direct hex values. Theme changes propagate automatically to all components.

## Scope and Roadmap

For detailed work packages, technical decisions, and development order, see [SCOPE.md](SCOPE.md).

## License

MIT
