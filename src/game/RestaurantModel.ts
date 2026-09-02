import {
  ADVERTISING, APPLIANCES, APPLIANCE_IDS, DINING_EXPANSION, INGREDIENT_IDS, INGREDIENTS,
  KITCHEN_EXPANSION, ORDER_PATIENCE_MS, RECIPES, REPUTATION_LEVELS, SAVE_KEY, SAVE_VERSION,
  SERVICE_DURATION_MS, STARTING_CASH_CENTS, STARTING_REPUTATION_POINTS, bulkQuote,
  type AdId, type ApplianceId, type IngredientId, type RecipeId,
} from "./data";

export type RestaurantPhase = "landing" | "planning" | "prep" | "service" | "summary";
export interface StorageLike { getItem(key: string): string | null; setItem(key: string, value: string): void; removeItem(key: string): void }
export interface OrderTicket { id: number; recipeId: RecipeId; createdAt: number; expiresAt: number }
export type ServiceEvent = { type: "order-arrived"; order: OrderTicket } | { type: "order-expired"; order: OrderTicket } | { type: "service-ended" };
export interface DemandPreview { baseline: number; adBonus: number; potential: number; capacity: number; admitted: number; turnedAway: number }
export interface ReputationProgress { level: number; points: number; levelStart: number; nextLevelAt: number | null; percent: number }
export interface NightSummary {
  day: number; potentialCustomers: number; admittedCustomers: number; customersTurnedAway: number;
  ordersCompleted: number; ordersMissed: number; startingCashCents: number; ingredientSpendingCents: number;
  advertisingSpendingCents: number; capitalSpendingCents: number; revenueCents: number;
  wastedIngredientValueCents: number; endingCashCents: number; remainingInventory: Record<IngredientId, number>;
  remainingInventoryValueCents: number; startingReputation: ReputationProgress; reputationChange: number;
  endingReputation: ReputationProgress;
}
export interface EndlessSave {
  version: number; day: number; cashCents: number; inventory: Record<IngredientId, number>;
  applianceOwnership: Record<ApplianceId, number>; installedSlots: Array<ApplianceId | null>;
  kitchenLevel: number; diningLevel: number; reputationPoints: number; selectedRecipeIds: RecipeId[];
  selectedAdId: AdId; startingCashCents: number; ingredientSpendingCents: number;
  advertisingSpendingCents: number; capitalSpendingCents: number;
}

const STARTING_INSTALLED: Array<ApplianceId | null> = ["oven", "prep-station", "assembly-station", "plating-station", null, null];

export class RestaurantModel {
  readonly serviceDurationMs: number;
  readonly inventory: Record<IngredientId, number> = { potato: 0, tomato: 0, onion: 0, cheese: 0 };
  readonly applianceOwnership: Record<ApplianceId, number> = { "prep-station": 1, oven: 1, "assembly-station": 1, "plating-station": 1, fryer: 0 };
  phase: RestaurantPhase;
  hasSave = false;
  day = 1;
  cashCents = STARTING_CASH_CENTS;
  kitchenLevel = 1;
  diningLevel = 1;
  reputationPoints = STARTING_REPUTATION_POINTS;
  installedSlots: Array<ApplianceId | null> = [...STARTING_INSTALLED];
  selectedRecipeIds: RecipeId[] = [];
  selectedAdId: AdId = "none";
  startingCashCents = STARTING_CASH_CENTS;
  startingReputationPoints = STARTING_REPUTATION_POINTS;
  ingredientSpendingCents = 0;
  advertisingSpendingCents = 0;
  capitalSpendingCents = 0;
  revenueCents = 0;
  wastedValueCents = 0;
  ordersCompleted = 0;
  ordersMissed = 0;
  activeOrders: OrderTicket[] = [];
  potentialCustomers = 0;
  admittedCustomers = 0;
  customersTurnedAway = 0;
  ordersGenerated = 0;
  reputationChange = 0;
  serviceStartedAt = 0;
  nextOrderAt = 0;
  platesRemaining = 4;
  lastFeedback = "Start a new restaurant or continue your saved one.";
  private nextOrderId = 1;
  private randomState: number;
  private readonly initialSeed: number;

  constructor(private readonly options: { serviceDurationMs?: number; seed?: number; storage?: StorageLike; startAtLanding?: boolean } = {}) {
    this.serviceDurationMs = options.serviceDurationMs ?? SERVICE_DURATION_MS;
    this.initialSeed = options.seed ?? 7283; this.randomState = this.initialSeed;
    this.hasSave = this.load();
    this.phase = options.startAtLanding ? "landing" : "planning";
    if (!this.hasSave) this.applyCleanState();
  }

  newRestaurant(): void { this.applyCleanState(); this.phase = "planning"; this.hasSave = true; this.lastFeedback = "Day 1 planning is open."; this.save(); }
  continueRestaurant(): boolean { if (!this.hasSave) return false; this.phase = "planning"; this.lastFeedback = `Welcome back to Day ${this.day}.`; return true; }
  resetEndlessSave(confirmed: boolean): boolean {
    if (!confirmed) return false; this.options.storage?.removeItem(SAVE_KEY); this.hasSave = false; this.applyCleanState(); this.phase = "landing";
    this.lastFeedback = "Endless save reset."; return true;
  }

  toggleRecipe(recipeId: RecipeId): boolean {
    if (this.phase !== "planning") return false;
    const index = this.selectedRecipeIds.indexOf(recipeId);
    if (index >= 0) this.selectedRecipeIds.splice(index, 1);
    else {
      const missing = this.missingAppliances(recipeId);
      if (missing.length) { this.lastFeedback = `${RECIPES[recipeId].displayName} requires installed ${missing.map((id) => APPLIANCES[id].displayName).join(" + ")}.`; return false; }
      if (this.selectedRecipeIds.length >= 2) { this.lastFeedback = "Choose two dishes — deselect one first."; return false; }
      this.selectedRecipeIds.push(recipeId);
    }
    this.lastFeedback = `${this.selectedRecipeIds.length}/2 dishes selected for Day ${this.day}.`; this.save(); return true;
  }

  missingAppliances(recipeId: RecipeId): ApplianceId[] { return RECIPES[recipeId].requiredAppliances.filter((id) => !this.isApplianceInstalled(id)); }
  isRecipeAvailable(recipeId: RecipeId): boolean { return this.missingAppliances(recipeId).length === 0; }
  isApplianceInstalled(id: ApplianceId): boolean { return this.installedSlots.slice(0, this.kitchenSlotCapacity).includes(id); }
  get kitchenSlotCapacity(): number { return this.kitchenLevel >= 2 ? KITCHEN_EXPANSION.toSlots : KITCHEN_EXPANSION.fromSlots; }
  get diningCapacity(): number { return this.diningLevel >= 2 ? DINING_EXPANSION.toCapacity : DINING_EXPANSION.fromCapacity; }
  installedCount(id: ApplianceId): number { return this.installedSlots.slice(0, this.kitchenSlotCapacity).filter((installed) => installed === id).length; }
  storedCount(id: ApplianceId): number { return this.applianceOwnership[id] - this.installedCount(id); }

  purchaseIngredients(ingredientId: IngredientId, quantity: number): boolean {
    if (this.phase !== "planning" || !Number.isInteger(quantity) || quantity <= 0) return false;
    const quote = bulkQuote(ingredientId, quantity);
    if (this.cashCents < quote.totalCents) { this.lastFeedback = `Not enough cash for ${quantity} ${INGREDIENTS[ingredientId].displayName}.`; return false; }
    this.cashCents -= quote.totalCents; this.ingredientSpendingCents += quote.totalCents; this.inventory[ingredientId] += quantity;
    this.lastFeedback = `Bought ${quantity} ${INGREDIENTS[ingredientId].displayName} · ${quote.tier.label}.`; this.save(); return true;
  }

  purchaseAppliance(id: ApplianceId): boolean {
    if (this.phase !== "planning" || APPLIANCES[id].priceCents <= 0) return false;
    const price = APPLIANCES[id].priceCents; if (this.cashCents < price) { this.lastFeedback = `Not enough cash for ${APPLIANCES[id].displayName}.`; return false; }
    this.cashCents -= price; this.capitalSpendingCents += price; this.applianceOwnership[id] += 1;
    this.lastFeedback = `${APPLIANCES[id].displayName} purchased and placed in storage.`; this.save(); return true;
  }
  installAppliance(id: ApplianceId, slotIndex: number): boolean {
    if (this.phase !== "planning" || slotIndex < 0 || slotIndex >= this.kitchenSlotCapacity || this.installedSlots[slotIndex] !== null || this.storedCount(id) <= 0) return false;
    this.installedSlots[slotIndex] = id; this.lastFeedback = `${APPLIANCES[id].displayName} installed in Slot ${slotIndex + 1}.`; this.revalidateMenu(); this.save(); return true;
  }
  removeAppliance(slotIndex: number): boolean {
    if (this.phase !== "planning" || slotIndex < 0 || slotIndex >= this.kitchenSlotCapacity || !this.installedSlots[slotIndex]) return false;
    const id = this.installedSlots[slotIndex]!; this.installedSlots[slotIndex] = null; this.lastFeedback = `${APPLIANCES[id].displayName} moved to storage.`; this.revalidateMenu(); this.save(); return true;
  }
  moveInstalledAppliance(fromIndex: number, toIndex: number): boolean {
    if (this.phase !== "planning" || fromIndex < 0 || toIndex < 0 || fromIndex >= this.kitchenSlotCapacity || toIndex >= this.kitchenSlotCapacity || fromIndex === toIndex || !this.installedSlots[fromIndex]) return false;
    [this.installedSlots[fromIndex], this.installedSlots[toIndex]] = [this.installedSlots[toIndex], this.installedSlots[fromIndex]];
    this.lastFeedback = `Swapped Slots ${fromIndex + 1} and ${toIndex + 1}.`; this.save(); return true;
  }
  buyKitchenExpansion(): boolean { return this.buyExpansion("kitchen"); }
  buyDiningExpansion(): boolean { return this.buyExpansion("dining"); }

  selectAdvertising(id: AdId): boolean {
    if (this.phase !== "planning") return false;
    const oldCost = ADVERTISING[this.selectedAdId].costCents; const newCost = ADVERTISING[id].costCents; const difference = newCost - oldCost;
    if (difference > this.cashCents) { this.lastFeedback = `Not enough cash for ${ADVERTISING[id].displayName}.`; return false; }
    this.cashCents -= difference; this.advertisingSpendingCents += difference; this.selectedAdId = id;
    this.lastFeedback = `${ADVERTISING[id].displayName} selected for Day ${this.day}.`; this.save(); return true;
  }

  demandPreview(): DemandPreview {
    const baseline = this.reputationLevel().baselineDemand; const modifier = ADVERTISING[this.selectedAdId].demandBonusBps;
    const potential = Math.ceil(baseline * (10_000 + modifier) / 10_000); const capacity = this.diningCapacity; const admitted = Math.min(potential, capacity);
    return { baseline, adBonus: potential - baseline, potential, capacity, admitted, turnedAway: Math.max(0, potential - capacity) };
  }
  reputationLevel(points = this.reputationPoints) { return [...REPUTATION_LEVELS].reverse().find((entry) => points >= entry.minimumPoints) ?? REPUTATION_LEVELS[0]; }
  reputationProgress(points = this.reputationPoints): ReputationProgress {
    const current = this.reputationLevel(points); const next = REPUTATION_LEVELS.find((entry) => entry.level === current.level + 1);
    const percent = next ? Math.round((points - current.minimumPoints) / (next.minimumPoints - current.minimumPoints) * 100) : 100;
    return { level: current.level, points, levelStart: current.minimumPoints, nextLevelAt: next?.minimumPoints ?? null, percent: Math.max(0, Math.min(100, percent)) };
  }

  beginPrep(): boolean {
    if (this.phase !== "planning" || this.selectedRecipeIds.length !== 2) { this.lastFeedback = "Choose exactly two available dishes before Prep."; return false; }
    if (this.selectedRecipeIds.some((id) => !this.isRecipeAvailable(id))) { this.revalidateMenu(); this.lastFeedback = "Install every required appliance before Prep."; return false; }
    const demand = this.demandPreview(); this.potentialCustomers = demand.potential; this.admittedCustomers = demand.admitted; this.customersTurnedAway = demand.turnedAway;
    this.phase = "prep"; this.lastFeedback = `CLOSED · Prep for ${demand.admitted} admitted customers.`; this.save(); return true;
  }
  takeIngredient(id: IngredientId): boolean { if ((this.phase !== "prep" && this.phase !== "service") || this.inventory[id] <= 0) return false; this.inventory[id] -= 1; return true; }
  recordWaste(valueCents: number): void { this.wastedValueCents += valueCents; }

  startService(now: number): ServiceEvent[] {
    if (this.phase !== "prep") return [];
    this.phase = "service"; this.serviceStartedAt = now; this.ordersGenerated = 0; this.activeOrders = [];
    this.nextOrderAt = now + this.orderIntervalMs(); this.lastFeedback = "OPEN · First order is in!";
    if (this.admittedCustomers <= 0) return [];
    const order = this.createOrder(now); return [{ type: "order-arrived", order }];
  }
  updateService(now: number): ServiceEvent[] {
    if (this.phase !== "service") return [];
    const events: ServiceEvent[] = [];
    const expired = this.activeOrders.filter((order) => order.expiresAt <= now);
    if (expired.length) {
      const ids = new Set(expired.map(({ id }) => id)); this.activeOrders = this.activeOrders.filter((order) => !ids.has(order.id)); this.ordersMissed += expired.length;
      expired.forEach((order) => events.push({ type: "order-expired", order })); this.lastFeedback = `${RECIPES[expired.at(-1)!.recipeId].displayName} expired — $0 earned.`;
    }
    if (now - this.serviceStartedAt >= this.serviceDurationMs) { return [...events, ...this.finishService()]; }
    if (now >= this.nextOrderAt && this.ordersGenerated < this.admittedCustomers && this.activeOrders.length < 3) {
      const order = this.createOrder(now); events.push({ type: "order-arrived", order }); this.nextOrderAt = now + this.orderIntervalMs();
    }
    return events;
  }
  serveDish(recipeId: RecipeId): boolean {
    if (this.phase !== "service") return false; const index = this.activeOrders.findIndex((order) => order.recipeId === recipeId);
    if (index < 0) { this.lastFeedback = `No active ${RECIPES[recipeId].displayName} order — serve refused.`; return false; }
    this.activeOrders.splice(index, 1); const price = RECIPES[recipeId].sellingPriceCents; this.cashCents += price; this.revenueCents += price; this.ordersCompleted += 1;
    this.lastFeedback = `${RECIPES[recipeId].displayName} served · +$${(price / 100).toFixed(0)}`; return true;
  }
  forceOrder(recipeId: RecipeId, now: number, patienceMs = ORDER_PATIENCE_MS): OrderTicket | null {
    if (this.phase !== "service" || !this.selectedRecipeIds.includes(recipeId) || this.activeOrders.length >= 3 || this.ordersGenerated >= this.admittedCustomers) return null;
    return this.createOrder(now, recipeId, patienceMs);
  }
  endService(_now: number): ServiceEvent[] { return this.phase === "service" ? this.finishService() : []; }

  summary(): NightSummary {
    const remainingInventory = { ...this.inventory }; const remainingInventoryValueCents = INGREDIENT_IDS.reduce((total, id) => total + this.inventory[id] * INGREDIENTS[id].purchaseCostCents, 0);
    return {
      day: this.day, potentialCustomers: this.potentialCustomers, admittedCustomers: this.admittedCustomers, customersTurnedAway: this.customersTurnedAway,
      ordersCompleted: this.ordersCompleted, ordersMissed: this.ordersMissed, startingCashCents: this.startingCashCents,
      ingredientSpendingCents: this.ingredientSpendingCents, advertisingSpendingCents: this.advertisingSpendingCents,
      capitalSpendingCents: this.capitalSpendingCents, revenueCents: this.revenueCents, wastedIngredientValueCents: this.wastedValueCents,
      endingCashCents: this.cashCents, remainingInventory, remainingInventoryValueCents,
      startingReputation: this.reputationProgress(this.startingReputationPoints), reputationChange: this.reputationChange,
      endingReputation: this.reputationProgress(),
    };
  }
  nextDay(): boolean {
    if (this.phase !== "summary") return false; this.day += 1; this.phase = "planning"; this.selectedRecipeIds = []; this.selectedAdId = "none";
    this.resetDailyAccounting(); this.lastFeedback = `Day ${this.day} planning is open. Leftovers are waiting in the pantry.`; this.save(); return true;
  }

  restartNight(): boolean {
    if (this.phase !== "prep" && this.phase !== "service") return false;
    if (!this.load()) return false; this.phase = "planning"; this.activeOrders = []; this.serviceStartedAt = 0;
    this.lastFeedback = `Day ${this.day} restored to the start of Prep planning.`; return true;
  }

  save(): void { if (!this.options.storage || !this.hasSave) return; this.options.storage.setItem(SAVE_KEY, JSON.stringify(this.toSave())); }
  private load(): boolean {
    const raw = this.options.storage?.getItem(SAVE_KEY); if (!raw) return false;
    try { const saved = JSON.parse(raw) as EndlessSave; if (saved.version !== SAVE_VERSION) return false; this.applySave(saved); return true; } catch { return false; }
  }
  private toSave(): EndlessSave {
    return { version: SAVE_VERSION, day: this.day, cashCents: this.cashCents, inventory: { ...this.inventory }, applianceOwnership: { ...this.applianceOwnership }, installedSlots: [...this.installedSlots], kitchenLevel: this.kitchenLevel, diningLevel: this.diningLevel, reputationPoints: this.reputationPoints, selectedRecipeIds: [...this.selectedRecipeIds], selectedAdId: this.selectedAdId, startingCashCents: this.startingCashCents, ingredientSpendingCents: this.ingredientSpendingCents, advertisingSpendingCents: this.advertisingSpendingCents, capitalSpendingCents: this.capitalSpendingCents };
  }
  private applySave(saved: EndlessSave): void {
    this.day = saved.day; this.cashCents = saved.cashCents; INGREDIENT_IDS.forEach((id) => { this.inventory[id] = saved.inventory[id] ?? 0; });
    APPLIANCE_IDS.forEach((id) => { this.applianceOwnership[id] = saved.applianceOwnership[id] ?? APPLIANCES[id].startingOwned; });
    this.installedSlots = [...saved.installedSlots.slice(0, 6)]; while (this.installedSlots.length < 6) this.installedSlots.push(null);
    this.kitchenLevel = saved.kitchenLevel; this.diningLevel = saved.diningLevel; this.reputationPoints = saved.reputationPoints;
    this.selectedRecipeIds = saved.selectedRecipeIds.filter((id) => RECIPES[id]); this.selectedAdId = ADVERTISING[saved.selectedAdId] ? saved.selectedAdId : "none";
    this.startingCashCents = saved.startingCashCents ?? saved.cashCents; this.ingredientSpendingCents = saved.ingredientSpendingCents ?? 0;
    this.advertisingSpendingCents = saved.advertisingSpendingCents ?? 0; this.capitalSpendingCents = saved.capitalSpendingCents ?? 0;
    this.startingReputationPoints = this.reputationPoints;
  }
  private applyCleanState(): void {
    this.day = 1; this.cashCents = STARTING_CASH_CENTS; this.kitchenLevel = 1; this.diningLevel = 1; this.reputationPoints = STARTING_REPUTATION_POINTS;
    INGREDIENT_IDS.forEach((id) => { this.inventory[id] = 0; }); APPLIANCE_IDS.forEach((id) => { this.applianceOwnership[id] = APPLIANCES[id].startingOwned; });
    this.installedSlots = [...STARTING_INSTALLED]; this.selectedRecipeIds = []; this.selectedAdId = "none"; this.resetDailyAccounting(); this.randomState = this.initialSeed;
  }
  private resetDailyAccounting(): void {
    this.startingCashCents = this.cashCents; this.startingReputationPoints = this.reputationPoints; this.ingredientSpendingCents = 0; this.advertisingSpendingCents = 0;
    this.capitalSpendingCents = 0; this.revenueCents = 0; this.wastedValueCents = 0; this.ordersCompleted = 0; this.ordersMissed = 0;
    this.activeOrders = []; this.potentialCustomers = 0; this.admittedCustomers = 0; this.customersTurnedAway = 0; this.ordersGenerated = 0;
    this.reputationChange = 0; this.serviceStartedAt = 0; this.nextOrderAt = 0; this.platesRemaining = 4; this.nextOrderId = 1;
  }
  private revalidateMenu(): void { this.selectedRecipeIds = this.selectedRecipeIds.filter((id) => this.isRecipeAvailable(id)); }
  private buyExpansion(type: "kitchen" | "dining"): boolean {
    if (this.phase !== "planning") return false; const expansion = type === "kitchen" ? KITCHEN_EXPANSION : DINING_EXPANSION;
    if ((type === "kitchen" ? this.kitchenLevel : this.diningLevel) >= 2) return false;
    if (this.cashCents < expansion.costCents) { this.lastFeedback = `Not enough cash for ${expansion.displayName}.`; return false; }
    this.cashCents -= expansion.costCents; this.capitalSpendingCents += expansion.costCents;
    if (type === "kitchen") this.kitchenLevel = 2; else this.diningLevel = 2;
    this.lastFeedback = `${expansion.displayName} purchased permanently.`; this.save(); return true;
  }
  private orderIntervalMs(): number { return this.admittedCustomers <= 1 ? this.serviceDurationMs : Math.max(4500, Math.floor((this.serviceDurationMs - 10_000) / this.admittedCustomers)); }
  private createOrder(now: number, forcedRecipe?: RecipeId, patienceMs = ORDER_PATIENCE_MS): OrderTicket {
    const recipeId = forcedRecipe ?? this.selectedRecipeIds[Math.floor(this.random() * this.selectedRecipeIds.length)];
    const order = { id: this.nextOrderId++, recipeId, createdAt: now, expiresAt: now + patienceMs }; this.activeOrders.push(order); this.ordersGenerated += 1; return order;
  }
  private finishService(): ServiceEvent[] {
    const events: ServiceEvent[] = this.activeOrders.map((order) => ({ type: "order-expired" as const, order }));
    this.ordersMissed += this.activeOrders.length + Math.max(0, this.admittedCustomers - this.ordersGenerated); this.activeOrders = [];
    const rate = this.admittedCustomers > 0 ? this.ordersCompleted / this.admittedCustomers : 0;
    const intendedChange = rate >= 0.8 ? 20 : rate >= 0.6 ? 10 : rate >= 0.4 ? 0 : rate >= 0.25 ? -10 : -20;
    const nextReputation = Math.max(0, this.reputationPoints + intendedChange); this.reputationChange = nextReputation - this.reputationPoints; this.reputationPoints = nextReputation;
    this.selectedAdId = "none"; this.phase = "summary"; this.lastFeedback = "SERVICE CLOSED";
    this.save(); events.push({ type: "service-ended" }); return events;
  }
  private random(): number { this.randomState = (this.randomState * 1664525 + 1013904223) >>> 0; return this.randomState / 0x1_0000_0000; }
}
