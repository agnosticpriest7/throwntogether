# Web development baseline — 2026-09-08

Development version **0.1.0-dev**, source **b4460f6c92a288e11645aab18ea0c87548272904**, Pages commit **819d6a46fa0b6c2f0db30af6405aafc06497a58c**. Public URL: https://agnosticpriest7.github.io/throwntogether/. Unity 6000.6.0f1, optimized Web with diagnostic UI; no Unity Development Build/profiler instrumentation mode.

## Deployed size

| Output | Bytes |
|---|---:|
| All 8 static files | 47,867,927 (45.65 MiB) |
| WebAssembly | 34,934,166 |
| Data | 12,574,572 |
| Framework JavaScript | 331,992 |
| Loader JavaScript | 20,263 |

Measured by summing file lengths under ignored Builds/Web, not Unity BuildReport's larger internal size. Previous d906a645 output totaled 47,565,785 bytes; this batch adds 302,142 bytes (about 0.64%). Seventeen source WAVs total 200,084 bytes. Output remains uncompressed, hashed and relative-path hosted. HTML, loader, data and wasm requests succeeded below /throwntogether/; wasm MIME is application/wasm. No generated build output is committed to main.

## Observed runtime and startup

- Environment: Windows development machine, Codex in-app browser, 1280×720 viewport. Canvas CSS approximately 1100.8×688, backing 3253×2033, devicePixelRatio approximately 2.955. Page visible and canvas focused for FPS observations.
- Sampled idle restaurant FPS: approximately **60**, including repeated observations with DEV/audio settings panels open. This is a small diagnostic baseline, not frame-time percentiles, a full gameplay performance benchmark or Xbox certification.
- Final build's first visit in this session: **4,876 ms** to createUnityInstance completion; warm refresh: **323 ms**. The earlier first visit to foundation source 456f940 measured 8,301 ms. Wasm/framework/browser caches and network conditions were not cleared or controlled, so none is a guaranteed cold-start figure.
- Timing comes from canvas data-loader-ready-ms, set from performance.now() when the Unity loader promise resolves. It includes navigation/download/initialization up to that boundary; it does not measure the end of Unity's splash screen or first playable frame. The restaurant rendered successfully afterward and diagnostics were closed by default.
- GC Allocated In Frame was **unavailable in this optimized Web player**, not zero. The diagnostic recorder reports that limit explicitly. Static inspection identifies recurring IMGUI status/diagnostic string construction (and LINQ in the expanded panel), plus browser gamepad string marshaling every 0.5 seconds. Audio uses a fixed source pool and a private random generator. No speculative optimization or unsupported numerical Web GC claim was made.
- Browser/runtime errors: none observed during load, focus, diagnostic UI and save/refresh checks. The pre-existing URP Edge Adaptive Spatial Upsampling warning remains parked. A manual filesystem-sync deprecation warning found during the first save check was resolved by enabling Unity's automatic persistent-data synchronization.

## Reproduce / compare

Run scripts/test-build-deploy-web.ps1, record build-info.json, sum the resulting static file lengths, then visit the published build. Record browser/hardware, viewport/DPR, cache conditions and the canvas data-loader-ready-ms attribute. Focus the game and sample F3/DEV FPS in the same idle scene, keeping panel state consistent. If allocation analysis becomes necessary, make a separate profiling build; do not equate Editor allocations with Web allocations. Repeat on physical Xbox/Edge before using these figures as a console performance target.

Final verification passed 4 browser-shell, 16 EditMode and 10 PlayMode tests. Unity MCP completed the original timed potato → prep → fryer → plate → customer loop with no new Console errors. Final browser settings verification changed SFX volume, saved, refreshed and confirmed the saved value; then restored/saved its original maximum value. Diagnostic visibility remained transient. No audible sound-balance, physical controller or TV testing was claimed.
