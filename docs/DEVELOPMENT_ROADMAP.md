# Thrown Together — Development Roadmap

Directional, not rigid.

## Phase 0 — Foundation
- Unity project
- Unity MCP
- source control
- permanent project docs
- usage budgeting
- EditMode/PlayMode baseline
- Web build/deployment

## Phase 1 — Unity Vertical Slice
Build the smallest complete restaurant interaction:
- one player
- optional second local player
- open-ring kitchen
- potato storage
- prep
- fryer
- fries
- plate
- service pickup
- one customer
- complete cook-to-customer flow

## Phase 2 — Core Restaurant Night
- small menu
- purchasing
- inventory
- prep/service phases
- orders
- revenue
- dishes/dining basics

## Phase 3 — Persistent Restaurant
- persistent cash/inventory
- bulk buying
- reputation
- advertising
- equipment purchases
- finite kitchen
- kitchen/dining expansion

## Phase 4 — Employees
First:
- server
- dishwasher

Then:
- prep cook
- line/station cook

Add wages, scheduling, assignments, experience, and simple understandable skill differences.

## Phase 5 — Restaurant Depth
- broader ingredients/recipes
- more appliances
- tangible appliance upgrades
- layout editing
- menu/pricing depth
- demand/reputation tuning

## Phase 6 — Management
- supervisors/managers
- promotion
- transfers
- operating reports
- owner can step away from selected jobs

## Phase 7 — Multiple Locations
- establish second location
- equipment/layout/staff/menu
- employee transfers
- manager-operated off-screen simulation
- location reporting
- player may visit/work at any location

## Phase 8 — Campaign / Challenges
If still desired:
- authored challenges
- three-star goals
- unlock integration

## Phase 9 — Production Art / Polish
Commission a human visual slice before full replacement, then production characters, animation, food, appliances, environments, UI, audio/music, accessibility, and performance.

## Phase 10 — Steam
- native Windows production build
- Steam integration
- saves/settings
- QA
- store assets
- release pipeline

## Current checkpoint — after Vertical Slice #1

First-service gameplay is complete. The follow-up is limited to reusable data definitions, development diagnostics/build provenance, regression coverage and documented optional local-input pairing. Employee/advertising/upgrade definitions are inert schemas; their systems and balance remain in the future phases above.

Next human gate: physical Xbox/controller/TV playtest using docs/DEVELOPMENT_NOTES.md, including build ID and controller connection checks. Player 2 activation and its join/disconnect/restart UX follow that feedback and explicit authorization. No Vertical Slice #2, restaurant redesign or subjective feel adjustment is authorized by this checkpoint. Stop autonomous development after this foundation verification/deployment.

## Authorized 0.2.0 checkpoint
The owner approved playtest polish, optional active Player 2, and a short two-dish restaurant shift after physical Xbox first-service success. This supersedes the previous no-Vertical-Slice-2 checkpoint above. Scope: PCM Web audio compatibility, readable feedback/diagnostics, isolated co-op joining, and a separate six-order prototype shift with fries/mushrooms. No economy, employees or persistent restaurant progression is included. Physical co-op/audio/shift feedback is the next gate; do not automatically start subsequent roadmap phases.
