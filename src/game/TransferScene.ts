import Phaser from "phaser";
import { AudioCues } from "./AudioCues";
import { ART_PALETTE, characterParts, drawMiniCharacter, populateFoodArt } from "./ArtFactory";
import { InputManager } from "./InputManager";
import { PlayerSession } from "./PlayerSession";
import { RestaurantModel, type DiningTable, type ServiceEvent } from "./RestaurantModel";
import type { RestaurantUI } from "./RestaurantUI";
import {
  APPLIANCES, CHOP_TIME_MS, DISHWASH_DURATION_MS, INGREDIENTS, KITCHEN_SLOTS, RECIPES, formatMoney, ingredientItem,
  type IngredientId, type KitchenItem, type RecipeId,
} from "./data";
import {
  GAME_HEIGHT, GAME_WIDTH, INTERACT_DISTANCE, PLAYER_SPEED,
  THROW_DURATION_MS, type Vec2,
} from "./config";
import {
  DIRTY_RETURN_POS, DISH_SINK_POS, ENTRANCE_POS, ISLAND_COUNTERS, OPEN_KITCHEN_PLAYER_STARTS,
  PANTRY_LAYOUT, SERVER_STAGING_POS, SERVICE_DOOR, SERVICE_PICKUP_POS, TRASH_POS,
} from "./layout";
import { canAutoCatch, clampRestaurantMovement, distance, throwLanding } from "./rules";

type StationType = "counter" | "chop" | "assembly" | "oven" | "fryer" | "plate" | "trash" | "pickup" | "sink";

interface Player {
  position: Vec2;
  facing: Vec2;
  held: KitchenItem | null;
  carryingDirtyPlate: boolean;
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
  indicator: Phaser.GameObjects.Arc; receiver: 0 | 1 | null; landingStationId: string | null;
}
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

  constructor(private readonly restaurant: RestaurantModel, private readonly playerSession: PlayerSession) { super("transfer"); }
  attachUI(ui: RestaurantUI): void { this.ui = ui; }

  create(): void {
    this.inputManager = new InputManager(this);
    this.drawKitchen();
    this.players = [this.makePlayer(0), this.makePlayer(1)];
    this.syncPlayerMode();
    this.playerSession.onChange(() => this.syncPlayerMode());
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

  update(time: number, delta: number): void {
    if (this.restaurant.phase !== this.lastPhase) { this.lastPhase = this.restaurant.phase; this.handlePhaseChange([]); }
    const kitchenActive = this.restaurant.phase === "prep" || this.restaurant.phase === "service";
    const inputs = [this.inputManager.read(0), this.inputManager.read(1)] as const;
    const joiningPlayerTwo = !this.playerSession.isActive(1) && kitchenActive && inputs[1].activity;
    if (joiningPlayerTwo) { this.playerSession.setMode("coop"); this.callout("PLAYER 2 JOINED!", "#7ed8ba"); }
    if (inputs.some((input, index) => this.playerSession.isActive(index as 0 | 1) && input.startPressed) && !joiningPlayerTwo) {
      if (this.restaurant.phase === "planning" && this.restaurant.beginPrep()) this.handlePhaseChange([]);
      else if (this.restaurant.phase === "prep") this.handlePhaseChange(this.restaurant.startService(performance.now()));
    }
    inputs.forEach((input, index) => {
      const player = this.players[index]; player.inputBadge.setText(input.gamepadLabel);
      if (input.resetPressed) window.dispatchEvent(new Event("tt-restart-night"));
      if (!kitchenActive || !this.playerSession.isActive(index as 0 | 1)) return;
      const length = Math.hypot(input.x, input.y) || 1;
      if (Math.abs(input.x) + Math.abs(input.y) > 0.15) player.facing = { x: input.x, y: input.y };
      player.position = clampRestaurantMovement(player.position, {
        x: player.position.x + input.x / Math.max(1, length) * PLAYER_SPEED * delta / 1000,
        y: player.position.y + input.y / Math.max(1, length) * PLAYER_SPEED * delta / 1000,
      });
      player.body.setPosition(player.position.x, player.position.y);
      player.body.setAngle(Math.abs(input.x) + Math.abs(input.y) > 0.15 ? Math.sin(time / 95 + index * Math.PI) * 2.5 : 0);
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
    this.players?.forEach((player) => { player.held = null; player.carryingDirtyPlate = false; this.updateHeld(player); });
    if (this.players) {
      this.players.forEach((player, index) => {
        player.position = { ...OPEN_KITCHEN_PLAYER_STARTS[index] };
        player.facing = { x: index === 0 ? 1 : -1, y: 0 };
        player.body.setPosition(player.position.x, player.position.y);
      });
    }
    this.updateSourceCounts();
  }

  private interact(index: 0 | 1): void {
    if (this.restaurant.phase !== "prep" && this.restaurant.phase !== "service") return;
    const player = this.players[index];
    const source = this.nearestSource(player.position); const station = this.nearestStation(player.position); const table = this.nearestTable(player.position);
    if (player.carryingDirtyPlate) {
      if (station?.type !== "sink") { this.callout("BRING THE DIRTY PLATE TO THE SINK", "#ffdc74"); return; }
      const event = this.restaurant.returnCarriedDirtyPlate(); if (event) { player.carryingDirtyPlate = false; this.updateHeld(player); this.handleServiceEvents([event]); this.callout("DIRTY PLATE RETURNED · USE AGAIN TO WASH", "#7ed8ba"); }
      return;
    }
    if (table && this.restaurant.phase === "service") { this.interactWithTable(player, table); return; }
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

  private interactWithTable(player: Player, table: DiningTable): void {
    if (player.held) {
      if (player.held.kind !== "dish" || player.held.state !== "plated") { this.callout("ONLY A PLATED ORDER GOES TO A TABLE", "#ff7e70"); return; }
      const events = this.restaurant.deliverDishToTable(player.held.recipeId, table.id, performance.now());
      if (!events.length) { this.callout(`TABLE ${table.id.slice(1)} ISN'T WAITING FOR THAT`, "#ff7e70"); return; }
      const dishName = RECIPES[player.held.recipeId].displayName; player.held = null; this.updateHeld(player); this.handleServiceEvents(events); this.audioCues.play("orderComplete");
      this.callout(`${dishName.toUpperCase()} SERVED TO TABLE ${table.id.slice(1)}`, "#7ed8ba"); return;
    }
    if (this.restaurant.collectDirtyPlateFromTable(table.id)) { player.carryingDirtyPlate = true; this.updateHeld(player); this.audioCues.play("pickup"); this.callout(`DIRTY PLATE FROM TABLE ${table.id.slice(1)} · RETURN TO SINK`, "#f5c85b"); return; }
    this.callout(`TABLE ${table.id.slice(1)} · ${table.state.replaceAll("_", " ").toUpperCase()}`, "#ffdc74");
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
    if (!this.restaurant.activeStaff.some(({ role }) => role === "server")) { this.callout("NO SERVER · CARRY THIS THROUGH THE DOOR TO ITS TABLE", "#ffdc74"); return; }
    if (!this.restaurant.serveDish(held.recipeId)) { this.callout("NO MATCHING ORDER · REFUSED", "#ff7e70"); return; }
    player.held = null; this.updateHeld(player); this.audioCues.play("orderComplete");
    this.callout(`${RECIPES[held.recipeId].displayName.toUpperCase()} READY FOR SERVER`, "#7ed8ba"); this.ui?.refresh(performance.now() + 1000);
  }

  private throwItem(index: 0 | 1): void {
    if (!this.playerSession.isActive(index)) return;
    const player = this.players[index]; const item = player.held;
    if (!item || player.carryingDirtyPlate || this.flight) return;
    if (!this.isThrowable(item)) { this.callout("CARRY THAT TO A STAGING COUNTER", "#ffdc74"); return; }
    player.held = null; this.updateHeld(player);
    const rawLanding = throwLanding(player.position, player.facing);
    const landingCounter = this.stations.filter(({ type, item: stationItem }) => type === "counter" && !stationItem)
      .map((station) => ({ station, range: distance(station.position, rawLanding) }))
      .filter(({ range }) => range <= 92).sort((a, b) => a.range - b.range)[0]?.station;
    const to = landingCounter ? { ...landingCounter.position } : rawLanding;
    const receiver = this.playerSession.mode === "coop" ? (index === 0 ? 1 : 0) as 0 | 1 : null;
    const indicator = this.add.circle(to.x, to.y, 48, 0xf5c85b, 0.16).setStrokeStyle(4, 0xffdc74, 0.9).setDepth(16);
    const shadow = this.add.ellipse(player.position.x, player.position.y + 8, 34, 13, 0x11151c, 0.35).setDepth(18);
    const visual = this.makeItemVisual(player.position.x, player.position.y, item).setDepth(25);
    this.flight = { item, from: { ...player.position }, to, elapsed: 0, visual, shadow, indicator, receiver, landingStationId: landingCounter?.id ?? null };
    this.tweens.add({ targets: player.body, scaleX: 1.08, scaleY: 0.94, duration: 100, yoyo: true });
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
    const receiver = flight.receiver === null ? null : this.players[flight.receiver];
    const caught = !!receiver && canAutoCatch(receiver.position, flight.to, receiver.held === null);
    flight.visual.destroy(); flight.shadow.destroy(); flight.indicator.destroy(); this.flight = null;
    if (caught && receiver && flight.receiver !== null) { receiver.held = flight.item; this.updateHeld(receiver); this.tweens.add({ targets: receiver.body, scaleX: 1.1, scaleY: 0.93, duration: 90, yoyo: true }); this.audioCues.play("catch"); this.callout(`P${flight.receiver + 1} CAUGHT IT!`, "#7ed8ba"); }
    else if (flight.landingStationId) {
      const station = this.stations.find(({ id }) => id === flight.landingStationId);
      if (station && !station.item) { this.setStationItem(station, flight.item); this.audioCues.play("pickup"); this.callout("NICE TOSS · LANDED ON THE ISLAND", "#7ed8ba"); return; }
      this.ruinThrownItem(flight, receiver);
    }
    else {
      this.ruinThrownItem(flight, receiver);
    }
  }

  private ruinThrownItem(flight: Flight, receiver: Player | null): void {
    this.createRuinedItem(flight.to, flight.item); this.restaurant.recordWaste(flight.item.valueCents); this.audioCues.play("miss");
    this.callout(receiver?.held ? `HANDS FULL · ${formatMoney(flight.item.valueCents)} WASTED` : `MISSED · ${formatMoney(flight.item.valueCents)} WASTED`, "#ff7e70");
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
    g.fillStyle(ART_PALETTE.ink).fillRoundedRect(18, 18, GAME_WIDTH - 36, GAME_HEIGHT - 36, 22);
    g.fillStyle(ART_PALETTE.cream).fillRoundedRect(25, 25, GAME_WIDTH - 50, GAME_HEIGHT - 50, 17);
    g.fillStyle(0x80b7c4).fillRoundedRect(36, 84, 888, 452, 8);
    g.lineStyle(1, 0xffffff, 0.22);
    for (let x = 36; x <= 924; x += 44) g.lineBetween(x, 84, x, 536);
    for (let y = 84; y <= 536; y += 40) g.lineBetween(36, y, 924, y);
    g.fillStyle(0x5f8f8c, 0.34).fillRoundedRect(48, 96, 214, 205, 14);
    g.lineStyle(2, 0xfff1cf, 0.7).strokeRoundedRect(48, 96, 214, 205, 14);
    g.fillStyle(0xffe7ba, 0.3).fillRoundedRect(286, 256, 408, 118, 18);
    g.lineStyle(3, ART_PALETTE.ink, 0.45).strokeRoundedRect(286, 256, 408, 118, 18);
    g.fillStyle(0xdb715e, 0.22).fillRoundedRect(752, 96, 160, 416, 14);
    this.add.text(60, 55, "OPEN KITCHEN · SOLO READY · CO-OP FRIENDLY", { fontFamily: "DM Mono, monospace", fontSize: "10px", color: "#35556b", fontStyle: "bold" });
    this.add.text(70, 103, "PANTRY", { fontFamily: "DM Mono, monospace", fontSize: "8px", color: "#fff1cf", fontStyle: "bold" }).setDepth(6);
    this.add.text(490, 263, "CENTRAL PREP + STAGING ISLAND", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: "#68412d", fontStyle: "bold" }).setOrigin(0.5).setDepth(6);
    this.add.text(878, 103, "SERVICE", { fontFamily: "DM Mono, monospace", fontSize: "8px", color: "#8a403e", fontStyle: "bold" }).setOrigin(1, 0).setDepth(6);
    g.fillStyle(ART_PALETTE.wood).fillRoundedRect(944, 84, 300, 452, 8);
    g.lineStyle(2, ART_PALETTE.woodLight, 0.7);
    for (let x = 956; x < 1240; x += 48) g.lineBetween(x, 84, x, 536);
    for (let y = 112; y < 536; y += 56) g.lineBetween(944, y, 1244, y);
    g.lineStyle(6, ART_PALETTE.ink, 1).lineBetween(936, 84, 936, SERVICE_DOOR.minY).lineBetween(936, SERVICE_DOOR.maxY, 936, 536);
    g.fillStyle(0xffe7ba, 0.55).fillRect(SERVICE_DOOR.kitchenX, SERVICE_DOOR.minY, SERVICE_DOOR.diningX - SERVICE_DOOR.kitchenX, SERVICE_DOOR.maxY - SERVICE_DOOR.minY);
    g.lineStyle(3, 0x6fa447, 0.9).lineBetween(SERVICE_DOOR.kitchenX, SERVICE_DOOR.minY, SERVICE_DOOR.diningX, SERVICE_DOOR.minY).lineBetween(SERVICE_DOOR.kitchenX, SERVICE_DOOR.maxY, SERVICE_DOOR.diningX, SERVICE_DOOR.maxY);
    this.add.text(936, SERVICE_DOOR.minY + 8, "CHEF DOOR", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: "#49352d", fontStyle: "bold", backgroundColor: "#fff1cfcc", padding: { x: 3, y: 1 } }).setOrigin(0.5).setAngle(-90).setDepth(12);
    this.add.text(1094, 55, "DINING ROOM · SERVE TABLES", { fontFamily: "DM Mono, monospace", fontSize: "10px", color: "#68412d", fontStyle: "bold" }).setOrigin(0.5, 0);
    this.sources = PANTRY_LAYOUT.map(({ id, position }) => this.drawSource(id, position));
    this.stations = [
      ...ISLAND_COUNTERS.map(({ id, position }) => this.drawStation(id, "counter", position, "STAGING", "□", 0x8391a6, 116)),
      this.drawStation("trash", "trash", TRASH_POS, "TRASH", "×", 0xc85f58, 70),
      this.drawStation("pickup", "pickup", SERVICE_PICKUP_POS, "SERVICE PICKUP", "↑", 0x7ed8ba, 100),
      this.drawStation("sink", "sink", DISH_SINK_POS, "DISH SINK", "≈", 0x72b7da, 100),
    ];
    this.configureApplianceStations();
    this.diningGraphics = this.add.graphics().setDepth(8);
    this.debugText = this.add.text(952, 88, "", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: "#ffdc74", backgroundColor: "#111821dd", padding: { x: 4, y: 3 } }).setDepth(40).setVisible(false);
  }

  private drawSource(id: IngredientId, position: Vec2): Source {
    const ingredient = INGREDIENTS[id];
    this.add.rectangle(position.x, position.y + 7, 94, 82, ART_PALETTE.shadow, 0.18).setDepth(3);
    this.add.rectangle(position.x, position.y, 94, 82, 0xffe7ba).setStrokeStyle(4, ART_PALETTE.ink, 1).setDepth(4);
    const food = this.add.container(position.x, position.y - 12).setDepth(6); populateFoodArt(this, food, ingredientItem(id)); food.setScale(1.25);
    this.add.text(position.x, position.y + 15, ingredient.displayName.toUpperCase(), { fontFamily: "DM Mono, monospace", fontSize: "8px", fontStyle: "bold", color: "#49352d" }).setOrigin(0.5).setDepth(6);
    const countText = this.add.text(position.x, position.y + 31, "STOCK 0", { fontFamily: "DM Mono, monospace", fontSize: "9px", fontStyle: "bold", color: "#79452f" }).setOrigin(0.5).setDepth(6);
    return { id, position, countText };
  }

  private drawStation(id: string, type: StationType, position: Vec2, label: string, icon: string, color: number, width = 108): Station {
    const background = this.add.rectangle(position.x, position.y + 5, width, 82, 0xf2d7aa).setStrokeStyle(4, ART_PALETTE.ink, 1).setDepth(4);
    const decor = this.add.graphics().setDepth(5); decor.fillStyle(ART_PALETTE.shadow, 0.18).fillRoundedRect(position.x - width / 2 + 5, position.y + 40, width - 10, 10, 4);
    if (type === "oven") { decor.fillStyle(0xc95e49).fillRoundedRect(position.x - 30, position.y - 28, 60, 53, 8); decor.lineStyle(3, ART_PALETTE.ink).strokeRoundedRect(position.x - 30, position.y - 28, 60, 53, 8); decor.fillStyle(0x492f2a).fillRoundedRect(position.x - 21, position.y - 13, 42, 27, 4); decor.fillStyle(0xf3a24f, 0.55).fillRoundedRect(position.x - 17, position.y - 9, 34, 19, 3); }
    else if (type === "fryer") { decor.fillStyle(ART_PALETTE.steel).fillRoundedRect(position.x - 29, position.y - 25, 58, 48, 7); decor.lineStyle(3, ART_PALETTE.ink).strokeRoundedRect(position.x - 29, position.y - 25, 58, 48, 7); decor.fillStyle(0xd99735).fillRoundedRect(position.x - 19, position.y - 15, 38, 24, 4); decor.lineStyle(2, 0xffe080).strokeCircle(position.x - 8, position.y - 4, 3).strokeCircle(position.x + 7, position.y - 8, 4); }
    else if (type === "sink") { decor.fillStyle(ART_PALETTE.steel).fillRoundedRect(position.x - 31, position.y - 27, 62, 48, 8); decor.lineStyle(3, ART_PALETTE.ink).strokeRoundedRect(position.x - 31, position.y - 27, 62, 48, 8); decor.fillStyle(0x72b7da).fillRoundedRect(position.x - 22, position.y - 17, 44, 27, 7); decor.lineStyle(4, ART_PALETTE.steelDark).arc(position.x, position.y - 19, 12, Math.PI, Math.PI * 2).lineBetween(position.x + 12, position.y - 19, position.x + 12, position.y - 7); }
    else if (type === "chop") { decor.fillStyle(0xbb7544).fillRoundedRect(position.x - 30, position.y - 23, 60, 45, 7); decor.lineStyle(3, ART_PALETTE.ink).strokeRoundedRect(position.x - 30, position.y - 23, 60, 45, 7); decor.lineStyle(5, 0xe6e9e4).lineBetween(position.x - 14, position.y + 10, position.x + 14, position.y - 12); }
    else if (type === "plate") { [0, 5, 10].forEach((offset) => { decor.fillStyle(0xfaf7e9).fillEllipse(position.x, position.y - 8 + offset, 46, 15); decor.lineStyle(2, ART_PALETTE.steelDark).strokeEllipse(position.x, position.y - 8 + offset, 46, 15); }); }
    else if (type === "assembly") { decor.fillStyle(0xfaf7e9).fillEllipse(position.x, position.y - 2, 52, 35); decor.lineStyle(3, ART_PALETTE.ink).strokeEllipse(position.x, position.y - 2, 52, 35); decor.fillStyle(0xe2a94c).fillCircle(position.x, position.y, 10); }
    else if (type === "trash") { decor.fillStyle(0x74564c).fillRoundedRect(position.x - 20, position.y - 23, 40, 48, 5); decor.lineStyle(3, ART_PALETTE.ink).strokeRoundedRect(position.x - 20, position.y - 23, 40, 48, 5).lineBetween(position.x - 25, position.y - 23, position.x + 25, position.y - 23); }
    else { decor.fillStyle(type === "pickup" ? 0xffefd0 : ART_PALETTE.woodLight).fillRoundedRect(position.x - 32, position.y - 20, 64, 40, 7); decor.lineStyle(3, ART_PALETTE.ink).strokeRoundedRect(position.x - 32, position.y - 20, 64, 40, 7); if (type === "pickup") decor.lineStyle(4, 0x6fa447).lineBetween(position.x, position.y + 9, position.x, position.y - 10).lineBetween(position.x, position.y - 10, position.x - 7, position.y - 2).lineBetween(position.x, position.y - 10, position.x + 7, position.y - 2); }
    const iconText = this.add.text(position.x, position.y - 20, type === "counter" ? icon : "", { fontFamily: "Nunito, sans-serif", fontSize: "20px", fontStyle: "bold", color: `#${ART_PALETTE.ink.toString(16)}` }).setOrigin(0.5).setDepth(5);
    const labelText = this.add.text(position.x, position.y + 24, label, { fontFamily: "DM Mono, monospace", fontSize: "8px", fontStyle: "bold", color: "#49352d" }).setOrigin(0.5).setDepth(6);
    const statusText = this.add.text(position.x, position.y + 36, "", { fontFamily: "DM Mono, monospace", fontSize: "7px", color: "#713c2d", fontStyle: "bold" }).setOrigin(0.5).setDepth(6);
    const itemVisual = this.add.container(position.x, position.y - 4).setDepth(14);
    const progressBg = this.add.rectangle(position.x - width / 2 + 7, position.y + 40, width - 14, 5, 0x11151c).setOrigin(0, 0.5).setDepth(15).setVisible(false);
    const progressFill = this.add.rectangle(position.x - width / 2 + 7, position.y + 40, width - 14, 5, 0x7ed8ba).setOrigin(0, 0.5).setDepth(16).setVisible(false);
    return { id, type, position, item: null, itemVisual, statusText, progressBg, progressFill, processStartedAt: 0, processDuration: 0, background, baseColor: color, highlighted: false, visuals: [background, decor, iconText, labelText, statusText, itemVisual, progressBg, progressFill] };
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
    const box = this.add.rectangle(position.x, position.y, 108, 82, 0xffe7ba, 0.35).setStrokeStyle(3, ART_PALETTE.ink, 0.35).setDepth(3);
    const text = this.add.text(position.x, position.y, label, { fontFamily: "DM Mono, monospace", fontSize: "8px", fontStyle: "bold", color: `#${color.toString(16).padStart(6, "0")}`, align: "center" }).setOrigin(0.5).setDepth(4);
    this.slotMarkers.push(box, text);
  }

  private makePlayer(index: 0 | 1): Player {
    const color = index === 0 ? 0x4595c6 : 0xdc625b; const heldVisual = this.add.container(0, -49).setVisible(false);
    const start = OPEN_KITCHEN_PLAYER_STARTS[index];
    const body = this.add.container(start.x, start.y, characterParts(this, color, "chef"));
    body.add(heldVisual); body.setDepth(20);
    body.add(this.add.text(0, 40, `P${index + 1}`, { fontFamily: "Nunito, sans-serif", fontSize: "11px", fontStyle: "bold", color: "#49352d", backgroundColor: "#fff1cfdd", padding: { x: 5, y: 2 } }).setOrigin(0.5));
    const inputBadge = this.add.text(0, 55, "TOUCH / KEYS", { fontFamily: "DM Mono, monospace", fontSize: "6px", color: "#49352d", backgroundColor: "#fff1cfcc", padding: { x: 4, y: 2 } }).setOrigin(0.5); body.add(inputBadge);
    return { position: { ...start }, facing: { x: index === 0 ? 1 : -1, y: 0 }, held: null, carryingDirtyPlate: false, body, heldVisual, inputBadge };
  }

  private setStationItem(station: Station, item: KitchenItem | null): void {
    station.item = item; station.processStartedAt = 0; station.processDuration = 0; station.statusText.setText(""); station.progressBg.setVisible(false); station.progressFill.setVisible(false); this.refreshStationVisual(station);
  }
  private refreshStationVisual(station: Station): void { station.itemVisual.removeAll(true); station.itemVisual.setVisible(Boolean(station.item)); if (station.item) this.populateItemVisual(station.itemVisual, station.item); }
  private makeItemVisual(x: number, y: number, item: KitchenItem): Phaser.GameObjects.Container { const container = this.add.container(x, y); this.populateItemVisual(container, item); return container.setDepth(14); }

  private populateItemVisual(container: Phaser.GameObjects.Container, item: KitchenItem): void {
    populateFoodArt(this, container, item);
  }

  private updateHeld(player: Player): void {
    player.heldVisual.removeAll(true);
    if (player.carryingDirtyPlate) { const plate = this.add.graphics(); plate.fillStyle(0xfaf7e9).fillCircle(0, 1, 18); plate.lineStyle(3, ART_PALETTE.ink).strokeCircle(0, 1, 18); plate.fillStyle(0xb65e45).fillCircle(-4, 0, 4).fillCircle(5, -4, 3); player.heldVisual.add(plate); player.heldVisual.setVisible(true).setDepth(30); return; }
    if (!player.held) { player.heldVisual.setVisible(false); return; } this.populateItemVisual(player.heldVisual, player.held); player.heldVisual.setVisible(true).setDepth(30);
  }
  private updateInteractionHighlights(kitchenActive: boolean): void {
    const active = new Set<Station>();
    if (kitchenActive) this.players.forEach((player, index) => {
      if (!this.playerSession.isActive(index as 0 | 1)) return;
      const station = this.nearestStation(player.position); if (station) active.add(station);
    });
    this.stations.forEach((station) => {
      const highlighted = active.has(station); if (station.highlighted === highlighted) return;
      station.highlighted = highlighted;
      station.background.setFillStyle(highlighted ? 0xfff4bd : 0xf2d7aa, 1).setStrokeStyle(highlighted ? 7 : 4, highlighted ? 0xffd33d : ART_PALETTE.ink, 1);
    });
  }
  private nearestStation(position: Vec2): Station | null { return this.stations.map((station) => ({ station, range: distance(position, station.position) })).filter(({ range }) => range <= INTERACT_DISTANCE).sort((a, b) => a.range - b.range)[0]?.station ?? null; }
  private nearestSource(position: Vec2): Source | null { return this.sources.map((source) => ({ source, range: distance(position, source.position) })).filter(({ range }) => range <= INTERACT_DISTANCE).sort((a, b) => a.range - b.range)[0]?.source ?? null; }
  private nearestTable(position: Vec2): DiningTable | null { return this.restaurant.diningTables.map((table) => ({ table, range: distance(position, table) })).filter(({ range }) => range <= INTERACT_DISTANCE).sort((a, b) => a.range - b.range)[0]?.table ?? null; }
  private updateSourceCounts(): void { this.sources?.forEach((source) => source.countText.setText(`STOCK ${this.restaurant.inventory[source.id]}`)); }
  private isThrowable(item: KitchenItem): boolean { return item.kind === "ingredient" && item.state !== "ruined" && INGREDIENTS[item.ingredientId].throwable; }
  private isCheeseBakeComponent(item: KitchenItem): boolean { return item.kind === "ingredient" && ((item.ingredientId === "potato" && item.state === "chopped") || (item.ingredientId === "cheese" && item.state === "raw")); }
  private matchesCheeseBake(a: KitchenItem, b: KitchenItem): boolean { return this.isCheeseBakeComponent(a) && this.isCheeseBakeComponent(b) && a.kind === "ingredient" && b.kind === "ingredient" && a.ingredientId !== b.ingredientId; }
  private isGardenComponent(item: KitchenItem): boolean { return item.kind === "ingredient" && item.state === "chopped" && (item.ingredientId === "tomato" || item.ingredientId === "lettuce"); }
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
    graphics.fillStyle(ART_PALETTE.wood, 0.08).fillRoundedRect(958, 92, 272, 432, 8);
    graphics.fillStyle(ART_PALETTE.ink).fillRect(1221, 250, 11, 86); graphics.fillStyle(0x74a9cb).fillRoundedRect(1223, 258, 7, 69, 3);
    this.diningLabels.push(this.add.text(1216, 293, "ENTRANCE", { fontFamily: "DM Mono, monospace", fontSize: "7px", fontStyle: "bold", color: "#49352d" }).setOrigin(0.5).setAngle(-90).setDepth(12));
    graphics.fillStyle(ART_PALETTE.woodDark).fillRoundedRect(946, 418, 42, 72, 7); graphics.lineStyle(3, ART_PALETTE.ink).strokeRoundedRect(946, 418, 42, 72, 7);
    this.diningLabels.push(this.add.text(DIRTY_RETURN_POS.x, DIRTY_RETURN_POS.y, `DIRTY\n${this.restaurant.dirtyReturnQueue}`, { fontFamily: "DM Mono, monospace", fontSize: "7px", fontStyle: "bold", align: "center", color: "#fff1cf" }).setOrigin(0.5).setDepth(12));
    [[980, 105], [1210, 505]].forEach(([x, y]) => { graphics.fillStyle(0x8a5536).fillRoundedRect(x - 12, y, 24, 20, 5); graphics.fillStyle(0x5c963e).fillCircle(x - 8, y, 11).fillCircle(x + 8, y - 3, 12).fillCircle(x, y - 10, 13); graphics.lineStyle(2, ART_PALETTE.ink).strokeRoundedRect(x - 12, y, 24, 20, 5); });
    this.restaurant.diningTables.forEach((table) => {
      const colors: Record<string, number> = { clean: ART_PALETTE.woodLight, reserved: 0xd9a24c, waiting_food: 0xe29a56, eating: 0xa87144, dirty: 0x9b5b4d };
      const highlighted = this.players.some((player, index) => this.playerSession.isActive(index as 0 | 1) && distance(player.position, table) <= INTERACT_DISTANCE);
      graphics.fillStyle(0x12171c, 0.35).fillEllipse(table.x, table.y + 12, 82, 30);
      graphics.fillStyle(colors[table.state] ?? 0x5b6672).fillRoundedRect(table.x - 34, table.y - 22, 68, 44, 8);
      graphics.lineStyle(highlighted ? 7 : 3, highlighted ? 0xffd33d : ART_PALETTE.ink, 0.95).strokeRoundedRect(table.x - 34, table.y - 22, 68, 44, 8);
      graphics.fillStyle(ART_PALETTE.woodDark).fillRoundedRect(table.x - 57, table.y - 14, 20, 28, 7).fillRoundedRect(table.x + 37, table.y - 14, 20, 28, 7);
      graphics.lineStyle(2, ART_PALETTE.ink).strokeRoundedRect(table.x - 57, table.y - 14, 20, 28, 7).strokeRoundedRect(table.x + 37, table.y - 14, 20, 28, 7);
      if (table.state === "dirty") { graphics.fillStyle(0xfaf7e9).fillCircle(table.x, table.y, 10); graphics.lineStyle(2, ART_PALETTE.ink).strokeCircle(table.x, table.y, 10); graphics.fillStyle(0xb65e45).fillCircle(table.x - 2, table.y, 3).fillCircle(table.x + 5, table.y - 3, 2); }
      this.diningLabels.push(this.add.text(table.x, table.y + 31, `${table.id.toUpperCase()} · ${table.state.replaceAll("_", " ").toUpperCase()}`, { fontFamily: "DM Mono, monospace", fontSize: "6px", fontStyle: "bold", color: "#49352d", backgroundColor: "#fff1cfbb", padding: { x: 3, y: 1 } }).setOrigin(0.5).setDepth(12));
    });
    this.restaurant.customers.filter(({ state }) => state !== "failed").forEach((customer) => {
      const table = this.restaurant.diningTables.find(({ id }) => id === customer.tableId);
      const waitingIndex = this.restaurant.customers.filter(({ state }) => state === "arriving" || state === "waiting_for_table").findIndex(({ id }) => id === customer.id);
      const seatingTask = this.restaurant.activeStaff.find(({ task }) => task?.type === "seat" && task.targetId === String(customer.id))?.task;
      const walkProgress = seatingTask ? Phaser.Math.Clamp((now - seatingTask.startedAt) / (seatingTask.endsAt - seatingTask.startedAt), 0, 1) : 1;
      const x = table ? Phaser.Math.Linear(customer.state === "walking_to_table" ? ENTRANCE_POS.x : table.x, table.x, walkProgress) : ENTRANCE_POS.x - (waitingIndex % 2) * 24;
      const y = table ? Phaser.Math.Linear(customer.state === "walking_to_table" ? 305 : table.y - 8, table.y - 8, walkProgress) : 365 + Math.floor(Math.max(0, waitingIndex) / 2) * 30;
      for (let member = 0; member < customer.size; member++) { const offset = customer.size === 2 ? (member ? 16 : -16) : 0; const mood = customer.failureReason ? "unhappy" : customer.state === "leaving" ? "happy" : "neutral"; drawMiniCharacter(graphics, x + offset, y, [0x71a7c7, 0xe18379, 0xe2b64c, 0x75a65a][(customer.id + member) % 4], "customer", customer.id + member, mood); }
      this.diningLabels.push(this.add.text(x, y + 24, customer.state === "waiting_for_food" ? RECIPES[customer.recipeId].icon : customer.state.replaceAll("_", " "), { fontFamily: "DM Mono, monospace", fontSize: "6px", fontStyle: "bold", color: "#49352d", backgroundColor: "#fff1cfcc", padding: { x: 2, y: 1 } }).setOrigin(0.5).setDepth(12));
    });
    this.restaurant.activeStaff.forEach((staff, index) => {
      let x = staff.role === "dishwasher" ? DISH_SINK_POS.x + 20 : 992 + index * 22; let y = staff.role === "dishwasher" ? DISH_SINK_POS.y - 30 : 510;
      const tableId = staff.task?.destination.match(/Table (\d+)/)?.[1]; const table = tableId ? this.restaurant.diningTables.find(({ id }) => id === `t${tableId}`) : undefined;
      if (staff.task && table) {
        const progress = Phaser.Math.Clamp((now - staff.task.startedAt) / (staff.task.endsAt - staff.task.startedAt), 0, 1);
        const start = staff.task.type === "deliver" ? SERVER_STAGING_POS : ENTRANCE_POS;
        x = Phaser.Math.Linear(start.x, table.x, progress); y = Phaser.Math.Linear(start.y, table.y + 38, progress);
      } else if (staff.task?.type === "clear") {
        const dirtyTable = this.restaurant.diningTables.find(({ id }) => id === staff.task?.targetId); const progress = Phaser.Math.Clamp((now - staff.task.startedAt) / (staff.task.endsAt - staff.task.startedAt), 0, 1);
        if (dirtyTable) { x = Phaser.Math.Linear(dirtyTable.x, DIRTY_RETURN_POS.x, progress); y = Phaser.Math.Linear(dirtyTable.y + 38, DIRTY_RETURN_POS.y, progress); }
      }
      drawMiniCharacter(graphics, x, y, staff.role === "server" ? 0x7f6bb3 : 0x4f9eb5, staff.role, index + 1);
      this.diningLabels.push(this.add.text(x, y + 25, `${staff.name} · ${staff.state}`, { fontFamily: "DM Mono, monospace", fontSize: "6px", fontStyle: "bold", color: "#49352d", backgroundColor: "#fff1cfcc", padding: { x: 2, y: 1 } }).setOrigin(0.5).setDepth(12));
    });
    if (this.aiDebug) this.debugText.setText(this.restaurant.activeStaff.map((staff) => `${staff.name}: ${staff.state}\n task=${staff.task?.type ?? "none"} target=${staff.task?.targetId ?? "-"}\n dest=${staff.task?.destination ?? "idle"}`).join("\n"));
  }
  private callout(message: string, color: string): void {
    this.calloutTimer?.remove(false); this.calloutText.setText(message).setColor(color).setVisible(true).setAlpha(1).setScale(0.92);
    this.tweens.add({ targets: this.calloutText, scale: 1, duration: 100, ease: "Back.Out" });
    this.calloutTimer = this.time.delayedCall(1350, () => this.tweens.add({ targets: this.calloutText, alpha: 0, duration: 220, onComplete: () => this.calloutText.setVisible(false) }));
  }
  private syncPlayerMode(): void {
    document.body.dataset.playerMode = this.playerSession.mode;
    this.players?.forEach((player, index) => player.body.setVisible(this.playerSession.isActive(index as 0 | 1)));
    this.ui?.render();
  }
  private debugSetPlayer(index: 0 | 1, x: number, y: number, held?: KitchenItem | null): void { const player = this.players[index]; player.position = { x: Math.max(48, Math.min(1220, x)), y: Math.max(88, Math.min(548, y)) }; player.body.setPosition(player.position.x, player.position.y); if (held !== undefined) { player.held = held; player.carryingDirtyPlate = false; this.updateHeld(player); } }
  private snapshot(): object {
    return { phase: this.restaurant.phase, day: this.restaurant.day, cashCents: this.restaurant.cashCents, revenueCents: this.restaurant.revenueCents, spendingCents: this.restaurant.ingredientSpendingCents, wasteCents: this.restaurant.wastedValueCents, inventory: { ...this.restaurant.inventory }, installedSlots: [...this.restaurant.installedSlots], plates: this.restaurant.platesRemaining,
      playerMode: this.playerSession.mode, activePlayerCount: this.playerSession.activePlayerCount, players: this.players.map((player, index) => ({ active: this.playerSession.isActive(index as 0 | 1), x: Math.round(player.position.x), y: Math.round(player.position.y), held: player.carryingDirtyPlate ? { kind: "dirty-plate" } : player.held })), stations: Object.fromEntries(this.stations.map((station) => [station.id, station.item])),
      lastCall: this.restaurant.lastCall, orders: this.restaurant.activeOrders.map((order) => ({ id: order.id, recipeId: order.recipeId, tableId: order.tableId, customerId: order.customerId })), readyDishes: this.restaurant.readyDishes.map((dish) => ({ ...dish })), customers: this.restaurant.customers.map((customer) => ({ ...customer })), tables: this.restaurant.diningTables.map((table) => ({ ...table })), staff: this.restaurant.activeStaff.map((staff) => ({ ...staff })), dirtyReturn: this.restaurant.dirtyReturnQueue, dirtyInTransit: this.restaurant.dirtyPlatesInTransit, inFlight: this.flight?.item ?? null, messCount: this.ruinedItems.length,
      highlightedStations: this.stations.filter((station) => station.highlighted).map((station) => station.id), summary: this.restaurant.summary() };
  }
  private syncAccessibleStatus(): void { const output = document.getElementById("game-status"); if (output) output.textContent = JSON.stringify(this.snapshot()); }
}
