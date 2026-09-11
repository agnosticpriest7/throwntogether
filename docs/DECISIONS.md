# Thrown Together — Decision Log

## Current locked decisions

- Primary game uses a persistent restaurant.
- Single-player must be fully supported.
- Local couch co-op remains core.
- Preferred kitchen is open/shared rather than sealed player halves.
- Throwing is optional rather than structurally mandatory.
- Employee-centric progression replaces conveyor/automation-centric progression.
- Management never removes the ability to work hands-on.
- Ingredient inventory persists.
- Bulk purchasing is supported.
- No ingredient spoilage/decay for now.
- No repair/maintenance economy as a central system.
- Kitchen and dining capacity are finite and expandable.
- Persistent reputation drives baseline demand.
- Advertising is temporary.
- Bigger is optional.
- Multiple locations are a later progression layer.
- Different owned restaurant concepts are allowed.
- Current production-engine direction is Unity 6.
- Prototype art may later be replaced by commissioned human work.

## Template

### 2026-09-09 — Modular character style and three builds
**Decision:** Kyle approved the broad, softly squared character prototype and requested clothing selection at the beginning plus slim and slightly heavier alternatives. The prototype exposes Slim, Standard, and Fuller builds, with independently chosen clothing and cosmetics.
**Reason:** Let players express their identity before cooking starts.
**Implications:** This explicitly expands the original single-body-build scope. All builds share the head, skeleton, movement, collision, and interaction reach. Clothing meshes fit each torso. Wardrobe choices are session-scoped for this prototype.

### YYYY-MM-DD — Decision
**Decision:**
**Reason:**
**Alternatives rejected:**
**Implications:**

2026-09-09 — Kyle approved the VisualDirectionTest study and requested proceeding. Apply the approved visual direction to existing playable kitchens while preserving gameplay dimensions, layouts, controls and behavior. Retain the separate study and source Blender modules.

2026-09-10 — Kyle approved starting with the floor and an original First Service rearrangement inspired by the reference. This supersedes the prior preserve-layout restriction for First Service only. Preserve room/chef dimensions and gameplay parameters; no full prop-art conversion bundled into this pass.

2026-09-10 — Kyle approved appliance refinement, cohesive modular counters and ingredient storage from the visual priority list. Preserve current layouts, player dimensions and gameplay. Use original Blender geometry and retain the earlier visual study; broader restaurant decoration and gameplay work remain deferred.

2026-09-10 — Kyle approved visual priorities 4–6: dining furniture, walls/windows/entrance and lighting/material polish. Keep room/gameplay layout, customer behavior, camera and all interaction parameters. Entrance is visual only; broader customer animation, decoration/exterior and gameplay systems remain deferred.

2026-09-10 — Owner approved visual priority 7: refine the service counter and dirty-dish return within the approved kitchen style. Preserve all serving/delivery/washing rules and layout; no new bell interaction or food/customer/HUD art pass.

2026-09-10 — Kyle approved priority 8 (held food/plate readability) and then requested a pause until more tokens are available. Preserve gameplay and carry anchors; reduce visual bulk and refine existing dishes. Stop after verified deployment; do not proceed to priority 9.

2026-09-10 — Kyle resumed for visual priorities 9–10: seated customer art/animation and order-bubble/HUD clarity. Preserve customer rules and gameplay. Automatic approval review requires explicit authorization before this candidate is committed/pushed/deployed.

2026-09-10 — Kyle approved restrained decoration and exterior framing, completing the visual priority list. Keep details subordinate to kitchen readability; preserve gameplay footprint/camera. Original small signs and greenery, cutaway awning, sidewalk and mat; no new playable exterior or gameplay feature.

2026-09-10 — Kyle approved a kitchen/dining divider, accessible service/return counters on both sides, a connecting doorway for future serving, and a total pool of five reusable plates to require washing. Dining expansion allowed if needed; current footprint fits after shifting tables. Three-order short shifts retain their existing order count and can finish without washing.

2026-09-10 — Restaurant entry belongs on the dining frontage, not the kitchen frontage. Owner requested correcting the entry location; retain the internal kitchen/dining doorway.

### 2026-09-10 — First timed service day
Owner requested 11 AM–10 PM in about five minutes, manual seating/service/clearing, outside patience, meal income and speed bonuses, purchasable equipment space/upgrades and a starter server. Initial tuning: 300 seconds, 12 arrivals, 45-second outside patience, eight-second meals, $10 base + up to $5 quick-service bonus. First purchases are predefined reusable counter/fryer bays rather than a construction UI; one-time server hire transports pass-to-table only. Money/purchases persist locally; no wages or ingredient costs added. Fixed-order practice remains separate and unpaid. These values need solo/co-op physical playtesting.

### 2026-09-10 — Service quality-of-life batch
Owner approved direct stock-to-hand plating, a multi-plate sink, seated customer patience, a dishwasher hire, broader player cosmetics and unique randomized customers. Initial seated patience: 90 seconds; dishwasher: $125 permanent hire applying next day. Washer handles sink plates and returns clean stock; players retain table clearing. Five-plate capacity, movement, reach, camera and recipe timing remain intact. No wages, new recipes or progression beyond this hire.
