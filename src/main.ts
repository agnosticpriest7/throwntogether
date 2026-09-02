import Phaser from "phaser";
import "./style.css";
import { GAME_HEIGHT, GAME_WIDTH } from "./game/config";
import { TransferScene } from "./game/TransferScene";

new Phaser.Game({
  type: Phaser.AUTO,
  parent: "game",
  width: GAME_WIDTH,
  height: GAME_HEIGHT,
  backgroundColor: "#202733",
  scene: TransferScene,
  render: { antialias: true, pixelArt: false },
  scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH },
  input: { gamepad: true },
});

if (import.meta.env.DEV && new URLSearchParams(location.search).has("test")) {
  const harness = document.createElement("nav");
  harness.id = "test-harness";
  harness.setAttribute("aria-label", "Development scenario controls");
  const scenarios: Array<[string, () => void]> = [
    ["Reset", () => window.__THROWN_TOGETHER__?.reset()],
    ["P1 pickup", () => { const api = window.__THROWN_TOGETHER__; api?.setPlayer(0, 120, 230); api?.interact(0); }],
    ["Ready catch", () => window.__THROWN_TOGETHER__?.setPlayer(1, 690, 230)],
    ["P1 throw", () => window.__THROWN_TOGETHER__?.throw(0)],
    ["Land throw", () => window.__THROWN_TOGETHER__?.advanceFlight()],
    ["P2 prep", () => { const api = window.__THROWN_TOGETHER__; api?.setPlayer(1, 820, 185); api?.interact(1); }],
    ["P2 to counter", () => { const api = window.__THROWN_TOGETHER__; api?.setPlayer(1, 540, 305); api?.interact(1); }],
    ["P1 to counter", () => { const api = window.__THROWN_TOGETHER__; api?.setPlayer(0, 420, 305); api?.interact(0); }],
    ["P1 from counter", () => { const api = window.__THROWN_TOGETHER__; api?.setPlayer(0, 420, 305); api?.interact(0); }],
    ["P2 from counter", () => { const api = window.__THROWN_TOGETHER__; api?.setPlayer(1, 540, 305); api?.interact(1); }],
    ["P1 finish", () => { const api = window.__THROWN_TOGETHER__; api?.setPlayer(0, 130, 105); api?.interact(0); }],
    ["Miss", () => { const api = window.__THROWN_TOGETHER__; api?.reset(); api?.setPlayer(0, 270, 210, "raw"); api?.setPlayer(1, 900, 500); api?.throw(0); api?.advanceFlight(); }],
    ["Hands full miss", () => { const api = window.__THROWN_TOGETHER__; api?.reset(); api?.setPlayer(0, 270, 260, "raw"); api?.setPlayer(1, 690, 260, "raw"); api?.throw(0); api?.advanceFlight(); }],
    ["Reverse catch", () => { const api = window.__THROWN_TOGETHER__; api?.reset(); api?.setPlayer(0, 270, 320); api?.setPlayer(1, 690, 320, "raw"); api?.throw(1); api?.advanceFlight(); }],
    ["Simultaneous counter", () => { const api = window.__THROWN_TOGETHER__; api?.reset(); api?.setPlayer(0, 420, 305, "raw"); api?.setPlayer(1, 540, 305, "raw"); api?.interact(0); api?.interact(1); }],
  ];
  scenarios.forEach(([label, run]) => {
    const button = document.createElement("button");
    button.type = "button"; button.textContent = label; button.addEventListener("click", run); harness.append(button);
  });
  document.querySelector("main")?.append(harness);
}

declare global {
  interface Window {
    __THROWN_TOGETHER__?: {
      snapshot(): object;
      reset(): void;
      setPlayer(player: 0 | 1, x: number, y: number, held?: "raw" | "prepped" | "ruined" | null): void;
      interact(player: 0 | 1): void;
      throw(player: 0 | 1): void;
      advanceFlight(): void;
    };
  }
}
