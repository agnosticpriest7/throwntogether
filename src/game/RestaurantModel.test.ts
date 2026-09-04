import { describe, expect, it } from "vitest";
import { RestaurantModel, type StorageLike } from "./RestaurantModel";
import { DISHWASH_DURATION_MS, EATING_DURATION_MS, ORDER_PATIENCE_MS, SAVE_KEY, SERVER_CLEAR_DURATION_MS, SERVER_DELIVERY_DURATION_MS, TABLE_WAIT_PATIENCE_MS, bulkQuote } from "./data";

class MemoryStorage implements StorageLike {
  data = new Map<string, string>();
  getItem(key: string) { return this.data.get(key) ?? null; }
  setItem(key: string, value: string) { this.data.set(key, value); }
  removeItem(key: string) { this.data.delete(key); }
}

function modelWithMenu(storage?: StorageLike, duration = 120_000): RestaurantModel {
  const model = new RestaurantModel({ storage, seed: 1, serviceDurationMs: duration });
  if (storage) model.newRestaurant();
  model.toggleRecipe("roast-potato"); model.toggleRecipe("garden-plate");
  return model;
}
function openService(storage?: StorageLike, withServer = true): RestaurantModel {
  const model = modelWithMenu(storage);
  if (withServer) { model.cashCents = Math.max(model.cashCents, 50_000); model.hireEmployee("server-ada"); model.setEmployeeScheduled("server-ada", true); }
  expect(model.beginPrep()).toBe(true); model.startService(0); return model;
}
function forcedOrder(model: RestaurantModel, recipe: "roast-potato" | "garden-plate" = "roast-potato", now = 1) {
  model.activeOrders = []; const order = model.forceOrder(recipe, now); expect(order).not.toBeNull(); return order!;
}
function deliver(model: RestaurantModel, recipe: "roast-potato" | "garden-plate" = "roast-potato", now = 1) {
  const order = forcedOrder(model, recipe, now); expect(model.revenueCents).toBe(0);
  if (model.activeStaff.some(({ role }) => role === "server")) { expect(model.queueReadyDish(recipe)).toBe(true); model.updateService(now); model.updateService(now + SERVER_DELIVERY_DURATION_MS); }
  else expect(model.deliverDishToTable(recipe, order.tableId, now).some(({ type }) => type === "delivery-complete")).toBe(true);
  return order;
}

describe("Endless persistence and migration", () => {
  it("restores economy, equipment, expansions, reputation, and persistent staff identity", () => {
    const storage = new MemoryStorage(); const model = new RestaurantModel({ storage }); model.newRestaurant(); model.cashCents = 100_000;
    model.purchaseIngredients("potato", 10); model.purchaseAppliance("fryer"); model.removeAppliance(2); model.installAppliance("fryer", 2);
    model.buyKitchenExpansion(); model.buyDiningExpansion(); model.hireEmployee("server-milo"); model.setEmployeeScheduled("server-milo", true); model.reputationPoints = 360; model.save();
    const restored = new RestaurantModel({ storage });
    expect(restored.cashCents).toBe(model.cashCents); expect(restored.inventory.potato).toBe(10); expect(restored.reputationPoints).toBe(360);
    expect(restored.installedSlots[2]).toBe("fryer"); expect(restored.kitchenLevel).toBe(2); expect(restored.diningLevel).toBe(2);
    expect(restored.staffRoster.find(({ id }) => id === "server-milo")).toMatchObject({ name: "Milo", role: "server", scheduled: true });
  });
  it("confirmed reset restores a clean restaurant without free staff", () => {
    const storage = new MemoryStorage(); const model = new RestaurantModel({ storage, startAtLanding: true }); model.newRestaurant(); model.purchaseIngredients("potato", 5);
    expect(model.resetEndlessSave(false)).toBe(false); expect(model.resetEndlessSave(true)).toBe(true); expect(storage.getItem(SAVE_KEY)).toBeNull();
    expect(model.cashCents).toBe(15_000); expect(model.inventory.potato).toBe(0); expect(model.staffRoster).toHaveLength(0);
  });
  it("migrates a version 1 save without corrupting restaurant state", () => {
    const storage = new MemoryStorage(); storage.setItem(SAVE_KEY, JSON.stringify({ version: 1, day: 4, cashCents: 22222, inventory: { potato: 7, tomato: 1, onion: 3, cheese: 2 }, applianceOwnership: { "prep-station": 1, oven: 1, "assembly-station": 1, "plating-station": 1, fryer: 0 }, installedSlots: ["oven", "prep-station", "assembly-station", "plating-station", null, null], kitchenLevel: 1, diningLevel: 2, reputationPoints: 220, selectedRecipeIds: [], selectedAdId: "none", startingCashCents: 22222, ingredientSpendingCents: 0, advertisingSpendingCents: 0, capitalSpendingCents: 0 }));
    const model = new RestaurantModel({ storage }); expect(model.day).toBe(4); expect(model.cashCents).toBe(22222); expect(model.inventory.potato).toBe(7); expect(model.inventory.lettuce).toBe(3); expect(model.diningLevel).toBe(2); expect(model.staffRoster).toHaveLength(0);
    expect(JSON.parse(storage.getItem(SAVE_KEY)!).version).toBe(3);
  });
  it("migrates version 2 lettuce stock and removes only the former free starter server", () => {
    const storage = new MemoryStorage();
    storage.setItem(SAVE_KEY, JSON.stringify({ version: 2, day: 3, cashCents: 30000, inventory: { potato: 2, tomato: 4, onion: 6, cheese: 1 }, applianceOwnership: { "prep-station": 1, oven: 1, "assembly-station": 1, "plating-station": 1, fryer: 0 }, installedSlots: ["oven", "prep-station", "assembly-station", "plating-station", null, null], kitchenLevel: 1, diningLevel: 1, reputationPoints: 50, selectedRecipeIds: [], selectedAdId: "none", startingCashCents: 30000, ingredientSpendingCents: 0, advertisingSpendingCents: 0, capitalSpendingCents: 0, staffRoster: [{ id: "server-ada", name: "Ada", role: "server", color: 1, scheduled: true }, { id: "server-milo", name: "Milo", role: "server", color: 2, scheduled: false }], payrollChargedDay: null }));
    const model = new RestaurantModel({ storage });
    expect(model.inventory.lettuce).toBe(6); expect(model.staffRoster.map(({ id }) => id)).toEqual(["server-milo"]); expect(model.staffRoster[0].scheduled).toBe(false);
  });
});

describe("Bulk supplier and finite kitchen", () => {
  it.each([[1, 0], [4, 0], [5, 500], [9, 500], [10, 1000], [19, 1000], [20, 2000]])("applies the threshold for %i units", (quantity, discountBps) => expect(bulkQuote("potato", quantity).tier.discountBps).toBe(discountBps));
  it("uses integer cents and rejects insufficient cash", () => { expect(bulkQuote("potato", 10)).toMatchObject({ totalCents: 1800, effectiveUnitCents: 180 }); const model = new RestaurantModel(); model.cashCents = 100; expect(model.purchaseIngredients("cheese", 5)).toBe(false); expect(model.inventory.cheese).toBe(0); });
  it("increases stock and charges purchases exactly once", () => { const model = new RestaurantModel(); expect(model.purchaseIngredients("tomato", 5)).toBe(true); expect(model.inventory.tomato).toBe(5); expect(model.cashCents).toBe(14050); });
  it("requires an installed fryer and respects finite slots", () => { const model = new RestaurantModel(); model.purchaseAppliance("fryer"); expect(model.isRecipeAvailable("fries")).toBe(false); expect(model.installAppliance("fryer", 4)).toBe(false); model.removeAppliance(2); model.installAppliance("fryer", 2); expect(model.isRecipeAvailable("fries")).toBe(true); model.removeAppliance(2); expect(model.isRecipeAvailable("fries")).toBe(false); });
  it("persists kitchen expansion capacity", () => { const model = new RestaurantModel(); model.cashCents = 50_000; expect(model.buyKitchenExpansion()).toBe(true); expect(model.kitchenSlotCapacity).toBe(6); });
});

describe("Staff hiring, scheduling, and payroll", () => {
  it("deducts a one-time hiring cost and persists the hire", () => { const storage = new MemoryStorage(); const model = new RestaurantModel({ storage }); model.newRestaurant(); const before = model.cashCents; expect(model.hireEmployee("server-milo")).toBe(true); expect(before - model.cashCents).toBe(10_000); expect(new RestaurantModel({ storage }).staffRoster.some(({ id }) => id === "server-milo")).toBe(true); });
  it("charges scheduled wages exactly once and off employees nothing", () => { const model = modelWithMenu(); model.hireEmployee("server-ada"); model.setEmployeeScheduled("server-ada", true); const before = model.cashCents; expect(model.beginPrep()).toBe(true); expect(before - model.cashCents).toBe(3000); expect(model.payrollSpendingCents).toBe(3000); expect(model.beginPrep()).toBe(false); expect(before - model.cashCents).toBe(3000); });
  it("blocks Prep when scheduled payroll is unaffordable", () => { const model = modelWithMenu(); model.hireEmployee("server-ada"); model.setEmployeeScheduled("server-ada", true); model.cashCents = 2999; expect(model.beginPrep()).toBe(false); expect(model.phase).toBe("planning"); expect(model.lastFeedback).toContain("payroll"); });
  it("does not charge an off employee", () => { const model = modelWithMenu(); model.hireEmployee("server-ada"); const before = model.cashCents; expect(model.beginPrep()).toBe(true); expect(model.cashCents).toBe(before); expect(model.payrollSpendingCents).toBe(0); });
  it("prevents two servers from reserving the same delivery", () => { const model = new RestaurantModel({ seed: 1 }); model.cashCents = 50_000; model.hireEmployee("server-milo"); model.setEmployeeScheduled("server-milo", true); model.toggleRecipe("roast-potato"); model.toggleRecipe("garden-plate"); model.beginPrep(); model.startService(0); forcedOrder(model); model.queueReadyDish("roast-potato"); model.updateService(1); expect(model.activeStaff.filter(({ task }) => task?.type === "deliver")).toHaveLength(1); expect(model.readyDishes[0].claimedBy).not.toBeNull(); });
});

describe("Physical dining and delivery", () => {
  it("self-seats a waiting party when no server is employed and creates a valid ticket", () => { const model = openService(undefined, false); const events = model.updateService(801); expect(model.activeStaff).toHaveLength(0); expect(events.some(({ type }) => type === "order-arrived")).toBe(true); expect(model.activeOrders[0].tableId).toMatch(/^t/); expect(model.selectedRecipeIds).toContain(model.activeOrders[0].recipeId); });
  it("leaves customers waiting when the room is full and expires table patience", () => { const model = openService(); model.diningTables.forEach((table) => { table.state = "waiting_food"; table.customerId = 999; }); model.updateService(801); expect(model.customers.some(({ state }) => state === "waiting_for_table")).toBe(true); model.updateService(TABLE_WAIT_PATIENCE_MS + 1); expect(model.leftWaitingForTable).toBeGreaterThan(0); });
  it("dining expansion adds two real tables and four seats", () => { const model = modelWithMenu(); model.cashCents = 50_000; model.buyDiningExpansion(); model.beginPrep(); model.startService(0); expect(model.diningCapacity).toBe(10); expect(model.diningTables).toHaveLength(5); });
  it("pickup alone earns nothing; delivery pays the correct table", () => { const model = openService(); const order = forcedOrder(model); expect(model.queueReadyDish("roast-potato")).toBe(true); expect(model.revenueCents).toBe(0); model.updateService(1); expect(model.activeStaff[0].task?.targetId).toBe(String(model.readyDishes[0].id)); model.updateService(1 + SERVER_DELIVERY_DURATION_MS); expect(model.activeOrders).toHaveLength(0); expect(model.ordersCompleted).toBe(1); expect(model.revenueCents).toBe(800); expect(model.customers.find(({ id }) => id === order.customerId)?.state).toBe("eating"); });
  it("an incorrect dish is refused and an abandoned customer never pays", () => { const model = openService(); forcedOrder(model, "roast-potato", 1); expect(model.queueReadyDish("garden-plate")).toBe(false); expect(model.revenueCents).toBe(0); model.updateService(ORDER_PATIENCE_MS + 2); expect(model.revenueCents).toBe(0); expect(model.leftWaitingForFood).toBeGreaterThan(0); });
  it("lets a chef deliver to the matching table and carry its dirty plate back", () => { const model = openService(undefined, false); const order = forcedOrder(model); expect(model.deliverDishToTable("garden-plate", order.tableId, 1)).toHaveLength(0); expect(model.deliverDishToTable("roast-potato", order.tableId, 1).some(({ type }) => type === "delivery-complete")).toBe(true); expect(model.revenueCents).toBe(800); model.updateService(1 + EATING_DURATION_MS); expect(model.collectDirtyPlateFromTable(order.tableId)).toBe(true); expect(model.dirtyPlatesInTransit).toBe(1); expect(model.dirtyReturnQueue).toBe(0); expect(model.returnCarriedDirtyPlate()?.type).toBe("dirty-dish-returned"); expect(model.dirtyPlatesInTransit).toBe(0); expect(model.dirtyReturnQueue).toBe(1); });
  it("returns a table to reusable service after eating and server clearing", () => { const model = openService(); const order = deliver(model); const deliveredAt = 1 + SERVER_DELIVERY_DURATION_MS; model.updateService(deliveredAt + EATING_DURATION_MS); const table = model.diningTables.find(({ id }) => id === order.tableId)!; expect(table.state).toBe("dirty"); model.updateService(deliveredAt + EATING_DURATION_MS + SERVER_CLEAR_DURATION_MS); expect(table.state).not.toBe("dirty"); expect(model.dirtyReturnQueue).toBe(1); });
});

describe("Service last call", () => {
  it("keeps service open after the clock ends until the final order expires", () => {
    const model = modelWithMenu(undefined, 1_000); expect(model.beginPrep()).toBe(true); model.startService(0);
    model.customers = []; model.activeOrders = []; model.potentialCustomers = 0; model.arrivals = 0;
    const order = model.forceOrder("roast-potato", 900, 2_000); expect(order).not.toBeNull();
    model.updateService(1_000);
    expect(model.phase).toBe("service"); expect(model.lastCall).toBe(true); expect(model.activeOrders).toHaveLength(1);
    const events = model.updateService(2_901);
    expect(events.map(({ type }) => type)).toEqual(expect.arrayContaining(["order-expired", "service-ended"]));
    expect(model.phase).toBe("summary");
  });

  it("closes immediately at last call when no guests or orders remain", () => {
    const model = modelWithMenu(undefined, 1_000); expect(model.beginPrep()).toBe(true); model.startService(0);
    model.customers = []; model.activeOrders = []; model.potentialCustomers = 0; model.arrivals = 0;
    expect(model.updateService(1_000).some(({ type }) => type === "service-ended")).toBe(true);
    expect(model.phase).toBe("summary");
  });
});

describe("Plate conservation and dishwashing", () => {
  it("dirty plates cannot be used until a human wash restores one", () => { const model = openService(); expect(model.useCleanPlate()).toBe(true); expect(model.platesRemaining).toBe(5); model.dirtyReturnQueue = 1; expect(model.claimDirtyPlate()).toBe(true); expect(model.platesRemaining).toBe(5); expect(model.completePlateWash("human")?.type).toBe("plate-washed"); expect(model.platesRemaining).toBe(6); });
  it("a scheduled dishwasher visibly claims and restores one returned plate", () => { const model = modelWithMenu(); model.cashCents = 50_000; model.hireEmployee("dishwasher-june"); model.setEmployeeScheduled("dishwasher-june", true); model.beginPrep(); model.startService(0); model.platesRemaining = 5; model.dirtyReturnQueue = 1; model.updateService(10); const dishwasher = model.activeStaff.find(({ role }) => role === "dishwasher")!; expect(dishwasher.task?.type).toBe("wash"); expect(model.dirtyReturnQueue).toBe(0); model.updateService(10 + DISHWASH_DURATION_MS); expect(model.platesRemaining).toBe(6); expect(model.dishwasherPlatesWashed).toBe(1); });
  it("conserves the configured total across clean, dirty, and claimed plates", () => { const model = openService(); model.useCleanPlate(); model.dirtyReturnQueue = 1; model.claimDirtyPlate(); expect(model.platesRemaining + model.dirtyReturnQueue + model.claimedDirtyPlates).toBe(6); model.completePlateWash(); expect(model.platesRemaining + model.dirtyReturnQueue + model.claimedDirtyPlates).toBe(6); });
});

describe("Demand, reputation, and persistent economy", () => {
  it("maps reputation and advertising to arrivals with the 50% testing boost", () => { const model = new RestaurantModel(); model.reputationPoints = 220; model.selectAdvertising("campaign"); expect(model.demandPreview()).toMatchObject({ baseline: 13, adBonus: 7, testBonus: 10, potential: 30, capacity: 6, admitted: 30 }); });
  it("expires advertising and bounds reputation after the shift", () => { const model = openService(); model.reputationPoints = 220; model.startingReputationPoints = 220; model.endService(1000); expect(model.selectedAdId).toBe("none"); expect(Math.abs(model.reputationChange)).toBeLessThanOrEqual(20); });
  it("successful visible service can increase reputation", () => { const model = openService(); model.potentialCustomers = 1; deliver(model); model.endService(5000); expect(model.reputationChange).toBeGreaterThan(0); });
  it("table-wait and food-wait failures can reduce reputation", () => { const model = openService(); model.diningTables.forEach((table) => { table.state = "waiting_food"; }); model.updateService(TABLE_WAIT_PATIENCE_MS + 1); model.endService(40_000); expect(model.reputationChange).toBeLessThanOrEqual(0); });
  it("preserves unused ingredients and delivered revenue into the next day", () => { const storage = new MemoryStorage(); const model = modelWithMenu(storage); model.purchaseIngredients("potato", 5); model.beginPrep(); model.startService(0); deliver(model); const cash = model.cashCents; model.endService(5000); model.nextDay(); const restored = new RestaurantModel({ storage }); expect(restored.inventory.potato).toBe(5); expect(restored.cashCents).toBe(cash); expect(restored.day).toBe(2); });
  it("uses the configured generous food patience and never restores ruined stock", () => { const model = openService(); const order = forcedOrder(model, "roast-potato", 1000); expect(order.expiresAt).toBe(1000 + ORDER_PATIENCE_MS); model.inventory.potato = 1; expect(model.takeIngredient("potato")).toBe(true); model.recordWaste(200); expect(model.inventory.potato).toBe(0); expect(model.wastedValueCents).toBe(200); });
});
