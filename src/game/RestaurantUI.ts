import {
  ADVERTISING, AD_IDS, APPLIANCES, APPLIANCE_IDS, DINING_EXPANSION, INGREDIENTS, INGREDIENT_IDS,
  KITCHEN_EXPANSION, RECIPES, RECIPE_IDS, STAFF_CANDIDATES, STAFF_ROLES, bulkQuote, formatMoney, selectedIngredientIds,
  type AdId, type ApplianceId, type IngredientId, type RecipeId,
} from "./data";
import { RestaurantModel, type ServiceEvent } from "./RestaurantModel";
import { PlayerSession, type PlayerMode } from "./PlayerSession";

type PlanningSection = "overview" | "pantry" | "supplier" | "kitchen" | "menu" | "staff" | "marketing";
type NavigationDirection = "up" | "down" | "left" | "right";
export type ConfirmationKind = "new-restaurant" | "reset-save";

export interface NavigationRect { left: number; top: number; right: number; bottom: number; }

export function confirmationMarkup(kind: ConfirmationKind): string {
  const resetting = kind === "reset-save";
  return `<section class="in-game-confirmation" role="alertdialog" aria-modal="true" aria-labelledby="confirmation-title" aria-describedby="confirmation-description">
    <div class="confirmation-card"><span class="flow-kicker">CONFIRM ACTION</span><h3 id="confirmation-title">${resetting ? "Reset Endless save?" : "Start a new restaurant?"}</h3>
    <p id="confirmation-description">${resetting ? "This permanently removes the saved restaurant and all progression." : "This overwrites the current Endless restaurant with a fresh Day 1 save."}</p>
    <p class="confirmation-controls">CONTROLLER: ← → CHOOSE · A CONFIRM · B CANCEL</p>
    <div class="confirmation-actions"><button id="confirmation-cancel">CANCEL</button><button class="danger-action" id="confirmation-accept">${resetting ? "RESET SAVE" : "START NEW"}</button></div></div></section>`;
}

export function spatialNavigationTarget(rects: NavigationRect[], currentIndex: number, direction: NavigationDirection): number {
  const current = rects[currentIndex];
  if (!current) return rects.length ? 0 : -1;
  const currentX = (current.left + current.right) / 2; const currentY = (current.top + current.bottom) / 2;
  let bestIndex = currentIndex; let bestScore = Number.POSITIVE_INFINITY;
  rects.forEach((rect, index) => {
    if (index === currentIndex) return;
    const x = (rect.left + rect.right) / 2; const y = (rect.top + rect.bottom) / 2;
    const primary = direction === "down" ? y - currentY : direction === "up" ? currentY - y : direction === "right" ? x - currentX : currentX - x;
    if (primary <= 4) return;
    const secondary = direction === "down" || direction === "up" ? Math.abs(x - currentX) : Math.abs(y - currentY);
    const score = primary * primary + secondary * secondary * 4;
    if (score < bestScore) { bestScore = score; bestIndex = index; }
  });
  return bestIndex;
}

export class RestaurantUI {
  private readonly overlay = document.getElementById("restaurant-overlay")!;
  private readonly hud = document.getElementById("restaurant-hud")!;
  private lastHudRefresh = 0;
  private recipeGuideOpen = false;
  private planningSection: PlanningSection = "overview";
  private readonly supplierQuantities: Record<IngredientId, number> = { potato: 1, tomato: 1, lettuce: 1, cheese: 1 };
  private gamepadNavReadyAt = 0;
  private gamepadConfirmHeld = false;
  private gamepadCancelHeld = false;
  private gamepadTabDirection = 0;
  private gamepadNavigationActive = false;
  private planningFocusKey: string | null = null;
  private pendingConfirmation: ConfirmationKind | null = null;

  constructor(private readonly model: RestaurantModel, private readonly playerSession: PlayerSession) {
    document.getElementById("reset-button")?.addEventListener("click", () => this.restartNight());
    window.addEventListener("tt-restart-night", () => this.restartNight());
    this.playerSession.onChange(() => this.render());
    this.render(); this.pollManagementGamepad();
  }

  render(forceHud = true): void {
    document.body.dataset.phase = this.model.phase;
    document.body.dataset.playerMode = this.playerSession.mode;
    const inKitchen = this.model.phase === "prep" || this.model.phase === "service";
    this.overlay.hidden = inKitchen; this.hud.hidden = !inKitchen;
    if (this.model.phase === "landing") this.renderLanding();
    else if (this.model.phase === "planning") this.renderPlanning();
    else if (this.model.phase === "summary") this.renderSummary();
    if (inKitchen && forceHud) this.renderHud();
  }

  refresh(now = performance.now()): void {
    if (now - this.lastHudRefresh < 100) return; this.lastHudRefresh = now;
    if (this.model.phase === "prep" || this.model.phase === "service") this.updateHud(now);
    else if (this.model.phase === "summary" && !this.overlay.hidden) this.renderSummary();
  }

  private renderLanding(): void {
    this.overlay.innerHTML = `<div class="flow-panel endless-landing"><span class="flow-kicker">OPEN KITCHEN · SOLO OR LOCAL CO-OP</span><h2>Choose how you are cooking</h2>
      <p>Run the full restaurant alone, or add a second local chef for faster parallel play. Both modes use the same Endless restaurant.</p>
      <div class="mode-choice" role="group" aria-label="Human player mode"><button data-player-mode="solo" class="${this.playerSession.mode === "solo" ? "is-selected" : ""}" aria-pressed="${this.playerSession.mode === "solo"}"><strong>SINGLE PLAYER</strong><span>One chef · full kitchen access</span></button><button data-player-mode="coop" class="${this.playerSession.mode === "coop" ? "is-selected" : ""}" aria-pressed="${this.playerSession.mode === "coop"}"><strong>LOCAL CO-OP</strong><span>Two chefs · shared open kitchen</span></button></div>
      <div class="landing-actions"><button class="primary-action" id="new-restaurant">NEW RESTAURANT</button>
      <button id="continue-restaurant" ${this.model.hasSave ? "" : "disabled"}>CONTINUE RESTAURANT</button>
      ${this.model.hasSave ? `<button class="danger-action" id="reset-save">RESET ENDLESS SAVE</button>` : ""}</div>
      <p class="flow-feedback">${this.model.hasSave ? `Saved restaurant · Day ${this.model.day} · ${formatMoney(this.model.cashCents)}` : "No Endless restaurant save found."}</p></div>${this.pendingConfirmation ? confirmationMarkup(this.pendingConfirmation) : ""}`;
    if (this.pendingConfirmation) this.overlay.querySelector<HTMLElement>(".endless-landing")!.inert = true;
    this.bindModeButtons(this.overlay);
    this.overlay.querySelector<HTMLButtonElement>("#new-restaurant")?.addEventListener("click", () => {
      if (this.model.hasSave) { this.openConfirmation("new-restaurant"); return; }
      this.model.newRestaurant(); this.planningSection = "overview"; this.render();
    });
    this.overlay.querySelector<HTMLButtonElement>("#continue-restaurant")?.addEventListener("click", () => { if (this.model.continueRestaurant()) this.render(); });
    this.overlay.querySelector<HTMLButtonElement>("#reset-save")?.addEventListener("click", () => this.openConfirmation("reset-save"));
    this.overlay.querySelector<HTMLButtonElement>("#confirmation-cancel")?.addEventListener("click", () => this.cancelConfirmation());
    this.overlay.querySelector<HTMLButtonElement>("#confirmation-accept")?.addEventListener("click", () => this.acceptConfirmation());
  }

  private renderPlanning(): void {
    const rep = this.model.reputationProgress(); const demand = this.model.demandPreview();
    const tabs: PlanningSection[] = ["overview", "pantry", "supplier", "kitchen", "menu", "staff", "marketing"];
    this.overlay.innerHTML = `<div class="planning-hub"><header class="planning-top"><div><span class="flow-kicker">DAY ${this.model.day} · PLANNING</span><h2>Restaurant Hub</h2></div>
      <div class="planning-stats"><span>CASH <b>${formatMoney(this.model.cashCents)}</b></span><span>REPUTATION <b>LV ${rep.level}</b></span><span>DEMAND <b>${demand.potential}</b></span><span>DINING <b>${demand.capacity}</b></span><button class="player-mode-toggle" data-player-mode="${this.playerSession.mode === "solo" ? "coop" : "solo"}">${this.playerSession.mode === "solo" ? "ADD PLAYER 2" : "USE SOLO MODE"}</button></div></header>
      <nav class="planning-tabs" aria-label="Planning sections">${tabs.map((tab) => `<button data-section="${tab}" class="${tab === this.planningSection ? "is-active" : ""}">${tab.toUpperCase()}</button>`).join("")}</nav>
      <section class="planning-content" aria-label="${this.sectionTitle()} planning section">${this.planningContent()}</section>
      <footer class="planning-footer"><span>${this.model.lastFeedback}</span><button class="primary-action" id="begin-prep" ${this.model.selectedRecipeIds.length !== 2 ? "disabled" : ""}>BEGIN PREP · START/MENU →</button></footer></div>`;
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-section]").forEach((button) => button.addEventListener("click", () => { this.planningSection = button.dataset.section as PlanningSection; this.renderPlanning(); }));
    this.bindModeButtons(this.overlay);
    this.bindPlanningActions();
    this.overlay.querySelector<HTMLButtonElement>("#begin-prep")?.addEventListener("click", () => { if (this.model.beginPrep()) { this.phaseChanged(); this.render(); } else this.renderPlanning(); });
    if (this.gamepadNavigationActive) this.restorePlanningFocus();
  }

  private planningContent(): string {
    if (this.planningSection === "pantry") return this.pantryMarkup();
    if (this.planningSection === "supplier") return this.supplierMarkup();
    if (this.planningSection === "kitchen") return this.kitchenMarkup();
    if (this.planningSection === "menu") return this.menuMarkup();
    if (this.planningSection === "staff") return this.staffMarkup();
    if (this.planningSection === "marketing") return this.marketingMarkup();
    return this.overviewMarkup();
  }
  private sectionTitle(): string { return this.planningSection[0].toUpperCase() + this.planningSection.slice(1); }

  private overviewMarkup(): string {
    const demand = this.model.demandPreview(); const rep = this.model.reputationProgress();
    return `<div class="overview-grid"><article><span>RESTAURANT</span><h3>Day ${this.model.day}</h3><p>Kitchen Level ${this.model.kitchenLevel} · ${this.model.kitchenSlotCapacity} active slots<br>Dining Level ${this.model.diningLevel} · ${this.model.diningCapacity / 2} tables / ${this.model.diningCapacity} seats</p></article>
      <article><span>REPUTATION</span><h3>Level ${rep.level}</h3><p>${rep.points} points · ${rep.nextLevelAt ? `${rep.percent}% to Level ${rep.level + 1}` : "Maximum level"}</p><i><em style="width:${rep.percent}%"></em></i></article>
      <article><span>TONIGHT'S DEMAND</span><h3>${demand.potential} potential guests</h3><p>${demand.baseline} baseline + ${demand.adBonus} advertising + ${demand.testBonus} test boost<br>${demand.capacity} seats · turnover determines capacity</p></article>
      <article><span>PANTRY</span><h3>${INGREDIENT_IDS.reduce((sum, id) => sum + this.model.inventory[id], 0)} ingredients</h3><p>${INGREDIENT_IDS.map((id) => `${INGREDIENTS[id].displayName} ${this.model.inventory[id]}`).join(" · ")}</p></article></div>
      <div class="hub-callout"><strong>Plan in any order.</strong><span>Review stock, configure the kitchen, choose dishes, schedule staff, and set advertising. Tonight's payroll: ${formatMoney(this.model.scheduledPayrollCents)}${this.model.payrollChargedDay === this.model.day ? " · PAID" : ""}.</span></div>`;
  }

  private pantryMarkup(): string {
    return `<div class="section-heading"><div><span class="flow-kicker">PERSISTENT PANTRY</span><h3>Leftovers stay between nights</h3></div><p>No spoilage or stock decay.</p></div><div class="pantry-grid">${INGREDIENT_IDS.map((id) => {
      const item = INGREDIENTS[id]; return `<article><i style="--ingredient-color:#${item.color.toString(16).padStart(6, "0")}">${item.icon}</i><div><strong>${item.displayName}</strong><span>Base value ${formatMoney(item.purchaseCostCents)} each</span></div><b>${this.model.inventory[id]}</b></article>`;
    }).join("")}</div>`;
  }

  private supplierMarkup(): string {
    return `<div class="section-heading"><div><span class="flow-kicker">SUPPLIER</span><h3>Buy tonight or stock up</h3></div><p>Bulk discounts trade flexibility for committed cash.</p></div><div class="supplier-grid">${INGREDIENT_IDS.map((id) => {
      const item = INGREDIENTS[id]; const quantity = this.supplierQuantities[id]; const quote = bulkQuote(id, quantity);
      return `<article class="supplier-card"><div class="supplier-title"><i style="--ingredient-color:#${item.color.toString(16).padStart(6, "0")}">${item.icon}</i><div><strong>${item.displayName}</strong><span>Owned ${this.model.inventory[id]} · Base ${formatMoney(item.purchaseCostCents)}</span></div></div>
        <div class="bulk-controls"><button data-quantity="${id}" data-delta="-1" aria-label="Reduce ${item.displayName} quantity">−</button><output>${quantity}</output><button data-quantity="${id}" data-delta="1" aria-label="Increase ${item.displayName} quantity">+</button><button data-quantity="${id}" data-delta="5">+5</button><button data-quantity="${id}" data-delta="10">+10</button></div>
        <dl><div><dt>Total</dt><dd>${formatMoney(quote.totalCents)}</dd></div><div><dt>Per unit</dt><dd>${formatMoney(quote.effectiveUnitCents)}</dd></div><div><dt>Tier</dt><dd>${quote.tier.label}</dd></div></dl>
        <button class="buy-action" data-buy-ingredient="${id}" ${this.model.cashCents < quote.totalCents ? "disabled" : ""}>BUY ${quantity}</button></article>`;
    }).join("")}</div>`;
  }

  private kitchenMarkup(): string {
    const slots = Array.from({ length: this.model.kitchenSlotCapacity }, (_, index) => {
      const id = this.model.installedSlots[index];
      if (!id) return `<article class="slot-card is-empty"><span>SLOT ${index + 1}</span><strong>EMPTY POSITION</strong><div>${APPLIANCE_IDS.filter((applianceId) => this.model.storedCount(applianceId) > 0).map((applianceId) => `<button data-install="${applianceId}" data-slot="${index}">Install ${APPLIANCES[applianceId].displayName}</button>`).join("") || "Nothing in storage"}</div></article>`;
      return `<article class="slot-card"><span>SLOT ${index + 1}</span><strong>${APPLIANCES[id].icon} ${APPLIANCES[id].displayName}</strong><div><button data-move="${index}" data-direction="-1" ${index === 0 ? "disabled" : ""}>←</button><button data-remove-slot="${index}">STORE</button><button data-move="${index}" data-direction="1" ${index === this.model.kitchenSlotCapacity - 1 ? "disabled" : ""}>→</button></div></article>`;
    }).join("");
    const shopIds: ApplianceId[] = ["prep-station", "oven", "fryer"];
    return `<div class="section-heading"><div><span class="flow-kicker">FINITE KITCHEN</span><h3>${this.model.kitchenSlotCapacity} active positions</h3></div><p>Stored equipment does not enable recipes.</p></div><div class="slot-grid">${slots}</div>
      <h4 class="subheading">STORED APPLIANCES</h4><div class="stored-row">${APPLIANCE_IDS.map((id) => `<span>${APPLIANCES[id].displayName} <b>${this.model.storedCount(id)}</b></span>`).join("")}</div>
      <h4 class="subheading">APPLIANCE SHOP</h4><div class="shop-grid">${shopIds.map((id) => `<article><strong>${id === "prep-station" ? "Extra Prep Station" : id === "oven" ? "Second Oven" : APPLIANCES[id].displayName}</strong><span>${formatMoney(APPLIANCES[id].priceCents)}</span><button data-buy-appliance="${id}" ${this.model.cashCents < APPLIANCES[id].priceCents ? "disabled" : ""}>BUY TO STORAGE</button></article>`).join("")}</div>
      <h4 class="subheading">PERMANENT EXPANSIONS</h4><div class="shop-grid expansions"><article><strong>${KITCHEN_EXPANSION.displayName}</strong><span>${this.model.kitchenLevel >= 2 ? "OWNED · 6 positions" : `${formatMoney(KITCHEN_EXPANSION.costCents)} · 4 → 6 positions`}</span><button data-expansion="kitchen" ${this.model.kitchenLevel >= 2 || this.model.cashCents < KITCHEN_EXPANSION.costCents ? "disabled" : ""}>EXPAND KITCHEN</button></article>
      <article><strong>${DINING_EXPANSION.displayName}</strong><span>${this.model.diningLevel >= 2 ? "OWNED · 5 tables / 10 seats" : `${formatMoney(DINING_EXPANSION.costCents)} · 3 → 5 tables`}</span><button data-expansion="dining" ${this.model.diningLevel >= 2 || this.model.cashCents < DINING_EXPANSION.costCents ? "disabled" : ""}>EXPAND DINING</button></article></div>`;
  }

  private menuMarkup(): string {
    return `<div class="section-heading"><div><span class="flow-kicker">TONIGHT'S MENU · ${this.model.selectedRecipeIds.length}/2</span><h3>Choose around your pantry and kitchen</h3></div><p>Insufficient stock is allowed. Missing installed equipment is not.</p></div><div class="planning-recipe-grid">${RECIPE_IDS.map((id) => {
      const recipe = RECIPES[id]; const selected = this.model.selectedRecipeIds.includes(id); const missing = this.model.missingAppliances(id); const available = missing.length === 0;
      const ingredients = recipe.ingredients.map(({ ingredientId }) => `${INGREDIENTS[ingredientId].displayName} · owned ${this.model.inventory[ingredientId]}`).join("<br>");
      const equipment = recipe.requiredAppliances.map((applianceId) => APPLIANCES[applianceId].displayName).join(" · ");
      return `<button class="planning-recipe-card ${selected ? "is-selected" : ""} ${available ? "" : "is-locked"}" data-recipe="${id}" ${available ? "" : "disabled"} aria-pressed="${selected}"><i style="--recipe-color:#${recipe.color.toString(16).padStart(6, "0")}">${recipe.icon}</i><strong>${recipe.displayName}</strong><span>${ingredients}</span><span>Sells ${formatMoney(recipe.sellingPriceCents)}</span><small>${available ? `Needs ${equipment}` : `Requires installed ${missing.map((applianceId) => APPLIANCES[applianceId].displayName).join(" + ")}`}</small></button>`;
    }).join("")}</div>`;
  }

  private staffMarkup(): string {
    const payroll = this.model.scheduledPayrollCents;
    return `<div class="section-heading"><div><span class="flow-kicker">STAFF ROSTER</span><h3>Hire once, schedule per shift</h3></div><p>Payroll is charged exactly once when Prep begins.</p></div>
      <div class="staff-grid">${STAFF_CANDIDATES.map((candidate) => {
        const role = STAFF_ROLES[candidate.role]; const employee = this.model.staffRoster.find(({ id }) => id === candidate.id);
        return `<article class="staff-card ${employee?.scheduled ? "is-scheduled" : ""}"><i style="--staff-color:#${candidate.colorVariant.toString(16).padStart(6, "0")}">${candidate.name[0]}</i><div><strong>${candidate.name}</strong><span>${role.displayName}</span><small>${candidate.role === "server" ? "Seats guests · delivers food · clears tables" : "Washes returned plates at the sink"}</small></div>
          ${employee ? `<button data-schedule="${employee.id}" data-working="${employee.scheduled ? "false" : "true"}">${employee.scheduled ? `WORKING · ${formatMoney(role.wageCents)}` : `OFF · SCHEDULE ${formatMoney(role.wageCents)}`}</button>` : `<button data-hire="${candidate.id}" ${this.model.cashCents < role.hireCostCents ? "disabled" : ""}>HIRE · ${formatMoney(role.hireCostCents)}</button>`}</article>`;
      }).join("")}</div><div class="payroll-total"><span>TONIGHT'S SCHEDULED PAYROLL</span><b>${formatMoney(payroll)}</b><small>${this.model.payrollChargedDay === this.model.day ? "PAID FOR THIS SHIFT" : payroll > this.model.cashCents ? "Insufficient cash — Prep is blocked" : "Charged when BEGIN PREP is confirmed"}</small></div>`;
  }

  private marketingMarkup(): string {
    const demand = this.model.demandPreview();
    return `<div class="section-heading"><div><span class="flow-kicker">MARKETING</span><h3>Choose your own pressure</h3></div><p>Advertising applies to Day ${this.model.day} only.</p></div><div class="ad-grid">${AD_IDS.map((id) => {
      const ad = ADVERTISING[id]; return `<button class="ad-card ${this.model.selectedAdId === id ? "is-selected" : ""}" data-ad="${id}" aria-pressed="${this.model.selectedAdId === id}"><strong>${ad.displayName}</strong><b>${formatMoney(ad.costCents)}</b><span>${ad.description}</span></button>`;
    }).join("")}</div><div class="demand-preview"><div><span>REPUTATION BASELINE</span><b>${demand.baseline}</b></div><div><span>AD EFFECT</span><b>+${demand.adBonus}</b></div><div><span>TEST BOOST · 50%</span><b>+${demand.testBonus}</b></div><div><span>POTENTIAL ARRIVALS</span><b>${demand.potential}</b></div><div><span>SIMULTANEOUS SEATS</span><b>${demand.capacity}</b></div><div class="${demand.turnedAway ? "warning" : ""}"><span>PRESSURE ABOVE ONE SEATING</span><b>${demand.turnedAway}</b></div></div>`;
  }

  private bindPlanningActions(): void {
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-quantity]").forEach((button) => button.addEventListener("click", () => { const id = button.dataset.quantity as IngredientId; this.supplierQuantities[id] = Math.max(1, this.supplierQuantities[id] + Number(button.dataset.delta)); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-buy-ingredient]").forEach((button) => button.addEventListener("click", () => { const id = button.dataset.buyIngredient as IngredientId; if (this.model.purchaseIngredients(id, this.supplierQuantities[id])) this.purchaseCue(); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-buy-appliance]").forEach((button) => button.addEventListener("click", () => { if (this.model.purchaseAppliance(button.dataset.buyAppliance as ApplianceId)) this.purchaseCue(); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-install]").forEach((button) => button.addEventListener("click", () => { this.model.installAppliance(button.dataset.install as ApplianceId, Number(button.dataset.slot)); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-remove-slot]").forEach((button) => button.addEventListener("click", () => { this.model.removeAppliance(Number(button.dataset.removeSlot)); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-move]").forEach((button) => button.addEventListener("click", () => { const from = Number(button.dataset.move); this.model.moveInstalledAppliance(from, from + Number(button.dataset.direction)); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-expansion]").forEach((button) => button.addEventListener("click", () => { const bought = button.dataset.expansion === "kitchen" ? this.model.buyKitchenExpansion() : this.model.buyDiningExpansion(); if (bought) this.purchaseCue(); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-recipe]").forEach((button) => button.addEventListener("click", () => { this.model.toggleRecipe(button.dataset.recipe as RecipeId); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-ad]").forEach((button) => button.addEventListener("click", () => { const oldSpend = this.model.advertisingSpendingCents; if (this.model.selectAdvertising(button.dataset.ad as AdId) && this.model.advertisingSpendingCents > oldSpend) this.purchaseCue(); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-hire]").forEach((button) => button.addEventListener("click", () => { if (this.model.hireEmployee(button.dataset.hire!)) this.purchaseCue(); this.renderPlanning(); }));
    this.overlay.querySelectorAll<HTMLButtonElement>("[data-schedule]").forEach((button) => button.addEventListener("click", () => { this.model.setEmployeeScheduled(button.dataset.schedule!, button.dataset.working === "true"); this.renderPlanning(); }));
  }

  private renderHud(): void {
    const menu = this.model.selectedRecipeIds.map((id) => RECIPES[id].displayName).join(" · ");
    this.hud.innerHTML = `<div class="hud-bar"><strong class="phase-pill ${this.model.phase}">${this.model.phase === "prep" ? "CLOSED · PREP" : "OPEN · SERVICE"}</strong><span class="hud-day">DAY ${this.model.day}</span>
      <span class="hud-cash">CASH <b></b></span><span class="hud-menu">${menu}</span><span class="hud-time"></span><span class="hud-player-mode">${this.playerSession.mode === "solo" ? "SOLO" : "2 CHEFS"}</span>${this.playerSession.mode === "solo" ? `<button class="hud-join-button" data-player-mode="coop">ADD P2</button>` : ""}<button class="hud-recipes-button" id="toggle-recipe-guide" aria-expanded="${this.recipeGuideOpen}">RECIPES</button></div>
      <div class="hud-stock"></div><div class="tickets"></div><div class="hud-feedback"></div>${this.model.phase === "prep" ? `<button class="open-action" id="open-restaurant">OPEN RESTAURANT · START/MENU</button>` : ""}
      <aside class="recipe-guide" aria-label="Tonight's recipe guide" ${this.recipeGuideOpen ? "" : "hidden"}><div class="recipe-guide__header"><div><span>TONIGHT'S MENU</span><strong>How to make each dish</strong></div><button id="close-recipe-guide" aria-label="Close recipe guide">×</button></div><div class="recipe-guide__grid">${this.recipeGuideMarkup()}</div></aside>`;
    this.hud.querySelector<HTMLButtonElement>("#open-restaurant")?.addEventListener("click", () => { const events = this.model.startService(performance.now()); this.phaseChanged(events); });
    this.hud.querySelector<HTMLButtonElement>("#toggle-recipe-guide")?.addEventListener("click", () => this.toggleRecipeGuide());
    this.hud.querySelector<HTMLButtonElement>("#close-recipe-guide")?.addEventListener("click", () => this.toggleRecipeGuide(false));
    this.hud.querySelector<HTMLButtonElement>(".hud-join-button")?.addEventListener("click", () => this.playerSession.setMode("coop"));
    this.updateHud(performance.now());
  }

  private updateHud(now: number): void {
    const remaining = this.model.phase === "service" ? Math.max(0, this.model.serviceDurationMs - (now - this.model.serviceStartedAt)) : this.model.serviceDurationMs;
    const minutes = Math.floor(remaining / 60_000); const seconds = Math.ceil((remaining % 60_000) / 1000).toString().padStart(2, "0");
    const stock = selectedIngredientIds(this.model.selectedRecipeIds).map((id) => `<span><i style="--stock-color:#${INGREDIENTS[id].color.toString(16).padStart(6, "0")}"></i>${INGREDIENTS[id].displayName} ${this.model.inventory[id]}</span>`).join("");
    const orders = this.model.activeOrders.map((order) => { const recipe = RECIPES[order.recipeId]; const patience = Math.max(0, order.expiresAt - now); const percent = Math.max(0, Math.min(100, patience / (order.expiresAt - order.createdAt) * 100)); return `<article class="ticket"><div><b>${recipe.icon}</b><strong>${recipe.displayName}<small> · TABLE ${order.tableId.slice(1)}</small></strong><span>${Math.ceil(patience / 1000)}s</span></div><i><em style="width:${percent}%"></em></i></article>`; }).join("");
    this.setText(".phase-pill", this.model.phase === "prep" ? "CLOSED · PREP" : this.model.lastCall ? "CLOSED · LAST CALL" : "OPEN · SERVICE");
    this.setText(".hud-cash b", formatMoney(this.model.cashCents)); this.setText(".hud-time", this.model.phase === "prep" ? "UNTIMED" : this.model.lastCall ? "LAST CALL" : `${minutes}:${seconds}`);
    const stockPanel = this.hud.querySelector<HTMLElement>(".hud-stock"); if (stockPanel) stockPanel.innerHTML = `${stock}<span>CLEAN ${this.model.platesRemaining}</span><span>DIRTY ${this.model.dirtyReturnQueue + this.model.claimedDirtyPlates + this.model.dirtyPlatesInTransit}</span><span>PICKUP ${this.model.readyDishes.length}/3</span><span>ARRIVALS ${this.model.arrivals}/${this.model.potentialCustomers}</span>`;
    const tickets = this.hud.querySelector<HTMLElement>(".tickets"); if (tickets) tickets.innerHTML = orders || `<span class="tickets-empty">${this.model.phase === "prep" ? "Prep ingredients before opening" : this.model.lastCall ? "Last call · finishing guests already inside" : this.model.arrivals >= this.model.potentialCustomers ? "All guests have arrived" : "Waiting for a party to be seated…"}</span>`;
    this.setText(".hud-feedback", this.model.lastFeedback);
  }

  private recipeGuideMarkup(): string {
    return this.model.selectedRecipeIds.map((id) => { const recipe = RECIPES[id]; const ingredients = recipe.ingredients.map(({ ingredientId, state }) => `<li>${INGREDIENTS[ingredientId].displayName} <b>${state}</b></li>`).join(""); const steps = recipe.steps.map((step, index) => `<li><span>${index + 1}</span>${step.label}</li>`).join(""); return `<article class="recipe-guide__card"><h3><i style="--guide-color:#${recipe.color.toString(16).padStart(6, "0")}">${recipe.icon}</i>${recipe.displayName}</h3><div class="recipe-guide__ingredients"><strong>NEEDED</strong><ul>${ingredients}</ul></div><ol>${steps}</ol><p>Serve for <b>${formatMoney(recipe.sellingPriceCents)}</b></p></article>`; }).join("");
  }
  private toggleRecipeGuide(open = !this.recipeGuideOpen): void { this.recipeGuideOpen = open; const guide = this.hud.querySelector<HTMLElement>(".recipe-guide"); if (guide) guide.hidden = !open; this.hud.querySelector<HTMLButtonElement>("#toggle-recipe-guide")?.setAttribute("aria-expanded", String(open)); }

  private renderSummary(): void {
    const summary = this.model.summary(); const inventory = INGREDIENT_IDS.map((id) => `<span>${INGREDIENTS[id].displayName} <b>${summary.remainingInventory[id]}</b></span>`).join("");
    this.overlay.innerHTML = `<div class="flow-panel endless-summary"><span class="flow-kicker">DAY ${summary.day} · NIGHT SUMMARY</span><h2>The restaurant carries on</h2><div class="summary-columns">
      <section><h3>CUSTOMERS + DINING</h3><dl><div><dt>Potential / arrivals</dt><dd>${summary.potentialCustomers} / ${summary.arrivals}</dd></div><div><dt>Guests seated</dt><dd>${summary.seatedCustomers}</dd></div><div><dt>Meals delivered</dt><dd>${summary.mealsDelivered}</dd></div><div><dt>Left waiting for table</dt><dd>${summary.leftWaitingForTable}</dd></div><div><dt>Left waiting for food</dt><dd>${summary.leftWaitingForFood}</dd></div><div><dt>Peak seats occupied</dt><dd>${summary.peakSeatsOccupied}</dd></div><div><dt>Table turns</dt><dd>${summary.tableTurns}</dd></div></dl></section>
      <section><h3>FINANCIALS</h3><dl><div><dt>Starting cash</dt><dd>${formatMoney(summary.startingCashCents)}</dd></div><div><dt>Ingredient spending</dt><dd>−${formatMoney(summary.ingredientSpendingCents)}</dd></div><div><dt>Advertising spending</dt><dd>−${formatMoney(summary.advertisingSpendingCents)}</dd></div><div><dt>Payroll</dt><dd>−${formatMoney(summary.payrollCents)}</dd></div><div><dt>Capital spending</dt><dd>−${formatMoney(summary.capitalSpendingCents)}</dd></div><div><dt>Revenue</dt><dd class="positive">+${formatMoney(summary.revenueCents)}</dd></div><div><dt>Wasted value</dt><dd>${formatMoney(summary.wastedIngredientValueCents)}</dd></div><div class="summary-final"><dt>Ending cash</dt><dd>${formatMoney(summary.endingCashCents)}</dd></div></dl></section>
      <section><h3>INVENTORY</h3><div class="summary-inventory">${inventory}</div><p>Remaining base value <b>${formatMoney(summary.remainingInventoryValueCents)}</b></p></section>
      <section><h3>STAFF + REPUTATION</h3><p>${summary.scheduledEmployees.join(" · ") || "No employees scheduled"}<br>${summary.serverDeliveries} deliveries · ${summary.tablesCleared} tables cleared · ${summary.dishwasherPlatesWashed} plates AI-washed</p><p>Started Level ${summary.startingReputation.level} · ${summary.startingReputation.points} pts</p><strong class="reputation-change ${summary.reputationChange >= 0 ? "positive" : "negative"}">${summary.reputationChange >= 0 ? "+" : ""}${summary.reputationChange} reputation</strong><p>Now Level ${summary.endingReputation.level} · ${summary.endingReputation.points} pts<br>${summary.endingReputation.nextLevelAt ? `${summary.endingReputation.percent}% to next level` : "Maximum level"}</p></section>
      </div><button class="primary-action" id="next-day">NEXT DAY →</button></div>`;
    this.overlay.querySelector<HTMLButtonElement>("#next-day")?.addEventListener("click", () => { if (this.model.nextDay()) { this.planningSection = "overview"; this.phaseChanged(); this.render(); } });
  }

  private restartNight(): void { if (this.model.restartNight()) { this.recipeGuideOpen = false; this.planningSection = "overview"; this.phaseChanged(); this.render(); } }
  private openConfirmation(kind: ConfirmationKind): void {
    this.pendingConfirmation = kind; this.renderLanding();
    const cancel = this.overlay.querySelector<HTMLButtonElement>("#confirmation-cancel");
    if (cancel) { this.gamepadNavigationActive = true; this.focusButton(cancel); }
  }
  private cancelConfirmation(): void { this.pendingConfirmation = null; this.renderLanding(); }
  private acceptConfirmation(): void {
    const kind = this.pendingConfirmation; this.pendingConfirmation = null;
    if (kind === "new-restaurant") { this.model.newRestaurant(); this.planningSection = "overview"; this.render(); return; }
    if (kind === "reset-save") { this.model.resetEndlessSave(true); this.render(); }
  }
  private bindModeButtons(root: ParentNode): void {
    root.querySelectorAll<HTMLButtonElement>("[data-player-mode]").forEach((button) => button.addEventListener("click", () => this.playerSession.setMode(button.dataset.playerMode as PlayerMode)));
  }
  private purchaseCue(): void { window.dispatchEvent(new Event("tt-purchase")); }
  private navigationKey(button: HTMLButtonElement): string {
    if (button.dataset.section) return `section:${button.dataset.section}`;
    if (button.id) return `id:${button.id}`;
    const data = Object.entries(button.dataset).sort(([a], [b]) => a.localeCompare(b)).map(([key, value]) => `${key}:${value}`).join("|");
    return data || `text:${button.textContent?.trim() ?? ""}`;
  }
  private visibleButtons(): HTMLButtonElement[] {
    const scope = this.pendingConfirmation ? this.overlay.querySelector(".in-game-confirmation") ?? this.overlay : this.overlay;
    return [...scope.querySelectorAll<HTMLButtonElement>("button:not(:disabled)")].filter((button) => button.offsetParent !== null);
  }
  private focusButton(button: HTMLButtonElement): void {
    this.overlay.querySelectorAll(".controller-focused").forEach((element) => element.classList.remove("controller-focused"));
    button.classList.add("controller-focused");
    button.focus();
    if (this.model.phase === "planning") this.planningFocusKey = this.navigationKey(button);
    button.scrollIntoView({ block: "nearest", inline: "nearest" });
  }
  private restorePlanningFocus(): void {
    const buttons = this.visibleButtons();
    const key = this.planningFocusKey ?? `section:${this.planningSection}`;
    const target = buttons.find((button) => this.navigationKey(button) === key)
      ?? buttons.find((button) => button.dataset.section === this.planningSection);
    if (target) this.focusButton(target);
  }
  private switchPlanningSection(direction: number): void {
    const sections: PlanningSection[] = ["overview", "pantry", "supplier", "kitchen", "menu", "staff", "marketing"];
    const nextIndex = (sections.indexOf(this.planningSection) + direction + sections.length) % sections.length;
    this.planningSection = sections[nextIndex]; this.planningFocusKey = `section:${this.planningSection}`;
    this.renderPlanning();
  }
  private moveManagementFocus(direction: NavigationDirection): void {
    const buttons = this.visibleButtons(); if (!buttons.length) return;
    let currentIndex = buttons.indexOf(document.activeElement as HTMLButtonElement);
    if (currentIndex < 0 && this.model.phase === "planning") currentIndex = buttons.findIndex((button) => button.dataset.section === this.planningSection);
    if (currentIndex < 0) currentIndex = 0;
    if (this.model.phase === "planning") {
      const targetIndex = spatialNavigationTarget(buttons.map((button) => button.getBoundingClientRect()), currentIndex, direction);
      this.focusButton(buttons[targetIndex]);
    } else {
      const delta = direction === "up" || direction === "left" ? -1 : 1;
      this.focusButton(buttons[(currentIndex + delta + buttons.length) % buttons.length]);
    }
  }
  private pollManagementGamepad(): void {
    const tick = (now: number) => {
      if (this.model.phase === "landing" || this.model.phase === "planning" || this.model.phase === "summary") {
        const pad = [...(navigator.getGamepads?.() ?? [])].find((candidate) => candidate?.connected);
        if (pad) {
          const vertical = (pad.axes[1] ?? 0) < -0.6 || pad.buttons[12]?.pressed ? "up" : (pad.axes[1] ?? 0) > 0.6 || pad.buttons[13]?.pressed ? "down" : null;
          const horizontal = (pad.axes[0] ?? 0) < -0.6 || pad.buttons[14]?.pressed ? "left" : (pad.axes[0] ?? 0) > 0.6 || pad.buttons[15]?.pressed ? "right" : null;
          const tabDirection = (pad.buttons[6]?.pressed || (pad.buttons[6]?.value ?? 0) > 0.5) ? -1 : (pad.buttons[7]?.pressed || (pad.buttons[7]?.value ?? 0) > 0.5) ? 1 : 0;
          if (tabDirection && !this.gamepadTabDirection) {
            this.gamepadNavigationActive = true;
            if (this.pendingConfirmation) this.moveManagementFocus(tabDirection < 0 ? "left" : "right");
            else if (this.model.phase === "planning") this.switchPlanningSection(tabDirection);
          }
          this.gamepadTabDirection = tabDirection;
          const direction = vertical ?? horizontal;
          if (direction && now >= this.gamepadNavReadyAt) {
            this.gamepadNavigationActive = true; this.moveManagementFocus(direction); this.gamepadNavReadyAt = now + 180;
          }
          const confirm = Boolean(pad.buttons[0]?.pressed);
          if (confirm && !this.gamepadConfirmHeld) {
            const focused = document.activeElement as HTMLButtonElement | null;
            if (focused?.tagName === "BUTTON") { this.gamepadNavigationActive = true; this.planningFocusKey = this.navigationKey(focused); focused.click(); }
          }
          this.gamepadConfirmHeld = confirm;
          const cancel = Boolean(pad.buttons[1]?.pressed);
          if (cancel && !this.gamepadCancelHeld && this.pendingConfirmation) this.cancelConfirmation();
          this.gamepadCancelHeld = cancel;
        } else { this.gamepadConfirmHeld = false; this.gamepadCancelHeld = false; this.gamepadTabDirection = 0; }
      }
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  }
  private setText(selector: string, text: string): void { const element = this.hud.querySelector<HTMLElement>(selector); if (element) element.textContent = text; }
  private phaseChanged(events: ServiceEvent[] = []): void { window.dispatchEvent(new CustomEvent("tt-phase-change", { detail: { phase: this.model.phase, events } })); }
}
