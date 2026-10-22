# Codex Implementation Prompt — Game Over & Victory Button Sprite Replacement

## Goal

Replace the **existing Game Over and Victory screen button graphics** with the corresponding sprites from the provided image asset, while preserving every button's current behavior.

This is a **UI skin replacement**, not a gameplay redesign.

**Source sprite sheet:** `Retro RPG Button State Grid(1).png` (attached/provided with this task).

The image has a transparent background and contains 3 button rows x 3 visual-state columns.

## Exact screen and button mapping

| Screen | Existing button to skin | Sprite row | Sprite columns |
| --- | --- | --- | --- |
| **Game Over / Defeat** | **Retry** | **Row 2** | Normal = Col 1, Hover/Highlighted = Col 2, Pressed = Col 3 |
| **Game Over / Defeat** | **Lobby** | **Row 3** | Normal = Col 1, Hover/Highlighted = Col 2, Pressed = Col 3 |
| **Victory / Stage Clear** | **Replay** | **Row 1** | Normal = Col 1, Hover/Highlighted = Col 2, Pressed = Col 3 |
| **Victory / Stage Clear** | **Lobby** | **Row 3** | Normal = Col 1, Hover/Highlighted = Col 2, Pressed = Col 3 |

**Rows are counted from top to bottom; columns from left to right.**

- Row 1 = Replay, across its 3 state variants.
- Row 2 = Retry, across its 3 state variants.
- Row 3 = Lobby, across its 3 state variants.
- Col 1 = normal/idle appearance.
- Col 2 = hover/highlighted appearance.
- Col 3 = pressed/click appearance, following the existing UI sprite-state convention in this project.

The darker third-column appearance may look like a disabled state, but **for this change explicitly use it for Pressed**, as requested. Preserve the button's **actual interactable/disabled logic** independently. Do not accidentally make enabled buttons inactive.

There must be **exactly two functional choices** in each result screen:

```text
Game Over:
    Retry
    Lobby

Victory:
    Replay
    Lobby
```

Do **not** show Replay on Game Over or Retry on Victory.

## Step 1 — Inspect the current project before editing

Locate the current result screens, their UI prefabs/scene objects, and the existing controllers/callbacks responsible for:

- displaying Game Over and Victory;
- Retry / Replay stage restart;
- returning to Lobby;
- ending gameplay, setting time scale, and resetting stage state;
- paying Stage Clear rewards and preserving persistent progress.

The same Lobby graphic may be used in both screens, but each button must preserve its own current callback and lifecycle.

Reuse the existing Canvas, UI Buttons, result-screen controllers, and event routing. Do not introduce new result screens or replace gameplay controllers.

## Step 2 — Import and slice the supplied sprite sheet

1. Import/copy the provided image into the existing UI art folder in the Unity project, without modifying the source artwork.
2. Use Unity UI-compatible `Sprite (2D and UI)` assets. Preserve the source image's alpha transparency.
3. Extract nine button sprites using precise per-button rectangles, **not naïve equal 3x3 tiles**: this sheet has substantial transparent padding above the artwork and between button rows/columns.
4. For reference, the source image is **1448 x 1086 px**. The following alpha-content rectangles were measured in **top-left-origin image coordinates** (left, top, right-exclusive, bottom-exclusive); use a few transparent pixels of padding if desired to avoid clipping the border:

   - Replay: C1 `(18,248,483,399)`; C2 `(499,248,949,399)`; C3 `(964,248,1430,399)`.
   - Retry: C1 `(18,455,483,607)`; C2 `(499,455,949,607)`; C3 `(964,455,1430,607)`.
   - Lobby: C1 `(18,662,483,816)`; C2 `(499,662,949,816)`; C3 `(964,662,1430,816)`.

   These are image-space reference coordinates, **not Unity Sprite Editor bottom-left-origin rect values**. Convert coordinates as required by the import tooling, or detect the sprites from their actual alpha bounds. Ensure no border, bolt, or pixel-art edge is cut off.
5. Use descriptive names, for example `Replay_Normal`, `Replay_Hover`, `Replay_Pressed`, and likewise for `Retry` and `Lobby`.
6. Use pixel-art-friendly import settings where appropriate (such as Point filtering, no destructive compression or mipmaps for UI), keeping the visual style consistent with the other already-skinned menus.

## Step 3 — Apply to the two result screens

For each of the four button placements:

- Retain the existing `Button` component and its `onClick` callback(s).
- Assign the row's Column 1 sprite as the button's normal `Image` sprite.
- Prefer Unity UI `Button.Transition = Sprite Swap` when compatible with the current implementation; map Column 2 to Highlighted and Column 3 to Pressed.
- Keep any existing `Selected`/keyboard-controller focus behavior functional. Reuse the hovered look for focused selection if appropriate.
- Preserve true interactability/disabled behavior rather than misusing the pressed art to disable the button.
- The artwork **already contains its own text** (`Replay`, `Retry`, `Lobby`). Hide/remove redundant visible Text/TMP overlays so no duplicate labels overlap the artwork. Preserve necessary hidden accessibility or controller references when applicable.
- Maintain consistent button scale and visual proportions; don't stretch the art non-uniformly.
- Fit each button cleanly into the existing result panel, with sufficiently large and aligned clickable hit areas. Minor RectTransform, anchoring, and spacing adjustments are permitted.

If the same `Lobby` sliced sprites can be reused on both screens, reuse them rather than duplicating the imported textures.

## Step 4 — Preserve all current behavior

**Game Over / Defeat**

- `Retry` must perform the project's existing retry/restart-current-stage action.
- `Lobby` must perform the existing return-to-lobby action.

**Victory / Stage Clear**

- `Replay` must perform the project's existing replay-current-stage action.
- `Lobby` must perform the existing return-to-lobby action.

**Both screens**

- Keep the existing win/loss detection, result screen display conditions, pause/time-scale handling, UI/input blocking, focus/navigation, and scene routing.
- Prevent double-clicking or repeat button events from causing duplicate scene transitions where the project already protects against that.
- Do not change Stage Clear reward logic; repeated clears must keep the existing reward policy, and a single Victory event must not grant rewards twice.
- Preserve Gold/Candy persistence, Stage progress, Deck presets, cards, Tower/Enemy/Skill behavior, and all other systems.
- This task must not change main menu, Lobby, Pause, or other button visuals.

## Verification / acceptance criteria

Verify the following in the Unity Editor when possible:

1. Trigger defeat: **Retry** and **Lobby** appear using their Row 2 and Row 3 sprites, respectively; there is no Replay button.
2. Trigger victory: **Replay** and **Lobby** appear using Row 1 and Row 3 sprites; there is no Retry button.
3. Each button displays Column 1 while idle, Column 2 on hover/highlight, and Column 3 while pressed.
4. No overlapping/duplicate text appears, and none of the sprite borders are clipped.
5. Retry restarts the current stage via the existing flow.
6. Replay replays the current stage via the existing flow.
7. Both Lobby buttons return to Lobby normally.
8. The game is not stuck in a paused time-scale state after retry/replay/scene navigation.
9. Stage Clear rewards and saved progress remain correct, including repeated clears and retries.
10. Existing UI controls outside these two result screens remain unchanged.
11. The Console has no new exceptions or missing-sprite references.

If a behavior cannot be manually tested, identify it as **not verified** rather than claiming success.

## Scope constraints

- No gameplay balance changes.
- No new result-screen mechanics or button destinations.
- No modifications to Tower, Enemy, Skill, Card, Deck, Elixir, Gold, or Candy mechanics.
- No redesign of unrelated UI screens.
- No rewriting the Unity UI architecture when a minimal sprite-swap integration will suffice.

## Final implementation report

Report:

1. UI prefabs/scenes/scripts and art files modified.
2. The nine sliced sprite assets and their state/row mappings.
3. How existing onClick callbacks were preserved.
4. Any layout or duplicate-text changes.
5. Tests executed and their results, distinguishing automated from manual checks.
6. Any unresolved problems or unverified behavior.

**Implement this task now in the current Unity project.**
