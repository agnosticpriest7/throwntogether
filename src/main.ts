import Phaser from "phaser";
import "./style.css";
import { GAME_HEIGHT, GAME_WIDTH } from "./game/config";
import { RestaurantModel, type ServiceEvent } from "./game/RestaurantModel";
import { RestaurantUI } from "./game/RestaurantUI";
import { TransferScene } from "./game/TransferScene";
import type { IngredientId, KitchenItem, RecipeId } from "./game/data";

const restaurant = new RestaurantModel();
const scene = new TransferScene(restaurant);

new Phaser.Game({
  type: Phaser.AUTO, parent: "game", width: GAME_WIDTH, height: GAME_HEIGHT,
  backgroundColor: "#202733", scene, render: { antialias: true, pixelArt: false },
  scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH }, input: { gamepad: true },
});

const restaurantUI = new RestaurantUI(restaurant);
scene.attachUI(restaurantUI);

if (import.meta.env.DEV && new URLSearchParams(location.search).has("test")) {
  const harness = document.createElement("nav"); harness.id = "test-harness"; harness.setAttribute("aria-label", "Development scenario controls");
  const scenarios: Array<[string, () => void]> = [
    ["New night", () => { restaurant.resetNight(); restaurantUI.render(); phaseChanged(); }],
    ["Setup Roast + Garden", () => quickSetup(["roast-potato", "garden-plate"], { potato: 4, tomato: 3, onion: 3, cheese: 0 })],
    ["Setup Roast + Cheese", () => quickSetup(["roast-potato", "cheese-bake"], { potato: 4, tomato: 0, onion: 0, cheese: 3 })],
    ["Open service", () => phaseChanged(restaurant.startService(performance.now()))],
    ["P1 take potato", () => actAt(0, 95, 305)],
    ["Give P1 raw potato", () => window.__THROWN_TOGETHER__?.giveIngredient(0, "potato")],
    ["Give P1 chopped potato", () => window.__THROWN_TOGETHER__?.giveIngredient(0, "potato", "chopped")],
    ["Give P2 tomato", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "tomato", "chopped")],
    ["Give P2 onion", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "onion", "chopped")],
    ["Give P2 cheese", () => window.__THROWN_TOGETHER__?.giveIngredient(1, "cheese")],
    ["Give plated Roast", () => window.__THROWN_TOGETHER__?.giveDish(1, "roast-potato", "plated")],
    ["Give plated Garden", () => window.__THROWN_TOGETHER__?.giveDish(1, "garden-plate", "plated")],
    ["Give plated Cheese", () => window.__THROWN_TOGETHER__?.giveDish(1, "cheese-bake", "plated")],
    ["P1 → oven", () => actAt(0, 120, 150)], ["P2 → chop", () => actAt(1, 820, 150)],
    ["P2 → assemble", () => actAt(1, 660, 150)], ["P2 → plate", () => actAt(1, 660, 440)],
    ["P2 → serve", () => actAt(1, 830, 440)], ["P1 → shared", () => actAt(0, 420, 305)],
    ["P2 → shared", () => actAt(1, 540, 305)], ["Ready catch", () => window.__THROWN_TOGETHER__?.setPlayer(1, 690, 305)],
    ["P1 throw", () => window.__THROWN_TOGETHER__?.throw(0)], ["Land throw", () => window.__THROWN_TOGETHER__?.advanceFlight()],
    ["Order Roast", () => restaurant.forceOrder("roast-potato", performance.now())],
    ["Order Garden", () => restaurant.forceOrder("garden-plate", performance.now())],
    ["Order Cheese", () => restaurant.forceOrder("cheese-bake", performance.now())],
    ["Only Roast Order", () => onlyOrder("roast-potato")],
    ["Only Garden Order", () => onlyOrder("garden-plate")],
    ["Only Cheese Order", () => onlyOrder("cheese-bake")],
    ["Expire soon", () => { const first = restaurant.activeOrders[0]; if (first) first.expiresAt = performance.now() - 1; }],
    ["End service", () => window.__THROWN_TOGETHER__?.endService()],
  ];
  scenarios.forEach(([label, run]) => { const button = document.createElement("button"); button.type = "button"; button.textContent = label; button.addEventListener("click", run); harness.append(button); });
  document.querySelector("main")?.append(harness);

  function phaseChanged(events: ServiceEvent[] = []): void { restaurantUI.render(); window.dispatchEvent(new CustomEvent("tt-phase-change", { detail: { events } })); }
  function quickSetup(recipes: [RecipeId, RecipeId], stock: Record<IngredientId, number>): void {
    restaurant.resetNight(); recipes.forEach((id) => restaurant.toggleRecipe(id)); restaurant.beginPurchasing();
    (Object.keys(stock) as IngredientId[]).forEach((id) => { for (let i = 0; i < stock[id]; i += 1) restaurant.purchaseIngredient(id); });
    restaurant.beginPrep(); phaseChanged();
  }
  function actAt(player: 0 | 1, x: number, y: number): void { const api = window.__THROWN_TOGETHER__; api?.setPlayer(player, x, y); api?.interact(player); }
  function onlyOrder(recipeId: RecipeId): void { restaurant.activeOrders.splice(0); restaurant.forceOrder(recipeId, performance.now()); restaurantUI.refresh(); }
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
