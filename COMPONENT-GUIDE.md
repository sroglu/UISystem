# UISystem Component Creation Guide

**Version**: 1.0.0 | **Updated**: 2026-04-03 | **Feature**: 006-m3-uisystem-overhaul

This guide is the authoritative reference for creating or modifying M3 components in UISystem. All rules are constitution-level unless marked as recommendations.

---

## 1. Mandatory Rules

These rules are enforced by constitution and must not be violated without an explicit entry in the plan's Complexity Tracking section.

| Rule | Enforcement |
|------|------------|
| **USS-Only Theming** — All visual properties resolved via USS `var(--m3-*)` | `grep style.color` in CI must return 0 |
| **Zero Cross-Assembly Dependencies** — No `mehmetsrl.*` references | `mehmetsrl.UISystem.asmdef` refs checked |
| **ScriptableObject Configuration** — No `static readonly` color/float constants | Grep check before merge |
| **Extend M3ComponentBase** — All new components must extend the base class | Code review gate |
| **USS Transition Durations** — All `transition:` durations use `var(--m3-motion-*)` tokens | USS lint check |
| **No DOTween in Runtime** — Use USS `transition` or `AnimationCurve` + `IVisualElementScheduler` | Asmdef deps check |
| **Material Symbols for Icons** — Use `MaterialSymbols` constants + `.m3-icon` CSS class, not Painter2D paths | Exception registry required for Painter2D icons |

---

## 2. USS-Only Theming Rule

### The Rule

**All visual properties (colors, opacity, background colors, border colors, text colors) MUST be resolved through USS custom properties.** Components MUST NOT assign theme colors via C# `style.color`, `style.backgroundColor`, or similar inline assignments.

### Why

The dual-source model (USS + inline C#) creates:
- Maintenance burden: two places to update when a color token changes
- Theme-switching bugs: USS updates instantly, C# updates only on next render or `RefreshThemeColors()` call
- Inconsistency: some properties animate via USS `transition`, C# assignments are instant

### How

**Correct approach** — let USS resolve the color:
```css
/* button.uss */
.m3-button--filled .m3-button__label {
    color: var(--m3-on-primary);
}
.m3-button--outlined .m3-button__label {
    color: var(--m3-primary);
}
```

No C# required. When theme switches, `light.uss` or `dark.uss` is swapped and USS re-resolves all `var(--m3-*)` values automatically.

**Wrong approach** — do NOT do this:
```csharp
// ❌ WRONG — inline C# color assignment
_label.style.color = new StyleColor(theme.GetColor(ColorRole.Primary));
```

### Documented Exceptions

These properties **cannot** be USS and must remain in C#. Each exception is justified below.

| Property | Component | Justification | Allowed In |
|----------|-----------|---------------|-----------|
| `SDFRectElement.StateOverlayOpacity` | All interactive | Overlay is rendered inside Painter2D `OnGenerateVisualContent`, clipped to the SDF rounded rect shape. CSS `opacity` on the element affects the whole element including children, not just the overlay layer. | `StateLayerController.cs` only |
| `SDFRectElement.TonalOverlayOpacity` / `TonalOverlayColor` | Card (elevated), others | Same reasoning as StateOverlayOpacity — a GPU-rendered layer inside `OnGenerateVisualContent`. | `RefreshThemeColors()` only |
| `SDFRectElement.RippleCenter` / `RippleRadius` / `RippleAlpha` | All interactive | Per-frame values for an expanding circle animation. These change 60 times/second during a ripple — USS cannot express center-relative expanding animations. | `RippleElement.cs` only |
| `SDFRectElement.FillColorOverride` | Card, Toggle | SDFRectElement fills its background via `Painter2D` before CSS `background-color` applies. The override is used when the element needs a custom fill color that cannot be expressed via USS alone (e.g., tonal surface colors computed from elevation). | Justified components only |
| `_floatingLabel.style.backgroundColor` (TextField) | M3TextField | The Outlined variant floating label needs a surface-colored background to create the "notch" effect where the label overlaps the border. This requires reading the panel's actual surface color at runtime, which `var(--m3-surface)` alone cannot guarantee for nesting scenarios. | M3TextField only |

**Adding a new exception**: Document it in this registry with justification before implementing.

---

## 3. Component Anatomy

All M3 components follow this visual tree structure:

```
ComponentRoot (extends M3ComponentBase)
├── _container : SDFRectElement       ← main visual, handles bg + shadow + outline + ripple
│   ├── RippleElement                 ← child of container, absolute positioned, pointer-none
│   └── [content children]           ← labels, icons, slots
└── [optional outer elements]        ← touch target wrappers, scrim overlays
```

### Naming Conventions

- Private fields: `_camelCase` (e.g., `_container`, `_label`, `_iconEl`)
- USS classes on container: `m3-[component]` (e.g., `m3-button`, `m3-card`)
- USS element classes: `m3-[component]__[element]` (e.g., `m3-button__label`, `m3-button__icon`)
- USS modifier classes: `m3-[component]--[modifier]` (e.g., `m3-button--filled`, `m3-button--large`)
- State classes added/removed by C#: `m3-[component]--active`, `m3-[component]--selected`, `m3-[component]--disabled`

### Required Files Per Component

| File | Location | Purpose |
|------|----------|---------|
| `M3[Name].cs` | `Runtime/Components/` | Component logic |
| `[name].uss` | `Styles/Components/` | Component styles |
| `M3[Name].uxml` | `UXML/` | Optional reusable template |
| `showcase/[Name]-overview.uxml` | `UXML/showcase/` | Showcase Overview tab |
| `showcase/[Name]-specs.uxml` | `UXML/showcase/` | Showcase Specs tab |
| `showcase/[Name]-guidelines.uxml` | `UXML/showcase/` | Showcase Guidelines tab |

---

## 4. M3ComponentBase Extension Pattern

### Basic Component Template

```csharp
using UnityEngine.UIElements;
using mehmetsrl.UISystem.Core;
using mehmetsrl.UISystem.Data;
using mehmetsrl.UISystem.ScriptableObjects;

namespace mehmetsrl.UISystem.Components
{
    [UxmlElement]
    public partial class M3MyComponent : M3ComponentBase
    {
        // ── UXML Attributes ──────────────────────────────────────────────
        private MyVariant _variant = MyVariant.Default;

        [UxmlAttribute]
        public MyVariant Variant
        {
            get => _variant;
            set { _variant = value; ApplyVariant(); }
        }

        // ── Visual Elements ──────────────────────────────────────────────
        private SDFRectElement _container;
        private Label _label;

        // ── Lifecycle ────────────────────────────────────────────────────
        public M3MyComponent()
        {
            AddToClassList("m3-mycomponent");
            BuildVisualTree();
        }

        protected override void BuildVisualTree()
        {
            _container = new SDFRectElement();
            _container.AddToClassList("m3-mycomponent__container");

            var ripple = new RippleElement();
            _container.Add(ripple);

            _label = new Label("Label");
            _label.AddToClassList("m3-mycomponent__label");
            _container.Add(_label);

            // Set up StateLayerController via base class
            InitStateLayer(_container, ripple);

            hierarchy.Add(_container);
        }

        protected override void ApplyTheme(ThemeData theme)
        {
            // ✅ CORRECT: Only set non-CSS properties (exceptions from registry)
            // _container.TonalOverlayColor = theme.GetColor(ColorRole.Primary); // only if elevated

            // ❌ WRONG: Do NOT assign colors here
            // _label.style.color = new StyleColor(theme.GetColor(ColorRole.OnSurface));
        }

        private void ApplyVariant()
        {
            RemoveFromClassList("m3-mycomponent--default");
            // ... add correct variant class
            AddToClassList($"m3-mycomponent--{_variant.ToString().ToLower()}");
        }
    }
}
```

### What M3ComponentBase Provides

- `InitStateLayer(SDFRectElement, RippleElement)` — attaches StateLayerController
- `StateLayer` property — access the attached controller
- Automatic `ThemeManager.OnThemeChanged` subscription on `AttachToPanelEvent`
- Automatic unsubscription on `DetachFromPanelEvent`
- Calls `ApplyTheme()` when theme changes (after USS is applied)
- Handles `Disabled` property toggling `.m3-disabled` class

---

## 5. Icon Usage

### Rule: Use Material Symbols font, not Painter2D paths

For all standard M3 icons, use font glyphs via the `MaterialSymbols` constants class and `.m3-icon` CSS class.

```csharp
// ✅ CORRECT
var iconLabel = new Label(MaterialSymbols.Search);
iconLabel.AddToClassList("m3-icon");  // applies MaterialSymbols-Filled font, 24px, centered
```

```csharp
// ❌ WRONG — Painter2D hardcoded paths
protected override void OnGenerateVisualContent(MeshGenerationContext ctx)
{
    var p = ctx.painter2D;
    p.MoveTo(new Vector2(12, 6));
    p.LineTo(new Vector2(18, 12));
    // ... 20 more lines for a search icon
}
```

### Available CSS Classes

| Class | Font | Use For |
|-------|------|---------|
| `.m3-icon` | MaterialSymbols-Filled | Standard filled icons (most components) |
| `.m3-icon-outlined` | MaterialSymbols-Standard | Outlined/unfilled icon variant |

### Decision Tree: Font vs Painter2D

```
Does the icon exist in Material Symbols codepoints file?
├── YES → Use MaterialSymbols constant + .m3-icon
└── NO → Is it a simple geometric shape (checkmark, dash, circle, line)?
    ├── YES → Use Painter2D in generateVisualContent (document in exception registry)
    └── NO → Request addition to codepoints or find closest alternative icon
```

**Components using Painter2D icons by exception (geometric only)**:
- `M3Checkbox` — checkmark (✓) and dash (—) are geometric shapes, not font icons
- `SDFRectElement` — shadow approximation, rounded rect fill, ripple circle

---

## 6. Animation Guidelines

### Decision Tree: USS Transition vs C# Scheduler

```
Is the animation a discrete property change (color, size, position, opacity)?
├── YES → Use USS `transition` in component's .uss file
└── NO — Is it a continuous expanding/contracting animation from a specific point?
    ├── YES → Use C# IVisualElementScheduler (schedule.Execute + MarkDirtyRepaint)
    └── NO → Use AnimationCurve + IVisualElementScheduler for custom easing
```

### USS Transition Animatable Properties (Unity 6.3)

✅ **Confirmed working**: `background-color`, `color`, `opacity`, `left`, `top`, `width`, `height`, `font-size`, `translate`, `scale`

❌ **Cannot transition**: CSS custom properties (`var(--m3-*)` values), computed layout properties, Painter2D content

### Transition Duration Tokens

**MANDATORY**: Always use `var(--m3-motion-*)` tokens, never hardcoded ms values.

```css
/* ✅ CORRECT */
transition: background-color var(--m3-motion-duration-short) ease-in-out;

/* ❌ WRONG */
transition: background-color 100ms ease-in-out;
```

| Token | Value | Use For |
|-------|-------|---------|
| `--m3-motion-duration-short` | 100ms | Checkbox, chip, radio, FAB quick feedback |
| `--m3-motion-duration-float` | 150ms | TextField floating label font-size |
| `--m3-motion-duration-standard` | 200ms | Toggle, navigation bar, most state changes |
| `--m3-motion-duration-long` | 300ms | Complex animations, ripple, future use |

### C# Animation Pattern (when USS is insufficient)

```csharp
// Use IVisualElementScheduler — NOT coroutines, NOT DOTween
private IVisualElementScheduledItem _animTimer;

private void StartAnimation()
{
    _elapsed = 0f;
    _animTimer?.Pause();
    _animTimer = schedule.Execute(Tick).Every(16).Until(() => _elapsed >= DurationMs);
}

private void Tick(TimerState ts)
{
    _elapsed += ts.deltaTime;
    float t = Mathf.Clamp01(_elapsed / DurationMs);
    float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out-cubic

    // Apply the animated value
    _myProperty = Mathf.Lerp(_start, _end, eased);
    MarkDirtyRepaint();
}
```

---

## 7. Typography Usage

### 15 M3 Type Roles

Apply by adding the USS class to a `Label` element:

```csharp
label.AddToClassList("m3-display-large");   // 57px, Regular
label.AddToClassList("m3-headline-medium"); // 28px, Regular
label.AddToClassList("m3-title-medium");    // 16px, Medium weight
label.AddToClassList("m3-body-large");      // 16px, Regular
label.AddToClassList("m3-label-small");     // 11px, Medium weight
```

Or in UXML:
```xml
<Label class="m3-title-large" text="Card Title" />
<Label class="m3-body-medium" text="Card body text goes here." />
```

### Role Selection Guide

| When to use | Role |
|-------------|------|
| Page title, hero text | Display Large/Medium/Small |
| Section headings | Headline Large/Medium/Small |
| Component titles, dialog headlines | Title Large |
| Field labels, chip labels, tab labels | Title Medium/Small |
| Body copy, description text | Body Large/Medium |
| Secondary body, caption | Body Small |
| Button labels, form labels | Label Large |
| Compact labels, badge counts | Label Medium/Small |

### Backward-Compatible Aliases (still valid)

| Old class | Maps to |
|-----------|---------|
| `.m3-display` | `.m3-display-large` |
| `.m3-headline` | `.m3-headline-large` |
| `.m3-title` | `.m3-title-large` |
| `.m3-body` | `.m3-body-large` |
| `.m3-label` | `.m3-label-large` |
| `.m3-caption` | `.m3-body-small` |

---

## 8. Performance Baseline — SDFRectElement Rendering

`SDFRectElement` uses CPU **Painter2D** for all shadow, overlay, and ripple rendering. This is the accepted architecture for this spec (see research.md R2 — GPU SDF Shader deferred).

### Accepted envelope

| Components with shadows | Expected performance |
|------------------------|---------------------|
| ≤ 30 elevated elements | No measurable frame budget impact |
| 31–50 elevated elements | Acceptable, monitor with Profiler |
| 50+ elevated elements | Profile and consider layout optimisation |

### Deferred work

A GPU SDF shader pass (URP custom render feature, per-element DrawMeshNow) was evaluated but deferred because per-element shader property blocks break UI Toolkit's native draw-call batching on mobile GPUs. The CPU Painter2D approach is safer across all target hardware until Unity exposes a batching-friendly custom mesh path for UI Toolkit elements.

Reference: `// TODO(006): GPU SDF Shader deferred — Painter2D accepted per research R2` comment in `SDFRectElement.cs`.

---

## 9. Exception Registry

All exceptions to the mandatory rules must be documented here.

| Rule | Exception | Component | Justification | Date Added |
|------|-----------|-----------|---------------|------------|
| USS-Only Theming | `SDFRectElement.StateOverlayOpacity` | All interactive | SDF-clipped overlay — cannot be CSS property | 2026-04-03 |
| USS-Only Theming | `SDFRectElement.TonalOverlayOpacity/Color` | Elevated components | GPU-rendered elevation tint layer | 2026-04-03 |
| USS-Only Theming | `SDFRectElement.RippleCenter/Radius/Alpha` | All interactive | Per-frame expanding circle animation | 2026-04-03 |
| USS-Only Theming | `SDFRectElement.FillColorOverride` | Card, Toggle | SDF fill rendered before CSS background-color | 2026-04-03 |
| USS-Only Theming | `_floatingLabel.style.backgroundColor` | M3TextField | Notch effect requires reading runtime surface color | 2026-04-03 |
| Material Symbols | Checkmark + Dash in M3Checkbox | M3Checkbox | Geometric shapes, not font icons | 2026-04-03 |
| GPU Shader | Painter2D CPU shadow rendering | SDFRectElement | Per-element shader properties break UI Toolkit batching on mobile GPUs | 2026-04-03 |
