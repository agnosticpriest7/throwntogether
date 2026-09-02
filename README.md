# Thrown Together

A browser-based, fixed-screen local co-op cooking prototype. Milestone 2 is a complete two-player test night: choose two dishes, buy stock with limited cash, prep the kitchen, serve timed orders, and review the night's finances.

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

Menu selection and purchasing use ordinary buttons, so they work with mouse or touch. During kitchen play, press **R**, either gamepad's **Start/Menu** button, or **Restart Night** above the game to return to menu selection.

## Single-night loop

1. Choose exactly two dishes from Roast Potato, Garden Plate, and Cheese Bake.
2. Spend the restaurant's $40 starting cash on ingredient quantities of your choice.
3. Use the untimed **CLOSED / PREP** phase to retrieve stock, chop ingredients, throw or use the shared counter, and stage the kitchen. Ovens remain paused while closed.
4. Select **Open Restaurant** to begin a two-minute service. Cook, assemble, plate, and serve the oldest compatible ticket before its patience expires.
5. Review spending, revenue, waste, completed and missed orders, and final cash, then select **Play Another Night**.

Four clean plates are available each night. Missed throws permanently ruin purchased ingredients and their value appears as waste in the summary. Wrong plated dishes are refused at the serving window and remain in the player's hands.

## Prototype recipes

| Dish | Ingredients | Kitchen path | Sale price |
|---|---|---|---:|
| Roast Potato | Potato | Chop → bake → plate | $8 |
| Garden Plate | Tomato + onion | Chop both → combine on plate | $10 |
| Cheese Bake | Potato + cheese | Chop potato → assemble → bake → plate | $15 |
