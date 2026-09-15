# BREAKS-001 Astra review — 2026-09-15

Reviewed source: `d2ad51327f1555195dbb5c3b60f71e052d2a15f1`; documentation HEAD `d27c296`. This is a review of deployed 0.27.0-dev, not a fix or release.

## Outcome

Four behavior defects reproduced through Unity MCP in canonical Play Mode. Repair these before extending employee behavior. Keep the current architecture; narrow role cleanup, storage recovery and route fixes should suffice.

Canonical `Application.dataPath` was verified as `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets`. The Editor initially had an empty, clean, unsaved scene. Review used an additive RestaurantShift and a disposable in-memory restaurant account, with time paused and explicit deterministic Advance calls. The saved career was not used. No runtime, scene, asset, build or deployment changes were made.

## Confirmed findings

### 1. Prep cannot recover after a full-counter break request (P1)

Location: `Assets/Scripts/KitchenPrepCook.cs:143-147`, in conjunction with `BreakStorageRoute`.

Reproduction: assign a restocking bin; let prep naturally collect a raw potato; fill ordinary counters; request break; then clear a reachable counter. Advance prep for 30 simulated seconds.

Observed: the worker retains the same raw potato, the reachable counter remains empty, and OnBreak remains false. Display: `Finishing: Assigned station unavailable — food retained`.

Cause: break routing sets Deposit/breakDeposit and clears destination when no counter is free. Subsequent null-destination recovery never retries break storage for raw food. Cut-food recovery instead calls regular DepositRoute, which can also exclude the break-specific counter fallback for restocking jobs.

Repair: route pending break deposits through break-specific live storage recovery regardless of raw/cut state. Add raw/full-counter and cut/full-bin plus full-counter regressions. Preserve item identity and reservations until actual storage succeeds.

### 2. A host on the way outside cannot reach its break home (P1)

Location: `Assets/Scripts/StaffMember.cs:51`; host cancellation calls this from `DiningHost.OnBreakChanged`.

Reproduction: allow the host to naturally approach a queued customer; request break while status is `Greeting next customer` and position is `(6.30, 0, -5.72)`. Advance host for 40 simulated seconds.

Observed: unchanged position, neither GoingToBreak nor OnBreak. Raw status is `Break route blocked`, but the displayed status is `Finishing: Waiting to greet customers`.

Cause: direct indoor grid routing from outside fails; the grid excludes rows below z=-5.2. The existing entrance/sidewalk route needs to bridge to the indoor grid. DisplayStatus also masks the actual travel blocker.

Repair: use valid entrance waypoints from outside, without teleportation; show the actual blocker. Test approach cancellation, safe reservation release, automatic seating, break-home travel, and Resume before reaching home from the actual outside position.

### 3. Dishwasher can wash an additional plate after break request (P2)

Location: `Assets/Scripts/KitchenDishwasher.cs:111-116,139-140`.

Reproduction: take two plates from finite stock, dirty and enqueue them. Let the employee finish the first. Before its next collection tick, have a player collect that clean plate (promoting the second), then request break.

Observed: the employee washes the second plate, returns it, and only then rests. OnBreak=true; second plate dirty=false; sink count=0. It should have left the second plate dirty because the employee had not started it at request time.

Cause: washingOwned is a boolean with no plate identity. Player collection promotes a new Busy plate without clearing this worker's stale boolean.

Repair: tie ownership to the exact plate/work instance and revalidate against the sink. Test manual collection with and without a next plate, player substitution, and ordinary one-plate completion. Do not infer ownership from Busy alone.

### 4. Departed dishwasher still owns washing attendance (P2)

Location: `Assets/Scripts/StaffMember.cs:75-79` and `Assets/Scripts/KitchenDishwasher.cs:91,144`.

Reproduction: let the employee begin a dirty plate (progress 0.0333), invoke the existing BeginDeparture/AdvanceDeparture flow until Gone, then interact with the sink using an empty-handed chef.

Observed: Gone=true, sink Busy=true and Working=true, but player Interact returns false. The washing component remains enabled on the day object even though its walker was deactivated.

Cause: departure does not release role attendance; OnDisable on the dishwasher component does not run when only the separate walker is disabled. Sink continues to reject the player as though the employee were washing.

Repair: an explicit departure cleanup hook must release washing/chopping attendance and applicable claims without discarding physical food/plates. Test departure during active work, not only after workers are already resting. This reproduction directly exercised staff departure; it did not run a complete customer-to-payment closing sequence.

## Checks that passed in this review

- Server naturally collected a meal, its table was cancelled, pass was occupied, and the break path stored the meal on an ordinary counter and reached OnBreak. No food loss observed.
- Cook was initialized in the disposable restaurant with installed Expo, started an actual fries component, then received a break with all counters full. It retained the same finished unplated food. Clearing one reachable counter allowed storage and rest without plating or beginning another component.

The first cook setup contained leftover matching food from the server check, so it did not start the intended appliance job. It was excluded; the passing cook result above is from the repeated check with isolated supply.

## Additional source-review follow-up

`DiningServer.StoreForBreak` uses the server component's transform for route origin and distance, although the physical employee is its separate walker. Live inspection showed component `(0,0,0)` versus employee `(3.6,0,0.8)`. The tested layout recovered, so this review does not claim a reproduced server stall. Repair should use the physical walker and validate transfer proximity; add a route-obstructed-origin regression and Resume during return travel.

Host completes its escort when the host reaches the table approach, not explicitly when the guest reaches its chair. Verify the approved chair-arrival boundary during the repair pass; no failed live escort test is claimed here.

## Verification limits / repair gate

This was a focused review, not another full release gate. The reported 102 EditMode / 174 PlayMode / 14 Web results remain prior-release evidence; they were not rerun here. No automated tests were added. Checks above are executed MCP gameplay probes, not NUnit counts.

Still required in the repair pass: cook appliance-clearing substeps with break/resume and player interference; Resume while carrying or walking home; prep claim cleanup at departure; host escort boundary; controller A/B navigation across all employee rows. Preserve existing tests and add targeted regressions for the four reproductions before a final full release gate. No TV, physical controller or Web/browser validation was performed in this review.

Stop at this review. No fixes, commits, pushes, builds or deployment were performed.

## Subsequent authorized repair

The owner subsequently requested fixes for the four confirmed findings. The 0.27.1 repair addresses those findings and adds nine focused regression cases, including prep departure attendance/claim cleanup and host Resume from outside. The server route-origin and host escort-boundary observations remain follow-up items; this patch does not claim to resolve every additional observation above. Release evidence is recorded in DEVELOPMENT_NOTES.md.
