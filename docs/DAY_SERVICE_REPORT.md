# End-of-day service report

0.28.0 keeps the existing Day Complete management flow and puts its actions beside a readable service report. No extra confirmation page or controls were added.

Customer outcomes show arrivals, served customers, outside-patience losses, seated unserved losses, and closing-time turnaways. Closing turnaways include guests still approaching the door and do not increase the existing LostCustomers total. Counters are recorded once at existing lifecycle transitions, and remain unchanged after settlement.

Earnings show dish sales, speed bonuses, menu variety, food-waste fees and the final banked payout. The payout retains the existing zero floor. Speed and variety explanations use the existing rules: speed decreases over the configured window; variety requires at least four selected and four different served dishes, and pays 5% of dish sales rounded down, capped at $15. Merely selecting four dishes does not qualify.

The short improvement note is advisory, not a claimed diagnosis of staff utilization. Outside losses suggest checking table turnover/host access and possible seating expansion. When queues and dirty tables actually overlapped during service, it suggests clearing/busser help instead. Unserved diners suggest checking cooking capacity, delivery help or menu workload. No patience losses produces a conservative suggestion to retain the layout before expanding.

The report describes the just-completed live service. It is not a saved history or a new progression system; reopening an existing career still opens the existing pre-service setup. Economics, customer patience, spawn schedules, staff and recipes are unchanged.

The right-hand menu retains Employee Management, Arrange Kitchen, Appliances/Counters/Dishes, chef appearance, Next Day/menu selection, Settings, Quit and Reset. A failed payment retains its Retry action; all nine possible rows fit. Existing D-pad/A/B navigation remains.

Validation: focused PlayMode checks cover customer reconciliation, closing-time counting, dirty-table evidence, controller traversal/return and the existing menu-variety/payment flow. Canonical Unity review used disposable memory storage and an actual served meal: 10 arrivals = 1 served + 6 outside losses + 3 unserved; $10 sales + $5 speed - $1 waste = $14 banked. Normal and maximum text sizes were captured and inspected under docs/art-reference/day-report/. No Console errors. Full release evidence is in DEVELOPMENT_NOTES.md. Physical couch/TV legibility remains for the owner to assess.
