export type IngredientId = "potato" | "tomato" | "onion" | "cheese";
export type RecipeId = "roast-potato" | "garden-plate" | "cheese-bake";
export type ItemState = "raw" | "chopped" | "assembled" | "cooked" | "plated" | "ruined";

export interface IngredientDefinition {
  id: IngredientId;
  displayName: string;
  purchaseCost: number;
  initialState: "raw";
  throwable: boolean;
  choppable: boolean;
  color: number;
  icon: string;
}

export interface RecipeIngredientRequirement {
  ingredientId: IngredientId;
  state: "raw" | "chopped";
}

export interface RecipeStep {
  id: string;
  label: string;
  station: "chop" | "assemble" | "oven" | "plate" | "serve";
  durationMs?: number;
}

export interface RecipeDefinition {
  id: RecipeId;
  displayName: string;
  icon: string;
  ingredients: RecipeIngredientRequirement[];
  steps: RecipeStep[];
  finalResult: string;
  sellingPrice: number;
  cookTimeMs?: number;
  color: number;
}

export interface IngredientItem {
  kind: "ingredient";
  ingredientId: IngredientId;
  state: "raw" | "chopped" | "ruined";
  value: number;
}

export interface DishItem {
  kind: "dish";
  recipeId: RecipeId;
  state: "assembled" | "cooked" | "plated" | "ruined";
  value: number;
}

export type KitchenItem = IngredientItem | DishItem;

export const CHOP_TIME_MS = 1400;

export const INGREDIENTS: Record<IngredientId, IngredientDefinition> = {
  potato: { id: "potato", displayName: "Potato", purchaseCost: 2, initialState: "raw", throwable: true, choppable: true, color: 0xc8904f, icon: "P" },
  tomato: { id: "tomato", displayName: "Tomato", purchaseCost: 2, initialState: "raw", throwable: true, choppable: true, color: 0xe65b4f, icon: "T" },
  onion: { id: "onion", displayName: "Onion", purchaseCost: 1, initialState: "raw", throwable: true, choppable: true, color: 0xc79bd8, icon: "O" },
  cheese: { id: "cheese", displayName: "Cheese", purchaseCost: 4, initialState: "raw", throwable: true, choppable: false, color: 0xf1c84b, icon: "C" },
};

export const RECIPES: Record<RecipeId, RecipeDefinition> = {
  "roast-potato": {
    id: "roast-potato", displayName: "Roast Potato", icon: "RP", color: 0xd99d52,
    ingredients: [{ ingredientId: "potato", state: "chopped" }],
    steps: [
      { id: "chop-potato", label: "Chop potato", station: "chop", durationMs: CHOP_TIME_MS },
      { id: "bake-potato", label: "Bake potato", station: "oven", durationMs: 5000 },
      { id: "plate", label: "Plate", station: "plate" },
      { id: "serve", label: "Serve", station: "serve" },
    ],
    finalResult: "Plated Roast Potato", sellingPrice: 8, cookTimeMs: 5000,
  },
  "garden-plate": {
    id: "garden-plate", displayName: "Garden Plate", icon: "GP", color: 0x67c79a,
    ingredients: [{ ingredientId: "tomato", state: "chopped" }, { ingredientId: "onion", state: "chopped" }],
    steps: [
      { id: "chop-tomato", label: "Chop tomato", station: "chop", durationMs: CHOP_TIME_MS },
      { id: "chop-onion", label: "Chop onion", station: "chop", durationMs: CHOP_TIME_MS },
      { id: "combine-plate", label: "Combine on plate", station: "plate" },
      { id: "serve", label: "Serve", station: "serve" },
    ],
    finalResult: "Plated Garden Plate", sellingPrice: 10,
  },
  "cheese-bake": {
    id: "cheese-bake", displayName: "Cheese Bake", icon: "CB", color: 0xe4b94e,
    ingredients: [{ ingredientId: "potato", state: "chopped" }, { ingredientId: "cheese", state: "raw" }],
    steps: [
      { id: "chop-potato", label: "Chop potato", station: "chop", durationMs: CHOP_TIME_MS },
      { id: "assemble", label: "Combine with cheese", station: "assemble" },
      { id: "bake", label: "Bake", station: "oven", durationMs: 6500 },
      { id: "plate", label: "Plate", station: "plate" },
      { id: "serve", label: "Serve", station: "serve" },
    ],
    finalResult: "Plated Cheese Bake", sellingPrice: 15, cookTimeMs: 6500,
  },
};

export const RECIPE_IDS = Object.keys(RECIPES) as RecipeId[];
export const INGREDIENT_IDS = Object.keys(INGREDIENTS) as IngredientId[];

export function ingredientItem(id: IngredientId): IngredientItem {
  return { kind: "ingredient", ingredientId: id, state: INGREDIENTS[id].initialState, value: INGREDIENTS[id].purchaseCost };
}

export function selectedIngredientIds(recipeIds: RecipeId[]): IngredientId[] {
  return [...new Set(recipeIds.flatMap((id) => RECIPES[id].ingredients.map((requirement) => requirement.ingredientId)))];
}
