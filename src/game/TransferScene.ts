import Phaser from "phaser";
import { AudioCues } from "./AudioCues";
import { InputManager } from "./InputManager";
import {
  DESTINATION_POS, GAME_HEIGHT, GAME_WIDTH, INTERACT_DISTANCE, PLAYER_SPEED, PLAYER_STARTS,
  PREP_POS, SHARED_POS, SOURCE_POS, THROW_DURATION_MS, type PotatoState, type Side, type Vec2,
} from "./config";
import { canAutoCatch, clampToSide, distance, throwLanding } from "./rules";

interface Player {
  side: Side;
  position: Vec2;
  held: PotatoState | null;
  body: Phaser.GameObjects.Container;
  heldVisual: Phaser.GameObjects.Container;
  label: Phaser.GameObjects.Text;
  inputBadge: Phaser.GameObjects.Text;
  processingUntil: number;
}

interface LoosePotato { state: PotatoState; position: Vec2; visual: Phaser.GameObjects.Container }

export class TransferScene extends Phaser.Scene {
  private inputManager!: InputManager;
  private readonly audioCues = new AudioCues();
  private players!: [Player, Player];
  private loose: LoosePotato | null = null;
  private shared: PotatoState | null = null;
  private destination: PotatoState | null = null;
  private sharedVisual!: Phaser.GameObjects.Container;
  private destinationVisual!: Phaser.GameObjects.Container;
  private objectiveText!: Phaser.GameObjects.Text;
  private calloutText!: Phaser.GameObjects.Text;
  private calloutTimer?: Phaser.Time.TimerEvent;
  private flight: { state: PotatoState; from: Vec2; to: Vec2; elapsed: number; duration: number; visual: Phaser.GameObjects.Container; shadow: Phaser.GameObjects.Ellipse; indicator: Phaser.GameObjects.Arc; receiver: 0 | 1 } | null = null;
  private messes: Phaser.GameObjects.Container[] = [];
  private completed = false;

  constructor() { super("transfer"); }

  create(): void {
    this.inputManager = new InputManager(this);
    this.drawKitchen();
    this.sharedVisual = this.makePotato(SHARED_POS.x, SHARED_POS.y, "raw").setVisible(false);
    this.destinationVisual = this.makePotato(DESTINATION_POS.x, DESTINATION_POS.y, "raw").setVisible(false);
    this.players = [this.makePlayer(0, "left"), this.makePlayer(1, "right")];
    this.objectiveText = this.add.text(GAME_WIDTH / 2, 40, "GET POTATO  →  THROW  →  CATCH  →  PREP  →  SAFE RETURN", {
      fontFamily: "DM Mono, monospace", fontSize: "12px", color: "#dbe2ec", letterSpacing: 1,
    }).setOrigin(0.5);
    this.calloutText = this.add.text(GAME_WIDTH / 2, 72, "", {
      fontFamily: "Nunito, sans-serif", fontSize: "18px", fontStyle: "bold", color: "#ffffff",
      backgroundColor: "#18202dcc", padding: { x: 12, y: 6 },
    }).setOrigin(0.5).setDepth(40).setVisible(false);
    this.resetPrototype(false);
    document.getElementById("reset-button")?.addEventListener("click", () => this.resetPrototype(true));
    window.addEventListener("tt-reset", () => this.resetPrototype(true));
    Object.assign(window, {
      __THROWN_TOGETHER__: {
        snapshot: () => this.snapshot(),
        reset: () => this.resetPrototype(false),
        setPlayer: (player: 0 | 1, x: number, y: number, held?: PotatoState | null) => this.debugSetPlayer(player, x, y, held),
        interact: (player: 0 | 1) => this.interact(player),
        throw: (player: 0 | 1) => this.throwItem(player),
        advanceFlight: () => { if (this.flight) this.updateFlight(this.flight.duration); },
      },
    });
  }

  update(_time: number, delta: number): void {
    const inputs = [this.inputManager.read(0), this.inputManager.read(1)] as const;
    inputs.forEach((input, index) => {
      const player = this.players[index];
      player.inputBadge.setText(input.gamepadLabel);
      if (input.resetPressed) this.resetPrototype(true);
      if (this.time.now >= player.processingUntil) {
        const length = Math.hypot(input.x, input.y) || 1;
        const next = clampToSide({
          x: player.position.x + (input.x / Math.max(1, length)) * PLAYER_SPEED * delta / 1000,
          y: player.position.y + (input.y / Math.max(1, length)) * PLAYER_SPEED * delta / 1000,
        }, player.side);
        player.position = next;
        player.body.setPosition(next.x, next.y);
        if (input.interactPressed) this.interact(index as 0 | 1);
        if (input.throwPressed) this.throwItem(index as 0 | 1);
      }
    });
    if (this.flight) this.updateFlight(delta);
    this.updateHighlights();
    this.syncAccessibleStatus();
  }

  private resetPrototype(showMessage: boolean): void {
    this.flight?.visual.destroy(); this.flight?.shadow.destroy(); this.flight?.indicator.destroy(); this.flight = null;
    this.loose?.visual.destroy();
    this.messes.forEach((mess) => mess.destroy()); this.messes = [];
    this.shared = null; this.destination = null; this.completed = false;
    this.objectiveText?.setText("GET POTATO  →  THROW  →  CATCH  →  PREP  →  SAFE RETURN").setColor("#dbe2ec");
    this.sharedVisual.setVisible(false); this.destinationVisual.setVisible(false);
    this.players?.forEach((player) => { player.held = null; player.processingUntil = 0; this.updateHeld(player); });
    if (this.players) {
      this.players[0].position = { ...PLAYER_STARTS.left }; this.players[0].body.setPosition(PLAYER_STARTS.left.x, PLAYER_STARTS.left.y);
      this.players[1].position = { ...PLAYER_STARTS.right }; this.players[1].body.setPosition(PLAYER_STARTS.right.x, PLAYER_STARTS.right.y);
    }
    this.loose = { state: "raw", position: { ...SOURCE_POS }, visual: this.makePotato(SOURCE_POS.x, SOURCE_POS.y, "raw") };
    if (showMessage) this.callout("FRESH START", "#f5c85b");
  }

  private interact(index: 0 | 1): void {
    const player = this.players[index];
    if (this.flight) return;
    if (player.held) {
      if (distance(player.position, SHARED_POS) <= INTERACT_DISTANCE && !this.shared) {
        this.shared = player.held; player.held = null; this.updateHeld(player); this.updateCounterVisuals();
        this.audioCues.play("pickup"); this.callout("SAFE TRANSFER READY", "#7ed8ba"); return;
      }
      if (distance(player.position, SHARED_POS) <= INTERACT_DISTANCE && this.shared) {
        this.callout("COUNTER OCCUPIED", "#ffdc74"); return;
      }
      if (distance(player.position, PREP_POS) <= INTERACT_DISTANCE && player.held === "raw") {
        player.processingUntil = this.time.now + 760; this.audioCues.play("process"); this.callout("CHOPPING…", "#f5c85b");
        this.time.delayedCall(760, () => { if (player.held === "raw") { player.held = "prepped"; this.updateHeld(player); this.audioCues.play("complete"); this.callout("POTATO PREPPED!", "#7ed8ba"); } }); return;
      }
      if (distance(player.position, DESTINATION_POS) <= INTERACT_DISTANCE && !this.destination && player.held === "prepped") {
        this.destination = player.held; player.held = null; this.completed = true; this.updateHeld(player); this.updateCounterVisuals();
        this.audioCues.play("complete"); this.objectiveText.setText("TRANSFER LOOP COMPLETE · PRESS R TO RUN IT AGAIN").setColor("#7ed8ba"); this.callout("LOOP COMPLETE!", "#7ed8ba"); return;
      }
      this.dropItem(player); return;
    }
    if (distance(player.position, SHARED_POS) <= INTERACT_DISTANCE && this.shared) {
      player.held = this.shared; this.shared = null; this.updateHeld(player); this.updateCounterVisuals(); this.audioCues.play("pickup"); this.callout("SAFE PICKUP", "#7ed8ba"); return;
    }
    if (distance(player.position, DESTINATION_POS) <= INTERACT_DISTANCE && this.destination) {
      player.held = this.destination; this.destination = null; this.updateHeld(player); this.updateCounterVisuals(); this.audioCues.play("pickup"); return;
    }
    if (this.loose && this.loose.state !== "ruined" && distance(player.position, this.loose.position) <= INTERACT_DISTANCE) {
      player.held = this.loose.state; this.loose.visual.destroy(); this.loose = null; this.updateHeld(player); this.audioCues.play("pickup"); this.callout("POTATO PICKED UP", "#f5c85b");
    }
  }

  private throwItem(index: 0 | 1): void {
    const player = this.players[index];
    if (!player.held || this.flight) return;
    const state = player.held; player.held = null; this.updateHeld(player);
    const to = throwLanding(player.side, player.position.y);
    const receiver = (index === 0 ? 1 : 0) as 0 | 1;
    const indicator = this.add.circle(to.x, to.y, 48, 0xf5c85b, 0.16).setStrokeStyle(4, 0xffdc74, 0.9).setDepth(16);
    const shadow = this.add.ellipse(player.position.x, player.position.y + 8, 34, 13, 0x11151c, 0.35).setDepth(18);
    const visual = this.makePotato(player.position.x, player.position.y, state).setDepth(25);
    this.flight = { state, from: { ...player.position }, to, elapsed: 0, duration: THROW_DURATION_MS, visual, shadow, indicator, receiver };
    this.tweens.add({ targets: indicator, scale: 1.15, alpha: 0.45, duration: 260, yoyo: true, repeat: 1 });
    this.audioCues.play("throw"); this.time.delayedCall(80, () => this.audioCues.play("incoming")); this.callout("INCOMING!", "#ffdc74");
  }

  private updateFlight(delta: number): void {
    const flight = this.flight; if (!flight) return;
    flight.elapsed = Math.min(flight.duration, flight.elapsed + delta);
    const t = flight.elapsed / flight.duration;
    const x = Phaser.Math.Linear(flight.from.x, flight.to.x, t);
    const y = Phaser.Math.Linear(flight.from.y, flight.to.y, t);
    const arc = Math.sin(Math.PI * t);
    flight.visual.setPosition(x, y - arc * 58).setScale(1 + arc * 0.35).setAngle(t * 300);
    flight.shadow.setPosition(x, y + 8).setScale(1 - arc * 0.28).setAlpha(0.35 - arc * 0.15);
    if (t < 1) return;
    const receiver = this.players[flight.receiver];
    const caught = canAutoCatch(receiver.position, flight.to, receiver.held === null);
    flight.visual.destroy(); flight.shadow.destroy(); flight.indicator.destroy(); this.flight = null;
    if (caught) {
      receiver.held = flight.state; this.updateHeld(receiver); this.audioCues.play("catch"); this.callout(`P${flight.receiver + 1} CAUGHT IT!`, "#7ed8ba");
    } else {
      this.createMess(flight.to); this.loose = { state: "ruined", position: flight.to, visual: this.makePotato(flight.to.x, flight.to.y, "ruined") };
      this.audioCues.play("miss"); this.callout(receiver.held ? "HANDS FULL — POTATO WASTED" : "MISSED — POTATO WASTED", "#ff7e70");
    }
  }

  private dropItem(player: Player): void {
    const state = player.held!; player.held = null; this.updateHeld(player);
    this.loose?.visual.destroy();
    this.loose = { state, position: { ...player.position }, visual: this.makePotato(player.position.x, player.position.y + 25, state) };
    this.audioCues.play("pickup"); this.callout("PLACED ON FLOOR", "#c1c8d2");
  }

  private drawKitchen(): void {
    const g = this.add.graphics();
    g.fillStyle(0x2d3745).fillRoundedRect(24, 22, GAME_WIDTH - 48, GAME_HEIGHT - 44, 16);
    g.fillStyle(0x343f4e).fillRect(36, 66, 408, 470); g.fillStyle(0x303a48).fillRect(516, 66, 408, 470);
    g.lineStyle(1, 0x455264, 0.45);
    for (let x = 36; x <= 924; x += 32) g.lineBetween(x, 66, x, 536);
    for (let y = 66; y <= 536; y += 32) g.lineBetween(36, y, 924, y);
    g.fillStyle(0x171c24).fillRoundedRect(444, 66, 72, 470, 6);
    g.fillStyle(0x687488).fillRoundedRect(438, 84, 84, 166, 8).fillRoundedRect(438, 360, 84, 158, 8);
    g.fillStyle(0x8b97a8).fillRoundedRect(421, 260, 118, 90, 10);
    g.fillStyle(0x252c36).fillRoundedRect(429, 270, 102, 70, 8);
    this.add.text(480, 354, "SHARED COUNTER", { fontFamily: "DM Mono, monospace", fontSize: "10px", color: "#aeb8c7" }).setOrigin(0.5);
    this.drawStation(SOURCE_POS, "POTATO", "SUPPLY", 0xd39b52);
    this.drawStation(PREP_POS, "KNIFE", "PREP", 0x66b9a8);
    this.drawStation(DESTINATION_POS, "✓", "FINISH TRAY", 0x7988d9);
    this.add.text(64, 92, "PLAYER 1 · LEFT KITCHEN", { fontFamily: "DM Mono, monospace", fontSize: "11px", color: "#f6b75e" });
    this.add.text(896, 92, "PLAYER 2 · RIGHT KITCHEN", { fontFamily: "DM Mono, monospace", fontSize: "11px", color: "#76c8df" }).setOrigin(1, 0);
  }

  private drawStation(pos: Vec2, icon: string, label: string, color: number): void {
    this.add.rectangle(pos.x, pos.y, 112, 82, 0x222a35).setStrokeStyle(3, color, 0.8);
    this.add.text(pos.x, pos.y - 8, icon, { fontFamily: "Nunito, sans-serif", fontSize: "22px", fontStyle: "bold", color: `#${color.toString(16).padStart(6, "0")}` }).setOrigin(0.5);
    this.add.text(pos.x, pos.y + 24, label, { fontFamily: "DM Mono, monospace", fontSize: "9px", color: "#c3ccd9" }).setOrigin(0.5);
  }

  private makePlayer(index: 0 | 1, side: Side): Player {
    const color = index === 0 ? 0xf2a94f : 0x62bed7;
    const heldVisual = this.add.container(0, -31).setVisible(false);
    const shadow = this.add.ellipse(0, 14, 54, 22, 0x10141a, 0.35);
    const body = this.add.container(PLAYER_STARTS[side].x, PLAYER_STARTS[side].y, [shadow]);
    const ring = this.add.circle(0, 0, 25, 0x202733).setStrokeStyle(5, color);
    const face = this.add.circle(0, -3, 14, color); const apron = this.add.rectangle(0, 12, 25, 14, 0xf2eee4).setOrigin(0.5);
    body.add([ring, face, apron, heldVisual]); body.setDepth(20);
    const label = this.add.text(0, 38, `P${index + 1}`, { fontFamily: "Nunito, sans-serif", fontSize: "12px", fontStyle: "bold", color: "#ffffff" }).setOrigin(0.5);
    const inputBadge = this.add.text(0, 54, "KEYBOARD", { fontFamily: "DM Mono, monospace", fontSize: "8px", color: index === 0 ? "#f6b75e" : "#76c8df", backgroundColor: "#18202dcc", padding: { x: 4, y: 2 } }).setOrigin(0.5);
    body.add([label, inputBadge]);
    return { side, position: { ...PLAYER_STARTS[side] }, held: null, body, heldVisual, label, inputBadge, processingUntil: 0 };
  }

  private makePotato(x: number, y: number, state: PotatoState): Phaser.GameObjects.Container {
    const container = this.add.container(x, y);
    this.populatePotato(container, state);
    return container.setDepth(14);
  }

  private populatePotato(container: Phaser.GameObjects.Container, state: PotatoState): void {
    const color = state === "ruined" ? 0x6b6258 : state === "prepped" ? 0xffd77c : 0xc8904f;
    const potato = this.add.ellipse(0, 0, 28, 22, color).setStrokeStyle(2, state === "ruined" ? 0x302d2b : 0x805a30);
    container.add(potato);
    if (state === "prepped") {
      container.add(this.add.line(0, 0, -8, -6, 8, 6, 0xffffff, 0.7).setLineWidth(2));
      container.add(this.add.line(0, 0, -8, 6, 8, -6, 0xffffff, 0.7).setLineWidth(2));
    }
    if (state === "ruined") container.add(this.add.text(0, 0, "×", { fontSize: "18px", color: "#ff8b7e", fontStyle: "bold" }).setOrigin(0.5));
  }

  private createMess(pos: Vec2): void {
    const mess = this.add.container(pos.x, pos.y);
    mess.add(this.add.ellipse(0, 9, 72, 35, 0x5d4938, 0.75));
    [[-22, 1], [15, -3], [28, 9], [-8, 12]].forEach(([x, y]) => mess.add(this.add.circle(x, y, 6, 0x3e332c, 0.7)));
    mess.add(this.add.text(0, 36, "WASTED", { fontFamily: "DM Mono, monospace", fontSize: "9px", color: "#ff8b7e", backgroundColor: "#18202dcc", padding: { x: 4, y: 2 } }).setOrigin(0.5));
    mess.setDepth(9); this.messes.push(mess);
  }

  private updateHeld(player: Player): void {
    player.heldVisual.removeAll(true);
    if (!player.held) { player.heldVisual.setVisible(false); return; }
    this.populatePotato(player.heldVisual, player.held);
    player.heldVisual.setVisible(true).setDepth(30);
  }

  private updateCounterVisuals(): void {
    this.replacePotatoVisual(this.sharedVisual, this.shared, SHARED_POS);
    this.replacePotatoVisual(this.destinationVisual, this.destination, DESTINATION_POS);
  }

  private replacePotatoVisual(oldVisual: Phaser.GameObjects.Container, state: PotatoState | null, pos: Vec2): void {
    oldVisual.removeAll(true);
    if (!state) { oldVisual.setVisible(false); return; }
    this.populatePotato(oldVisual, state);
    oldVisual.setPosition(pos.x, pos.y).setVisible(true).setDepth(15);
  }

  private updateHighlights(): void {
    const stations = [SOURCE_POS, PREP_POS, SHARED_POS, DESTINATION_POS];
    stations.forEach((pos, index) => {
      const close = this.players.some((player) => distance(player.position, pos) <= INTERACT_DISTANCE);
      // Existing station art remains static; a compact pulse marker avoids noisy outlines.
      if (close && index === 2 && !this.flight) this.sharedVisual.setScale(1.06);
      else if (index === 2) this.sharedVisual.setScale(1);
    });
  }

  private callout(message: string, color: string): void {
    this.calloutTimer?.remove(false);
    this.calloutText.setText(message).setColor(color).setVisible(true).setAlpha(1).setScale(0.92);
    this.tweens.add({ targets: this.calloutText, scale: 1, duration: 100, ease: "Back.Out" });
    this.calloutTimer = this.time.delayedCall(1300, () => this.tweens.add({ targets: this.calloutText, alpha: 0, duration: 220, onComplete: () => this.calloutText.setVisible(false) }));
  }

  private debugSetPlayer(index: 0 | 1, x: number, y: number, held?: PotatoState | null): void {
    const player = this.players[index]; player.position = clampToSide({ x, y }, player.side); player.body.setPosition(player.position.x, player.position.y);
    if (held !== undefined) { player.held = held; this.updateHeld(player); }
  }

  private snapshot(): object {
    return {
      players: this.players.map((player) => ({ side: player.side, x: Math.round(player.position.x), y: Math.round(player.position.y), held: player.held })),
      loose: this.loose ? { state: this.loose.state, ...this.loose.position } : null,
      shared: this.shared, destination: this.destination, inFlight: this.flight?.state ?? null,
      messCount: this.messes.length, completed: this.completed,
    };
  }

  private syncAccessibleStatus(): void {
    const output = document.getElementById("game-status");
    if (output) output.textContent = JSON.stringify(this.snapshot());
  }
}
