import { CATCH_RADIUS, type Vec2 } from "./config";
import { KITCHEN_BOUNDS } from "./layout";

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
