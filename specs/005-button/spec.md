# Feature Specification: UISystem Button — WP-4 + WP-5 + WP-10 (partial)

**Feature Branch**: `005-uisystem-button`
**Created**: 2026-04-01
**Status**: Draft
**Input**: UISystem Button — WP-4 State Layer Controller + WP-5 Button Component (4 variants) + WP-10 partial Editor Context Menu

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — State Layer Interaction Feedback (Priority: P1)

A developer adds an interactive UISystem element to their scene. When the user interacts with it — hovering, pressing, focusing — the element provides immediate M3-compliant visual feedback: a tinted overlay at the correct M3 opacity level. On press, a ripple animation expands from the touch point. Disabled elements show reduced opacity and reject all interaction.

**Why this priority**: All subsequent components (Button, Card, TextField) depend on a working state feedback system. Without it, components feel non-interactive. It is the foundational interaction layer.

**Independent Test**: Add an `SDFRectElement` to a UIDocument scene. Attach `StateLayerController` to it. Enter Play mode. Hover (desktop) → verify tinted overlay appears at 8% opacity. Click → verify ripple expands and pressed overlay appears at 10% opacity. Release → verify overlay clears. Mark element disabled → verify it renders at 38% opacity and click produces no visual change.

**Acceptance Scenarios**:

1. **Given** a non-disabled `SDFRectElement` with `StateLayerController`, **When** the pointer enters the element, **Then** a state overlay at 8% opacity appears over the element.
2. **Given** a hovered element, **When** the pointer is pressed down, **Then** the overlay increases to 10% opacity and a ripple animation starts from the pointer position.
3. **Given** a pressed element, **When** the pointer is released or leaves, **Then** the overlay clears (returns to 0% or 8% if still hovered).
4. **Given** a focused element (keyboard navigation), **When** focus enters, **Then** a 10% opacity overlay appears.
5. **Given** a `disabled = true` element, **When** any pointer or focus event fires, **Then** no state change occurs and the element renders at 38% opacity.

---

### User Story 2 — M3 Button Component (Priority: P2)

A developer places an `M3Button` in their UIDocument with `variant="filled"` and `text="Save"`. In Play mode the button renders as a pill-shaped filled button with the theme's primary color, correct label typography, and elevation shadow. Clicking triggers the button's `OnClick` event. Switching the theme (light↔dark) automatically updates the button's colors without any per-button code.

**Why this priority**: The Button is the proof-of-concept component validating the full foundation stack (WP-1+2+3+4) working together. It is the gate before component expansion (WP-6+).

**Independent Test**: Create a UIDocument scene with four `M3Button` instances (Filled, Outlined, Text, Tonal). Enter Play mode. Each button renders with correct M3 colors, pill shape, correct label size. Click each — verify `OnClick` fires (log to console). Call `ThemeManager.Instance.ToggleLightDark()` — verify all buttons update colors immediately. Disable one button — verify 38% opacity and no click response.

**Acceptance Scenarios**:

1. **Given** an `M3Button` with `variant="filled"`, **When** rendered in Play mode, **Then** the background uses the theme's primary color, label uses the on-primary color, shape is full pill, and an elevation shadow is visible.
2. **Given** an `M3Button` with `variant="outlined"`, **When** rendered, **Then** background is transparent and a 1dp outline uses the theme's outline color.
3. **Given** an `M3Button` with `variant="text"`, **When** rendered, **Then** no background, no border, label uses the theme's primary color.
4. **Given** an `M3Button` with `variant="tonal"`, **When** rendered, **Then** background uses the secondary-container color, label uses the on-secondary-container color.
5. **Given** any enabled button, **When** clicked, **Then** the `OnClick` event fires exactly once.
6. **Given** the active theme is switched, **When** `ThemeManager.SetTheme()` is called, **Then** all button color tokens update automatically in the same frame.

---

### User Story 3 — Editor Context Menu for Button Creation (Priority: P3)

A developer right-clicks in the Unity Project window and uses `Assets > Create > UISystem > Button (Filled)` to generate a pre-configured UXML snippet. The generated file is placed in the selected folder and contains a correctly configured `M3Button` with the selected variant.

**Why this priority**: Improves workflow ergonomics but is not required for the component to function. Can be skipped without blocking any other work.

**Independent Test**: In the Unity Editor, right-click a Project folder → `Assets > Create > UISystem > Button (Filled)`. Verify a `.uxml` file is created. Open it — verify it contains an `M3Button` element with `variant="filled"`.

**Acceptance Scenarios**:

1. **Given** the UISystem package is present, **When** the developer uses `Assets > Create > UISystem > Button (Filled)`, **Then** a `.uxml` file with a Filled `M3Button` is created in the selected folder.
2. **Given** the context menu item for Outlined, **When** executed, **Then** a `.uxml` with `variant="outlined"` is created.

---

### Edge Cases

- What happens when `M3Button.text` is empty? → Button renders without label; minimum height is maintained (does not collapse).
- What happens when `StateLayerController`'s target element is not found? → Warning logged, no crash, no state changes.
- What happens when `ThemeManager.Instance` is null? → Elements render using USS fallback color values; no null reference exception.
- What happens when the button is disabled at runtime (toggled)? → Immediately updates visual state and enables/disables event handling.
- What happens on multi-touch (two fingers press simultaneously)? → Last event wins; pressed state is active while any pointer is down.

---

## Requirements *(mandatory)*

### Functional Requirements

**WP-4 — State Layer Controller**

- **FR-001**: `StateLayerController` MUST respond to `PointerEnterEvent`, `PointerLeaveEvent`, `PointerDownEvent`, `PointerUpEvent`, `FocusInEvent`, and `FocusOutEvent` on the target element via C# callbacks — NOT CSS pseudo-classes.
- **FR-002**: `StateLayerController` MUST set `SDFRectElement.StateOverlayOpacity` to 0.08 on hover, 0.10 on press, 0.10 on focus, and 0.0 on idle.
- **FR-003**: `StateLayerController` MUST call `RippleElement.StartRipple(pointerLocalPosition)` on `PointerDownEvent`.
- **FR-004**: When the `Disabled` property is true, `StateLayerController` MUST NOT respond to any pointer or focus events.
- **FR-005**: When `Disabled` is true, the target element MUST render at 38% opacity via the `.m3-disabled` USS class.
- **FR-006**: `StateLayerController` MUST be a plain C# class (not a MonoBehaviour) that can be constructed with a target `VisualElement` and an optional `RippleElement`.

**WP-5 — Button Component**

- **FR-007**: `M3Button` MUST be a `[UxmlElement]` `VisualElement` with `[UxmlAttribute]` properties: `text` (string), `variant` (ButtonVariant), `disabled` (bool).
- **FR-008**: `M3Button` MUST expose `public event Action OnClick` that fires on click when not disabled.
- **FR-009**: `M3Button` MUST internally compose: `SDFRectElement` (root container) + `RippleElement` (overlay child) + `Label` (text child) + hidden icon `VisualElement` (placeholder).
- **FR-010**: `M3Button` MUST use `StateLayerController` for all interaction state feedback on its root element.
- **FR-011**: The **Filled** variant MUST: background = `var(--m3-primary)`, label color = `var(--m3-on-primary)`, corner-radius = 9999, elevation-2 shadow.
- **FR-012**: The **Outlined** variant MUST: background = transparent, outline = 1dp `var(--m3-outline)`, no shadow.
- **FR-013**: The **Text** variant MUST: background = transparent, no outline, label color = `var(--m3-primary)`.
- **FR-014**: The **Tonal** variant MUST: background = `var(--m3-secondary-container)`, label color = `var(--m3-on-secondary-container)`.
- **FR-015**: All variants MUST apply the `m3-label` typography USS class to the label.
- **FR-016**: All variants MUST maintain a minimum touch target height of 48dp and standard height of 40dp with horizontal padding of 24dp.
- **FR-017**: `button.uss` MUST use only `var(--m3-*)` custom properties — no hardcoded color values.

**WP-10 — Editor Context Menu**

- **FR-018**: `UISystemMenuItems` MUST register `Assets > Create > UISystem > Button (Filled)` and `Assets > Create > UISystem > Button (Outlined)` in the Unity Editor context menu.
- **FR-019**: Each menu item MUST create a `.uxml` file in the currently selected Project folder containing a pre-configured `M3Button` with the correct `variant` attribute value.

### Key Entities

- **M3Button**: Interactive button `VisualElement`. Properties: `text` (string), `variant` (ButtonVariant), `disabled` (bool). Event: `OnClick` (Action). Internal composition: SDFRectElement + RippleElement + Label + icon slot.
- **ButtonVariant**: Enum — `Filled=0`, `Outlined=1`, `Text=2`, `Tonal=3`. Explicit integer values prevent serialization breakage on reorder.
- **StateLayerController**: Plain C# class. Attaches to a target `VisualElement`, holds a `RippleElement` reference. Controls state overlay opacity and ripple triggers.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 4 Button variants render with visually distinct, M3-correct colors in a single Play mode session — zero console errors.
- **SC-002**: Clicking any enabled button fires `OnClick` with no perceptible delay.
- **SC-003**: Hover state overlay appears within one rendered frame of the pointer entering the element.
- **SC-004**: Theme switching (light↔dark) updates all button colors in the same frame — zero per-button C# callbacks required.
- **SC-005**: Zero compile errors and zero UISystem-related warnings after implementation.
- **SC-006**: A disabled button does not fire `OnClick` after 10 rapid click attempts.

---

## Assumptions

- Foundation layer (WP-1+2+3) and all 8-step fixes are complete; all `var(--m3-*)` tokens are available via ThemeManager.
- `SDFRectElement.StateOverlayOpacity` and `StateOverlayColor` C# properties are the correct mechanism for state overlay — USS `background-color` overlays cannot be clipped to rounded rect boundaries.
- `RippleElement` is a child of `SDFRectElement`; rounded-rect clipping of the ripple is handled by the parent element's geometry.
- USS `var()` custom properties cannot be transitioned in Unity 6 UI Toolkit — all state changes are instant (V1 constraint, motion deferred to WP-6+).
- `:hover` and `:active` CSS pseudo-classes are explicitly NOT used — unreliable on mobile touch devices.
- Target platform: mobile (Android/iOS). Hover states are a desktop ergonomic that gracefully does nothing on touch-only devices.
- Icon slot is a hidden placeholder `VisualElement` in V1 — actual icon support is deferred to WP-6.
- WP-10 editor tooling generates static UXML files only — no runtime code generation or wizard UI in this branch.
