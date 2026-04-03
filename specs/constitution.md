<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 2.0.0 (MAJOR — UI Backend principle redefined from uGUI to UI Toolkit; Development Constraints section rewritten)
Modified principles:
  - Principle IV: "Mobile-First Performance" — removed CanvasRenderer/MaterialPropertyBlock/UV channel packing rules; replaced with UI Toolkit batching guidance
  - Development Constraints: "uGUI only" → "UI Toolkit (UXML + USS + C#)"; Shader Base from fork of UI/Default → URP Shader Graph
Added sections: N/A
Removed sections: N/A
Templates requiring updates:
  ✅ .specify/memory/constitution.md — this file (updated now)
  ✅ .specify/templates/plan-template.md — "Constitution Check" gate section references this file dynamically; no hardcoded principle names
  ✅ spec.md — will be regenerated to reflect UI Toolkit
Deferred TODOs: None
-->

# UISystem Constitution

## Core Principles

### I. Zero Dependencies

UISystem MUST have zero runtime or editor dependencies on the parent Infrastructural
framework. All code lives under `Assets/UISystem/`. Assembly definitions are
`mehmetsrl.UISystem` (runtime) and `mehmetsrl.UISystem.Editor` (editor-only).
No references to `mehmetsrl.Presenter`, `mehmetsrl.Bindings`, `mehmetsrl.SubSystem`,
or any other Infrastructural assembly are permitted.

**Rationale**: UISystem is designed as a standalone submodule usable in any Unity project.
Coupling it to Infrastructural internals would break portability and make the library
impossible to use without the full framework.

**Compliance gate**: Verify via Assembly Definition Inspector — `mehmetsrl.UISystem.asmdef`
MUST list zero references to other `mehmetsrl.*` assemblies.

### II. ScriptableObject Configuration

All configuration MUST be expressed via ScriptableObjects. Hardcoded values, static
configuration dictionaries, and magic constants are prohibited. `ThemeData`,
`TypographyConfig`, elevation presets, shape presets, and motion presets are all
ScriptableObjects. Components MUST reference configuration SOs via serialized fields,
not via static accessors or singletons that bypass serialization.

**Rationale**: SO-based config is inspectable, overridable per-project, and persists across
domain reloads — the Unity-native pattern for reusable library configuration.

**Compliance gate**: No `static readonly` color/float constants in runtime code.
Configuration values MUST trace back to a serialized SO field.

### III. Unity Conventions

Code MUST follow established Unity and project conventions:
- Namespace pattern: `mehmetsrl.UISystem`, `mehmetsrl.UISystem.Core`,
  `mehmetsrl.UISystem.Components`, `mehmetsrl.UISystem.Enums`, `mehmetsrl.UISystem.Data`
- Editor code: `mehmetsrl.UISystem.Editor` namespace, `mehmetsrl.UISystem.Editor` asmdef
- All asmdefs: `autoReferenced: true`
- Odin Inspector attributes (`[TitleGroup]`, `[BoxGroup]`, `[ReadOnly]`, etc.) MUST be
  used for editor tooling to match Infrastructural framework quality
- Folder layout: `Runtime/` for runtime code, `Editor/` for editor-only code,
  `Assets/` for SO and prefab assets, `Samples~/` for sample scenes,
  `Styles/` for USS stylesheets, `UXML/` for UXML templates

**Rationale**: Consistency with the rest of the framework reduces cognitive overhead and
makes UISystem feel native to the project.

### IV. Mobile-First Performance

UISystem MUST be designed for mobile (Android/iOS, Mali GPUs, fill-rate sensitive).
Specific rules:
- UI Toolkit's internal batching MUST be preserved. Material overrides per VisualElement
  that break batching MUST be avoided or explicitly justified with profiling data.
- Every shader feature (shadow, ripple, state overlay) in the Shader Graph MUST be
  independently toggleable to allow profiling and fallback on low-end hardware.
- dp → pixel conversion MUST use `PanelSettings` with `Scale With Screen Size` and
  reference resolution 1080×1920. `Screen.dpi` MUST NOT be used as the primary
  conversion method.
- USS transitions MUST use durations from `ThemeData` motion presets — no hardcoded
  `200ms` or similar magic values in USS files.

**Rationale**: UISystem targets mobile casual/hypercasual games. On Mali GPUs, fill rate
is the primary bottleneck. UI Toolkit's batching model must be respected. `Screen.dpi`
is unreliable on many Android OEM devices.

**Compliance gate**: Profile the Button test scene on a reference Android device — all
UISystem elements MUST appear as a minimal draw call group in the GPU profiler.

### V. Incremental Delivery

Each spec phase MUST produce a working, independently testable deliverable:
- **Foundation (004)**: UI Shader Graph element + ThemeManager (SO → USS sync) +
  Typography USS classes — verifiable in a sample scene without any component existing
- **Button (005)**: 4 button styles × light/dark themes + state overlay + ripple,
  verifiable in isolation
- **Components (006)**: Each component (Card, Toggle, TextField, Dialog, Snackbar, Nav)
  independently testable before the next one begins

No phase MUST be considered complete until its Unity Editor test scene passes manual
verification. Merging to `main` MUST NOT happen until the phase deliverable is verified.

**Rationale**: The SCOPE.md explicitly notes that issues found in Button may require
revisiting Foundation. Incremental delivery ensures regressions are caught at each
phase boundary, not at final integration.

## Development Constraints

- **UI Backend**: UI Toolkit (UXML + USS + C#). Unity 6.3+ required. Built-in Render
  Pipeline is explicitly out of scope — UISystem requires URP for Shader Graph UI support.
- **Shader Base**: `SDFRect.shadergraph` MUST be a URP UI Shader Graph (new in Unity 6.3).
  The hand-written `UI/Default` shader fork from the previous uGUI implementation MUST
  NOT be carried forward.
- **Font Assets**: Weight-per-asset strategy (Regular 400 + Medium 500 as separate TMP
  font assets linked via Font Weights table). "Fake bold" via SDF dilation is prohibited.
- **Toggle Thumb**: Separate `VisualElement` (not a shader parameter). Independent state
  layers and icon support require it.
- **External Dependencies**: TextMeshPro (Unity package), URP (Unity package), and Odin
  Inspector are the only permitted external dependencies. DOTween MUST NOT be introduced
  into UISystem runtime (motion uses USS `transition` or `AnimationCurve` + coroutines).

## Delivery Model

Specs are organized as phase-based branches in the parent Infrastructural repository:

| Branch | Spec | Work Packages | Gate to next phase |
|--------|------|---------------|--------------------|
| `004-uisystem-foundation` | Foundation | WP-1 + WP-2 + WP-3 | Test scene verified |
| `005-uisystem-button` | Core + Button | WP-4 + WP-5 + WP-10 (partial) | Button test scene verified |
| `006-uisystem-components` | Components | WP-6 → WP-9 + WP-10 (complete) | Each component verified |

Spec artifacts (`spec.md`, `plan.md`, `tasks.md`, `research.md`, `quickstart.md`) live
under `Infrastructural/specs/NNN-feature-name/`. Actual Unity code lives in the
`Assets/UISystem/` submodule on the same branch.

## Governance

All development on UISystem MUST comply with this constitution. Violations require explicit
justification documented in the plan's **Complexity Tracking** section before implementation
begins.

**Amendment procedure**: Amendments MUST be proposed as a PR modifying this file. The
version MUST be incremented (MAJOR for principle removal/redefinition, MINOR for new
principle, PATCH for clarification). `LAST_AMENDED_DATE` MUST be updated on every change.

**Compliance review**: Every `speckit.plan` run MUST include a "Constitution Check" section
verifying the implementation approach against each principle. Violations discovered during
implementation MUST be flagged before proceeding, not after.

**Supersession**: This constitution supersedes all informal conventions. When in conflict,
the constitution takes precedence over prior specs, code comments, or verbal agreements.

**Version**: 2.0.0 | **Ratified**: 2026-03-30 | **Last Amended**: 2026-04-01
