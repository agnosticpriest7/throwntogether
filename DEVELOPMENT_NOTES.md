# Development Notes

## Architecture

- Phaser owns the fixed 960×600 canvas and render loop; all artwork is programmatic geometry.
- `RestaurantModel` owns the versioned Endless save, cent-based economy, persistent inventory, appliance ownership/installation, expansions, reputation, demand, nightly menu/advertising, ticket queue, service clock, and multi-day summary calculations.
- Recipe, ingredient, bulk tier, appliance, authored slot, expansion, reputation-demand, and advertising definitions live in `data.ts`.
- `RestaurantUI` owns phase overlays and the live HUD. `TransferScene` owns players, carried/in-flight items, stations, processing progress, and physical interactions.
- Management sections are deliberately non-linear: Overview, Pantry, Supplier, Kitchen, Menu, and Marketing can be revisited until Begin Prep locks planning.
- The Phaser kitchen rebuilds its appliance stations from the saved authored slot configuration at Prep. Divider, shared counter, storage, staging, and serving structures do not consume slots.
- Ingredient and dish state uses the typed states `raw`, `chopped`, `assembled`, `cooked`, and `ruined`. Fries uses the same data-driven cooking path with a fryer station.
- Pure side-clamping, landing, and catch rules live in `rules.ts` and have automated unit coverage.

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

## Known limitations

- **Two-controller feel and assignment require human playtesting with two physical gamepads.** Automated checks can verify the input architecture but cannot reproduce hardware feel, browser-specific pad IDs, trigger response, or simultaneous wireless input.
- Two-player multi-touch ergonomics should be assessed on the intended tablet/phone sizes; the responsive layout is functional in portrait and more comfortable in landscape.
- Throw speed, landing distance, catch radius, and interaction distance are initial tuning values and should be assessed by two players side-by-side.
- Browsers require a user input before Web Audio can start; the first pickup/throw interaction provides that gesture.
- The four plates reset between nights; dirty dishes and washing are deferred beyond Milestone 3.
- Saves use one versioned local-storage record and have no cloud synchronization or migration from future incompatible versions yet.
- Appliance upgrades are represented as future data hooks only; no upgrade tree is active.
- Desktop and TV layouts preserve the 16:10 kitchen surface and scale it from available viewport height so the header, game, and control legend fit without page scrolling. Phone layouts retain their taller scrollable presentation.
- Default order patience is 38.5 seconds (10% longer than the original 35-second prototype value).
- Pressing interact away from a station no longer drops or ruins the held item. Intentional disposal uses the shared divider trash chute and records wasted value without creating floor mess. Missed throws still create gameplay-visible floor mess with no cleaning mechanic or movement penalty.
- The nearest counter or appliance within interaction range gets a high-contrast outline and brighter fill for immediate affordance feedback.
