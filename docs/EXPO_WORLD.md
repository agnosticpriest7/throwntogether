# In-world Expo — 0.24.0

## Interaction
The normal HUD no longer calls ExpoSharedPanel or ExpoOrderStrip. Existing dish bubbles are the selection targets; a 46/720-pixel contextual hint replaces the board. The old panel code is retained for compatibility, not shown in normal play.

Stick/D-pad selects spatially in all four directions. Candidates must lie ahead of the input direction; distance plus a sideways penalty chooses the nearest suitable bubble in camera space. There is no wrap at an edge. Selection retains its ticket Guid, and vanished orders still require a fresh confirmation before acting on a replacement. A toggles Hold/Fire, X promotes a fired ticket one rank, B exits. Keyboard uses existing movement/E/Q plus X. Each press requires releasing the navigation axis before another directional move.

RestaurantExpo owns one transient operator lease across every desk in that restaurant. Other chefs continue normal play. Closing, disabling, losing focus, leaving reach, closing the station or ending service releases or invalidates the lease. Public commands recheck it.

## Priority
KitchenTicket.Priority is a read-only public rank (0 = not fired). KitchenTicketBoard owns a separate priority list and exposes PrioritizedTickets. Fire appends, TryPromote swaps upward by one, Hold/Serve/Cancel remove and renumber. Ready retains rank. FireSequence and ActiveTickets order remain unchanged, so promotion does not change bay assignment. RestaurantExpo exposes PrioritizedTickets, TryPromote(ticket), and PriorityFor(DiningTable).

The current service has one ticket per seated table and one recipe per ticket, potentially with several components. Every component of that table's compound meal shares its ticket priority. Separate multi-dish customer parties are not implemented; future party order work must retain one table/party ticket as its priority owner. No patience or save-schema changes: orders/ranks remain transient per service.

CookProduction allocates food in priority order, then held tickets. The existing cook uses that ordering when seeking work and continues searching lower orders if a component/resource is unavailable. No preemption or multi-cook scheduler was added. Work already in hand follows existing safe completion/Hold behavior. Priority is a resource preference, not a finish-one-table barrier. Manual serving and servers remain unaffected by Hold or priority.

## Presentation
Existing food icons and patience bars remain. Fired orders gain a green border and numbered FIRED/READY badge; held orders have a red border and HOLD badge. The operator's selection has an additional yellow outline. Feedback is anchored to actual bubbles and remains visible to everyone when Expo is closed. Capacity still comes from the existing board (currently two active slots); a refused fire reports EXPO FULL in the small hint.

## Files
Runtime: ChefInput.cs, ExpoTicketBrowser.cs, ExpoWorldPresentation.cs (new), RestaurantHud.cs, RestaurantExpo.cs, KitchenTicket.cs, KitchenTicketBoard.cs, CookProduction.cs.
Tests: KitchenTicketTests.cs, ExpoStationTests.cs, CookTests.cs.
Metadata: build-config.json; documentation and screenshots under docs/art-reference/expo-world/.

## Verification
Targeted run passed 15 EditMode and 38 PlayMode cases before two additional cook-priority cases and final lease guards. Updated obsolete control expectations intentionally (A toggle, one operator, spatial selection); retained all delivery/staging/identity/capacity regressions. Live canonical Unity used empty-scene entry and disposable in-memory storage: real seating, A Fire, Down selection, A Fire, X promotion, A Hold, Ready feedback and B exit. Actual gameplay-camera screenshots show the full restaurant with only the small hint and bubble feedback. Original Editor scene restored without saving.

Full pipeline results will be recorded in DEVELOPMENT_NOTES.md. Physical Xbox/TV feel remains owner validation. No new party system, patience design, employee role, recipe or bay-capacity upgrade in this pass.
