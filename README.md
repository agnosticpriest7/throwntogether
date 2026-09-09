# Thrown Together

Development now uses Unity 6. Open this repository with **6000.6.0f1**, then open `Assets/Scenes/RestaurantDevelopment.unity` and press Play. Bootstrap remains a separate minimal startup scene.

The canonical local checkout is `C:\Projects\ThrownTogetherUnity\ThrownTogetherUnity`.

Read `AGENTS.md` and the design and technical documents in `docs/` before development. Vertical Slice #1 provides one chef's potato → prep → fryer → plate → customer loop. Move with WASD/arrows/left stick; use stations with E/Space/controller A; open the menu with Y/Escape and request restart with R (confirmation required). Xbox Edge Menu remains browser-owned. See `docs/DEVELOPMENT_NOTES.md` for the complete flow, limitations and human playtest checklist.

## Browser prototype archive

The existing Git history is preserved. The final browser prototype, including its art reference, is available at:

- Branch: `legacy/web-prototype`
- Annotated tag: `web-prototype-final`
- Final browser commit: `540af75152c94420d8a88e20c46b50162b4c9493`

The Unity transition continues directly from that commit on `main`. The browser-specific GitHub Pages workflow is retired from main along with the browser source; it remains available in the archive.

The old local browser checkout at `C:\Projects\ThrownTogether` is preserved. Use the canonical Unity checkout for future main development; do not pull Unity main into the old browser folder if you want to keep its current browser files.

## Source control

Track Assets (including `.meta` files), Packages, ProjectSettings and documentation. Unity-generated caches, local settings, logs, test results and build outputs are ignored. Keep Force Text serialization and Visible Meta Files enabled.

Run the two project test assemblies through Unity's Test Runner. See `docs/BOOTSTRAP_REPORT.md` for the bootstrap results and batch test instructions.

## Local Web test builds

From this repository in PowerShell:

```powershell
# Tests, local Web build, then GitHub Pages deployment (default).
./scripts/test-build-deploy-web.ps1
# Tests and local build, without publishing.
./scripts/test-build-deploy-web.ps1 -Mode Build
# Both test suites only.
./scripts/test-build-deploy-web.ps1 -Mode Test
```

Commit source changes first. Deployment also requires main to be pushed and equal to origin/main. Requires 64-bit Windows PowerShell/PowerShell, Git, tar, Node.js 20+ (browser-shell tests), the licensed Unity version in ProjectVersion.txt with Web Build Support, and authenticated GitHub CLI (`gh`) for Pages settings. Use `-UnityPath` if Unity is installed outside the default Hub directory.

The script builds a committed snapshot in ignored `Builds/Workspace`, leaving the canonical Editor open and main clean. Logs and test XML are in `Builds/PipelineLogs`; static output is in `Builds/Web`. Both test suites must pass before building. A failed test/build prevents publication. `-Mode Build` deliberately still runs tests. Concurrent runs are blocked; each Unity stage has a configurable 90-minute timeout.

The explicit scene list is `build-config.json`: RestaurantDevelopment (practice) and RestaurantShift. It overrides the checked-in Build Settings scene list. The public build opens a main menu. Play a level or Practice leads to First Service, Prep Island, or Split Line; choosing a kitchen starts a fresh session.

Deployment uses an isolated repository under ignored `Builds/Publish-*`, with ordinary commits/pushes to **gh-pages** only. No generated files enter main. Retained staging folders/logs can be inspected after failures. Pages is configured automatically using `gh`; if permissions prevent this, select **Settings > Pages > Build and deployment > Source: Deploy from a branch > Branch: gh-pages > / (root) > Save**. No GitHub-hosted Unity compilation or license secret is needed.

Public test URL: **https://agnosticpriest7.github.io/throwntogether/**. Allow Pages a few minutes after publication, then refresh. `build-info.json` at that URL identifies the source commit. Xbox Edge/controller testing is a human follow-up, not implied by a successful deployment.

### Controller and accessibility controls
The player starts at the main menu. Choose Play a level or Practice, then a kitchen. During gameplay, Recipe book is available from the pause menu; cooking and held items remain paused while browsing. **Y / Escape** opens or closes it; use **D-pad / left stick / arrow keys** to navigate, **A / Enter** to select and **B / Backspace** to go back. Menu pauses the kitchen. Restart (also R) and mode changes require confirmation; the three-line Menu button remains Xbox Edge's own control. Choose free/guided practice or a 3/6/12-order fries/mushroom/salad shift. Editor play starts unpaused for authoring; Y opens the same menu.

The menu provides three text sizes, high contrast, reduced visual effects and five audio volumes. **Save settings** persists preferences locally; unsaved changes reset on scene reload. P1 uses keyboard/first gamepad; resume and press A on a second pad to join P2. Keyboard + one-pad mode and P2 leave are in the menu. P2 must put down held food before leaving; disconnect/reconnect retains it. Player prompts, dish shapes and ticket states supplement color. The latest visual/menu changes still need a physical Xbox/TV review.

## 0.4.0 overnight review

The kitchen now uses visual station models instead of floating nameplates: ingredient crates, a chopping board and knife, fryer basket, plate stack, ordinary counters, and a service pass with a bell. A mint/coral floor border identifies each player's interaction target. Food shapes are larger; progress bars and steady ready lights replace station text panels. Menu > Settings > Text and accessibility retains larger text, high contrast and reduced effects.

**Plating:** place cooked food on either ordinary counter, then add a clean plate to the same counter. Plate-first also works. The finished dish stays **on the counter**; press A / E again to pick it up, then serve. No dedicated plating station is needed.

**Menu > Shift length and guided practice:** select short (3 dishes), standard (6), or long (12), then Start selected shift. These reuse the existing fries/mushroom sequence and two seats, with no order expiry or failure timer. Guided full loop teaches the same interactions. Practice one step starts with a raw, cut, cooked, or plated ingredient for prep, frying, plating, or serving. Checkpoint selection resets practice only after confirmation. Guidance follows P1; ordinary co-op remains available.

Session results show elapsed play time and each player's accepted prep/fry/plate/serve actions. Paused menu time is excluded; counts are contributions, not points or an economy. Restart resets them. Completed sessions show a result card; Y / Escape provides replay and session choices.

### Restore a previous Web build

The owner-confirmed co-op/food-shape build before this batch is deployment commit `d31291d325bdbfabce7a0b6d975a53920310295c` (source `e88c1e537649b03e763c7a5c00b974eac47edabf`, version 0.3.0-dev). This is a rollback reference, not a claim that every feature was physically verified.

```powershell
# Download and validate the previous snapshot for inspection; does not publish.
./scripts/restore-web-deployment.ps1 -DeploymentCommit d31291d325bdbfabce7a0b6d975a53920310295c
# Explicitly restore it as a NEW gh-pages commit, leaving main unchanged.
./scripts/restore-web-deployment.ps1 -DeploymentCommit d31291d325bdbfabce7a0b6d975a53920310295c -Publish
```

New builds include a SHA-256 artifact manifest. Publication verifies the files against it and rejects source files, compressed output, missing payloads or changed bytes. Generated manifests and restored snapshots stay under ignored Builds/ or on gh-pages.

Latest variety build: use **Menu > Kitchen, shift length and practice > Kitchen** to switch between First Service, Prep Island and Split Line. Tomato Salad needs chopping and ordinary-counter plating, with no frying; try its guided loop under **Practice one step**. **Co-op setup and controller help** explains joining and recovery. Layout changes reset the current session only after confirmation.

## 0.6.0 kitchen care
Open **Quick Play** to choose a kitchen. Tutorial, Career, Trials and Endless are visible but greyed out until implemented.

**Cutting/washing:** press Use to begin, then stay still. Moving pauses progress; press Use again to resume. Another chef can take over an abandoned job. Fryers continue unattended. After customers eat, collect dirty plates from the kitchen return rack, wash them at the sink (3 seconds), and pick up the clean plate. The clean-plate source remains unlimited for this development version.

**Garden Salad:** chop tomato and lettuce separately, combine both with one clean plate on an ordinary counter, then pick up and serve. Either ingredient can be added first. Pause > Recipe book has the steps.
