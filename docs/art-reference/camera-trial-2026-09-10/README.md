# 45-degree production camera trial — 0.9.2

Both playable scenes retain the full restaurant, original assets and every non-camera serialized component. The only scene changes are camera rotation, position and orthographic size.

| Setting | Trial | Previous baseline |
|---|---|---|
| Projection | Orthographic | Orthographic |
| Pitch / yaw / roll | 45° / 0° / 0° | 55° / 0° / 0° |
| Orthographic size | 7.2 | 8.1 |
| Aim point | (0, 0.3, 1) | Same |
| Aim distance | 20 | Same |
| Position | (0, 14.442137, -13.142134) | (0, 16.68304, -10.471529) |
| Character scale | Unchanged | Unchanged |

The Web shell fits a 16:10 canvas inside the browser/TV viewport. These Unity camera captures use that same aspect ratio; HUD review is performed separately in the actual Web player.

- [Normal scene](normal.png)
- [Two temporary chefs holding fries, facing opposite directions](two-chefs-held-food.png)
- [Expansion-distance view](expansion-50-percent-area.png): size 8.81816, calculated as 7.2 × √1.5. This represents 50% more floor area, not 50% more width and depth. No expansion geometry or system was created.

Front-facing chef/customer faces and cabinet fronts are more apparent. Prep boards, fryer baskets and sink remain visible. Expansion distance retains major silhouettes and the kitchen/dining distinction; fine expressions become smaller. Away-facing chef heads still partly obscure held dishes. This existing occlusion was documented, not hidden by moving carry anchors, scaling chefs or remodeling assets.

Review chefs, plates and expansion zoom were never saved to either scene. Materials and lighting are unchanged. `ApplyCameraTrial.RestoreBaseline` is an isolated batch authoring entry point that restores the previous camera values; reviewing/reverting only the three camera fields in each scene is also sufficient. Do not run a second Unity process on the canonical open project.

Unity MCP remained unresponsive. These are isolated Unity renders, not MCP screenshots or a physical co-op playtest. The full automated service loop tests exercise targeting and Chef.Use; live MCP loop/Console verification could not be performed. See `docs/DEVELOPMENT_NOTES.md` for final test and deployment results.

TV check: walk behind the island/divider and around both pass counters; turn while carrying each dish; complete preparation, frying, plating, service and washing; join P2 and move to opposite corners. Check the top ticket/menu, customer cards and bottom prompts at default and larger text sizes. This is a reversible trial awaiting Kyle's assessment, not final camera approval.
