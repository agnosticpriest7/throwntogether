# Thrown Together — Game Design Brief

Working title. This repository copy is the current design reference through Milestone 4.

## Project goal

Build a fixed-screen restaurant cooking game that is fully playable by one person, with a second local co-op chef as an optional efficiency upgrade. The central fantasy is choosing how ambitious the restaurant will be, investing real restaurant money, configuring a kitchen the players own, and living with those decisions during service.

The core design pillars are:

1. Players choose difficulty through their nightly menu and demand decisions.
2. Ingredients are purchased persistent inventory; waste is a real financial loss.
3. Throwing loose ingredients is an optional high-skill convenience while open routes and central staging counters are always safe.
4. The kitchen itself—owned equipment, installed capacity, and workspace—is progression.
5. Human chefs stay at the center while deterministic role-specific staff turn money into focused automation.
6. Chaos should come from player decisions, coordination, limited capacity, and service pressure rather than environmental gimmicks.

## Endless restaurant

Endless Mode is one persistent restaurant across an unlimited sequence of days:

1. Review cash, reputation, pantry, kitchen, physical dining room, and staff roster.
2. Buy bulk ingredients and capital equipment.
3. Store, install, move, or swap owned appliances in finite authored slots.
4. Choose two dishes from recipes enabled by installed equipment.
5. Hire employees, schedule a shift, and optionally advertise to increase this night's demand.
6. Pay scheduled payroll once, enter untimed Prep, and open for timed Service.
7. Cook table-linked tickets while servers seat, deliver, clear, and turn tables.
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
- Persistent employee IDs, names, roles, visual variation, and schedule choice
- In-progress nightly planning selections and accounting where needed to prevent double charging

Autosave meaningful planning changes and completed nights. Provide New Restaurant, Continue Restaurant, and a confirmed Reset Endless Save action.

Prototype starting restaurant:

- Day 1
- $150 cash
- Empty pantry
- Reputation Level 1
- Kitchen Level 1 with four core workstations installed
- Dining Level 1 with three two-seat tables
- One basic server, Ada, already hired and scheduled; her normal $30 wage applies

## Planning Hub

Planning is a controller-friendly hub rather than a rigid wizard. Players may revisit:

- Restaurant Overview
- Pantry
- Supplier
- Kitchen
- Menu
- Staff
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

Meals add their recipe sale price only when an AI server successfully delivers them to the associated customer/table. Ingredient waste is reported but receives no duplicate fine.

## Kitchen ownership and capacity

Owned and installed appliances are separate. Stored appliances do not enable recipes. Equipment may only be reconfigured during Planning and occupies fixed authored positions.

Kitchen Level 1 has four active appliance/workstation positions. Kitchen Expansion I costs $400 and permanently increases this to six visibly usable positions. The pantry, central island counters, sink, trash, and serving destination do not consume slots.

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

Reputation changes gradually from customer-visible outcomes: successful deliveries, table-wait departures, food-wait departures, and completion rate. Daily change remains bounded. Kitchen waste, payroll, and unused stock have no direct reputation effect.

Dining Level 1 has three authored two-seat tables. Dining Expansion I costs $300 and permanently adds two more two-seat tables, increasing simultaneous seating from 6 to 10. Seating is physical rather than a nightly admission cap: customers arrive across service, wait when every table is occupied, and may leave when table patience expires. Turnover allows more guests than the simultaneous seat count to be served.

Potential arrivals equal reputation baseline adjusted by temporary advertising. Players may knowingly advertise beyond comfortable kitchen, staff, or dining throughput.

| Advertising | Cost | Upcoming-night demand |
|---|---:|---:|
| None | $0 | +0% |
| Local Flyers | $20 | +25% |
| Local Campaign | $50 | +50% |

Advertising is charged once, expires after that service, and never directly grants permanent reputation.

## Kitchen and service rules

The kitchen is one open ring workspace. One chef can reach the pantry, every installed appliance, all three central staging counters, plating, service pickup, trash, and dish sink. The compact pantry occupies the upper-left corner; appliances line the outer working edge; the central island supports staging and co-op handoffs; pickup and dish support sit beside the dining-room service edge. Both chefs share the full kitchen when local co-op is active.

Single Player is the default session mode and never requires a second controller. Local Co-op adds a second independently controlled chef without changing the restaurant save. P2 may be selected before play or joined from the kitchen. Co-op enables parallel prep, transport, cooking, and organic specialization, but no recipe or route requires a handoff.

Throwable ingredients travel in the chef's facing direction. A teammate with free hands may catch them, or a well-aimed solo/co-op toss may land on an empty central island counter. A miss becomes unusable purchased waste and leaves a floor mess. Throwing is useful for speed, never mandatory for ordinary transport.

Prep is untimed. Ingredients may be retrieved, moved, thrown, chopped, assembled, and staged. Ovens and fryers do not complete cooking before Service.

Service lasts two minutes for rapid prototype iteration. Customers arrive in solo or two-person parties and follow readable arriving, table-wait, seating, food-wait, eating, leaving, and failed states. A seated party creates one table-linked prototype order. Up to three tickets remain visible. Correct plated food enters one of three pickup slots; pickup itself earns nothing. Wrong dishes are refused. An available server reserves and delivers the correct dish, and revenue is credited on delivery. Expired food waits earn $0 and do not end the night.

Tables progress through clean, reserved, waiting for food, eating, dirty, and reusable states. After eating, a server clears the dirty plate to the kitchen return. The restaurant owns six finite plates. Either chef may wash a returned plate for 2.5 seconds at the sink, or a scheduled dishwasher performs the same visible one-at-a-time job. A dirty or in-use plate cannot be used for plating.

## Staff and deterministic AI

Hires persist in the restaurant roster. Each employee has a stable ID, first name, role, and simple visual variation. Planning marks each hire Working or Off; only working employees appear and only they cost a wage. Payroll is checked and charged exactly once when Prep begins. Insufficient payroll blocks Prep with an explanation.

| Role | Hire | Wage per shift | Work |
|---|---:|---:|---|
| Server | $100 | $30 | Deliver ready food, seat waiting parties, clear dirty tables |
| Dishwasher | $120 | $35 | Wash returned plates one at a time at the sink |

Server tasks use an explicit deterministic priority order and reservation: ready-food delivery, seating, dirty-table clearing, then idle. Authored destinations keep staff and customers out of kitchen walls, counters, and tables; if a target disappears, the task safely returns to idle. Two servers can operate simultaneously without claiming the same dish, customer, or table. Staff are not LLM agents and never decide what chefs should cook.

The Night Summary reports operations, all categories of spending, revenue, waste, ending cash, remaining pantry quantities/value, and reputation movement. Next Day returns to Planning with persistent state.

## Future direction

Future role-specific staff may include prep cooks, station cooks, and a more capable sous-chef. They should operate physical systems with legible jobs rather than become a general AI that plays the kitchen. Milestone 4 implements only Server and Dishwasher.

Campaign Mode may later provide authored kitchens and three-star objectives, but it is not part of Endless Mode foundation work.

## Explicitly out of scope through Milestone 4

- Campaign and three-star levels
- General-purpose AI chefs, sous-chef, prep/station cooks, or AI partner
- Staff traits, levels, morale, complex scheduling, tips, hosts, bartenders, or reservations
- Groups larger than two, customer personalities, physical table editing, takeout, or delivery
- Spoilage, freshness, stock decay, repairs, degradation, or breakdowns
- Appliance upgrade tree
- Decorations, large content catalogues, procedural kitchens, or environmental hazards
- Online multiplayer, final art/music, or Steam integration

Do not begin general kitchen AI or Campaign Mode without a new milestone instruction.
