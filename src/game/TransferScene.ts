import Phaser from "phaser";
import { AudioCues } from "./AudioCues";
import { InputManager } from "./InputManager";
import { RestaurantModel, type ServiceEvent } from "./RestaurantModel";
import type { RestaurantUI } from "./RestaurantUI";
import {
  APPLIANCES, CHOP_TIME_MS, DISHWASH_DURATION_MS, INGREDIENTS, KITCHEN_SLOTS, RECIPES, formatMoney, ingredientItem,
  type IngredientId, type KitchenItem, type RecipeId,
} from "./data";
import {
  GAME_HEIGHT, GAME_WIDTH, INTERACT_DISTANCE, PLAYER_SPEED, PLAYER_STARTS,
  SHARED_POS, THROW_DURATION_MS, type Side, type Vec2,
} from "./config";
import { canAutoCatch, clampToSide, distance, throwLanding } from "./rules";

type StationType = "counter" | "chop" | "assembly" | "oven" | "fryer" | "plate" | "trash" | "pickup" | "sink";

interface Player {
  side: Side;
  position: Vec2;
  held: KitchenItem | null;
  body: Phaser.GameObjects.Container;
  heldVisual: Phaser.GameObjects.Container;
  inputBadge: Phaser.GameObjects.Text;
}

interface Station {
  id: string;
  type: StationType;
  position: Vec2;
  item: KitchenItem | null;
  itemVisual: Phaser.GameObjects.Container;
  statusText: Phaser.GameObjects.Text;
  progressBg: Phaser.GameObjects.Rectangle;
  progressFill: Phaser.GameObjects.Rectangle;
  processStartedAt: number;
  processDuration: number;
  background: Phaser.GameObjects.Rectangle;
  baseColor: number;
  highlighted: boolean;
  visuals: Phaser.GameObjects.GameObject[];
}

interface Source { id: IngredientId; position: Vec2; countText: Phaser.GameObjects.Text }
interface RuinedItem { position: Vec2; visual: Phaser.GameObjects.Container }
interface Flight {
  item: KitchenItem; from: Vec2; to: Vec2; elapsed: number;
  visual: Phaser.GameObjects.Container; shadow: Phaser.GameObjects.Ellipse;
  indicator: Phaser.GameObjects.Arc; receiver: 0 | 1;
}

const SOURCE_LAYOUT: Array<{ id: IngredientId; position: Vec2 }> = [
  { id: "potato", position: { x: 95, y: 305 } }, { id: "tomato", position: { x: 215, y: 305 } },
  { id: "onion", position: { x: 95, y: 440 } }, { id: "cheese", position: { x: 215, y: 440 } },
];
export class TransferScene extends Phaser.Scene {
  private inputManager!: InputManager;
  private readonly audioCues = new AudioCues();
  private ui?: RestaurantUI;
  private players!: [Player, Player];
  private stations: Station[] = [];
  private sources: Source[] = [];
  private ruinedItems: RuinedItem[] = [];
  private flight: Flight | null = null;
  private calloutText!: Phaser.GameObjects.Text;
  private calloutTimer?: Phaser.Time.TimerEvent;
  private slotMarkers: Phaser.GameObjects.GameObject[] = [];
  private lastPhase = "landing";
  private diningGraphics!: Phaser.GameObjects.Graphics;
  private diningLabels: Phaser.GameObjects.GameObject[] = [];
  private debugText!: Phaser.GameObjects.Text;
  private aiDebug = false;

  constructor(private readonly restaurant: RestaurantModel) { super("transfer"); }
  attachUI(ui: RestaurantUI): void { this.ui = ui; }

  create(): void {
    this.inputManager = new InputManager(this);
    this.drawKitchen();
    this.players = [this.makePlayer(0, "left"), this.makePlayer(1, "right")];
    this.calloutText = this.add.text(GAME_WIDTH / 2, 68, "", {
      fontFamily: "Nunito, sans-serif", fontSize: "18px", fontStyle: "bold", color: "#ffffff",
      backgroundColor: "#18202dee", padding: { x: 13, y: 7 },
    }).setOrigin(0.5).setDepth(50).setVisible(false);
    this.resetKitchen();
    this.input.keyboard?.on("keydown-F3", () => { this.aiDebug = !this.aiDebug; this.debugText.setVisible(this.aiDebug); });
    window.addEventListener("tt-purchase", () => this.audioCues.play("purchase"));
    window.addEventListener("tt-phase-change", ((event: CustomEvent<{ events?: ServiceEvent[] }>) => this.handlePhaseChange(event.detail?.events ?? [])) as EventListener);
    Object.assign(window, {
      __THROWN_TOGETHER__: {
        snapshot: () => this.snapshot(),
        reset: () => { this.restaurant.restartNight(); this.configureApplianceStations(); this.resetKitchen(); },
        setPlayer: (player: 0 | 1, x: number, y: number, held?: KitchenItem | null) => this.debugSetPlayer(player, x, y, held),
        interact: (player: 0 | 1) => this.interact(player), throw: (player: 0 | 1) => this.throwItem(player),
        advanceFlight: () => { if (this.flight) this.updateFlight(THROW_DURATION_MS); },
        giveIngredient: (player: 0 | 1, id: IngredientId, state: "raw" | "chopped" = "raw") => {
          const item = ingredientItem(id); item.state = state; this.players[player].held = item; this.updateHeld(this.players[player]);
        },
        giveDish: (player: 0 | 1, id: RecipeId, state: "assembled" | "cooked" | "plated") => {
          this.players[player].held = { kind: "dish", recipeId: id, state, valueCents: this.recipeValueCents(id) }; this.updateHeld(this.players[player]);
        },
        endService: () => this.handleServiceEvents(this.restaurant.endService(performance.now())),
      },
    });
  }

  update(_time: number, delta: number): void {
    if (this.restaurant.phase !== this.lastPhase) { this.lastPhase = this.restaurant.phase; this.handlePhaseChange([]); }
    const kitchenActive = this.restaurant.phase === "prep" || this.restaurant.phase === "service";
    const inputs = [this.inputManager.read(0), this.inputManager.read(1)] as const;
    if (inputs.some((input) => input.startPressed)) {
      if (this.restaurant.phase === "planning" && this.restaurant.beginPrep()) this.handlePhaseChange([]);
      else if (this.restaurant.phase === "prep") this.handlePhaseChange(this.restaurant.startService(performance.now()));
    }
    inputs.forEach((input, index) => {
      const player = this.players[index]; player.inputBadge.setText(input.gamepadLabel);
      if (input.resetPressed) window.dispatchEvent(new Event("tt-restart-night"));
      if (!kitchenActive) return;
      const length = Math.hypot(input.x, input.y) || 1;
      player.position = clampToSide({
        x: player.position.x + input.x / Math.max(1, length) * PLAYER_SPEED * delta / 1000,
        y: player.position.y + input.y / Math.max(1, length) * PLAYER_SPEED * delta / 1000,
      }, player.side);
      player.body.setPosition(player.position.x, player.position.y);
      if (input.interactPressed) this.interact(index as 0 | 1);
      if (input.throwPressed) this.throwItem(index as 0 | 1);
    });
    this.updateInteractionHighlights(kitchenActive);
    if (this.flight) this.updateFlight(delta);
    this.updateStations(performance.now());
    if (this.restaurant.phase === "service") this.handleServiceEvents(this.restaurant.updateService(performance.now()));
    this.renderDiningRoom();
    this.updateSourceCounts(); this.ui?.refresh(); this.syncAccessibleStatus();
  }

  private handlePhaseChange(events: ServiceEvent[]): void {
    this.lastPhase = this.restaurant.phase;
    if (this.restaurant.phase === "landing" || this.restaurant.phase === "planning") this.resetKitchen();
    if (this.restaurant.phase === "prep") { this.configureApplianceStations(); this.resetKitchen(); this.callout("CLOSED · PREP TIME", "#f5c85b"); }
    if (this.restaurant.phase === "service") {
      this.audioCues.play("serviceStart"); this.callout("RESTAURANT OPEN!", "#7ed8ba");
      this.stations.filter((station) => station.type === "oven" || station.type === "fryer").forEach((station) => this.tryStartCooking(station, performance.now()));
    }
    if (this.restaurant.phase === "summary") this.callout("SERVICE CLOSED", "#ffdc74");
    this.handleServiceEvents(events); this.ui?.render();
  }

  private handleServiceEvents(events: ServiceEvent[]): void {
    events.forEach((event) => {
      if (event.type === "order-arrived") this.audioCues.play("order");
      if (event.type === "order-expired") this.audioCues.play("expire");
      if (event.type === "customer-arrived") this.audioCues.play("customerArrival");
      if (event.type === "customer-seated") this.audioCues.play("seating");
      if (event.type === "server-pickup") this.audioCues.play("serverPickup");
      if (event.type === "delivery-complete") this.audioCues.play("delivery");
      if (event.type === "customer-left") this.audioCues.play(event.happy ? "satisfied" : "unhappy");
      if (event.type === "dirty-dish-returned") this.audioCues.play("dirtyReturn");
      if (event.type === "plate-washed") this.audioCues.play("plateWashed");
      if (event.type === "service-ended") { this.audioCues.play("serviceEnd"); this.ui?.render(); }
    });
  }

  private resetKitchen(): void {
    this.flight?.visual.destroy(); this.flight?.shadow.destroy(); this.flight?.indicator.destroy(); this.flight = null;
    this.ruinedItems.forEach((item) => item.visual.destroy()); this.ruinedItems = [];
    this.stations.forEach((station) => this.setStationItem(station, null));
    this.players?.forEach((player) => { player.held = null; this.updateHeld(player); });
    if (this.players) {
      this.players[0].position = { ...PLAYER_STARTS.left }; this.players[0].body.setPosition(PLAYER_STARTS.left.x, PLAYER_STARTS.left.y);
      this.players[1].position = { ...PLAYER_STARTS.right }; this.players[1].body.setPosition(PLAYER_STARTS.right.x, PLAYER_STARTS.right.y);
    }
    this.updateSourceCounts();
  }

  private interact(index: 0 | 1): void {
    if (this.restaurant.phase !== "prep" && this.restaurant.phase !== "service") return;
    const player = this.players[index];
    const source = this.nearestSource(player.position); const station = this.nearestStation(player.position);
    if (!player.held) {
      if (station?.type === "sink") { this.startHumanWash(station); return; }
      if (station?.item && station.processStartedAt === 0) {
        player.held = station.item; this.setStationItem(station, null); this.updateHeld(player); this.audioCues.play("pickup"); return;
      }
      if (source) {
        if (!this.restaurant.takeIngredient(source.id)) { this.callout(`${INGREDIENTS[source.id].displayName.toUpperCase()} OUT OF STOCK`, "#ff7e70"); return; }
        player.held = ingredientItem(source.id); this.updateHeld(player); this.updateSourceCounts(); this.audioCues.play("pickup");
        this.callout(`${INGREDIENTS[source.id].displayName.toUpperCase()} TAKEN`, "#f5c85b"); return;
      }
      return;
    }
    if (!station) { this.callout("MOVE CLOSER TO A COUNTER · USE TRASH TO DISCARD", "#ffdc74"); return; }
    if (station.type === "trash") { this.trashHeld(player); return; }
    if (station.type === "pickup") { this.serve(player); return; }
    if (station.type === "sink") { this.callout("PUT FOOD SOMEWHERE SAFE BEFORE WASHING", "#ffdc74"); return; }
    if (station.type === "chop") { this.useChop(player, station); return; }
    if (station.type === "assembly") { this.useAssembly(player, station); return; }
    if (station.type === "oven" || station.type === "fryer") { this.useCooker(player, station); return; }
    if (station.type === "plate") { this.usePlate(player, station); return; }
    if (station.item) { this.callout("WORKSPACE OCCUPIED", "#ffdc74"); return; }
    this.setStationItem(station, player.held); player.held = null; this.updateHeld(player); this.audioCues.play("pickup");
  }

  private useChop(player: Player, station: Station): void {
    const item = player.held!;
    if (station.item) { this.callout("CHOPPING BOARD OCCUPIED", "#ffdc74"); return; }
    if (item.kind !== "ingredient" || item.state !== "raw" || !INGREDIENTS[item.ingredientId].choppable) { this.callout("THAT DOESN'T NEED CHOPPING", "#ff7e70"); return; }
    this.setStationItem(station, item); player.held = null; this.updateHeld(player);
    station.processStartedAt = performance.now(); station.processDuration = CHOP_TIME_MS; station.statusText.setText("CHOPPING"); this.audioCues.play("process");
  }

  private useAssembly(player: Player, station: Station): void {
    const held = player.held!;
    if (!station.item) {
      if (!this.isCheeseBakeComponent(held)) { this.callout("NEEDS CHOPPED POTATO OR CHEESE", "#ff7e70"); return; }
      this.setStationItem(station, held); player.held = null; this.updateHeld(player); return;
    }
    if (this.matchesCheeseBake(station.item, held) && this.restaurant.selectedRecipeIds.includes("cheese-bake")) {
      this.setStationItem(station, { kind: "dish", recipeId: "cheese-bake", state: "assembled", valueCents: station.item.valueCents + held.valueCents });
      player.held = null; this.updateHeld(player); this.audioCues.play("complete"); this.callout("CHEESE BAKE ASSEMBLED", "#7ed8ba"); return;
    }
    this.callout("THOSE ITEMS DON'T COMBINE HERE", "#ff7e70");
  }

  private useCooker(player: Player, station: Station): void {
    if (station.item) { this.callout(`${station.type.toUpperCase()} OCCUPIED`, "#ffdc74"); return; }
    const item = player.held!; const recipeId = this.cookingRecipeFor(item, station.type);
    if (!recipeId || !this.restaurant.selectedRecipeIds.includes(recipeId)) { this.callout(`${station.type.toUpperCase()} REFUSES THAT ITEM`, "#ff7e70"); return; }
    this.setStationItem(station, item); player.held = null; this.updateHeld(player);
    if (this.restaurant.phase === "prep") { station.statusText.setText("READY WHEN OPEN"); this.callout("STAGED · WAITS FOR SERVICE", "#f5c85b"); }
    else this.tryStartCooking(station, performance.now());
  }

  private usePlate(player: Player, station: Station): void {
    const held = player.held!;
    if (station.item) {
      if (this.matchesGardenPlate(station.item, held) && this.restaurant.selectedRecipeIds.includes("garden-plate")) {
        if (!this.consumePlate()) return;
        this.setStationItem(station, { kind: "dish", recipeId: "garden-plate", state: "plated", valueCents: station.item.valueCents + held.valueCents });
        player.held = null; this.updateHeld(player); this.audioCues.play("complete"); this.callout("GARDEN PLATE READY", "#7ed8ba"); return;
      }
      this.callout("PLATING STATION OCCUPIED", "#ffdc74"); return;
    }
    if (held.kind === "dish" && held.state === "cooked") {
      if (!this.consumePlate()) return;
      held.state = "plated"; this.setStationItem(station, held); player.held = null; this.updateHeld(player);
      this.audioCues.play("complete"); this.callout(`${RECIPES[held.recipeId].displayName.toUpperCase()} PLATED`, "#7ed8ba"); return;
    }
    if (this.isGardenComponent(held)) {
      this.setStationItem(station, held); player.held = null; this.updateHeld(player); this.callout("ADD THE OTHER CHOPPED VEGETABLE", "#f5c85b"); return;
    }
    this.callout("ITEM ISN'T READY TO PLATE", "#ff7e70");
  }

  private serve(player: Player): void {
    const held = player.held;
    if (!held || held.kind !== "dish" || held.state !== "plated") { this.callout("ONLY PLATED DISHES CAN BE SERVED", "#ff7e70"); return; }
    if (!this.restaurant.serveDish(held.recipeId)) { this.callout("NO MATCHING ORDER · REFUSED", "#ff7e70"); return; }
    player.held = null; this.updateHeld(player); this.audioCues.play("orderComplete");
    this.callout(`${RECIPES[held.recipeId].displayName.toUpperCase()} READY FOR SERVER`, "#7ed8ba"); this.ui?.refresh(performance.now() + 1000);
  }

  private throwItem(index: 0 | 1): void {
    const player = this.players[index]; const item = player.held;
    if (!item || this.flight) return;
    if (!this.isThrowable(item)) { this.callout("USE THE SHARED COUNTER FOR THAT", "#ffdc74"); return; }
    player.held = null; this.updateHeld(player);
    const to = throwLanding(player.side, player.position.y); const receiver = (index === 0 ? 1 : 0) as 0 | 1;
    const indicator = this.add.circle(to.x, to.y, 48, 0xf5c85b, 0.16).setStrokeStyle(4, 0xffdc74, 0.9).setDepth(16);
    const shadow = this.add.ellipse(player.position.x, player.position.y + 8, 34, 13, 0x11151c, 0.35).setDepth(18);
    const visual = this.makeItemVisual(player.position.x, player.position.y, item).setDepth(25);
    this.flight = { item, from: { ...player.position }, to, elapsed: 0, visual, shadow, indicator, receiver };
    this.tweens.add({ targets: indicator, scale: 1.15, alpha: 0.45, duration: 260, yoyo: true, repeat: 1 });
    this.audioCues.play("throw"); this.time.delayedCall(80, () => this.audioCues.play("incoming")); this.callout("INCOMING!", "#ffdc74");
  }

  private updateFlight(delta: number): void {
    const flight = this.flight; if (!flight) return;
    flight.elapsed = Math.min(THROW_DURATION_MS, flight.elapsed + delta); const t = flight.elapsed / THROW_DURATION_MS;
    const x = Phaser.Math.Linear(flight.from.x, flight.to.x, t); const y = Phaser.Math.Linear(flight.from.y, flight.to.y, t); const arc = Math.sin(Math.PI * t);
    flight.visual.setPosition(x, y - arc * 58).setScale(1 + arc * 0.35).setAngle(t * 300);
    flight.shadow.setPosition(x, y + 8).setScale(1 - arc * 0.28).setAlpha(0.35 - arc * 0.15);
    if (t < 1) return;
    const receiver = this.players[flight.receiver]; const caught = canAutoCatch(receiver.position, flight.to, receiver.held === null);
    flight.visual.destroy(); flight.shadow.destroy(); flight.indicator.destroy(); this.flight = null;
    if (caught) { receiver.held = flight.item; this.updateHeld(receiver); this.audioCues.play("catch"); this.callout(`P${flight.receiver + 1} CAUGHT IT!`, "#7ed8ba"); }
    else {
      this.createRuinedItem(flight.to, flight.item); this.restaurant.recordWaste(flight.item.valueCents); this.audioCues.play("miss");
      this.callout(receiver.held ? `HANDS FULL · ${formatMoney(flight.item.valueCents)} WASTED` : `MISSED · ${formatMoney(flight.item.valueCents)} WASTED`, "#ff7e70");
    }
  }

  private updateStations(now: number): void {
    this.stations.forEach((station) => {
      if (station.type === "sink" && station.processStartedAt > 0) {
        const progress = Math.min(1, (now - station.processStartedAt) / station.processDuration);
        station.progressBg.setVisible(true); station.progressFill.setVisible(true).setScale(progress, 1);
        if (progress >= 1) { station.processStartedAt = 0; station.progressBg.setVisible(false); station.progressFill.setVisible(false); station.statusText.setText("CLEAN PLATE RETURNED"); const event = this.restaurant.completePlateWash("human"); if (event) this.handleServiceEvents([event]); }
        return;
      }
      if (!station.item) { station.progressBg.setVisible(false); station.progressFill.setVisible(false); return; }
      if ((station.type === "oven" || station.type === "fryer") && station.processStartedAt === 0 && this.restaurant.phase === "service") this.tryStartCooking(station, now);
      if (station.processStartedAt <= 0) return;
      const progress = Math.min(1, (now - station.processStartedAt) / station.processDuration);
      station.progressBg.setVisible(true); station.progressFill.setVisible(true).setScale(progress, 1);
      if (progress < 1) return;
      station.processStartedAt = 0; station.progressBg.setVisible(false); station.progressFill.setVisible(false);
      if (station.type === "chop" && station.item.kind === "ingredient") {
        station.item.state = "chopped"; station.statusText.setText("CHOPPED · TAKE"); this.refreshStationVisual(station); this.audioCues.play("complete");
      } else if (station.type === "oven" || station.type === "fryer") {
        const recipeId = this.cookingRecipeFor(station.item, station.type);
        if (recipeId) this.setStationItem(station, { kind: "dish", recipeId, state: "cooked", valueCents: station.item.valueCents });
        station.statusText.setText("COOKED · TAKE"); this.audioCues.play("complete"); this.callout(`${station.type.toUpperCase()} READY!`, "#7ed8ba");
      }
    });
  }

  private tryStartCooking(station: Station, now: number): void {
    if (!station.item || station.processStartedAt > 0 || station.item.state === "cooked") return;
    const recipeId = this.cookingRecipeFor(station.item, station.type); if (!recipeId) return;
    station.processStartedAt = now; station.processDuration = RECIPES[recipeId].cookTimeMs ?? 5000; station.statusText.setText(station.type === "fryer" ? "FRYING" : "BAKING"); this.audioCues.play("process");
  }

  private drawKitchen(): void {
    const g = this.add.graphics();
    g.fillStyle(0x2d3745).fillRoundedRect(24, 22, GAME_WIDTH - 48, GAME_HEIGHT - 44, 16);
    g.fillStyle(0x343f4e).fillRect(36, 84, 408, 452); g.fillStyle(0x303a48).fillRect(516, 84, 408, 452);
    g.lineStyle(1, 0x455264, 0.38);
    for (let x = 36; x <= 924; x += 32) g.lineBetween(x, 84, x, 536);
    for (let y = 84; y <= 536; y += 32) g.lineBetween(36, y, 924, y);
    g.fillStyle(0x171c24).fillRoundedRect(444, 84, 72, 452, 6);
    g.fillStyle(0x687488).fillRoundedRect(438, 94, 84, 156, 8).fillRoundedRect(438, 360, 84, 158, 8);
    this.add.text(62, 55, "PLAYER 1 · LEFT KITCHEN", { fontFamily: "DM Mono, monospace", fontSize: "10px", color: "#f6b75e" });
    this.add.text(898, 55, "PLAYER 2 · RIGHT KITCHEN", { fontFamily: "DM Mono, monospace", fontSize: "10px", color: "#76c8df" }).setOrigin(1, 0);
    g.fillStyle(0x263841).fillRoundedRect(944, 84, 300, 452, 8);
    g.lineStyle(4, 0x7d8b96, 0.9).lineBetween(936, 84, 936, 536);
    this.add.text(1094, 55, "DINING ROOM · AI STAFF", { fontFamily: "DM Mono, monospace", fontSize: "10px", color: "#9de0ca" }).setOrigin(0.5, 0);
    this.sources = SOURCE_LAYOUT.map(({ id, position }) => this.drawSource(id, position));
    this.stations = [
      this.drawStation("shared", "counter", SHARED_POS, "SHARED", "⇄", 0xa7b2c2, 112),
      this.drawStation("trash", "trash", { x: 480, y: 440 }, "TRASH", "×", 0xc85f58, 70),
      this.drawStation("left-counter", "counter", { x: 340, y: 305 }, "STAGING", "□", 0x8391a6),
      this.drawStation("right-counter", "counter", { x: 660, y: 305 }, "STAGING", "□", 0x8391a6),
      this.drawStation("pickup", "pickup", { x: 880, y: 305 }, "SERVICE PICKUP", "↑", 0x7ed8ba, 100),
      this.drawStation("sink", "sink", { x: 820, y: 440 }, "DISH SINK", "≈", 0x72b7da, 100),
    ];
    this.configureApplianceStations();
    this.diningGraphics = this.add.graphics().setDepth(8);
    this.debugText = this.add.text(952, 88, "", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: "#ffdc74", backgroundColor: "#111821dd", padding: { x: 4, y: 3 } }).setDepth(40).setVisible(false);
  }

  private drawSource(id: IngredientId, position: Vec2): Source {
    const ingredient = INGREDIENTS[id];
    this.add.rectangle(position.x, position.y, 94, 92, 0x222a35).setStrokeStyle(3, ingredient.color, 0.85).setDepth(4);
    this.add.text(position.x, position.y - 16, ingredient.icon, { fontFamily: "Nunito, sans-serif", fontSize: "24px", fontStyle: "bold", color: `#${ingredient.color.toString(16).padStart(6, "0")}` }).setOrigin(0.5).setDepth(5);
    this.add.text(position.x, position.y + 13, ingredient.displayName.toUpperCase(), { fontFamily: "DM Mono, monospace", fontSize: "8px", color: "#d7dde7" }).setOrigin(0.5).setDepth(5);
    const countText = this.add.text(position.x, position.y + 31, "STOCK 0", { fontFamily: "DM Mono, monospace", fontSize: "9px", color: "#f5c85b" }).setOrigin(0.5).setDepth(5);
    return { id, position, countText };
  }

  private drawStation(id: string, type: StationType, position: Vec2, label: string, icon: string, color: number, width = 108): Station {
    const background = this.add.rectangle(position.x, position.y, width, 92, 0x222a35).setStrokeStyle(3, color, 0.82).setDepth(4);
    const iconText = this.add.text(position.x, position.y - 20, icon, { fontFamily: "Nunito, sans-serif", fontSize: "24px", fontStyle: "bold", color: `#${color.toString(16).padStart(6, "0")}` }).setOrigin(0.5).setDepth(5);
    const labelText = this.add.text(position.x, position.y + 18, label, { fontFamily: "DM Mono, monospace", fontSize: "9px", color: "#d7dde7" }).setOrigin(0.5).setDepth(5);
    const statusText = this.add.text(position.x, position.y + 34, "", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: "#f5c85b" }).setOrigin(0.5).setDepth(6);
    const itemVisual = this.add.container(position.x, position.y - 4).setDepth(14);
    const progressBg = this.add.rectangle(position.x - width / 2 + 7, position.y + 40, width - 14, 5, 0x11151c).setOrigin(0, 0.5).setDepth(15).setVisible(false);
    const progressFill = this.add.rectangle(position.x - width / 2 + 7, position.y + 40, width - 14, 5, 0x7ed8ba).setOrigin(0, 0.5).setDepth(16).setVisible(false);
    return { id, type, position, item: null, itemVisual, statusText, progressBg, progressFill, processStartedAt: 0, processDuration: 0, background, baseColor: color, highlighted: false, visuals: [background, iconText, labelText, statusText, itemVisual, progressBg, progressFill] };
  }

  private configureApplianceStations(): void {
    const permanent = this.stations.filter((station) => !station.id.startsWith("slot-"));
    this.stations.filter((station) => station.id.startsWith("slot-")).forEach((station) => station.visuals.forEach((visual) => visual.destroy()));
    this.slotMarkers.forEach((marker) => marker.destroy()); this.slotMarkers = []; this.stations = permanent;
    KITCHEN_SLOTS.forEach((slot) => {
      if (slot.requiredKitchenLevel > this.restaurant.kitchenLevel) { this.drawSlotMarker({ x: slot.x, y: slot.y }, "EXPANSION", 0x4e5664); return; }
      const applianceId = this.restaurant.installedSlots[slot.index];
      if (!applianceId) { this.drawSlotMarker({ x: slot.x, y: slot.y }, `SLOT ${slot.index + 1} EMPTY`, 0x687488); return; }
      const appliance = APPLIANCES[applianceId];
      this.stations.push(this.drawStation(`slot-${slot.index}`, appliance.stationType, { x: slot.x, y: slot.y }, appliance.displayName.toUpperCase(), appliance.icon, appliance.color));
    });
  }

  private drawSlotMarker(position: Vec2, label: string, color: number): void {
    const box = this.add.rectangle(position.x, position.y, 108, 92, 0x1a2029, 0.6).setStrokeStyle(2, color, 0.7).setDepth(3);
    const text = this.add.text(position.x, position.y, label, { fontFamily: "DM Mono, monospace", fontSize: "8px", color: `#${color.toString(16).padStart(6, "0")}`, align: "center" }).setOrigin(0.5).setDepth(4);
    this.slotMarkers.push(box, text);
  }

  private makePlayer(index: 0 | 1, side: Side): Player {
    const color = index === 0 ? 0xf2a94f : 0x62bed7; const heldVisual = this.add.container(0, -31).setVisible(false);
    const body = this.add.container(PLAYER_STARTS[side].x, PLAYER_STARTS[side].y, [this.add.ellipse(0, 14, 54, 22, 0x10141a, 0.35)]);
    body.add([this.add.circle(0, 0, 25, 0x202733).setStrokeStyle(5, color), this.add.circle(0, -3, 14, color), this.add.rectangle(0, 12, 25, 14, 0xf2eee4), heldVisual]); body.setDepth(20);
    body.add(this.add.text(0, 38, `P${index + 1}`, { fontFamily: "Nunito, sans-serif", fontSize: "12px", fontStyle: "bold", color: "#fff" }).setOrigin(0.5));
    const inputBadge = this.add.text(0, 54, "TOUCH / KEYS", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: index === 0 ? "#f6b75e" : "#76c8df", backgroundColor: "#18202dcc", padding: { x: 4, y: 2 } }).setOrigin(0.5); body.add(inputBadge);
    return { side, position: { ...PLAYER_STARTS[side] }, held: null, body, heldVisual, inputBadge };
  }

  private setStationItem(station: Station, item: KitchenItem | null): void {
    station.item = item; station.processStartedAt = 0; station.processDuration = 0; station.statusText.setText(""); station.progressBg.setVisible(false); station.progressFill.setVisible(false); this.refreshStationVisual(station);
  }
  private refreshStationVisual(station: Station): void { station.itemVisual.removeAll(true); station.itemVisual.setVisible(Boolean(station.item)); if (station.item) this.populateItemVisual(station.itemVisual, station.item); }
  private makeItemVisual(x: number, y: number, item: KitchenItem): Phaser.GameObjects.Container { const container = this.add.container(x, y); this.populateItemVisual(container, item); return container.setDepth(14); }

  private populateItemVisual(container: Phaser.GameObjects.Container, item: KitchenItem): void {
    if (item.kind === "ingredient") {
      const definition = INGREDIENTS[item.ingredientId]; const color = item.state === "ruined" ? 0x655c54 : definition.color;
      container.add(this.add.ellipse(0, 0, 30, 24, color).setStrokeStyle(2, 0x332b25));
      container.add(this.add.text(0, 0, item.state === "ruined" ? "×" : definition.icon, { fontFamily: "Nunito, sans-serif", fontSize: "12px", fontStyle: "bold", color: item.state === "ruined" ? "#ff7e70" : "#fff" }).setOrigin(0.5));
      if (item.state === "chopped") { container.add(this.add.line(0, 0, -11, -8, 11, 8, 0xffffff, 0.8).setLineWidth(2)); container.add(this.add.line(0, 0, -11, 8, 11, -8, 0xffffff, 0.8).setLineWidth(2)); }
      return;
    }
    const recipe = RECIPES[item.recipeId]; if (item.state === "plated") container.add(this.add.circle(0, 2, 21, 0xf1eee7).setStrokeStyle(2, 0xaeb8c7));
    const foodColor = item.state === "cooked" ? Phaser.Display.Color.ValueToColor(recipe.color).darken(22).color : recipe.color;
    container.add(this.add.circle(0, item.state === "plated" ? 0 : 2, item.state === "assembled" ? 15 : 13, foodColor));
    container.add(this.add.text(0, 0, item.state === "ruined" ? "×" : recipe.icon, { fontFamily: "DM Mono, monospace", fontSize: "9px", fontStyle: "bold", color: item.state === "ruined" ? "#ff7e70" : "#17202a" }).setOrigin(0.5));
  }

  private updateHeld(player: Player): void { player.heldVisual.removeAll(true); if (!player.held) { player.heldVisual.setVisible(false); return; } this.populateItemVisual(player.heldVisual, player.held); player.heldVisual.setVisible(true).setDepth(30); }
  private updateInteractionHighlights(kitchenActive: boolean): void {
    const active = new Set<Station>();
    if (kitchenActive) this.players.forEach((player) => { const station = this.nearestStation(player.position); if (station) active.add(station); });
    this.stations.forEach((station) => {
      const highlighted = active.has(station); if (station.highlighted === highlighted) return;
      station.highlighted = highlighted;
      station.background.setFillStyle(highlighted ? 0x3b4656 : 0x222a35, 1).setStrokeStyle(highlighted ? 6 : 3, highlighted ? 0xffe38a : station.baseColor, highlighted ? 1 : 0.82);
    });
  }
  private nearestStation(position: Vec2): Station | null { return this.stations.map((station) => ({ station, range: distance(position, station.position) })).filter(({ range }) => range <= INTERACT_DISTANCE).sort((a, b) => a.range - b.range)[0]?.station ?? null; }
  private nearestSource(position: Vec2): Source | null { return this.sources.map((source) => ({ source, range: distance(position, source.position) })).filter(({ range }) => range <= INTERACT_DISTANCE).sort((a, b) => a.range - b.range)[0]?.source ?? null; }
  private updateSourceCounts(): void { this.sources?.forEach((source) => source.countText.setText(`STOCK ${this.restaurant.inventory[source.id]}`)); }
  private isThrowable(item: KitchenItem): boolean { return item.kind === "ingredient" && item.state !== "ruined" && INGREDIENTS[item.ingredientId].throwable; }
  private isCheeseBakeComponent(item: KitchenItem): boolean { return item.kind === "ingredient" && ((item.ingredientId === "potato" && item.state === "chopped") || (item.ingredientId === "cheese" && item.state === "raw")); }
  private matchesCheeseBake(a: KitchenItem, b: KitchenItem): boolean { return this.isCheeseBakeComponent(a) && this.isCheeseBakeComponent(b) && a.kind === "ingredient" && b.kind === "ingredient" && a.ingredientId !== b.ingredientId; }
  private isGardenComponent(item: KitchenItem): boolean { return item.kind === "ingredient" && item.state === "chopped" && (item.ingredientId === "tomato" || item.ingredientId === "onion"); }
  private matchesGardenPlate(a: KitchenItem, b: KitchenItem): boolean { return this.isGardenComponent(a) && this.isGardenComponent(b) && a.kind === "ingredient" && b.kind === "ingredient" && a.ingredientId !== b.ingredientId; }
  private cookingRecipeFor(item: KitchenItem, stationType: StationType): RecipeId | null {
    if (stationType === "oven" && item.kind === "ingredient" && item.ingredientId === "potato" && item.state === "chopped") return "roast-potato";
    if (stationType === "oven" && item.kind === "dish" && item.recipeId === "cheese-bake" && item.state === "assembled") return "cheese-bake";
    if (stationType === "fryer" && item.kind === "ingredient" && item.ingredientId === "potato" && item.state === "chopped") return "fries";
    return null;
  }
  private recipeValueCents(recipeId: RecipeId): number { return RECIPES[recipeId].ingredients.reduce((total, requirement) => total + INGREDIENTS[requirement.ingredientId].purchaseCostCents, 0); }
  private consumePlate(): boolean { if (!this.restaurant.useCleanPlate()) { this.callout("NO CLEAN PLATES · WASH AT SINK", "#ff7e70"); return false; } return true; }

  private startHumanWash(station: Station): void {
    if (station.processStartedAt > 0) { this.callout("WASHING IN PROGRESS", "#f5c85b"); return; }
    if (!this.restaurant.claimDirtyPlate()) { this.callout("NO DIRTY PLATES AT RETURN", "#ffdc74"); return; }
    station.processStartedAt = performance.now(); station.processDuration = DISHWASH_DURATION_MS; station.statusText.setText("WASHING"); this.audioCues.play("process");
  }

  private trashHeld(player: Player): void { const item = player.held!; player.held = null; this.updateHeld(player); this.restaurant.recordWaste(item.valueCents); this.audioCues.play("miss"); this.callout(`TRASHED · ${formatMoney(item.valueCents)} WASTED`, "#ff7e70"); }
  private createRuinedItem(position: Vec2, item: KitchenItem): void {
    const ruined: KitchenItem = item.kind === "ingredient" ? { ...item, state: "ruined" } : { ...item, state: "ruined" };
    const visual = this.add.container(position.x, position.y).setDepth(9); visual.add(this.add.ellipse(0, 11, 70, 34, 0x5d4938, 0.75)); this.populateItemVisual(visual, ruined);
    visual.add(this.add.text(0, 36, "WASTED", { fontFamily: "DM Mono, monospace", fontSize: "8px", color: "#ff8b7e", backgroundColor: "#18202dcc", padding: { x: 4, y: 2 } }).setOrigin(0.5)); this.ruinedItems.push({ position, visual });
  }
  private renderDiningRoom(): void {
    if (!this.diningGraphics) return;
    const pickup = this.stations.find(({ type }) => type === "pickup"); if (pickup) pickup.statusText.setText(`SLOTS ${this.restaurant.readyDishes.length}/3`);
    const sink = this.stations.find(({ type }) => type === "sink"); if (sink && sink.processStartedAt === 0) sink.statusText.setText(`DIRTY ${this.restaurant.dirtyReturnQueue}`);
    this.diningGraphics.clear(); this.diningLabels.forEach((object) => object.destroy()); this.diningLabels = [];
    const graphics = this.diningGraphics; const now = performance.now();
    graphics.fillStyle(0x1b252d, 0.75).fillRoundedRect(958, 92, 272, 432, 8);
    graphics.fillStyle(0x6c7b88).fillRect(1222, 250, 10, 86);
    this.diningLabels.push(this.add.text(1217, 293, "ENTRANCE", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: "#d7dde7" }).setOrigin(0.5).setAngle(-90).setDepth(12));
    graphics.fillStyle(0x394c55).fillRoundedRect(948, 418, 38, 72, 5);
    this.diningLabels.push(this.add.text(967, 454, `DIRTY\n${this.restaurant.dirtyReturnQueue}`, { fontFamily: "DM Mono, monospace", fontSize: "7px", align: "center", color: "#f0b78a" }).setOrigin(0.5).setDepth(12));
    this.restaurant.diningTables.forEach((table) => {
      const colors: Record<string, number> = { clean: 0x527b68, reserved: 0x756743, waiting_food: 0x8a6640, eating: 0x3f6f7c, dirty: 0x744d47 };
      graphics.fillStyle(0x12171c, 0.35).fillEllipse(table.x, table.y + 12, 82, 30);
      graphics.fillStyle(colors[table.state] ?? 0x5b6672).fillRoundedRect(table.x - 34, table.y - 22, 68, 44, 8);
      graphics.lineStyle(2, 0xd1dae2, 0.55).strokeRoundedRect(table.x - 34, table.y - 22, 68, 44, 8);
      graphics.fillStyle(0x8995a5).fillCircle(table.x - 47, table.y, 10).fillCircle(table.x + 47, table.y, 10);
      if (table.state === "dirty") { graphics.lineStyle(3, 0xe8e5dc).strokeCircle(table.x, table.y, 9); graphics.lineStyle(2, 0xaa665d).lineBetween(table.x - 5, table.y - 5, table.x + 5, table.y + 5); }
      this.diningLabels.push(this.add.text(table.x, table.y + 31, `${table.id.toUpperCase()} · ${table.state.replaceAll("_", " ").toUpperCase()}`, { fontFamily: "DM Mono, monospace", fontSize: "6px", color: "#d7dde7" }).setOrigin(0.5).setDepth(12));
    });
    this.restaurant.customers.filter(({ state }) => state !== "failed").forEach((customer) => {
      const table = this.restaurant.diningTables.find(({ id }) => id === customer.tableId);
      const waitingIndex = this.restaurant.customers.filter(({ state }) => state === "arriving" || state === "waiting_for_table").findIndex(({ id }) => id === customer.id);
      const seatingTask = this.restaurant.activeStaff.find(({ task }) => task?.type === "seat" && task.targetId === String(customer.id))?.task;
      const walkProgress = seatingTask ? Phaser.Math.Clamp((now - seatingTask.startedAt) / (seatingTask.endsAt - seatingTask.startedAt), 0, 1) : 1;
      const x = table ? Phaser.Math.Linear(customer.state === "walking_to_table" ? 1202 : table.x, table.x, walkProgress) : 1202 - (waitingIndex % 2) * 24;
      const y = table ? Phaser.Math.Linear(customer.state === "walking_to_table" ? 305 : table.y - 8, table.y - 8, walkProgress) : 365 + Math.floor(Math.max(0, waitingIndex) / 2) * 30;
      for (let member = 0; member < customer.size; member++) { const offset = customer.size === 2 ? (member ? 13 : -13) : 0; graphics.fillStyle(customer.failureReason ? 0xb95851 : 0xd6c583).fillCircle(x + offset, y, 9); graphics.fillStyle(0x28323d).fillCircle(x + offset, y - 2, 3); }
      this.diningLabels.push(this.add.text(x, y + 12, customer.state === "waiting_for_food" ? RECIPES[customer.recipeId].icon : customer.state.replaceAll("_", " "), { fontFamily: "DM Mono, monospace", fontSize: "6px", color: "#fff" }).setOrigin(0.5).setDepth(12));
    });
    this.restaurant.activeStaff.forEach((staff, index) => {
      let x = staff.role === "dishwasher" ? 840 : 992 + index * 22; let y = staff.role === "dishwasher" ? 440 : 510;
      const tableId = staff.task?.destination.match(/Table (\d+)/)?.[1]; const table = tableId ? this.restaurant.diningTables.find(({ id }) => id === `t${tableId}`) : undefined;
      if (staff.task && table) {
        const progress = Phaser.Math.Clamp((now - staff.task.startedAt) / (staff.task.endsAt - staff.task.startedAt), 0, 1);
        const start = staff.task.type === "deliver" ? { x: 965, y: 305 } : { x: 1202, y: 305 };
        x = Phaser.Math.Linear(start.x, table.x, progress); y = Phaser.Math.Linear(start.y, table.y + 38, progress);
      } else if (staff.task?.type === "clear") {
        const dirtyTable = this.restaurant.diningTables.find(({ id }) => id === staff.task?.targetId); const progress = Phaser.Math.Clamp((now - staff.task.startedAt) / (staff.task.endsAt - staff.task.startedAt), 0, 1);
        if (dirtyTable) { x = Phaser.Math.Linear(dirtyTable.x, 967, progress); y = Phaser.Math.Linear(dirtyTable.y + 38, 454, progress); }
      }
      graphics.fillStyle(staff.role === "server" ? 0x8e79d3 : 0x62aeca).fillCircle(x, y, 12); graphics.fillStyle(0xf2eee4).fillRect(x - 7, y + 8, 14, 10);
      this.diningLabels.push(this.add.text(x, y + 21, `${staff.name} · ${staff.state}`, { fontFamily: "DM Mono, monospace", fontSize: "6px", color: "#dcd4ff" }).setOrigin(0.5).setDepth(12));
    });
    if (this.aiDebug) this.debugText.setText(this.restaurant.activeStaff.map((staff) => `${staff.name}: ${staff.state}\n task=${staff.task?.type ?? "none"} target=${staff.task?.targetId ?? "-"}\n dest=${staff.task?.destination ?? "idle"}`).join("\n"));
  }
  private callout(message: string, color: string): void {
    this.calloutTimer?.remove(false); this.calloutText.setText(message).setColor(color).setVisible(true).setAlpha(1).setScale(0.92);
    this.tweens.add({ targets: this.calloutText, scale: 1, duration: 100, ease: "Back.Out" });
    this.calloutTimer = this.time.delayedCall(1350, () => this.tweens.add({ targets: this.calloutText, alpha: 0, duration: 220, onComplete: () => this.calloutText.setVisible(false) }));
  }
  private debugSetPlayer(index: 0 | 1, x: number, y: number, held?: KitchenItem | null): void { const player = this.players[index]; player.position = clampToSide({ x, y }, player.side); player.body.setPosition(player.position.x, player.position.y); if (held !== undefined) { player.held = held; this.updateHeld(player); } }
  private snapshot(): object {
    return { phase: this.restaurant.phase, day: this.restaurant.day, cashCents: this.restaurant.cashCents, revenueCents: this.restaurant.revenueCents, spendingCents: this.restaurant.ingredientSpendingCents, wasteCents: this.restaurant.wastedValueCents, inventory: { ...this.restaurant.inventory }, installedSlots: [...this.restaurant.installedSlots], plates: this.restaurant.platesRemaining,
      players: this.players.map((player) => ({ side: player.side, x: Math.round(player.position.x), y: Math.round(player.position.y), held: player.held })), stations: Object.fromEntries(this.stations.map((station) => [station.id, station.item])),
      orders: this.restaurant.activeOrders.map((order) => ({ id: order.id, recipeId: order.recipeId, tableId: order.tableId, customerId: order.customerId })), readyDishes: this.restaurant.readyDishes.map((dish) => ({ ...dish })), customers: this.restaurant.customers.map((customer) => ({ ...customer })), tables: this.restaurant.diningTables.map((table) => ({ ...table })), staff: this.restaurant.activeStaff.map((staff) => ({ ...staff })), dirtyReturn: this.restaurant.dirtyReturnQueue, inFlight: this.flight?.item ?? null, messCount: this.ruinedItems.length,
      highlightedStations: this.stations.filter((station) => station.highlighted).map((station) => station.id), summary: this.restaurant.summary() };
  }
  private syncAccessibleStatus(): void { const output = document.getElementById("game-status"); if (output) output.textContent = JSON.stringify(this.snapshot()); }
}
