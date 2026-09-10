# Camera and composition comparison — September 10, 2026

**Recommendation: B, 45° orthographic, with modestly tighter framing and unchanged character scale.** It exposes the existing cabinet fronts and faces without sacrificing as much work-surface visibility as C. This is a recommendation for the next camera target, not a production change.

[Open the comparison gallery](index.html). Click any image for its full 1920×1080 capture. [Exact camera settings](capture-settings.json).

## Scope and controls

Source: `Assets/VisualDirection/VisualDirectionTest.unity` at main `d94e43d52852e8fe203865a6cbff2d28e15b46b8`. This is the original separate module study, **not the current full RestaurantShift level**. Its saved sample props and materials are retained, including its large demonstration food. It has two posed chef references, six counter/appliance samples and no live HUD/dining service. No newer production models were substituted.

All six images use exactly the same scene, station positions, chef poses/scales, materials and lights. A preserves the saved normal camera position, rotation and orthographic size exactly. B/C retain its aim point (-0.25, 0.45, 0), zero yaw and orthographic projection. Only pitch and framing change. Angles are downward from horizontal; 90° would be straight down.

| Variant | Downward pitch | Normal orthographic size | Expansion size | Framing change from A |
|---|---:|---:|---:|---|
| A — saved baseline | 55° | 6.5 | 9.75 | None |
| B — moderate, recommended | 45° | 5.75 | 8.625 | Objects about 13% larger horizontally |
| C — stronger depth | 35° | 5.25 | 7.875 | Objects about 24% larger horizontally |

Expansion images multiply each variant's normal size by 1.5: the same objects occupy two-thirds of their normal pixel size. This approximates fitting 50% more world width/height. No larger kitchen, extra stations or extra people were fabricated. Different normal framing means these are presentation bundles, not a pitch-only experiment.

## Visual evaluation

| Criterion | A — baseline | B — moderate | C — stronger |
|---|---|---|---|
| Character readability | Hats and food dominate; facial features are least exposed | Better face area and recognizable colored bodies | Most face/body depth, but held food still hides mouths |
| Appliance readability | Prep board, baskets and basin clearly readable from above; shallow cabinet fronts | Front panels, handles and worktops all read well | Strong cabinet silhouette, but basin and basket interiors are more foreshortened |
| Work surfaces | Most available top-down area and easiest spatial overview | Good usable top area; modest overlap behind the island | Less top area; foreground counters conceal more lower body/foot position |
| Depth | Clear but comparatively flat | Convincing thickness without a large visibility penalty | Strongest depth; begins to feel less overhead than the concept |
| Two-player visibility | Both existing posed chefs remain separate | Best compromise of faces, food and spatial separation | Both heads/food visible, but floor-position cues are weaker near counters |
| Expansion | Clearest overview; smallest face details at this framing | Best overall compromise; ingredient silhouettes survive but fine expressions diminish | Larger-looking faces, but greater overlap risk as a restaurant becomes denser |
| Concept presentation | Warm/cool palette present, more diagram-like | Closest useful balance of overhead readability and toy-like depth | Strong dimension, but not automatically closer: the reference also preserves generous worktop visibility |

These are image-based judgments. Two static references do not verify moving co-op players, all headings, overlapping chefs, targeting, HUD placement, TV readability or Web performance. Those checks belong in a reversible full-restaurant camera trial after selection.

## Recommended next decisions

- **Camera:** use B's 45° pitch as the target to try next. Keep orthographic projection and the current axis alignment.
- **Character scale:** keep 1×. The camera alone improves faces. Larger chefs would increase counter/food occlusion and reduce visual aisle clearance. The study's oversized original carried props are not evidence that current production food needs another redesign.
- **Framing:** tighten modestly, approximately B's 11.5% reduction in orthographic size. Do not copy size 5.75 into the much larger production restaurant. Refit that scene with HUD, dining area, perimeter and both players in view.
- **Lighting/materials:** the existing warm wood versus cool steel already works. Retain the soft-shadow key and restrained palette first. No material tint, light, AO or render-pipeline settings were edited. An SSAO feature exists in the PC renderer, but that does not establish Web parity; this pass does not promise a new Web AO effect. Do not add cost before a matching hosted comparison.
- **Assets:** no broad remodel is justified by this camera pass. The geometry already has useful front panels, handles, rims and thickness that the more overhead view hides. Lighting cannot add missing silhouettes, and camera work alone cannot make this intentionally sparse sample as busy as a complete restaurant. Evaluate B on the full current restaurant before considering selective model changes.

## Verification and reproducibility

Unity MCP Console and read-only scene/camera requests stalled and were terminated. **Live MCP inspection, MCP screenshots and live Console verification could not be completed.** Images were instead rendered by Unity 6000.6.0f1 in `Builds/Workspace`, an isolated copy of the current source assets. The live Editor was not altered.

`CameraCompositionComparison.Capture` is an explicit batch-only Editor helper. Invoke Unity with `-batchmode -quit -projectPath <isolated-project> -executeMethod CameraCompositionComparison.Capture -logFile <log>`. Do not launch a second Unity process on the canonical open project. It writes this folder relative to the isolated project; copy the output back for review. No scene or asset is saved. Shaders are synchronously warmed before readback to avoid a gray first-frame baseline. Camera and shader-compilation settings are restored in `finally`.

The final run exited successfully, produced six PNGs/settings JSON, verified every original scene transform restored, and verified source-scene bytes unchanged. Capture log: `Builds/CameraComparisonValidation/Author.log` (ignored local evidence). No execution/compilation errors or warnings attributable to the new helper in the final run. Existing project warnings are not claimed fixed. Source scene SHA-256 is recorded in the JSON.

No shared runtime code/assets were touched, so gameplay tests and a Web build/deployment were not rerun for this review-only pass. Production camera, gameplay geometry, collider dimensions, interactions, station layout and live release remain unchanged. This comparison was initially delivered locally for review and subsequently versioned with the separately authorized 0.9.2 production camera trial. These test-scene images remain the unchanged comparison reference.
