# Development Notes

## Architecture

- Phaser owns the fixed 960×600 canvas and render loop; all artwork is programmatic geometry.
- `TransferScene` coordinates the intentionally small milestone state: two players, one loose/held/in-flight potato, one shared-counter slot, one prep station, and one finish tray.
- Ingredient state uses a small typed state model (`raw`, `prepped`, `ruined`) so later data-driven content is not implied or prematurely implemented.
- Pure side-clamping, landing, and catch rules live in `rules.ts` and have automated unit coverage.

## Input

- The browser Gamepad API is polled every frame. The first two connected pad indices are assigned stably to Players 1 and 2 while connected; assignments are visible beneath the characters.
- Standard mapping is used: axes 0/1, button 0 for interact, button 7/right trigger for throw, and button 9 for reset.
- Both keyboard schemes remain enabled for debugging and simultaneous-input testing.
- Development builds expose an explicit `?test` scenario harness for repeatable browser checks; it is excluded from production by Vite's development flag.

## Throw and catch

- Throws use a short 520 ms deterministic horizontal transfer to a landing point on the opposite side, aligned with and clamped from the thrower's Y position.
- The apparent arc is visual rather than physics-driven. A pulsing landing circle, shadow, item arc, and distinct generated audio cues communicate the incoming item.
- At arrival, catch succeeds only when the receiver is within 68 px and has an empty carry slot. A failure creates a ruined potato and persistent visible mess.

## Shared counter

- The divider's central counter is a single shared slot. Either player can deposit or retrieve any usable potato state while remaining within their own movement bounds.
- Throwability does not restrict shared-counter use; raw and prepped potatoes may both use the safe route.

## Known limitations / human playtesting

- **Two-controller feel and assignment require human playtesting with two physical gamepads.** Automated checks can verify the input architecture but cannot reproduce hardware feel, browser-specific pad IDs, trigger response, or simultaneous wireless input.
- Throw speed, landing distance, catch radius, and interaction distance are initial tuning values and should be assessed by two players side-by-side.
- Browsers require a user input before Web Audio can start; the first pickup/throw interaction provides that gesture.
- The floor mess is gameplay-visible but intentionally has no cleaning mechanic or movement penalty in Milestone 1.
