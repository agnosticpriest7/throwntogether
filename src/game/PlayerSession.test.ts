import { describe, expect, it, vi } from "vitest";
import { PlayerSession } from "./PlayerSession";

describe("optional local co-op session", () => {
  it("starts with one active human player", () => {
    const session = new PlayerSession();
    expect(session.mode).toBe("solo");
    expect(session.activePlayerCount).toBe(1);
    expect(session.isActive(0)).toBe(true);
    expect(session.isActive(1)).toBe(false);
  });

  it("can add and remove the optional second local player", () => {
    const session = new PlayerSession(); const changed = vi.fn(); session.onChange(changed);
    session.setMode("coop");
    expect(session.activePlayerCount).toBe(2);
    expect(session.isActive(1)).toBe(true);
    session.setMode("solo");
    expect(session.isActive(1)).toBe(false);
    expect(changed).toHaveBeenCalledTimes(2);
  });
});
