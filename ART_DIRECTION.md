# Thrown Together Art Direction

This document is the visual bible for the first production-direction art pass. The current Milestone 4 restaurant geometry remains authoritative; art supports gameplay rather than changing it.

## Visual principles

- **Simplified cartoon:** Build objects from a few rounded, recognizable forms. Silhouette and color must identify an object before labels do.
- **Top-down readability:** Use a mostly top-down view with a small suggestion of depth from short side faces and soft shadows. Never let perspective move the apparent interaction point.
- **Toy-like proportions:** Characters have a large rounded head, compact torso, short limbs, and oversized work accessories. They are cute but remain human-like.
- **Chunky outlines:** Use a shared dark cocoa outline (`#49352d`) at roughly 2–4 logical pixels. Important interactables may use a thicker yellow focus outline.
- **Limited shading:** Prefer one flat base color plus, at most, one lighter or darker detail. Avoid filters, elaborate gradients, and directional lighting.
- **Warm restraint:** Cream, wood, blue, and coral define the restaurant. Food may be more saturated so ingredients remain readable during service.
- **Soft depth only:** Shadows are short, low-opacity ellipses or small downward offsets. No long shadows or realistic ambient occlusion.

## Color strategy

| Purpose | Color family | Current anchor |
|---|---|---|
| Universal outline/text | Cocoa brown | `#49352d` |
| Paper, plates, UI surfaces | Warm cream | `#fff1cf` / `#fff8e7` |
| Player 1 kitchen/uniform | Cool blue | `#78afd5` / `#4595c6` |
| Player 2 kitchen/uniform | Coral | `#dd7770` / `#dc625b` |
| Dining/shared structures | Natural wood | `#a9673d` / `#c98450` |
| Success/ready/interact | Leaf green | `#6fa447` |
| Primary action/focus | Golden yellow | `#ffcc3f` |

Large surfaces stay moderately saturated. Ingredients, ready food, patience bars, and interaction focus receive the strongest accents.

## Character rules

- Gameplay characters use a head approximately half the visible body height, a compact rounded torso, short arms/legs, two large eyes, and one simple mouth stroke.
- Chefs share the same reusable construction. Player 1 is blue; Player 2 is coral. Both wear cream uniforms, aprons, and chef hats.
- Servers use purple uniforms and a low dark cap. Dishwashers use teal uniforms and a blue work cap. Staff remain visually related to chefs but immediately role-readable.
- Customers use the same human-like head/body language with a small palette of skin, hair, and shirt variations. Mood is conveyed with a mouth change rather than detailed animation.
- Held items sit above and slightly in front of the character. Their silhouette must remain visible against both kitchen halves.
- Walking may use a subtle two-frame bob/lean. Use, chop, throw, and receive poses should move reusable arms/items rather than replace the character with a separate illustration.

## Environment rules

- The blue and coral tile fields communicate player territory. Grid lines are broad and low contrast; they must never compete with food.
- Shared counters and the divider use cream/wood, visually distinct from either player side.
- Dining uses simple warm planks, authored wooden tables, chunky chairs, and no more than one or two small plants in open corners.
- Counters use a light cream or wood top, cocoa outline, and a short dark lower shadow/face.
- Empty expansion positions use translucent cream with a dashed/subtle outline. They remain visible without resembling active equipment.
- Decorations never change collision or obscure the entrance, table paths, pickup, sink, or dirty return.

## Appliance rules

- Every appliance needs a distinct silhouette and one identifying internal feature: oven window/glow, fryer oil/bubbles, sink basin/faucet, prep board/knife, plate stack, or assembly bowl.
- The cocoa outer silhouette is shared across the set. Steel, coral, blue, or wood details communicate material without texture noise.
- Active-state animation is restrained: oven glow, fryer bubbles, sink water, or a progress bar. Interaction focus is always the existing high-contrast yellow outline.
- Appliance artwork remains centered on the authoritative station coordinate and inside its interaction footprint.

## Food and plate rules

- Ingredient icons use recognizable produce silhouettes instead of letters: spotted potato, tomato with leaf crown, layered onion, and cheese wedge.
- Chopped states add genuinely different pieces or internal cuts. Cooked/assembled dishes use recipe-specific forms.
- Finished dishes sit on a cream plate with a grey inner rim. Fries use a red carton, Garden Plate uses leafy/tomato shapes, Cheese Bake uses a browned casserole, and Roast Potato uses a split golden potato.
- Dirty plates use two or three large crumbs/sauce marks. Ruined food receives a muddy overlay and a large coral X.

## UI rules

- HUD and management UI use cream/off-white cards, cocoa text and outlines, rounded corners, and short offset shadows.
- Golden fill indicates the selected tab or primary action; green indicates purchase/ready/success; muted red indicates destructive or failed state.
- Numbers stay large enough to scan from a television. Order tickets prioritize the illustrated dish mark, patience bar, and table number.
- Controller focus must remain more prominent than hover. Existing DOM order, buttons, and spatial navigation must not be rearranged solely for styling.
- Touch controls use the same cream cards and cocoa outlines, with green Use and gold Throw buttons. They may not overlap the game canvas.

## Asset technical rules

- The runtime art system lives in `src/game/ArtFactory.ts`. Prefer reusable Phaser vector components or clean SVG for future additions. Use transparent PNG only when a shape cannot be represented efficiently in vector form.
- Recommended logical sizes: ingredients 24–40 px, plated food 40–52 px, gameplay characters 48–72 px, appliances 64–108 px, UI dish marks 32–56 px.
- Name standalone files with lowercase kebab-case under `src/assets/<category>/`, for example `src/assets/food/garden-plate.svg`.
- SVG should use a tight viewBox and the shared outline/palette. PNG should be tightly cropped, transparent, and no larger than twice its maximum display size.
- Keep antialiasing enabled for vector curves. Avoid runtime blur, bloom, color filters, and huge source images.
- Animation convention: idle is still or a very small bob; walk is a two-step loop; work states move one tool/arm or use an appliance state cue. Favor 6–10 fps for sprite animation if vector motion is replaced later.
- All runtime assets must be committed locally and must never depend on external URLs.

## Preservation checklist

Before merging future art changes, verify that side bounds, divider, shared counter, appliance centers, pickup, sink, dirty return, table centers, entrance, AI destinations, throw arc/landing marker, touch controls, and focused management buttons still align with the existing gameplay geometry.
