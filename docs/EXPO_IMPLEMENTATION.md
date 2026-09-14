# Expo implementation record

## EXPO-002 — Accepted foundation
Reviewed Claude commit: 882addb40ae26aa965a0b3576a388eb87bf4a8e1.
Imported only the two new runtime classes, new EditMode tests and their three meta files onto expo/order-rail, based on the latest roadmap commit fd6e84f. Collaboration instructions, generated settings changes and unrelated files were not imported.

Codex reviewed the implementation against CURRENT_TASK and inspected the actual test XML and log in the Claude clone: 77 EditMode passed, 0 failed (64 existing + 13 new). These tests ran in Claude's isolated project; they are not a canonical PlayMode or Web verification. No repeated test run was needed for this unchanged, disconnected implementation.

The two-slot model enforces explicit firing, counts Ready against capacity, preserves terminal states, orders equal-time fires deterministically, rejects foreign tickets and exposes read-only collection snapshots. Dish/seat mappings and all runtime side effects remain outside the model. Corrected a comment: internal mutators are accessible within the runtime assembly, not technically exclusive to the board.

The collaborator's Unity-import settings drift was excluded, including removal of SENTIS_ANALYTICS_ENABLED. Keep canonical settings unchanged. No gameplay is gated yet; no Expo station is present and nothing is deployed.

Next dependency: Codex's customer/table/dish integration, followed by a precisely scoped per-chef Expo UI assignment. Preserve the approved free movable station, two active orders, fire-before-service, advance prep, normal patience and late-service behavior.
