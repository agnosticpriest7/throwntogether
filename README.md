# Thrown Together

Development now uses Unity 6. Open this repository with **6000.6.0f1**, then open `Assets/Scenes/RestaurantDevelopment.unity` and press Play. Bootstrap remains a separate minimal startup scene.

The canonical local checkout is `C:\Projects\ThrownTogetherUnity\ThrownTogetherUnity`.

Read `AGENTS.md` and the design and technical documents in `docs/` before development. Vertical Slice #1 provides one chef's potato → prep → fryer → plate → customer loop. Move with WASD/arrows/left stick; use stations with E/Space/controller A; restart with R/Start. See `docs/DEVELOPMENT_NOTES.md` for the complete flow, limitations and human playtest checklist.

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

Commit source changes first. Deployment also requires main to be pushed and equal to origin/main. Requires Git, tar, the licensed Unity version in ProjectVersion.txt with Web Build Support, and authenticated GitHub CLI (`gh`) for Pages settings. Use `-UnityPath` if Unity is installed outside the default Hub directory.

The script builds a committed snapshot in ignored `Builds/Workspace`, leaving the canonical Editor open and main clean. Logs and test XML are in `Builds/PipelineLogs`; static output is in `Builds/Web`. Both test suites must pass before building. A failed test/build prevents publication. `-Mode Build` deliberately still runs tests. Concurrent runs are blocked; each Unity stage has a configurable 90-minute timeout.

The explicit scene list is `build-config.json`, currently only RestaurantDevelopment. It overrides the checked-in Build Settings scene list. The public build is the tiny first-service gameplay slice.

Deployment uses an isolated repository under ignored `Builds/Publish-*`, with ordinary commits/pushes to **gh-pages** only. No generated files enter main. Retained staging folders/logs can be inspected after failures. Pages is configured automatically using `gh`; if permissions prevent this, select **Settings > Pages > Build and deployment > Source: Deploy from a branch > Branch: gh-pages > / (root) > Save**. No GitHub-hosted Unity compilation or license secret is needed.

Public test URL: **https://agnosticpriest7.github.io/throwntogether/**. Allow Pages a few minutes after publication, then refresh. `build-info.json` at that URL identifies the source commit. Xbox Edge/controller testing is a human follow-up, not implied by a successful deployment.
