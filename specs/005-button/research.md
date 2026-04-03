# Research: UISystem Button — WP-4 + WP-5 + WP-10 (partial)

**Branch**: `005-uisystem-button` | **Updated**: 2026-04-01
**Purpose**: Resolve technical decisions for StateLayerController, M3Button, and Editor tooling.

---

## R1: StateLayerController — C# Class vs MonoBehaviour

**Decision**: Plain C# class (not MonoBehaviour).

**Rationale**: StateLayerController manages VisualElement event callbacks — it has no need for Unity lifecycle methods. Being a plain class allows it to be constructed directly inside M3Button's constructor, keeping the component self-contained. MonoBehaviour would require a separate GameObject, complicate the component tree, and violate the "VisualElement-native" design.

**Implementation pattern**:
```csharp
public class StateLayerController
{
    private readonly VisualElement _target;
    private readonly SDFRectElement _sdfTarget; // null if target is not SDFRectElement
    private readonly RippleElement  _ripple;    // nullable
    private bool _disabled;

    public StateLayerController(VisualElement target, RippleElement ripple = null) { ... }
    public void Attach()   { /* RegisterCallback for all 6 event types */ }
    public void Detach()   { /* UnregisterCallback for all 6 event types */ }
    public bool Disabled   { get; set; }
}
```

**Lifecycle**: Parent component (M3Button) calls `Attach()` in its constructor; `Detach()` in a `Dispose()` or when detached from panel.

**Alternatives considered**:
- MonoBehaviour: Rejected — requires separate GameObject, incompatible with VisualElement-native design
- Extension method on VisualElement: Rejected — can't store state or a RippleElement reference cleanly

---

## R2: State Overlay — SDFRectElement.StateOverlayOpacity vs USS background

**Decision**: Use `SDFRectElement.StateOverlayOpacity` C# property (not USS `background-color` on a child overlay element).

**Rationale**: The state overlay must be clipped to the rounded rectangle boundary. A USS `background-color` on a child `VisualElement` would be a rectangle, not a rounded rect. `SDFRectElement.StateOverlayOpacity` feeds directly into the Painter2D render pass which already clips to the correct rounded shape. This is the only correct approach for M3-style state layers on rounded rects.

**API confirmed**:
```csharp
// SDFRectElement.cs
public float StateOverlayOpacity
{
    get => _stateOverlayOpacity;
    set { _stateOverlayOpacity = Mathf.Clamp01(value); MarkDirtyRepaint(); }
}
public Color StateOverlayColor { get; set; } // triggers MarkDirtyRepaint()
```

**M3 opacity values**:
- Idle: 0.0
- Hovered: 0.08
- Pressed: 0.10
- Focused: 0.10
- Disabled: element-level `opacity: 0.38` via `.m3-disabled` USS class (different mechanism)

**Alternatives considered**:
- Child VisualElement with USS background: Rejected — rectangular, not rounded
- USS `:hover`/`:active` pseudo-classes: Rejected — unreliable on mobile touch (no hover concept)

---

## R3: RippleElement Integration into M3Button

**Decision**: M3Button creates and owns a `RippleElement` instance, adds it as a child of the root `SDFRectElement`, and passes it to `StateLayerController`.

**API confirmed**:
```csharp
// RippleElement.cs — fixed timer bug in 004 fixes
public void StartRipple(Vector2 localPosition) // Vector2 in element's local space
public Color RippleColor { get; set; }         // default: Color.white
public float PeakOpacity { get; set; }         // default: 0.10f
```

**Ripple color**: For Filled buttons, ripple should use `StateOverlayColor` tinted with on-primary. Setting `RippleElement.RippleColor` to match the button variant's foreground color ensures the ripple is visible against the background.

**StateLayerController passes localPosition** from `PointerDownEvent.localPosition` directly to `RippleElement.StartRipple()`. The ripple element's coordinate space matches its parent `SDFRectElement`, so no coordinate transform needed.

---

## R4: M3Button UXML Registration

**Decision**: `[UxmlElement]` partial class pattern (same as SDFRectElement). UXML tag = `<mehmetsrl.UISystem.Components.M3Button>`.

**Namespace confirmed**: `mehmetsrl.UISystem.Components` per Constitution Principle III.

**UxmlAttributes**:
```csharp
[UxmlAttribute("text")]    public string Text { ... }
[UxmlAttribute("variant")] public ButtonVariant Variant { ... }
[UxmlAttribute("disabled")] public bool Disabled { ... }
```

**Note on `OnClick`**: C# event (`public event Action OnClick`) cannot be a `[UxmlAttribute]`. It is wired from C# code only.

**ButtonVariant enum** goes in `mehmetsrl.UISystem.Enums` namespace with explicit values.

---

## R5: USS Architecture for button.uss

**Decision**: Single `button.uss` file with 4 variant classes. No inline styles in M3Button C# code.

**Structure**:
```uss
/* Base button — shared across all variants */
.m3-button { min-height: 40px; ... }

/* Touch target wrapper adds padding to meet 48dp minimum */
.m3-button--filled { background-color: var(--m3-primary); ... }
.m3-button--outlined { background-color: transparent; border-width: 1px; ... }
.m3-button--text { background-color: transparent; ... }
.m3-button--tonal { background-color: var(--m3-secondary-container); ... }

/* Label inside button */
.m3-button__label--filled { color: var(--m3-on-primary); }
.m3-button__label--outlined { color: var(--m3-primary); }
.m3-button__label--text { color: var(--m3-primary); }
.m3-button__label--tonal { color: var(--m3-on-secondary-container); }
```

**M3Button applies USS classes programmatically** when `Variant` is set. No hardcoded color values in C#.

---

## R6: state-layer.uss Content

**Decision**: `state-layer.uss` defines only the `.m3-disabled` USS class. State overlay opacity is driven via C# (`StateOverlayOpacity`), not via USS properties.

**Content**:
```uss
/* .m3-disabled — applied by StateLayerController when Disabled = true */
.m3-disabled {
    opacity: 0.38;
}
/* .m3-hovered, .m3-pressed, .m3-focused — documentary only */
/* Actual overlay rendered by SDFRectElement.StateOverlayOpacity (C# property) */
```

**Rationale**: USS `opacity` applies to the entire element subtree, which is correct for disabled state. For hover/press/focus, the overlay is per-element Painter2D rendering that clips to the rounded rect — USS cannot replicate this.

---

## R7: Editor MenuItem — WP-10 Scope

**Decision**: Static `[MenuItem]` methods in `UISystemMenuItems.cs`. Creates a `.uxml` file via `AssetDatabase.CreateAsset` + `File.WriteAllText`.

**Pattern**:
```csharp
[MenuItem("Assets/Create/UISystem/Button (Filled)")]
public static void CreateFilledButton()
{
    string path = GetSelectedFolderPath() + "/NewFilledButton.uxml";
    File.WriteAllText(path, GenerateButtonUxml(ButtonVariant.Filled));
    AssetDatabase.Refresh();
}
```

**Generated UXML** contains a minimal `<ui:UXML>` with a single `<mehmetsrl.UISystem.Components.M3Button variant="filled" text="Button" />`.

---

## R8: Assembly — No New References Needed

**Decision**: No changes to asmdef files required.

**Confirmed**:
- `mehmetsrl.UISystem.asmdef` already has `Unity.TextMeshPro` + `Sirenix.OdinInspector.Attributes.dll`
- `M3Button`, `StateLayerController`, `ButtonVariant` all live in the same runtime assembly
- Editor menu items live in `mehmetsrl.UISystem.Editor.asmdef` (already references runtime)
- Zero new external dependencies introduced

---

## R9: Constitution Compliance Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Zero Dependencies | ✅ PASS | No new `mehmetsrl.*` references; TMP + Odin already present |
| II. SO Configuration | ✅ PASS | No hardcoded colors in C#; all colors via `var(--m3-*)` in USS |
| III. Unity Conventions | ✅ PASS | `mehmetsrl.UISystem.Components` for M3Button; `mehmetsrl.UISystem.Core` for StateLayerController |
| IV. Mobile-First | ✅ PASS | C# pointer events (not CSS pseudo-classes); no per-element material overrides; PanelSettings unchanged |
| V. Incremental Delivery | ✅ PASS | ButtonDemo scene is the independently verifiable deliverable |

**One deviation to document**: Constitution IV mentions "every shader feature must be independently toggleable" — not applicable because we use Painter2D, not Shader Graph. This is the accepted Painter2D deviation from 004.
