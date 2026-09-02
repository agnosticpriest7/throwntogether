export type TouchControl = "up" | "down" | "left" | "right" | "interact" | "throw";
type Direction = Exclude<TouchControl, "interact" | "throw">;

interface DirectionState {
  active: boolean;
  minimumHoldUntil: number;
}

const DIRECTIONS: Direction[] = ["up", "down", "left", "right"];

export class TouchControls {
  private readonly directions: [Record<Direction, DirectionState>, Record<Direction, DirectionState>] = [
    TouchControls.makeDirections(), TouchControls.makeDirections(),
  ];
  private pendingInteract: [boolean, boolean] = [false, false];
  private pendingThrow: [boolean, boolean] = [false, false];

  constructor(root: ParentNode = document) {
    root.querySelectorAll<HTMLButtonElement>("[data-touch-player][data-touch-control]").forEach((button) => {
      const player = Number(button.dataset.touchPlayer) as 0 | 1;
      const control = button.dataset.touchControl as TouchControl;
      const release = (event: PointerEvent): void => {
        event.preventDefault();
        button.setAttribute("aria-pressed", "false");
        if (DIRECTIONS.includes(control as Direction)) this.directions[player][control as Direction].active = false;
      };

      button.setAttribute("aria-pressed", "false");
      button.addEventListener("pointerdown", (event) => {
        event.preventDefault();
        button.setPointerCapture(event.pointerId);
        button.setAttribute("aria-pressed", "true");
        if (control === "interact") this.pendingInteract[player] = true;
        else if (control === "throw") this.pendingThrow[player] = true;
        else {
          const state = this.directions[player][control];
          state.active = true;
          state.minimumHoldUntil = performance.now() + 80;
        }
      });
      button.addEventListener("pointerup", release);
      button.addEventListener("pointercancel", release);
      button.addEventListener("lostpointercapture", release);
      button.addEventListener("contextmenu", (event) => event.preventDefault());
    });
  }

  movement(player: 0 | 1): { x: number; y: number } {
    const now = performance.now();
    const pressed = (direction: Direction): boolean => {
      const state = this.directions[player][direction];
      return state.active || state.minimumHoldUntil > now;
    };
    return {
      x: Number(pressed("right")) - Number(pressed("left")),
      y: Number(pressed("down")) - Number(pressed("up")),
    };
  }

  consumeInteract(player: 0 | 1): boolean {
    const pending = this.pendingInteract[player];
    this.pendingInteract[player] = false;
    return pending;
  }

  consumeThrow(player: 0 | 1): boolean {
    const pending = this.pendingThrow[player];
    this.pendingThrow[player] = false;
    return pending;
  }

  private static makeDirections(): Record<Direction, DirectionState> {
    return {
      up: { active: false, minimumHoldUntil: 0 }, down: { active: false, minimumHoldUntil: 0 },
      left: { active: false, minimumHoldUntil: 0 }, right: { active: false, minimumHoldUntil: 0 },
    };
  }
}
