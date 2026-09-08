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

## Post-slice data and diagnostic foundations

All six requested definition families now share ContentDefinition metadata: a stable content ID, display name and description. IngredientDefinition retains the existing state names/colors. RecipeDefinition describes the plated output and references the ordered ProcessingRecipe steps; DishRecipe remains a compatibility subclass so the existing Fries asset GUID survives. ProcessingRecipe remains the shared timed transition data. ApplianceDefinition lists supported processing assets; prep and fryer reference their respective definitions. Existing states, timing and scene geometry are unchanged.

EmployeeRoleDefinition, AdvertisingCampaignDefinition and RestaurantUpgradeDefinition are typed, creatable metadata schemas only. They have no live instances, economy effects, balancing values or runtime systems. Role capabilities/wages, advertising effects/pricing/duration, and upgrade effects/prices/unlocks remain design questions for their later milestones. No consequential creative choice was needed for this foundation work.

Each ChefInput continues to own its action map. BindDevices assigns an explicit device set; null restores unrestricted single-player fallback. AcceptsDevice and LastActiveDevice support diagnostics and assignment tests. No join manager or Player 2 is active. After physical controller testing, add a session-owned join coordinator that instantiates the existing Chef prefab, assigns each chef distinct devices (or InputUser pairing), handles disconnect/reconnect without silently transferring ownership, and connects each player's HUD focus independently. Coordinate scene restart at session level before enabling multiple players. Keyboard sharing, join/leave controls, respawn position and shared-camera framing need approval/playtest evidence later. Do not enable a second chef merely because the input maps support isolation.

DevelopmentDiagnostics is attached at runtime only in the Editor or builds compiled with THROWNTOGETHER_DIAGNOSTICS. The expanded panel starts OFF; F3 or clicking the small DEV strip toggles it. It reports the full build commit/time, last active input device, connected gamepad names/IDs, interaction target, held item/state and sampled FPS. The always-visible small strip shows the short commit and connected gamepad count. Gamepad enumeration means detected by Unity/browser, not physically verified. No controller binding was added or changed.

The Web pipeline passes -developmentDiagnostics and -buildCommit HEAD into BuildAutomation, which writes a Resources/DevelopmentBuildStamp.json only in the isolated build snapshot and sets the diagnostics compilation symbol for that build. Direct release builds omit -developmentDiagnostics; the overlay/strip code is excluded and any stale stamp is removed. Git-generated provenance remains in build-info.json as well. This is an optimized Web build with diagnostic UI, not Unity's heavyweight Development Build/profiler mode.

Regression coverage now includes exact existing recipe durations, appliance/process compatibility, raw/cut/cooked plating and order checks, wrong ingredient rejection, invalid service sequence, full single-chef cooking/delivery, restart to clean initial state, and synthetic assigned/unassigned gamepad isolation. Movement/camera feel is not assessed by these tests.

### Web input boundary
WebInputFocus.jslib reads document visibility, canvas focus and browser Gamepad API status. ChefInput gates Web input on that focus boundary, resets actions on focus loss and requires interaction/restart release before resuming. Native movement/mappings are unchanged. Development diagnostics reuse that focus signal and show mapped-gamepad P1 eligibility plus connection events. The development-only Web template preserves the original 16:10 canvas aspect, provides focus/fullscreen helpers, suppresses focused gameplay scroll keys, and lets Unity track CSS size/devicePixelRatio. Browser-shell Node tests are included in the local pipeline; the ordinary template is selected when -developmentDiagnostics is omitted.

## Pre-playtest audio and settings foundation

RestaurantAudio.prefab owns AudioPlayback, SettingsService and RestaurantAudioFeedback. The scene supplies chef/prep/fryer/order references; there is no global audio singleton or persistent gameplay object. AudioCue assets select from non-null clips with a private random generator and bounded pitch/gain variation. Ten reusable 2D AudioSources provide a fixed voice budget; missing clips or saturation safely skip feedback. Frying owns a looping source that stops on completion/disable. ChefController emits an observation only after a successful interaction; the feedback observer never changes cooking or order state. Removing the audio prefab leaves the cooking rules intact.

Assets/Audio/Restaurant.mixer routes Music, SFX, UI and Ambience into Master. Exposed MasterVolume/MusicVolume/SFXVolume/UIVolume/AmbienceVolume parameters accept decibels. SettingsService applies linear 0–1 settings in Start, converting silence to -80 dB. Only supported mixer volume functionality is used on Web; no DSP effects or music are included. See [Unity 6 Web audio support](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-audio.html). Browser audio activation remains subject to a click/key gesture and physical Edge testing.

Seventeen original mono PCM16/22050 Hz WAVs cover nine placeholder cues: pickup, place, chop, fryer start, sizzle loop, cooking complete, plate, customer success and UI click. Reproduce with `node scripts/generate-placeholder-audio.cjs`. These are quiet synthetic test sounds, not production audio or licensed recordings. Decompress-on-load import favors short feedback latency; no speculative streaming system was added. Music/Ambience groups are available but have no content.

SettingsRepository separates a versioned JSON envelope from ISettingsStorage. Schema 1 contains only five audio volumes. PlayerPrefsSettingsStorage stores ThrownTogether.Settings and its previous value under .backup; on Web this is browser-local storage and can be cleared by browser policy/user action. Future restaurant saves must use a separate namespace/storage implementation. Version 0 is an explicit masterVolume-only migration fixture; loading it migrates in memory and writes schema 1 only on explicit save. Missing data uses defaults. Invalid data, unsupported versions and storage failures retain defaults and block writes to protect existing bytes. This is a settings foundation, not transactional restaurant persistence or cloud sync.

The development Web template opts into autoSyncPersistentDataPath so Unity synchronizes browser-local writes through its supported automatic path; PlayerPrefs.Save remains the explicit settings boundary. This removes Unity 6's manual-sync deprecation warning. See [Unity Web template configuration](https://docs.unity3d.com/6000.0/Documentation/Manual/web-templates-build-configuration.html). Release templates must preserve that option when persistent settings are exposed outside development diagnostics.

F3/DEV exposes development-only audio sliders and an explicit Save audio settings button. Changes preview in memory; unsaved values reset on restart. Diagnostic visibility is deliberately not persisted and still starts OFF. Tests use in-memory storage and never write the user's PlayerPrefs key. Development identity now includes build-config.json's developmentVersion, source commit and UTC build timestamp; direct release builds omit the diagnostic panel/template/stamp. build-info.json also carries developmentVersion. The development canvas records loader completion milliseconds in data-loader-ready-ms for repeatable browser inspection. FPS and the GC Allocated In Frame counter are diagnostic samples; unsupported counters explicitly report unavailable. Editor allocation samples include Editor work and cannot be substituted for Web measurements.
