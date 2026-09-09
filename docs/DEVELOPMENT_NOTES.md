# Vertical Slice #1 — first service

The authored `Assets/Scenes/RestaurantDevelopment.unity` scene is the Web entry in `build-config.json`. Bootstrap remains unchanged. Greybox only: one chef, one seated customer and one Fries order.

## Controls and complete loop

- Move: left stick, WASD or arrow keys. Movement is screen-aligned with immediate stopping; CharacterController prevents walking through counters.
- Use/pick up/place: controller south button (Xbox A), E or Space. Face a nearby station; its label highlights and the bottom prompt describes the action.
- Restart the slice: R or controller Start. This resets the entire scene; it is a prototype retry control, not a save system.

Take a potato from the back-left supply. Place it at the central prep island; wait 1.5 seconds and take the cut potato. Place that in the fryer; wait 5 seconds and take the fries. Put fries on the plating counter, take a clean plate from the front-left source, then interact with the fries to plate them. Carry the plated fries to service pickup. The dish automatically transfers to the customer after a short delay; the customer eats and the HUD confirms completion. Plating also works with a plate on the counter and fries in your hands. You can combine an empty plate with ready fries directly at the fryer. Spare and plating counters provide temporary item storage.

## Small reusable foundations

- `ChefController` handles CharacterController movement, proximity/facing selection and interaction. `ChefInput` owns a per-chef Input System action map with optional device binding. No global single-player input singleton; a later join flow can assign devices to separate chef instances.
- `CarrySlot` enforces one item and transfers ownership without duplicating it. `Carryable` displays an `ItemPayload`; raw potato, cut strips, golden fries and plates use different primitive visuals.
- `IngredientDefinition`, `ProcessingRecipe` and `DishRecipe` ScriptableObjects live in `Assets/Data/VerticalSlice`. Prep and fryer share timed `ProcessingStation` logic driven by separate recipe assets. Raw potato is rejected by the fryer; busy stations cannot release their contents.
- `SourceStation` supplies items, currently unlimited. Its supply policy is the future inventory boundary. `CounterStation` supports pickup/place and combining cooked food with a clean plate.
- `ServiceStation` validates `CustomerOrder` against a dish recipe and performs a short placeholder delivery. The order progresses Waiting → Delivering → Eating → Complete and rejects duplicate service. Employee/server AI is absent.
- Chef and carryable prefabs live in `Assets/Prefabs/VerticalSlice`; materials are shared assets. The authored scene contains layout and component references, not a runtime monolithic scene generator. The Editor creation tool is guarded against overwriting an existing restaurant scene.
- Minimal HUD shows order status, nearby station labels/progress, contextual action, carried item and controls. No management UI.

## Known limits

One player/order/recipe only. No throwing, burning, purchases, finite inventory, persistence, dirty dishes, money, reputation or employees. Automatic delivery is an explicit placeholder. Controls have not been physically tested on an Xbox controller or Xbox Edge. TV/couch readability and movement feel require human feedback. Restart discards all slice progress. The shared Input System maps permit future pairing but Player 2 gameplay is not implemented. The infrastructure-era URP warning remains parked unless visible rendering is affected.

## Automated and Editor validation

Live Editor tests: EditMode 3/3 and PlayMode 4/4 passed, including the existing smoke tests. Coverage includes authored recipe progression and timing, raw-potato rejection by the fryer, plate requirements, single-item pickup/place, timed processing, delivery/completion, movement/stopping/range, and synthetic Input System gamepad movement plus south-button pickup. The synthetic gamepad test is not physical controller validation.

The full loop was also exercised through Unity MCP in Play Mode with the chef positioned at each station: actual station interactions and timers produced plated Fries, delivery, eating and Complete. The running scene and visible success HUD were captured and inspected. No new Console errors remained after these checks. The deployment pipeline reruns both suites on the committed snapshot before building and publishing; build provenance remains available at build-info.json.

Batch validation exposed Unity 6000.6 compiler helpers retaining response-file handles between processes. The pipeline contains each batch Editor and its descendants in a Windows job, releases that job when the stage exits, and regenerates only Builds/Workspace/Library/Bee/artifacts/rsp before each stage, retaining larger caches. Synthetic input explicitly advances input processing and the production input handler with fixed steps, restoring its temporary focus settings afterward.


Final committed-snapshot validation passed EditMode 3/3 and PlayMode 4/4, then built Web successfully from `8ee1b2d399a1ffff293d7f55e7f6f99da9f4ef25`. Deployment commit `de36ced8aa5c5b588f7756b37d021d6dfff3ed9d` on gh-pages completed successfully. The public restaurant rendered correctly; loader, framework, data and WebAssembly assets returned HTTP 200 below /throwntogether/, with application/wasm for WebAssembly. Browser Console showed only the previously parked URP upscaling warning and no errors. No physical controller, Xbox Edge or TV testing was performed. Refresh with a build query if an already-open tab retains the older Bootstrap page.
## Exact human playtest checklist

1. Does movement feel responsive?
2. Is the elevated camera comfortable on a TV?
3. Is it obvious where potatoes come from?
4. Is pickup/place interaction intuitive?
5. Does prepping feel responsive?
6. Is fryer progress readable?
7. Is plating understandable?
8. Is service pickup obvious?
9. Is the customer/order readable from couch distance?
10. Does the complete potato-to-fries loop feel satisfying?

Also try raw potato at the fryer, unplated fries at service, interacting while hands are full, and restarting after completion. Record actual platform, input and findings in PLAYTEST_FEEDBACK.md. The public URL is https://agnosticpriest7.github.io/throwntogether/.

## Safe foundation follow-up

The data and diagnostics foundations preserve the first-service gameplay. See TECHNICAL_ARCHITECTURE.md for the six definition families, compatibility migration and proposed optional Player 2 coordinator. No second chef, new live recipe, progression system or Vertical Slice #2 was added.

Diagnostics: the detailed overlay starts OFF. Toggle with F3 or click the tiny DEV/version strip at the bottom right. The strip shows the deployed commit and detected gamepad count; the expanded panel includes commit/time, active input device, gamepad names/IDs, target, held item/state and FPS. Gameplay mappings remain WASD/arrows/left stick, E/Space/A and R/Start. Diagnostics are compiled out when -developmentDiagnostics is omitted from the direct build invocation.

Additional physical playtest checks: confirm Xbox Edge shows the expected commit; press a controller button if no pad is initially detected; confirm the gamepad count and last-active-device display; disconnect/reconnect the controller and verify keyboard fallback where available; run the existing ten-question checklist above. Neither physical controller, couch readability, Xbox Edge nor second-player behavior is implied by automated tests. No subjective feel changes were made.

Regression additions cover data compatibility and exact times, plating by food state, wrong ingredients, invalid service, restart, and one chef with an assigned gamepad ignoring an unassigned gamepad. The existing full loop remains the acceptance test. Employee capabilities, campaign balance, upgrade effects and future join/restart policies remain deferred design questions, not implemented features.

Live foundation verification: EditMode 8/8 and PlayMode 6/6 passed. Unity MCP exercised the existing timed loop through customer completion with exactly one chef and diagnostics initially closed. Console errors: zero. The committed-snapshot pipeline reruns both suites before publication.

## Web / Xbox Edge readiness audit

Input System 1.20.0 is active (new input backend). Its installed WebGLSupport maps browser devices with mapping="standard" to Gamepad; non-standard devices become Joystick and are not bound to chef controls. The existing left-stick/buttonSouth bindings support the standard Xbox layout. Player 1 currently accepts all mapped pads through the documented solo fallback, not exclusive pairing. No remapping or second player was added. Browser detection can require activating the page and pressing a controller button; a missing pad before interaction is not proof of a hardware failure.

The existing F3/DEV panel now distinguishes browser Gamepad API names/mapping from Unity gamepads, labels each mapped pad's Player 1 eligibility, reports last active device, live canvas/document focus, and last Unity controller connection event. Solo fallback naturally accepts a newly discovered device after reconnection; explicit future device pairing still needs its own reconnect policy. The API can be unavailable/blocked and reports that state without throwing.

Development builds use Assets/WebGLTemplates/Development: a focusable canvas, focused-canvas arrow/Space scroll suppression, focus prompt/button and optional Fullscreen button. A standard pad's A rising edge can focus the canvas only while the page is already active; it cannot activate an inactive browser tab. Web input is disabled without visible-document/canvas focus; held interaction/restart buttons must be released after focus regain. Browser shortcuts, Tab and Escape remain available. No gameplay mapping, movement parameter or processing time was changed.

Resize/fullscreen retain the original 960:600 (16:10) render aspect through letterboxing, including portrait/ultrawide windows. The camera itself is untouched. Unity matches render-target size to canvas CSS size and native devicePixelRatio, preserving high-DPI scaling. The shell does not disable browser zoom. Fullscreen is a user-initiated browser request; unsupported/denied requests show a message rather than failing the game. Omitting -developmentDiagnostics selects Unity's ordinary release template and removes diagnostic UI/helpers.

Hosting remains relative-path HTTPS GitHub Pages with uncompressed hashed data/wasm, no worker threads or cross-origin isolation requirement, and no Unity data cache. The existing URP upscaling warning is parked; new runtime errors are not accepted. Node browser-shell tests run before Unity tests in the pipeline (Node.js 20+ required). Automated tests cover aspect geometry, focus predicates, scroll keys, standard A focus eligibility, Unity focus/input suppression, release-after-focus, and simulated controller removal/replacement. These are synthetic tests, not physical Xbox or Edge certification.

### Short physical Xbox / Edge checklist
1. Refresh and confirm the DEV build ID; focus the page, press A, and open F3/DEV. Record browser controller name/mapping, Unity detection and P1: yes.
2. Complete potato → prep → fryer → plate → customer with left stick/A. Verify arrows/Space do not scroll if testing keyboard fallback.
3. Leave/return to the tab; focus the game, release held buttons and resume. Check Focus: YES and no stuck movement or accidental action.
4. Disconnect/reconnect the controller, press a button, and confirm detection/P1 eligibility and a connection event before resuming.
5. Enter/exit fullscreen, resize/zoom Edge and check the whole room, DEV panel and controls remain visible on the TV. Record console errors and actual Xbox/Edge/controller versions.

Physical controller, Xbox Edge, TV scaling and platform-specific fullscreen permissions remain unverified until that checklist is performed.

References: https://developer.mozilla.org/en-US/docs/Web/API/Gamepad_API/Using_the_Gamepad_API and installed InputSystem/Runtime/Plugins/WebGL/WebGLSupport.cs. Browser API detection and Unity mapping are intentionally reported separately.

Live audit verification: 8 PlayMode tests passed, including synthetic focus/reconnect checks. Unity MCP completed the original timed cooking/delivery loop; Console errors remained zero. The deployment pipeline runs browser-shell tests plus all EditMode/PlayMode tests on committed source before Web publication.

Hosted audit result: source d906a6452a30e33c20c717f59bd6d52a99488d52 passed 4 browser-shell, 8 EditMode and 8 PlayMode tests, built Web and deployed as gh-pages 8a867f555b6b605c51aa1eb46e3446dc5a1c9322. The available in-app browser loaded the restaurant and diagnostics, showed Keyboard as last active input, reflected canvas focus loss/regain, kept scroll offsets at zero after arrow/Space input, and entered/exited development fullscreen. DOM geometry preserved 16:10 at 800x1000 and 1600x800 viewports; the normal high-DPI canvas backing matched the browser scale. Temporary viewport overrides were restored. Visual TV scaling and real Xbox Edge permissions remain physical-playtest items. Hosted loader/framework/data/wasm/helper requests returned HTTP 200 (wasm MIME application/wasm). No browser/runtime or Unity Console errors; only the previously parked URP upscaling warning. No scene, prefab, cooking data or movement-controller changes were made.

## Final pre-playtest foundation batch

Added a reusable five-group audio mixer/prefab, nine cue assets with seventeen original generated placeholder WAVs, and versioned local volume settings. No music, progression save or new gameplay content. F3/DEV retains its original toggle and adds audio sliders plus explicit Save; diagnostics still start closed. Confirm browser audio after a click/key gesture, check pickup/prep/frying/plate/success feedback, mute Master/SFX/UI independently, save and refresh to check persistence. Sound quality/balance and Xbox/TV playback remain human checks; these sounds are placeholders.

Development identity includes 0.1.0-dev, commit and UTC build time. See TECHNICAL_ARCHITECTURE.md for settings migration/protection and Web counter limits. The narrow hygiene review retained authored scene/data/prefab organization, existing source-station supply and automatic service placeholders. New feedback uses explicit scene references and does not select or mutate gameplay outcomes. No speed, camera, range, timing, layout, customer behavior or control changes; Vertical Slice #2 remains unstarted.

New regression coverage checks all five settings values round-trip, invalid/future schemas remain untouched, version 0 migration is explicit, normalized levels map to mixer dB, missing clips are harmless, and every authored exposed volume can be applied/restored. Existing cooking/input/restart coverage remains in the full suites. Performance and final deployment measurements are recorded below after committed-build verification.

Live verification: EditMode 16/16 and PlayMode 10/10 passed. Unity MCP exercised pickup, timed prep, timed frying, plating, delivery and customer Complete; the fryer loop stopped after cooking. Diagnostics started closed, mixer settings applied successfully, and Console errors remained zero. Runtime state was discarded on leaving Play Mode.

Final publication: source b4460f6c92a288e11645aab18ea0c87548272904 passed 4 browser-shell, 16 EditMode and 10 PlayMode tests and deployed as gh-pages 819d6a46fa0b6c2f0db30af6405aafc06497a58c. Public identity and the restaurant/DEV/audio UI were verified. SFX settings survived browser refresh and were restored afterward. Automatic Web filesystem synchronization removes the deprecation warning found during save verification. No new Unity or browser/runtime errors; the existing URP warning remains parked. See [WEB_PERFORMANCE_BASELINE.md](WEB_PERFORMANCE_BASELINE.md) for exact output sizes, approximate 60 FPS and measured load/cache/GC limitations.

Human checks remaining: perform the short Xbox/Edge checklist above, activate browser audio with a click/key if needed, confirm each placeholder cue including sizzle stop, and assess volume/TV audibility. No physical controller or audible balance testing was performed. All foundation work stops here; no Vertical Slice #2 or additional gameplay milestone is started.

## Xbox Edge native game-controls retest

Kyle's physical Xbox test reported Pads: 0, D-pad apparently arriving as arrow keys, no left-stick movement or face-button gamepad input, and inability to invoke the usual hold-Menu → Use game controls path. This is recorded as an unresolved physical compatibility issue, not a mapping defect or a confirmed fix.

Inspection found that Unity's generated loader defaults disabledCanvasEvents to contextmenu and dragstart and calls preventDefault on those canvas events. The development template now overrides that list to dragstart only, preserving the native canvas context menu. This is the strongest plausible interference point, not proof of Xbox causation. Our earlier Focus game/Fullscreen buttons, document capture-phase scroll-key handler and A-button focus polling have also been removed. Canvas focus now follows only a primary pointer click; Menu/right-click, browser shortcuts, focus regain and fullscreen are left to the browser. CSS still fits the original 16:10 canvas without page scrolling. No pointer locking was requested by project code. The loading overlay disappears after initialization and the hint bar sits outside the canvas.

Comparison: NVIDIA's official Edge gamepad guidance tells users to choose Use game controls via the browser controller icon; it does not prescribe keyboard emulation or a page-owned controller-mode switch. The exact hold-Menu route comes from Kyle's working Xbox practice. Unity's loader default can suppress the canvas context menu even when a custom template does not explicitly register that event. Native fullscreen is not established as the cause; removing our helper reduces interference and leaves Edge chrome accessible. See https://nvidia.custhelp.com/app/answers/detail/a_id/5298/kw/display%20drivers%20update and the generated Unity loader's disabledCanvasEvents implementation.

Current retest (supersedes the custom Focus game/Fullscreen instructions above):
1. Refresh and confirm the new DEV build ID. While still in Browsing Controls, click the game once with the browsing pointer, then hold Menu (≡) over the canvas and choose Use game controls. If needed, try over the visible Xbox hint or use Edge's address-bar controller icon where offered.
2. Confirm Pads becomes nonzero, record the browser pad name/mapping and Player 1 eligibility, then test left stick/A. D-pad-as-arrows alone does not confirm gamepad detection.
3. Confirm Edge's Menu UI remains accessible; test using Edge's own fullscreen controls only after mode switching works. Report Xbox/Edge versions and whether the menu differs over canvas versus hint bar if it still fails.

No gameplay input binding, controller-to-keyboard translation, camera, timing, layout or gameplay-feel changes. Native Xbox Game Controls is not considered fixed until another physical Xbox test succeeds. Node regressions execute the actual template configuration and check that context/menu keys remain uncancelled, right-click does not refocus, and no custom fullscreen/gamepad-polling API is needed. The existing pipeline runs both Unity test suites before Web publication.

Hosted refresh verification exposed a stale unversioned web-shell.js cache referring to removed buttons. The template now appends the generated DATA_FILENAME hash to the helper URL, keeping each build's HTML and helper in sync. This is a hosting compatibility correction; no browser event or gameplay mapping is synthesized.

Final Xbox retest candidate: source d6081af0106f9510585db448408c30a9fc6cc6ef passed 5 browser-shell, 16 EditMode and 10 PlayMode tests, built successfully and deployed as gh-pages dd94f9cf7646af912e520f522938c31b2ca2b64a. Pages reports built. The existing in-app browser session now loads and refreshes successfully using the versioned helper, with the Xbox hint visible and custom buttons absent. No new browser/runtime or Unity Console errors; the prior URP warning remains. A right-click attempt in the in-app browser does not expose Xbox's native menu UI to this tooling, so native Menu/Game Controls behavior is not verified. Main is pushed and source changes are limited to the shell, diagnostic wording, tests and documentation. Kyle's physical retest remains required before calling the Xbox issue fixed.

## Xbox A-button follow-up

Physical feedback: joystick now works, but A/other buttons do nothing. An automated gamepad test reproduced a shared focus-release gate blocking A while Menu remained held after focus regain (10/11 passed before the fix; 11/11 afterward). Use and Restart now wait independently for their own release. This is a confirmed software defect, but not yet a confirmed explanation of the Xbox symptom. Bindings, movement and cooking behavior are unchanged.

DEV now shows browser button indices currently down (standard mapping: A=0, Menu=9), Unity A/Menu states, individual release gates and cumulative Use attempts/result. Browser signals are sampled twice per second: hold A briefly when reading them. No browser events are synthesized or intercepted.

Physical retest: refresh to the latest build ID, enable Use game controls, release buttons, face the potato supply and press A. If unsuccessful, open DEV with the browsing pointer, return to Game Controls and report Browser API down indices, Unity A/Menu, release gates, Use attempts/result and Target. Native Menu access must continue working. Physical Xbox verification remains pending.

Verification: source 4a10ffa8e3f81bfc97156528c9c543daa3dc2424 passed 6 browser-shell, 16 EditMode and 11 PlayMode tests and built/deployed as gh-pages dbf2c2bba808b6caccc94dc6bac47e36efc8d146. Pages reports built and the public build-info matches. Available desktop browser loaded the restaurant and expanded DEV fields correctly, with no new runtime errors (the existing URP shader warning remains). Unity Console errors: zero. Main was clean and matched origin/main; legacy branch/tag remain at the final browser snapshot. Physical Xbox causation and A-button success are not verified.

## Retained Xbox interaction evidence
Kyle confirmed pressing/releasing A while Take potato was visible in build 4a10ffa, without pickup. Prior screenshots with Use attempts=2 prove some Use calls reached gameplay, but do not identify the failed potato attempt. No root cause is confirmed. Diagnostic history now retains browser standard A pressed/value/focus evidence, performed Unity Use control/gate state, and the last interaction target/result/held item or exception. Browser sampling runs once per development Update, including with DEV closed; strings refresh twice per second. It is read-only and never focuses the canvas or synthesizes input. Counts reflect observed samples, not guaranteed capture of every browser event. Scene restart/page refresh resets the history.

Next physical test: load the new build, leave DEV closed, enter Game Controls, face POTATOES until Take potato appears, press/release A once, then open DEV and photograph it. No need to hold a button while taking the photo. Native Menu behavior and all gameplay bindings/parameters remain unchanged. Audio decode error from the earlier photo remains unresolved; no claim of an Xbox fix.
