# Modular chef prototype

The approved broad, softly squared head is shared by every chef. Following Kyle's September 9 direction, the original single-build scope now includes Slim, Standard, and Fuller torsos. These are modest visual width/depth changes, with matching apron geometry. Movement speed, collision, head size, carry anchor, and interaction reach remain shared.

## Try it

Open RestaurantShift and enter Play Mode. In the Editor, open the menu with Escape and choose **Choose your chef**. In a standalone build, **Quick Play → kitchen → Your chef → Ready — start cooking** makes clothing selection available before gameplay starts.

Use the player row to configure Player 1 or Player 2, including before P2 joins. Left/right changes an option; Enter/A advances it. Mouse buttons also advance choices. Select None, Waist apron, or Bib apron; clothing color and body color are independent. Separate Face and Accessories pages select eyes, mouth, hair, headwear, and glasses. Headwear temporarily hides the selected tuft; removing it restores the hair choice.

Choices last for the application session, across kitchen loads and P2 leaving/rejoining. They are not yet saved between application launches. There was no existing appearance-save infrastructure; audio/display settings storage is unchanged.

## Assets and implementation

- `Assets/Art/Characters/ModularChef.fbx`: Blender-authored meshes and one seven-bone skeleton, imported as Generic.
- `Assets/Resources/ChefVisual.prefab`: reusable visual and wardrobe preview source.
- `Assets/Prefabs/VerticalSlice/Chef.prefab`: existing controller/input/carry prefab with its old placeholder graphics replaced by ChefVisual.
- `Assets/Scenes/CharacterReview.unity`: four example looks in all three builds, with no gameplay scripts.
- `ChefAppearanceData` and `ChefWardrobe`: appearance choices independent of the controller.
- `ChefAppearance`: mesh selection, color overrides, and shared procedural walk/carry poses. It never changes the controller root or carry slot.
- `RestaurantMenu`: opening wardrobe step and paused wardrobe access. `LocalCoopSession` applies the second player's choices at join.
- `ChefWardrobePreview`: isolated render preview on layer 31, positioned outside the kitchen, released when its scene unloads.

The editable Blender source and initial 2D sheet live in the workspace's sibling `Character Art` folder. `modular-chef-v02.blend` is the current source; `build_characters.py` documents initial construction and is intended for a fresh Blender file. No Blender runtime is needed by Unity or the game.

Faces use small triangulated graphics on the flat central face surface, with separate eyes and mouths rigidly weighted to the head. The ink is unlit and double-sided; the tooth has its own shallow offset. This is a surface-geometry prototype rather than protruding eyeballs. All feature choices remain separate for later blinking/expression work.

## Prototype limits

These are procedural prototype meshes, not finished production art. The rig uses rigid toy-part weights and procedural limb swings/carry positioning, not authored animation clips. Elbows, nuanced reaches, facial animation, final hair sculpting, apron cloth motion, and deformation polish remain future work. The preview supports static option review; it is not a polished character-creation menu.

Human follow-up: inspect silhouettes and carried food at TV distance, compare the three builds, and check physical couch co-op/controller navigation. No physical Xbox or controller testing is implied by automated input tests.

## Validation, September 9

34 Edit Mode and 33 Play Mode tests passed. Character coverage checks all build/clothing combinations against the unchanged motor and carry anchor; independent eye/mouth combinations; headwear removal restoring hair; imported face triangles; the forward carry pose; the pre-kitchen wardrobe gate; and P2 joining in their chosen build/clothing. Existing cooking, co-op, settings, and audio tests also passed.

Editor visual checks covered the four example looks, three unclothed builds, the opening wardrobe preview, and a live chef moving and holding a clean plate at the actual gameplay camera angle. A close-up comparison additionally checked all three builds in bib aprons carrying actual Carryable plates through their existing carry slots; no obvious apron/arm clipping was seen in that pose. The latest Console check contained no warnings or errors.

The isolated WebGL build succeeded at `Builds/CharacterPrototypeWeb`, using the current working source and the existing strict build method. The first restricted process could not connect to licensing; it was stopped and the successful retry ran with access to the existing local licensing service. The output has not been published or physically browser/controller-tested.
