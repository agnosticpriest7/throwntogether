# BREAKS-001 — employee breaks implementation contract

Design pass: 2026-09-15, after HOST-001 / 0.26.0-dev (source d542f19, documentation b641fa6). Read current source and applicable AGENTS.md before implementation. This document is a design, not an implemented feature. Owner authorizes implementation by submitting the accompanying Sol prompt.

## Outcome and scope

During service the player can ask any one of the six employees to take a break and later resume. This is especially useful for stopping prep-bin restocking. No hire is lost, no fee is charged, and no new fatigue, wages, schedule, timer or automatic resume system is added.

Use four visible states: Working; Finishing task (with a concrete blocker if necessary); Going to break; On break. An employee on break stays visibly at their configured home, idle, and takes no jobs. There is no minimum break duration. Resume works while finishing, walking home or already resting. Reset all break requests at next service: breaks are session/day state, not new save data.

Only active, hired employees receive controls. Disable them during opening arrival, final staff departure and management. Permit breaks after 10 PM while admitted customers are still being served; departure overrides breaks when the existing closing conditions are met. Preserve the established wait-for-last-customer and staff-exit-before-payment sequence.

## Controller UI

Add Employees to the ordinary in-service pause menu, near Recipe book. Reuse existing navigation/paging/focus. During service this opens a small Employee Controls page, distinct from between-day hiring/training: one row per active employee containing concise role, status, and Take break or Resume action. Example: Prep cook — Working — Take break; Cook — Finishing: needs free counter — Resume.

A applies/toggles the request, B returns to the pause menu, existing Resume / B then returns to play. Keep the existing pause behavior: the restaurant stays paused while this page is open; work/home movement resumes when the player closes it. Clearly acknowledge the request. Both controllers follow existing shared pause-menu ownership, with no new bindings or duplicate panels. If a row disappears, clamp selection safely. Keep existing prep/cook HUD panels; give break status precedence there. Do not add a permanent large employee overlay or redesign management.

## Safe stopping rules

The boundary is one already-started piece of work, not every job a state machine might chain next.

| Role | Finish before resting | Do not begin after request |
| --- | --- | --- |
| Prep cook | If chopping has begun, finish that one portion and store it. If already carrying raw food but chopping has not begun, set it on an ordinary counter. For cut food, prefer assigned compatible bin, then reachable empty ordinary counter as a break-specific fallback. | No fresh ingredient retrieval and no further chopping/refill. |
| Cook | If food is already cooking under this cook's current job, let that component finish, retrieve and put it away. If already carrying food/partial plate, put it away. If already carrying a complete meal, prefer normal pass/Expo staging, else an empty counter. | No fresh retrieval, appliance loading, plate acquisition, extra component, or multi-step completion of the entire meal. |
| Server | Deliver a meal already in hands if the diner is still valid. Otherwise return it to the available pass or an empty counter. Release its delivery claim when delivery/storage completes. | No pickup on the way to the pass if hands were empty when requested; no next delivery. |
| Busser | Return a dirty plate already carried to the dirty rack; if unavailable, retain it or use an ordinary counter as fallback. | No new table pickup. |
| Dishwasher | Finish the particular plate it is actively washing, return that clean plate to stock, then stop. A dirty plate already carried goes into the sink without starting washing. A clean plate already carried goes to stock. | No next plate from rack/sink and no next wash cycle. Busy sink alone does not mean this employee has begun washing: require actual work ownership. |
| Host | Finish an already-admitted escort through the customer reaching their chair. If only approaching an outside customer, cancel that unstarted claim/table reservation and return home. | No new greeting or escort. |

All transfers remain physical and validated at arrival. Never delete, duplicate, teleport or silently drop food/plates; never overwrite an occupied slot. When storage is full or unreachable, remain Finishing task with e.g. "Needs free counter" or "Dirty rack blocked" and permit Resume. Retry against live state. A valid compatible bin/counter becoming free should unblock it. Do not allow a generic fallback to put dirty plates in prep bins or prep food in plate stock.

Cook special cases: current code has a multi-frame appliance-clearing swap. A break during it must safely finish only the in-progress transfer/store, not reload the parked input. Preserve both parked input and appliance output. If the player removes/replaces the tracked food, release stale references and re-evaluate what the worker actually owns; do not retrieve some new item to finish a cancelled job. Partial plates already on counters stay there. Autonomous appliances keep their normal timers. A request must not accidentally call Park's fired-order re-plating path and resume recipe work.

Dishwasher special case: Taking a clean plate advances the sink queue and marks the next plate Busy. Do not count that new Busy plate as another already-started wash; release worker attendance and stop after returning the original plate. No endless drain-the-sink loop.

Prep claims: preserve the existing refresh-physical-deficits-before-release ordering, and keep reservations only while genuinely completing/storing the original portion. Do not release-and-reacquire work in a loop while break is pending.

## Host availability policy

At the moment a host break is requested, the host stops offering the 25% outside-patience benefit and normal automatic FIFO seating becomes available. An already-admitted escort still completes; its occupied/reserved table must never be selected by automatic seating. An unstarted approach reservation is released before automatic seating considers that guest. Host returns to its configured home via a valid entrance route even if it is currently on the sidewalk.

Restore the host benefit on Resume and allow new greetings from the host's actual position. Accumulate waiting budget continuously at the currently applicable rate (1 normal, .75 on duty); never reset a guest's wait. The HUD countdown must agree with current rate. Merely checking host != null is insufficient: 0.26.0 uses that check both for the bonus and automatic seating. Introduce a single explicit host availability query and apply it consistently. This does not require a new customer/Expo architecture.

## Implementation ownership

StaffMember should own the request and shared resting/home-travel state. Each role remains responsible for reaching its safe stopping boundary and releasing its own reservations/attendance. Do not simply make AllowWork false at request time: that freezes held food and in-progress transfers. Gate new work at role boundaries, then hand home travel to StaffMember once hands, claims and attendance are settled.

Prefer a small role callback/interface or explicit methods sufficient for these six roles, not a generic job scheduler. Track intent separately from work phase. Repeated requests are idempotent. Resume during home travel must not require reaching home before responding to work. On resume, replan from live demand, inventory, ticket state and position; discard stale selections. Preserve Fire/Hold and priority; breaks never change tickets.

Departure uses its existing higher-priority cleanup/exit flow, including staff already outside. Ensure arrival/role/day loops advance a walker once per tick; break-home travel must not be counted twice. No deactivating worker GameObjects to implement breaks. No scene/prefab remodel or character-scale/routing rewrite.

Primary files to inspect:
- StaffMember.cs, StaffHomes.cs, RestaurantDay.cs, RestaurantMenu.cs, RestaurantHud.cs.
- KitchenPrepCook.cs / PrepProduction.cs; KitchenCook.cs / CookProduction.cs.
- DiningServer.cs, DiningBusser.cs, KitchenDishwasher.cs / WashingStation.cs, DiningHost.cs.
- Relevant tests: StaffLayoutTests, HostTests, CookTests, PrepCookTests, RestaurantDayTests, ExpoStationTests, and existing input-menu cases.

No account schema or employee pricing changes should be needed. Break intent belongs to live employees. Use purchase IDs consistently (busser maps to hire-busser, dishwasher to hire-dishwasher).

## Required verification

Add focused regressions based on observable outcomes rather than mirroring branch logic:

1. Each role can request/resume; resting employees do not work despite available demand; repeated requests are safe.
2. Restock prep finishes only the in-progress portion, stops with bins unfilled, resumes filling without duplicate claims/items. Raw-food and full-bin/counter cases preserve food.
3. Cook request during fetch, loading, active cooking, compound assembly and appliance-clearing transfer preserves all items and prevents the next component. Check full-pass/counter recovery and player interference. Existing Fire/Hold priority behavior passes.
4. Server completes a carried delivery but does not collect the next; cancelled customer releases claim and retains/stores dish.
5. Busser completes only the held plate return. Washer completes one actively worked plate, does not process the automatically promoted next plate, releases attendance so a player can wash.
6. Host request during approach releases reservation and permits automatic seating; request during admitted escort finishes it. Outside budget remains continuous across rate changes. Resume from outside/home works.
7. Request/resume while walking home, after closing while serving, final departure overriding break, and next-day reset. No payment/closing hang.
8. Controller navigates all employee rows, A applies and B returns properly; simulation stays paused while the menu is open. Correct statuses appear without extra large HUD.
9. Other employees and both players continue working normally after closing pause; no global work pause caused by one employee break.

Run focused tests while iterating. Then full EditMode, PlayMode and Web checks through the established release pipeline once on final source. Baseline is 102 EditMode + 169 PlayMode + 14 Web checks; discover actual counts. Avoid an extra full candidate run immediately before the same full committed release gate unless a concrete unresolved concern needs it.

Use Unity MCP for live review with a disposable memory account: prep bins partially filled -> break -> single portion stored -> home -> resume; cook active component break; host approach/escort break; closing. Capture actual gameplay/menu view where possible, and check Console before/after. Verify canonical Application.dataPath before mutations. Never use MCP while a batch Editor is active (connection may attach to the wrong Editor). Finish scripts before live tests to avoid domain-reload state loss. Never alter the owner's saved career or terminate production Editor processes.

Update development notes and staff documentation with stopping rules, limitations and release evidence. Commit/push, build/deploy through the established Web workflow only after verification passes, preserve diagnostics, and verify public build-info matches the tested source. Stop after release; no subsequent feature work. Report exact checks and any unverified cases honestly. Physical TV/controller validation belongs to the owner.

## Escalation boundary

If implementing these rules requires a broad rewrite of jobs, customer ownership or routing, stop and explain that concrete dependency for Astra review. Narrow break hooks and ownership cleanup are in scope. Do not bundle an unrelated HOST-001 audit or other backlog items. This design inspection is not a certification of the previous host implementation.
