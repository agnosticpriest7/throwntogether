# Thrown Together — Playtest Feedback

### Room to move — owner-requested layout adjustment
Kyle reports the 0.2.0 level feels claustrophobic. Both practice and shift now use a 20% larger floor footprint and station spacing, with unchanged station/chef sizes. Counter-row spacing increases from 3 to 3.6 units; the fixed camera's orthographic size increases from 6.5 to 7.8 to frame the room. Movement, interaction reach and cooking timings stay unchanged. Physical follow-up: assess two-chef passing room, TV readability and the slightly longer travel between stations.

Use this for observations from actual playable builds, not speculative ideas.

## Template

### Build / Date

**Environment**
- Platform:
- Input:
- Players:

**What felt good**
-

**Problems**
-

**Requested changes**
-

**Needs another test**
-

**Resolved**
- [ ]

### Xbox Edge physical feedback — native mode menu
Reported by Kyle: development page remains in Browsing Controls; Unity Pads: 0; D-pad appears to arrive as keyboard arrows; left stick and face buttons do not reach Unity as gamepad input. The usual hold-Menu → Use game controls action could not be enabled on this page, although Kyle uses it on other games on the same Xbox. Exact Xbox/Edge versions were not provided. Shell compatibility changes are awaiting physical retest; unresolved.

### 0.3.0 physical feedback and approved overnight follow-up
Kyle reports co-op works great and distinct food shapes work great. Item/station nameplates and opaque panels obscure the chef; he requests recognizable objects without nameplates. He requests ordinary-counter plating instead of a dedicated plating station: put food down, then add a plate. Earlier room expansion was reported more comfortable. These are owner observations, not agent-performed physical tests.

0.4.0 review focus: identify stations/food without labels, check visibility while holding each state, assemble in both orders on both counters, and assess the compact HUD at TV distance. Counter assembly now leaves the dish on the counter, requiring one pickup before serving. Confirm the updated interaction is intuitive in solo and co-op.

### 2026-09-10 — Cannot serve at customer tables
Kyle could not place/serve dishes at the visible dining tables in 0.10.0. Reproduced by walking to the authored table with a matching plated meal: no focus target. Fixed a misplaced runtime interaction component on the separate order-logic root. Regression now approaches real table surfaces, covers both tables and dirty-plate clearing, and verifies the server's delivery location. TV/controller confirmation required after a full Web-page refresh to 0.10.1.
