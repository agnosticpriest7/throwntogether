import { describe, expect, it } from "vitest";
import { canAutoCatch, clampToSide, throwLanding } from "./rules";

describe("transfer rules", () => {
  it("keeps both players on their assigned kitchen side", () => {
    expect(clampToSide({ x: 800, y: 20 }, "left")).toEqual({ x: 420, y: 72 });
    expect(clampToSide({ x: 100, y: 900 }, "right")).toEqual({ x: 540, y: 548 });
  });

  it("supports predictable throws in both directions", () => {
    expect(throwLanding("left", 300)).toEqual({ x: 690, y: 300 });
    expect(throwLanding("right", 300)).toEqual({ x: 270, y: 300 });
  });

  it("requires free hands for automatic catches", () => {
    expect(canAutoCatch({ x: 690, y: 300 }, { x: 690, y: 300 }, true)).toBe(true);
    expect(canAutoCatch({ x: 690, y: 300 }, { x: 690, y: 300 }, false)).toBe(false);
    expect(canAutoCatch({ x: 800, y: 300 }, { x: 690, y: 300 }, true)).toBe(false);
  });

  it("fails a throw outside the catch circle", () => {
    const landing = throwLanding("left", 200);
    expect(canAutoCatch({ x: 690, y: 269 }, landing, true)).toBe(false);
  });
});
