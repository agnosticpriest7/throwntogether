# Restaurant finishing details
Original modular Blender geometry, created for Thrown Together. No copied reference props/logos.

Source: `restaurant-details-v01.blend`; explicit construction: `build_details.py`.
Units: meters. Unity wrappers preserve the established FBX orientation convention.
Seven modules: PottedPlant, LowPlanter, SidewalkTile, EntranceMat, EntranceAwning, KitchenSign, DiningSign.
Meshes have no collision. Shared RM/KT materials plus three DT stone/pot materials. Per-module triangle counts in metrics.json.

AuthorRestaurantDetails creates reusable prefabs and adds a single finishing-details root to both playable scenes. Entrance sconces reuse RestaurantRoom/WallSconce without new realtime lights. Plants/signs remain at the perimeter; exterior does not create a playable exit. Shallow cutaway canopy placement prevents foreground occlusion. Small sign lettering is decorative, not required gameplay information.
Authoring is guarded against blind regeneration and runs only in an isolated Unity batch project. Capture variants never save camera changes. Do not run over later hand edits.
