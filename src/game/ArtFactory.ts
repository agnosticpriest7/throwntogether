import Phaser from "phaser";
import type { KitchenItem } from "./data";

export const ART_PALETTE = {
  ink: 0x49352d,
  cream: 0xfff1cf,
  paper: 0xfff8e7,
  blueFloor: 0x78afd5,
  blueTile: 0x91c3e2,
  coralFloor: 0xdd7770,
  coralTile: 0xeb9188,
  wood: 0xa9673d,
  woodLight: 0xc98450,
  woodDark: 0x70452f,
  green: 0x6fa447,
  steel: 0xbac6cd,
  steelDark: 0x6f7f89,
  shadow: 0x3d2e29,
} as const;

export type CharacterRole = "chef" | "server" | "dishwasher" | "customer";

export function characterParts(scene: Phaser.Scene, color: number, role: CharacterRole): Phaser.GameObjects.GameObject[] {
  const g = scene.add.graphics();
  g.fillStyle(ART_PALETTE.shadow, 0.2).fillEllipse(0, 17, 48, 18);
  g.fillStyle(ART_PALETTE.ink).fillRoundedRect(-15, 19, 11, 13, 5).fillRoundedRect(4, 19, 11, 13, 5);
  g.lineStyle(3, ART_PALETTE.ink, 1).fillStyle(color).fillRoundedRect(-22, -3, 44, 34, 15).strokeRoundedRect(-22, -3, 44, 34, 15);
  if (role !== "customer") {
    g.fillStyle(ART_PALETTE.cream).fillRoundedRect(-14, 3, 28, 23, 8);
    g.lineStyle(2, ART_PALETTE.ink, 0.7).strokeRoundedRect(-14, 3, 28, 23, 8);
  }
  g.fillStyle(color).fillCircle(-22, 7, 8).fillCircle(22, 7, 8);
  g.lineStyle(3, ART_PALETTE.ink, 1).strokeCircle(-22, 7, 8).strokeCircle(22, 7, 8);
  const skin = role === "server" ? 0xc98662 : role === "dishwasher" ? 0xe0a27b : 0xf0b78d;
  g.fillStyle(skin).fillCircle(0, -21, 23); g.lineStyle(3, ART_PALETTE.ink, 1).strokeCircle(0, -21, 23);
  g.fillStyle(ART_PALETTE.ink).fillCircle(-8, -23, 3).fillCircle(8, -23, 3);
  g.lineStyle(2, ART_PALETTE.ink, 1).arc(0, -15, 6, 0.15, Math.PI - 0.15);
  if (role === "chef") {
    g.fillStyle(ART_PALETTE.paper).fillCircle(-12, -44, 12).fillCircle(0, -50, 14).fillCircle(13, -44, 12).fillRoundedRect(-21, -44, 42, 15, 6);
    g.lineStyle(3, ART_PALETTE.ink, 1).strokeCircle(-12, -44, 12).strokeCircle(0, -50, 14).strokeCircle(13, -44, 12).strokeRoundedRect(-21, -44, 42, 15, 6);
  } else if (role === "server") {
    g.fillStyle(0x563c35).fillRoundedRect(-22, -44, 44, 15, 8);
    g.fillStyle(ART_PALETTE.paper).fillCircle(0, 10, 3);
  } else if (role === "dishwasher") {
    g.fillStyle(0x3d6f80).fillRoundedRect(-20, -43, 40, 12, 6);
  }
  return [g];
}

export function drawMiniCharacter(
  g: Phaser.GameObjects.Graphics,
  x: number,
  y: number,
  color: number,
  role: CharacterRole,
  variation = 0,
  mood: "neutral" | "happy" | "unhappy" = "neutral",
): void {
  const skinTones = [0xf0b78d, 0xc98662, 0x8d5c43, 0xe1a477];
  const hair = [0x563c35, 0xd3a340, 0x2e2524, 0x9b5439];
  g.fillStyle(ART_PALETTE.shadow, 0.18).fillEllipse(x, y + 13, 30, 11);
  g.fillStyle(ART_PALETTE.ink).fillRoundedRect(x - 10, y + 10, 8, 10, 4).fillRoundedRect(x + 2, y + 10, 8, 10, 4);
  g.fillStyle(color).fillRoundedRect(x - 14, y - 3, 28, 24, 10); g.lineStyle(2, ART_PALETTE.ink, 1).strokeRoundedRect(x - 14, y - 3, 28, 24, 10);
  if (role !== "customer") g.fillStyle(ART_PALETTE.cream).fillRoundedRect(x - 9, y + 2, 18, 15, 5);
  g.fillStyle(skinTones[variation % skinTones.length]).fillCircle(x, y - 13, 15); g.lineStyle(2, ART_PALETTE.ink, 1).strokeCircle(x, y - 13, 15);
  g.fillStyle(hair[variation % hair.length]).fillRoundedRect(x - 14, y - 27, 28, 10, 7);
  g.fillStyle(ART_PALETTE.ink).fillCircle(x - 5, y - 14, 2).fillCircle(x + 5, y - 14, 2);
  g.lineStyle(1.5, ART_PALETTE.ink, 1);
  if (mood === "happy") g.arc(x, y - 8, 5, 0.1, Math.PI - 0.1);
  else if (mood === "unhappy") g.arc(x, y - 4, 5, Math.PI + 0.1, Math.PI * 2 - 0.1);
  else g.lineBetween(x - 3, y - 7, x + 3, y - 7);
  if (role === "server") g.fillStyle(0x563c35).fillRoundedRect(x - 14, y - 29, 28, 8, 4);
  if (role === "dishwasher") g.fillStyle(0x3d6f80).fillRoundedRect(x - 13, y - 28, 26, 7, 4);
}

export function populateFoodArt(scene: Phaser.Scene, container: Phaser.GameObjects.Container, item: KitchenItem): void {
  const g = scene.add.graphics(); const ink = ART_PALETTE.ink;
  if (item.kind === "ingredient") {
    if (item.ingredientId === "potato") {
      g.fillStyle(0xc78b4c).fillEllipse(0, 0, 31, 24); g.lineStyle(2, ink).strokeEllipse(0, 0, 31, 24); g.fillStyle(0x8b5f39).fillCircle(-7, -3, 2).fillCircle(6, 4, 2);
      if (item.state === "chopped") { g.fillStyle(0xf0c978).fillRoundedRect(-13, -9, 10, 18, 3).fillRoundedRect(-1, -11, 10, 20, 3).fillRoundedRect(10, -8, 9, 17, 3); }
    } else if (item.ingredientId === "tomato") {
      g.fillStyle(0xe94e43).fillCircle(0, 1, 14); g.lineStyle(2, ink).strokeCircle(0, 1, 14); g.fillStyle(0x579342).fillTriangle(-8, -9, 0, -16, 8, -9);
      if (item.state === "chopped") { g.lineStyle(2, 0xffc0aa).lineBetween(-10, -6, 10, 8).lineBetween(-9, 8, 9, -7); }
    } else if (item.ingredientId === "lettuce") {
      g.fillStyle(0x78b957).fillCircle(-6, 1, 10).fillCircle(5, 3, 11).fillCircle(1, -6, 11); g.lineStyle(2, ink).strokeCircle(-6, 1, 10).strokeCircle(5, 3, 11).strokeCircle(1, -6, 11);
      if (item.state === "chopped") g.fillStyle(0xa9d879).fillRoundedRect(-13, -8, 10, 8, 3).fillRoundedRect(-2, -11, 11, 8, 3).fillRoundedRect(3, 1, 12, 8, 3).fillRoundedRect(-10, 3, 10, 7, 3);
    } else {
      g.fillStyle(0xf2c84b).fillTriangle(-15, 10, 15, 8, 8, -14); g.lineStyle(2, ink).strokeTriangle(-15, 10, 15, 8, 8, -14); g.fillStyle(0xc79534).fillCircle(5, 2, 3).fillCircle(-4, 5, 2);
    }
  } else {
    if (item.state === "plated") { g.fillStyle(0xfdf8e9).fillCircle(0, 2, 22); g.lineStyle(3, 0x8fa0a7).strokeCircle(0, 2, 22).lineStyle(1, 0xd3d9d7).strokeCircle(0, 2, 16); }
    if (item.recipeId === "fries") {
      g.fillStyle(0xd6453e).fillRoundedRect(-11, -3, 22, 20, 3); g.fillStyle(0xf2bf3c);
      [-9, -4, 1, 6].forEach((x, index) => g.fillRoundedRect(x, -14 + index % 2 * 2, 5, 23, 2));
    } else if (item.recipeId === "garden-plate") {
      g.fillStyle(0x65a94c).fillCircle(-7, 1, 8).fillCircle(4, 4, 9).fillCircle(8, -4, 7); g.fillStyle(0xa9d879).fillRoundedRect(-11, -7, 10, 6, 2).fillRoundedRect(0, -3, 11, 6, 2); g.fillStyle(0xe94e43).fillCircle(-4, -4, 4).fillCircle(7, 4, 4);
    } else if (item.recipeId === "cheese-bake") {
      g.fillStyle(0xd98b43).fillRoundedRect(-13, -9, 26, 20, 5); g.lineStyle(2, ink).strokeRoundedRect(-13, -9, 26, 20, 5); g.fillStyle(0xf2c84b).fillCircle(-6, -3, 3).fillCircle(4, 4, 3).fillCircle(7, -5, 2);
    } else {
      g.fillStyle(0xd99a4b).fillEllipse(0, 1, 26, 19); g.lineStyle(2, ink).strokeEllipse(0, 1, 26, 19); g.fillStyle(0xf0c978).fillRoundedRect(-9, -5, 18, 7, 3);
    }
  }
  if (item.state === "ruined") { g.fillStyle(0x4c3b34, 0.65).fillCircle(0, 1, 20); g.lineStyle(4, 0xff806f).lineBetween(-11, -10, 11, 12).lineBetween(11, -10, -11, 12); }
  container.add(g);
}
