# Thrown Together

A browser-based, fixed-screen local co-op cooking prototype. Milestone 1 tests whether two players on opposite sides of a kitchen can quickly throw, catch, process, and safely return one potato.

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

Gamepads are assigned in browser gamepad-index order and each player's current assignment is shown under their character. Keyboard controls remain active as a debug fallback even when pads are connected.

Press **R**, either gamepad's **Start/Menu** button, or the **Restart** button above the game for an immediate clean reset.

## Prototype loop

Player 1 picks up the potato from the supply, throws it across the divider, and Player 2 automatically catches it when standing in the landing circle with free hands. Player 2 preps it at the knife station, places it on the shared counter, and Player 1 retrieves it and places it on the finish tray.

The shared counter also accepts the raw potato in either direction, providing the slower, safe alternative to throwing. A missed catch ruins the potato and leaves a visible floor mess; reset to try again.
