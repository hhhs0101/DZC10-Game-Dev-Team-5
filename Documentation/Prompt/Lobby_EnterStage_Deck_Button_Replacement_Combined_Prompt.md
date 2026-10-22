# Codex Implementation Prompt — Replace Lobby Enter Stage and Deck Buttons

## Goal

Update the **existing Lobby screen** in the current Unity project. Replace the visual artwork of the existing **Enter Stage** and **Deck** buttons with the corresponding sprites from the supplied image:

`Pixel-Art Enter Stage and Deck Buttons(5).png`

This is a **UI visual/state replacement**, not a menu redesign. Preserve all existing button behavior, scene navigation, deck editing, save data, and other Lobby controls.

## Source Asset and Sprite Layout

The supplied PNG is a transparent **2048 × 682** sprite sheet with two rows and three columns of buttons:

| Row | Left column | Middle column | Right column |
| --- | --- | --- | --- |
| Top | Enter Stage — Normal | Enter Stage — Hover/Highlighted | Enter Stage — Pressed |
| Bottom | Deck — Normal | Deck — Hover/Highlighted | Deck — Pressed |

Use this state mapping to match the other previously replaced Lobby buttons. The rightmost artwork looks muted; nevertheless, this patch treats it as the **Pressed** state under the established three-column convention. **Do not replace the existing Disabled state with the Pressed state without checking current behavior.** If the current Enter Stage button must become non-interactable for locked stages or other conditions, retain a clearly distinguishable Disabled state (reuse the prior disabled visual or provide an appropriate fallback).

**Important:** Slice each visible button into an individual sprite using its actual artwork bounds and transparent margins. Do not assume that a naive equal-sized 3×2 grid will align perfectly. Preserve the outer border, shadow and edge detail, and avoid including any pixels from adjacent buttons. Use Unity's Sprite Editor or an equivalent editor/import pipeline already in the project.

## Implementation Steps

1. **Inspect the existing UI first.** Find the actual Lobby Canvas, the Enter Stage and Deck `Button` components, their `Image` components, the handlers they invoke, and any code controlling the `interactable` state. Avoid changing unrelated Main Menu, Stage Select, Deck Editor, or Settings controls.
2. **Import the supplied PNG into the Unity project** at the project's existing UI art directory. Keep the original source intact, and create appropriate sprite sub-assets or individually sliced textures. Do not reference the user's Downloads directory at runtime.
3. **Configure pixel-art-friendly import settings:** Sprite (2D and UI), correct transparency/alpha, Point filtering for crisp pixels, no mipmaps, and avoid lossy compression or filtering that blurs the art. Keep Pixels Per Unit consistent with the current UI import approach where applicable.
4. **Replace the existing Enter Stage button's visuals:** Normal = top-left, Highlighted/Hover = top-middle, Pressed = top-right. Preserve the existing `Button.onClick`, interactable gating, keyboard/controller navigation where present, and click hitbox.
5. **Replace the existing Deck button's visuals:** Normal = bottom-left, Highlighted/Hover = bottom-middle, Pressed = bottom-right. Preserve the existing `Button.onClick`, transition to the deck editor, preset functionality, and click hitbox.
6. **Prefer Unity UI `Button` + `Image` with `Transition = Sprite Swap`** if that fits the current architecture. If a custom button-state component already exists, reuse it rather than introducing a second competing state mechanism. Ensure hovered, pressed, selected, and disabled visuals do not get stuck after navigation or clicking.
7. **Prevent duplicate labels.** The supplied button sprites already contain the English labels and do not need extra overlay text. Disable/remove only redundant `Text`/TextMeshPro overlays on these two buttons, without affecting text or accessibility elsewhere.
8. **Preserve layout and UI stacking.** Keep the buttons in their existing positions and respect current anchors/Canvas Scaler. Fit sprite art without stretching out of proportion or clipping its shadows. Maintain correct draw order above the Lobby background and below any necessary overlays. Do not alter the previously installed wooden table Lobby background, logo, Back, or Settings graphics.
9. **Keep behavior unchanged.** In particular, do not modify stage availability logic, stage loading, deck persistence, deck selection constraints, or interaction with the game-state manager. Treat this as an art swap.

## Behavioral Acceptance Tests

- The Enter Stage button displays the supplied **top-row** sprite for its Normal, Hover/Highlighted, and Pressed states.
- The Deck button displays the supplied **bottom-row** sprite for its Normal, Hover/Highlighted, and Pressed states.
- Mouse hover and press visibly change the displayed button artwork; releasing the mouse restores the correct state.
- Existing keyboard/controller selection feedback and disabled/interactable behavior still work, if supported by the project.
- Clicking **Enter Stage** triggers exactly the same stage-entry behavior as before.
- Clicking **Deck** opens the same Deck Editor as before; deck presets and saved data remain unaffected.
- No duplicate button text is rendered; buttons remain readable and clickable at the target resolutions.
- The sheet's transparent background does not show black rectangles or visual seams; pixel art remains crisp.
- The Lobby logo, background, Back/Settings buttons, and other scene UI elements are unchanged.

## Deliverables and Reporting

Implement the changes **in the current project**, not as a standalone demo. Report:

1. The scene, prefab, texture, sprite sub-assets, and scripts changed.
2. The final six sprite-to-state mappings.
3. How the sheet was sliced, with any adjustments to pivots, padding, button dimensions, or import settings.
4. What was done to preserve Disabled/interactable behavior.
5. Actual test results (clearly distinguish automated checks from manual play-mode verification).
6. Any unresolved issues.

Do not claim that the buttons were visually verified in Play Mode unless you actually performed that verification. Do not make unrelated gameplay, scene, or save-data changes.
