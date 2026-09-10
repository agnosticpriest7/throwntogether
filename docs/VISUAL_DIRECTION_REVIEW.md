# Visual direction prototype — approval review

This is a local, static visual study, not a production conversion or a playable level. It has not been committed or deployed. Existing playable scenes, chef prefab, gameplay scripts, materials and build configuration are unchanged.

## Open and review

Open `Assets/VisualDirection/VisualDirectionTest.unity` in Unity. It contains one prep counter, one fryer, one sink, three plain counters, a 12 × 9 unit tiled floor sample, short walls, one soft directional light, and two posed instances of the current chef. Input/behavior is disabled on these scene instances only. Their carried props reproduce the existing fries plate and tomato geometry/colors at the existing carry anchors. Static material copies preserve the look when reopening the scene.

Actual Unity camera captures at 1920 × 1080:

- [Gameplay view](../ArtSource/VisualDirection/Review/01-gameplay.png): 55-degree pitch, no yaw, orthographic size 6.5.
- [Closer material view](../ArtSource/VisualDirection/Review/02-close.png): same angle, size 3.6, framed on appliances and chefs.
- [Expansion-distance view](../ArtSource/VisualDirection/Review/03-expansion-distance.png): same scene and scale, size 9.75. This shows the effect of 1.5× wider framing; it is not a populated larger restaurant or a crowd-performance test.

Use `Thrown Together > Visual direction > Capture three review views` to recapture. The command restores the intended gameplay camera afterward. Do not blindly rerun the creation command over this saved scene.

## Dimensions and modular assets

Chef scale remains (1,1,1), controller height 1.85, carry anchor (0,1.10,0.65). The station body is 1.8 × 1.5 units, with a 1.9 × 1.6 worktop ending at 1.20 units, matching the existing stations. Decorative handles add approximately 0.04 units to visual depth; no gameplay collider or reach was resized. Appliance controls/tap extend above the working surface. Floor tiles are 1 unit; wall sections are 1.9 units long. Module roots are floor-centered, Y-up in Unity, front toward -Z, suitable for drag-and-drop placement.

Seven independent Blender-authored FBXs and Unity prefabs live under `Assets/VisualDirection`. The editable source and repeatable construction script are in `ArtSource/VisualDirection`; `kitchen-modules-v01.blend` is the final source for this first direction study. The existing character Blender file was not overwritten. The supplied reference informed palette, silhouette and material separation, not copied props, characters, decoration or layout.

The study uses shared flat-color URP materials, two-segment bevels, dark inset reveals instead of an outline post-process, and no textures or transparency. New module meshes range from 120 triangles per floor tile to 5,244 for the fryer. Seven FBXs total 686,916 bytes; the arranged static scene geometry reports 28,052 triangles, excluding skinned chefs. The chef material copies are review-only and are not a replacement production material system. No WebGL frame-rate or large-crowd performance claim is made; a production pass should evaluate floor batching and repeated material/draw costs before expanding content.

## Readability findings

- The angled orthographic view exposes work surfaces while retaining the appliance fronts. Warm wood and cool metal separate cleanly.
- Prep's board/knife, fryer's two baskets, and sink's tap/blue basin are distinct without labels at the intended framing. Small controls and basket wires lose detail at expansion distance; broad silhouettes must carry identification.
- Both chefs remain separated by silhouette and color in this static arrangement. The existing oversized food is conspicuous, but partly covers faces and aprons. This study intentionally preserves those dimensions rather than hiding that tradeoff.
- Short rear/side walls preserve the view. Full-height foreground walls or chefs walking behind taller appliances still need an occlusion study before adoption.
- The wider camera is promising for broad station recognition, but faces and fine accessory choices become harder to distinguish. This is not evidence of readability in a crowded moving kitchen or from Xbox/TV distance.
- The result is intentionally simpler/flatter than the reference's finished illustration. Decorative detail and a full restaurant art pass remain out of scope pending owner approval.

## Verification

Used Blender MCP for source authoring/export and Unity MCP for import, scene assembly, dimension checks and actual camera captures. Reopened the saved scene and recaptured to verify persisted colors/poses. No missing scripts, materials or shaders; Unity Console errors zero. Fourteen existing API-deprecation warnings surfaced during recompilation, none from the prototype. Shared assets/code were not changed, so gameplay regression suites were not rerun. The review scene is excluded from `build-config.json`. No production assets were replaced and no gameplay feature or deployment was added.

## Subsequent approval
The owner approved this study and requested proceeding. Its shared modules now support the 0.8.0 playable kitchen integration; the review scene and captures remain a separate historical comparison. See DEVELOPMENT_NOTES.md for integration verification.
