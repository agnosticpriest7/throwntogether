# Employee breaks

During an active career service, pause and open **Employee Controls** to ask an individual employee to take a break or resume. Breaks are free, have no timer, and last only for the current service day. Controls are unavailable while staff are arriving, during final departure, and in between-day management.

The shared `StaffMember` owns break intent, travel to the configured staff home, rest, resume and departure priority. Each role remains responsible for reaching a safe stopping boundary before calling `StaffMember.BeginBreak`:

- Prep cooks finish only a portion already being chopped. Raw food already carried goes to a free ordinary counter; prepared food prefers its assigned compatible bin and then a free counter.
- Cooks finish only a component already cooking. They store held food or partial plates on a free counter, and put a complete meal on the pass when possible. They do not acquire a plate, load another appliance or start the next component after a request.
- Servers finish a valid delivery already in hand. Otherwise they return the dish to the pass or a free counter and release their delivery claim.
- Bussers return a dirty plate already in hand to the dirty rack and do not collect another table.
- Dishwashers finish the particular plate they already own at the sink, return that clean plate to stock, and stop before the sink's automatically promoted next plate.
- Hosts complete an escort after admission. A host merely approaching the outside queue releases the guest/table claim immediately.

Food, dishes and reservations are never deleted or teleported to finish a break. If every valid destination is occupied, the employee remains in **Finishing task** with a concrete blocker and retries live state. Resume can be used during finishing, while walking home, or while resting; the role replans from current position and inventory.

While a host is taking a break, approaching home or resting, normal FIFO automatic seating is restored and outside patience drains at the normal rate. An already admitted escort remains protected and finishes at its reserved chair. Resume restores the host's patience benefit immediately.

At closing, final staff departure overrides all breaks. Staff still preserve held items through the existing departure stow flow, then leave after the last admitted customer. Starting the next service creates fresh working staff; break state is deliberately not saved.

TV/controller review should confirm the six-row page remains readable at couch distance, A toggles the intended employee, B returns to pause, host fallback seating is understandable, and crowded layouts still leave usable routes to configured staff homes.

0.27.1 recovery fixes: pending prep breaks retry newly freed counters for both raw and prepared food. Hosts outside use the existing authored entrance crossing to return home or resume greeting; blocked home routes report their actual blocker. Dishwasher ownership follows the specific plate rather than the sink's Busy flag, so manual collection cannot make a break process the next queued plate. Final departure explicitly releases dishwasher attendance and prep attendance/claims while preserving physical contents for players.

0.28.1 follow-up: servers route storage from the employee's physical location, verify nearby placement, and resume correctly with carried meals or during home travel. Hosts finish an escort at the guest's chair-arrival boundary. Cook parking retains break intent through blocked outputs and appliance-clearing recovery. Full storage still legitimately requires the player to free a counter; no food is discarded to force an employee onto break.
