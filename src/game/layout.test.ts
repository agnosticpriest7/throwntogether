import { describe, expect, it } from "vitest";
import { KITCHEN_SLOTS } from "./data";
import {
  DIRTY_RETURN_POS, DISH_SINK_POS, FIXED_KITCHEN_INTERACTIONS, ISLAND_COUNTERS,
  SERVICE_PICKUP_POS, SERVER_STAGING_POS, isInsideOpenKitchen,
} from "./layout";

describe("open-ring kitchen layout", () => {
  it("places every human kitchen interaction inside one shared reachable area", () => {
    expect([...FIXED_KITCHEN_INTERACTIONS, ...KITCHEN_SLOTS.map(({ x, y }) => ({ x, y }))].every(isInsideOpenKitchen)).toBe(true);
  });

  it("provides multiple central staging counters for solo and co-op circulation", () => {
    expect(ISLAND_COUNTERS).toHaveLength(3);
    expect(new Set(ISLAND_COUNTERS.map(({ position }) => position.x)).size).toBe(3);
  });

  it("keeps pickup and dish support on the dining-facing service edge", () => {
    expect(SERVICE_PICKUP_POS.x).toBeGreaterThan(800);
    expect(DISH_SINK_POS.x).toBeGreaterThan(750);
    expect(Math.hypot(SERVICE_PICKUP_POS.x - SERVER_STAGING_POS.x, SERVICE_PICKUP_POS.y - SERVER_STAGING_POS.y)).toBeLessThan(100);
    expect(Math.hypot(DISH_SINK_POS.x - DIRTY_RETURN_POS.x, DISH_SINK_POS.y - DIRTY_RETURN_POS.y)).toBeLessThan(160);
  });
});
