import { CATCH_RADIUS, type Vec2 } from "./config";
import { KITCHEN_BOUNDS, RESTAURANT_BOUNDS, SERVICE_DOOR } from "./layout";

export const distance = (a: Vec2, b: Vec2): number => Math.hypot(a.x - b.x, a.y - b.y);

export function throwLanding(position: Vec2, facing: Vec2, distancePx = 270): Vec2 {
  const length = Math.hypot(facing.x, facing.y) || 1;
  return clampToKitchen({
    x: position.x + facing.x / length * distancePx,
    y: position.y + facing.y / length * distancePx,
  });
}

export function canAutoCatch(player: Vec2, landing: Vec2, handsFree: boolean): boolean {
  return handsFree && distance(player, landing) <= CATCH_RADIUS;
}

export function clampToKitchen(position: Vec2): Vec2 {
  return {
    x: Math.max(KITCHEN_BOUNDS.minX, Math.min(KITCHEN_BOUNDS.maxX, position.x)),
    y: Math.max(KITCHEN_BOUNDS.minY, Math.min(KITCHEN_BOUNDS.maxY, position.y)),
  };
}

export function clampRestaurantMovement(current: Vec2, position: Vec2): Vec2 {
  const next = {
    x: Math.max(RESTAURANT_BOUNDS.minX, Math.min(RESTAURANT_BOUNDS.maxX, position.x)),
    y: Math.max(RESTAURANT_BOUNDS.minY, Math.min(RESTAURANT_BOUNDS.maxY, position.y)),
  };
  if (next.y >= SERVICE_DOOR.minY && next.y <= SERVICE_DOOR.maxY) return next;
  if (current.x <= SERVICE_DOOR.kitchenX) next.x = Math.min(next.x, SERVICE_DOOR.kitchenX);
  else if (current.x >= SERVICE_DOOR.diningX) next.x = Math.max(next.x, SERVICE_DOOR.diningX);
  else next.x = current.x < (SERVICE_DOOR.kitchenX + SERVICE_DOOR.diningX) / 2 ? SERVICE_DOOR.kitchenX : SERVICE_DOOR.diningX;
  return next;
}
