# Environment bootstrap — 2026-09-08

Completed environment bootstrap only. No gameplay or Vertical Slice #1 work was started.

## Verified project

`C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity` is the project containing AGENTS.md and ASTRA_BOOTSTRAP_PROMPT.md. All original docs were read. A sibling project named `ThrownTogether` also exists; MCP briefly selected that project. Final MCP queries verified the exact intended Assets path above.

## Health and tools

| Item | Result |
| --- | --- |
| Unity | 6000.6.0f1 (f7f8ed4d1e24) |
| URP | 17.6.0; active PC_RPAsset; URP/Lit shader supported |
| Input System | 1.20.0 installed; activeInputHandler = 1 |
| Test Framework | 1.8.0; both test assemblies compiled and ran |
| AI Navigation | 2.0.14 installed |
| AI Assistant / MCP | 2.19.0-pre.2; live commands working in the intended project |
| AI Inference | 2.6.1 installed; unused by bootstrap |
| Unity Pipeline | 0.6.0-exp.1 installed; local API also verified |
| Build modules | Windows and WebGL available; no player build performed |
| Serialization | ForceText and Visible Meta Files already enabled |

Final Unity MCP Console query: **0 errors, 3 warnings**, all repeated failures to collect the Windows signature for codex.exe. These do not block MCP or development. Batch logs also contained licensing/account messages and signature collection messages; both test runs passed. Account/cloud setup was left alone.

Bootstrap is open, clean, and unchanged, with three active root objects: Main Camera, Directional Light, Global Volume. No restaurant or additional scene was created. Existing SampleScene and build scene selection were preserved; the build list still targets SampleScene and should be revisited when establishing the build workflow.

## Files and folders

- Added `.gitignore`, excluding Library, Temp, Obj, Logs, UserSettings, Build, Builds, Artifacts, TestResults, captures and generated IDE files. Assets, Packages, ProjectSettings and meta files remain eligible for source control.
- Added shallow Assets folders: Art, Audio, Data, Materials, Prefabs, Scripts, Tests/EditMode and Tests/PlayMode. Unity generated their meta files. Existing Scenes, Settings and TutorialInfo were preserved.
- Added `Assets/Tests/EditMode/ThrownTogether.Tests.EditMode.asmdef` and `BootstrapSmokeTests.cs`, plus meta files.
- Added `Assets/Tests/PlayMode/ThrownTogether.Tests.PlayMode.asmdef` and `RuntimeSmokeTests.cs`, plus meta files.
- Added this report and a usage ledger entry.
- Unity rewrote `ProjectSettings/ProjectSettings.asset` during test execution. No deliberate player-setting change was made; the test-runner flag is off afterward. Without an existing Git baseline, a byte-level before/after diff is unavailable.
- Generated local logs/results under ignored TestResults and Logs; normal Library/UserSettings caches were refreshed by Unity.

No Git repository was present. No repository was initialized, and no remote, commit, merge or history operation was performed.

## Tests

| Test | Result | Purpose |
| --- | --- | --- |
| BootstrapSceneAndRenderPipelineAreAvailable | EditMode: 1 passed, 0 failed | Loads the Bootstrap scene asset, checks the current pipeline asset and ForceText serialization |
| PlayModeAdvancesFramesAndDestroysTemporaryObjects | PlayMode: 1 passed, 0 failed | Confirms Play Mode, coroutine frame advancement and temporary object destruction with cleanup |

Results: `TestResults/EditMode.xml` and `TestResults/PlayMode.xml`. Tests ran in Unity batch mode against this exact project, not the sibling project. The PlayMode process exited with code 0. No physical controller, browser, mobile or couch co-op testing was performed.

To rerun interactively, use Window > General > Test Runner and run the ThrownTogether test assembly in each mode. For batch runs, close this project's Editor first and use the installed Unity executable with `-batchmode -projectPath <project> -runTests -testPlatform EditMode` (or `PlayMode`), `-assemblyNames ThrownTogether.Tests.EditMode` (or `.PlayMode`), `-testResults <xml path>` and `-logFile <log path>`. Do not add `-quit`; the test runner exits when finished.

## Next step and human action

No bootstrap blocker remains. When multiple Unity projects are open, verify MCP's Application.dataPath before any edits. Recommended next task: establish the local Git baseline and a minimal Windows/Web build workflow, without gameplay. Remote setup needs an explicitly chosen repository; Vertical Slice #1 remains gated on an explicit instruction.

Account-wide monthly project usage is unavailable; no usage percentages or costs were inferred. Work was limited to environment checks, shallow folders and two smoke tests.
