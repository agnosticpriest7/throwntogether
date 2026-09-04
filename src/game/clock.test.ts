import { expect, it } from "vitest";
import { restaurantClock } from "./data";

it("scales the service clock from 11 AM through noon to 9 PM and holds at closing", () => {
  expect(restaurantClock(0, 120000)).toBe("11:00 AM");
  expect(restaurantClock(12000, 120000)).toBe("12:00 PM");
  expect(restaurantClock(60000, 120000)).toBe("4:00 PM");
  expect(restaurantClock(120000, 120000)).toBe("9:00 PM");
  expect(restaurantClock(150000, 120000)).toBe("9:00 PM");
});
