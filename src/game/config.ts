export const GAME_WIDTH = 960;
export const GAME_HEIGHT = 600;
export const PLAYER_SPEED = 230;
export const INTERACT_DISTANCE = 78;
export const CATCH_RADIUS = 68;
export const THROW_DURATION_MS = 520;

export type Side = "left" | "right";
export type PotatoState = "raw" | "prepped" | "ruined";

export interface Vec2 { x: number; y: number }

export const SIDE_BOUNDS: Record<Side, { minX: number; maxX: number }> = {
  left: { minX: 48, maxX: 420 },
  right: { minX: 540, maxX: 912 },
};

export const PLAYER_STARTS: Record<Side, Vec2> = {
  left: { x: 190, y: 430 },
  right: { x: 770, y: 430 },
};

export const SOURCE_POS: Vec2 = { x: 120, y: 230 };
export const PREP_POS: Vec2 = { x: 820, y: 185 };
export const DESTINATION_POS: Vec2 = { x: 130, y: 105 };
export const SHARED_POS: Vec2 = { x: 480, y: 305 };
