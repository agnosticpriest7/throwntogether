# Thrown Together — Agent Instructions

## Role
The human owner is the Game Director / Decision Maker / Playtester.

AI agents own implementation, architecture, Unity scene/prefab work, debugging, tests, builds, documentation, and routine technical decisions.

Before major work read:
- `docs/GAME_DESIGN.md`
- `docs/DESIGN_PILLARS.md`
- `docs/TECHNICAL_ARCHITECTURE.md`
- `docs/DECISIONS.md`
- `docs/USAGE_BUDGET.md`
- recent relevant entries in `docs/PLAYTEST_FEEDBACK.md`

## Core rules
1. Single-player must remain fully viable.
2. Local couch co-op remains core.
3. Players are never forced out of hands-on restaurant work.
4. Employees, not automation, are the primary long-term progression system.
5. Growth increases capacity; bigger is not automatically better.
6. The restaurant is persistent.
7. Menus can change frequently and should interact with inventory, equipment, and staffing.
8. Ingredients cost money and inventory persists.
9. Bulk purchasing is supported.
10. No ingredient spoilage/decay unless explicitly approved later.
11. Reputation drives baseline demand; advertising is temporary.
12. Throwing is optional convenience/skill, not required for basic operation.
13. Do not introduce repair/maintenance spending as a core economy mechanic.
14. Prefer simple interacting systems over simulation for simulation's sake.

## Unity working rules
Use Unity MCP whenever live Editor inspection or manipulation is useful.

For gameplay work:
1. Inspect current project/scene state.
2. Check Console before changes.
3. Implement the smallest coherent feature.
4. Compile and fix errors.
5. Run relevant EditMode tests.
6. Run relevant PlayMode tests.
7. Enter Play Mode when practical and validate the feature.
8. Check Console again.
9. Update docs only where architecture/design changed.
10. Do not report completion with unexplained new Console errors.

Never claim physical controller, Xbox Edge, couch co-op, or mobile testing unless it actually occurred.

## Architecture
Prefer modular scripts, prefabs, ScriptableObjects/data assets, authored scenes, and reusable UI.

Avoid one enormous scene containing all logic. Minimize simultaneous edits to the same `.unity` scene/prefab. Prefer prefab/data changes where possible.

## Creative escalation
Ask the human when a choice materially changes player progression, economy, employee behavior, menu philosophy, restaurant expansion, difficulty, visual identity, commercial direction, or scope.

Do not ask the human to choose class names, interfaces, folders, serialization details, ordinary test structure, or minor implementation patterns.

## Completion standard
A feature is complete only when it works, relevant tests pass, the project builds, there are no new unexplained Console errors, existing gameplay is not obviously regressed, and human-playtest items are documented.
