import { describe, expect, it } from "vitest";
import { canAutoCatch, clampToKitchen, throwLanding } from "./rules";

describe("transfer rules", () => {
  it("lets either player use the whole open kitchen while keeping them out of dining", () => {
    expect(clampToKitchen({ x: 800, y: 20 })).toEqual({ x: 800, y: 88 });
    expect(clampToKitchen({ x: 1000, y: 900 })).toEqual({ x: 912, y: 548 });
  });

  it("supports predictable throws in both directions", () => {
    expect(throwLanding({ x: 300, y: 300 }, { x: 1, y: 0 })).toEqual({ x: 570, y: 300 });
    expect(throwLanding({ x: 700, y: 300 }, { x: -1, y: 0 })).toEqual({ x: 430, y: 300 });
  });

  it("requires free hands for automatic catches", () => {
    expect(canAutoCatch({ x: 690, y: 300 }, { x: 690, y: 300 }, true)).toBe(true);
    expect(canAutoCatch({ x: 690, y: 300 }, { x: 690, y: 300 }, false)).toBe(false);
    expect(canAutoCatch({ x: 800, y: 300 }, { x: 690, y: 300 }, true)).toBe(false);
  });

  it("fails a throw outside the catch circle", () => {
    const landing = throwLanding({ x: 300, y: 200 }, { x: 1, y: 0 });
    expect(canAutoCatch({ x: 690, y: 269 }, landing, true)).toBe(false);
  });
});
