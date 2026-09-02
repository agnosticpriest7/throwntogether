import Phaser from "phaser";
import "./style.css";
import { GAME_HEIGHT, GAME_WIDTH } from "./game/config";
import { RestaurantModel, type ServiceEvent } from "./game/RestaurantModel";
import { RestaurantUI } from "./game/RestaurantUI";
import { TransferScene } from "./game/TransferScene";
import type { IngredientId, KitchenItem, RecipeId } from "./game/data";

const restaurant = new RestaurantModel({ storage: window.localStorage, startAtLanding: true });
const scene = new TransferScene(restaurant);

new Phaser.Game({
  type: Phaser.AUTO, parent: "game", width: GAME_WIDTH, height: GAME_HEIGHT,
  backgroundColor: "#202733", scene, render: { antialias: true, pixelArt: false },
  scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH }, input: { gamepad: true },
});

const restaurantUI = new RestaurantUI(restaurant); scene.attachUI(restaurantUI);

if (import.meta.env.DEV && new URLSearchParams(location.search).has("test")) {
  const harness = document.createElement("nav"); harness.id = "test-harness"; harness.setAttribute("aria-label", "Development scenario controls");
  const scenarios: Array<[string, () => void]> = [
    ["New restaurant", () => { restaurant.newRestaurant(); restaurantUI.render(); phaseChanged(); }],
    ["Fund restaurant", () => { restaurant.cashCents = 100_000; restaurant.save(); restaurantUI.render(); }],
    ["Setup Roast + Garden", () => quickSetup(false)], ["Setup Fries + Roast", () => quickSetup(true)],
    ["Open service", () => phaseChanged(restaurant.startService(performance.now()))],
    ["P1 take potato", () => actAt(0, 95, 305)], ["Give P1 raw potato", () => window.__THROWN_TOGETHER__?.giveIngredient(0, "potato")],
    ["Give P1 chopped potato", () => window.__THROWN_TOGETHER__?.giveIngredient(0, "potato", "chopped")],
    ["Give P2 tomato", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "tomato", "chopped")], ["Give P2 onion", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "onion", "chopped")],
    ["Give P2 cheese", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "cheese")],
    ["Give plated Roast", () => window.__THROWN_TOGETHER__?.giveDish(1, "roast-potato", "plated")], ["Give plated Garden", () => window.__THROWN_TOGETHER__?.giveDish(1, "garden-plate", "plated")],
    ["Give plated Cheese", () => window.__THROWN_TOGETHER__?.giveDish(1, "cheese-bake", "plated")], ["Give plated Fries", () => window.__THROWN_TOGETHER__?.giveDish(1, "fries", "plated")],
    ["P1 → slot 1", () => actAt(0, 120, 150)], ["P2 → slot 2", () => actAt(1, 820, 150)], ["P2 → slot 3", () => actAt(1, 660, 150)], ["P2 → slot 4", () => actAt(1, 660, 440)],
    ["P2 → serve", () => actAt(1, 830, 440)], ["P1 → shared", () => actAt(0, 420, 305)], ["P2 → shared", () => actAt(1, 540, 305)],
    ["P1 floor interact", () => actAt(0, 250, 530)], ["P1 → trash", () => actAt(0, 420, 440)],
    ["Ready catch", () => window.__THROWN_TOGETHER__?.setPlayer(1, 690, 305)], ["P1 throw", () => window.__THROWN_TOGETHER__?.throw(0)], ["Land throw", () => window.__THROWN_TOGETHER__?.advanceFlight()],
    ["Only Roast Order", () => onlyOrder("roast-potato")], ["Only Garden Order", () => onlyOrder("garden-plate")], ["Only Fries Order", () => onlyOrder("fries")],
    ["Expire soon", () => { const first = restaurant.activeOrders[0]; if (first) first.expiresAt = performance.now() - 1; }], ["End service", () => window.__THROWN_TOGETHER__?.endService()],
    ["Next day", () => { restaurant.nextDay(); restaurantUI.render(); phaseChanged(); }],
  ];
  scenarios.forEach(([label, run]) => { const button = document.createElement("button"); button.type = "button"; button.textContent = label; button.addEventListener("click", run); harness.append(button); });
  document.querySelector("main")?.append(harness);

  function phaseChanged(events: ServiceEvent[] = []): void { restaurantUI.render(); window.dispatchEvent(new CustomEvent("tt-phase-change", { detail: { events } })); }
  function quickSetup(withFries: boolean): void {
    restaurant.newRestaurant();
    if (withFries) { restaurant.purchaseAppliance("fryer"); restaurant.removeAppliance(2); restaurant.installAppliance("fryer", 2); }
    restaurant.purchaseIngredients("potato", 10); restaurant.purchaseIngredients("tomato", 5); restaurant.purchaseIngredients("onion", 5);
    restaurant.toggleRecipe("roast-potato"); restaurant.toggleRecipe(withFries ? "fries" : "garden-plate"); restaurant.beginPrep(); phaseChanged();
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
