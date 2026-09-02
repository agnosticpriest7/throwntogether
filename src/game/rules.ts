import { CATCH_RADIUS, SIDE_BOUNDS, type Side, type Vec2 } from "./config";

export const distance = (a: Vec2, b: Vec2): number => Math.hypot(a.x - b.x, a.y - b.y);

export function throwLanding(side: Side, y: number): Vec2 {
  return {
    x: side === "left" ? 690 : 270,
    y: Math.max(84, Math.min(520, y)),
  };
}

export function canAutoCatch(player: Vec2, landing: Vec2, handsFree: boolean): boolean {
  return handsFree && distance(player, landing) <= CATCH_RADIUS;
}

export function clampToSide(position: Vec2, side: Side): Vec2 {
  const bounds = SIDE_BOUNDS[side];
  return {
    x: Math.max(bounds.minX, Math.min(bounds.maxX, position.x)),
    y: Math.max(72, Math.min(548, position.y)),
  };
}
