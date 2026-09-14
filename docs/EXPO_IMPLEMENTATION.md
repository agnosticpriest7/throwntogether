# Expo implementation record

## EXPO-002 — Accepted foundation
Reviewed Claude commit: 882addb40ae26aa965a0b3576a388eb87bf4a8e1.
Imported only the two new runtime classes, new EditMode tests and their three meta files onto expo/order-rail, based on the latest roadmap commit fd6e84f. Collaboration instructions, generated settings changes and unrelated files were not imported.

Codex reviewed the implementation against CURRENT_TASK and inspected the actual test XML and log in the Claude clone: 77 EditMode passed, 0 failed (64 existing + 13 new). These tests ran in Claude's isolated project; they are not a canonical PlayMode or Web verification. No repeated test run was needed for this unchanged, disconnected implementation.

The two-slot model enforces explicit firing, counts Ready against capacity, preserves terminal states, orders equal-time fires deterministically, rejects foreign tickets and exposes read-only collection snapshots. Dish/seat mappings and all runtime side effects remain outside the model. Corrected a comment: internal mutators are accessible within the runtime assembly, not technically exclusive to the board.

The collaborator's Unity-import settings drift was excluded, including removal of SENTIS_ANALYTICS_ENABLED. Keep canonical settings unchanged. No gameplay is gated yet; no Expo station is present and nothing is deployed.

Next dependency: Codex's customer/table/dish integration, followed by a precisely scoped per-chef Expo UI assignment. Preserve the approved free movable station, two active orders, fire-before-service, advance prep, normal patience and late-service behavior.

## EXPO-003/005 — Lifecycle and physical delivery integration
Source: 70e69a7 (integration branch only). RestaurantExpo owns current table-ticket mappings, physical dish assignments and carrier claims. RestaurantDay.EnableExpo() constructs it once; real seating creates Waiting tickets and departures cancel live tickets without rewriting Served. Ready dishes hold capacity. Claims bind both ticket and Carryable so a previous customer's server cannot serve a replacement at the reused seat. Direct player service may deliberately retarget a matching fired ticket; old bindings/claims are invalidated atomically.
All acceptance and payment pass through the current table and successful physical transfer. Trashed/destroyed/changed dishes invalidate Ready back to Active; food and plates are never deleted by cancellation. Existing practice/legacy scenes are unchanged until a physical Expo initializes EnableExpo. The final release must install that station before enabling the requirement.

Verification: 77 EditMode + 95 PlayMode + 14 Web-script checks passed (186 total), from a committed isolated integration-branch snapshot. Nine new PlayMode cases cover real seating/manual cooking, fire gate, matching order priority, retargeting, stale claims/replacement diner, invalidated dishes, late service, queue/table reuse, server handling and duplicate claims. No Web player was built or deployed for this intermediate checkpoint.
Live canonical Unity MCP at 10:02 PM: unfired dish refused; Fire accepted; duplicate Fire refused; dish served; ticket Served and active capacity 0/2. Two departing guests kept service open/unpaid; after street exit, day closed/paid once with ticket still Served. Console: zero errors/warnings. Memory-only account; exited Play Mode without saving scene changes.
Next: Claude's ExpoStation/per-chef browser assignment using the stable RestaurantExpo interface. Codex retains final physical placement (including existing full-layout saves), HUD integration and release verification. No physical controller/TV claim.

## EXPO-004 — Accepted browser checkpoint
Reviewed Claude cf58492, including the three requested corrections. Imported only ChefInput, ExpoStation, ExpoTicketBrowser, ExpoStationTests and new metas. Confirmation refuses stale replacement selections and unavailable input; selection invalidation survives empty boards. Seven-seat regression verifies cursor/scroll bounds after navigation, firing reorder and deletion.
Verified Claude XML reports: 77 EditMode and 106 PlayMode passed against final source. These are clone-run results, not rerun canonical suites. Canonical MCP confirmed Application.dataPath, completed script compilation and availability of ExpoStation/ChefInput browser APIs. No Console errors; recompilation emitted obsolete FindObjectsSortMode warnings in unchanged authoring code. No scene saved, generated collaborator settings/material drift imported, Web build or deployment performed.
Next remains Codex-owned physical movable station placement (including full existing layouts), HUD/dual-panel visual review, full integrated gameplay and release verification. This checkpoint does not enable firing in shipped scenes.
