# 0.4.0 — Overnight review

**Ready to test:** https://agnosticpriest7.github.io/throwntogether/

Refresh and confirm **DEV 0.4.0-dev c7e6342e**. Xbox Edge still owns hold Menu -> Use game controls.

## What changed

- Recognizable ingredient crates, prep board/knife, fryer basket, plate stack and service pass/bell. Floating station nameplates and opaque world panels are gone.
- Larger food shapes and plate rims, small floor target borders, progress bars and ready lights. Compact edge HUD and clearer plated-food ticket icons.
- **Either ordinary counter can plate food.** Put down cooked food, add a plate (or do plate-first), then press Use again to collect the finished dish. It stays on the counter after assembly.
- **Y / Escape -> Shift length and guided practice:** 3-, 6- or 12-dish shifts, full-loop guidance, or practice starting at prep/frying/plating/serving. Existing two recipes and two seats; no order expiry.
- Session results show play time and each player's prep/fry/plate/serve contributions. Small customer/service presentation refinements; reduced effects remains available.
- More co-op/assembly regressions, verified Web artifact manifests and an optional safe deployment-restore command.

## First things to try

1. Walk around in solo/co-op. Can you identify stations and held foods without nameplates, from your TV distance?
2. Plate on both counters, in both orders. The extra pickup after assembly is intentional.
3. Try a short shift, then a guided/practice starting point through the menu. Check results.
4. Check the largest text size; have P2 contest a counter, disconnect/reconnect holding food, and open/cancel the menu.

Movement, camera, reach, cooking times and the room layout are unchanged. No new recipe, throwing, dishwashing, economy, employees or additional milestone.

## Verification

22 EditMode + 23 PlayMode + nine browser/artifact checks passed. Normal-timer cooking/service completed through Unity MCP; keyboard counter plating also worked in the hosted build. No new Unity/browser errors; one pre-existing URP shader warning remains. Desktop sample about 60 FPS; total Web size 48,043,773 bytes, up about 0.08%. Physical Xbox/TV review is yours; no agent physical test is claimed.

Runtime source: `c7e6342e46a5efb573c5ee64156b389d8e2e76d1`.
Deployment: `0ac5793ef4140394d5ff97ec24a01d7dd9f47c20`.

The prior owner-tested co-op/food-shape build remains recoverable. See [README](../README.md) for restore commands and [development notes](DEVELOPMENT_NOTES.md) for detailed checks. Work stops at this review.
