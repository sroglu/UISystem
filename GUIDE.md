# UISystem — Documentation Guide

Read this file to navigate UISystem documentation. Follow the tree to find the right document for your task.

## Document Tree

```
Assets/UISystem/
├── README.md                  — Package overview, component inventory, quick start
├── SCOPE.md                   — 8 work packages, technical decisions, architecture layers, M3 reference links
├── GUIDELINES.md              — Lessons from Button phase: pill shapes, Painter2D bounds, optical centering, domain reload
├── COMPONENT-GUIDE.md         — Authoritative component creation reference (7 mandatory rules, exception registry, anatomy)
│
└── specs/
    ├── constitution.md        — Governing principles v2.0.0: zero deps, SO config, Unity conventions, mobile-first, incremental delivery
    │
    ├── 004-foundation/        — Foundation phase (SDFRectElement, ThemeManager, Typography)
    │   ├── spec.md            — User stories & acceptance criteria
    │   ├── plan.md            — Implementation plan & constitution check
    │   ├── tasks.md           — Task breakdown
    │   ├── research.md        — Technical research findings
    │   ├── data-model.md      — Entity definitions
    │   ├── quickstart.md      — Quick reference
    │   ├── contracts/         — API surface (public-api.md)
    │   └── checklists/        — Verification (requirements.md)
    │
    └── 005-button/            — Button phase (5 variants, state overlay, ripple)
        ├── spec.md
        ├── plan.md
        ├── tasks.md
        ├── research.md
        ├── data-model.md
        ├── quickstart.md
        ├── contracts/
        └── checklists/
```

### Parent Repo Specs (outside submodule)

```
Infrastructural/specs/006-m3-uisystem-overhaul/
├── spec.md                    — 6 user stories: USS-pure theming, M3ComponentBase, typography, animation, SDF shadows, dynamic color
├── plan.md                    — Implementation roadmap, complexity tracking, constitution check
├── tasks.md                   — Phase 2 task breakdown
├── data-model.md              — ThemeData, ColorPalette (27+ roles), TextRole (15 roles), M3ComponentBase
├── research.md                — Phase 0 research
├── quickstart.md              — Verification guide
├── audit-inline-colors.md     — C# inline color assignment audit
├── audit-transition-durations.md — Hardcoded animation duration audit
├── contracts/public-api.md    — API surface changes
└── checklists/requirements.md — Acceptance criteria
```

## When to Read What

| Task | Start Here |
|------|-----------|
| Creating a new M3 component | `COMPONENT-GUIDE.md` → then `specs/constitution.md` |
| Understanding UISystem overview | `README.md` → then `SCOPE.md` |
| Debugging visual/layout issues | `GUIDELINES.md` |
| Checking phase deliverables | `specs/NNN/spec.md` + `tasks.md` |
| Verifying rules compliance | `specs/constitution.md` + `COMPONENT-GUIDE.md` |
| Understanding color/theme system | `SCOPE.md` (WP-2) + `specs/006/data-model.md` |
| Typography setup | `SCOPE.md` (WP-3) + `specs/006/data-model.md` |
| Reviewing what changed in 006 | `specs/006/plan.md` + audit files |
