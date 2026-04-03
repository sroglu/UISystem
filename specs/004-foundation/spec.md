# Feature Specification: UISystem Foundation Layer (UI Toolkit)

**Feature Branch**: `004-uisystem-foundation`
**Created**: 2026-03-31
**Updated**: 2026-04-01 — Migrated from uGUI to UI Toolkit (UXML + USS + URP Shader Graph)
**Status**: Draft

## User Scenarios & Testing *(mandatory)*

### User Story 1 - UI Shader Graph Element (Priority: P1)

A developer adds an `SDFRectElement` custom VisualElement to a UIDocument to render a
rounded rectangle with per-corner radius, optional soft shadow, and optional outline.
The element reads its visual parameters from USS custom properties and drives a URP
Shader Graph material. Batching behavior is acceptable for mobile (UI Toolkit manages
internally).

**Why this priority**: `SDFRectElement` is the visual backbone of the entire UISystem.
No component (Button, Card, Toggle, etc.) can be built without it. Proving that the
Shader Graph works correctly in UI Toolkit is the foundational gate for all subsequent work.

**Independent Test**: Create a UIDocument with a `PanelSettings` (Scale With Screen Size,
1080×1920). Add several `SDFRectElement` instances with different corner radii, shadow,
and outline settings via USS. Open the Game View — all elements MUST render with correct
rounded shapes and soft shadows. Console MUST show zero shader errors.

**Acceptance Scenarios**:

1. **Given** a `VisualElement` using `SDFRectElement` with `--corner-radius: 16px` set in
   USS, **When** viewed in Play mode, **Then** the element renders as a rounded rectangle
   with 16px corner radius on all corners.
2. **Given** an `SDFRectElement` with shadow USS parameters (`--shadow-offset-y: 4px`,
   `--shadow-blur: 8px`, `--shadow-color: rgba(0,0,0,0.3)`), **When** viewed in the
   Game View, **Then** a soft drop shadow appears beneath the element.
3. **Given** an `SDFRectElement` with `--outline-thickness: 1.5px` and `--outline-color`,
   **When** viewed, **Then** a visible border appears at the element's edge.
4. **Given** a `--state-overlay-color` and `--state-overlay-opacity: 0.08` set in USS,
   **When** the element is rendered, **Then** a semi-transparent overlay is composited
   over the base color.
5. **Given** an `SDFRectElement` with ripple parameters set from C# (`RippleCenter`,
   `RippleRadius`, `RippleAlpha`), **When** the ripple animation runs (radius 0→1 over
   300ms), **Then** an expanding circle ripple is rendered from the specified origin.

---

### User Story 2 - Theme System with USS Sync (Priority: P2)

A developer creates a `ThemeData` ScriptableObject with a full M3-inspired color palette,
elevation presets, shape presets, and motion presets. A `ThemeManager` component syncs
these values to USS custom properties on the root panel at startup and on theme change.
All UISystem elements that consume `--m3-*` USS variables update visually without any
C# callback on the component side.

**Why this priority**: Theming is the system that gives UISystem components their visual
identity. USS variable sync is what makes M3 color roles flow through to every component
automatically.

**Independent Test**: Create a scene with `ThemeManager` holding `DefaultLight` and
`DefaultDark` ThemeData assets. Add two `SDFRectElement` elements that read
`--m3-primary` and `--m3-surface` from USS. Trigger `SetTheme(darkTheme)` from a
button. Both elements MUST update to dark colors within the same frame, with no C#
code on the elements themselves.

**Acceptance Scenarios**:

1. **Given** a `ThemeData` SO with `Primary = #6750A4`, **When** `ThemeManager` syncs
   it at startup, **Then** `--m3-primary` is set on the panel's root element and any
   USS rule using `var(--m3-primary)` resolves to `#6750A4`.
2. **Given** a `ThemeManager` with `DefaultLight` active, **When** `SetTheme(DefaultDark)`
   is called, **Then** all USS custom properties update to dark theme values and elements
   re-render within the same frame.
3. **Given** a `ThemeData` with `ElevationPreset[2]` configured, **When** `ThemeManager`
   syncs, **Then** `--m3-elevation-2-shadow-blur` and related USS variables are set
   correctly.
4. **Given** a `ThemeManager` configured with `DontDestroyOnLoad`, **When** the scene
   transitions, **Then** `ThemeManager.Instance` remains accessible and the active theme
   is preserved.
5. **Given** a custom editor inspector for `ThemeData`, **When** opened in the Inspector,
   **Then** all 17 color roles are displayed as visual color swatches.

---

### User Story 3 - Typography USS Classes (Priority: P3)

A developer applies a USS class (`.m3-title`, `.m3-body`, etc.) to any `Label` or
`TextElement` in a UXML template. The element automatically adopts the correct font,
size, and weight defined in the typography stylesheet. An optional `TypographyResolver`
C# component allows assigning the role from the Inspector without touching USS.

**Why this priority**: Typography is independently deliverable. A working type scale
provides value even without any component. It is lower priority than the shader and
theme system because it is purely styling.

**Independent Test**: Create a UIDocument with 6 `Label` elements, each with a different
`.m3-*` class (`.m3-display`, `.m3-headline`, `.m3-title`, `.m3-body`, `.m3-label`,
`.m3-caption`). Enter Play mode — each label MUST display at the correct font size using
the correct weight font asset (Regular or Medium/Bold).

**Acceptance Scenarios**:

1. **Given** a `Label` with class `.m3-label` and `typography.uss` loaded in
   `PanelSettings`, **When** the panel is rendered, **Then** the label displays at 14px
   using the Medium weight font asset.
2. **Given** a `Label` with class `.m3-title`, **When** rendered, **Then** font size is
   22px and Medium weight font asset is used — no SDF dilation fake bold.
3. **Given** `typography.uss` linking Regular and Medium TMP font assets via
   `-unity-font-definition`, **When** `.m3-body` (Regular) and `.m3-label` (Medium) are
   applied, **Then** correct font assets are active on each element.
4. **Given** a `TypographyResolver` component with `TextRole.Caption` assigned in the
   Inspector, **When** the scene starts, **Then** it adds the `.m3-caption` USS class
   to the target element programmatically.

---

### User Story 4 - Foundation Sample Scene (Priority: P4)

A developer opens `Samples~/Foundation/FoundationDemo`, presses Play, and sees all
three foundation systems (Shader Graph element, USS theme sync, typography classes)
working together. The scene uses UIDocument + PanelSettings (no Canvas). A theme-switch
button toggles light/dark in real time by calling `ThemeManager.SetTheme()`.

**Why this priority**: The sample scene is the end-to-end verification gate for the
entire foundation phase. Lower priority because it depends on all three prior stories.

**Independent Test**: Open `Samples~/Foundation/FoundationDemo.unity`, enter Play mode.
Verify: (a) `SDFRectElement` rounded shapes with shadows are visible, (b) text roles
display at correct sizes, (c) clicking "Switch Theme" button updates all `--m3-*`
USS variables and elements re-render in dark theme.

**Acceptance Scenarios**:

1. **Given** the `FoundationDemo` scene with `ThemeManager` holding `DefaultLight`,
   **When** Play mode is entered, **Then** all elements use light theme USS variable
   values and typography classes display at correct sizes.
2. **Given** the demo in Play mode, **When** "Switch Theme" button is pressed, **Then**
   all elements update to `DefaultDark` USS variable values within the same frame.
3. **Given** the demo scene, **When** `Unity_GetConsoleLogs` is checked, **Then** zero
   errors and zero shader compilation warnings appear.

---

### Edge Cases

- What happens when `ThemeManager.Instance` is accessed before `Awake`? (null guard required)
- What happens when no `PanelSettings` is assigned to the `UIDocument`? (Unity error, document in setup guide)
- What happens when `SDFRectElement` corner radius exceeds half the element's size? (SDF math must clamp gracefully)
- What happens when `ThemeManager` is present but `ThemeData` is null? (graceful no-op, warning log)
- What happens when two `ThemeManager` instances exist in the same scene? (second self-destructs with warning)
- What happens when a USS variable referenced with `var(--m3-primary)` is not set? (USS falls back to initial value, no crash)

## Requirements *(mandatory)*

### Functional Requirements

**WP-1 — UI Shader Graph**

- **FR-001**: The Shader Graph MUST render a rounded rectangle SDF with configurable
  corner radius (per-corner or uniform), implemented via Inigo Quilez's `sdRoundedBox`
  as a Custom Function node.
- **FR-002**: The shader MUST support a soft shadow with configurable offset, blur, and
  color via material properties or USS custom properties.
- **FR-003**: The shader MUST support an outline/border with configurable thickness and color.
- **FR-004**: The shader MUST support a state overlay (color + opacity) for hover, pressed,
  focused, and disabled states.
- **FR-005**: The shader MUST support a ripple effect driven by C# parameters
  (center, radius, alpha).
- **FR-006**: The Shader Graph MUST be a URP UI Shader Graph (Unity 6.3+ UI material type).
  It MUST handle UI Toolkit's clipping and masking correctly.
- **FR-007**: `SDFRectElement` MUST be a custom `VisualElement` subclass registered with
  `[UxmlElement]`, exposing Shader Graph parameters as USS custom properties or C#
  properties.

**WP-2 — Theme System**

- **FR-008**: `ThemeData` MUST be a ScriptableObject containing: 17 color roles
  (Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer, Secondary, OnSecondary,
  SecondaryContainer, OnSecondaryContainer, Surface, OnSurface, SurfaceVariant,
  OnSurfaceVariant, Error, OnError, Outline, OutlineVariant, Background), 6 elevation
  presets (shadow offset + blur + color + tonal overlay alpha), 7 shape presets, and
  4 motion presets.
- **FR-009**: `ThemeManager` MUST be a MonoBehaviour singleton with `DontDestroyOnLoad`.
  It MUST expose `ActiveTheme`, `SetTheme(ThemeData)`, and `OnThemeChanged` C# event.
- **FR-010**: On `SetTheme()` or scene start, `ThemeManager` MUST sync all `ThemeData`
  values to USS custom properties (`--m3-primary`, `--m3-surface`, etc.) on the root
  `VisualElement` of all active `UIDocument` panels.
- **FR-011**: Two USS theme files (`light.uss`, `dark.uss`) MUST define all `--m3-*`
  variables as the CSS source of truth; `ThemeManager` overrides them at runtime from SO.
- **FR-012**: A custom editor inspector for `ThemeData` MUST display all 17 color roles
  as visual swatches.
- **FR-013**: `DefaultLight.asset` and `DefaultDark.asset` MUST be provided with a
  complete M3-compatible color set.
- **FR-014**: dp conversion MUST use `PanelSettings` with `Scale With Screen Size`
  (1080×1920 reference). `Screen.dpi` MUST NOT be used.

**WP-3 — Typography System**

- **FR-015**: `typography.uss` MUST define USS classes for all 6 roles: `.m3-display`,
  `.m3-headline`, `.m3-title`, `.m3-body`, `.m3-label`, `.m3-caption`. Each class MUST
  set `font-size` and `-unity-font-definition` (pointing to correct weight TMP font asset).
- **FR-016**: Font weight variation MUST use separate `TMP_FontAsset` instances per weight
  (Regular 400, Medium 500). SDF dilation fake bold MUST NOT be used.
- **FR-017**: An optional `TypographyResolver` MonoBehaviour MUST allow assigning a
  `TextRole` from the Inspector, which programmatically adds the corresponding USS class.
- **FR-018**: A `DefaultTypography.asset` ScriptableObject MUST be provided referencing
  font assets. Pre-generated Roboto or Inter TMP font assets MUST be included.

### Key Entities

- **ThemeData**: ScriptableObject holding the full visual identity — colors, elevations,
  shapes, motion. Source of truth; synced to USS variables at runtime by ThemeManager.
- **ThemeManager**: Scene-level singleton. Owns the active `ThemeData`. Syncs SO → USS
  custom properties. Broadcasts `OnThemeChanged` for C# listeners.
- **SDFRectElement**: Custom `VisualElement` using a URP Shader Graph for SDF rendering
  (rounded corners, shadow, outline, state overlay, ripple).
- **TypographyConfig**: ScriptableObject defining one `TextStyle` per text role.
  Referenced for font asset lookup by `TypographyResolver`.
- **TypographyResolver**: Optional MonoBehaviour / C# helper that applies a `TextRole`
  as a USS class to a `VisualElement`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can render a themed rounded-rectangle panel with shadow in
  under 60 seconds by adding an `SDFRectElement` to a UIDocument and setting USS
  custom properties.
- **SC-002**: Calling `ThemeManager.SetTheme(darkTheme)` updates all USS custom property
  consumers within a single frame — no partial update across frames.
- **SC-003**: Applying `.m3-label` USS class to a Label element automatically sets the
  correct font size (14px) and Medium weight font asset without any C# code.
- **SC-004**: The Foundation sample scene opens, enters Play mode, and demonstrates all
  three systems working together with zero console errors.
- **SC-005**: No `NullReferenceException` is thrown when UISystem elements are present
  in a scene without a `ThemeManager`, or when a USS variable is undefined.

## Assumptions

- Unity 6.3+ (6000.3) is required. URP Shader Graph for UI Toolkit is a Unity 6.3 feature.
- TextMeshPro is available as a Unity package (included by default in Unity 6).
- URP (Universal Render Pipeline) is the active render pipeline.
- Odin Inspector is available in the project for custom editor tooling (`ThemeDataEditor`).
- `DefaultTypography.asset` ships with pre-generated TMP font assets (Roboto or Inter);
  developers replace font assets with project-specific ones.
- `ThemeManager` uses a MonoBehaviour singleton (`DontDestroyOnLoad`).
- DOTween is NOT a dependency of UISystem. Ripple and transitions use USS `transition`
  or coroutine/Update-based lerp.
- Sample fonts (Roboto/Inter) are downloaded from Google Fonts and imported manually;
  no automated font download tooling is included.
- The existing uGUI-based `SDFRectGraphic.cs`, `FoundationDemo` scene, and related
  uGUI code will be deleted and rebuilt from scratch — no migration of old code.
