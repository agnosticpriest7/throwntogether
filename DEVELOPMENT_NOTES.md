# Development Notes

## Architecture

- Phaser owns the fixed 1280×600 shared restaurant canvas and render loop; all artwork is programmatic geometry. The 32:15 surface keeps the divided kitchen and dining room readable together and scales from available TV viewport height.
- `RestaurantModel` owns the versioned Endless save, cent-based economy, persistent inventory/equipment/staff, expansions, reputation/demand, customer/table state machines, task reservations, finite plates, service clock, and multi-day summaries.
- Recipe, ingredient, bulk tier, appliance, authored kitchen slot, authored table, staff role/candidate, timing, expansion, reputation-demand, and advertising definitions live in `data.ts`.
- `RestaurantUI` owns phase overlays and the live HUD. `TransferScene` owns players, carried/in-flight items, stations, processing progress, and physical interactions.
- Management sections are deliberately non-linear: Overview, Pantry, Supplier, Kitchen, Menu, Staff, and Marketing can be revisited until Begin Prep locks planning and charges scheduled payroll.
- The Phaser kitchen rebuilds its appliance stations from the saved authored slot configuration at Prep. Divider, shared counter, storage, staging, and serving structures do not consume slots.
- Ingredient and dish state uses the typed states `raw`, `chopped`, `assembled`, `cooked`, and `ruined`. Fries uses the same data-driven cooking path with a fryer station.
- Pure side-clamping, landing, and catch rules live in `rules.ts` and have automated unit coverage.
- `ArtFactory.ts` owns reusable character construction, customer/staff variants, palette constants, and ingredient/dish illustration. `TransferScene` composes those parts with station-specific appliance/environment vectors; gameplay coordinates are unchanged.

## Dining, staff, and plate architecture

- Customer parties are deterministic solo/two-person entities with arriving, table-wait, walking/seating, food-wait, eating, leaving, and failed states. One prototype order is created per seated party and retains customer/table identity.
- Tables use authored positions and move through clean, reserved, waiting-food, eating, dirty, and clean-again states. Level 1 exposes three two-seat tables; Dining Expansion I exposes two additional authored tables.
- Servers use an explicit priority state machine: deliver reserved pickup dish, seat a waiting party, clear a dirty table, idle. Dish/customer/table reservations prevent two servers claiming one task. Movement is represented over authored safe kitchen-boundary/table/entrance destinations; complex collision avoidance is intentionally omitted.
- The service boundary has three pickup slots. Pickup validates a matching plated order but does not pay; successful server delivery credits revenue. Ready food whose customer abandons is removed and earns nothing.
- Six plates are conserved between clean stock, plated/in-use food, dirty return, and claimed washing work. Servers create dirty-return work after eating. A human Use interaction at the sink or a visible dishwasher task washes one plate for 2.5 seconds and returns it clean.
- Hired staff persist with stable ID, name, role, color variation, and the last selected schedule. Only scheduled staff spawn. Payroll is affordability-checked and charged once at Begin Prep; the saved charged-day marker prevents a restart from charging twice.
- Press F3 to toggle the developer AI overlay. It is off by default and shows employee state, task, reserved target, and destination.

## Input

- The browser Gamepad API is polled every frame. The first two connected pad indices are assigned stably to Players 1 and 2 while connected; assignments are visible beneath the characters.
- Standard mapping is used: axes 0/1, button 0 for interact, button 7/right trigger for throw, and button 9/Start to begin Prep when the menu is ready or open Service during Prep. Keyboard R remains the restart shortcut.
- In management screens, buttons 6/7 (left/right triggers) switch Planning Hub tabs, stick/D-pad input uses spatial focus navigation, and button 0 confirms. Focus is restored across Planning Hub redraws so repeated purchases and menu selections remain controller-accessible.
- Both keyboard schemes remain enabled for debugging and simultaneous-input testing.
- Touch controls use independent Pointer Events state for each player, including multi-touch D-pad holds and latched Use/Throw presses. They do not synthesize keyboard events.
- Planning buttons are native keyboard/touch controls. A lightweight management poll lets either connected gamepad navigate focus with the stick/D-pad and select with the south face button.
- Development builds expose an explicit `?test` scenario harness for repeatable browser checks; it is excluded from production by Vite's development flag.

## Throw and catch

- Throws use a short 520 ms deterministic horizontal transfer to a landing point on the opposite side, aligned with and clamped from the thrower's Y position.
- The apparent arc is visual rather than physics-driven. A pulsing landing circle, shadow, item arc, and distinct generated audio cues communicate the incoming item.
- At arrival, catch succeeds only when the receiver is within 68 px and has an empty carry slot. A failure creates a ruined purchased ingredient and persistent visible mess; stock is not restored.

## Shared counter

- The divider's central counter is a single shared slot. Either player can deposit or retrieve any usable kitchen item while remaining within their own movement bounds.
- Ingredients can be thrown; assemblies and cooked dishes use the shared counter as their safe transfer route.

## Milestone 3 human playtest checklist

- Does persistent cash make later nights more interesting?
- Is bulk buying tempting?
- Does bulk stock influence menu choices naturally?
- Is it obvious leftovers persist?
- Does finite appliance space create real choices?
- Is appliance swapping understandable?
- Does buying the fryer feel like gaining a new capability?
- Does Fries make the fryer meaningful?
- Do players want to save for kitchen expansion?
- Is dining capacity understandable without table simulation?
- Does reputation clearly explain baseline turnout?
- Does advertising feel like voluntary risk/reward?
- Can players knowingly create too much demand?
- Does the restaurant feel increasingly like theirs?
- Is Planning quick enough that players still want to get back to cooking?

## Milestone 4 human playtest checklist

- Does the dining room make the restaurant feel alive?
- Can both human players remain focused on cooking?
- Is the service pickup obvious?
- Is watching servers deliver food satisfying?
- Is one server a meaningful bottleneck?
- Does hiring a second server feel valuable?
- Do wages create a real economic choice?
- Is it obvious why a customer left?
- Does table turnover make dining expansion meaningful?
- Does running out of clean plates create good pressure?
- Is human dishwashing useful rather than miserable?
- Does hiring a dishwasher feel like a meaningful upgrade?
- Can humans still help dishes while the dishwasher works?
- Is the dining room readable without distracting from cooking?
- Does advertising feel more dangerous/interesting now?
- Does the whole thing feel more like an operating restaurant than a cooking minigame?

## Known limitations

- **Two-controller feel and assignment require human playtesting with two physical gamepads.** Automated checks can verify the input architecture but cannot reproduce hardware feel, browser-specific pad IDs, trigger response, or simultaneous wireless input.
- Two-player multi-touch ergonomics should be assessed on the intended tablet/phone sizes; the responsive layout is functional in portrait and more comfortable in landscape.
- Throw speed, landing distance, catch radius, and interaction distance are initial tuning values and should be assessed by two players side-by-side.
- Browsers require a user input before Web Audio can start; the first pickup/throw interaction provides that gesture.
- Customer groups are limited to one- or two-person parties and use one clearly associated prototype order per party rather than one order per individual.
- Navigation uses authored destinations and timed deterministic movement/state changes. Basic AI overlap is accepted; there is no general-purpose pathfinding or crowd avoidance.
- Servers and dishwashers are the only AI roles. They have no traits, levels, morale, training, or emergent recipe logic.
- Saves use one versioned local-storage record, migrate v1 to v2, and have no cloud synchronization or guarantee for unknown future versions.
- Appliance upgrades are represented as future data hooks only; no upgrade tree is active.
- Desktop and TV layouts preserve the 32:15 whole-restaurant surface and scale it from available viewport height so the header, game, and control legend fit without page scrolling. Phone layouts retain their taller scrollable presentation.
- Default order patience is 38.5 seconds (10% longer than the original 35-second prototype value).
- Pressing interact away from a station no longer drops or ruins the held item. Intentional disposal uses the shared divider trash chute and records wasted value without creating floor mess. Missed throws still create gameplay-visible floor mess with no cleaning mechanic or movement penalty.
- The nearest counter or appliance within interaction range gets a high-contrast outline and brighter fill for immediate affordance feedback.
