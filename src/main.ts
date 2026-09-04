import Phaser from "phaser";
import "./style.css";
import { GAME_HEIGHT, GAME_WIDTH } from "./game/config";
import { RestaurantModel, type ServiceEvent } from "./game/RestaurantModel";
import { RestaurantUI } from "./game/RestaurantUI";
import { TransferScene } from "./game/TransferScene";
import { PlayerSession } from "./game/PlayerSession";
import type { IngredientId, KitchenItem, RecipeId } from "./game/data";

// Some TV remotes advertise a coarse pointer without providing a touchscreen.
window.addEventListener("pointerdown", (event) => {
  if (event.pointerType === "touch") document.body.dataset.touchActive = "true";
});
window.addEventListener("gamepadconnected", () => { delete document.body.dataset.touchActive; });

const restaurant = new RestaurantModel({ storage: window.localStorage, startAtLanding: true });
const playerSession = new PlayerSession();
const scene = new TransferScene(restaurant, playerSession);

new Phaser.Game({
  type: Phaser.AUTO, parent: "game", width: GAME_WIDTH, height: GAME_HEIGHT,
  backgroundColor: "#fff1cf", scene, render: { antialias: true, pixelArt: false },
  scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH }, input: { gamepad: true },
});

const restaurantUI = new RestaurantUI(restaurant, playerSession); scene.attachUI(restaurantUI);

if (import.meta.env.DEV && new URLSearchParams(location.search).has("test")) {
  const harness = document.createElement("nav"); harness.id = "test-harness"; harness.setAttribute("aria-label", "Development scenario controls");
  const scenarios: Array<[string, () => void]> = [
    ["New restaurant", () => { restaurant.newRestaurant(); restaurantUI.render(); phaseChanged(); }],
    ["Solo mode", () => playerSession.setMode("solo")], ["Co-op mode", () => playerSession.setMode("coop")],
    ["Fund restaurant", () => { restaurant.cashCents = 100_000; restaurant.save(); restaurantUI.render(); }],
    ["Setup Roast + Garden", () => quickSetup(false)], ["Setup Fries + Roast", () => quickSetup(true)],
    ["Setup with dishwasher", () => staffSetup()],
    ["Open service", () => phaseChanged(restaurant.startService(performance.now()))],
    ["P1 take potato", () => actAt(0, 88, 145)], ["Give P1 raw potato", () => window.__THROWN_TOGETHER__?.giveIngredient(0, "potato")],
    ["Give P1 chopped potato", () => window.__THROWN_TOGETHER__?.giveIngredient(0, "potato", "chopped")],
    ["Give P2 tomato", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "tomato", "chopped")], ["Give P2 lettuce", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "lettuce", "chopped")],
    ["Give P2 cheese", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "cheese")],
    ["Give P1 plated Roast", () => window.__THROWN_TOGETHER__?.giveDish(0, "roast-potato", "plated")], ["Give P1 plated Garden", () => window.__THROWN_TOGETHER__?.giveDish(0, "garden-plate", "plated")],
    ["Give P1 plated Cheese", () => window.__THROWN_TOGETHER__?.giveDish(0, "cheese-bake", "plated")], ["Give P1 plated Fries", () => window.__THROWN_TOGETHER__?.giveDish(0, "fries", "plated")],
    ["P1 → slot 1", () => actAt(0, 330, 145)], ["P1 → slot 2", () => actAt(0, 660, 145)], ["P1 → slot 3", () => actAt(0, 495, 145)], ["P1 → slot 4", () => actAt(0, 825, 145)],
    ["P1 → pickup", () => actAt(0, 875, 305)], ["P1 → Table 1", () => actAt(0, 1035, 145)], ["P1 wash dish", () => actAt(0, 820, 470)], ["P1 → island", () => actAt(0, 355, 315)], ["P2 → island", () => actAt(1, 625, 315)],
    ["P1 floor interact", () => actAt(0, 250, 530)], ["P1 → trash", () => actAt(0, 105, 470)],
    ["Ready island toss", () => { playerSession.setMode("solo"); window.__THROWN_TOGETHER__?.setPlayer(0, 85, 315); }],
    ["Ready catch", () => { playerSession.setMode("coop"); window.__THROWN_TOGETHER__?.setPlayer(0, 355, 315); window.__THROWN_TOGETHER__?.setPlayer(1, 625, 315); }], ["P1 throw", () => window.__THROWN_TOGETHER__?.throw(0)], ["Land throw", () => window.__THROWN_TOGETHER__?.advanceFlight()],
    ["Only Roast Order", () => onlyOrder("roast-potato")], ["Only Garden Order", () => onlyOrder("garden-plate")], ["Only Fries Order", () => onlyOrder("fries")],
    ["Expire soon", () => { const first = restaurant.activeOrders[0]; if (first) first.expiresAt = performance.now() - 1; }], ["End service", () => window.__THROWN_TOGETHER__?.endService()],
    ["Return dirty plate", () => { if (restaurant.platesRemaining > 0) { restaurant.platesRemaining -= 1; restaurant.dirtyReturnQueue += 1; } }],
    ["Next day", () => { restaurant.nextDay(); restaurantUI.render(); phaseChanged(); }],
  ];
  scenarios.forEach(([label, run]) => { const button = document.createElement("button"); button.type = "button"; button.textContent = label; button.addEventListener("click", run); harness.append(button); });
  document.querySelector("main")?.append(harness);

  function phaseChanged(events: ServiceEvent[] = []): void { restaurantUI.render(); window.dispatchEvent(new CustomEvent("tt-phase-change", { detail: { events } })); }
  function quickSetup(withFries: boolean): void {
    restaurant.newRestaurant();
    if (withFries) { restaurant.purchaseAppliance("fryer"); restaurant.removeAppliance(2); restaurant.installAppliance("fryer", 2); }
    restaurant.purchaseIngredients("potato", 10); restaurant.purchaseIngredients("tomato", 5); restaurant.purchaseIngredients("lettuce", 5);
    restaurant.toggleRecipe("roast-potato"); restaurant.toggleRecipe(withFries ? "fries" : "garden-plate"); restaurant.beginPrep(); phaseChanged();
  }
  function staffSetup(): void {
    restaurant.newRestaurant(); restaurant.cashCents = 100_000; restaurant.hireEmployee("dishwasher-june"); restaurant.setEmployeeScheduled("dishwasher-june", true);
    restaurant.purchaseIngredients("potato", 10); restaurant.purchaseIngredients("tomato", 5); restaurant.purchaseIngredients("lettuce", 5);
    restaurant.toggleRecipe("roast-potato"); restaurant.toggleRecipe("garden-plate"); restaurant.beginPrep(); phaseChanged();
  }
  function actAt(player: 0 | 1, x: number, y: number): void { const api = window.__THROWN_TOGETHER__; api?.setPlayer(player, x, y); api?.interact(player); }
  function onlyOrder(recipeId: RecipeId): void { restaurant.activeOrders.splice(0); restaurant.ordersGenerated = 0; restaurant.forceOrder(recipeId, performance.now()); restaurantUI.refresh(performance.now() + 1000); }
}

declare global {
  interface Window {
    __THROWN_TOGETHER__?: {
      snapshot(): object; reset(): void; setPlayer(player: 0 | 1, x: number, y: number, held?: KitchenItem | null): void;
      interact(player: 0 | 1): void; throw(player: 0 | 1): void; advanceFlight(): void;
      giveIngredient(player: 0 | 1, id: IngredientId, state?: "raw" | "chopped"): void;
      giveDish(player: 0 | 1, id: RecipeId, state: "assembled" | "cooked" | "plated"): void; endService(): void;
    };
  }
}
