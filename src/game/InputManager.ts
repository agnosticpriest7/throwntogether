import Phaser from "phaser";
import { TouchControls } from "./TouchControls";

export interface PlayerInput {
  x: number;
  y: number;
  interactPressed: boolean;
  throwPressed: boolean;
  startPressed: boolean;
  resetPressed: boolean;
  gamepadLabel: string;
}

interface KeySet {
  up: Phaser.Input.Keyboard.Key;
  down: Phaser.Input.Keyboard.Key;
  left: Phaser.Input.Keyboard.Key;
  right: Phaser.Input.Keyboard.Key;
  interact: Phaser.Input.Keyboard.Key;
  throwItem: Phaser.Input.Keyboard.Key;
}

export class InputManager {
  private readonly keys: [KeySet, KeySet];
  private readonly resetKey: Phaser.Input.Keyboard.Key;
  private readonly touchControls = new TouchControls();
  private assigned: [number | null, number | null] = [null, null];
  private buttonWasDown = [false, false, false, false, false, false];
  private pendingInteract: [boolean, boolean] = [false, false];
  private pendingThrow: [boolean, boolean] = [false, false];
  private pendingReset = false;

  constructor(scene: Phaser.Scene) {
    const keyboard = scene.input.keyboard!;
    this.keys = [
      {
        up: keyboard.addKey("W"), down: keyboard.addKey("S"),
        left: keyboard.addKey("A"), right: keyboard.addKey("D"),
        interact: keyboard.addKey("E"), throwItem: keyboard.addKey("Q"),
      },
      {
        up: keyboard.addKey(Phaser.Input.Keyboard.KeyCodes.UP), down: keyboard.addKey(Phaser.Input.Keyboard.KeyCodes.DOWN),
        left: keyboard.addKey(Phaser.Input.Keyboard.KeyCodes.LEFT), right: keyboard.addKey(Phaser.Input.Keyboard.KeyCodes.RIGHT),
        interact: keyboard.addKey(Phaser.Input.Keyboard.KeyCodes.SHIFT), throwItem: keyboard.addKey(Phaser.Input.Keyboard.KeyCodes.FORWARD_SLASH),
      },
    ];
    this.resetKey = keyboard.addKey("R");
    this.keys.forEach((keys, player) => {
      keys.interact.on("down", () => { this.pendingInteract[player] = true; });
      keys.throwItem.on("down", () => { this.pendingThrow[player] = true; });
    });
    this.resetKey.on("down", () => { this.pendingReset = true; });
    // Native keydown latching also catches ultra-short taps that begin and end
    // between two Phaser updates (common with accessibility and test tools).
    window.addEventListener("keydown", (event) => {
      if (event.code === "KeyE") this.pendingInteract[0] = true;
      if (event.code === "ShiftLeft" || event.code === "ShiftRight") this.pendingInteract[1] = true;
      if (event.code === "KeyQ") this.pendingThrow[0] = true;
      if (event.code === "Slash") this.pendingThrow[1] = true;
      if (event.code === "KeyR") this.pendingReset = true;
    });
  }

  updateAssignments(): void {
    const pads = [...(navigator.getGamepads?.() ?? [])].filter((pad): pad is Gamepad => pad !== null);
    for (let player = 0; player < 2; player += 1) {
      const current = this.assigned[player];
      if (current !== null && pads.some((pad) => pad.index === current)) continue;
      const used = new Set(this.assigned.filter((value): value is number => value !== null));
      this.assigned[player] = pads.find((pad) => !used.has(pad.index))?.index ?? null;
    }
  }

  read(player: 0 | 1): PlayerInput {
    this.updateAssignments();
    const keys = this.keys[player];
    const keyboardX = Number(keys.right.isDown) - Number(keys.left.isDown);
    const keyboardY = Number(keys.down.isDown) - Number(keys.up.isDown);
    const touch = this.touchControls.movement(player);
    const padIndex = this.assigned[player];
    const pad = padIndex === null ? null : navigator.getGamepads?.()[padIndex] ?? null;
    const padX = Math.abs(pad?.axes[0] ?? 0) > 0.18 ? pad!.axes[0] : 0;
    const padY = Math.abs(pad?.axes[1] ?? 0) > 0.18 ? pad!.axes[1] : 0;
    const base = player * 3;
    const interactDown = (pad?.buttons[0]?.pressed ?? false);
    const throwDown = (pad?.buttons[7]?.pressed ?? false) || (pad?.buttons[7]?.value ?? 0) > 0.55;
    const startDown = pad?.buttons[9]?.pressed ?? false;
    const keyboardInteract = this.pendingInteract[player];
    const keyboardThrow = this.pendingThrow[player];
    const keyboardReset = this.pendingReset;
    const touchInteract = this.touchControls.consumeInteract(player);
    const touchThrow = this.touchControls.consumeThrow(player);
    this.pendingInteract[player] = false;
    this.pendingThrow[player] = false;
    if (player === 0) this.pendingReset = false;
    const result: PlayerInput = {
      x: touch.x || keyboardX || padX,
      y: touch.y || keyboardY || padY,
      interactPressed: touchInteract || keyboardInteract || (interactDown && !this.buttonWasDown[base]),
      throwPressed: touchThrow || keyboardThrow || (throwDown && !this.buttonWasDown[base + 1]),
      startPressed: startDown && !this.buttonWasDown[base + 2],
      resetPressed: keyboardReset,
      gamepadLabel: pad ? `PAD ${pad.index + 1} · ${InputManager.shortPadName(pad.id)}` : "TOUCH / KEYS",
    };
    this.buttonWasDown[base] = interactDown;
    this.buttonWasDown[base + 1] = throwDown;
    this.buttonWasDown[base + 2] = startDown;
    return result;
  }

  private static shortPadName(id: string): string {
    const cleaned = id.replace(/\([^)]*\)/g, "").replace(/standard gamepad/gi, "").trim();
    return (cleaned || "GAMEPAD").slice(0, 20).toUpperCase();
  }
}
