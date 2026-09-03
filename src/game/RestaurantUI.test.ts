import { describe, expect, it } from "vitest";
import { confirmationMarkup, spatialNavigationTarget, type NavigationRect } from "./RestaurantUI";

const rect = (left: number, top: number, width = 100, height = 40): NavigationRect => ({ left, top, right: left + width, bottom: top + height });

describe("management controller navigation", () => {
  const layout = [rect(0, 0), rect(110, 0), rect(220, 0), rect(0, 80, 150, 100), rect(170, 80, 150, 100), rect(220, 240, 120, 45)];

  it("moves down from a tab into the nearest menu option", () => {
    expect(spatialNavigationTarget(layout, 0, "down")).toBe(3);
  });

  it("moves horizontally through options without changing rows", () => {
    expect(spatialNavigationTarget(layout, 3, "right")).toBe(4);
  });

  it("reaches the footer action from the bottom option", () => {
    expect(spatialNavigationTarget(layout, 4, "down")).toBe(5);
  });

  it("does not wrap when there is no target in that direction", () => {
    expect(spatialNavigationTarget(layout, 0, "up")).toBe(0);
  });
});

describe("controller-friendly confirmations", () => {
  it("uses focusable in-game controls instead of a browser dialog", () => {
    const markup = confirmationMarkup("new-restaurant");
    expect(markup).toContain('role="alertdialog"');
    expect(markup).toContain('id="confirmation-cancel"');
    expect(markup).toContain('id="confirmation-accept"');
    expect(markup).toContain("A CONFIRM · B CANCEL");
    expect(markup).not.toContain("window.confirm");
  });

  it("clearly distinguishes destructive save reset confirmation", () => {
    expect(confirmationMarkup("reset-save")).toContain("RESET SAVE");
    expect(confirmationMarkup("reset-save")).toContain("all progression");
  });
});
