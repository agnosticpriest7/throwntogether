# Thrown Together — Technical Architecture

## Engine / platform
- Unity 6
- Universal Render Pipeline
- Windows development
- Web build for rapid couch/browser testing
- Native Windows build for eventual Steam release

## Tools
- Codex
- GPT-6 Astra for high-value architecture/gameplay work
- Unity MCP for live Editor control and inspection
- GitHub
- Unity Test Framework
- Unity Input System

## Camera / world
- fixed elevated camera
- orthographic or mild perspective
- mostly planar gameplay
- stylized 3D
- grid-aware placement where useful
- smooth analogue movement

## Input
Support:
- one local human player
- optional second local human player
- gamepads
- keyboard/debug fallback
- Web controller testing

Touch may exist as a development convenience but should not drive core architecture.

## Scene strategy
Prefer focused scenes such as:
- Bootstrap
- MainMenu
- Restaurant
- Development/Test scenes

Avoid one giant monolithic scene.

## Suggested project structure

Assets/
- Art/
- Audio/
- Data/
  - Ingredients/
  - Recipes/
  - Appliances/
  - Staff/
  - Upgrades/
- Materials/
- Prefabs/
  - Characters/
  - Customers/
  - Staff/
  - Kitchen/
  - Furniture/
  - Appliances/
  - UI/
- Scenes/
- Scripts/
  - Core/
  - Input/
  - Interaction/
  - Cooking/
  - Inventory/
  - Economy/
  - Restaurant/
  - Customers/
  - Staff/
  - Persistence/
  - UI/
- Tests/
  - EditMode/
  - PlayMode/

Do not reorganize working content merely to match this exactly.

## Data-driven content
Strong candidates for ScriptableObjects:
- IngredientDefinition
- RecipeDefinition
- ApplianceDefinition
- EmployeeRoleDefinition
- EmployeeTraitDefinition
- RestaurantUpgradeDefinition
- AdvertisingCampaignDefinition

New recipes should normally be content/data, not bespoke engine code.

## Employees
Use deterministic task-driven game AI. Do not use an LLM inside the runtime game for ordinary employee behavior.

Design for roles, capabilities, wages, assignments, experience, and later promotion/transfer.

## Customers
Use deterministic state/task simulation such as arrival, waiting, seating, ordering, waiting for food, eating, and departure.

## Persistence
Use versioned save data that can migrate as the game evolves.

## Multi-location later
Do not continuously render off-screen restaurants. Simulate their economic/operational results.

## Testing
EditMode: economy, inventory, recipes, wages, reputation, discounts, serialization, progression rules.

PlayMode: movement, interaction, cooking, employee/customer behavior, local join, vertical slices.

## Completion
Major features should compile, pass relevant tests, create no new unexplained Console errors, receive Play Mode validation when practical, and produce a successful Web build when they affect the playable game.

## Agent-friendly architecture
- isolate systems
- use prefabs
- use data assets
- minimize shared-scene churn
- avoid unnecessary hard references
- keep code ownership boundaries understandable

## Local build and Web publication

`Assets/Editor/BuildAutomation.cs` exposes CLI entry points `ThrownTogether.Editor.BuildAutomation.BuildWeb` and `BuildWindows`. Both read the ordered scene list from root `build-config.json`, validate scene assets, use StrictMode and fail on a failed build report or compilation errors. RestaurantDevelopment is the configured scene for Vertical Slice #1. Windows shares the implementation but is not part of the Web pipeline's validation.

## Vertical Slice #1 foundations

The ThrownTogether.Runtime assembly separates per-chef input/movement, single-item carry slots, interactables, data-driven processing recipes, plating, dish matching and customer orders into reusable components. The development restaurant is an authored scene using shared data/material assets and chef/item prefabs. Processing stations use deterministic elapsed-time transitions (1.5-second prep, 5-second frying); service uses a temporary automatic delivery, not employee AI. Input maps belong to individual chefs and expose device binding for later local joining. Full implementation details, controls and human acceptance checks are in `docs/DEVELOPMENT_NOTES.md`.

For direct CLI use on a closed/disposable project, invoke Unity with `-batchmode -quit -projectPath <project> -buildTarget WebGL -executeMethod ThrownTogether.Editor.BuildAutomation.BuildWeb -logFile <log>`. Default output: Builds/Web; `-buildOutput <path>` overrides it. For Windows use `-buildTarget Win64 -executeMethod ThrownTogether.Editor.BuildAutomation.BuildWindows` (default Builds/Windows/ThrownTogether.exe). Direct invocation applies Web player settings to that checkout; the wrapper isolates these changes.

Recommended Windows commands:

```powershell
./scripts/test-build-deploy-web.ps1 -Mode Test
./scripts/test-build-deploy-web.ps1 -Mode Build
./scripts/test-build-deploy-web.ps1
```

The wrapper requires clean main, snapshots committed Assets/Packages/ProjectSettings/config into ignored Builds/Workspace, retains its Library cache, runs all discovered EditMode then PlayMode tests, checks exit codes and fresh XML (including nonzero test counts), and only then builds. It checks generated index/data/wasm files and rejects compressed or oversized files. Each batch process is assigned to a Windows job through scripts/UnityBatchJob.cs; closing the job releases its child compilers without affecting the canonical Editor. Tiny compiler response caches are regenerated between stages to avoid Unity 6000.6 file-lock failures. This wrapper requires 64-bit Windows PowerShell/PowerShell. Canonical Unity can remain open. Output: Builds/Web; diagnostics: Builds/PipelineLogs. Deployment additionally requires local HEAD to match freshly fetched origin/main, both before and after the build.

Web settings favor static-host compatibility: compression disabled, decompression fallback disabled, native threads disabled (no cross-origin isolation headers needed), data caching disabled, hashed build filenames and Unity's default template with relative asset URLs. These work below `/throwntogether/` without root-relative asset paths. The deploy root contains `.nojekyll` and `build-info.json`. This is a test distribution, not a production optimization pass.

Publishing uses a separate temporary Git repository in Builds/Publish-* and normal fast-forward commits to gh-pages. An existing deployment's files are replaced in the index, without checking out browser or Unity sources there. Concurrent remote deployment updates cause push rejection rather than overwrite. No force push is used. Main, legacy/web-prototype and web-prototype-final are never publication targets.

Pages source: **Deploy from a branch, gh-pages, / (root)**. The script configures this with authenticated `gh api`; if unavailable it reports that exact human action. Public test URL: **https://agnosticpriest7.github.io/throwntogether/**. Publishing is local and explicit, not triggered by every main push. Check `/throwntogether/build-info.json` for deployed provenance. The retired browser Actions workflow remains only in legacy history.

References: [GitHub Pages publishing sources](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site), [Unity Web deployment and compression](https://docs.unity3d.com/Manual/webgl-deploying.html).
