# Implementation Plan: UISystem Foundation Layer (UI Toolkit)

**Branch**: `004-uisystem-foundation` | **Date**: 2026-04-01 | **Spec**: `specs/004-uisystem-foundation/spec.md`
**Input**: Feature specification — WP-1 (UI Shader Graph) + WP-2 (Theme System) + WP-3 (Typography)

## Summary

Build the visual and theming foundation for UISystem using Unity 6.3's UI Toolkit (UXML + USS + C#)
and URP Shader Graph. Three deliverables: a custom `SDFRectElement` VisualElement backed by a Shader
Graph material for SDF rendering; a `ThemeManager` that syncs `ThemeData` ScriptableObject values
to USS custom properties for zero-code theming; and a `typography.uss` stylesheet with TMP font assets
for M3-inspired type roles. All three are demonstrated in a Foundation sample scene.

## Technical Context

**Language/Version**: C# (Unity 6.3, 6000.3)
**Primary Dependencies**: UI Toolkit (built-in), URP (Universal Render Pipeline), TextMeshPro (Unity package), Odin Inspector (Sirenix)
**Storage**: ScriptableObject assets on disk; USS stylesheets; no runtime database
**Testing**: Manual verification in Unity Editor + Play mode; Unity_GetConsoleLogs via MCP
**Target Platform**: Mobile (Android/iOS), secondary desktop
**Project Type**: Unity library (submodule, UPM-compatible)
**Performance Goals**: Minimal draw calls (UI Toolkit internal batching); smooth USS transitions at 60fps
**Constraints**: Zero dependencies on Infrastructural framework; URP required
**Scale/Scope**: 3 foundation systems; 6 typography roles; 17 color roles; 6 elevation levels

## Constitution Check

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Zero Dependencies | ✅ PASS | No references to mehmetsrl.Presenter, Bindings, or any Infrastructural assembly |
| II. ScriptableObject Config | ✅ PASS | ThemeData and TypographyConfig are SOs; no hardcoded constants |
| III. Unity Conventions | ✅ PASS | mehmetsrl.UISystem namespace, Odin attributes, Runtime/Editor/Styles/UXML layout |
| IV. Mobile-First Performance | ✅ PASS | PanelSettings scale, USS transitions, Shader Graph keyword toggles for shadow/ripple |
| V. Incremental Delivery | ✅ PASS | Foundation sample scene is the independent verifiable deliverable for phase 004 |

**Complexity Tracking**: No constitution violations.

## Project Structure

```
Assets/UISystem/
├── Runtime/
│   ├── Shaders/
│   │   └── SDFRect.shadergraph          ← WP-1
│   ├── Materials/
│   │   └── SDFRect.mat                  ← WP-1 (default material instance)
│   ├── ScriptableObjects/
│   │   ├── ThemeData.cs                 ← WP-2
│   │   └── TypographyConfig.cs          ← WP-3
│   ├── Core/
│   │   ├── ThemeManager.cs              ← WP-2
│   │   ├── SDFRectElement.cs            ← WP-1
│   │   ├── RippleElement.cs             ← WP-1
│   │   └── TypographyResolver.cs        ← WP-3
│   ├── Enums/
│   │   ├── TextRole.cs                  ← WP-3
│   │   ├── ColorRole.cs                 ← WP-2
│   │   └── MotionPresetType.cs          ← WP-2
│   └── Data/
│       ├── ElevationPreset.cs           ← WP-2
│       ├── ShapePresets.cs              ← WP-2
│       ├── MotionPreset.cs              ← WP-2
│       ├── ColorPalette.cs              ← WP-2
│       └── TextStyle.cs                 ← WP-3
├── Styles/
│   ├── Themes/
│   │   ├── light.uss                    ← WP-2 (CSS source of truth)
│   │   └── dark.uss                     ← WP-2
│   ├── typography.uss                   ← WP-3
│   └── state-layer.uss                  ← WP-4 placeholder (empty at Foundation)
├── UXML/
│   └── FoundationDemo.uxml              ← US4 (sample scene UI)
├── Editor/
│   ├── Inspectors/
│   │   └── ThemeDataEditor.cs           ← WP-2 (color swatches)
│   └── mehmetsrl.UISystem.Editor.asmdef
├── Assets/
│   ├── Themes/
│   │   ├── DefaultLight.asset           ← WP-2
│   │   └── DefaultDark.asset            ← WP-2
│   ├── Typography/
│   │   ├── DefaultTypography.asset      ← WP-3
│   │   └── Fonts/
│   │       ├── Roboto-Regular SDF.asset ← WP-3
│   │       └── Roboto-Medium SDF.asset  ← WP-3
│   └── PanelSettings/
│       └── DefaultPanelSettings.asset   ← US4
├── Samples~/
│   └── Foundation/
│       ├── FoundationDemo.unity         ← US4
│       └── FoundationDemo.uxml          ← US4
├── mehmetsrl.UISystem.asmdef
├── package.json
├── README.md
└── SCOPE.md
```

**Structure Decision**: Unity UI Toolkit layout — `Runtime/` for C# + Shader Graph, `Styles/` for USS,
`UXML/` for templates, `Editor/` for editor-only code, `Assets/` for ScriptableObject instances.
Follows UPM package conventions (package.json, Samples~/).

## Phase 0 — Research (Complete)

All open questions from SCOPE.md resolved in `research.md`. Summary:

| Question | Answer |
|----------|--------|
| Shader Graph UI node capabilities | URP UI Shader Graph target available in Unity 6.3; Custom Function node wraps Quilez SDF |
| Batching with per-element material overrides | Per-element different values may break batching; accept and profile on device |
| generateVisualContent vs material approach | Use `generateVisualContent` + `Material` in `MeshGenerationContext` |
| Fill rate on mobile | Shadow opt-in via Shader Graph keyword; profile on Mali G52/G57 |
| USS custom property performance | ~30 properties on theme change (not per frame) — negligible |
| USS cascading from :root | Confirmed working like CSS; root → all children |
| PanelSettings scale mode | Scale With Screen Size 1080×1920 match=0.5 — confirmed |
| TMP font in UI Toolkit | `-unity-font-definition` in USS; TMP rendering path confirmed in Unity 6.3 |
| :hover on mobile | Don't use; use C# PointerEnterEvent/PointerLeaveEvent instead |
| USS transitions | Supported for standard properties; ripple via MarkDirtyRepaint() |

## Phase 1 — Design (Complete)

- `data-model.md` — SDFRectElement, ThemeData, TypographyConfig entities with full field definitions
- `contracts/public-api.md` — ThemeManager, SDFRectElement, USS classes, USS variables public contract
- `quickstart.md` — Step-by-step setup guide: PanelSettings → ThemeManager → SDFRectElement → Typography

## Implementation Strategy

### WP-1: UI Shader Graph + SDFRectElement

1. Create `SDFRect.shadergraph` (URP UI Shader Graph type)
   - Custom Function node for Quilez `sdRoundedBox` HLSL
   - Shader Graph properties: `_CornerRadius`, `_ShadowBlur`, `_ShadowOffset`, `_ShadowColor`, `_OutlineThickness`, `_OutlineColor`, `_StateOverlayOpacity`, `_StateOverlayColor`, `_RippleCenter`, `_RippleRadius`, `_RippleAlpha`
   - Keywords: `SHADOW_ENABLED`, `OUTLINE_ENABLED`, `RIPPLE_ENABLED` for mobile optimization
2. Create `SDFRect.mat` default material instance from the Shader Graph
3. Implement `SDFRectElement.cs`:
   - `[UxmlElement]` registration
   - `generateVisualContent` override → draw quad with Shader Graph material
   - C# properties map to `_material.SetFloat/SetColor` calls
   - `MarkDirtyRepaint()` on property change
4. Implement `RippleElement.cs` (sub-element for ripple animation, driven by `schedule.Execute`)

### WP-2: ThemeManager + ThemeData + USS Sync

1. Implement data structs: `ColorPalette`, `ElevationPreset`, `ShapePresets`, `MotionPreset`
2. Implement `ThemeData.cs` ScriptableObject with Odin Inspector attributes
3. Implement `ThemeManager.cs`:
   - Singleton pattern + `DontDestroyOnLoad`
   - `SyncToPanel(UIDocument)` iterates all `--m3-*` variables and sets them on panel root
   - `SetTheme(ThemeData)` → sync + fire `OnThemeChanged`
   - `ToggleLightDark()` convenience method
4. Implement `ThemeDataEditor.cs` (Editor) — custom inspector with color swatch grid
5. Create `light.uss` and `dark.uss` with all `--m3-*` variable definitions
6. Create `DefaultLight.asset` and `DefaultDark.asset` with M3 baseline colors

### WP-3: Typography USS + Assets

1. Create `typography.uss` with all 6 `.m3-*` classes
   - `font-size`, `-unity-font-definition`, `-unity-font-style`
2. Import Roboto-Regular.ttf + Roboto-Medium.ttf → generate TMP SDF font assets
   - 44pt sampling, 5px padding, 512×512, Unicode 20-7E, A0-FF, 100-17F
3. Create `DefaultTypography.asset` referencing the generated font assets
4. Implement `TypographyResolver.cs` (optional helper MonoBehaviour)
5. Implement `TextStyle.cs` struct, `TextRole.cs` enum, `TypographyConfig.cs` SO

### US4: Foundation Sample Scene

1. Create `DefaultPanelSettings.asset` with correct Scale With Screen Size config
2. Build `FoundationDemo.uxml` — 2-column layout:
   - Left: Cards section (3 card variants using SDFRectElement)
   - Right: Typography scale + Elevation demo + Switch Theme button
3. Build `FoundationDemo.unity`:
   - `[UISystem]` GameObject with `ThemeManager`
   - `UIDocument` with `DefaultPanelSettings` + `FoundationDemo.uxml` as source
4. Wire "Switch Theme" button → `ThemeManager.ToggleLightDark()`

## Verification

1. `Unity_GetConsoleLogs` (error, warning) — zero shader errors, zero NullReference
2. Enter Play mode → `ScreenCapture.CaptureScreenshot` → review:
   - SDFRectElement renders with rounded corners + soft shadow
   - Typography roles display at correct sizes
   - 3 card variants visually distinct
3. Click "Switch Theme" → screenshot → verify dark theme colors applied
4. Check GPU profiler (or UI Toolkit Debugger) for draw call count
