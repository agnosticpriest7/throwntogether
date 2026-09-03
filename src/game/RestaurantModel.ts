import {
  ADVERTISING, APPLIANCES, APPLIANCE_IDS, DINING_EXPANSION, DISHWASH_DURATION_MS,
  EATING_DURATION_MS, INGREDIENT_IDS, INGREDIENTS, KITCHEN_EXPANSION, ORDER_PATIENCE_MS,
  PICKUP_SLOT_COUNT, PLATE_COUNT, RECIPES, REPUTATION_LEVELS, SAVE_KEY, SAVE_VERSION,
  SERVER_CLEAR_DURATION_MS, SERVER_DELIVERY_DURATION_MS, SERVER_SEAT_DURATION_MS,
  SERVICE_DURATION_MS, STAFF_CANDIDATES, STAFF_ROLES, STARTING_CASH_CENTS,
  STARTING_REPUTATION_POINTS, TABLES, TABLE_WAIT_PATIENCE_MS, bulkQuote,
  type AdId, type ApplianceId, type IngredientId, type RecipeId, type StaffRoleId,
} from "./data";

export type RestaurantPhase = "landing" | "planning" | "prep" | "service" | "summary";
export interface StorageLike { getItem(key: string): string | null; setItem(key: string, value: string): void; removeItem(key: string): void }
export interface EmployeeRecord { id: string; name: string; role: StaffRoleId; colorVariant: number; scheduled: boolean }
export type CustomerState = "arriving" | "waiting_for_table" | "walking_to_table" | "waiting_for_food" | "eating" | "leaving" | "failed";
export interface CustomerParty {
  id: number; size: 1 | 2; recipeId: RecipeId; state: CustomerState; arrivedAt: number;
  waitExpiresAt: number; foodExpiresAt: number; stateEndsAt: number; tableId: string | null;
  orderId: number | null; failureReason: "table" | "food" | null;
}
export type TableLifecycle = "clean" | "reserved" | "waiting_food" | "eating" | "dirty";
export interface DiningTable { id: string; x: number; y: number; seats: 2; state: TableLifecycle; customerId: number | null }
export interface OrderTicket { id: number; recipeId: RecipeId; createdAt: number; expiresAt: number; customerId: number; tableId: string }
export interface ReadyDish { id: number; recipeId: RecipeId; orderId: number; customerId: number; tableId: string; claimedBy: string | null }
export interface StaffTask { type: "deliver" | "seat" | "clear" | "wash"; targetId: string; destination: string; startedAt: number; endsAt: number }
export interface StaffRuntime { employeeId: string; name: string; role: StaffRoleId; state: string; task: StaffTask | null; x: number; y: number; completedTasks: number }
export type ServiceEvent =
  | { type: "customer-arrived"; customer: CustomerParty }
  | { type: "customer-seated"; customer: CustomerParty }
  | { type: "order-arrived"; order: OrderTicket }
  | { type: "order-expired"; order: OrderTicket }
  | { type: "server-pickup"; dish: ReadyDish }
  | { type: "delivery-complete"; order: OrderTicket }
  | { type: "customer-left"; customer: CustomerParty; happy: boolean }
  | { type: "dirty-dish-returned" }
  | { type: "plate-washed"; by: "human" | "dishwasher" }
  | { type: "service-ended" };
export interface DemandPreview { baseline: number; adBonus: number; potential: number; capacity: number; admitted: number; turnedAway: number }
export interface ReputationProgress { level: number; points: number; levelStart: number; nextLevelAt: number | null; percent: number }
export interface NightSummary {
  day: number; potentialCustomers: number; arrivals: number; admittedCustomers: number; seatedCustomers: number;
  customersTurnedAway: number; leftWaitingForTable: number; leftWaitingForFood: number; ordersCompleted: number;
  ordersMissed: number; peakSeatsOccupied: number; mealsDelivered: number; tableTurns: number;
  scheduledEmployees: string[]; payrollCents: number; serverDeliveries: number; tablesCleared: number;
  dishwasherPlatesWashed: number; startingCashCents: number; ingredientSpendingCents: number;
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
  advertisingSpendingCents: number; capitalSpendingCents: number; staffRoster?: EmployeeRecord[];
  payrollSpendingCents?: number; payrollChargedDay?: number;
}

const STARTING_INSTALLED: Array<ApplianceId | null> = ["oven", "prep-station", "assembly-station", "plating-station", null, null];

export class RestaurantModel {
  readonly serviceDurationMs: number;
  readonly inventory: Record<IngredientId, number> = { potato: 0, tomato: 0, onion: 0, cheese: 0 };
  readonly applianceOwnership: Record<ApplianceId, number> = { "prep-station": 1, oven: 1, "assembly-station": 1, "plating-station": 1, fryer: 0 };
  phase: RestaurantPhase;
  hasSave = false; day = 1; cashCents = STARTING_CASH_CENTS; kitchenLevel = 1; diningLevel = 1;
  reputationPoints = STARTING_REPUTATION_POINTS; installedSlots: Array<ApplianceId | null> = [...STARTING_INSTALLED];
  selectedRecipeIds: RecipeId[] = []; selectedAdId: AdId = "none"; staffRoster: EmployeeRecord[] = [];
  startingCashCents = STARTING_CASH_CENTS; startingReputationPoints = STARTING_REPUTATION_POINTS;
  ingredientSpendingCents = 0; advertisingSpendingCents = 0; capitalSpendingCents = 0; payrollSpendingCents = 0;
  payrollChargedDay = 0; revenueCents = 0; wastedValueCents = 0; ordersCompleted = 0; ordersMissed = 0;
  activeOrders: OrderTicket[] = []; readyDishes: ReadyDish[] = []; customers: CustomerParty[] = [];
  diningTables: DiningTable[] = []; activeStaff: StaffRuntime[] = []; dirtyReturnQueue = 0; claimedDirtyPlates = 0;
  potentialCustomers = 0; admittedCustomers = 0; customersTurnedAway = 0; ordersGenerated = 0;
  arrivals = 0; seatedCustomers = 0; leftWaitingForTable = 0; leftWaitingForFood = 0;
  peakSeatsOccupied = 0; tableTurns = 0; serverDeliveries = 0; tablesCleared = 0; dishwasherPlatesWashed = 0;
  reputationChange = 0; serviceStartedAt = 0; nextCustomerAt = 0; platesRemaining = PLATE_COUNT;
  lastFeedback = "Start a new restaurant or continue your saved one.";
  private nextOrderId = 1; private nextCustomerId = 1; private nextDishId = 1;
  private randomState: number; private readonly initialSeed: number;

  constructor(private readonly options: { serviceDurationMs?: number; seed?: number; storage?: StorageLike; startAtLanding?: boolean } = {}) {
    this.serviceDurationMs = options.serviceDurationMs ?? SERVICE_DURATION_MS;
    this.initialSeed = options.seed ?? 7283; this.randomState = this.initialSeed;
    this.hasSave = this.load(); this.phase = options.startAtLanding ? "landing" : "planning";
    if (!this.hasSave) this.applyCleanState();
  }

  newRestaurant(): void { this.applyCleanState(); this.phase = "planning"; this.hasSave = true; this.lastFeedback = "Day 1 planning is open."; this.save(); }
  continueRestaurant(): boolean { if (!this.hasSave) return false; this.phase = "planning"; this.lastFeedback = `Welcome back to Day ${this.day}.`; return true; }
  resetEndlessSave(confirmed: boolean): boolean { if (!confirmed) return false; this.options.storage?.removeItem(SAVE_KEY); this.hasSave = false; this.applyCleanState(); this.phase = "landing"; this.lastFeedback = "Endless save reset."; return true; }

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

  hireEmployee(candidateId: string): boolean {
    if (this.phase !== "planning" || this.staffRoster.some(({ id }) => id === candidateId)) return false;
    const candidate = STAFF_CANDIDATES.find(({ id }) => id === candidateId); if (!candidate) return false;
    const role = STAFF_ROLES[candidate.role];
    if (this.cashCents < role.hireCostCents) { this.lastFeedback = `Not enough cash to hire ${candidate.name}.`; return false; }
    this.cashCents -= role.hireCostCents; this.capitalSpendingCents += role.hireCostCents;
    this.staffRoster.push({ id: candidate.id, name: candidate.name, role: candidate.role, colorVariant: candidate.colorVariant, scheduled: false });
    this.lastFeedback = `${candidate.name} hired as ${role.displayName}.`; this.save(); return true;
  }
  setEmployeeScheduled(employeeId: string, scheduled: boolean): boolean {
    if (this.phase !== "planning") return false; const employee = this.staffRoster.find(({ id }) => id === employeeId); if (!employee) return false;
    employee.scheduled = scheduled; this.lastFeedback = `${employee.name} is ${scheduled ? "working" : "off"} tonight.`; this.save(); return true;
  }
  get scheduledPayrollCents(): number { return this.staffRoster.filter(({ scheduled }) => scheduled).reduce((total, employee) => total + STAFF_ROLES[employee.role].wageCents, 0); }

  selectAdvertising(id: AdId): boolean {
    if (this.phase !== "planning") return false;
    const oldCost = ADVERTISING[this.selectedAdId].costCents; const newCost = ADVERTISING[id].costCents; const difference = newCost - oldCost;
    if (difference > this.cashCents) { this.lastFeedback = `Not enough cash for ${ADVERTISING[id].displayName}.`; return false; }
    this.cashCents -= difference; this.advertisingSpendingCents += difference; this.selectedAdId = id;
    this.lastFeedback = `${ADVERTISING[id].displayName} selected for Day ${this.day}.`; this.save(); return true;
  }
  demandPreview(): DemandPreview {
    const baseline = this.reputationLevel().baselineDemand; const modifier = ADVERTISING[this.selectedAdId].demandBonusBps;
    const potential = Math.ceil(baseline * (10_000 + modifier) / 10_000); const capacity = this.diningCapacity;
    return { baseline, adBonus: potential - baseline, potential, capacity, admitted: potential, turnedAway: Math.max(0, potential - capacity) };
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
    const payroll = this.scheduledPayrollCents;
    if (this.payrollChargedDay !== this.day && payroll > this.cashCents) { this.lastFeedback = `Cannot begin Prep: scheduled payroll is $${(payroll / 100).toFixed(0)}, but only $${(this.cashCents / 100).toFixed(0)} is available.`; return false; }
    if (this.payrollChargedDay !== this.day) { this.cashCents -= payroll; this.payrollSpendingCents = payroll; this.payrollChargedDay = this.day; }
    const demand = this.demandPreview(); this.potentialCustomers = demand.potential; this.admittedCustomers = demand.potential;
    this.prepareDiningTables();
    this.phase = "prep"; this.lastFeedback = `CLOSED · Prep for up to ${demand.potential} arrivals. Payroll reserved.`; this.save(); return true;
  }
  takeIngredient(id: IngredientId): boolean { if ((this.phase !== "prep" && this.phase !== "service") || this.inventory[id] <= 0) return false; this.inventory[id] -= 1; return true; }
  recordWaste(valueCents: number): void { this.wastedValueCents += valueCents; }
  useCleanPlate(): boolean { if (this.platesRemaining <= 0) { this.lastFeedback = "No clean plates — wash a returned dish."; return false; } this.platesRemaining -= 1; return true; }
  returnCleanPlate(): void { this.platesRemaining = Math.min(PLATE_COUNT, this.platesRemaining + 1); }
  claimDirtyPlate(): boolean { if (this.dirtyReturnQueue <= 0) return false; this.dirtyReturnQueue -= 1; this.claimedDirtyPlates += 1; return true; }
  completePlateWash(by: "human" | "dishwasher" = "human"): ServiceEvent | null {
    if (this.claimedDirtyPlates <= 0) return null; this.claimedDirtyPlates -= 1; this.returnCleanPlate();
    if (by === "dishwasher") this.dishwasherPlatesWashed += 1; this.lastFeedback = `${by === "human" ? "Chef" : "Dishwasher"} returned a clean plate.`;
    return { type: "plate-washed", by };
  }

  startService(now: number): ServiceEvent[] {
    if (this.phase !== "prep") return [];
    this.phase = "service"; this.serviceStartedAt = now; this.ordersGenerated = 0; this.activeOrders = []; this.readyDishes = [];
    this.customers = []; this.prepareDiningTables();
    this.activeStaff = this.staffRoster.filter(({ scheduled }) => scheduled).map((employee, index) => ({ employeeId: employee.id, name: employee.name, role: employee.role, state: "idle", task: null, x: employee.role === "server" ? 960 + index * 18 : 835, y: employee.role === "server" ? 545 : 330, completedTasks: 0 }));
    this.nextCustomerAt = now; this.lastFeedback = "OPEN · Customers are arriving!"; return this.updateService(now);
  }
  updateService(now: number): ServiceEvent[] {
    if (this.phase !== "service") return [];
    const events: ServiceEvent[] = [];
    if (now - this.serviceStartedAt >= this.serviceDurationMs) return this.finishService();
    while (now >= this.nextCustomerAt && this.arrivals < this.potentialCustomers) { events.push(this.createCustomer(this.nextCustomerAt)); this.nextCustomerAt += this.customerIntervalMs(); }
    this.advanceCustomers(now, events); this.advanceStaff(now, events); this.assignStaffTasks(now, events);
    const occupiedSeats = this.customers.filter(({ state }) => ["walking_to_table", "waiting_for_food", "eating"].includes(state)).reduce((total, customer) => total + customer.size, 0);
    this.peakSeatsOccupied = Math.max(this.peakSeatsOccupied, occupiedSeats);
    return events;
  }
  queueReadyDish(recipeId: RecipeId): boolean {
    if (this.phase !== "service" || this.readyDishes.length >= PICKUP_SLOT_COUNT) { this.lastFeedback = "Service pickup is full."; return false; }
    const order = this.activeOrders.find((ticket) => ticket.recipeId === recipeId && !this.readyDishes.some(({ orderId }) => orderId === ticket.id));
    if (!order) { this.lastFeedback = `No waiting ${RECIPES[recipeId].displayName} order — pickup refused.`; return false; }
    this.readyDishes.push({ id: this.nextDishId++, recipeId, orderId: order.id, customerId: order.customerId, tableId: order.tableId, claimedBy: null });
    this.lastFeedback = `${RECIPES[recipeId].displayName} is waiting for a server at pickup.`; return true;
  }
  serveDish(recipeId: RecipeId): boolean { return this.queueReadyDish(recipeId); }
  forceOrder(recipeId: RecipeId, now: number, patienceMs = ORDER_PATIENCE_MS): OrderTicket | null {
    if (this.phase !== "service" || !this.selectedRecipeIds.includes(recipeId) || this.activeOrders.length >= 3) return null;
    let table = this.diningTables.find(({ state }) => state === "clean");
    if (!table) { table = this.diningTables[0]; if (!table) return null; }
    const customer = this.createSyntheticSeatedCustomer(recipeId, now, table); return this.createOrder(customer, now, patienceMs);
  }
  endService(_now: number): ServiceEvent[] { return this.phase === "service" ? this.finishService() : []; }

  summary(): NightSummary {
    const remainingInventory = { ...this.inventory }; const remainingInventoryValueCents = INGREDIENT_IDS.reduce((total, id) => total + this.inventory[id] * INGREDIENTS[id].purchaseCostCents, 0);
    return {
      day: this.day, potentialCustomers: this.potentialCustomers, arrivals: this.arrivals, admittedCustomers: this.admittedCustomers,
      seatedCustomers: this.seatedCustomers, customersTurnedAway: this.customersTurnedAway, leftWaitingForTable: this.leftWaitingForTable,
      leftWaitingForFood: this.leftWaitingForFood, ordersCompleted: this.ordersCompleted, ordersMissed: this.ordersMissed,
      peakSeatsOccupied: this.peakSeatsOccupied, mealsDelivered: this.ordersCompleted, tableTurns: this.tableTurns,
      scheduledEmployees: this.staffRoster.filter(({ scheduled }) => scheduled).map(({ name }) => name), payrollCents: this.payrollSpendingCents,
      serverDeliveries: this.serverDeliveries, tablesCleared: this.tablesCleared, dishwasherPlatesWashed: this.dishwasherPlatesWashed,
      startingCashCents: this.startingCashCents, ingredientSpendingCents: this.ingredientSpendingCents,
      advertisingSpendingCents: this.advertisingSpendingCents, capitalSpendingCents: this.capitalSpendingCents,
      revenueCents: this.revenueCents, wastedIngredientValueCents: this.wastedValueCents, endingCashCents: this.cashCents,
      remainingInventory, remainingInventoryValueCents, startingReputation: this.reputationProgress(this.startingReputationPoints),
      reputationChange: this.reputationChange, endingReputation: this.reputationProgress(),
    };
  }
  nextDay(): boolean {
    if (this.phase !== "summary") return false; this.day += 1; this.phase = "planning"; this.selectedRecipeIds = []; this.selectedAdId = "none";
    this.resetDailyAccounting(); this.lastFeedback = `Day ${this.day} planning is open. Leftovers and staff are waiting.`; this.save(); return true;
  }
  restartNight(): boolean { if (this.phase !== "prep" && this.phase !== "service") return false; if (!this.load()) return false; this.phase = "planning"; this.clearServiceRuntime(); this.lastFeedback = `Day ${this.day} restored to planning.`; return true; }

  save(): void { if (!this.options.storage || !this.hasSave) return; this.options.storage.setItem(SAVE_KEY, JSON.stringify(this.toSave())); }
  private load(): boolean {
    const raw = this.options.storage?.getItem(SAVE_KEY); if (!raw) return false;
    try { const saved = JSON.parse(raw) as EndlessSave; if (saved.version !== 1 && saved.version !== SAVE_VERSION) return false; this.applySave(saved); if (saved.version === 1 && this.options.storage) this.options.storage.setItem(SAVE_KEY, JSON.stringify(this.toSave())); return true; } catch { return false; }
  }
  private toSave(): EndlessSave {
    return { version: SAVE_VERSION, day: this.day, cashCents: this.cashCents, inventory: { ...this.inventory }, applianceOwnership: { ...this.applianceOwnership }, installedSlots: [...this.installedSlots], kitchenLevel: this.kitchenLevel, diningLevel: this.diningLevel, reputationPoints: this.reputationPoints, selectedRecipeIds: [...this.selectedRecipeIds], selectedAdId: this.selectedAdId, startingCashCents: this.startingCashCents, ingredientSpendingCents: this.ingredientSpendingCents, advertisingSpendingCents: this.advertisingSpendingCents, capitalSpendingCents: this.capitalSpendingCents, staffRoster: this.staffRoster.map((employee) => ({ ...employee })), payrollSpendingCents: this.payrollSpendingCents, payrollChargedDay: this.payrollChargedDay };
  }
  private applySave(saved: EndlessSave): void {
    this.day = saved.day; this.cashCents = saved.cashCents; INGREDIENT_IDS.forEach((id) => { this.inventory[id] = saved.inventory[id] ?? 0; });
    APPLIANCE_IDS.forEach((id) => { this.applianceOwnership[id] = saved.applianceOwnership[id] ?? APPLIANCES[id].startingOwned; });
    this.installedSlots = [...saved.installedSlots.slice(0, 6)]; while (this.installedSlots.length < 6) this.installedSlots.push(null);
    this.kitchenLevel = saved.kitchenLevel; this.diningLevel = saved.diningLevel; this.reputationPoints = saved.reputationPoints;
    this.selectedRecipeIds = saved.selectedRecipeIds.filter((id) => RECIPES[id]); this.selectedAdId = ADVERTISING[saved.selectedAdId] ? saved.selectedAdId : "none";
    this.startingCashCents = saved.startingCashCents ?? saved.cashCents; this.ingredientSpendingCents = saved.ingredientSpendingCents ?? 0;
    this.advertisingSpendingCents = saved.advertisingSpendingCents ?? 0; this.capitalSpendingCents = saved.capitalSpendingCents ?? 0;
    this.payrollSpendingCents = saved.payrollSpendingCents ?? 0; this.payrollChargedDay = saved.payrollChargedDay ?? 0;
    this.staffRoster = (saved.staffRoster?.length ? saved.staffRoster : this.startingStaff()).map((employee) => ({ ...employee })); this.startingReputationPoints = this.reputationPoints;
  }
  private applyCleanState(): void {
    this.day = 1; this.cashCents = STARTING_CASH_CENTS; this.kitchenLevel = 1; this.diningLevel = 1; this.reputationPoints = STARTING_REPUTATION_POINTS;
    INGREDIENT_IDS.forEach((id) => { this.inventory[id] = 0; }); APPLIANCE_IDS.forEach((id) => { this.applianceOwnership[id] = APPLIANCES[id].startingOwned; });
    this.installedSlots = [...STARTING_INSTALLED]; this.selectedRecipeIds = []; this.selectedAdId = "none"; this.staffRoster = this.startingStaff(); this.resetDailyAccounting(); this.randomState = this.initialSeed;
  }
  private startingStaff(): EmployeeRecord[] { return STAFF_CANDIDATES.filter(({ startingHired }) => startingHired).map((candidate) => ({ id: candidate.id, name: candidate.name, role: candidate.role, colorVariant: candidate.colorVariant, scheduled: candidate.startingScheduled ?? false })); }
  private resetDailyAccounting(): void {
    this.startingCashCents = this.cashCents; this.startingReputationPoints = this.reputationPoints; this.ingredientSpendingCents = 0; this.advertisingSpendingCents = 0;
    this.capitalSpendingCents = 0; this.payrollSpendingCents = 0; this.payrollChargedDay = 0; this.revenueCents = 0; this.wastedValueCents = 0;
    this.ordersCompleted = 0; this.ordersMissed = 0; this.potentialCustomers = 0; this.admittedCustomers = 0; this.customersTurnedAway = 0;
    this.ordersGenerated = 0; this.arrivals = 0; this.seatedCustomers = 0; this.leftWaitingForTable = 0; this.leftWaitingForFood = 0;
    this.peakSeatsOccupied = 0; this.tableTurns = 0; this.serverDeliveries = 0; this.tablesCleared = 0; this.dishwasherPlatesWashed = 0;
    this.reputationChange = 0; this.serviceStartedAt = 0; this.nextCustomerAt = 0; this.platesRemaining = PLATE_COUNT;
    this.nextOrderId = 1; this.nextCustomerId = 1; this.nextDishId = 1; this.clearServiceRuntime();
  }
  private clearServiceRuntime(): void { this.activeOrders = []; this.readyDishes = []; this.customers = []; this.diningTables = []; this.activeStaff = []; this.dirtyReturnQueue = 0; this.claimedDirtyPlates = 0; }
  private prepareDiningTables(): void { this.diningTables = TABLES.filter(({ requiredDiningLevel }) => requiredDiningLevel <= this.diningLevel).map((table) => ({ ...table, state: "clean", customerId: null })); }
  private revalidateMenu(): void { this.selectedRecipeIds = this.selectedRecipeIds.filter((id) => this.isRecipeAvailable(id)); }
  private buyExpansion(type: "kitchen" | "dining"): boolean {
    if (this.phase !== "planning") return false; const expansion = type === "kitchen" ? KITCHEN_EXPANSION : DINING_EXPANSION;
    if ((type === "kitchen" ? this.kitchenLevel : this.diningLevel) >= 2) return false;
    if (this.cashCents < expansion.costCents) { this.lastFeedback = `Not enough cash for ${expansion.displayName}.`; return false; }
    this.cashCents -= expansion.costCents; this.capitalSpendingCents += expansion.costCents;
    if (type === "kitchen") this.kitchenLevel = 2; else this.diningLevel = 2;
    this.lastFeedback = `${expansion.displayName} purchased permanently.`; this.save(); return true;
  }
  private customerIntervalMs(): number { return this.potentialCustomers <= 1 ? this.serviceDurationMs : Math.max(1500, Math.floor((this.serviceDurationMs - 8000) / this.potentialCustomers)); }
  private createCustomer(now: number): ServiceEvent {
    const recipeId = this.selectedRecipeIds[Math.floor(this.random() * this.selectedRecipeIds.length)]; const remaining = this.potentialCustomers - this.arrivals; const size = (remaining > 1 && this.random() < 0.42 ? 2 : 1) as 1 | 2;
    const customer: CustomerParty = { id: this.nextCustomerId++, size, recipeId, state: "arriving", arrivedAt: now, waitExpiresAt: now + TABLE_WAIT_PATIENCE_MS, foodExpiresAt: 0, stateEndsAt: now + 800, tableId: null, orderId: null, failureReason: null };
    this.customers.push(customer); this.arrivals += 1; return { type: "customer-arrived", customer };
  }
  private advanceCustomers(now: number, events: ServiceEvent[]): void {
    for (const customer of this.customers) {
      if (customer.state === "arriving" && now >= customer.stateEndsAt) customer.state = "waiting_for_table";
      if (customer.state === "waiting_for_table" && now >= customer.waitExpiresAt) { customer.state = "failed"; customer.failureReason = "table"; this.leftWaitingForTable += customer.size; this.customersTurnedAway += customer.size; events.push({ type: "customer-left", customer, happy: false }); }
      if (customer.state === "waiting_for_food" && now >= customer.foodExpiresAt) this.failFoodWait(customer, events);
      if (customer.state === "eating" && now >= customer.stateEndsAt) { customer.state = "leaving"; customer.stateEndsAt = now + 1000; const table = this.tableFor(customer); if (table) table.state = "dirty"; events.push({ type: "customer-left", customer, happy: true }); }
      if (customer.state === "leaving" && now >= customer.stateEndsAt) customer.state = "failed";
    }
  }
  private advanceStaff(now: number, events: ServiceEvent[]): void {
    for (const staff of this.activeStaff) {
      if (!staff.task || now < staff.task.endsAt) continue;
      const task = staff.task; staff.task = null; staff.state = "idle"; staff.completedTasks += 1;
      if (task.type === "seat") this.completeSeating(Number(task.targetId), now, events);
      else if (task.type === "deliver") this.completeDelivery(Number(task.targetId), now, staff, events);
      else if (task.type === "clear") { const table = this.diningTables.find(({ id }) => id === task.targetId); if (table?.state === "dirty") { table.state = "clean"; table.customerId = null; this.dirtyReturnQueue += 1; this.tablesCleared += 1; this.tableTurns += 1; events.push({ type: "dirty-dish-returned" }); } }
      else if (task.type === "wash") { const event = this.completePlateWash("dishwasher"); if (event) events.push(event); }
    }
  }
  private assignStaffTasks(now: number, events: ServiceEvent[]): void {
    for (const staff of this.activeStaff.filter(({ task }) => !task)) {
      if (staff.role === "dishwasher") { if (this.claimDirtyPlate()) staff.task = { type: "wash", targetId: "sink", destination: "Dish sink", startedAt: now, endsAt: now + DISHWASH_DURATION_MS }; }
      else {
        const dish = this.readyDishes.find(({ claimedBy }) => !claimedBy);
        if (dish) { dish.claimedBy = staff.employeeId; staff.state = "delivering"; staff.task = { type: "deliver", targetId: String(dish.id), destination: `Table ${dish.tableId.slice(1)}`, startedAt: now, endsAt: now + SERVER_DELIVERY_DURATION_MS }; events.push({ type: "server-pickup", dish }); continue; }
        const customer = this.customers.find((entry) => entry.state === "waiting_for_table" && !this.diningTables.some(({ customerId }) => customerId === entry.id));
        const table = this.diningTables.find(({ state }) => state === "clean");
        if (customer && table && this.activeOrders.length < 3) { customer.state = "walking_to_table"; customer.tableId = table.id; table.state = "reserved"; table.customerId = customer.id; staff.state = "seating"; staff.task = { type: "seat", targetId: String(customer.id), destination: `Table ${table.id.slice(1)}`, startedAt: now, endsAt: now + SERVER_SEAT_DURATION_MS }; continue; }
        const dirty = this.diningTables.find((candidate) => candidate.state === "dirty" && !this.activeStaff.some(({ task }) => task?.type === "clear" && task.targetId === candidate.id));
        if (dirty) { staff.state = "clearing"; staff.task = { type: "clear", targetId: dirty.id, destination: "Dirty return", startedAt: now, endsAt: now + SERVER_CLEAR_DURATION_MS }; }
      }
    }
  }
  private completeSeating(customerId: number, now: number, events: ServiceEvent[]): void {
    const customer = this.customers.find(({ id }) => id === customerId); if (!customer || customer.state !== "walking_to_table" || !customer.tableId) return;
    const table = this.tableFor(customer); if (!table) { customer.state = "waiting_for_table"; customer.tableId = null; return; }
    customer.state = "waiting_for_food"; customer.foodExpiresAt = now + ORDER_PATIENCE_MS; table.state = "waiting_food"; this.seatedCustomers += customer.size;
    const order = this.createOrder(customer, now); events.push({ type: "customer-seated", customer }, { type: "order-arrived", order });
  }
  private createOrder(customer: CustomerParty, now: number, patienceMs = ORDER_PATIENCE_MS): OrderTicket {
    const order: OrderTicket = { id: this.nextOrderId++, recipeId: customer.recipeId, createdAt: now, expiresAt: now + patienceMs, customerId: customer.id, tableId: customer.tableId! };
    customer.orderId = order.id; customer.foodExpiresAt = order.expiresAt; this.activeOrders.push(order); this.ordersGenerated += 1; return order;
  }
  private createSyntheticSeatedCustomer(recipeId: RecipeId, now: number, table: DiningTable): CustomerParty {
    const customer: CustomerParty = { id: this.nextCustomerId++, size: 1, recipeId, state: "waiting_for_food", arrivedAt: now, waitExpiresAt: now, foodExpiresAt: now + ORDER_PATIENCE_MS, stateEndsAt: 0, tableId: table.id, orderId: null, failureReason: null };
    table.state = "waiting_food"; table.customerId = customer.id; this.customers.push(customer); this.seatedCustomers += 1; return customer;
  }
  private completeDelivery(dishId: number, now: number, _staff: StaffRuntime, events: ServiceEvent[]): void {
    const dishIndex = this.readyDishes.findIndex(({ id }) => id === dishId); if (dishIndex < 0) return; const [dish] = this.readyDishes.splice(dishIndex, 1);
    const orderIndex = this.activeOrders.findIndex(({ id }) => id === dish.orderId); const customer = this.customers.find(({ id }) => id === dish.customerId);
    if (orderIndex < 0 || !customer || customer.state !== "waiting_for_food") { this.wastedValueCents += this.recipeIngredientValue(dish.recipeId); this.dirtyReturnQueue += 1; return; }
    const [order] = this.activeOrders.splice(orderIndex, 1); customer.state = "eating"; customer.stateEndsAt = now + EATING_DURATION_MS; const table = this.tableFor(customer); if (table) table.state = "eating";
    const price = RECIPES[order.recipeId].sellingPriceCents; this.cashCents += price; this.revenueCents += price; this.ordersCompleted += 1; this.serverDeliveries += 1;
    this.lastFeedback = `${RECIPES[order.recipeId].displayName} delivered to Table ${order.tableId.slice(1)} · +$${(price / 100).toFixed(0)}`; events.push({ type: "delivery-complete", order });
  }
  private failFoodWait(customer: CustomerParty, events: ServiceEvent[]): void {
    customer.state = "failed"; customer.failureReason = "food"; this.leftWaitingForFood += customer.size; this.ordersMissed += 1;
    const order = this.activeOrders.find(({ id }) => id === customer.orderId); if (order) { this.activeOrders = this.activeOrders.filter(({ id }) => id !== order.id); events.push({ type: "order-expired", order }); }
    const orphaned = this.readyDishes.filter(({ customerId }) => customerId === customer.id); this.readyDishes = this.readyDishes.filter(({ customerId }) => customerId !== customer.id);
    if (orphaned.length) { this.dirtyReturnQueue += orphaned.length; this.wastedValueCents += orphaned.reduce((total, dish) => total + this.recipeIngredientValue(dish.recipeId), 0); }
    const table = this.tableFor(customer); if (table) { table.state = "clean"; table.customerId = null; }
    events.push({ type: "customer-left", customer, happy: false });
  }
  private tableFor(customer: CustomerParty): DiningTable | undefined { return this.diningTables.find(({ id }) => id === customer.tableId); }
  private recipeIngredientValue(recipeId: RecipeId): number { return RECIPES[recipeId].ingredients.reduce((total, requirement) => total + INGREDIENTS[requirement.ingredientId].purchaseCostCents, 0); }
  private finishService(): ServiceEvent[] {
    const events: ServiceEvent[] = [];
    for (const customer of this.customers) if (["arriving", "waiting_for_table", "walking_to_table"].includes(customer.state)) { customer.state = "failed"; customer.failureReason = "table"; this.leftWaitingForTable += customer.size; this.customersTurnedAway += customer.size; }
    for (const customer of this.customers) if (customer.state === "waiting_for_food") this.failFoodWait(customer, events);
    const completionBase = Math.max(1, this.seatedCustomers); const rate = this.ordersCompleted / completionBase; const failures = this.leftWaitingForTable + this.leftWaitingForFood;
    const intendedChange = failures === 0 && rate >= 0.8 ? 20 : rate >= 0.6 ? 10 : rate >= 0.4 ? 0 : rate >= 0.25 ? -10 : -20;
    const nextReputation = Math.max(0, this.reputationPoints + Math.max(-20, Math.min(20, intendedChange))); this.reputationChange = nextReputation - this.reputationPoints; this.reputationPoints = nextReputation;
    this.selectedAdId = "none"; this.phase = "summary"; this.lastFeedback = "SERVICE CLOSED"; this.save(); events.push({ type: "service-ended" }); return events;
  }
  private random(): number { this.randomState = (this.randomState * 1664525 + 1013904223) >>> 0; return this.randomState / 0x1_0000_0000; }
}
