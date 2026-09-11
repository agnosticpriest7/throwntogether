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

The measured pre-playtest baseline is in [WEB_PERFORMANCE_BASELINE.md](WEB_PERFORMANCE_BASELINE.md): 47.87 MB static output, approximately 60 sampled idle FPS in the available browser, cache-qualified startup measurements and explicit limits on Web GC visibility. No performance tuning or gameplay parameter changes were made.

F3/DEV exposes development-only audio sliders and an explicit Save audio settings button. Changes preview in memory; unsaved values reset on restart. Diagnostic visibility is deliberately not persisted and still starts OFF. Tests use in-memory storage and never write the user's PlayerPrefs key. Development identity now includes build-config.json's developmentVersion, source commit and UTC build timestamp; direct release builds omit the diagnostic panel/template/stamp. build-info.json also carries developmentVersion. The development canvas records loader completion milliseconds in data-loader-ready-ms for repeatable browser inspection. FPS and the GC Allocated In Frame counter are diagnostic samples; unsupported counters explicitly report unavailable. Editor allocation samples include Editor work and cannot be substituted for Web measurements.

### Xbox Edge browser-owned controls boundary
The current development shell supersedes the earlier custom focus/fullscreen helpers: it sets Unity loader disabledCanvasEvents to [dragstart], allowing the native canvas context menu, and retains only primary-click canvas focus plus passive focus-status/resize listeners. No document key suppression, gamepad-driven focus polling, pointer lock or page fullscreen request remains. A visible Xbox Edge hold Menu → Use game controls hint directs the user to the browser-owned mode. Runtime ChefInput mappings and WebInputFocus checks remain unchanged. The physical retest and investigation evidence are in DEVELOPMENT_NOTES.md; this change is not physical Xbox certification.

## 0.2.0 runtime additions
LocalCoopSession owns explicit per-chef device assignments and the optional second chef lifecycle. A joins an unused pad; the join press passes through the existing release gate to avoid accidental interaction. Dropped controllers do not transfer hands/items. Keyboard + pad is an explicit UI choice; two-pad mode assigns distinct pads. Restart/mode changes deliberately start a new solo session. Shared CounterStation/ProcessingStation operations are synchronous and reject occupied or busy slots.

RestaurantShift consumes a ShiftDefinition order array and fills available CustomerOrder seats. Completed seats are cleared and assigned the next ticket; two seats provide bounded concurrency. ServiceStation reserves the matching waiting seat and retains that destination during delivery, rejecting additional delivery attempts while occupied. ProcessingStation selects a compatible process from ApplianceDefinition while retaining the original fallback recipe. Practice remains a separate configured scene.

PcmWave reads validated PCM16 RIFF data and creates AudioClips with SetData. Web build preparation temporarily substitutes TextAsset bytes on AudioCue assets, removing compressed clip references from the deployed scene dependency graph, and restores authoring assets afterward. This avoids browser decodeAudioData for placeholder SFX without suppressing runtime errors or globally muting audio. PCM generation is lazy per cue variant and cached; native builds retain imported clips. Regression coverage includes WAV validation, runtime clip creation, two-controller isolation/reconnect/slot contention, and the complete six-order solo shift.

## 0.3.0 menu, readability and feedback
RestaurantMenu is installed by RestaurantHud and polls a separate menu InputActionMap before chef input. Y/Escape toggles; D-pad/stick/arrows navigate; A/Enter selects; B/Backspace returns. Start/Menu remains browser-owned on Web. The initial built player opens paused; Editor authoring remains unpaused. Time.timeScale is restored on close/destruction, gameplay is blocked through the closing frame, and chef actions must release before resuming. Mode/restart confirmations default to Cancel. Player 1 assignment is refreshed on resume for controllers first exposed while browsing the menu.

Settings schema 2 supersedes the schema-1 description above: it includes DisplaySettingsData (three text sizes, high contrast, reduced effects) alongside the five audio volumes. Schema 0/1 migrates in memory; an explicit save writes version 2. Unknown/invalid saves remain protected. Unsaved changes reset with the scene. No progression data is persisted.

IngredientDefinition.visualKind drives potato/mushroom silhouettes, raw/prepared/cooked meshes and GUI dish icons. Carryable reuses authored mesh/shader dependencies; source stations display a visual sample. StationPresentation creates a bounded set of cached blade/steam meshes, hidden when idle or reduced-effects is enabled. Effects never move gameplay objects or alter timers/colliders. Ticket text explicitly distinguishes waiting, delivering, eating and served; numeric progress and P1/P2 badges avoid relying only on color.

LocalCoopSession exposes assigned device names and connection events. P2 leave is rejected while holding an item; successful leave disables/destroys only the empty-handed second chef and preserves the ongoing order. Menu blocks joining, and reconnect retains existing ownership/items. Coverage includes controller menu navigation/cancel, pause and A-release isolation, shared processing contention, safe leave/rejoin, and settings migration/round-trip. Physical Xbox input and TV readability remain human checks.

## 0.4.0 clarity, practice and session architecture

Owner-approved overnight scope supersedes the 0.3.0 shift-variety deferral. No movement/reach/camera/layout/timer changes, new recipe, economy, employee, throwing or dishwashing system.

- `KitchenPresentation` adds visual-only mesh dressing from Carryable's explicit mesh/shader dependencies, keeping authored colliders and station/slot transforms authoritative. It replaces renderer appearance, not station behavior. Ground target borders, compact progress, and ready lights replace normal-play world labels. `CustomerPresentation` observes existing delivery/eating states; reduced effects also suppresses the original eating bob. No new customer phases or durations.
- `Carryable` keeps collider-free mesh construction and data-driven colors; larger visual scale and a two-tone plate rim improve recognition. `FoodIcon` draws corresponding plated fries/mushroom slices using a cached procedural disc texture. No imported art, runtime CreatePrimitive or browser audio decoder dependency was introduced.
- Every ordinary `CounterStation` accepts plate + cooked food in either order. Combination transfers the resulting plate to the counter and releases/destroys only the consumed food. Hands are empty after assembly; picking up the plate is a separate ordinary interaction. The old PLATING COUNTER scene objects are now COUNTER. ProcessingStation still owns processing input validation; it is not advertised as an ordinary assembly counter.
- `SessionOptions` is transient session configuration: 3/6/12 dishes, selected practice start. `RestaurantShift` cycles the existing authored recipe sequence up to TotalOrders, with at most two live seats and no expiry. Defaults stay six orders; no asset mutation or progression save.
- `PracticeGuide` is opt-in and observes real hands/counters/stations/order state. Checkpoints spawn the needed existing payload into P1 hands in a fresh practice scene. They do not modify recipes or station rules. Full guidance covers supply, processing, ordinary-counter plating, pickup, and service. It follows P1; co-op is still usable but is not a coordinated two-player tutorial.
- `SessionSummary` records successful chef interaction events separately for P1/P2. Processing counts starts, not subsequent pickups. Elapsed time uses scaled gameplay time and freezes on completion. No scoring, ranking, persistence or claim of equivalent task effort.
- Menu additions reuse the existing action map, confirmation flow and release gates. Eight main rows fit the fixed logical canvas. No browser Menu interception or gamepad-to-keyboard translation.
- `check-web-build.cjs` inventories/validates deployable files, hashes and source identity. Pipeline writes and rechecks build-manifest.json. `restore-web-deployment.ps1` only accepts a full commit from freshly fetched gh-pages ancestry, validates a separate snapshot, and optionally appends a new deployment commit. It never switches canonical branches or force-pushes. Old snapshots without manifests get one generated from their Git-verified bytes. See README for the known-good reference and commands.

Coverage adds both ingredients x both assembly orders x all ordinary counters, invalid raw/cut assembly without item loss, shared-item contention, paused join rejection, 3/6/12-order completion/no expiry/data immutability, all guided checkpoint completions, collider-free station displays, contribution counting, and Web manifest tampering/missing/source/compression rejection. Existing controller, reconnect, cooking, settings and PCM audio tests remain.

Publication preserves byte-for-byte artifacts: both isolated publisher and restoration repository set core.autocrlf=false before staging. This matters on Windows because a build manifest made before Git normalization would otherwise disagree with the files actually deployed. Verification includes extracting the committed deployment and checking its manifest, not merely checking the pre-push working files.

## Background music — 0.4.1
The two owner-supplied songs, First Service and One More Order, alternate continuously across menus, practice/shift switches and restarts. Music defaults to 60% for new settings; existing saved preferences are preserved. Menu > Audio controls Music and Master, with the existing explicit Save action. Web streams the original OGG Vorbis files from StreamingAssets/Music through a dedicated HTML audio voice, avoiding Unity's previously failing compressed Web AudioClip conversion. Passive pointer/key listeners retry browser-blocked autoplay without capturing controls or showing alerts. A click on the game may be needed before the browser permits music. Hidden tabs pause music; returning resumes when permitted. Missing/unsupported music logs a warning and leaves gameplay usable. No music is routed through the short-SFX PCM conversion or voice pool. Native/Editor playback uses one persistent streaming AudioSource; effective gain follows Master multiplied by Music. No gameplay changes.

## Kitchen variety — 0.5.0
KitchenLayoutDefinition stores explicit ordered positions for nine station anchors and both chef spawn locations. Scene-owned KitchenLayout references transforms and definitions directly, applies the transient SessionOptions.Kitchen choice before normal runtime startup, and preserves camera/room/movement parameters. Both authored scenes share the three definitions; alternative choices do not duplicate entire scenes. AuthorKitchenVariety is an explicit Editor authoring tool, never a runtime regeneration path. Session changes require the existing reset confirmation; no progression is saved.

IngredientDefinition.platingState defaults to Cooked, preserving potato/mushroom behavior. Tomato specifies Cut; ItemPayload.CanPlate compares the ingredient's explicit ready state. TomatoSalad has one PrepTomato step, and fryer appliance lists exclude tomatoes. Recipe matching remains ingredient + state + plate. Visuals and ticket icons include a tomato silhouette. PracticeGuide selects the source matching the order and supports cold plating without recommending the fryer. FirstShift mixes the existing dishes with salad; customer rules/timers are unchanged.

Co-op help reuses the existing menu inputs and paused join gate. ConnectionHelp distinguishes controller ownership and retained items, and P2 automatic reconnection adds an action-release gate. Tests include capsule-grid walkable reachability for both spawns and all stations in all layouts, preserved camera/speed/reach, all three ingredients plated in both orders, salad state rejection and guided service, and replacement-controller item ownership. These are automated checks, not physical controller or subjective comfort verification.

## Main menu and recipe book — 0.5.1
RestaurantMenu now has a guarded frontend state: shipped players open at Title, cannot dismiss it into the hidden initial scene, and explicitly choose a kitchen for a fresh shift or practice session. Existing scene-owned layout data supplies level names/descriptions; no duplicate level scenes are needed. Return to main menu requires confirmation. Editor Play remains direct for authoring. The keyboard-P1 setup choice survives scene loads; loading from the menu applies an action-release gate to prevent the selection press using an item.

Resources/RecipeBook is an explicit catalog of the three live RecipeDefinition assets. The pause/main-menu book renders preparation steps and durations from those assets, with ordinary-counter assembly, separate plate pickup, and service instructions. Tomato salad omits frying. Browsing uses the existing paused menu and controller navigation; it does not mutate recipes, held food, timers, or input mappings.

## Hands-on kitchen work — 0.6.0
ProcessingStation.requiresAttendance marks the prep board. WorkAttendance claims a job for one chef after Use, captures its movement revision/position, and stops contributing time after movement or controller disable. Work progress remains at the station; an empty-handed chef can press Use to resume an abandoned job. Only one attendance claim can be active per chef, and a running job rejects another worker. Fryers retain unattended timing. Cutting remains 1.5 seconds; washing is an initial 3-second development value.

WashingStation accepts only dirty plates, retains progress on interruption, and returns the same clean plate for pickup. CustomerOrder returns its finished dish once to an authored DishReturnStation queue before seat reuse; the queue owns unheld returned objects, with normal CarrySlot ownership after pickup. Restart destroys scene-owned dishes and resets the rack. The existing unlimited clean-plate source remains available: this batch does not add finite inventory or shortages. KitchenLayout now holds twelve anchors (the original nine plus lettuce, sink, and return rack); all three kitchens share these new station types.

ItemPayload retains the first ingredient plus serialized IngredientPortion additions and explicit dirty-plate state. RecipeDefinition requires an exact set of prepared portions, independent of assembly order. IngredientDefinition.combineWith permits the requested tomato/lettuce combination, rejects duplicate portions and unrelated mixtures. Garden Salad replaces the former one-ingredient salad in the existing asset (GUID retained), tickets, recipe book and guided practice. Both ingredients must be Cut, never fried; partial plates can be stored but cannot fulfil an order. Shape rendering includes green lettuce, both salad components, and stained dirty plates.

Title lists Tutorial, Quick Play, Career, Trials, and Endless. Only Quick Play is enabled; unavailable modes are grey and ignore activation. Recipe book, co-op setup, settings, and existing in-game practice tools remain available. No career progression or additional mode logic is included.

## Modular chefs and wardrobe — 0.7.0
ChefVisual is a reusable Blender-authored visual prefab under the existing Chef controller prefab. ChefAppearance selects modular body/clothing/face/accessory meshes, applies material property colors, and poses the shared skeleton for walking/carrying. All three body builds preserve the existing motor, collision, carry anchor, movement and interaction reach.

ChefWardrobe stores independent P1/P2 appearance data for the current application session. RestaurantMenu opens the wardrobe after kitchen selection and before scene loading; the pause menu can reopen it. LocalCoopSession applies P2's selection on join. ChefWardrobePreview owns an isolated layer-31 render-texture stage and releases it on scene unload. CharacterReview is an authoring scene excluded from the explicit playable build list. Editable Blender sources remain outside this repository in the sibling Character Art folder; runtime FBX, materials, prefab and metadata are versioned here.

## Approved kitchen art — 0.8.0
StationArt references an authored visual child using the approved Blender modules in Assets/VisualDirection/Prefabs. Legacy cabinet/worktop renderers are disabled while every original collider, carry slot and station transform remains authoritative. KitchenPresentation skips duplicate authored geometry and retains ingredient displays, service/rack dressing, per-player target borders, ready indicators and working feedback. A production-only sink mesh omits the prototype demonstration plate; live dishes remain governed by existing CarrySlot ownership.

ApplyKitchenArt is explicit Editor authoring with a before/after collider-property and world-transform comparison. It authors both practice and shift scenes; all three KitchenLayout definitions remain unchanged. Kitchen tiles and dining planks are combined into two-submesh assets per floor at the exact existing footprint. Short modular cutaway walls retain the old collision boundaries. Camera pitch is 55 degrees, orthographic size 8.1, aimed at (0,0.3,1): the approved direction framed for the existing larger room and HUD. No chef scale, reach, movement or timing changes. The visual review scene remains separate and excluded from the playable build list.

## Warm floors and First Service circuit — 0.8.1
AuthorFirstServiceFloor is explicit Editor authoring, not runtime generation. It updates the existing floor mesh assets in place, preserving GUIDs and exact collision footprints. Each combined floor has seven shared-material submeshes (five restrained face colors, chamfers, recessed joints), no added colliders or textures, and no per-tile GameObjects. The same floor style is shared across layouts. Kitchen0 alone stores the new perimeter/pantry/island positions and safe spawns; KitchenLayout still moves the original station roots and their visuals/slots. Kitchen1/2 and all runtime gameplay code remain unchanged.

## Refined kitchen kit — 0.8.2
AuthorKitchenRefinement imports the Blender-authored ModelsV2 into prefab wrappers that preserve the imported axis conversion below a Unity Y-up root. MaterialsV2 uses a shared URP palette. Explicit guarded authoring swaps StationArt.visual in the two playable scenes and compares original collider properties/world transforms before saving. There is no runtime scene regeneration or collision change. StationArt.includesPantryDisplay skips the old procedural source-crate/previews; each pantry prefab is a static, non-interactive mesh containing its ingredient display. Actual source/held food still uses the existing item definitions and Carryable. Other station effects, markers, service dressing and plate stacks remain active. Counter cap variants are available for future authored layouts; no additional kitchen or progression is introduced.

## Dining and room presentation — 0.8.3
AuthorRestaurantRoom is guarded explicit authoring. It imports seven Blender modules with shared opaque URP materials, replaces only table/chair renderers, and replaces previous visual-only wall instances. Original table/chair/boundary colliders and CarrySlots remain authoritative. Chairs retain the customerVisual parent and thus existing visibility/bob behavior; no new seating logic. Restaurant room art owns three window modules, low boundaries, a decorative closed entrance, two sconces and two shadowless point lights (range 4). The existing directional soft-shadow light and ambient colors are adjusted; the camera/render pipeline remain unchanged. Window panes are opaque for predictable Web rendering. Original physics and carry-slot signatures are compared before scene save.

Service presentation (0.8.4): AuthorServicePresentation imports two original Blender overlays using shared Kitchen/MaterialsV2 materials. It adds collider-free Service presentation art children to existing ServiceStation and DishReturnStation roots, preserving layout transforms, slot positions and all behavior. StationArt.includesServiceDisplay opts out of the old runtime shelf/bell/tray geometry only; KitchenPresentation still creates per-player target borders. The serving mat, bell and low rail carry no gameplay components. Actual delivered food and returned dirty plates remain the sole live items. ArtSource/ServicePass holds editable source and metrics; generation is guarded against blind reruns.

Food presentation (0.8.5): Carryable keeps all ItemPayload and CarrySlot contracts. Its visual child uses a uniform 1.12 scale and state-dependent primitive arrangements; ownership and carry transforms are unaffected. Two serialized reusable meshes (PlateProfile and MushroomSliceCap) improve silhouettes without runtime mesh generation. AuthorFoodPolish is guarded, isolated Editor authoring plus unsaved camera-review setup; the Carryable prefab references these meshes explicitly for player inclusion. Plate presentation retains a primitive fallback; the shipped prefab is tested for both authored references. Combined salad drawing is order-independent. No new texture/material assets or colliders.

Customer/HUD candidate (0.8.6): authored CustomerPresentation components reference disabled ChefAppearance rigs under customerVisual. Initialize applies fixed NPC choices independently of ChefWardrobe, caches bones and tints torso/trousers/shoes through property blocks. ApplyPose observes phase and reduced effects without changing order state or item ownership. CustomerOrder retains its two-second eating timer but no longer bobs the root shared with chair colliders. KitchenPresentation reuses authored presenters, retaining its primitive fallback for unconverted scenes. OrderBubbleLayout supplies safe-area geometry and phase labels; RestaurantHud uses existing FoodIcon assets and accessibility settings for active-seat cards. No patience, navigation or new gameplay system.

0.8.8 finishing art: AuthorRestaurantDetails imports seven static Blender modules into reusable prefabs and a scene-owned Restaurant finishing details root. Perimeter decoration and Exterior framing are visual only: no MonoBehaviour gameplay paths, collision or added real-time lights. Existing RM/KT materials are reused with three DT materials. Authoring validates original collider/slot/camera signatures; camera captures are unsaved. No runtime asset generation or external dependencies.

0.9.0: SourceStation plate supply uses a scene-local capacity of five, an issued-object set and returned-plate stack. Ingredients retain their previous supply policy. Returned clean plates are hidden/parented to the supply until reused; their identity and total issuance are preserved. KitchenPresentation renders the count as five cached plate meshes, hiding legacy decorative stacks. PracticeGuide Serving obtains its plate through SourceStation. Scene reload restores stock; no persistent inventory/economy introduced. KitchenDiningDivider is an authored prefab with intentional wall/held-open-door collision and pass/door openings. KitchenLayoutDefinition retains explicit station positions across all variants; serving policy is unchanged.

## Restaurant day and persistent purchases (0.10.0)

`RestaurantShift` selects fixed-order practice or the default timed `RestaurantDay` from `SessionOptions`. `DayServiceDefinition` explicitly configures duration, arrivals, patience, dining route, bonus window, offers and actor prefab. `RestaurantDay` owns the clock, queue, seat reservations, closing and a single settlement ID. `DiningTable` is the manual interaction/payment boundary; `CustomerOrder` retains its legacy practice lifecycle but leaves the physical plate dirty on a manually served table. `DiningWalker` follows dining-only waypoints; `DiningServer` transfers the same Carryable between pass, its CarrySlot and a matching table, reserving a target and rechecking it at delivery.

`RestaurantAccount` uses copy-before-write commits through ISettingsStorage, separately from audio/display settings. Its schema-1 RestaurantSave stores cash, sequential/active/settled day IDs and stable purchase IDs. It rejects future/corrupt data, stale or duplicate settlement, insufficient funds and duplicate purchases. Restart creates a new day ID, invalidating any old payout. PlayerPrefs is a local browser/device save, not cloud storage or a transaction service. A backup string is retained on writes. Future schema migrations must explicitly transform known versions; unknown schemas must not be overwritten.

Offers reuse RestaurantUpgradeDefinition and EmployeeRoleDefinition data. Counter/fryer offers reference reusable gameplay prefabs and per-kitchen bay positions, spawned at day initialization before presentation dressing; the speed offer changes only runtime processingSpeed on fryers. Ownership applies next day. Purchase transactions are closed during service. The starter server transports only, leaving cooking, clearing and washing to players. No inventory costs/wages or free-placement construction are implemented.

## Service quality of life (0.11.0)
SourceStation now exposes finite-pool TakeCleanPlate/ReturnCleanPlate operations shared by player plating and staff. In-hand plating reuses ItemPayload.CanPlate, transfers prepared food into an issued plate, and preserves pool identity. WashingStation owns queued CarrySlots, one active plate and mutually exclusive human/employee work claims. It never creates replacement dishes; employee washing shares its normal duration/progress. KitchenDishwasher follows bounded collision-checked waypoint routes to the sink and supply, yielding to an active human wash. It takes no table-clearing or return-rack tasks. Dishwasher ownership is another stable schema-1 purchase ID, so existing accounts need no migration.

DiningTable exposes waiting/patience/departure state; RestaurantDay owns the timed unhappy departure and seat-release lifecycle. Patience is data-driven through DayServiceDefinition.seatedPatience and rendered below the existing order bubble. Server reservations are canceled at departure and invalid deliveries retain the physical dish with the server.

ChefAppearance's old selection indices are stable. Additive authored mesh parts under existing rig bones provide new hair/face/clothing options, with expanded shared color palettes. CustomerPresentation derives civilian looks from an arrival seed rather than player wardrobe state, reapplies that seed to seated art, and enables NPC-exclusive scarf/vest details. AuthorQualityOfLife is guarded additive authoring, not an automatic asset regeneration path. QualityOfLifeReview renders an unsaved review stage.

### Service growth (0.12)
`DayServiceDefinition` owns per-completed-day demand, sidewalk endpoints and the busser offer. `RestaurantAccount.completedDays` advances only on successful atomic settlement; its optional schema-1 field preserves old cash/purchases. `RestaurantShift` activates an authored, initially inactive two-table group only when the dining capacity purchase is owned. `RestaurantDay` registers all active physical table roots and routes customers through column-specific aisles; doorstep waiting begins after sidewalk travel. Departure frees the seat at the doorway while the walking visual continues offscreen.

`DiningBusser` revalidates dirty-table items, transports them to `DishReturnStation`, and yields to player clearing. `KitchenDishwasher` uses the collision-checked kitchen route for rack → sink → clean stock, sharing the existing ownership and attendance rules. No plates are spawned by employees. Both hires remain optional and next-day purchases. `RestaurantAudioFeedback` discovers active stations/chefs and maintains per-station processing loops; `AudioPlayback` reserves loop voices across pause. New original PCM work effects follow the existing safe Web PCM build path. The bottom controls panel is gated by `PracticeGuide.Active`.

### Trash and idle dining staff (0.12.1)
`TrashStation` uses the normal chef interaction path and never destroys an issued plate: plated food becomes dirty, unplated food is released/destroyed, and empty plates are rejected. `RestaurantDay` tracks waste fees; `RestaurantAccount.Settle` accepts an optional fee amount and atomically pays max(0, meals + bonuses - waste). Practice has no paid day and no fee. `DayServiceDefinition` owns the $1 cost and separate server/busser standby points. Dining employees distinguish standby from work destinations, preventing remote pass/rack transfers after moving their idle positions.


## Between-day presentation — 0.13.0
DayPresentation is a scene-local visual coordinator attached by RestaurantHud. It observes RestaurantDay.Closed, pauses scaled simulation during its 2.5-second closing/opening transitions, and interpolates camera orthographic size, ambient tri-light colors and the scene's existing light colors/intensities using smoothstep. No restaurant/account or station state is mutated. Authored service values are captured per scene and restored exactly. Night remains active while the existing management menu is open; that menu alone gains a translucent backdrop and fade.

Next-day selection retains the existing paid-day guard, SessionOptions and scene loader. A one-use handoff initializes the next scene at night before rendering; duplicate selection is blocked while the menu fades. The new day and purchased objects still initialize through RestaurantDay.Begin, with elapsed time frozen at zero until opening completes. Input uses the existing menu blocking and action-release gates. Unscaled transition time allows the presentation to run while management/gameplay are paused. No interpolation occurs in non-career play.


## Startup presentation — 0.13.1
Explicit build configuration and Editor build order start with Boot, then MainMenu. These lightweight scenes use StartupSequence, CanvasGroup fades and replaceable RawImage assets. No new external packages are required; the runtime assembly references the already installed uGUI assembly. The title gate hands off to the existing RestaurantDevelopment-hosted RestaurantMenu front end via a one-use explicit request, also supporting Editor startup from Boot. The final transition temporarily blocks menu selection and fades a black IMGUI veil before releasing input. Normal menu options, co-op setup and gameplay scene loading remain authoritative and unchanged.


### Daily menu and category storage (0.14.0)
`DailyMenu` resolves the `RecipeBook` catalog against `RestaurantAccount` ownership and selected stable recipe IDs. `RecipeDefinition.requiredPurchases` gates the catalog; `IngredientDefinition.requiredPurchase` gates refrigerator retrieval. `RestaurantDay` cannot start without three resolved dishes and snapshots those definitions for customer spawning and revenue. `RestaurantMenu` hosts selection in the existing first-launch and between-day flow. Schema 1 gains the optional `selectedMenu` field: old saves migrate to base recipes, explicit empty drafts remain empty, unknown future schema remains read-only. Purchases and draft changes still use atomic account writes.

`FoodState` appends Griddled/Grilled without changing existing enum values. Appliance ScriptableObjects enumerate valid processes; recipe steps identify ingredient, input, output, duration and display station. All component states must exactly match a recipe before service. Ordinary-counter assembly accepts a subset of a catalog recipe, rejects duplicate ingredients and preserves a single physical plate. New food shapes and multi-component icons use existing low-cost meshes/materials.

`IngredientStorageDefinition` groups definitions; a `SourceStation` holds a category and its reusable item prefab. `ChefInput` owns its own selector, selected index, remembered choices and assigned-device navigation. No global pause is introduced by storage. Dispensing revalidates ownership, range and free hands; focus/disconnect/menu closing cancels the chooser and action-release gating prevents duplicate retrieval. ProduceRack/Refrigerator/Griddle/Grill are reusable prefabs under `Assets/Data/MenuExpansion`. Existing layout anchor transforms are retained, with vacated crate anchors available for the new predefined appliance bays.

`RestaurantDay` tracks distinct served recipe IDs. Variety is 5% of base revenue (integer floor, cap $15), requiring both four menu entries and four distinct served dishes; it joins the existing atomic once-only settlement. No ingredient cost, inventory, new staff role or expansion-system replacement is introduced.


### Saved kitchen arrangement (0.15.0)
KitchenFurniture registers stable base-anchor IDs and owned purchase IDs on RestaurantDay initialization. Optional FurniturePlacement records in schema 1 store kitchen index, bay and quarter-turn rotation; ownership remains authoritative. Saved transforms apply before staff initialization/service, and all draft edits reuse the existing RestaurantMenu pause/input gate. Rendering uses lightweight projected numbered buttons with the gameplay camera. Validation uses existing colliders and KitchenStaffRoute; rejected moves roll back both pieces, and cancellation/scene unload tolerates destroyed objects. No production scene geometry is reauthored by the editor.

### Controller and arrangement repair (0.16.0)
Automatic first-pad assignment runs before menu focus gating; explicit keyboard-P1 remains opt-in. Real gamepad A may focus a visible canvas without intercepting Edge Menu or text entry. Wardrobe is optional. All kitchen station anchors except trash register with KitchenFurniture, including pass/rack; staff routes target actual station positions. Twenty bays include two inside-right service bays. Modular station assets should fit a 1.9 m horizontal footprint, with colliders and routes validated at each rotation. Arrangement saves atomically include the once-per-break fee marker; first setup is free, subsequent changed saves cost 10 once per settled day. Save and Cancel participate in the controller navigation graph. Recipe pictures are rendered from actual Carryable dish geometry by BakeDishIcons and cached through FoodIcon.

### Repeat equipment and staff training (0.17.0)
Optional schema-1 equipment records store unique instance IDs, offer IDs and actual paid prices; old purchases remain compatible. Ownership derives from either legacy purchases or remaining instances. Furniture IDs reference each instance, and station purchase/payment/initial placement commit together. Resale removes only that instance and its saved placement records. KitchenFurniture checks actual colliders/spawn/routes before payment, instantiates owned copies, and keeps the fixed starter stations. Additional plate count extends SourceStation's existing finite pool. Training records are per role with three bounded levels; staff movement/washing reads the multiplier without changing player or customer movement.
RestaurantMenu retains one controller action map, with Day Complete as the parent of management pages, category tabs and selection-following lists. Daily-menu primary actions are pinned outside the scrolling recipe list.
