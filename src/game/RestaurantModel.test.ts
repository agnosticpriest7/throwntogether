import { describe, expect, it } from "vitest";
import { RestaurantModel, type StorageLike } from "./RestaurantModel";
import { SAVE_KEY, bulkQuote } from "./data";

class MemoryStorage implements StorageLike {
  data = new Map<string, string>();
  getItem(key: string) { return this.data.get(key) ?? null; }
  setItem(key: string, value: string) { this.data.set(key, value); }
  removeItem(key: string) { this.data.delete(key); }
}

function modelWithMenu(storage?: StorageLike): RestaurantModel {
  const model = new RestaurantModel({ storage, seed: 1, serviceDurationMs: 1000 });
  if (storage) model.newRestaurant();
  model.toggleRecipe("roast-potato"); model.toggleRecipe("garden-plate");
  return model;
}

describe("Endless persistence", () => {
  it("restores cash, inventory, reputation, appliances, slots, and expansions", () => {
    const storage = new MemoryStorage(); const model = new RestaurantModel({ storage }); model.newRestaurant();
    model.cashCents = 100_000; model.purchaseIngredients("potato", 10); model.purchaseAppliance("fryer");
    model.removeAppliance(2); model.installAppliance("fryer", 2); model.buyKitchenExpansion(); model.buyDiningExpansion();
    model.reputationPoints = 360; model.save();
    const restored = new RestaurantModel({ storage });
    expect(restored.cashCents).toBe(model.cashCents); expect(restored.inventory.potato).toBe(10);
    expect(restored.reputationPoints).toBe(360); expect(restored.applianceOwnership.fryer).toBe(1);
    expect(restored.installedSlots[2]).toBe("fryer"); expect(restored.kitchenLevel).toBe(2); expect(restored.diningLevel).toBe(2);
  });

  it("confirmed reset removes the save and restores clean starting state", () => {
    const storage = new MemoryStorage(); const model = new RestaurantModel({ storage, startAtLanding: true }); model.newRestaurant(); model.purchaseIngredients("potato", 5);
    expect(storage.getItem(SAVE_KEY)).not.toBeNull(); expect(model.resetEndlessSave(false)).toBe(false);
    expect(model.resetEndlessSave(true)).toBe(true); expect(storage.getItem(SAVE_KEY)).toBeNull();
    expect(model.cashCents).toBe(15_000); expect(model.inventory.potato).toBe(0); expect(model.day).toBe(1);
  });
});

describe("Bulk supplier", () => {
  it.each([[1, 0], [4, 0], [5, 500], [9, 500], [10, 1000], [19, 1000], [20, 2000]])("applies the threshold for %i units", (quantity, discountBps) => {
    expect(bulkQuote("potato", quantity).tier.discountBps).toBe(discountBps);
  });

  it("calculates total and effective unit cost in integer cents", () => {
    expect(bulkQuote("potato", 10)).toMatchObject({ totalCents: 1800, effectiveUnitCents: 180 });
    expect(bulkQuote("onion", 5)).toMatchObject({ totalCents: 475, effectiveUnitCents: 95 });
  });

  it("rejects insufficient cash without changing inventory", () => {
    const model = new RestaurantModel(); model.cashCents = 100;
    expect(model.purchaseIngredients("cheese", 5)).toBe(false); expect(model.cashCents).toBe(100); expect(model.inventory.cheese).toBe(0);
  });

  it("increases inventory by the purchased quantity", () => {
    const model = new RestaurantModel(); expect(model.purchaseIngredients("tomato", 5)).toBe(true);
    expect(model.inventory.tomato).toBe(5); expect(model.cashCents).toBe(14_050);
  });
});

describe("Finite appliances", () => {
  it("purchases an appliance into owned storage", () => {
    const model = new RestaurantModel(); expect(model.purchaseAppliance("fryer")).toBe(true);
    expect(model.applianceOwnership.fryer).toBe(1); expect(model.storedCount("fryer")).toBe(1);
  });

  it("cannot install beyond kitchen slot capacity", () => {
    const model = new RestaurantModel(); model.purchaseAppliance("prep-station");
    expect(model.installAppliance("prep-station", 4)).toBe(false); expect(model.kitchenSlotCapacity).toBe(4);
  });

  it("requires an installed, not merely owned, fryer for Fries", () => {
    const model = new RestaurantModel(); model.purchaseAppliance("fryer");
    expect(model.isRecipeAvailable("fries")).toBe(false); model.removeAppliance(2); expect(model.installAppliance("fryer", 2)).toBe(true);
    expect(model.isRecipeAvailable("fries")).toBe(true); model.removeAppliance(2); expect(model.isRecipeAvailable("fries")).toBe(false);
  });

  it("kitchen expansion permanently increases active slots", () => {
    const model = new RestaurantModel(); model.cashCents = 50_000;
    expect(model.buyKitchenExpansion()).toBe(true); expect(model.kitchenSlotCapacity).toBe(6);
  });
});

describe("Demand and reputation", () => {
  it("maps reputation points to data-defined baseline demand", () => {
    const model = new RestaurantModel(); model.reputationPoints = 0; expect(model.demandPreview().baseline).toBe(8);
    model.reputationPoints = 220; expect(model.demandPreview().baseline).toBe(13); model.reputationPoints = 1620; expect(model.demandPreview().baseline).toBe(50);
  });

  it("applies ads to one night and caps admissions at dining capacity", () => {
    const model = new RestaurantModel(); model.reputationPoints = 220; model.selectAdvertising("campaign");
    expect(model.demandPreview()).toMatchObject({ baseline: 13, potential: 20, capacity: 10, admitted: 10, turnedAway: 10 });
  });

  it("expires advertising after service", () => {
    const model = modelWithMenu(); model.selectAdvertising("flyers"); model.beginPrep(); model.startService(0); model.endService(1000);
    expect(model.selectedAdId).toBe("none");
  });

  it("bounds daily reputation movement", () => {
    const model = modelWithMenu(); model.reputationPoints = 220; model.startingReputationPoints = 220; model.beginPrep(); model.startService(0); model.endService(1000);
    expect(Math.abs(model.reputationChange)).toBeLessThanOrEqual(20);
  });
});

describe("Persistent economy", () => {
  it("charges ingredient, advertising, and capital purchases once", () => {
    const model = new RestaurantModel(); model.purchaseIngredients("potato", 5); model.selectAdvertising("flyers"); model.selectAdvertising("flyers"); model.purchaseAppliance("prep-station");
    expect(model.ingredientSpendingCents).toBe(950); expect(model.advertisingSpendingCents).toBe(2000); expect(model.capitalSpendingCents).toBe(6000);
    expect(model.cashCents).toBe(6050);
  });

  it("decrements persistent stock and never restores ruined food", () => {
    const model = modelWithMenu(); model.purchaseIngredients("potato", 1); model.beginPrep();
    expect(model.takeIngredient("potato")).toBe(true); model.recordWaste(200);
    expect(model.inventory.potato).toBe(0); expect(model.wastedValueCents).toBe(200);
  });

  it("keeps revenue and unused inventory across days and reloads", () => {
    const storage = new MemoryStorage(); const model = modelWithMenu(storage); model.purchaseIngredients("potato", 5); model.beginPrep(); model.startService(0);
    const recipe = model.activeOrders[0].recipeId; model.serveDish(recipe); const cashAfterSale = model.cashCents; model.endService(1000); model.nextDay();
    expect(model.inventory.potato).toBe(5); expect(model.cashCents).toBe(cashAfterSale); expect(model.day).toBe(2);
    const restored = new RestaurantModel({ storage }); expect(restored.inventory.potato).toBe(5); expect(restored.cashCents).toBe(cashAfterSale); expect(restored.day).toBe(2);
  });
});

describe("Preserved service order rules", () => {
  it("refuses an incorrect dish and pays the data-defined price for a correct dish", () => {
    const model = modelWithMenu(); model.beginPrep(); model.startService(0); model.activeOrders = []; model.ordersGenerated = 0;
    model.forceOrder("roast-potato", 0);
    expect(model.serveDish("garden-plate")).toBe(false); expect(model.activeOrders).toHaveLength(1); expect(model.revenueCents).toBe(0);
    expect(model.serveDish("roast-potato")).toBe(true); expect(model.activeOrders).toHaveLength(0); expect(model.revenueCents).toBe(800);
  });

  it("expires an order without revenue", () => {
    const model = modelWithMenu(); model.beginPrep(); model.startService(0); model.activeOrders = []; model.ordersGenerated = 0;
    model.forceOrder("garden-plate", 0, 100); model.updateService(101);
    expect(model.ordersMissed).toBe(1); expect(model.revenueCents).toBe(0);
  });

  it("generates only selected-menu dishes and never exceeds admitted demand", () => {
    const model = new RestaurantModel({ seed: 3, serviceDurationMs: 120_000 }); model.toggleRecipe("roast-potato"); model.toggleRecipe("garden-plate"); model.beginPrep(); model.startService(0);
    for (let now = 15_000; now < 115_000; now += 15_000) model.updateService(now);
    expect(model.ordersGenerated).toBeLessThanOrEqual(model.admittedCustomers);
    expect(model.activeOrders.every(({ recipeId }) => recipeId === "roast-potato" || recipeId === "garden-plate")).toBe(true);
  });
});
