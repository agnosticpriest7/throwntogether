# Thrown Together

A browser-based, fixed-screen restaurant cooking prototype for one player or optional two-player local co-op. It combines a persistent Endless restaurant, living dining room, deterministic staff, finite dishes, and a warm cartoon open-ring kitchen.

## Play online

The latest successful build from `main` is available at:

**[Play Thrown Together](https://agnosticpriest7.github.io/throwntogether/)**

## Requirements

- Node.js 20.19+ (Node.js 22.12+ also supported)
- npm
- A modern desktop browser
- One standard-mapped gamepad or keyboard for solo play; a second controller is optional for co-op

## Visual direction

The game uses a reusable Phaser vector art system: blue/coral tiled kitchens, warm wood dining, cream paper UI, chunky cocoa outlines, compact human-like chefs/customers/staff, illustrated appliances, and recipe-specific food states. Runtime art remains local and lightweight with no external asset requests. See [ART_DIRECTION.md](ART_DIRECTION.md) for palette, proportions, environment, UI, and technical asset rules.

## Run locally

```sh
npm install
npm run dev
```

Open the local URL printed by Vite. To create the production bundle:

```sh
npm run build
```

To preview that bundle:

```sh
npm run preview
```

## Deployment

GitHub Pages deployment is automatic. Every push to `main` runs the test suite and production build, uploads the generated `dist` directory, and deploys it only after those checks succeed. The workflow can also be started manually from **GitHub → Actions → Deploy game to GitHub Pages → Run workflow**.

Vite's production base path is `/throwntogether/`, matching the repository-scoped Pages URL. Deployment configuration lives in `.github/workflows/deploy-pages.yml`; generated `dist` files are not committed.

## Controls

| Player | Move | Interact / pick up / place / prep | Throw |
|---|---|---|---|
| Gamepad 1 / 2 | Left stick | South face button (Xbox A) | Right trigger |
| Player 1 keyboard | WASD | E | Q |
| Player 2 keyboard | Arrow keys | Shift | `/` |
| Touch Player 1 / 2 | Left/right on-screen D-pad | On-screen Use | On-screen Throw |

Choose **Single Player** or **Local Co-op** on the landing screen. Solo mode shows one chef and one touch panel; **Add P2** in the kitchen or the mode control in Planning activates the second chef and second touch panel. Moving P2's assigned controls during kitchen play also joins P2. Gamepads are assigned in browser gamepad-index order, while keyboard and touch remain active.

Planning uses native focusable controls for keyboard and touch. On a connected gamepad, use the left/right triggers to switch Planning Hub tabs, the stick/D-pad to move through the visible options, and the south face button to select. Moving down through a section reaches **BEGIN PREP** once two dishes are selected; **Start/Menu** also begins Prep directly when the menu is ready. During Prep, pressing **Start/Menu** again opens the restaurant. Press **R** or **Restart Night** above the game to restore the saved start-of-Prep planning state.

Kitchen appliances and counters gain a bright outline when a player is close enough to use them. Pressing Use away from a workstation keeps the carried item in hand; deliberate disposal requires the trash bin. Missed throws can still ruin food on the floor.

## Open-ring kitchen

The restaurant now uses one shared kitchen instead of two sealed halves. Pantry storage is clustered in the upper-left corner, installed appliances occupy authored positions around the outer edge, three staging counters form a central prep island, and the sink/pickup support the dining-facing service edge. Continuous space above and below the island gives a solo chef short circuits through every recipe while two chefs can work in parallel.

Throwing follows the chef's facing direction. A teammate can catch a toss, or an accurate throw can land on an empty island counter. Walking every item remains possible, so throwing is optimization rather than a recipe requirement.

Select **Recipes** in the kitchen HUD at any time during Prep or Service to see the selected dishes' required ingredient states, ordered station steps, and selling prices.

## Endless restaurant loop

1. Start a new restaurant or continue the versioned browser save. A new restaurant begins on Day 1 with $150, an empty pantry, Reputation Level 1, four installed core workstations, three two-seat tables, six plates, and no staff.
2. Move freely among Overview, Pantry, Supplier, Kitchen, Menu, Staff, and Marketing. Buy bulk ingredients, configure appliances, choose two dishes, schedule hired employees, and optionally advertise.
3. Select **Begin Prep** to pay that shift's scheduled payroll exactly once and lock management. Retrieve persistent stock, circulate around the central island, chop, stage, and optionally throw while closed.
4. Select **Open Restaurant** for a two-minute arrival window. Without a server, customers self-seat and create table-linked kitchen tickets. The temporary testing balance generates 50% more potential arrivals. At 0:00, **Last Call** stops new arrivals but keeps the restaurant open through unresolved service.
5. Plate the matching food, walk through the marked chef door, and use it on the correct table. Delivery credits revenue. If a server is later hired and scheduled, Service Pickup automates that trip.
6. After customers eat, collect each dirty table's plate, carry it back through the door, and return it to the sink. Either chef can then wash it, or a scheduled dishwasher washes one plate at a time. Clean plates return to circulation.
7. Once Last Call has no unresolved guests or tickets, review customer, dining, staff, financial, inventory, and reputation results. Select **Next Day** with the restaurant state preserved.

Supplier discounts are 5% at 5 units, 10% at 10 units, and 20% at 20 units. Ingredients never spoil or decay. Advertising is charged once and expires after service. Dining Level 1 provides three two-seat tables; the $300 Dining Expansion permanently adds two more tables. Physical turnover means a room can serve more customers than it seats simultaneously.

The open kitchen begins with four active authored appliance positions on its outer edge and can expand to six. Appliances can be purchased into storage and installed, stored, moved, or swapped during Planning only. A stored appliance does not unlock its recipes.

## Save management

Endless state is autosaved in versioned browser local storage after purchases, menu/marketing/staff decisions, kitchen configuration, expansions, and completed nights. Save v3 persists employee identity and scheduling, converts legacy onion stock to lettuce, and removes the former free starter server while preserving paid/progression state. The landing screen provides **New Restaurant**, **Continue Restaurant**, and **Reset Endless Save**. Destructive actions use an in-game confirmation panel navigable by controller, keyboard, or touch rather than a browser dialog. Controller focus begins visibly on Cancel; D-pad/stick or either trigger chooses an action, A confirms, and B cancels. Saves remain in the browser/profile where they were created.

## Staff and plates

| Employee role | Hire cost | Wage per scheduled shift | Priority |
|---|---:|---:|---|
| Server | $100 | $30 | Deliver food → seat parties → clear tables → idle |
| Dishwasher | $120 | $35 | Wash returned plates → idle |

Hires are permanent; scheduling is remembered until changed. Off employees cost nothing. If current cash cannot cover scheduled payroll, **Begin Prep** is blocked. Two servers reserve tasks independently so they cannot claim the same pickup dish, customer, or table.

The six-plate pool is conserved across clean, plated/in-use, hand-carried dirty, returned-dirty, and currently-washing states. Food abandoned after its customer leaves is orphaned and earns no revenue. Human chefs use the service door to work the dining room; staff are deterministic role AI and never cook recipes.

## Prototype recipes

| Dish | Ingredients | Kitchen path | Sale price |
|---|---|---|---:|
| Roast Potato | Potato | Chop → bake → plate | $8 |
| Garden Plate | Tomato + lettuce | Chop both → combine on plate | $10 |
| Cheese Bake | Potato + cheese | Chop potato → assemble → bake → plate | $15 |
| Fries | Potato | Chop → fry → plate | $12 |

Fries requires an installed Prep Station, Fryer, and Plating Station. Buying a fryer puts it in storage; it must be deliberately installed into an available kitchen slot.
