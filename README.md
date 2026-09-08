# Thrown Together

Development now uses Unity 6. Open this repository as a Unity project with **6000.6.0f1**, then open `Assets/Scenes/Bootstrap.unity`.

The canonical local checkout is `C:\Projects\ThrownTogetherUnity\ThrownTogetherUnity`.

Read `AGENTS.md` and the design and technical documents in `docs/` before development. The environment bootstrap includes minimal EditMode and PlayMode smoke tests; gameplay development has not begun.

## Browser prototype archive

The existing Git history is preserved. The final browser prototype, including its art reference, is available at:

- Branch: `legacy/web-prototype`
- Annotated tag: `web-prototype-final`
- Final browser commit: `540af75152c94420d8a88e20c46b50162b4c9493`

The Unity transition continues directly from that commit on `main`. The browser-specific GitHub Pages workflow is retired from main along with the browser source; it remains available in the archive. No Unity deployment workflow has been added.

The old local browser checkout at `C:\Projects\ThrownTogether` is preserved. Use the canonical Unity checkout for future main development; do not pull Unity main into the old browser folder if you want to keep its current browser files.

## Source control

Track Assets (including `.meta` files), Packages, ProjectSettings and documentation. Unity-generated caches, local settings, logs, test results and build outputs are ignored. Keep Force Text serialization and Visible Meta Files enabled.

Run the two project test assemblies through Unity's Test Runner. See `docs/BOOTSTRAP_REPORT.md` for the bootstrap results and batch test instructions.
