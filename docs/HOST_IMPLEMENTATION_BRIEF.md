# HOST-001 — bounded Sol implementation brief

Design pass: 2026-09-15. Inspected baseline: main 5f761c1, playable 0.25.0-dev. This file is a proposed implementation contract, not evidence the host is implemented. Owner will authorize implementation by supplying the prompt after switching models.

## Player behavior and tuning

- Add one optional Host hire to existing Employee Management. Proposed initial cost: $150, matching the server. No wages, desk, unlock prerequisite, or new equipment slot.
- With no host, preserve today's automatic FIFO seating exactly.
- With host, physically greet the front waiting customer and escort that customer to a clean, reserved table. One escort at a time. Go directly to the next job when available; home only when idle.
- While the host is on duty, outside waiting patience drains at 75% of normal. This is 25% slower decay, not 25% longer patience: a 45-second allowance lasts 60 seconds. It applies to all queued customers, not only the escorted one. It does not affect seated patience, tips, demand, cooking, or Expo.
- Reuse training: $50/$100/$150 and +10%/+20%/+30% host movement speed. No additional patience bonus from training.
- Reuse staff arrival before service, departure after all customers, congestion, editable home, and persistence. Default home must be reachable, to the right of the dining entrance, clear of doorway and existing homes. Find an appropriate clear default rather than moving furniture.
- Existing one-customer-per-seat model remains. No parties, reservations UI, host desk, employee breaks, or new player controls.

## Ownership and integration

RestaurantDay remains sole owner of Guest lifecycle, FIFO selection, table reservation, seating, patience and departure. Guest is currently a private nested object; do not publish a mutable list or let DiningHost directly manipulate phase integers.

Add a small DiningHost state machine using DiningWalker and StaffMember. Introduce the smallest internal day-owned claim/access boundary needed: acquire eligible front guest, read its current position/target, begin admission, complete/cancel escort. A claim must identify this particular guest, not just a table, and become invalid after cancellation/departure/reset. It must not survive reuse of a table. No second ticket/Expo system or persistent service claims.

Suggested state flow: Idle -> Approach queue head -> Escort -> Idle. Validate a route and clean table before acquiring; reserve atomically with the guest claim to prevent duplicate seating. While approaching, the guest remains outside, keeps its place and keeps losing patience. A reserved table alone does not make the guest admitted. At actual greeting, recheck guest, claim, table, patience and closing; only then enter the existing admitted phase. Following customers must not bypass the claimed head.

Escort uses the established dining route and the customer's actual walker. Host leads, customer follows with a small gap; no teleporting or seated visual appearing before arrival. Cap escort pace to what the guest can follow; training may speed greeting/return travel without stretching the pair apart. Avoid naive follow-the-current-position shortcuts through tables. Reuse shared route waypoints where practical. RestaurantDay performs Seat only when the customer reaches its chair; host need only reach the table approach.

If the customer expires or closing occurs before greeting, release the reserved table and claim exactly once, then use existing outside departure. After greeting/admission, allow completion after 10 PM as today. Dirty/occupied tables never qualify. Preserve current seat-release timing at the departing customer's aisle; do not wait for them to leave the building.

If a route cannot be obtained, do not reserve indefinitely or silently teleport. Release unstarted work, retain FIFO, show a small existing-style blocked status and retry. No day-loop hang caused by a leaked reservation. Unexpected host destruction must release an unstarted claim; already admitted customers should finish their existing route. Absent/unavailable host must not strand the restaurant permanently.

## Patience and UI correctness

Current outside expiry uses Elapsed - guest.arrived; replacing it with an accumulated waiting budget is appropriate for variable decay. Start that budget at the existing phase-4-to-phase-0 arrival boundary. Do not change seated SeatedAt calculations. Keep real elapsed wait separate where useful; do not falsify timestamps to implement the bonus.

Update the existing outside countdown in RestaurantHud to use the same effective remaining patience source as expiry. Currently it subtracts OldestWait from outsidePatience, which would become misleading. Define whether display is effective wall-clock seconds and test it; recommended display is remaining wall-clock seconds at current decay rate. Expose the 25% outside-patience benefit in the hire description. Add at most a compact host status consistent with existing employees; no new large HUD panel.

## Files to inspect/change as needed

- New DiningHost.cs and .meta, focused tests and .meta, Host employee asset and .meta.
- RestaurantDay.cs: admission boundary, waiting budget, host creation/tick.
- DayServiceDefinition.cs and Assets/Resources/ServiceDay.asset: host role/configuration.
- RestaurantMenu.cs: include host in existing employee list and training.
- StaffHomes.cs: append host role, safe default and editor marker. Do not change old role IDs or saved homes.
- StaffMember.cs only for necessary lifecycle integration; it already supports arrival, idle and departure.
- RestaurantHud.cs: correct outside countdown and compact status if necessary.
- RestaurantAccount.cs only if existing generic purchase/training storage genuinely needs adjustment. Prefer existing string IDs and optional data; no gratuitous schema bump.
- DiningTable.cs only if a narrowly scoped safe reservation cancellation method is needed. Never clear a new occupant's order.
- Existing tests may require adding the sixth role; preserve all five-role behavior and assertions.

Do not rebuild scenes, rerun broad authoring scripts, rewrite routing, change the grid, remodel characters, modify camera, or touch cooking/Expo behavior. Existing 0.25.0 layout and staff changes still await owner TV testing.

## Acceptance tests

1. No-host baseline retains FIFO, clean-table checks, patience and closing semantics.
2. Hire/save/load/training work through normal management; old saves default to no host. Failed persistence stays atomic.
3. Sixth home selectable by controller, valid/save/reload/cancel; default does not block entrance.
4. All staff enter before customers/clock; host exits after final customer; settlement still completes.
5. Host approaches actual queue head and escorts physically. No seating before customer arrival, no duplicate table reservation, no bypass by later guests.
6. Dirty table rejected; cleared and vacated table usable while earlier customer is still walking outside.
7. Outside decay is .75 with host and 1 without; seated patience unchanged; HUD countdown agrees. Expiration during approach releases claim/table.
8. Closing during approach cancels admission; closing during admitted escort allows completion. No leaked claims on reset/destroy or reused table.
9. Blocked route recovers without teleport or permanent reservation; multi-table expansion works.
10. Existing service/Expo/cook/busser/dishwasher/co-op tests remain green.

## Efficient execution and release

Inspect current HEAD/diff and AGENTS.md first; preserve unrelated work. Follow docs/USAGE_BUDGET.md. Implement only this feature. Run focused tests while iterating, then full release checks once on final code. Prior 0.25 baseline was 102 EditMode + 164 PlayMode + 14 Web checks; discover current counts rather than hardcoding success.

Use Unity MCP for a disposable memory-save live review: hire, opening, queue, escort, dirty/cleared table, closing. Verify Application.dataPath is C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets before mutations. Never run MCP while a batch Unity Editor is active: it can attach to the isolated test/build project. Do not overwrite the owner's career save or stop production Unity processes. Finish script changes before live review to avoid domain-reload state loss.

After checks pass, document behavior/tuning/limitations, commit and push, build and deploy Web development using the established release workflow. Verify public build-info matches the built source commit. Preserve diagnostics. No claim of physical TV/controller testing. Stop after reporting files, results, build identity and remaining physical checks. If a cross-system redesign appears necessary, report the concrete blocker instead of expanding scope.
