import { describe, expect, it } from "vitest";
import { RestaurantModel } from "./RestaurantModel";

function purchasingModel(): RestaurantModel {
  const model = new RestaurantModel({ seed: 1 });
  model.toggleRecipe("roast-potato"); model.toggleRecipe("garden-plate"); model.beginPurchasing();
  return model;
}

function serviceModel(): RestaurantModel {
  const model = purchasingModel(); model.beginPrep(); model.startService(1_000); return model;
}

describe("single-night restaurant rules", () => {
  it("deducts the correct cash when purchasing", () => {
    const model = purchasingModel();
    expect(model.purchaseIngredient("cheese")).toBe(true);
    expect(model.cash).toBe(36); expect(model.ingredientSpending).toBe(4);
  });

  it("refuses purchases without enough cash", () => {
    const model = purchasingModel();
    for (let i = 0; i < 9; i += 1) model.purchaseIngredient("cheese");
    expect(model.cash).toBe(4); expect(model.purchaseIngredient("cheese")).toBe(true);
    expect(model.cash).toBe(0); expect(model.purchaseIngredient("potato")).toBe(false); expect(model.cash).toBe(0);
  });

  it("decrements stock when an ingredient is taken", () => {
    const model = purchasingModel(); model.purchaseIngredient("potato"); model.beginPrep();
    expect(model.takeIngredient("potato")).toBe(true); expect(model.inventory.potato).toBe(0);
  });

  it("does not restore ruined ingredients to stock", () => {
    const model = purchasingModel(); model.purchaseIngredient("potato"); model.beginPrep(); model.takeIngredient("potato");
    model.recordWaste(2); expect(model.inventory.potato).toBe(0); expect(model.wastedValue).toBe(2);
  });

  it("satisfies a compatible order", () => {
    const model = serviceModel(); model.activeOrders = [];
    model.forceOrder("roast-potato", 1_000);
    expect(model.serveDish("roast-potato")).toBe(true);
    expect(model.activeOrders).toHaveLength(0);
  });

  it("does not satisfy an order with an incorrect dish", () => {
    const model = serviceModel(); model.activeOrders = [];
    model.forceOrder("roast-potato", 1_000);
    expect(model.serveDish("garden-plate")).toBe(false);
    expect(model.activeOrders).toHaveLength(1); expect(model.revenue).toBe(0);
  });

  it("adds data-defined revenue for a completed order", () => {
    const model = serviceModel(); model.activeOrders = [];
    model.forceOrder("roast-potato", 1_000); model.serveDish("roast-potato");
    expect(model.revenue).toBe(8); expect(model.ordersCompleted).toBe(1);
  });

  it("expires orders for no revenue", () => {
    const model = serviceModel(); model.activeOrders = [];
    model.forceOrder("garden-plate", 1_000, 100);
    model.updateService(1_101);
    expect(model.ordersMissed).toBe(1); expect(model.revenue).toBe(0);
  });

  it("generates orders only from the selected menu", () => {
    const model = serviceModel();
    for (let now = 20_000; now < 100_000; now += 15_000) model.updateService(now);
    expect(model.activeOrders.every((order) => model.selectedRecipeIds.includes(order.recipeId))).toBe(true);
    expect(model.activeOrders.some((order) => order.recipeId === "cheese-bake")).toBe(false);
  });

  it("calculates final cash without deducting spending twice", () => {
    const model = purchasingModel();
    model.purchaseIngredient("potato"); model.purchaseIngredient("tomato"); model.beginPrep(); model.startService(1_000);
    model.activeOrders = []; model.forceOrder("garden-plate", 1_000); model.serveDish("garden-plate");
    const summary = model.summary();
    expect(summary.ingredientSpending).toBe(4); expect(summary.revenueEarned).toBe(10);
    expect(summary.finalCash).toBe(46);
  });
});
