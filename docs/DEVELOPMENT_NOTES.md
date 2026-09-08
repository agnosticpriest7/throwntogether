# Vertical Slice #1 — first service

The authored `Assets/Scenes/RestaurantDevelopment.unity` scene is the Web entry in `build-config.json`. Bootstrap remains unchanged. Greybox only: one chef, one seated customer and one Fries order.

## Controls and complete loop

- Move: left stick, WASD or arrow keys. Movement is screen-aligned with immediate stopping; CharacterController prevents walking through counters.
- Use/pick up/place: controller south button (Xbox A), E or Space. Face a nearby station; its label highlights and the bottom prompt describes the action.
- Restart the slice: R or controller Start. This resets the entire scene; it is a prototype retry control, not a save system.

Take a potato from the back-left supply. Place it at the central prep island; wait 1.5 seconds and take the cut potato. Place that in the fryer; wait 5 seconds and take the fries. Put fries on the plating counter, take a clean plate from the front-left source, then interact with the fries to plate them. Carry the plated fries to service pickup. The dish automatically transfers to the customer after a short delay; the customer eats and the HUD confirms completion. Plating also works with a plate on the counter and fries in your hands. You can combine an empty plate with ready fries directly at the fryer. Spare and plating counters provide temporary item storage.

## Small reusable foundations

- `ChefController` handles CharacterController movement, proximity/facing selection and interaction. `ChefInput` owns a per-chef Input System action map with optional device binding. No global single-player input singleton; a later join flow can assign devices to separate chef instances.
- `CarrySlot` enforces one item and transfers ownership without duplicating it. `Carryable` displays an `ItemPayload`; raw potato, cut strips, golden fries and plates use different primitive visuals.
- `IngredientDefinition`, `ProcessingRecipe` and `DishRecipe` ScriptableObjects live in `Assets/Data/VerticalSlice`. Prep and fryer share timed `ProcessingStation` logic driven by separate recipe assets. Raw potato is rejected by the fryer; busy stations cannot release their contents.
- `SourceStation` supplies items, currently unlimited. Its supply policy is the future inventory boundary. `CounterStation` supports pickup/place and combining cooked food with a clean plate.
- `ServiceStation` validates `CustomerOrder` against a dish recipe and performs a short placeholder delivery. The order progresses Waiting → Delivering → Eating → Complete and rejects duplicate service. Employee/server AI is absent.
- Chef and carryable prefabs live in `Assets/Prefabs/VerticalSlice`; materials are shared assets. The authored scene contains layout and component references, not a runtime monolithic scene generator. The Editor creation tool is guarded against overwriting an existing restaurant scene.
- Minimal HUD shows order status, nearby station labels/progress, contextual action, carried item and controls. No management UI.

## Known limits

One player/order/recipe only. No throwing, burning, purchases, finite inventory, persistence, dirty dishes, money, reputation or employees. Automatic delivery is an explicit placeholder. Controls have not been physically tested on an Xbox controller or Xbox Edge. TV/couch readability and movement feel require human feedback. Restart discards all slice progress. The shared Input System maps permit future pairing but Player 2 gameplay is not implemented. The infrastructure-era URP warning remains parked unless visible rendering is affected.

## Automated and Editor validation

Live Editor tests: EditMode 3/3 and PlayMode 4/4 passed, including the existing smoke tests. Coverage includes authored recipe progression and timing, raw-potato rejection by the fryer, plate requirements, single-item pickup/place, timed processing, delivery/completion, movement/stopping/range, and synthetic Input System gamepad movement plus south-button pickup. The synthetic gamepad test is not physical controller validation.

The full loop was also exercised through Unity MCP in Play Mode with the chef positioned at each station: actual station interactions and timers produced plated Fries, delivery, eating and Complete. The running scene and visible success HUD were captured and inspected. No new Console errors remained after these checks. The deployment pipeline reruns both suites on the committed snapshot before building and publishing; build provenance remains available at build-info.json.

Batch validation exposed Unity 6000.6 failing to rewrite cached compiler response files between processes. The pipeline now regenerates only Builds/Workspace/Library/Bee/artifacts/rsp before each stage, retaining the larger caches. Synthetic input waits for bounded observed movement rather than relying on a short first-frame timing assumption.

## Exact human playtest checklist

1. Does movement feel responsive?
2. Is the elevated camera comfortable on a TV?
3. Is it obvious where potatoes come from?
4. Is pickup/place interaction intuitive?
5. Does prepping feel responsive?
6. Is fryer progress readable?
7. Is plating understandable?
8. Is service pickup obvious?
9. Is the customer/order readable from couch distance?
10. Does the complete potato-to-fries loop feel satisfying?

Also try raw potato at the fryer, unplated fries at service, interacting while hands are full, and restarting after completion. Record actual platform, input and findings in PLAYTEST_FEEDBACK.md. The public URL is https://agnosticpriest7.github.io/throwntogether/.
