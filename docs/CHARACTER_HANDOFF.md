# Character prototype handoff — September 9, 2026

## Current status

The characters are implemented in the local Unity working tree. They have NOT been committed, pushed, or deployed to the live website. A separate local WebGL build succeeded. This explains why Kyle did not see them in the live version. This handoff does not itself authorize publication; Kyle asked for a summary to continue in the main Astra development chat.

Canonical Unity project/repository: `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity`.
Do not confuse this with the other `C:/Projects/ThrownTogetherUnity/ThrownTogether` directory.

`git status --short` was checked for this handoff: the implementation still consists of modified tracked files and new untracked assets/scripts, including Unity `.meta` files. Preserve the working tree. No changes were committed by this task.

## User direction and delivered work

1. Kyle supplied a character brief and asked for 2D prototypes, then Blender models, then Unity integration. The referenced prior image was unavailable; Kyle explicitly authorized an original interpretation of the written description.
2. Generated a 2D concept sheet with a broad, softly squared head, compact toy-like body, and four cosmetic examples. Created an initial undecorated Blender shape study. Kyle approved that style.
3. Kyle then explicitly expanded the original single-build scope: clothing selection at the beginning, plus slightly skinnier and slightly heavier bodies. Implemented Slim, Standard, and Fuller torso builds with matching aprons. All share the same head and seven-bone skeleton. The controller, collision dimensions, movement speed, and carry/interaction reach remain unchanged.
4. Created and imported a Blender-authored modular FBX, URP materials, and a reusable visual prefab. Replaced the old placeholder graphics in the existing chef prefab. Added shared procedural walking and carrying poses.
5. Added independent selection of body color, clothing type, clothing color, eyes, mouth, hair, headwear, and glasses. Clothing: None, Waist apron, Bib apron. Hair is temporarily hidden under any headwear and restored when headwear is removed.
6. Implemented the four example looks: teal/mustard cap/cream waist apron; peach/sleepy eyes/glasses/blue beanie/navy bib; lavender/happy eyes/coral headband/yellow bib; sky blue/oval eyes/tooth/dark tuft/rust bib. These are combinable choices, not locked presets.
7. Added the opening wardrobe step and live 3D preview using the existing menu. Configure either player's appearance before P2 joins. P2 receives its own selection when joining. Choices survive kitchen loads and leaving/rejoining during the same application session.

## How to review locally

In the Unity Editor, open RestaurantShift, enter Play Mode, then Escape → Choose your chef. Editor startup deliberately bypasses the standalone front-end gate.

In a standalone build: Quick Play → select kitchen → Your chef → Ready — start cooking. Selecting a kitchen now opens the wardrobe before loading the gameplay scene. Use the Player row for P1/P2; left/right adjusts an option, Enter/A advances or activates, and mouse buttons also work.

## Art sources and images (outside the Git repository)

- Approved 2D sheet: `C:/Projects/ThrownTogetherUnity/Character Art/Concepts/character-study-v01.png`
- Initial base study: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/shared-base-study-v01.blend`
- Current editable source: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/modular-chef-v02.blend`
- Construction script: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/build_characters.py` — intended for a fresh Blender file, not blind reruns in the current scene. The current saved blend is authoritative.
- Final four looks: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/four-chefs-v02.png`
- Slim/Standard/Fuller comparison, left to right: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/three-builds-v02.png`
- Wardrobe screenshot: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/wardrobe-menu-v02.png`
- All three builds carrying actual plates: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/carry-fit-v02.png`
- Actual gameplay camera screenshot: `C:/Projects/ThrownTogetherUnity/Character Art/Blender/gameplay-carry-v02.png`

The earlier `unity-character-review-v02.png` was an intermediate review before the final hat-color/band refinements. Prefer the final images listed above. The `.blend1` file is Blender's backup.

## Unity assets and implementation

New assets:
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Art/Characters/ModularChef.fbx`
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Materials/Characters/`
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Resources/ChefVisual.prefab`
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Scenes/CharacterReview.unity` — 12 examples: four looks × three builds. Enter Play Mode to apply their saved appearance settings.

New scripts:
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Scripts/ChefAppearanceData.cs` — choice data, example definitions, and session-scoped ChefWardrobe.
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Scripts/ChefAppearance.cs` — visible parts, material color overrides, and procedural bone posing.
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Scripts/ChefWardrobePreview.cs` — render-texture preview, isolated on layer 31 outside the kitchen; released on scene unload.
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Editor/AuthorCharacters.cs` — import/prefab authoring and review-scene creation. Outputs already exist; do not rerun blindly over later changes.
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Editor/CharacterValidation.cs` — local Test Runner API helper, writes Edit/Play result XML.

Modified integration files:
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Prefabs/VerticalSlice/Chef.prefab`
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Scripts/RestaurantMenu.cs`
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Scripts/LocalCoopSession.cs`

Tests:
- New `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Tests/EditMode/CharacterAppearanceTests.cs`
- Extended `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Assets/Tests/PlayMode/ClarityAndPracticeTests.cs`

Documentation:
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/docs/CHARACTER_PROTOTYPE.md`
- Updated `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/docs/DECISIONS.md`
- Updated `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/docs/USAGE_LEDGER.md`

## Validation and local build

34 Edit Mode tests and 33 Play Mode tests passed (67 total). Coverage includes build/clothing combinations preserving motor and carry anchor, independent faces, hair/headwear compatibility, imported face geometry, forward carrying, the opening wardrobe gate, and P2 appearance on join. Existing gameplay/settings/audio regression tests passed as well.

Results:
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/TestResults/characters-edit.xml`
- `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/TestResults/characters-play.xml`

Editor visual checks covered four looks, three builds, the menu preview, actual gameplay scale, movement, and carrying actual plates. No obvious apron/arm clipping was seen in the checked carry pose. The final source-Editor Console check had no warnings or errors, and `git diff --check` passed.

Local WebGL output: `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Builds/CharacterPrototypeWeb/index.html`
Build log: `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Builds/CharacterValidation/web-build.log`
Build driver: `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Builds/CharacterValidation/run-web.ps1`

The build used an isolated current-working-source snapshot in `C:/Projects/ThrownTogetherUnity/ThrownTogetherUnity/Builds/Workspace` and the existing strict BuildAutomation.BuildWeb method. First restricted attempt could not access Unity licensing; that owned process was stopped. The approved elevated retry succeeded. Source project settings were not switched to WebGL. The output is about 98.5 MB including streaming assets. It was NOT published and was NOT tested on a physical Xbox/controller or in a browser. WebGL output requires an HTTP server; double-clicking index.html is not a sufficient runtime test.

## Remaining work / cautions

- Review the uncommitted implementation before integrating with other main-chat work; include all new assets and their `.meta` files in any eventual commit.
- Source art is outside the repository and must be copied/versioned deliberately if desired.
- These are procedural prototype meshes with rigid toy-part skinning, not finished production art. Hair, cloth, elbows, deformation, and animation need polish. There are no authored walk/carry animation clips or facial animation yet.
- Faces are triangulated flat surface graphics on the head, not texture decals. They use unlit double-sided ink, separate eyes/mouths, and a shallow tooth layer. Earlier import warnings from duplicate polygon endpoints were fixed before final tests/build.
- Appearance choices currently reset on application restart. There was no existing appearance persistence system; audio/display settings storage was left unchanged.
- No progression, unlocks, alternate species, collision changes, or polished character-creation UI were introduced.
- To get this onto the live site, continue through the established main-chat review/commit/push/test/build/deploy workflow when publication is requested. The normal wrapper requires committed clean main; this local candidate was built separately specifically to validate uncommitted work.

## Subsequent integration status
The owner authorized review, commit, push and publication in the main development chat. This handoff is retained as historical provenance. Runtime integration was published as 0.7.0-dev from 944bc4253d99055cdaccadc6cd465902861564a2; see DEVELOPMENT_NOTES.md for final verification. Editable source art remains preserved in the external Character Art folder.
