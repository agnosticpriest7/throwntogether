# Thrown Together — Development Roadmap

Directional, not rigid. This document reflects the current implemented game and the current design direction rather than the original prototype plan.

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
- recipe complexity/value progression foundations
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

The next major work should prioritize gameplay depth over additional art polish. The central design direction is now **Expo + employees + physical kitchen layout + production flow**.

---

# Core Gameplay Direction

Thrown Together should become a physical restaurant game where the player gradually builds a team capable of running the restaurant, while always retaining the ability to jump back into any hands-on job.

The key optimization fantasy is:

> **PlateUp optimizes machines. Thrown Together optimizes a living restaurant team.**

Orders create work. The player decides what work enters the kitchen. Employees claim work according to their roles, skills and priorities. Kitchen layout determines how efficiently people and food move. As complexity grows, the player can increasingly act as Expediter/manager while still stepping onto a station whenever the restaurant needs help.

---

# Game Modes

The project should support three distinct modes with different goals rather than treating them as variations of one progression system.

## Quick Play — Custom Restaurant Session

**Purpose:** Flexible low-commitment play where the player chooses what kind of restaurant session to run.

Direction:

- choose kitchen/layout
- choose recipes from the available recipe pool
- choose shift length / starting demand where appropriate
- customer growth may be optional or player-controlled
- missed customers/orders do not automatically end the run by default
- suitable for experimenting with layouts, recipes, employees and co-op
- useful as a relaxed couch-play mode

Whether Quick Play exposes every recipe immediately or is partially gated by Career progression remains an open design decision.

Future options may include difficulty presets, custom customer counts, employee/equipment starting conditions and optional arcade failure rules.

## Endless — Mandatory Escalation / Survival

**Purpose:** High-pressure arcade survival mode.

The current mode labeled Career most closely resembles this concept and should ultimately become **Endless**.

Core Endless rules:

- one persistent restaurant for the run
- choose the active menu from available recipes
- normal between-day management remains available
- player can buy appliances, rearrange the kitchen, hire/train staff and expand
- workload increases every day automatically
- escalation is mandatory rather than optional
- difficulty should continue rising until the restaurant eventually fails
- if a customer leaves because their order was not served before patience expires, the run ends
- ordinary kitchen mistakes such as dropping/burning/wasting food are not themselves instant game over; they matter because they consume time and can cause a customer to leave

### Endless workload model

Endless should not simply add a fixed number of customers every day regardless of restaurant type.

Use a rising **workload/demand budget**. Menu complexity and dining setup convert that budget into the actual number and shape of orders.

Examples:

- simple, fast menu → more individual orders
- complex menu → fewer orders, but more work per ticket
- Double/Family Tables → fewer parties but larger simultaneous bursts
- To-Go Counter → additional kitchen throughput without consuming dining tables

Increasing recipe difficulty or table size may reduce total party/order count by a percentage while the overall workload still rises from day to day.

This allows a fast-food restaurant and a high-end restaurant to reach comparable difficulty through very different service patterns.

End-of-run summary should emphasize:

- days survived
- customers served
- revenue earned
- highest/most difficult completed day
- other useful performance statistics

Long-term consideration: local/personal best records and eventual leaderboard-style scoring.

Endless answers the question:

> **How long can this restaurant survive as workload keeps increasing?**

## Career — Build a Restaurant Company

**Purpose:** Long-term restaurant ownership and company-building progression.

Career should be broader and more strategic than Endless. A poor service day creates consequences but does **not** erase the player's company or end the campaign.

Career progression should grow from one hands-on restaurant into a multi-location restaurant business.

### Career starting groups

Career should begin by choosing one of a small number of starter food groups rather than choosing an irreversible restaurant class.

Initial planned starter groups:

- **Burgers**
- **Italian**
- **Mexican**

Each group begins with a small, simple three-item menu that teaches a few different production areas.

Burger example:

- Basic Burger
- Fries
- Soft Drink

Italian and Mexican starter trios are still to be designed.

The starter group only determines where the restaurant begins. It does not permanently define the restaurant's concept.

### Mastery = depth

Serving food successfully earns **Food Mastery** within its family.

Mastery should unlock deeper variations and more demanding production options within food the player already understands.

Burger-family example direction:

- Basic Burger
- Cheeseburger
- Bacon / Mushroom variants
- Double Burger
- premium/gourmet burger variants
- eventual signature burger possibilities

Fries and beverages can have their own mastery branches.

Mastery should not simply create linear stat upgrades. New recipes should change workload, station pressure, ingredients, prep and assembly.

### Reputation = breadth

Reputation expands the player's culinary opportunity pool.

Direction:

- low reputation mostly exposes recipes from the three starter groups
- at milestones, the player receives a controlled random selection of new recipe opportunities
- skipped recipes are not permanently lost; they can return later
- around Reputation 10, a mid-tier set of cuisine groups should begin entering the opportunity pool (exact threshold/tuning remains provisional)
- later reputation tiers introduce more specialized cuisine families, ingredients, appliances and staff needs

This creates varied Careers without allowing pure randomness to permanently derail the player's preferred direction.

### Recipe opportunity draft

A typical unlock should present a small choice, such as **choose 1 of 3 opportunities**.

An opportunity may introduce:

- a new recipe
- a new ingredient
- a new appliance requirement
- access to a new food family

New recipe access should create physical operational consequences rather than simply expanding a menu list.

### Restaurant identity emerges from play

Career should not force arbitrary permanent restaurant classes.

Restaurant identity should emerge from:

- chosen menu
- recipe complexity
- equipment
- staffing
- prep strategy
- table mix
- takeout capacity
- pricing/economy
- reputation

A player can remain a high-volume burger restaurant forever, evolve into an upscale burger concept, mix in Italian/Mexican dishes, or eventually operate multiple restaurants with different concepts.

### Early Career — Owner/operator

- start with one small restaurant
- player personally performs most work
- select menu
- earn money
- purchase appliances and counters
- expand kitchen/dining capacity
- hire first employees
- improve reputation
- earn food mastery

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
- training / cross-training
- task priorities
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

# Highest-Priority Gameplay Work

These systems should be pursued before another major art pass.

## 1. Physical Expo / Order Rail

This is the next major gameplay milestone.

Add a physical **Expo Counter / Order Rail** in the kitchen.

Flow:

**Dining-room order → Waiting ticket → Player fires ticket → Active kitchen queue → Production → Completed dish / Expo → Server / customer**

Direction:

- customer orders first exist in a Waiting Orders pool
- player physically walks to the Expo station and interacts with it
- show recipe and customer urgency/patience clearly
- player chooses which order to fire next
- allow multiple fired orders at once
- fired queue should have a limited starting capacity rather than accepting everything automatically
- player can prioritize/reorder work where appropriate
- completed dishes should remain associated with their ticket/order
- eventually the Expediter role can be hired/trained out to an employee

The player should be able to spend a busy service acting primarily as Expediter, then leave Expo and personally help whichever station is failing.

The system should first be proven with the player still doing the cooking manually before attempting full kitchen-employee automation.

## 2. Employee Kitchen Orchestration

Employees should not simply remove a mechanic from the player. They should form a configurable production team.

Future roles may include:

- prep cook
- grill / fryer / griddle station chefs
- general line cook
- beverage specialist/director
- pastry chef
- dishwasher
- server
- busser
- host
- Expediter
- supervisor / kitchen manager

Design principle:

> Employees are assigned work types, stations and priorities rather than each employee blindly owning a complete customer order.

Fired tickets can eventually decompose into available tasks. Employees claim tasks they are qualified and prioritized to perform.

Example employee priorities:

**Dishwasher**
1. Wash dishes
2. Restock clean plates
3. Prep potatoes when dish queue is clear

**Prep Cook**
1. Keep tomato Prep Bin stocked
2. Keep lettuce Prep Bin stocked
3. Help another approved task when buffers are full

This is the human-staff equivalent of an automation network.

## 3. Physical Employee Traffic and Congestion

Kitchen layout should matter because employees occupy real space.

Direction:

- employees cannot simply walk through each other
- brief yielding / sidestepping is acceptable
- reroute if blocked long enough
- never allow permanent pathfinding deadlocks
- poor layout should cost time, not break the simulation
- station access, food movement and staff routes should remain visually understandable

The player should be able to look at a bad service and realize that two employees repeatedly crossing the same aisle is the problem, then fix that problem through Arrange Kitchen.

## 4. Service Pacing, Complexity and Patience

Current service should move toward fewer, more meaningful orders rather than relying on a constant stream of fast-expiring tickets.

Customer patience should relate to the expected complexity of the ordered food rather than relying only on one global timer.

Direction:

- simple/fast dishes → shorter expected wait, high potential volume
- complex/high-end dishes → longer expected wait, lower volume, higher value
- recipe complexity contributes to expected service time
- future customer traits may further modify patience
- service should have enough breathing room for Expo decisions to matter

This supports restaurants ranging from fast-food throughput to high-end dining execution.

## 5. Prep Bins / Production Buffers

Add a purchasable **Prep Bin**.

Current direction:

- one bin holds one processed ingredient type at a time
- starting capacity: about 5
- upgrades: roughly 8–10
- bin choice should matter based on the active menu
- prep employees can eventually keep designated bins stocked

Example:

**Prep Cook → processed potato Prep Bin → Fry/Griddle Cook**

Prep Bins should turn menu planning and advance preparation into a real production strategy rather than simply increasing storage.

## 6. Dining Capacity and Demand-Shaping Equipment

Add dining/service options that allow players to deliberately take on more throughput and risk.

### Standard Table

Normal small-party service.

### Double Table

- generates multiple simultaneous orders
- better revenue opportunity per seating event
- creates burst pressure

### Family Table

- larger parties / larger simultaneous ticket bursts
- fewer parties may arrive overall for equivalent workload
- strong income potential but greater risk if kitchen is unprepared

### To-Go Counter (Dining Side)

- orders do not consume a dining table
- adds kitchen throughput
- reduced income per to-go plate compared with equivalent dine-in food
- can eventually have dedicated service staff

Owning more/larger tables should not automatically manufacture demand. Reputation/advertising/Endless workload determine how much demand exists; tables and takeout determine how that demand can be accepted and shaped.

## 7. Menu / Mastery / Reputation Progression

Before implementing a large quantity of new dishes, define:

- Burger starter tree
- Italian starter tree
- Mexican starter tree
- first mastery branches
- initial recipe-complexity ratings
- reputation opportunity tiers
- first mid-tier cuisine groups
- appliance/ingredient dependencies

This design work should be settled enough that Codex can build against a coherent progression model rather than adding recipes ad hoc.

---

# Other Shared Development Priorities

## Physical Xbox / couch-co-op validation

Continue real-device testing of:

- Xbox Edge game-control activation
- P1/P2 joining and reconnecting
- menu navigation
- ingredient selection
- kitchen arrangement
- TV-distance readability
- startup/title skipping

Automated tests must not substitute for physical controller verification.

## Kitchen layout and expansion refinement

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

## More appliances / ingredients / recipes

Continue expanding content through systems that reuse existing ingredients rather than introducing isolated one-appliance/one-dish branches.

Design principle:

> A new appliance should create new uses for ingredients the player already understands while adding only a manageable amount of new pantry complexity.

Recipe value should scale with operational complexity, including ingredient count, preparation stages, appliance requirements and multi-component assembly.

## Reputation, demand and advertising

Career especially needs demand driven by restaurant success rather than mandatory daily escalation.

Direction:

- persistent reputation defines baseline demand
- advertising temporarily boosts demand
- players may intentionally create more demand than current capacity can handle
- service quality affects reputation
- small high-reputation restaurants remain viable

Endless should use its own mandatory workload curve rather than relying only on Career reputation mechanics.

## Economy depth

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

## Main menu / presentation polish

Continue integrating the game's persistent restaurant identity into presentation.

Direction:

- morning main-menu atmosphere
- warm/cozy restaurant presentation
- service-day lighting
- 10 PM closing transition
- nighttime between-day management
- sunrise / 11 AM reopening

Use the restaurant world itself where practical rather than disconnected generic menus.

## Audio / music

Build on the existing audio foundation with:

- tactile cooking sounds
- appliance ambience
- pickup/place/prep/serve feedback
- UI audio
- restaurant ambience
- music appropriate for long repeated sessions

Preferred musical identity: warm playful indie-funk / light jazzy restaurant groove rather than frantic comedy music.

## Art and character polish

Continue the approved stylized 3D / 2.5D direction, but gameplay work currently takes priority over another large visual pass.

Direction:

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

## Signature dishes

Possible later Career system once normal mastery progression is proven:

- allow experienced restaurants to create a limited number of signature dishes
- combine unlocked components within a known food family
- calculate resulting production complexity, ingredient cost, expected wait and value
- let the player name the dish

Do not build this before the standard recipe/mastery system is fun and understandable.

---

# Endless-Specific Later Phases

## Escalation tuning

Tune mandatory daily growth using real playtests.

Potential escalation levers:

- workload budget
- party/customer count
- arrival spacing
- recipe complexity
- table-size burst pressure
- takeout volume

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
- recipes
- shift length
- starting customer pressure
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

## Xbox

After the core game is proven and stable on PC:

- controller-only usability throughout
- account/sign-in integration as required
- suspend/resume
- save behavior
- platform UI
- multiple-controller/user handling
- certification and performance work

---

# Mode Identity Summary

| Mode | Core fantasy | Menu approach | Demand growth | Missed customer |
| --- | --- | --- | --- | --- |
| **Quick Play** | Play the restaurant session you want | Player chooses available recipes | Player choice | Normally continue |
| **Endless** | Survive as long as possible | Player chooses available recipes | Mandatory rising workload, modified by complexity/table mix | Run ends |
| **Career** | Build a restaurant company | Starter group → Mastery depth + Reputation breadth | Reputation/growth driven | Business consequence, continue |

The three modes should share the same core cooking, Expo, restaurant, layout, employee and content systems while applying different progression and failure rules.