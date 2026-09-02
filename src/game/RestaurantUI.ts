import { INGREDIENTS, RECIPES, selectedIngredientIds, type IngredientId, type RecipeId } from "./data";
import { RestaurantModel, type ServiceEvent } from "./RestaurantModel";

export class RestaurantUI {
  private readonly overlay = document.getElementById("restaurant-overlay")!;
  private readonly hud = document.getElementById("restaurant-hud")!;
  private lastHudRefresh = 0;
  private recipeGuideOpen = false;

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
    if (this.model.phase === "prep" || this.model.phase === "service") this.updateHud(now);
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
    const menu = this.model.selectedRecipeIds.map((id) => RECIPES[id].displayName).join(" · ");
    this.hud.innerHTML = `<div class="hud-bar"><strong class="phase-pill ${this.model.phase}">${this.model.phase === "prep" ? "CLOSED · PREP" : "OPEN · SERVICE"}</strong>
      <span class="hud-cash">CASH <b></b></span><span class="hud-menu">${menu}</span><span class="hud-time"></span>
      <button class="hud-recipes-button" id="toggle-recipe-guide" aria-expanded="${this.recipeGuideOpen}">RECIPES</button></div>
      <div class="hud-stock"></div><div class="tickets"></div><div class="hud-feedback"></div>
      ${this.model.phase === "prep" ? `<button class="open-action" id="open-restaurant">OPEN RESTAURANT</button>` : ""}
      <aside class="recipe-guide" aria-label="Tonight's recipe guide" ${this.recipeGuideOpen ? "" : "hidden"}>
        <div class="recipe-guide__header"><div><span>TONIGHT'S MENU</span><strong>How to make each dish</strong></div><button id="close-recipe-guide" aria-label="Close recipe guide">×</button></div>
        <div class="recipe-guide__grid">${this.recipeGuideMarkup()}</div>
      </aside>`;
    this.hud.querySelector<HTMLButtonElement>("#open-restaurant")?.addEventListener("click", () => {
      const events = this.model.startService(performance.now()); this.phaseChanged(events);
    });
    this.hud.querySelector<HTMLButtonElement>("#toggle-recipe-guide")?.addEventListener("click", () => this.toggleRecipeGuide());
    this.hud.querySelector<HTMLButtonElement>("#close-recipe-guide")?.addEventListener("click", () => this.toggleRecipeGuide(false));
    this.updateHud(performance.now());
  }

  private updateHud(now: number): void {
    const remaining = this.model.phase === "service" ? Math.max(0, this.model.serviceDurationMs - (now - this.model.serviceStartedAt)) : this.model.serviceDurationMs;
    const minutes = Math.floor(remaining / 60_000); const seconds = Math.ceil((remaining % 60_000) / 1000).toString().padStart(2, "0");
    const stock = selectedIngredientIds(this.model.selectedRecipeIds).map((id) => `<span><i style="--stock-color:#${INGREDIENTS[id].color.toString(16).padStart(6, "0")}"></i>${INGREDIENTS[id].displayName} ${this.model.inventory[id]}</span>`).join("");
    const orders = this.model.activeOrders.map((order) => {
      const recipe = RECIPES[order.recipeId]; const patience = Math.max(0, order.expiresAt - now); const percent = Math.max(0, Math.min(100, patience / (order.expiresAt - order.createdAt) * 100));
      return `<article class="ticket"><div><b>${recipe.icon}</b><strong>${recipe.displayName}</strong><span>${Math.ceil(patience / 1000)}s</span></div><i><em style="width:${percent}%"></em></i></article>`;
    }).join("");
    const cash = this.hud.querySelector<HTMLElement>(".hud-cash b"); if (cash) cash.textContent = `$${this.model.cash + this.model.revenue}`;
    const time = this.hud.querySelector<HTMLElement>(".hud-time"); if (time) time.textContent = this.model.phase === "prep" ? "UNTIMED" : `${minutes}:${seconds}`;
    const stockPanel = this.hud.querySelector<HTMLElement>(".hud-stock"); if (stockPanel) stockPanel.innerHTML = `${stock}<span>PLATES ${this.model.platesRemaining}</span>`;
    const tickets = this.hud.querySelector<HTMLElement>(".tickets"); if (tickets) tickets.innerHTML = orders || `<span class="tickets-empty">${this.model.phase === "prep" ? "Prep ingredients before opening" : "Waiting for next order…"}</span>`;
    const feedback = this.hud.querySelector<HTMLElement>(".hud-feedback"); if (feedback) feedback.textContent = this.model.lastFeedback;
  }

  private recipeGuideMarkup(): string {
    return this.model.selectedRecipeIds.map((id) => {
      const recipe = RECIPES[id];
      const ingredients = recipe.ingredients.map(({ ingredientId, state }) => `<li>${INGREDIENTS[ingredientId].displayName} <b>${state}</b></li>`).join("");
      const steps = recipe.steps.map((step, index) => `<li><span>${index + 1}</span>${step.label}</li>`).join("");
      return `<article class="recipe-guide__card"><h3><i style="--guide-color:#${recipe.color.toString(16).padStart(6, "0")}">${recipe.icon}</i>${recipe.displayName}</h3>
        <div class="recipe-guide__ingredients"><strong>NEEDED</strong><ul>${ingredients}</ul></div><ol>${steps}</ol><p>Serve for <b>$${recipe.sellingPrice}</b></p></article>`;
    }).join("");
  }

  private toggleRecipeGuide(open = !this.recipeGuideOpen): void {
    this.recipeGuideOpen = open;
    const guide = this.hud.querySelector<HTMLElement>(".recipe-guide"); if (guide) guide.hidden = !open;
    this.hud.querySelector<HTMLButtonElement>("#toggle-recipe-guide")?.setAttribute("aria-expanded", String(open));
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
    this.recipeGuideOpen = false; this.model.resetNight(); this.phaseChanged(); this.render();
  }

  private phaseChanged(events: ServiceEvent[] = []): void {
    window.dispatchEvent(new CustomEvent("tt-phase-change", { detail: { phase: this.model.phase, events } }));
  }
}
