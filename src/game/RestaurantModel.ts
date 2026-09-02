import { INGREDIENT_IDS, INGREDIENTS, RECIPES, type IngredientId, type RecipeId } from "./data";

export type RestaurantPhase = "menu" | "purchase" | "prep" | "service" | "summary";

export interface OrderTicket {
  id: number;
  recipeId: RecipeId;
  createdAt: number;
  expiresAt: number;
}

export type ServiceEvent =
  | { type: "order-arrived"; order: OrderTicket }
  | { type: "order-expired"; order: OrderTicket }
  | { type: "service-ended" };

export interface NightSummary {
  startingCash: number;
  ingredientSpending: number;
  revenueEarned: number;
  wastedIngredientValue: number;
  ordersCompleted: number;
  ordersMissed: number;
  finalCash: number;
}

export class RestaurantModel {
  readonly startingCash = 40;
  readonly serviceDurationMs: number;
  readonly inventory: Record<IngredientId, number> = { potato: 0, tomato: 0, onion: 0, cheese: 0 };
  phase: RestaurantPhase = "menu";
  selectedRecipeIds: RecipeId[] = [];
  cash = this.startingCash;
  ingredientSpending = 0;
  revenue = 0;
  wastedValue = 0;
  ordersCompleted = 0;
  ordersMissed = 0;
  activeOrders: OrderTicket[] = [];
  serviceStartedAt = 0;
  nextOrderAt = 0;
  platesRemaining = 4;
  lastFeedback = "Choose exactly two dishes for tonight.";
  private nextOrderId = 1;
  private randomState: number;

  constructor(options: { serviceDurationMs?: number; seed?: number } = {}) {
    this.serviceDurationMs = options.serviceDurationMs ?? 120_000;
    this.randomState = options.seed ?? 7283;
  }

  toggleRecipe(recipeId: RecipeId): boolean {
    if (this.phase !== "menu") return false;
    const index = this.selectedRecipeIds.indexOf(recipeId);
    if (index >= 0) this.selectedRecipeIds.splice(index, 1);
    else if (this.selectedRecipeIds.length < 2) this.selectedRecipeIds.push(recipeId);
    else { this.lastFeedback = "Choose two dishes — deselect one first."; return false; }
    this.lastFeedback = `${this.selectedRecipeIds.length}/2 dishes selected.`;
    return true;
  }

  beginPurchasing(): boolean {
    if (this.phase !== "menu" || this.selectedRecipeIds.length !== 2) return false;
    this.phase = "purchase"; this.lastFeedback = "Buy the quantities you want. Keeping cash is allowed."; return true;
  }

  purchaseIngredient(ingredientId: IngredientId): boolean {
    if (this.phase !== "purchase") return false;
    const cost = INGREDIENTS[ingredientId].purchaseCost;
    if (this.cash < cost) { this.lastFeedback = `Not enough cash for ${INGREDIENTS[ingredientId].displayName}.`; return false; }
    this.cash -= cost; this.ingredientSpending += cost; this.inventory[ingredientId] += 1;
    this.lastFeedback = `Bought ${INGREDIENTS[ingredientId].displayName} for $${cost}.`;
    return true;
  }

  removePurchasedIngredient(ingredientId: IngredientId): boolean {
    if (this.phase !== "purchase" || this.inventory[ingredientId] <= 0) return false;
    const cost = INGREDIENTS[ingredientId].purchaseCost;
    this.inventory[ingredientId] -= 1; this.cash += cost; this.ingredientSpending -= cost;
    this.lastFeedback = `Removed one ${INGREDIENTS[ingredientId].displayName} from tonight's basket.`;
    return true;
  }

  beginPrep(): boolean {
    if (this.phase !== "purchase") return false;
    this.phase = "prep"; this.lastFeedback = "CLOSED · Prep freely, then open the restaurant."; return true;
  }

  takeIngredient(ingredientId: IngredientId): boolean {
    if ((this.phase !== "prep" && this.phase !== "service") || this.inventory[ingredientId] <= 0) return false;
    this.inventory[ingredientId] -= 1; return true;
  }

  recordWaste(value: number): void {
    this.wastedValue += value;
  }

  startService(now: number): ServiceEvent[] {
    if (this.phase !== "prep") return [];
    this.phase = "service"; this.serviceStartedAt = now; this.nextOrderAt = now + 18_000;
    this.lastFeedback = "OPEN · First order is in!";
    return [{ type: "order-arrived", order: this.createOrder(now) }];
  }

  updateService(now: number): ServiceEvent[] {
    if (this.phase !== "service") return [];
    const events: ServiceEvent[] = [];
    const expired = this.activeOrders.filter((order) => order.expiresAt <= now);
    if (expired.length) {
      const expiredIds = new Set(expired.map((order) => order.id));
      this.activeOrders = this.activeOrders.filter((order) => !expiredIds.has(order.id));
      this.ordersMissed += expired.length;
      expired.forEach((order) => events.push({ type: "order-expired", order }));
      this.lastFeedback = `${RECIPES[expired[expired.length - 1].recipeId].displayName} expired — $0 earned.`;
    }
    if (now - this.serviceStartedAt >= this.serviceDurationMs) {
      this.ordersMissed += this.activeOrders.length;
      this.activeOrders.forEach((order) => events.push({ type: "order-expired", order }));
      this.activeOrders = []; this.phase = "summary"; this.lastFeedback = "SERVICE CLOSED";
      events.push({ type: "service-ended" }); return events;
    }
    if (now >= this.nextOrderAt && this.activeOrders.length < 3) {
      const order = this.createOrder(now); events.push({ type: "order-arrived", order });
      const progress = Math.min(1, (now - this.serviceStartedAt) / this.serviceDurationMs);
      this.nextOrderAt = now + Math.round(18_000 - progress * 8_000);
    }
    return events;
  }

  serveDish(recipeId: RecipeId): boolean {
    if (this.phase !== "service") return false;
    const orderIndex = this.activeOrders.findIndex((order) => order.recipeId === recipeId);
    if (orderIndex < 0) { this.lastFeedback = `No active ${RECIPES[recipeId].displayName} order — serve refused.`; return false; }
    this.activeOrders.splice(orderIndex, 1);
    this.revenue += RECIPES[recipeId].sellingPrice; this.ordersCompleted += 1;
    this.lastFeedback = `${RECIPES[recipeId].displayName} served · +$${RECIPES[recipeId].sellingPrice}`;
    return true;
  }

  forceOrder(recipeId: RecipeId, now: number, patienceMs = 35_000): OrderTicket | null {
    if (this.phase !== "service" || !this.selectedRecipeIds.includes(recipeId) || this.activeOrders.length >= 3) return null;
    const order = { id: this.nextOrderId++, recipeId, createdAt: now, expiresAt: now + patienceMs };
    this.activeOrders.push(order); return order;
  }

  endService(now: number): ServiceEvent[] {
    if (this.phase !== "service") return [];
    return this.updateService(this.serviceStartedAt + this.serviceDurationMs + Math.max(1, now - this.serviceStartedAt));
  }

  get serviceRemainingMs(): number {
    if (this.phase !== "service") return this.phase === "summary" ? 0 : this.serviceDurationMs;
    return Math.max(0, this.serviceDurationMs - (performance.now() - this.serviceStartedAt));
  }

  summary(): NightSummary {
    return {
      startingCash: this.startingCash,
      ingredientSpending: this.ingredientSpending,
      revenueEarned: this.revenue,
      wastedIngredientValue: this.wastedValue,
      ordersCompleted: this.ordersCompleted,
      ordersMissed: this.ordersMissed,
      finalCash: this.cash + this.revenue,
    };
  }

  resetNight(): void {
    this.phase = "menu"; this.selectedRecipeIds = []; this.cash = this.startingCash;
    this.ingredientSpending = 0; this.revenue = 0; this.wastedValue = 0;
    this.ordersCompleted = 0; this.ordersMissed = 0; this.activeOrders = [];
    this.serviceStartedAt = 0; this.nextOrderAt = 0; this.platesRemaining = 4;
    INGREDIENT_IDS.forEach((id) => { this.inventory[id] = 0; });
    this.lastFeedback = "Choose exactly two dishes for tonight.";
    this.nextOrderId = 1; this.randomState = 7283;
  }

  private createOrder(now: number): OrderTicket {
    const index = Math.floor(this.random() * this.selectedRecipeIds.length);
    const order = { id: this.nextOrderId++, recipeId: this.selectedRecipeIds[index], createdAt: now, expiresAt: now + 35_000 };
    this.activeOrders.push(order); return order;
  }

  private random(): number {
    this.randomState = (this.randomState * 1664525 + 1013904223) >>> 0;
    return this.randomState / 0x1_0000_0000;
  }
}
