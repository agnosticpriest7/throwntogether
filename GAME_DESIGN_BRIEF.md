# Thrown Together — Game Design Brief

Working title. This repository copy is the current design reference through Milestone 3.

## Project goal

Build a fixed-screen local co-op cooking game for two players on the same PC. The central fantasy is that two people choose how ambitious their restaurant will be, invest real restaurant money, configure a kitchen they own, and live with those decisions during service.

The core design pillars are:

1. Players choose difficulty through their nightly menu and demand decisions.
2. Ingredients are purchased persistent inventory; waste is a real financial loss.
3. Throwing loose ingredients is fast while the central shared counter is safe.
4. The kitchen itself—owned equipment, installed capacity, and workspace—is progression.
5. Chaos should come from player decisions, coordination, limited capacity, and service pressure rather than environmental gimmicks.

## Endless restaurant

Endless Mode is one persistent restaurant across an unlimited sequence of days:

1. Review cash, reputation, pantry, kitchen, and dining capacity.
2. Buy bulk ingredients and capital equipment.
3. Store, install, move, or swap owned appliances in finite authored slots.
4. Choose two dishes from recipes enabled by installed equipment.
5. Optionally advertise to increase this night's demand.
6. Begin locked Planning, enter untimed Prep, and open for timed Service.
7. Complete or miss paced customer tickets.
8. Review operations, finances, inventory, and reputation.
9. Persist the restaurant and begin the next day.

Menus may change nightly. Ingredients, cash, appliance ownership and installation, expansions, and reputation persist. Ingredients have no spoilage, freshness, expiration, or stock decay in the current design.

## Persistent save

Use a versioned browser local-storage record. Persist at minimum:

- Day number and restaurant cash
- Ingredient inventory
- Appliance ownership and installed authored slots
- Kitchen and dining expansion levels
- Reputation points and visible level
- In-progress nightly planning selections and accounting where needed to prevent double charging

Autosave meaningful planning changes and completed nights. Provide New Restaurant, Continue Restaurant, and a confirmed Reset Endless Save action.

Prototype starting restaurant:

- Day 1
- $150 cash
- Empty pantry
- Reputation Level 1
- Kitchen Level 1 with four core workstations installed
- Dining Level 1 with capacity for 10 customers

## Planning Hub

Planning is a controller-friendly hub rather than a rigid wizard. Players may revisit:

- Restaurant Overview
- Pantry
- Supplier
- Kitchen
- Menu
- Marketing

Begin Prep intentionally locks purchasing, advertising, menu changes, and kitchen configuration until the following day.

## Economy and pantry

Use one restaurant currency for ingredients, advertising, appliances, and permanent expansions. Reputation is not currency. Use integer cents internally.

Bulk ingredient discounts are data-driven:

| Quantity | Discount |
|---:|---:|
| 1–4 | 0% |
| 5–9 | 5% |
| 10–19 | 10% |
| 20+ | 20% |

The Supplier shows chosen quantity, total, effective per-unit price, discount tier, current cash, and owned quantity. Players may intentionally overbuy or underbuy; do not recommend an optimum. Leftovers carry forward without decay.

Completed dishes add their recipe sale price to persistent cash. Ingredient waste is reported but receives no duplicate fine.

## Kitchen ownership and capacity

Owned and installed appliances are separate. Stored appliances do not enable recipes. Equipment may only be reconfigured during Planning and occupies fixed authored positions.

Kitchen Level 1 has four active appliance/workstation positions. Kitchen Expansion I costs $400 and permanently increases this to six visibly usable positions. The divider, shared counter, storage, staging counters, and serving destination do not consume slots.

Prototype purchasable equipment:

- Extra Prep Station — $60
- Second Oven — $100
- Fryer — $120

Definitions retain hooks for future tangible upgrades such as extra racks, baskets, or batch capacity, but no upgrade tree exists yet. There is no repair, degradation, maintenance, or random-breakdown system.

## Recipes and physical cooking

Recipes, ingredients, required states, equipment requirements, processing times, and prices are structured data. Current dishes are:

| Dish | Ingredients | Required installed equipment | Steps | Sale |
|---|---|---|---|---:|
| Roast Potato | Potato | Prep Station, Oven, Plating Station | Chop, bake, plate, serve | $8 |
| Garden Plate | Tomato, onion | Prep Station, Plating Station | Chop both, combine on plate, serve | $10 |
| Cheese Bake | Potato, cheese | Prep Station, Assembly Station, Oven, Plating Station | Chop, assemble, bake, plate, serve | $15 |
| Fries | Potato | Prep Station, Fryer, Plating Station | Chop, fry, plate, serve | $12 |

Players may select a recipe despite insufficient stock. A recipe with missing installed equipment is unavailable and explains what is required. Equipment is never auto-installed.

## Demand, reputation, dining, and advertising

Persistent reputation points map to visible Levels 1–10 and baseline nightly customer demand:

| Level | Baseline demand |
|---:|---:|
| 1 | 8 |
| 2 | 10 |
| 3 | 13 |
| 4 | 16 |
| 5 | 20 |
| 6 | 24 |
| 7 | 29 |
| 8 | 35 |
| 9 | 42 |
| 10 | 50 |

Reputation changes gradually from customer-facing service performance and is bounded per night. Invisible kitchen mistakes only affect reputation when they reduce completed service.

Dining Level 1 admits up to 10 customers per night. Dining Expansion I costs $300 and permanently increases abstract capacity to 16. No tables, servers, or physical dining room are simulated yet.

Potential demand equals reputation baseline adjusted by the selected temporary advertising. Admitted demand is capped by dining capacity; excess customers are tracked as turned away. Players may knowingly advertise beyond comfortable kitchen or dining capacity.

| Advertising | Cost | Upcoming-night demand |
|---|---:|---:|
| None | $0 | +0% |
| Local Flyers | $20 | +25% |
| Local Campaign | $50 | +50% |

Advertising is charged once, expires after that service, and never directly grants permanent reputation.

## Kitchen and service rules

Players remain restricted to their own sides of the divider. Throwable ingredients may be caught automatically by a receiver with free hands. Missed ingredients become unusable purchased waste and leave a floor mess. Any usable item may use the shared counter.

Prep is untimed. Ingredients may be retrieved, moved, thrown, chopped, assembled, and staged. Ovens and fryers do not complete cooking before Service.

Service lasts two minutes for rapid prototype iteration. No more orders are generated than admitted demand. Up to three tickets remain visible and arrivals are paced. Correct plated dishes satisfy the oldest compatible ticket and earn revenue. Wrong dishes are refused. Expired tickets earn $0 and do not end the night.

The Night Summary reports operations, all categories of spending, revenue, waste, ending cash, remaining pantry quantities/value, and reputation movement. Next Day returns to Planning with persistent state.

## Future direction

Future AI staff may include servers, dishwashers, prep cooks, station cooks, and more capable sous-chefs. Staff would be paid per shift and operate physical systems. This is context only; Milestone 3 contains no AI staff.

Campaign Mode may later provide authored kitchens and three-star objectives, but it is not part of Endless Mode foundation work.

## Explicitly out of scope through Milestone 3

- Campaign and three-star levels
- AI staff, AI partner, staff wages, or staff pathfinding
- Physical dining rooms, customers, tables, or waiters
- Spoilage, freshness, stock decay, repairs, degradation, or breakdowns
- Full dishwashing loop or appliance upgrade tree
- Decorations, large content catalogues, procedural kitchens, or environmental hazards
- Online multiplayer, final art/music, or Steam integration

Do not begin AI staff or Campaign Mode without a new milestone instruction.
