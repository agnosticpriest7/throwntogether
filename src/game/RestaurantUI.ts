import { INGREDIENTS, RECIPES, selectedIngredientIds, type IngredientId, type RecipeId } from "./data";
import { RestaurantModel, type ServiceEvent } from "./RestaurantModel";

export class RestaurantUI {
  private readonly overlay = document.getElementById("restaurant-overlay")!;
  private readonly hud = document.getElementById("restaurant-hud")!;
  private lastHudRefresh = 0;

  constructor(private readonly model: RestaurantModel) {
    document.getElementById("reset-button")?.addEventListener("click", () => this.playAnotherNight());
    window.addEventListener("tt-restart-night", () => this.playAnotherNight());
    this.render();
  }

  render(forceHud = true): void {
    document.body.dataset.phase = this.model.phase;
    const inKitchen = this.model.phase === "prep" || this.model.phase === "service";
    this.overlay.hidden = inKitchen;
    this.hud.hidden = !inKitchen;
    if (this.model.phase === "menu") this.renderMenu();
    else if (this.model.phase === "purchase") this.renderPurchasing();
    else if (this.model.phase === "summary") this.renderSummary();
    if (inKitchen && forceHud) this.renderHud();
  }

  refresh(now = performance.now()): void {
    if (now - this.lastHudRefresh < 100) return;
    this.lastHudRefresh = now;
    if (this.model.phase === "prep" || this.model.phase === "service") this.renderHud();
    else if (this.model.phase === "summary" && !this.overlay.hidden) this.renderSummary();
  }

  private renderMenu(): void {
    const cards = (Object.values(RECIPES)).map((recipe) => {
      const selected = this.model.selectedRecipeIds.includes(recipe.id);
      const ingredients = recipe.ingredients.map(({ ingredientId }) => `${INGREDIENTS[ingredientId].displayName} $${INGREDIENTS[ingredientId].purchaseCost}`).join(" · ");
      return `<button class="recipe-card${selected ? " is-selected" : ""}" data-recipe="${recipe.id}" aria-pressed="${selected}">
        <span class="recipe-card__icon" style="--recipe-color:#${recipe.color.toString(16).padStart(6, "0")}">${recipe.icon}</span>
        <strong>${recipe.displayName}</strong><span>${ingredients}</span>
        <span>${recipe.steps.length} steps · Sells for <b>$${recipe.sellingPrice}</b></span>
      </button>`;
    }).join("");
    this.overlay.innerHTML = `<div class="flow-panel"><span class="flow-kicker">1 · MENU SELECTION</span><h2>Choose tonight's two dishes</h2>
      <p>Compare ingredients, steps, and selling price. No choice is labelled for you.</p>
      <div class="recipe-grid">${cards}</div><p class="flow-feedback">${this.model.lastFeedback}</p>
      <button class="primary-action" id="continue-purchase" ${this.model.selectedRecipeIds.length !== 2 ? "disabled" : ""}>BUY INGREDIENTS →</button></div>`;
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-recipe]").forEach((button) => button.addEventListener("click", () => {
      this.model.toggleRecipe(button.dataset.recipe as RecipeId); this.render();
    }));
    this.overlay.querySelector<HTMLButtonElement>("#continue-purchase")?.addEventListener("click", () => { if (this.model.beginPurchasing()) this.render(); });
  }

  private renderPurchasing(): void {
    const ingredients = selectedIngredientIds(this.model.selectedRecipeIds).map((id) => {
      const ingredient = INGREDIENTS[id];
      return `<article class="purchase-card"><span class="purchase-card__icon" style="--ingredient-color:#${ingredient.color.toString(16).padStart(6, "0")}">${ingredient.icon}</span>
        <div><strong>${ingredient.displayName}</strong><span>$${ingredient.purchaseCost} each</span></div>
        <div class="quantity-control"><button data-remove="${id}" aria-label="Remove one ${ingredient.displayName}" ${this.model.inventory[id] === 0 ? "disabled" : ""}>−</button>
        <output aria-label="${ingredient.displayName} owned">${this.model.inventory[id]}</output><button data-buy="${id}" aria-label="Buy one ${ingredient.displayName}" ${this.model.cash < ingredient.purchaseCost ? "disabled" : ""}>+</button></div></article>`;
    }).join("");
    this.overlay.innerHTML = `<div class="flow-panel"><div class="flow-title-row"><div><span class="flow-kicker">2 · INGREDIENT PURCHASING</span><h2>Stock tonight's kitchen</h2></div><div class="cash-card"><span>CASH</span><strong>$${this.model.cash}</strong></div></div>
      <p>Choose quantities manually. You may under-buy, over-buy, or keep cash in reserve.</p><div class="purchase-grid">${ingredients}</div>
      <div class="purchase-total"><span>Tonight's spending</span><strong>$${this.model.ingredientSpending}</strong></div><p class="flow-feedback">${this.model.lastFeedback}</p>
      <button class="primary-action" id="enter-prep">ENTER KITCHEN · CLOSED/PREP →</button></div>`;
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-buy]").forEach((button) => button.addEventListener("click", () => {
      if (this.model.purchaseIngredient(button.dataset.buy as IngredientId)) window.dispatchEvent(new Event("tt-purchase"));
      this.render();
    }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-remove]").forEach((button) => button.addEventListener("click", () => { this.model.removePurchasedIngredient(button.dataset.remove as IngredientId); this.render(); }));
    this.overlay.querySelector<HTMLButtonElement>("#enter-prep")?.addEventListener("click", () => {
      if (this.model.beginPrep()) { this.phaseChanged(); this.render(); }
    });
  }

  private renderHud(): void {
    const now = performance.now();
    const remaining = this.model.phase === "service" ? Math.max(0, this.model.serviceDurationMs - (now - this.model.serviceStartedAt)) : this.model.serviceDurationMs;
    const minutes = Math.floor(remaining / 60_000); const seconds = Math.ceil((remaining % 60_000) / 1000).toString().padStart(2, "0");
    const stock = selectedIngredientIds(this.model.selectedRecipeIds).map((id) => `<span><i style="--stock-color:#${INGREDIENTS[id].color.toString(16).padStart(6, "0")}"></i>${INGREDIENTS[id].displayName} ${this.model.inventory[id]}</span>`).join("");
    const menu = this.model.selectedRecipeIds.map((id) => RECIPES[id].displayName).join(" · ");
    const orders = this.model.activeOrders.map((order) => {
      const recipe = RECIPES[order.recipeId]; const patience = Math.max(0, order.expiresAt - now); const percent = Math.max(0, Math.min(100, patience / (order.expiresAt - order.createdAt) * 100));
      return `<article class="ticket"><div><b>${recipe.icon}</b><strong>${recipe.displayName}</strong><span>${Math.ceil(patience / 1000)}s</span></div><i><em style="width:${percent}%"></em></i></article>`;
    }).join("");
    this.hud.innerHTML = `<div class="hud-bar"><strong class="phase-pill ${this.model.phase}">${this.model.phase === "prep" ? "CLOSED · PREP" : "OPEN · SERVICE"}</strong>
      <span class="hud-cash">CASH <b>$${this.model.cash + this.model.revenue}</b></span><span class="hud-menu">${menu}</span>
      <span class="hud-time">${this.model.phase === "prep" ? "UNTIMED" : `${minutes}:${seconds}`}</span></div>
      <div class="hud-stock">${stock}<span>PLATES ${this.model.platesRemaining}</span></div>
      <div class="tickets">${orders || `<span class="tickets-empty">${this.model.phase === "prep" ? "Prep ingredients before opening" : "Waiting for next order…"}</span>`}</div>
      <div class="hud-feedback">${this.model.lastFeedback}</div>
      ${this.model.phase === "prep" ? `<button class="open-action" id="open-restaurant">OPEN RESTAURANT</button>` : ""}`;
    this.hud.querySelector<HTMLButtonElement>("#open-restaurant")?.addEventListener("click", () => {
      const events = this.model.startService(performance.now()); this.phaseChanged(events); this.renderHud();
    });
  }

  private renderSummary(): void {
    const summary = this.model.summary();
    this.overlay.innerHTML = `<div class="flow-panel summary-panel"><span class="flow-kicker">5 · NIGHT SUMMARY</span><h2>Kitchen closed</h2>
      <dl><div><dt>Starting cash</dt><dd>$${summary.startingCash}</dd></div><div><dt>Ingredient spending</dt><dd>−$${summary.ingredientSpending}</dd></div>
      <div><dt>Revenue earned</dt><dd class="positive">+$${summary.revenueEarned}</dd></div><div><dt>Wasted ingredient value</dt><dd>$${summary.wastedIngredientValue}</dd></div>
      <div><dt>Orders completed</dt><dd>${summary.ordersCompleted}</dd></div><div><dt>Orders missed / expired</dt><dd>${summary.ordersMissed}</dd></div>
      <div class="summary-final"><dt>Final cash</dt><dd>$${summary.finalCash}</dd></div></dl>
      <button class="primary-action" id="another-night">PLAY ANOTHER NIGHT</button></div>`;
    this.overlay.querySelector<HTMLButtonElement>("#another-night")?.addEventListener("click", () => this.playAnotherNight());
  }

  private playAnotherNight(): void {
    this.model.resetNight(); this.phaseChanged(); this.render();
  }

  private phaseChanged(events: ServiceEvent[] = []): void {
    window.dispatchEvent(new CustomEvent("tt-phase-change", { detail: { phase: this.model.phase, events } }));
  }
}
