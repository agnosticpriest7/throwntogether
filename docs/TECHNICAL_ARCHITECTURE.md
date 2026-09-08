# Thrown Together — Technical Architecture

## Engine / platform
- Unity 6
- Universal Render Pipeline
- Windows development
- Web build for rapid couch/browser testing
- Native Windows build for eventual Steam release

## Tools
- Codex
- GPT-6 Astra for high-value architecture/gameplay work
- Unity MCP for live Editor control and inspection
- GitHub
- Unity Test Framework
- Unity Input System

## Camera / world
- fixed elevated camera
- orthographic or mild perspective
- mostly planar gameplay
- stylized 3D
- grid-aware placement where useful
- smooth analogue movement

## Input
Support:
- one local human player
- optional second local human player
- gamepads
- keyboard/debug fallback
- Web controller testing

Touch may exist as a development convenience but should not drive core architecture.

## Scene strategy
Prefer focused scenes such as:
- Bootstrap
- MainMenu
- Restaurant
- Development/Test scenes

Avoid one giant monolithic scene.

## Suggested project structure

Assets/
- Art/
- Audio/
- Data/
  - Ingredients/
  - Recipes/
  - Appliances/
  - Staff/
  - Upgrades/
- Materials/
- Prefabs/
  - Characters/
  - Customers/
  - Staff/
  - Kitchen/
  - Furniture/
  - Appliances/
  - UI/
- Scenes/
- Scripts/
  - Core/
  - Input/
  - Interaction/
  - Cooking/
  - Inventory/
  - Economy/
  - Restaurant/
  - Customers/
  - Staff/
  - Persistence/
  - UI/
- Tests/
  - EditMode/
  - PlayMode/

Do not reorganize working content merely to match this exactly.

## Data-driven content
Strong candidates for ScriptableObjects:
- IngredientDefinition
- RecipeDefinition
- ApplianceDefinition
- EmployeeRoleDefinition
- EmployeeTraitDefinition
- RestaurantUpgradeDefinition
- AdvertisingCampaignDefinition

New recipes should normally be content/data, not bespoke engine code.

## Employees
Use deterministic task-driven game AI. Do not use an LLM inside the runtime game for ordinary employee behavior.

Design for roles, capabilities, wages, assignments, experience, and later promotion/transfer.

## Customers
Use deterministic state/task simulation such as arrival, waiting, seating, ordering, waiting for food, eating, and departure.

## Persistence
Use versioned save data that can migrate as the game evolves.

## Multi-location later
Do not continuously render off-screen restaurants. Simulate their economic/operational results.

## Testing
EditMode: economy, inventory, recipes, wages, reputation, discounts, serialization, progression rules.

PlayMode: movement, interaction, cooking, employee/customer behavior, local join, vertical slices.

## Completion
Major features should compile, pass relevant tests, create no new unexplained Console errors, receive Play Mode validation when practical, and produce a successful Web build when they affect the playable game.

## Agent-friendly architecture
- isolate systems
- use prefabs
- use data assets
- minimize shared-scene churn
- avoid unnecessary hard references
- keep code ownership boundaries understandable
