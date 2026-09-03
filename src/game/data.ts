export type IngredientId = "potato" | "tomato" | "onion" | "cheese";
export type RecipeId = "roast-potato" | "garden-plate" | "cheese-bake" | "fries";
export type ItemState = "raw" | "chopped" | "assembled" | "cooked" | "plated" | "ruined";
export type ApplianceId = "prep-station" | "oven" | "assembly-station" | "plating-station" | "fryer";
export type AdId = "none" | "flyers" | "campaign";
export type StaffRoleId = "server" | "dishwasher";

export interface IngredientDefinition {
  id: IngredientId; displayName: string; purchaseCostCents: number; initialState: "raw";
  throwable: boolean; choppable: boolean; color: number; icon: string;
}
export interface RecipeIngredientRequirement { ingredientId: IngredientId; state: "raw" | "chopped" }
export interface RecipeStep { id: string; label: string; station: "chop" | "assemble" | "oven" | "fryer" | "plate" | "serve"; durationMs?: number }
export interface RecipeDefinition {
  id: RecipeId; displayName: string; icon: string; ingredients: RecipeIngredientRequirement[];
  requiredAppliances: ApplianceId[]; steps: RecipeStep[]; finalResult: string;
  sellingPriceCents: number; cookTimeMs?: number; color: number;
}
export interface ApplianceDefinition {
  id: ApplianceId; displayName: string; stationType: "chop" | "assembly" | "oven" | "plate" | "fryer";
  priceCents: number; startingOwned: number; icon: string; color: number; futureUpgrades: string[];
}
export interface BulkTier { minQuantity: number; discountBps: number; label: string }
export interface AdvertisingDefinition { id: AdId; displayName: string; costCents: number; demandBonusBps: number; description: string }
export interface ReputationLevel { level: number; minimumPoints: number; baselineDemand: number }
export interface KitchenSlotDefinition { index: number; x: number; y: number; zone: "north" | "south"; requiredKitchenLevel: number }
export interface StaffRoleDefinition { id: StaffRoleId; displayName: string; hireCostCents: number; wageCents: number; color: number; taskPriority: string[] }
export interface StaffCandidateDefinition { id: string; name: string; role: StaffRoleId; colorVariant: number; startingHired?: boolean; startingScheduled?: boolean }
export interface TableDefinition { id: string; x: number; y: number; seats: 2; requiredDiningLevel: number }
export interface IngredientItem { kind: "ingredient"; ingredientId: IngredientId; state: "raw" | "chopped" | "ruined"; valueCents: number }
export interface DishItem { kind: "dish"; recipeId: RecipeId; state: "assembled" | "cooked" | "plated" | "ruined"; valueCents: number }
export type KitchenItem = IngredientItem | DishItem;

export const CHOP_TIME_MS = 1400;
export const SAVE_VERSION = 2;
export const SAVE_KEY = "thrown-together:endless-save";
export const STARTING_CASH_CENTS = 15_000;
export const STARTING_REPUTATION_POINTS = 0;
export const SERVICE_DURATION_MS = 120_000;
export const ORDER_PATIENCE_MS = 38_500;
export const TABLE_WAIT_PATIENCE_MS = 30_000;
export const EATING_DURATION_MS = 7_000;
export const SERVER_SEAT_DURATION_MS = 2_400;
export const SERVER_DELIVERY_DURATION_MS = 2_200;
export const SERVER_CLEAR_DURATION_MS = 2_600;
export const DISHWASH_DURATION_MS = 2_500;
export const PICKUP_SLOT_COUNT = 3;
export const PLATE_COUNT = 6;

export const INGREDIENTS: Record<IngredientId, IngredientDefinition> = {
  potato: { id: "potato", displayName: "Potato", purchaseCostCents: 200, initialState: "raw", throwable: true, choppable: true, color: 0xc8904f, icon: "P" },
  tomato: { id: "tomato", displayName: "Tomato", purchaseCostCents: 200, initialState: "raw", throwable: true, choppable: true, color: 0xe65b4f, icon: "T" },
  onion: { id: "onion", displayName: "Onion", purchaseCostCents: 100, initialState: "raw", throwable: true, choppable: true, color: 0xc79bd8, icon: "O" },
  cheese: { id: "cheese", displayName: "Cheese", purchaseCostCents: 400, initialState: "raw", throwable: true, choppable: false, color: 0xf1c84b, icon: "C" },
};

export const APPLIANCES: Record<ApplianceId, ApplianceDefinition> = {
  "prep-station": { id: "prep-station", displayName: "Prep Station", stationType: "chop", priceCents: 6000, startingOwned: 1, icon: "╱", color: 0x66b9a8, futureUpgrades: ["Food processor", "Batch prep"] },
  oven: { id: "oven", displayName: "Oven", stationType: "oven", priceCents: 10_000, startingOwned: 1, icon: "▦", color: 0xd47755, futureUpgrades: ["Second rack", "Batch capacity"] },
  "assembly-station": { id: "assembly-station", displayName: "Assembly Station", stationType: "assembly", priceCents: 0, startingOwned: 1, icon: "+", color: 0xe1ad54, futureUpgrades: [] },
  "plating-station": { id: "plating-station", displayName: "Plating Station", stationType: "plate", priceCents: 0, startingOwned: 1, icon: "○", color: 0x7988d9, futureUpgrades: [] },
  fryer: { id: "fryer", displayName: "Fryer", stationType: "fryer", priceCents: 12_000, startingOwned: 0, icon: "≈", color: 0xe3a948, futureUpgrades: ["Second basket", "Batch capacity"] },
};

export const RECIPES: Record<RecipeId, RecipeDefinition> = {
  "roast-potato": {
    id: "roast-potato", displayName: "Roast Potato", icon: "RP", color: 0xd99d52,
    ingredients: [{ ingredientId: "potato", state: "chopped" }], requiredAppliances: ["prep-station", "oven", "plating-station"],
    steps: [{ id: "chop-potato", label: "Chop potato", station: "chop", durationMs: CHOP_TIME_MS }, { id: "bake-potato", label: "Bake potato", station: "oven", durationMs: 5000 }, { id: "plate", label: "Plate", station: "plate" }, { id: "serve", label: "Serve", station: "serve" }],
    finalResult: "Plated Roast Potato", sellingPriceCents: 800, cookTimeMs: 5000,
  },
  "garden-plate": {
    id: "garden-plate", displayName: "Garden Plate", icon: "GP", color: 0x67c79a,
    ingredients: [{ ingredientId: "tomato", state: "chopped" }, { ingredientId: "onion", state: "chopped" }], requiredAppliances: ["prep-station", "plating-station"],
    steps: [{ id: "chop-tomato", label: "Chop tomato", station: "chop", durationMs: CHOP_TIME_MS }, { id: "chop-onion", label: "Chop onion", station: "chop", durationMs: CHOP_TIME_MS }, { id: "combine-plate", label: "Combine on plate", station: "plate" }, { id: "serve", label: "Serve", station: "serve" }],
    finalResult: "Plated Garden Plate", sellingPriceCents: 1000,
  },
  "cheese-bake": {
    id: "cheese-bake", displayName: "Cheese Bake", icon: "CB", color: 0xe4b94e,
    ingredients: [{ ingredientId: "potato", state: "chopped" }, { ingredientId: "cheese", state: "raw" }], requiredAppliances: ["prep-station", "assembly-station", "oven", "plating-station"],
    steps: [{ id: "chop-potato", label: "Chop potato", station: "chop", durationMs: CHOP_TIME_MS }, { id: "assemble", label: "Combine with cheese", station: "assemble" }, { id: "bake", label: "Bake", station: "oven", durationMs: 6500 }, { id: "plate", label: "Plate", station: "plate" }, { id: "serve", label: "Serve", station: "serve" }],
    finalResult: "Plated Cheese Bake", sellingPriceCents: 1500, cookTimeMs: 6500,
  },
  fries: {
    id: "fries", displayName: "Fries", icon: "FR", color: 0xf0bd45,
    ingredients: [{ ingredientId: "potato", state: "chopped" }], requiredAppliances: ["prep-station", "fryer", "plating-station"],
    steps: [{ id: "chop-potato", label: "Chop potato", station: "chop", durationMs: CHOP_TIME_MS }, { id: "fry", label: "Fry", station: "fryer", durationMs: 4500 }, { id: "plate", label: "Plate", station: "plate" }, { id: "serve", label: "Serve", station: "serve" }],
    finalResult: "Plated Fries", sellingPriceCents: 1200, cookTimeMs: 4500,
  },
};

export const BULK_TIERS: BulkTier[] = [
  { minQuantity: 20, discountBps: 2000, label: "20% bulk discount" },
  { minQuantity: 10, discountBps: 1000, label: "10% bulk discount" },
  { minQuantity: 5, discountBps: 500, label: "5% bulk discount" },
  { minQuantity: 1, discountBps: 0, label: "Base price" },
];
export const ADVERTISING: Record<AdId, AdvertisingDefinition> = {
  none: { id: "none", displayName: "No Advertising", costCents: 0, demandBonusBps: 0, description: "+0% demand" },
  flyers: { id: "flyers", displayName: "Local Flyers", costCents: 2000, demandBonusBps: 2500, description: "+25% demand" },
  campaign: { id: "campaign", displayName: "Local Campaign", costCents: 5000, demandBonusBps: 5000, description: "+50% demand" },
};
export const STAFF_ROLES: Record<StaffRoleId, StaffRoleDefinition> = {
  server: { id: "server", displayName: "Server", hireCostCents: 10_000, wageCents: 3_000, color: 0x59b7d9, taskPriority: ["deliver", "seat", "clear", "idle"] },
  dishwasher: { id: "dishwasher", displayName: "Dishwasher", hireCostCents: 12_000, wageCents: 3_500, color: 0x9b86df, taskPriority: ["wash", "idle"] },
};
export const STAFF_CANDIDATES: StaffCandidateDefinition[] = [
  { id: "server-ada", name: "Ada", role: "server", colorVariant: 0, startingHired: true, startingScheduled: true },
  { id: "server-milo", name: "Milo", role: "server", colorVariant: 1 },
  { id: "dishwasher-june", name: "June", role: "dishwasher", colorVariant: 0 },
];
export const TABLES: TableDefinition[] = [
  { id: "t1", x: 1035, y: 145, seats: 2, requiredDiningLevel: 1 },
  { id: "t2", x: 1175, y: 145, seats: 2, requiredDiningLevel: 1 },
  { id: "t3", x: 1105, y: 315, seats: 2, requiredDiningLevel: 1 },
  { id: "t4", x: 1035, y: 475, seats: 2, requiredDiningLevel: 2 },
  { id: "t5", x: 1175, y: 475, seats: 2, requiredDiningLevel: 2 },
];
export const REPUTATION_LEVELS: ReputationLevel[] = [
  { level: 1, minimumPoints: 0, baselineDemand: 8 }, { level: 2, minimumPoints: 100, baselineDemand: 10 },
  { level: 3, minimumPoints: 220, baselineDemand: 13 }, { level: 4, minimumPoints: 360, baselineDemand: 16 },
  { level: 5, minimumPoints: 520, baselineDemand: 20 }, { level: 6, minimumPoints: 700, baselineDemand: 24 },
  { level: 7, minimumPoints: 900, baselineDemand: 29 }, { level: 8, minimumPoints: 1120, baselineDemand: 35 },
  { level: 9, minimumPoints: 1360, baselineDemand: 42 }, { level: 10, minimumPoints: 1620, baselineDemand: 50 },
];
export const KITCHEN_SLOTS: KitchenSlotDefinition[] = [
  { index: 0, x: 330, y: 145, zone: "north", requiredKitchenLevel: 1 },
  { index: 1, x: 660, y: 145, zone: "north", requiredKitchenLevel: 1 },
  { index: 2, x: 495, y: 145, zone: "north", requiredKitchenLevel: 1 },
  { index: 3, x: 825, y: 145, zone: "north", requiredKitchenLevel: 1 },
  { index: 4, x: 450, y: 470, zone: "south", requiredKitchenLevel: 2 },
  { index: 5, x: 615, y: 470, zone: "south", requiredKitchenLevel: 2 },
];
export const KITCHEN_EXPANSION = { id: "kitchen-expansion-1", displayName: "Kitchen Expansion I", costCents: 40_000, fromSlots: 4, toSlots: 6 } as const;
export const DINING_EXPANSION = { id: "dining-expansion-1", displayName: "Dining Expansion I", costCents: 30_000, fromCapacity: 6, toCapacity: 10, fromTables: 3, toTables: 5 } as const;

export const RECIPE_IDS = Object.keys(RECIPES) as RecipeId[];
export const INGREDIENT_IDS = Object.keys(INGREDIENTS) as IngredientId[];
export const APPLIANCE_IDS = Object.keys(APPLIANCES) as ApplianceId[];
export const AD_IDS = Object.keys(ADVERTISING) as AdId[];

export function formatMoney(cents: number): string { const sign = cents < 0 ? "−" : ""; const absolute = Math.abs(cents); return `${sign}$${(absolute / 100).toFixed(absolute % 100 === 0 ? 0 : 2)}`; }
export function ingredientItem(id: IngredientId): IngredientItem { return { kind: "ingredient", ingredientId: id, state: INGREDIENTS[id].initialState, valueCents: INGREDIENTS[id].purchaseCostCents }; }
export function selectedIngredientIds(recipeIds: RecipeId[]): IngredientId[] { return [...new Set(recipeIds.flatMap((id) => RECIPES[id].ingredients.map(({ ingredientId }) => ingredientId)))]; }
export function bulkTierFor(quantity: number): BulkTier { return BULK_TIERS.find((tier) => quantity >= tier.minQuantity) ?? BULK_TIERS[BULK_TIERS.length - 1]; }
export function bulkQuote(ingredientId: IngredientId, quantity: number): { totalCents: number; effectiveUnitCents: number; tier: BulkTier } {
  const tier = bulkTierFor(quantity); if (quantity <= 0) return { totalCents: 0, effectiveUnitCents: 0, tier };
  const totalCents = Math.round(INGREDIENTS[ingredientId].purchaseCostCents * quantity * (10_000 - tier.discountBps) / 10_000);
  return { totalCents, effectiveUnitCents: Math.round(totalCents / quantity), tier };
}
