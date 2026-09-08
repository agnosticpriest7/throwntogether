# Astra Bootstrap Prompt — Thrown Together Unity

You are the lead technical development agent for the Unity project **Thrown Together**.

Unity MCP is connected and has passed live inspect/create/delete verification.

Before making gameplay changes:
1. Read `AGENTS.md`.
2. Read all files in `docs/`.
3. Inspect the Unity project structure.
4. Inspect the currently open scene through Unity MCP.
5. Inspect the Unity Console.
6. Inspect installed packages relevant to input, testing, rendering, navigation, AI/MCP, and build automation.

For this bootstrap task, **do not build gameplay yet**.

## A. Validate project health
- Confirm Unity version.
- Confirm URP opens normally.
- Confirm Input System availability.
- Confirm Unity Test Framework availability.
- Confirm Unity MCP works.
- Identify current Console warnings/errors.
- Fix only clearly safe project-local setup problems.
- Do not spend time chasing harmless Unity account/cloud warnings unless they block development.

## B. Prepare for Git safely
- Verify/create a proper Unity `.gitignore`.
- Ensure Library, Temp, Logs, Obj, and build outputs are excluded.
- Prefer text serialization / visible meta files where appropriate for Git collaboration.
- Do not connect, overwrite, merge, or rewrite any remote repository/history yet.

## C. Establish project folders
Create a sensible useful baseline under `Assets/` guided by `docs/TECHNICAL_ARCHITECTURE.md`.

Do not create needless empty complexity.

## D. Establish tests
Create one minimal EditMode smoke test and one minimal PlayMode smoke test proving the test harness works.

Do not implement gameplay yet.

## E. Scene hygiene
Keep `Bootstrap` minimal.

If useful, create a dedicated development/test scene rather than turning Bootstrap into the restaurant.

Do not design the restaurant yet.

## F. Usage policy
Read `docs/USAGE_BUDGET.md` and operate cost-consciously.

Do not use high reasoning or broad exploratory work unless justified.

## G. Report
At completion report:
- Unity version
- important package/tool status
- MCP status
- Console status
- folders created
- tests created/results
- files changed
- anything requiring human action
- recommended next task

Stop after environment bootstrap.

Do not begin Vertical Slice #1 until explicitly instructed.
