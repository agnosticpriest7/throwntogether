export const GAME_WIDTH = 1280;
export const GAME_HEIGHT = 600;
export const PLAYER_SPEED = 230;
export const INTERACT_DISTANCE = 78;
export const CATCH_RADIUS = 68;
export const THROW_DURATION_MS = 520;

export type PotatoState = "raw" | "prepped" | "ruined";

export interface Vec2 { x: number; y: number }

export const SOURCE_POS: Vec2 = { x: 120, y: 230 };
export const PREP_POS: Vec2 = { x: 820, y: 185 };
export const DESTINATION_POS: Vec2 = { x: 130, y: 105 };
