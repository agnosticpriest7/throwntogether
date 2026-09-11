# Thrown Together — Development Roadmap

Directional, not rigid. This document reflects the current implemented game rather than the original prototype plan.

## Current implemented baseline — 0.17.x

Thrown Together is now well beyond the original vertical slice. The current game already includes:

- Unity 6 / URP production project
- GitHub source control and Web deployment
- automated EditMode, PlayMode and Web verification
- single-player and optional local couch co-op foundations
- stylized 3D / 2.5D presentation with angled orthographic camera
- persistent restaurant account/save data
- timed service days
- customer seating, ordering, eating and clearing
- dishwashing and a finite reusable plate pool
- ingredient prep, cooking, plating and serving
- multiple ingredients, recipes and appliances
- daily menu selection with a minimum of three dishes
- recipe complexity/value progression
- menu-variety bonus
- Produce Rack and Refrigerator category storage
- purchasable Griddle and Grill progression
- between-day management hub
- appliance/counter/plate purchasing
- repeat equipment purchases
- equipment resale
- kitchen rearrangement using validated physical placement bays
- kitchen expansion/progression foundations
- employees including server, busser and dishwasher
- employee training
- day/night presentation transitions
- studio/publisher/title startup sequence
- controller-focused menus and diagnostics

The next work should build on this baseline rather than recreate earlier prototype milestones.

---

# Game Modes

The project should support three distinct modes with different goals rather than treating them as variations of one progression system.

## Quick Play — Custom Restaurant Session

**Purpose:** Flexible low-commitment play where the player chooses how demanding the session should be.

Quick Play should allow the player to configure a restaurant/session without committing to the long-term Career structure or mandatory Endless escalation.

Direction:

- choose available kitchen/layout
- choose menu/dishes from available content
- choose shift length / starting demand where appropriate
- allow customer growth to be optional or player-controlled
- missed customers/orders do not automatically end the run by default
- suitable for experimenting with layouts, recipes, employees and co-op
- useful as a relaxed couch-play mode

Future options may include difficulty presets, custom customer counts, employee/equipment starting conditions and optional arcade failure rules.

## Endless — Mandatory Escalation / Survival

**Purpose:** High-pressure arcade survival mode.

The current mode labeled Career most closely resembles this concept and should ultimately become **Endless**.

Core Endless rules:

- one persistent restaurant for the run
- normal between-day management remains available
- player can buy appliances, rearrange the kitchen, hire/train staff and expand
- customer demand increases every day automatically
- escalation is mandatory rather than optional
- difficulty should continue rising until the restaurant eventually fails
- if a customer leaves because their order was not served before patience expires, the run ends
- ordinary kitchen mistakes such as dropping/burning/wasting food are not themselves instant game over; they matter because they consume time and can cause a customer to leave

End-of-run summary should emphasize:

- days survived
- customers served
- revenue earned
- highest/most difficult completed day
- other useful performance statistics

Long-term consideration: local/personal best records and eventual leaderboard-style scoring.

Endless answers the question:

> **How long can this restaurant survive as demand keeps increasing?**

## Career — Build a Restaurant Company

**Purpose:** Long-term restaurant ownership and company-building progression.

Career should be broader and more strategic than Endless. A poor service day creates consequences but does **not** erase the player's company or end the campaign.

Career progression should grow from one hands-on restaurant into a multi-location restaurant business.

### Early Career — Owner/operator

- start with one small restaurant
- player personally performs most work
- select menu
- earn money
- purchase appliances and counters
- expand kitchen/dining capacity
- hire first employees
- improve reputation

### Established Restaurant

- broader recipes and appliance choices
- staff scheduling/roles
- employee skills and training
- more meaningful menu/pricing decisions
- reputation and advertising influence demand
- increasingly capable restaurant without mandatory infinite daily escalation

### Employee progression

Employees should become long-term assets rather than disposable automation.

Future Career employee depth:

- role skills / strengths / weaknesses
- experience gained through work
- wages
- training
- promotions
- employment history
- transfers between locations
- supervisors / managers

Keep statistics understandable rather than turning the game into a dense personnel simulator.

### Managers

Managers are a major Career milestone.

A sufficiently developed employee should eventually be able to manage a restaurant without the player physically working there.

Managers should enable the company-growth fantasy without preventing hands-on play. The owner can still visit any location and personally cook/work whenever desired.

### Additional restaurants

A successful Career should eventually allow the player to open additional locations.

Each new restaurant needs its own:

- physical space/layout
- equipment
- menu
- employees
- manager when appropriate
- reputation/performance state
- financial results

Locations may share the same restaurant concept or use different menus/concepts.

Experienced employees may be transferred between locations, creating meaningful staffing decisions.

Off-screen manager-run locations should be economically/operationally simulated rather than fully rendered at all times.

Career answers the question:

> **Can I build one small restaurant into a successful restaurant company?**

---

# Shared Systems — Next Development Priorities

These systems benefit all three modes and should generally be developed before mode-specific late-game content.

## 1. Physical Xbox / couch-co-op validation

Continue real-device testing of:

- Xbox Edge game-control activation
- P1/P2 joining and reconnecting
- menu navigation
- ingredient selection
- kitchen arrangement
- TV-distance readability
- startup/title skipping

Automated tests must not substitute for physical controller verification.

## 2. Kitchen layout and expansion refinement

Continue developing the current physical arrangement system.

Direction:

- valid placement bays should be obvious visually
- available locations should illuminate/highlight during layout editing
- appliances/counters should feel physically picked up and repositioned, not merely reassigned through a numbered menu
- retain collision, walking-route and spawn validation
- support perimeter bays and central island clusters
- camera should reframe appropriately as kitchen footprint expands
- between-day management remains the natural time for layout changes

Future consideration: saved layout presets for different menu styles once the game has enough content to justify them.

## 3. More appliances / ingredients / recipes

Continue expanding content through systems that reuse existing ingredients rather than introducing isolated one-appliance/one-dish branches.

Design principle:

> A new appliance should create new uses for ingredients the player already understands while adding only a manageable amount of new pantry complexity.

Recipe value should continue scaling with operational complexity, including ingredient count, preparation stages, appliance requirements and multi-component assembly.

## 4. Employee depth

Build outward from the existing server, busser and dishwasher systems.

Likely future roles:

- prep cook
- line/station cook
- host
- supervisor
- manager

Progress toward skills, experience, wages, promotion and transfer carefully, keeping employees readable and emotionally persistent.

## 5. Reputation, demand and advertising

Career especially needs demand driven by restaurant success rather than mandatory daily escalation.

Direction:

- persistent reputation defines baseline demand
- advertising temporarily boosts demand
- players may intentionally create more demand than current capacity can handle
- service quality affects reputation
- small high-reputation restaurants remain viable

Endless should use its own mandatory escalation curve rather than relying only on Career reputation mechanics.

## 6. Economy depth

Current purchase/resale/training systems provide the foundation.

Future work may include:

- ingredient purchasing
- bulk discounts
- persistent stock
- wages
- pricing decisions
- location-level operating results

Existing direction remains:

- no ingredient spoilage/decay for now
- no repair/maintenance economy as a core money sink

## 7. Main menu / presentation polish

Continue integrating the game's persistent restaurant identity into presentation.

Direction:

- morning main-menu atmosphere
- warm/cozy restaurant presentation
- service-day lighting
- 10 PM closing transition
- nighttime between-day management
- sunrise / 11 AM reopening

Use the restaurant world itself where practical rather than disconnected generic menus.

## 8. Audio / music

Build on the existing audio foundation with:

- tactile cooking sounds
- appliance ambience
- pickup/place/prep/serve feedback
- UI audio
- restaurant ambience
- music appropriate for long repeated sessions

Preferred musical identity: warm playful indie-funk / light jazzy restaurant groove rather than frantic comedy music.

## 9. Art and character polish

Continue the approved stylized 3D / 2.5D direction:

- angled orthographic camera
- readable work surfaces and appliance fronts
- warm dining / cooler commercial-kitchen separation
- simple reusable Blender-built modules
- expressive but readable characters
- avoid decorative clutter that compromises gameplay readability

Prototype/AI-assisted assets may be replaced or refined by human specialists where final production quality benefits materially.

---

# Career-Specific Later Phases

## Multi-location simulation

- location operating summaries
- revenue / wages / performance
- manager effectiveness
- staffing shortages
- customer/reputation results
- menu/equipment differences

Do not require all owned restaurants to run as fully rendered live scenes simultaneously.

## Employee transfer and promotion

- transfer experienced workers between locations
- promote employees into supervisors/managers
- moving a strong worker should benefit one restaurant while weakening another
- preserve hire date / role history / progression where practical

## Restaurant concepts

Career should not force arbitrary restaurant classes.

Concept should emerge primarily from:

- menu
- equipment
- layout
- pricing
- decor/theme where eventually supported

A burger-focused restaurant may gradually broaden its menu or remain specialized indefinitely.

---

# Endless-Specific Later Phases

## Escalation tuning

Tune mandatory daily growth using real playtests.

Potential escalation levers:

- customer count
- arrival spacing
- patience
- menu complexity pressure
- simultaneous table demand

Avoid difficulty increases that merely feel unfair or remove meaningful player counterplay.

## Run failure

Primary failure condition:

> A customer leaves because their order was not completed in time.

Failure ends the Endless run and produces a clear results screen.

## Endless scoring

Primary ranking:

- days survived

Secondary statistics:

- customers served
- revenue
- recipes served
- peak customer load
- staff/equipment state at failure

---

# Quick Play-Specific Later Phases

Quick Play should remain the least restrictive mode.

Potential configuration options:

- kitchen/layout
- menu
- shift length
- starting customer count
- demand growth on/off
- employee availability
- starting cash/equipment presets
- relaxed or failure-enabled rules

Do not require every configuration option immediately. Add them as shared systems mature.

---

# Long-Term Production

## Production art / polish

- final character models/animations
- appliances and food
- environment art
- UI
- accessibility
- audio/music
- performance
- controller polish

## Windows / Steam

- native Windows production build
- Steam integration
- saves/settings validation
- achievements where appropriate
- QA
- store assets
- release pipeline

## Campaign / authored challenges

A separate authored challenge / three-star mode remains optional and is not currently a primary development priority. Revisit only if it adds something meaningfully different from Quick Play, Endless and Career.

---

# Mode identity summary

| Mode | Core fantasy | Demand growth | Missed customer |
| --- | --- | --- | --- |
| **Quick Play** | Play the restaurant session you want | Player choice | Normally continue |
| **Endless** | Survive as long as possible | Mandatory and increasing | Run ends |
| **Career** | Build a restaurant company | Reputation/growth driven | Business consequence, continue |

The three modes should share the same core cooking, restaurant, layout, employee and content systems while applying different progression and failure rules.