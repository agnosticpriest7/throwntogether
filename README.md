# Thrown Together

A browser-based, fixed-screen local co-op cooking prototype. Milestone 3 adds the foundation of Endless Mode: one persistent restaurant, bulk purchasing, finite appliance slots, equipment-gated menus, demand management, expansion, and a repeatable multi-night service loop.

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

Planning uses native focusable controls for keyboard and touch. On a connected gamepad, use either stick/D-pad to move focus and the south face button to select. During kitchen play, press **R**, either gamepad's **Start/Menu** button, or **Restart Night** above the game to restore the saved start-of-Prep planning state.

Select **Recipes** in the kitchen HUD at any time during Prep or Service to see the selected dishes' required ingredient states, ordered station steps, and selling prices.

## Endless restaurant loop

1. Start a new restaurant or continue the versioned browser save. A new restaurant begins on Day 1 with $150, an empty pantry, Reputation Level 1, four installed core workstations, Kitchen Level 1, and Dining Level 1.
2. Move freely among Overview, Pantry, Supplier, Kitchen, Menu, and Marketing. Buy bulk ingredients, store/install owned appliances, choose two available dishes, and optionally advertise.
3. Select **Begin Prep** to lock management for the night. Retrieve persistent stock, chop, throw/catch, use the shared counter, and stage installed cooking equipment while closed.
4. Select **Open Restaurant** for a two-minute service. Tickets are paced up to admitted demand, which is reputation demand plus advertising capped by dining capacity.
5. Review operations, finances, remaining inventory, and reputation. Select **Next Day** to return to Planning with cash, leftovers, appliances, expansions, and reputation intact.

Supplier discounts are 5% at 5 units, 10% at 10 units, and 20% at 20 units. Ingredients never spoil or decay. Advertising is charged once and expires after service. Four clean plates reset each night; dishwashing remains outside this milestone.

The kitchen begins with four active authored appliance positions and can expand to six. Appliances can be purchased into storage and installed, stored, moved, or swapped during Planning only. A stored appliance does not unlock its recipes.

## Save management

Endless state is autosaved in versioned browser local storage after purchases, menu/marketing decisions, kitchen configuration, expansions, and completed nights. The landing screen provides **New Restaurant**, **Continue Restaurant**, and a confirmed **Reset Endless Save** action. Saves currently remain in the browser/profile where they were created.

## Prototype recipes

| Dish | Ingredients | Kitchen path | Sale price |
|---|---|---|---:|
| Roast Potato | Potato | Chop → bake → plate | $8 |
| Garden Plate | Tomato + onion | Chop both → combine on plate | $10 |
| Cheese Bake | Potato + cheese | Chop potato → assemble → bake → plate | $15 |
| Fries | Potato | Chop → fry → plate | $12 |

Fries requires an installed Prep Station, Fryer, and Plating Station. Buying a fryer puts it in storage; it must be deliberately installed into an available kitchen slot.
