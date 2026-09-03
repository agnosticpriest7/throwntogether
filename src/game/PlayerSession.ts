export type PlayerMode = "solo" | "coop";

export class PlayerSession {
  private currentMode: PlayerMode = "solo";
  private readonly listeners = new Set<(mode: PlayerMode) => void>();

  get mode(): PlayerMode { return this.currentMode; }
  get activePlayerCount(): 1 | 2 { return this.currentMode === "coop" ? 2 : 1; }

  isActive(player: 0 | 1): boolean { return player === 0 || this.currentMode === "coop"; }

  setMode(mode: PlayerMode): void {
    if (mode === this.currentMode) return;
    this.currentMode = mode;
    this.listeners.forEach((listener) => listener(mode));
  }

  onChange(listener: (mode: PlayerMode) => void): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }
}
