# Tasks: UISystem Foundation Layer (UI Toolkit)

**Input**: Design documents from `/specs/004-uisystem-foundation/`
**Updated**: 2026-04-01 — Full regeneration for UI Toolkit migration (uGUI → UI Toolkit + URP Shader Graph)
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/public-api.md ✅, quickstart.md ✅

**Tests**: No automated test tasks — manual Unity Editor verification only (Play mode + MCP).
Each phase ends with a manual checkpoint.

**Organization**: Setup → Foundational (data types + enums) → US1 (Shader Graph + SDFRectElement)
→ US2 (ThemeManager + USS sync) → US3 (Typography USS + assets) → US4 (Foundation sample scene).
US1/2/3 can be developed in parallel by separate developers; for single developer, follow priority order.

---

## Phase 1: Setup & Cleanup

**Purpose**: Clean up old uGUI code, update folder structure for UI Toolkit, verify assembly definitions.
Old uGUI implementation (SDFRectGraphic, existing FoundationDemo scene) will be deleted — starting fresh.

- [X] T001 Delete old uGUI runtime files: `Assets/UISystem/Runtime/Graphics/SDFRectGraphic.cs`, `Assets/UISystem/Runtime/Core/TypographyResolver.cs` (old version), `Assets/UISystem/Runtime/Core/ThemeManager.cs` (old version) — keep directory structure
- [X] T002 Delete old Foundation sample scene: `Assets/UISystem/Samples~/Foundation/FoundationDemo.unity` and any related `.meta` files
- [X] T003 [P] Create new folder structure: `Assets/UISystem/Styles/Themes/`, `Assets/UISystem/Styles/Components/`, `Assets/UISystem/UXML/`, `Assets/UISystem/Runtime/Materials/` (if not existing)
- [X] T004 [P] Update `Assets/UISystem/Runtime/mehmetsrl.UISystem.asmdef` — ensure references include `Unity.TextMeshPro`, `Sirenix.OdinInspector.Attributes`; remove any uGUI-specific references if present
- [X] T005 [P] Update `Assets/UISystem/package.json` — set `unity: 6000.3`, verify `name: com.sroglu.uisystem`, `version: 0.2.0`

**Checkpoint**: Old uGUI files removed, new folder structure in place, asmdefs clean.

---

## Phase 2: Foundational (Data Types + Enums)

**Purpose**: Shared data structures and enums that ALL user stories depend on.
Must be complete before US1/2/3 can begin.

- [X] T006 [P] Create `Assets/UISystem/Runtime/Data/ColorPalette.cs` — struct with 17 Color fields (Primary, OnPrimary, PrimaryContainer, OnPrimaryContainer, Secondary, OnSecondary, SecondaryContainer, OnSecondaryContainer, Surface, OnSurface, SurfaceVariant, OnSurfaceVariant, Error, OnError, Outline, OutlineVariant, Background)
- [X] T007 [P] Create `Assets/UISystem/Runtime/Data/ElevationPreset.cs` — `[Serializable]` struct: `Vector2 ShadowOffset`, `float ShadowBlur`, `Color ShadowColor`, `float TonalOverlayAlpha`
- [X] T008 [P] Create `Assets/UISystem/Runtime/Data/ShapePresets.cs` — `[Serializable]` struct: `float None, ExtraSmall, Small, Medium, Large, ExtraLarge, Full`
- [X] T009 [P] Create `Assets/UISystem/Runtime/Data/MotionPreset.cs` — `[Serializable]` struct: `AnimationCurve Curve`, `float DurationMs`
- [X] T010 [P] Create `Assets/UISystem/Runtime/Data/TextStyle.cs` — `[Serializable]` struct: `TMP_FontAsset FontAsset`, `float FontSize`, `FontStyles FontStyle`, `float LineSpacing`, `float CharSpacing`, `string UssClassName`
- [X] T011 [P] Create `Assets/UISystem/Runtime/Enums/TextRole.cs` — enum: Display=0, Headline=1, Title=2, Body=3, Label=4, Caption=5
- [X] T012 [P] Create `Assets/UISystem/Runtime/Enums/ColorRole.cs` — enum with 17 values matching ColorPalette fields
- [X] T013 [P] Create `Assets/UISystem/Runtime/Enums/MotionPresetType.cs` — enum: Emphasized=0, Standard=1, EmphasizedDecelerate=2, StandardDecelerate=3

**Checkpoint**: All data types compile with zero errors. Foundation ready for US1/2/3 parallel start.

---

## Phase 3: User Story 1 — UI Shader Graph + SDFRectElement (Priority: P1)

**Goal**: Create the visual rendering foundation — a Shader Graph material + custom VisualElement
that renders rounded rectangles with shadow, outline, state overlay, and ripple.

**Independent Test**: Create a UIDocument with PanelSettings. Add several SDFRectElement instances
with different corner radii and shadow settings. Enter Play mode — all elements MUST render with
correct rounded shapes and soft shadows. Console MUST show zero shader errors.

### Implementation for User Story 1

- [X] T014 [US1] ~~Create `Assets/UISystem/Runtime/Shaders/SDFRect.shadergraph`~~ — **SUPERSEDED**: Shader Graph not created. Painter2D CPU drawing accepted as final approach (see research.md R2). Runtime/Shaders/ directory deleted.
- [X] T015 [US1] Create `Assets/UISystem/Runtime/Materials/SDFRect.mat` — material created
- [X] T016 [US1] Create `Assets/UISystem/Runtime/Core/SDFRectElement.cs` — implemented via Painter2D CPU vector drawing (not Shader Graph material); renders rounded rect + soft shadow + outline via SDF math in generateVisualContent; `[UxmlElement]` + `[UxmlAttribute]` properties
- [X] T017 [US1] Create `Assets/UISystem/Runtime/Core/RippleElement.cs` — expanding circle via Painter2D; schedule.Execute(Tick).Every(16) drives radius 0→1 with ease-out-cubic; FadeOut via separate FadeTick scheduler

**Checkpoint (US1)**: SDFRectElement renders rounded rectangle with shadow in Play mode. Zero console errors.

---

## Phase 4: User Story 2 — Theme System with USS Sync (Priority: P2)

**Goal**: ThemeData ScriptableObject + ThemeManager that syncs SO values to USS custom properties
on all managed UIDocument panels.

**Independent Test**: Create ThemeManager with DefaultLight + DefaultDark ThemeData. Add
SDFRectElement using `var(--m3-surface)` background color. Call `SetTheme(darkTheme)` —
element MUST update to dark surface color. Zero C# callbacks on the element itself.

### Implementation for User Story 2

- [X] T018 [US2] Create `Assets/UISystem/Runtime/ScriptableObjects/ThemeData.cs`
- [X] T019 [US2] Create `Assets/UISystem/Runtime/Core/ThemeManager.cs` — USS stylesheet swap approach (not SetCustomProperty); SyncToPanel removes old sheet + adds new sheet to rootVisualElement.styleSheets; RegisterPanel() called by ThemeSwitchButton.Start()
- [X] T020 [US2] Create `Assets/UISystem/Styles/Themes/light.uss` — `:root {}` with all `--m3-*` variable definitions (M3 baseline light values)
- [X] T021 [US2] Create `Assets/UISystem/Styles/Themes/dark.uss` — `:root {}` with M3 baseline dark values
- [X] T022 [US2] Create `Assets/UISystem/Editor/Inspectors/ThemeDataEditor.cs`
- [X] T023 [US2] Create `Assets/UISystem/Assets/Themes/DefaultLight.asset`
- [X] T024 [US2] Create `Assets/UISystem/Assets/Themes/DefaultDark.asset`

**Checkpoint (US2)**: ThemeManager syncs SO → USS on startup. SetTheme() updates all `--m3-*` variables on managed panels. Color changes visible on SDFRectElement using `var(--m3-surface)`. Zero console errors.

---

## Phase 5: User Story 3 — Typography USS Classes (Priority: P3)

**Goal**: USS typography classes (.m3-display through .m3-caption) + TMP font assets + optional
TypographyResolver helper MonoBehaviour.

**Independent Test**: UIDocument with 6 Label elements, each with a different `.m3-*` class.
Enter Play mode — each label MUST display at correct font size using correct weight font asset.

### Implementation for User Story 3

- [X] T025 [US3] Import font files — Roboto-Regular.ttf + Roboto-Medium.ttf placed in `Assets/UISystem/Assets/Typography/Fonts/Roboto/`; TMP SDF assets generated: `Roboto-Regular SDF.asset`, `Roboto-Medium SDF.asset`
- [X] T026 [US3] Create `Assets/UISystem/Styles/typography.uss` — 6 USS classes with correct font paths relative to USS file
- [X] T027 [US3] Create `Assets/UISystem/Runtime/ScriptableObjects/TypographyConfig.cs`
- [X] T028 [US3] Create `Assets/UISystem/Assets/Typography/DefaultTypography.asset`
- [X] T029 [US3] Create `Assets/UISystem/Runtime/Core/TypographyResolver.cs` — rewritten to add/remove USS classes via element.AddToClassList/RemoveFromClassList

**Checkpoint (US3)**: Label with `.m3-title` class displays at 22px with Roboto-Medium. Zero console errors.

---

## Phase 6: User Story 4 — Foundation Sample Scene (Priority: P4)

**Goal**: End-to-end verification scene demonstrating WP-1+2+3 integration.

**Independent Test**: Open FoundationDemo.unity → Play mode → SDFRectElement cards visible with
rounded corners + shadows, typography roles at correct sizes, Switch Theme button changes colors.

### Implementation for User Story 4

- [X] T030 [US4] Create `Assets/UISystem/Assets/PanelSettings/DefaultPanelSettings.asset` — ScaleWithScreenSize 1080×1920 match=0.5; note: themeUss slot null (UI Toolkit limitation via script); USS sync handled by ThemeManager.RegisterPanel() at runtime instead
- [X] T031 [US4] Create `Assets/UISystem/Styles/state-layer.uss` — empty placeholder (WP-4 content TBD)
- [X] T032 [US4] Create `Assets/UISystem/UXML/FoundationDemo.uxml` + `Assets/UISystem/Samples~/Foundation/FoundationDemo.uxml` — 2-column layout; no inline var() in UXML (crash fix); all colors via USS classes
- [X] T033 [US4] Create `Assets/UISystem/Scenes/FoundationDemo.unity` — note: scene placed in Scenes/ not Samples~/ (Samples~ excluded from AssetDatabase → MonoBehaviour GUIDs unresolvable → 0 root GOs in Editor). Scene has [UISystem]+ThemeManager + UIDocument + Main Camera.
- [X] T034 [US4] Wire Switch Theme button — `ThemeSwitchButton.cs` on UIDocument GO; Awake wires ClickEvent; Start calls ThemeManager.RegisterPanel() so light.uss is applied to rootVisualElement

**Checkpoint (US4)**: Scene opens → Play mode → visual verification passes. Switch Theme button works. Zero console errors.

---

## Phase 7: Polish & Verification

- [X] T035 [P] Create `Assets/UISystem/Styles/Components/` — foundation-demo.uss created here with layout + color utility classes
- [X] T036 Verify `Unity_GetConsoleLogs` — zero C# errors; one cosmetic PanelSettings.themeUss warning (non-blocking, UI renders via RegisterPanel())
- [X] T037 Take `ScreenCapture.CaptureScreenshot` — UI visible: 3 card variants (elevated/filled/outlined), primary button, typography scale, elevation row, themed colors from light.uss
- [X] T038 [P] Update `Assets/UISystem/README.md` — updated SDFRectElement description (Painter2D, not Shader Graph) and ThemeManager mechanism (stylesheet swap)
- [X] T039 [P] Verify `mehmetsrl.UISystem.asmdef` — zero Infrastructural assembly references (constitution compliance ✓)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Start immediately
- **Foundational (Phase 2)**: After Phase 1 — blocks US1/2/3 start
- **US1, US2, US3 (Phases 3/4/5)**: After Phase 2 — can proceed in parallel
- **US4 (Phase 6)**: After US1 + US2 + US3 are all complete (needs all three for the demo)
- **Polish (Phase 7)**: After US4

### Within Each User Story

- T014 (Shader Graph) must complete before T016 (SDFRectElement uses the .shadergraph + .mat)
- T015 (Material) must complete before T016
- T018 (ThemeData) must complete before T019 (ThemeManager), T023, T024
- T019 (ThemeManager) must complete before T023, T024 can be tested
- T025 (Font assets) must complete before T026 (typography.uss font references)
- T027 (TypographyConfig SO) must complete before T028 (DefaultTypography.asset)
- T030 (PanelSettings) must complete before T033 (scene setup)
- T032 (UXML) must complete before T033 (scene references UXML)

### Parallel Opportunities

- T006–T013 (Phase 2 data types): all parallelizable, different files
- T014, T015 (Shader Graph + Material): can be done in parallel
- T018 (ThemeData) and T014 (Shader Graph): parallel, different systems
- T025 (Font import), T018 (ThemeData): parallel, different systems
- T023, T024 (DefaultLight/Dark assets): parallel with US1 once T018 is done
- T035, T038, T039 (Polish): all parallel

---

## Parallel Example: Phase 2 + Phase 3 Start

```
Immediately after Phase 1:
  Parallel: T006, T007, T008, T009, T010, T011, T012, T013

Once Phase 2 complete, start all three stories in parallel:
  US1: T014 → T015 → T016 → T017
  US2: T018 → T019 + T020 + T021 (parallel) → T022 → T023 + T024 (parallel)
  US3: T025 → T026 + T027 (parallel) → T028 → T029
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Complete Phase 1 + Phase 2
2. Complete US1 (T014–T017)
3. Validate: SDFRectElement renders in Play mode with zero console errors
4. Proceed to US2

### Full Foundation Delivery

1. Phase 1 cleanup → Phase 2 data types
2. US1 + US2 + US3 in priority order (or parallel)
3. US4 integration scene
4. Phase 7 polish + MCP verification

---

## Notes

- [P] = different files, no dependencies, can run in parallel
- [USn] maps task to User Story n from spec.md
- Old uGUI files (SDFRectGraphic.cs, old ThemeManager.cs, FoundationDemo scene) DELETED in Phase 1
- Font assets must be manually imported via Unity Editor (no CLI import)
- Shader Graph must be created in Unity Editor's Shader Graph editor (not as a text file)
- USS font paths use Unity project database URLs — may need adjustment after font asset creation
- Verify Platform Settings on TMP font assets: SDFAA, Unicode range set correctly
