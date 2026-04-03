# Research: UISystem Foundation Layer (UI Toolkit)

**Branch**: `004-uisystem-foundation` | **Updated**: 2026-04-01
**Purpose**: Resolve technical uncertainties for UI Toolkit + URP Shader Graph implementation.

---

## R1: URP UI Shader Graph in Unity 6.3

**Decision**: Use Unity 6.3's built-in **UI Shader Graph** material type (URP).

**How it works**: Unity 6.3 added a dedicated UI material type to Shader Graph for URP. A Shader Graph with "Material" → "UI" target renders into the UI Toolkit pipeline with correct depth, blending, and masking. The graph replaces the old hand-written `UI/Default` HLSL fork entirely.

**Key nodes available**:
- `Screen Position`, `UV`, `Vertex Color` — standard inputs
- Custom Function node — wraps any HLSL including Inigo Quilez `sdRoundedBox`
- `Sample Texture 2D`, `Lerp`, `Smoothstep`, `Step` — all standard
- `Time` — for ripple animation

**Masking / Clipping**: UI Shader Graph automatically integrates with UI Toolkit's GPU-based clip rects. No manual stencil buffer code needed.

**Limitations discovered**:
- Per-element material property overrides (`SetMaterialDirty` etc.) break UI Toolkit's built-in batching. Need to evaluate via benchmarking.
- Unity's UI Toolkit batching is internal and opaque — we cannot see draw call breakdown the same way as Canvas Frame Debugger.

**Rationale**: Shader Graph removes the need to maintain hand-written HLSL, supports visual debugging in the graph editor, and is the forward-compatible path for Unity 6+.

---

## R2: Per-Element Visual Parameters — USS Custom Properties

**Decision**: Visual parameters (corner radius, shadow, outline) are communicated to the Shader Graph via **USS custom properties** on the `SDFRectElement`, NOT via UV channel packing.

**How it works**:
- The `SDFRectElement` custom VisualElement exposes USS custom properties (`--corner-radius`, `--shadow-blur`, etc.).
- In the `generateVisualContent` callback or via USS, these values are read and passed to the material as **per-element material properties** using `MeshGenerationContext.GetMeshDataForCurrentElement()` or similar Unity 6.3 APIs.

**Batching trade-off**:
- If each `SDFRectElement` instance uses a different material property value, UI Toolkit may not batch them (same behavior as Canvas).
- **Mitigation**: For components that share the same visual profile (e.g., all Filled Buttons), the same USS property values → same material batch.
- The WP-1 SCOPE already acknowledges this: "UI Toolkit handles batching internally; evaluate alternative approaches if batching breaks."

**Alternative: `generateVisualContent` for pure CPU drawing**:
- `Painter2D` in `generateVisualContent` draws rounded rectangles, soft shadows (layered concentric fills), and outlines entirely in software.
- **Accepted as the implementation approach**: Painter2D eliminates all Shader Graph complexity, works on all platforms, and produces visually acceptable results for M3-style cards and buttons. Shadow quality difference vs GPU SDF is acceptable for the target use case.

**Decision**: Painter2D CPU drawing via `generateVisualContent`. No Shader Graph material needed. T014 (Shader Graph) was superseded — `SDFRect.shadergraph` was not created. Corner radius, shadow, and outline are all Painter2D operations.

---

## R3: SDFRectElement — VisualElement + Shader Graph Integration

**Decision**: `SDFRectElement` is a custom `VisualElement` using `generateVisualContent` + a `Material` reference to the Shader Graph material.

**Implementation pattern**:
```csharp
[UxmlElement]
public partial class SDFRectElement : VisualElement
{
    [UxmlAttribute] public float CornerRadius { get; set; } = 12f;
    [UxmlAttribute] public float ShadowBlur   { get; set; } = 0f;
    // ...

    private Material _material; // reference to SDFRect.mat (Shader Graph instance)

    public SDFRectElement()
    {
        generateVisualContent += OnGenerateVisualContent;
    }

    void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        // Use ctx.painter (Painter2D) or ctx.GetMeshDataForCurrentElement()
        // to draw a quad and set material properties
        _material.SetFloat("_CornerRadius", CornerRadius);
        _material.SetFloat("_ShadowBlur", ShadowBlur);
        // Draw rect via MeshGenerationContext
        var rect = contentRect;
        ctx.DrawRectangle(new RectangleParams { ... material = _material });
    }
}
```

**Note on `generateVisualContent`**: This approach works but materializes a new mesh each repaint. For static elements, this is fine. For animated ripple, `MarkDirtyRepaint()` triggers re-generation each frame.

**Rationale**: `generateVisualContent` is the documented Unity 6+ approach for custom VisualElement rendering with materials.

---

## R4: ThemeManager — SO → USS Variables Sync

**Decision**: `ThemeManager` syncs `ThemeData` SO values to USS custom properties on the **root VisualElement** of all active `UIDocument` panels at runtime.

**How USS custom properties cascade**:
- Setting a custom property on `:root` (or the panel root) cascades to all child VisualElements via normal CSS variable inheritance.
- USS: `background-color: var(--m3-primary);` in any child element resolves to the value set on the root.
- Unity confirms: USS custom properties cascade exactly like CSS custom properties (confirmed in Unity 6 docs).

**Sync mechanism**:
```csharp
void SyncToPanel(UIDocument doc)
{
    var root = doc.rootVisualElement;
    root.style.SetCustomProperty("--m3-primary", ColorToString(_activeTheme.Colors.Primary));
    root.style.SetCustomProperty("--m3-surface", ColorToString(_activeTheme.Colors.Surface));
    // ... all 17 color roles
    // ... elevation variables
    // ... shape variables
}
```

**Performance**: Setting ~30 custom properties once per theme change (not per frame) is negligible. USS re-compute is triggered on the panel, similar to class change.

**For animated theme transitions**: Not supported in V1. Theme switch is instant — swap SO, re-sync all USS variables, UI Toolkit re-applies styles in the same frame.

---

## R5: Typography — UI Toolkit + TMP Font Assets

**Decision**: Use `-unity-font-definition` USS property to reference TMP font assets per typography role.

**How it works in Unity 6.3**:
- UI Toolkit's `Label`, `TextField`, etc. support TMP font assets via `-unity-font-definition: url("path/to/FontAsset.asset")` in USS.
- Each `.m3-*` USS class sets the correct font asset and size.
- There is NO font rendering path conflict — UI Toolkit in Unity 6.3 uses TMP rendering internally for text.

**USS example**:
```css
.m3-title {
    font-size: 22px;
    -unity-font-definition: url("project://database/Assets/UISystem/Assets/Typography/Fonts/Roboto-Medium SDF.asset?...");
    -unity-font-style: bold;
}
```

**Font asset references**: In USS, font assets must be referenced by their project database URL. This is cumbersome to write by hand — the `TypographyResolver` C# component can apply the class name programmatically, avoiding manual USS path management.

**Weight strategy**: Same as before — separate TMP font asset per weight (Regular 400, Medium 500). No SDF dilation.

---

## R6: PanelSettings Scale Mode

**Decision**: `PanelSettings` with **Scale With Screen Size**, reference 1080×1920, `Match` = 0.5 (horizontal + vertical average).

**Confirmed behavior**:
- `PanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize`
- `PanelSettings.referenceResolution = new Vector2Int(1080, 1920)`
- `PanelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight`
- `PanelSettings.match = 0.5f`
- All UI Toolkit length values in USS pixels scale automatically — exactly like Canvas Scaler in uGUI.

**Screen.dpi**: Not used. PanelSettings handles physical pixel adaptation.

---

## R7: USS :hover on Mobile (Touch)

**Decision**: Do NOT rely on `:hover` for mobile interaction state. Use C# `PointerEnterEvent` / `PointerLeaveEvent` for hover, and `:active` (`:focus`) for press state.

**Why**: On touch devices, `:hover` may fire inconsistently (on pointer-down in some Unity versions, or not at all). Using explicit event handlers gives deterministic behavior.

**For state layer implementation (WP-4)**:
- `RegisterCallback<PointerEnterEvent>` → add `.m3-hovered` class
- `RegisterCallback<PointerLeaveEvent>` → remove `.m3-hovered` class
- `RegisterCallback<PointerDownEvent>` → add `.m3-pressed` class
- `RegisterCallback<PointerUpEvent>` → remove `.m3-pressed` class
- USS transitions animate `background-color` changes on class changes.

---

## R8: USS Transitions

**Decision**: USS `transition` property works for `opacity`, `background-color`, `translate`, `scale` in UI Toolkit. Custom properties (`var(--m3-*)`) themselves are NOT directly transitionable.

**Confirmed**: Unity UI Toolkit supports CSS transitions for standard style properties. Duration and easing use values defined in `ThemeData` motion presets (read by `ThemeManager` when setting up USS variables).

**Ripple animation**: Cannot be done purely in USS (requires animated radius). Will use a `RippleElement` custom VisualElement with `generateVisualContent` + `schedule.Execute(...).Every(16ms)` for frame updates, calling `MarkDirtyRepaint()`.

---

## R9: Disabled State in UI Toolkit

**Decision**: Use `SetEnabled(false)` on the VisualElement. This applies the `:disabled` pseudo-class automatically in USS.

**M3 disabled rules**: Container at 12% opacity, content (text/icon) at 38% opacity. Implemented via:
```css
.m3-button:disabled {
    opacity: 0.38;
}
.m3-button:disabled > .m3-button-container {
    opacity: 0.12;
}
```

**WP-4 concern**: This is addressed in the State Layer Controller. Noted here for reference.

---

## Summary: All Decisions Resolved

| Topic | Decision |
|-------|----------|
| Shader type | URP UI Shader Graph (Unity 6.3) |
| Per-element params | USS custom properties → material properties |
| VisualElement rendering | `generateVisualContent` + Material |
| Batching | Accept potential extra draw calls; benchmark on device |
| ThemeManager sync | SO → USS custom properties on panel root |
| USS cascading | Confirmed working from root → all children |
| Typography | `-unity-font-definition` + TMP font assets in USS |
| PanelSettings | Scale With Screen Size, 1080×1920, match=0.5 |
| :hover on mobile | C# pointer events instead (PointerEnterEvent/PointerLeaveEvent) |
| USS transitions | Supported for standard properties; ripple via MarkDirtyRepaint |
| Disabled state | `SetEnabled(false)` → `:disabled` pseudo-class → USS |
| Shadow performance | Opt-in Shader Graph keyword; profile on Mali G52/G57 |
