import type { IngredientId } from "./data";
import type { Vec2 } from "./config";

export const KITCHEN_BOUNDS = { minX: 48, maxX: 912, minY: 88, maxY: 548 } as const;
export const RESTAURANT_BOUNDS = { minX: 48, maxX: 1220, minY: 88, maxY: 548 } as const;
export const SERVICE_DOOR = { kitchenX: 912, diningX: 960, minY: 340, maxY: 420 } as const;

export const OPEN_KITCHEN_PLAYER_STARTS: [Vec2, Vec2] = [
  { x: 380, y: 500 },
  { x: 590, y: 500 },
];

export const PANTRY_LAYOUT: Array<{ id: IngredientId; position: Vec2 }> = [
  { id: "potato", position: { x: 88, y: 145 } },
  { id: "tomato", position: { x: 198, y: 145 } },
  { id: "lettuce", position: { x: 88, y: 242 } },
  { id: "cheese", position: { x: 198, y: 242 } },
];

export const ISLAND_COUNTERS = [
  { id: "island-west", position: { x: 355, y: 315 } },
  { id: "island-center", position: { x: 490, y: 315 } },
  { id: "island-east", position: { x: 625, y: 315 } },
] as const;

export const TRASH_POS: Vec2 = { x: 105, y: 470 };
export const SERVICE_PICKUP_POS: Vec2 = { x: 875, y: 305 };
export const DISH_SINK_POS: Vec2 = { x: 820, y: 470 };
export const DIRTY_RETURN_POS: Vec2 = { x: 967, y: 454 };
export const SERVER_STAGING_POS: Vec2 = { x: 965, y: 305 };
export const ENTRANCE_POS: Vec2 = { x: 1202, y: 305 };

export const FIXED_KITCHEN_INTERACTIONS: Vec2[] = [
  ...PANTRY_LAYOUT.map(({ position }) => position),
  ...ISLAND_COUNTERS.map(({ position }) => position),
  TRASH_POS,
  SERVICE_PICKUP_POS,
  DISH_SINK_POS,
];

export function isInsideOpenKitchen(position: Vec2): boolean {
  return position.x >= KITCHEN_BOUNDS.minX && position.x <= KITCHEN_BOUNDS.maxX
    && position.y >= KITCHEN_BOUNDS.minY && position.y <= KITCHEN_BOUNDS.maxY;
}
