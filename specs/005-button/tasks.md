# Tasks: UISystem Button — WP-4 + WP-5 + WP-10 (partial)

**Input**: Design documents from `/specs/005-uisystem-button/`
**Updated**: 2026-04-01
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/public-api.md ✅, quickstart.md ✅

**Tests**: No automated test tasks — manual Unity Editor verification only (Play mode + MCP).
Each phase ends with a manual checkpoint.

**Organization**: Setup → Foundational → US1 (StateLayerController) → US2 (M3Button + USS + scene) → US3 (Editor menu) → Polish.
US2 depends on US1 complete. US3 depends on US2 complete. Sequential delivery required.

---

## Phase 1: Setup

**Purpose**: Create shared enum and folder structure before any user story begins.

- [x] T001 [P] Create `Assets/UISystem/Runtime/Enums/ButtonVariant.cs` — enum with `Filled=0`, `Outlined=1`, `Text=2`, `Tonal=3`; namespace `mehmetsrl.UISystem.Enums`; XML doc comments per value
- [x] T002 [P] Create `Assets/UISystem/Runtime/Components/` folder — add `.gitkeep` placeholder so git tracks the empty directory before M3Button.cs is written

**Checkpoint**: ButtonVariant compiles, zero errors.

---

## Phase 2: Foundational

**Purpose**: Update `state-layer.uss` with the `.m3-disabled` rule — required by StateLayerController (US1) and applied to M3Button disabled state (US2).

- [x] T003 Update `Assets/UISystem/Styles/state-layer.uss` — replace placeholder comment with real content: `.m3-disabled { opacity: 0.38; }` + documentary comments for `.m3-hovered`, `.m3-pressed`, `.m3-focused` explaining they are driven by `SDFRectElement.StateOverlayOpacity` C# property (not USS background-color)

**Checkpoint**: state-layer.uss has `.m3-disabled` rule, zero parse errors.

---

## Phase 3: User Story 1 — State Layer Controller (Priority: P1)

**Goal**: Plain C# interaction feedback controller — hover/pressed/focused overlays + ripple + disabled state.

**Independent Test**: Add an `SDFRectElement` + `RippleElement` to a UIDocument. Construct `StateLayerController(sdfElement, rippleElement)`, call `Attach()`. Enter Play mode. Hover → 8% overlay. Press → 10% overlay + ripple. Release → clears. Set `Disabled=true` → element dims to 38%, clicks ignored.

### Implementation for User Story 1

- [x] T004 [US1] Create `Assets/UISystem/Runtime/Core/StateLayerController.cs` — plain C# class (NOT MonoBehaviour) in namespace `mehmetsrl.UISystem.Core`; constructor `StateLayerController(VisualElement target, RippleElement ripple = null)`; private fields `_isHovered`, `_isPressed`, `_isFocused` (bool); private `_sdfTarget` (SDFRectElement cast of target, nullable); `public bool Disabled { get; set; }` — when set to true applies/removes `.m3-disabled` on target; `public Color OverlayColor { get; set; } = Color.white` — assigned to `_sdfTarget.StateOverlayColor`; `public void Attach()` — registers `PointerEnterEvent`, `PointerLeaveEvent`, `PointerDownEvent`, `PointerUpEvent`, `FocusInEvent`, `FocusOutEvent` on target using private handler methods; `public void Detach()` — unregisters all 6 callbacks; private `UpdateOverlay()` — sets `_sdfTarget.StateOverlayOpacity` to: 0.10 if `_isPressed || _isFocused`, 0.08 if `_isHovered`, 0.0 otherwise; on `PointerDownEvent` also calls `_ripple?.StartRipple(evt.localPosition)`; Attach() must be idempotent (check if already attached)

**Checkpoint (US1)**: StateLayerController compiles. Attach() registers callbacks without error. Manual test: hover/press/disabled states all produce correct visual feedback.

---

## Phase 4: User Story 2 — M3 Button Component (Priority: P2)

**Goal**: `M3Button` VisualElement with 4 variants, full interaction feedback, theme-aware colors.

**Independent Test**: ButtonDemo.unity scene with 4 M3Button instances. Play mode: all 4 variants render with M3-correct colors and pill shape. Hover/press shows overlay + ripple. Click fires `OnClick`. Toggle theme → all colors update. One disabled button shows 38% opacity and ignores clicks.

### Implementation for User Story 2

- [x] T005 [US2] Create `Assets/UISystem/Runtime/Components/M3Button.cs` — `[UxmlElement]` `partial class M3Button : VisualElement` in namespace `mehmetsrl.UISystem.Components`; private fields: `SDFRectElement _root`, `RippleElement _ripple`, `Label _label`, `VisualElement _iconSlot`, `StateLayerController _stateLayer`; `public event Action OnClick`; `[UxmlAttribute("text")] public string Text` — getter/setter that updates `_label.text`; `[UxmlAttribute("variant")] public ButtonVariant Variant` — getter/setter that calls `ApplyVariant()`; `[UxmlAttribute("disabled")] public bool Disabled` — getter/setter that sets `_stateLayer.Disabled` and updates `_root`; constructor: creates `_root = new SDFRectElement()`, sets `_root.CornerRadius = 9999`, adds `.m3-button` USS class; creates `_ripple = new RippleElement()`, adds to `_root`; creates `_label = new Label()`, adds `.m3-button__label` class, adds to `_root`; creates `_iconSlot = new VisualElement()`, hides it; creates `_stateLayer = new StateLayerController(_root, _ripple)`, calls `Attach()`; registers `ClickEvent` on `_root` → fires `OnClick` if not disabled; adds `_root` as child; calls `ApplyVariant(ButtonVariant.Filled)` as default; private `ApplyVariant(ButtonVariant v)` — removes all variant USS classes from `_root` and label, then adds `m3-button--{variant}` on `_root` and `m3-button__label--{variant}` on `_label`; also sets `_root.ShadowBlur` and `_root.ShadowOffsetY` from elevation-2 values for Filled variant only (0 for others); sets `_stateLayer.OverlayColor` to white for Filled/Tonal, primary-tint for Text/Outlined

- [x] T006 [P] [US2] Create `Assets/UISystem/Styles/Components/button.uss` — `.m3-button` base class: `min-height: 40px`, `padding-top: 10px`, `padding-bottom: 10px`, `padding-left: 24px`, `padding-right: 24px`, `align-items: center`, `justify-content: center`, `flex-direction: row`; `.m3-button--filled`: `background-color: var(--m3-primary, rgb(103,80,164))`; `.m3-button--outlined`: `background-color: rgba(0,0,0,0)`, `border-width: 1px`, `border-color: var(--m3-outline, rgb(121,116,126))`; `.m3-button--text`: `background-color: rgba(0,0,0,0)`; `.m3-button--tonal`: `background-color: var(--m3-secondary-container, rgb(232,222,248))`; label classes: `.m3-button__label--filled`: `color: var(--m3-on-primary, rgb(255,255,255))`; `.m3-button__label--outlined`: `color: var(--m3-primary, rgb(103,80,164))`; `.m3-button__label--text`: `color: var(--m3-primary, rgb(103,80,164))`; `.m3-button__label--tonal`: `color: var(--m3-on-secondary-container, rgb(28,25,35))`; all fallback values match M3 baseline light theme

- [x] T007 [P] [US2] Create `Assets/UISystem/UXML/M3Button.uxml` — minimal UXML template: `<ui:UXML>` with `<Style src="../Styles/Components/button.uss" />`, `<Style src="../Styles/state-layer.uss" />`, `<mehmetsrl.UISystem.Components.M3Button variant="Filled" text="Button" />`

- [x] T008 [US2] Create `Assets/UISystem/Samples~/Button/ButtonDemo.uxml` — 2-row layout showing all 4 variants: `<Style>` tags for typography.uss + button.uss + state-layer.uss + foundation-demo.uss; `<VisualElement class="foundation-root">` containing `<VisualElement>` with 4 `<mehmetsrl.UISystem.Components.M3Button>` elements (Filled text="Filled", Outlined text="Outlined", Text text="Text", Tonal text="Tonal") and one disabled Filled button; plus a `<mehmetsrl.UISystem.Core.SDFRectElement name="btn-switch-theme" corner-radius="9999" class="btn-primary">` Switch Theme button at the bottom

- [x] T009 [US2] Create `Assets/UISystem/Scenes/ButtonDemo.unity` — Unity scene with: `[UISystem]` GameObject containing ThemeManager component (LightTheme=DefaultLight.asset, DarkTheme=DefaultDark.asset, ActiveTheme=DefaultLight.asset, LightSheet=light.uss, DarkSheet=dark.uss); `UIDocument` GameObject with UIDocument component (PanelSettings=DefaultPanelSettings.asset, SourceAsset=ButtonDemo.uxml from Samples~/Button/) and ThemeSwitchButton MonoBehaviour; Main Camera; scene saved to `Assets/UISystem/Scenes/ButtonDemo.unity`

**Checkpoint (US2)**: All 4 button variants render correctly in Play mode. Hover/press/ripple functional. OnClick fires. Theme switch updates all button colors. Disabled button dims and ignores clicks.

---

## Phase 5: User Story 3 — Editor Context Menu (Priority: P3)

**Goal**: `Assets > Create > UISystem > Button (Filled/Outlined)` context menu items that generate UXML snippets.

**Independent Test**: Right-click a Project folder → `Assets > Create > UISystem > Button (Filled)`. Verify a `.uxml` file is created with `<mehmetsrl.UISystem.Components.M3Button variant="Filled" text="Button" />`.

### Implementation for User Story 3

- [x] T010 [US3] Create `Assets/UISystem/Editor/MenuItems/UISystemMenuItems.cs` — `static class UISystemMenuItems` in namespace `mehmetsrl.UISystem.Editor`; `[MenuItem("Assets/Create/UISystem/Button (Filled)", priority = 200)]` static method `CreateFilledButton()` — gets selected folder via `AssetDatabase.GetAssetPath(Selection.activeObject)`, falls back to `"Assets"` if null; builds UXML content string with `<ui:UXML>` root containing `<Style src=...>` for button.uss and state-layer.uss plus `<mehmetsrl.UISystem.Components.M3Button variant="Filled" text="Button" />`; writes via `System.IO.File.WriteAllText(path + "/NewFilledButton.uxml", content)`; calls `AssetDatabase.Refresh()`; `[MenuItem("Assets/Create/UISystem/Button (Outlined)", priority = 201)]` same pattern for Outlined variant

**Checkpoint (US3)**: Context menu items appear in Unity Editor. Generated UXML files contain correct M3Button variant attributes.

---

## Phase 6: Polish & Verification

- [x] T011 [P] Verify `Unity_GetConsoleLogs` — zero C# compile errors; zero UISystem runtime warnings; confirm ThemeSwitchButton picks up new `.m3-button--*` classes correctly
- [ ] T012 [P] Take `ScreenCapture.CaptureScreenshot` in Play mode — ButtonDemo scene showing all 4 variants, ripple animation, disabled state visible
- [x] T013 [P] Update `Assets/UISystem/README.md` — add M3Button and StateLayerController to Features section; update Components table to mark Button as implemented

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Start immediately — ButtonVariant.cs and folder structure
- **Foundational (Phase 2)**: After Phase 1 — state-layer.uss .m3-disabled rule
- **US1 (Phase 3)**: After Phase 2 — StateLayerController needs .m3-disabled in USS
- **US2 (Phase 4)**: After US1 complete — M3Button depends on StateLayerController
- **US3 (Phase 5)**: After US2 complete — Editor menu generates M3Button UXML
- **Polish (Phase 6)**: After US3 (or after US2 for faster verification)

### Within Each User Story

- T005 (M3Button.cs) must complete before T009 (ButtonDemo scene references M3Button)
- T006 (button.uss) must complete before T008 (ButtonDemo.uxml uses button USS classes)
- T007 (M3Button.uxml) and T006 (button.uss) are parallel — different files
- T008 (ButtonDemo.uxml) must complete before T009 (scene references UXML)

### Parallel Opportunities

- T001, T002 (Phase 1): Parallel — different files
- T006, T007 (Phase 4): Parallel — button.uss and M3Button.uxml are independent
- T011, T012, T013 (Phase 6): All parallel — verification and docs

---

## Parallel Example: Phase 4 (US2)

```
After T004 (StateLayerController) complete:

  Parallel: T006 (button.uss), T007 (M3Button.uxml)
  Sequential: T005 (M3Button.cs) — depends on ButtonVariant + StateLayerController
  Sequential after T005 + T006: T008 (ButtonDemo.uxml)
  Sequential after T008: T009 (ButtonDemo.unity scene)
```

---

## Implementation Strategy

### MVP First (US1 + US2 only)

1. Complete Phase 1 + Phase 2 (Setup + Foundational)
2. Complete US1 (T004 StateLayerController)
3. Validate: hover/press/disabled on a bare SDFRectElement in Play mode
4. Complete US2 (T005–T009 M3Button + ButtonDemo)
5. **STOP and VALIDATE**: ButtonDemo scene passes all quickstart.md scenarios 1–5
6. Proceed to US3 if needed

### Full Delivery

1. Phase 1 + 2 setup
2. US1 → US2 → US3 sequentially
3. Phase 6 polish + MCP verification

---

## Notes

- [P] = different files, no dependencies, can run in parallel
- [USn] maps task to User Story n from spec.md
- `ButtonDemo.unity` placed in `Assets/UISystem/Scenes/` (NOT `Samples~/`) — Samples~ excluded from AssetDatabase → MonoBehaviour GUIDs unresolvable (same issue as FoundationDemo, documented in 004)
- `StateLayerController.Attach()` must be idempotent — called from M3Button constructor, which may run during UXML deserialization
- USS fallback values in button.uss match M3 baseline light theme — renders correctly even without ThemeManager
