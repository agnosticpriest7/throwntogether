# Thrown Together

A browser-based, fixed-screen local co-op cooking prototype. Milestone 4 adds a living dining room, table-linked orders, deterministic AI servers, persistent staff and shift wages, finite dirty dishes, human washing, and an optional AI dishwasher to the persistent Endless restaurant.

## Play online

The latest successful build from `main` is available at:

**[Play Thrown Together](https://agnosticpriest7.github.io/throwntogether/)**

## Requirements

- Node.js 20.19+ (Node.js 22.12+ also supported)
- npm
- A modern desktop browser
- Two standard-mapped gamepads recommended for co-op playtesting

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

Touch controls are split into independent Player 1 (left) and Player 2 (right) panels and support simultaneous multi-touch input. Gamepads are assigned in browser gamepad-index order and each player's current assignment is shown under their character. Keyboard and touch controls remain active when pads are connected.

Planning uses native focusable controls for keyboard and touch. On a connected gamepad, use the left/right triggers to switch Planning Hub tabs, the stick/D-pad to move through the visible options, and the south face button to select. Moving down through a section reaches **BEGIN PREP** once two dishes are selected; **Start/Menu** also begins Prep directly when the menu is ready. During Prep, pressing **Start/Menu** again opens the restaurant. Press **R** or **Restart Night** above the game to restore the saved start-of-Prep planning state.

Kitchen appliances and counters gain a bright outline when a player is close enough to use them. Pressing Use away from a workstation keeps the carried item in hand; deliberate disposal requires using the shared trash chute in the center divider. Missed throws can still ruin food on the floor.

Select **Recipes** in the kitchen HUD at any time during Prep or Service to see the selected dishes' required ingredient states, ordered station steps, and selling prices.

## Endless restaurant loop

1. Start a new restaurant or continue the versioned browser save. A new restaurant begins on Day 1 with $150, an empty pantry, Reputation Level 1, four installed core workstations, three two-seat tables, six plates, and server Ada already hired.
2. Move freely among Overview, Pantry, Supplier, Kitchen, Menu, Staff, and Marketing. Buy bulk ingredients, configure appliances, choose two dishes, schedule hired employees, and optionally advertise.
3. Select **Begin Prep** to pay that shift's scheduled payroll exactly once and lock management. Retrieve persistent stock, chop, throw/catch, use the shared counter, and stage equipment while closed.
4. Select **Open Restaurant** for a two-minute service. Customers arrive over time; servers seat parties, which creates table-linked kitchen tickets.
5. Plate the matching food and put it in **Service Pickup**. An available server reserves it, delivers it to the correct table, and only then credits revenue.
6. After customers eat, servers clear plates to **Dirty Return**. Either chef can wash at the sink, or a scheduled dishwasher washes one plate at a time. Clean plates return to circulation.
7. Review customer, dining, staff, financial, inventory, and reputation results. Select **Next Day** with the restaurant state preserved.

Supplier discounts are 5% at 5 units, 10% at 10 units, and 20% at 20 units. Ingredients never spoil or decay. Advertising is charged once and expires after service. Dining Level 1 provides three two-seat tables; the $300 Dining Expansion permanently adds two more tables. Physical turnover means a room can serve more customers than it seats simultaneously.

The kitchen begins with four active authored appliance positions and can expand to six. Appliances can be purchased into storage and installed, stored, moved, or swapped during Planning only. A stored appliance does not unlock its recipes.

## Save management

Endless state is autosaved in versioned browser local storage after purchases, menu/marketing/staff decisions, kitchen configuration, expansions, and completed nights. Save v2 persists employee identity and scheduling and migrates Milestone 3 v1 saves by adding the starting server without discarding restaurant progress. The landing screen provides **New Restaurant**, **Continue Restaurant**, and a confirmed **Reset Endless Save** action. Saves remain in the browser/profile where they were created.

## Staff and plates

| Employee role | Hire cost | Wage per scheduled shift | Priority |
|---|---:|---:|---|
| Server | $100 | $30 | Deliver food → seat parties → clear tables → idle |
| Dishwasher | $120 | $35 | Wash returned plates → idle |

Hires are permanent; scheduling is remembered until changed. Off employees cost nothing. If current cash cannot cover scheduled payroll, **Begin Prep** is blocked. Two servers reserve tasks independently so they cannot claim the same pickup dish, customer, or table.

The six-plate pool is conserved across clean, plated/in-use, returned-dirty, and currently-washing states. Food abandoned after its customer leaves is orphaned and earns no revenue. Human chefs remain kitchen-bound; staff are deterministic role AI and never cook recipes.

## Prototype recipes

| Dish | Ingredients | Kitchen path | Sale price |
|---|---|---|---:|
| Roast Potato | Potato | Chop → bake → plate | $8 |
| Garden Plate | Tomato + onion | Chop both → combine on plate | $10 |
| Cheese Bake | Potato + cheese | Chop potato → assemble → bake → plate | $15 |
| Fries | Potato | Chop → fry → plate | $12 |

Fries requires an installed Prep Station, Fryer, and Plating Station. Buying a fryer puts it in storage; it must be deliberately installed into an available kitchen slot.
