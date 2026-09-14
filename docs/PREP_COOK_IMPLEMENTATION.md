# Prep Cook milestone — September 14, 2026

In progress, not released. Kyle approved one prep cook, five-portion single-type bins, fired-ticket demand, physical work, management assignments, saves, tests and Web deployment. Hot-station cooks remain a later milestone.

Claude's PREP-001 assignment is ready in the collaboration checkout. It owns only the pure reservation ledger and targeted EditMode tests. Codex owns runtime demand snapshots, physical movement, attended processing, bins, hiring/training/assignment UI, persistence and release.

The ledger receives deficits after accounting for compatible physical food. Reservations cover missing portions not represented by that physical snapshot. Update the snapshot before releasing a completed claim. Hold stops new work; food already collected must finish its current safe step and remain available. Serving held orders is unrestricted.

Bins retain five actual Carryable objects through CarrySlots. They accept one Cut ingredient type, reject raw food/plates/mixtures, preserve ownership on failure, and transfer the existing portion on retrieval. Empty bins may switch type. Both players use normal Use interactions without modal UI. Bin contents, like existing counter food, are service-local; purchases/layout/assignments persist. Persistent ingredient inventory is outside this pass.

Release acceptance: hire/train/assign; buy/move/sell/reload bins; fired demand and Hold recovery; no duplicate worker production; player intervention; blocked-route recovery; full regression checks; live memory-only service; no unexplained Console errors; Web build/deployment. Physical TV/controller results must be reported separately by Kyle.

## Integration

Claude PREP-001 commit f51f6a3 reviewed and imported with its original metas and eleven EditMode tests. The reservation contract was also committed in the collaboration checkout (c028d53); unrelated generated drift remains untouched there.

PrepProduction projects active/ready ticket prep steps onto scene-local physical food. Each ingredient portion is allocated once; finished food is preferred over flexible cut/raw food. Player-held portions count. Dining-table food and portions bound to another ticket do not supply a new ticket. Employee-reserved food is excluded until its reservation is released, preventing double-counting. Inputs are currently unlimited, as before.

KitchenPrepCook physically walks between source, prep station and output with existing staff routes/visuals. It never spawns prepared food, changes player controls, or invokes cooking remotely. Source dispensing and staff processing enforce 1.85m reach. Staff chopping uses ProcessingStation's existing recipe/timing and audio observation; the ordinary Update does not advance staff work a second time. Manual attendance remains authoritative for human work. A player taking completed food off the board safely ends the employee's claim.

Hire: $150, once per role, with existing $50/$100/$150 training tiers. Existing 10/20/30% training multiplier applies to walking and staff chopping, not player timing. Prep Bin: $60 repeat purchase, existing counter shop, bay validation, $100 rearrangement and 75% resale rules. Bins preserve the existing modular counter footprint.

Employees → Prep cook assignment offers automatic or specific prep station/ingredient choices using the existing controller menu. Optional schema-1 prepAssignments stores kitchen index, stable furniture ID and ingredient ID. Old saves default to automatic; failed writes do not mutate assignments. Missing assigned stations are shown explicitly and cause waiting rather than silently switching. Purchased equipment and hired roles use existing save structures. Assignments apply next service.

Only one prep cook is hireable in this milestone. Multiple worker competition is tested at the ledger boundary; full kitchen congestion/rerouting around other people and additional kitchen roles remain future work. Existing static staff routing is reused. No wages, ingredient purchase costs, bin upgrades or persistent per-service food inventory added.

## Verification checkpoint

Canonical Unity Assets path verified; initial Console clear. 90 EditMode tests passed. Initial seven bin/worker PlayMode checks passed, including held-order cooking/plating/serving, manual stock and occupied station recovery. Additional controller/management and full-output tests plus release pipeline verification are in progress. No deployment is claimed by this checkpoint.

0.19.0-dev released on 2026-09-14: final source 5ee621111e1628ec2f5e7bda36035db826a4cbc9 passed 90 EditMode + 122 PlayMode + 14 Web checks (226 total). The verified Web payload contains 10 files / 63,659,844 bytes, builtAtUtc 2026-09-14T16:34:20.0980910Z. GitHub Pages commit 26c4733d6353c75fe0b1ac26dc251f2e0613c03b reports built with no error; public build-info independently matches version and source. Development diagnostics preserved. Final regression fixes covered catalog-aware expectations and safe bin fallback positions in all three legacy layouts. Physical TV/controller readability and employee traffic still require owner playtesting; no physical hardware validation is claimed.
