# UISystem

A modular, reusable UI component library for Unity, inspired by Material Design 3 (M3) principles.

This is **not** a 1:1 implementation of the M3 specification. It leverages Google's years of UX research — sizing, spacing, state feedback, color hierarchy, typography scale — to provide a Unity-native, performant, and flexible UI foundation.

**Target platforms:** Mobile (Android/iOS) — casual and hypercasual games as priority, utility apps supported.

**Dependencies:** None. Works as a standalone submodule. Can be used alongside AssetSystem but does not require it.

**UI Backend:** uGUI (Canvas-based). UI Toolkit's runtime side is not yet mature enough and has limited custom shader support, so uGUI is the preferred backend. Migration to UI Toolkit may be evaluated in the future.

> **Note:** This document defines scope and work plan. Detailed technical decisions for each work package will be finalized during implementation.

---

## Architecture Layers

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

Dependencies flow top-down. Foundation depends on nothing. Core Systems depend only on Foundation. Components depend on both. Editor Tooling accesses all layers but runs only in the Editor — it is excluded from runtime builds.

---

## Work Packages

### WP-1: Foundation — SDF UI Shader

**Goal:** Write a single SDF shader that serves as the visual foundation for all UI components.

**Shader Capabilities:**

- **Rounded Rectangle SDF** — Independent radius per corner (Vector4). Based on Inigo Quilez's `sdRoundedBox` function.
- **Shadow / Glow** — Soft shadow via offset (Vector2), blur (float), and color (Color) parameters. Computed through smoothstep over SDF distance values. M3's 5 elevation levels will be defined as presets of these parameters.
- **Outline / Border** — Thickness and color parameters. Inner/outer selection based on SDF distance.
- **State Overlay** — Overlay color + opacity parameters for hover, pressed, focused, and disabled states. Alpha-blended on top of base color in the fragment shader.
- **Ripple Effect** — Expanding circle animation from touch point. `_RippleCenter` (Vector2), `_RippleRadius` (float), `_RippleAlpha` (float) parameters, driven from script.

**Technical Details:**

- The shader will be forked from Unity's default UI shader (`UI/Default`). Stencil, masking, and clipping requirements of the UI system will be preserved.
- Additional parameters will be sent as vertex data via UV channels (TexCoord1, TexCoord2). `Additional Shader Channels` must be enabled on the Canvas.
- Single shader, single material instance (per-component override via MaterialPropertyBlock). Draw call batching must be maintained.

**Deliverables:**

- `Shaders/SDFRect.shader`
- `Scripts/SDFRectGraphic.cs` — MonoBehaviour derived from uGUI's `Graphic` base class, managing shader parameters. This component handles packing parameters into UV channels.
- Sample scene demonstrating various corner radius, shadow, and overlay combinations.

**To Research / Clarify:**

- [ ] `MaterialPropertyBlock` vs shared material instance: profile the impact on batching. `MaterialPropertyBlock` may not be fully compatible with the `Graphic` class in uGUI — behavior may differ based on Canvas render mode (Screen Space Overlay vs Camera). Alternative: each component holds its own material clone, but this increases draw calls.
- [ ] UV channel packing strategy: how many parameters fit, what to do on overflow (use TexCoord3? struct packing?).
- [ ] SDF fragment shader performance on mobile GPUs: test the fill rate impact of the `smoothstep` + shadow + ripple combination on Mali GPUs. If needed, evaluate fallback options such as moving shadow to a separate pass or using 9-slice sprites.
- [ ] Correctly porting Unity's UI shader stencil/masking mechanism to the custom shader — requires understanding the `Graphic.materialForRendering` pipeline.

---

### WP-2: Foundation — Theme System (Design Tokens)

**Goal:** Build a ScriptableObject-based theming infrastructure inspired by M3's token hierarchy.

**Token Hierarchy (simplified):**

```
Reference Tokens (raw colors)
  └─ System Tokens (roles: primary, secondary, surface, error...)
       └─ Component Tokens (button-container-color = system.primary)
```

In Unity, this hierarchy will be flattened into a single `ThemeData` ScriptableObject. The full M3 hierarchy introduces unnecessary complexity for game projects.

**ThemeData ScriptableObject contents:**

- **Color Palette**
  - Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer
  - Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer
  - Tertiary, OnTertiary (optional, can be left empty initially)
  - Surface, OnSurface, SurfaceVariant, OnSurfaceVariant
  - Error, OnError
  - Outline, OutlineVariant
  - Background (may equal Surface)
- **Elevation Presets** — 6 levels (0–5), each holding shadow offset, blur, color, and tonal overlay alpha values.
- **Shape Presets** — None (0), ExtraSmall (4), Small (8), Medium (12), Large (16), ExtraLarge (28), Full (9999 = fully rounded). Values in dp, converted to pixels at runtime based on screen density.
- **Motion Presets** — Structs holding `AnimationCurve` + duration (ms). Emphasized, Standard, EmphasizedDecelerate, and StandardDecelerate presets are sufficient.

**ThemeManager (Singleton MonoBehaviour or Static Class):**

- Holds the active `ThemeData` reference.
- Light/Dark switching: swaps between two `ThemeData` SO instances.
- `OnThemeChanged` event: all components listen to this event and update their visuals.
- Runtime theme switching support.

**Deliverables:**

- `ScriptableObjects/ThemeData.cs`
- `Scripts/ThemeManager.cs`
- `Editor/ThemeDataEditor.cs` — Custom inspector: visual color palette preview, side-by-side light/dark SO comparison.
- At least 2 sample theme assets: one light, one dark.

**To Research / Clarify:**

- [ ] dp → pixel conversion strategy: is `Screen.dpi` reliable? Should the Canvas Scaler reference resolution be used as the basis? Or a fixed scale factor? This decision directly affects shape, spacing, and typography sizing.
- [ ] ThemeManager lifecycle: Singleton pattern, ServiceLocator, or ScriptableObject-based event system? Singleton provides cross-scene persistence but reduces testability.
- [ ] Automatic color palette generation: M3 generates tonal palettes from a seed color (HCT color space). Is runtime generation in Unity necessary, or is offline generation (as an editor tool) sufficient? Manual definition should be enough for the initial version; an editor tool can be added later.

---

### WP-3: Foundation — Typography System

**Goal:** Model M3's typography scale on top of the TextMeshPro infrastructure.

**Typography Scale (M3-inspired, simplified):**

| Role       | Size | Weight   | Usage                          |
|------------|------|----------|--------------------------------|
| Display    | 36+  | Regular  | Splash screen, large numbers   |
| Headline   | 28   | Regular  | Section headings               |
| Title      | 22   | Medium   | Card titles, dialog titles     |
| Body       | 16   | Regular  | General text                   |
| Label      | 14   | Medium   | Button labels, captions        |
| Caption    | 12   | Regular  | Helper text, timestamps        |

Each role defines: font asset reference, size, weight (TMP font style), line height, letter spacing.

**TypographyConfig ScriptableObject:**

- A `TextStyle` struct per role: `TMP_FontAsset`, `fontSize`, `fontStyle`, `lineSpacing`, `characterSpacing`.
- Text colors can be automatically assigned via ThemeData reference (OnSurface, OnPrimary, etc.).

**TypographyResolver:**

- A `TextRole` enum is assigned to a TMP_Text component.
- The resolver pulls the correct style from ThemeData and TypographyConfig and applies it.
- Automatic update on theme change.

**Deliverables:**

- `ScriptableObjects/TypographyConfig.cs`
- `Scripts/TypographyResolver.cs` — Helper component added to TMP_Text.
- `Enums/TextRole.cs`
- Default `TypographyConfig` asset (with Roboto or Inter from Google Fonts).

**To Research / Clarify:**

- [ ] Font asset management: single TMP font asset (all weights as font styles), or separate assets per weight? Will TMP-native effects like outline/shadow via material presets be used?
- [ ] Dynamic font asset vs static: impact on atlas size on mobile. Character set coverage across languages (Turkish, English).
- [ ] Should M3's "large/medium/small" variants per role be included initially? The table above defines a single size per role. If needed, each role can be expanded to 3 variants (6 → 18 styles). The simplified version is recommended for the first release.

---

### WP-4: Core Systems — State Layer Controller

**Goal:** Manage interaction states (normal, hovered, pressed, focused, disabled) of components and provide visual feedback.

**M3 State Layer Principle:**

Every interactive component has a semi-transparent overlay layer whose opacity changes based on state. M3 specifies the following overlay opacities:

| State    | Overlay Opacity |
|----------|----------------|
| Enabled  | 0%             |
| Hovered  | 8%             |
| Focused  | 10%            |
| Pressed  | 10%            |
| Disabled | Element: 38% opacity, container: 12% opacity |

**StateLayerController MonoBehaviour:**

- Implements `IPointerEnterHandler`, `IPointerExitHandler`, `IPointerDownHandler`, `IPointerUpHandler`, `ISelectHandler`, `IDeselectHandler`.
- Updates the SDF shader's state overlay parameters (`_StateOverlayColor` and `_StateOverlayOpacity` from WP-1).
- Triggers ripple effect on pressed state.
- Smooth transitions between states: not instant, but lerped using curves and durations from MotionPresets.

**Deliverables:**

- `Scripts/StateLayerController.cs`
- Ripple animation management (Coroutine or Update-based).

**To Research / Clarify:**

- [ ] Touch vs mouse input differences: hover does not exist on mobile, only pressed and released. Should hover be active only for Editor and PC builds?
- [ ] Ripple management when multiple fingers press different components simultaneously?
- [ ] When `interactable=false` is set on a disabled state, uGUI events are not triggered — how to apply disabled visuals? `OnEnable`/`OnDisable` or `interactable` property observer?

---

### WP-5: Components — Button

**Goal:** The first and most fundamental component. Serves as the proof-of-concept for the entire infrastructure (shader, theme, state, typography) working together.

**M3 Button Types (simplified):**

| Type      | Usage                           | Visual                         |
|-----------|---------------------------------|--------------------------------|
| Filled    | Primary action                  | Container: Primary color, text: OnPrimary |
| Outlined  | Secondary/alternative action    | Transparent container, border, text: Primary |
| Text      | Lowest emphasis                 | No container, text: Primary    |
| Tonal     | Between filled and outlined     | Container: SecondaryContainer  |

All button types use the same MonoBehaviour; behavior and visuals differ via `ButtonStyle` enum. All use the same SDF shader.

**Button MonoBehaviour:**

- `SDFRectGraphic` (WP-1) reference — visuals.
- `StateLayerController` (WP-4) reference — interaction.
- `TMP_Text` child — button label, auto-styled as Label via `TypographyResolver` (WP-3).
- `ButtonStyle` enum: Filled, Outlined, Text, Tonal.
- Listens to `ThemeManager.OnThemeChanged` event and updates colors.
- Optional icon support (left or right, `Image` or `TMP_SpriteAsset`).

**Sizing Derived from M3 Principles:**

- Minimum height: 40dp
- Horizontal padding: 24dp (16dp on icon side if icon present)
- Corner radius: shape preset "Full" (20dp, i.e. half the height — pill shape)
- Label: `TextRole.Label` (14sp, Medium weight)

**Deliverables:**

- `Scripts/Components/M3Button.cs`
- `Prefabs/M3Button.prefab` — Prefab with default configuration.
- `Editor/M3ButtonEditor.cs` — Custom inspector: ButtonStyle selection, preview, quick theme color override.
- Test scene: 4 button types × light/dark theme combinations.

**To Research / Clarify:**

- [ ] Prefab variant strategy: single prefab + enum, or separate prefab variant per style? The enum approach is more flexible but provides a weaker "drag-and-drop" inspector UX. Prefab variants offer drag-and-drop convenience.
- [ ] Icon integration: TMP inline sprite or separate `Image` child? Layout calculation (icon + spacing + label) — HorizontalLayoutGroup or manual RectTransform calculation?
- [ ] Auto-sizing: should button width adjust to content (Content Size Fitter) or be fixed/flexible as an option?

---

### WP-6: Components — Card

**Goal:** A card component for content grouping and information display.

**M3 Card Types:**

| Type      | Usage               | Visual                              |
|-----------|----------------------|-------------------------------------|
| Elevated  | Default              | Surface color, elevation shadow     |
| Filled    | Less prominent       | SurfaceVariant color, no shadow     |
| Outlined  | Equal hierarchy      | Surface color, outline border       |

**Card MonoBehaviour:**

- `SDFRectGraphic`-based container.
- `CardStyle` enum: Elevated, Filled, Outlined.
- Corner radius: M3 preset "Medium" (12dp).
- Child content area: inner RectTransform separated from container by padding.
- Optional header area (title + subtitle), media area (image), action area (button row).

**Deliverables:**

- `Scripts/Components/M3Card.cs`
- `Prefabs/M3Card.prefab`
- Sample: vertical scroll card list demo scene.

**To Research / Clarify:**

- [ ] Child layout management within cards: fixed slots (header/media/content/actions) or free-form content area? M3 defines card anatomy, but being this rigid in game UI may be unnecessary.
- [ ] Clickable card: should the entire card behave like a button (with state layer), or only the action area within it? Should both be supported?

---

### WP-7: Components — Toggle & TextField

**Goal:** Form elements — binary selection and text input.

**Toggle (Switch):**

- M3 switch visuals: track (pill shape) + thumb (circle, size changes based on state).
- Drawable via SDF shader: track = rounded rect, thumb = circle SDF.
- Animated transition: thumb position + track color lerp.
- Theme colors: on state → Primary/OnPrimary, off state → SurfaceVariant/Outline.

**TextField:**

- M3 defines Filled and Outlined variants.
- Built on top of TMP_InputField, visual layer via SDFRectGraphic.
- Floating label animation: label slides up and scales down on focus.
- Optional sub-labels: helper text, error text, character counter.
- Error state: outline/underline in Error color.

**Deliverables:**

- `Scripts/Components/M3Toggle.cs`
- `Scripts/Components/M3TextField.cs`
- Prefabs and test scene.

**To Research / Clarify:**

- [ ] Toggle thumb animation: inside the SDF shader (thumb position as a shader parameter) or via a separate GameObject's RectTransform.anchoredPosition? The shader approach yields a single draw call but is complex; the GameObject approach is simple but uses 2 draw calls (track + thumb).
- [ ] TextField floating label: scale + position animation of a separate TMP_Text. Should placeholder text and label be the same? In M3, the label is always visible (at placeholder position when empty, small and above when filled/focused). Integrating this behavior with uGUI's InputField may be challenging — a custom input handler may be needed.
- [ ] Mobile keyboard interaction: compatibility with `TouchScreenKeyboard`. When InputField focus opens the soft keyboard, should UI shifting (scroll/reposition) be managed by this component or externally?

---

### WP-8: Components — Dialog & Snackbar

**Goal:** Overlay UI elements for user notification and confirmation.

**Dialog:**

- Modal overlay: semi-transparent black scrim (M3 uses `Surface` + 32% opacity).
- Dialog container: Surface color, Elevation Level 3, corner radius "ExtraLarge" (28dp).
- Anatomy: icon (optional) + title + content + action buttons (bottom-right, max 2).
- Open/close animation: scale + fade, using "Emphasized" curve from MotionPresets.

**Snackbar:**

- Notification bar that appears from the bottom of the screen and auto-dismisses.
- Container: InverseSurface color, corner radius "ExtraSmall" (4dp).
- Optional single action button (text style).
- Auto-dismiss: configurable duration (4–10 seconds).
- Queue system: multiple snackbars are shown sequentially.

**Deliverables:**

- `Scripts/Components/M3Dialog.cs`
- `Scripts/Components/M3Snackbar.cs`
- `Scripts/Systems/SnackbarManager.cs` — Queue and display management.
- Prefabs, test scene.

**To Research / Clarify:**

- [ ] Canvas ordering for Dialog and Snackbar: separate "Overlay" Canvas, or on top of the existing Canvas via sorting order? The separate Canvas approach is safer (always on top) but multiple Canvases may have a performance cost.
- [ ] Snackbar safe area compliance: must not overflow into notch and home indicator areas. Is `Screen.safeArea` usage sufficient?
- [ ] Should the dialog scrim-tap-to-dismiss behavior be configurable? (M3 default: scrim tap dismisses, but "required action" dialogs do not dismiss.)

---

### WP-9: Components — Navigation

**Goal:** Navigation bar and tab components for screen-to-screen transitions.

**Bottom Navigation Bar:**

- M3 spec: 3–5 items, each with icon + optional label.
- Active item: Primary-colored icon, indicator (pill shape) background.
- Container: Surface color, Elevation Level 2.
- Animated transition: active indicator slide/morph.

**Tab Bar (Top Tabs):**

- Scrollable or fixed.
- Active tab: Primary-colored underline (indicator), text color change.

**Deliverables:**

- `Scripts/Components/M3BottomNav.cs`
- `Scripts/Components/M3TabBar.cs`
- Prefabs, demo scene with multiple "pages."

**To Research / Clarify:**

- [ ] Should the navigation component integrate with a screen management (screen/page system) or remain purely visual? If screen management is included, it significantly expands the scope. For the initial version, a visual-only component + `OnItemSelected(int index)` callback should suffice.
- [ ] Bottom nav safe area: overlap with the system gesture bar at the bottom. Padding calculation.
- [ ] Active tab indicator animation: RectTransform position + width lerp, or shader-based?

---

### WP-10: Editor Tooling — Component Builder Wizard

**Goal:** Editor tools for rapidly creating UISystem components and layout structures, and saving them as prefabs.

**Motivation:**

Creating an M3Button manually requires: create GameObject → add SDFRectGraphic → create child GameObject → add TMP_Text → add StateLayerController → add TypographyResolver → bind ThemeManager references → set parameters → save as prefab. This process takes 2–5 minutes per component and carries a high risk of errors. With the wizard, the same task takes 10–15 seconds.

**Sub-tools:**

**A) Component Creator Window (`EditorWindow`):**

A step-by-step wizard for creating a single component.

- **Step 1 — Type Selection:** Button, Card, Toggle, TextField, Dialog, Snackbar, BottomNav, TabBar. List selection.
- **Step 2 — Style Selection:** Sub-styles shown based on the selected component (Button → Filled/Outlined/Text/Tonal, Card → Elevated/Filled/Outlined, etc.).
- **Step 3 — Configuration:** Active ThemeData selection, size overrides (width/height, optional), content (button text, icon selection, etc.).
- **Step 4 — Create:** "Create in Scene" or "Create as Prefab" option. If prefab is selected, the save path is shown.

The wizard builds the correct GameObject hierarchy, adds all components, binds references, and registers with the theme. The user just selects and clicks.

**B) Layout Composer Window (`EditorWindow`):**

A tool for composing multiple components within a layout.

- **Layout Presets:** Common structures created with a single click:
  - **Button Row** — Horizontal button group (1–4 buttons, configurable spacing). For dialog bottoms, form submit areas.
  - **Card List** — Vertical card list (ScrollRect + VerticalLayoutGroup + N cards). For inventory, settings, level select screens.
  - **Card Grid** — Grid card layout (GridLayoutGroup + N cards). For item shops, character selection screens.
  - **Header + Content + Footer** — Three-section page layout. General screen template.
  - **Form Layout** — Vertical TextField + Label groups. For settings, login, profile edit screens.

- **Workflow:** User selects a preset → sets item count and spacing → specifies the component type for each item → "Generate" places it in the scene or saves it as a prefab.

**C) Context Menu Shortcuts:**

UISystem shortcuts added to the Hierarchy panel right-click menu:

```
GameObject > UISystem > Button (Filled)
GameObject > UISystem > Button (Outlined)
GameObject > UISystem > Card (Elevated)
GameObject > UISystem > Toggle
GameObject > UISystem > TextField
GameObject > UISystem > Layout > Button Row
GameObject > UISystem > Layout > Card List
...
```

This enables quick creation without opening the wizard — same logic as Unity's built-in "UI > Button" menu.

**D) Prefab Saver Utility:**

Helper for saving created structures as prefabs:

- Save path selection (default: `Assets/UISystem/Prefabs/Custom/`).
- Overwrite confirmation if a prefab already exists.
- Option to save as a prefab variant (preserving the base UISystem prefab).

**Deliverables:**

- `Editor/Wizard/ComponentCreatorWindow.cs`
- `Editor/Wizard/LayoutComposerWindow.cs`
- `Editor/Wizard/UISystemMenuItems.cs` — Context menu shortcuts.
- `Editor/Wizard/PrefabSaverUtility.cs`
- `Editor/Wizard/ComponentFactory.cs` — Central class for all component creation logic. Both the wizard and context menu delegate to this.

**To Research / Clarify:**

- [ ] Preview mechanism: can a live preview of the component be shown inside the wizard? Setting up a preview render pipeline within an `EditorWindow` (e.g., `PreviewRenderUtility`) can be complex. A static preview image/icon may be a sufficient alternative.
- [ ] Undo support: use `Undo.RegisterCreatedObjectUndo` to enable Ctrl+Z for the created hierarchy. For the Layout Composer, undo grouping (`Undo.SetCurrentGroupName`) is needed when multiple objects are created.
- [ ] Custom preset definition in the Layout Composer: can users save and reuse their own layout presets? This feature may be excessive for the first release, but the architecture should leave the door open (presets stored as ScriptableObjects make this extensible).

---

## Development Order and Dependencies

```
WP-1 (SDF Shader) ──────────────┐
                                 ├──► WP-4 (State Layer) ──► WP-5 (Button) ──► WP-6 (Card)
WP-2 (Theme System) ────────────┤                                │
                                 │                                ▼
WP-3 (Typography) ──────────────┘                        WP-7 (Toggle/TextField)
                                                                  │
                                                                  ▼
                                                         WP-8 (Dialog/Snackbar)
                                                                  │
                                                                  ▼
                                                         WP-9 (Navigation)

WP-10 (Component Builder Wizard) ◄── Can start after WP-5 is complete,
                                      expands as each new component is added.
```

**Phase 1 — Foundation:** WP-1, WP-2, WP-3 can be developed in parallel. Component work cannot begin until the shader is ready.

**Phase 2 — Core + First Component:** WP-4 + WP-5. Button is the proof-of-concept for the entire infrastructure. Issues discovered here may require revisiting Foundation.

**Phase 2.5 — Editor Tooling Foundation:** The first version of WP-10 (Component Creator + Context Menu) should start immediately after WP-5 is complete. Begins with the Button wizard; new types are added to the wizard as each new component (WP-6, WP-7…) is completed. The Layout Composer becomes useful once at least 2–3 components are ready.

**Phase 3 — Component Expansion:** WP-6 → WP-7 → WP-8 → WP-9 sequentially. Each component builds on its predecessor and carries forward lessons learned. Each completed component is added to WP-10.

---

## Folder Structure

```
UISystem/
├── README.md
├── SCOPE.md                          ← this file
├── Runtime/
│   ├── Shaders/
│   │   └── SDFRect.shader
│   ├── ScriptableObjects/
│   │   ├── ThemeData.cs
│   │   └── TypographyConfig.cs
│   ├── Core/
│   │   ├── ThemeManager.cs
│   │   ├── StateLayerController.cs
│   │   ├── TypographyResolver.cs
│   │   └── MotionPresets.cs
│   ├── Components/
│   │   ├── M3Button.cs
│   │   ├── M3Card.cs
│   │   ├── M3Toggle.cs
│   │   ├── M3TextField.cs
│   │   ├── M3Dialog.cs
│   │   ├── M3Snackbar.cs
│   │   ├── M3BottomNav.cs
│   │   └── M3TabBar.cs
│   ├── Graphics/
│   │   └── SDFRectGraphic.cs
│   ├── Enums/
│   │   ├── ButtonStyle.cs
│   │   ├── CardStyle.cs
│   │   ├── ElevationLevel.cs
│   │   ├── ShapePreset.cs
│   │   └── TextRole.cs
│   └── Data/
│       ├── ElevationPreset.cs
│       └── TextStyleData.cs
├── Editor/
│   ├── Inspectors/
│   │   ├── ThemeDataEditor.cs
│   │   └── M3ButtonEditor.cs
│   └── Wizard/
│       ├── ComponentCreatorWindow.cs
│       ├── LayoutComposerWindow.cs
│       ├── UISystemMenuItems.cs
│       ├── PrefabSaverUtility.cs
│       └── ComponentFactory.cs
├── Assets/
│   ├── Themes/
│   │   ├── DefaultLight.asset
│   │   └── DefaultDark.asset
│   ├── Typography/
│   │   ├── DefaultTypography.asset
│   │   └── Fonts/
│   └── Prefabs/
│       ├── M3Button.prefab
│       ├── M3Card.prefab
│       ├── M3Toggle.prefab
│       ├── M3TextField.prefab
│       ├── M3Dialog.prefab
│       ├── M3Snackbar.prefab
│       └── M3BottomNav.prefab
├── Samples~/
│   ├── AllComponents/
│   ├── ThemeSwitching/
│   └── MobileDemo/
└── package.json                      ← can also be used as a UPM-compatible package
```

---

## Out of Scope (For Now)

- **Dynamic Color generation:** Automatic palette generation from a seed color (HCT color space). Manual definition is sufficient for the first release.
- **UI Toolkit backend:** Runtime UI Toolkit support to be evaluated in the future.
- **Screen/Page management:** Navigation components are visual only; the screen transition system is outside this submodule.
- **Localization integration:** The typography system manages fonts/sizes; localization string management is external.
- **Accessibility:** M3's contrast ratio and touch target requirements will be followed in principle, but WCAG compliance testing is out of scope.
- **Complex components:** Chip, Slider, Date Picker, Bottom Sheet, Drawer, Menu, Progress Indicator — to be added as needed, not in the initial scope.
- **Asset Store release:** Documentation and packaging requirements to be addressed later.

---

## Nice to Have (Optional)

### Design Token Importer

An editor tool that reads design tokens from mature web design systems (Bootstrap, Tailwind CSS, Chakra UI, Ant Design) and converts them into a `ThemeData` ScriptableObject.

**What it does:** These frameworks publish their color palettes, spacing scales, border-radius values, font sizes, and shadow presets in JSON or CSS custom properties format. The importer parses these files, converts the values to Unity's dp/pixel space, and maps them to ThemeData fields.

**Why optional:** M3's own tokens + manual fine-tuning is sufficient for most projects. The importer becomes valuable when rapid prototyping with different visual identities across multiple projects is needed. If not needed for the first release, it can be deferred — as long as the ThemeData SO is well-designed, adding an importer on top of it is straightforward.
