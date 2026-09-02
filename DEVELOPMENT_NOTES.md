# Development Notes

## Architecture

- Phaser owns the fixed 960×600 canvas and render loop; all artwork is programmatic geometry.
- `RestaurantModel` owns the pure single-night economy, inventory, selected menu, ticket queue, service clock, order results, and summary calculations.
- Recipe and ingredient definitions live in `data.ts`; costs, sell prices, required states, steps, chop times, and cook times are data rather than scene conditionals.
- `RestaurantUI` owns phase overlays and the live HUD. `TransferScene` owns players, carried/in-flight items, stations, processing progress, and physical interactions.
- Ingredient and dish state uses the typed states `raw`, `chopped`, `assembled`, `cooked`, and `ruined`. The abstraction is deliberately limited to the three prototype recipes.
- Pure side-clamping, landing, and catch rules live in `rules.ts` and have automated unit coverage.

## Input

- The browser Gamepad API is polled every frame. The first two connected pad indices are assigned stably to Players 1 and 2 while connected; assignments are visible beneath the characters.
- Standard mapping is used: axes 0/1, button 0 for interact, button 7/right trigger for throw, and button 9 for reset.
- Both keyboard schemes remain enabled for debugging and simultaneous-input testing.
- Touch controls use independent Pointer Events state for each player, including multi-touch D-pad holds and latched Use/Throw presses. They do not synthesize keyboard events.
- Development builds expose an explicit `?test` scenario harness for repeatable browser checks; it is excluded from production by Vite's development flag.

## Throw and catch

- Throws use a short 520 ms deterministic horizontal transfer to a landing point on the opposite side, aligned with and clamped from the thrower's Y position.
- The apparent arc is visual rather than physics-driven. A pulsing landing circle, shadow, item arc, and distinct generated audio cues communicate the incoming item.
- At arrival, catch succeeds only when the receiver is within 68 px and has an empty carry slot. A failure creates a ruined purchased ingredient and persistent visible mess; stock is not restored.

## Shared counter

- The divider's central counter is a single shared slot. Either player can deposit or retrieve any usable kitchen item while remaining within their own movement bounds.
- Ingredients can be thrown; assemblies and cooked dishes use the shared counter as their safe transfer route.

## Milestone 2 human playtest checklist

- Is choosing two dishes a meaningful decision?
- Does buying ingredients make wasted food feel consequential?
- Is purchasing understandable without explanation?
- Does Prep feel useful?
- Are tickets readable while both players are moving?
- Is 2 minutes long enough to expose coordination issues?
- Do throwing and the shared counter both remain useful?
- Does either player fall into a permanent boring role?
- Do the three recipes feel meaningfully different?
- Does the Night Summary make players want another night?

## Known limitations

- **Two-controller feel and assignment require human playtesting with two physical gamepads.** Automated checks can verify the input architecture but cannot reproduce hardware feel, browser-specific pad IDs, trigger response, or simultaneous wireless input.
- Two-player multi-touch ergonomics should be assessed on the intended tablet/phone sizes; the responsive layout is functional in portrait and more comfortable in landscape.
- Throw speed, landing distance, catch radius, and interaction distance are initial tuning values and should be assessed by two players side-by-side.
- Browsers require a user input before Web Audio can start; the first pickup/throw interaction provides that gesture.
- The four plates reset between nights; dishwashing and persistent inventory are intentionally outside Milestone 2.
- The floor mess is gameplay-visible but intentionally has no cleaning mechanic or movement penalty.
