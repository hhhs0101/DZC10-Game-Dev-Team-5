# Codex Implementation Prompt — Lobby Top-Corner Back & Settings Button Sprite Replacement

## Goal

Replace the **existing top-left and top-right corner buttons** in the game's current Lobby UI using the supplied `Pixel-Art Back and Settings Button Sprite Sheet(6).png`.

This is a **visual/UI skin replacement**, not a navigation or menu-system redesign. Inspect the actual Lobby scene, its hierarchy, and button event bindings before making changes. Keep all existing functionality and scene transitions intact.

## Source Asset

- Source image: `Pixel-Art Back and Settings Button Sprite Sheet(6).png`
- Image dimensions: **1672 × 941**, RGBA with transparent background.
- There are **two rows × three columns** of illustrated buttons:

| Row | Column 1 | Column 2 | Column 3 |
|---|---|---|---|
| Back (top row) | Normal / idle | Hover / highlighted | Pressed / pointer down |
| Settings (bottom row) | Normal / idle | Hover / highlighted | Pressed / pointer down |

Use the above mapping as requested, even though the third-column styling appears muted/gray. Treat the third column as **Pressed**, not automatically as Disabled. Preserve separate disabled behavior if the existing application needs it.

The visual content of each button already contains **its icon and label**; do not draw another text or icon overlay on top.

## Locate and Match the Existing Buttons

1. Find the current Lobby view and identify the buttons anchored near the **top-left** and **top-right**.
2. Check the actual purpose and callback of each existing button. The desired graphics are:
   - **Back** visual for the existing back/navigation button (normally top-left).
   - **Settings** visual for the existing settings button (normally top-right).
3. If placement differs, preserve the existing button's **function** when matching the sprite. Do not change what a button does just to match a position. If the current corner buttons have different purposes, do not silently repurpose them; report the mismatch.
4. Modify only these **corner buttons**. Leave the already-customized main menu logo, central Start Game / Settings / Exit buttons, the lobby background, deck editor, and unrelated UI unchanged.

## Sprite Import and Slicing

Import the sheet into the Unity project under an appropriate UI sprite asset directory. Do not rely on an external filesystem path at runtime.

Extract six **independent button sprites**:

- `Back_Normal`
- `Back_Hover`
- `Back_Pressed`
- `Settings_Normal`
- `Settings_Hover`
- `Settings_Pressed`

**Important:** Slice each button around its actual visible artwork, preserving all edge pixels and transparent padding needed for a clean cutout. Do not include neighboring buttons or large unused transparent margins. Prefer correctly aligned dimensions/pivots across the three states of each button so that hover/press never causes the button to jump in position or size.

The artwork approximately occupies these regions of the original image (pixel coordinates from top left, **visual guides, not authoritative cutting boundaries**):

| Sprite | Approx. X extent | Approx. Y extent |
|---|---:|---:|
| Back / Normal | 35–562 | 258–439 |
| Back / Hover | 577–1093 | 258–439 |
| Back / Pressed | 1111–1636 | 258–439 |
| Settings / Normal | 35–562 | 500–682 |
| Settings / Hover | 577–1093 | 500–682 |
| Settings / Pressed | 1111–1636 | 500–684 |

Inspect transparency and the final extracted images; exclude stray pixels that may appear outside the actual buttons elsewhere in the sheet.

Recommended pixel-art import configuration, subject to the project's existing UI pipeline:

- Texture Type: **Sprite (2D and UI)**.
- Alpha transparency preserved.
- Filter Mode: **Point (no filter)**.
- Mip Maps: disabled for UI sprites, where applicable.
- Avoid lossy compression or resampling that blurs pixel edges.
- Use consistent PPU and UI sizing so each button state renders at the same apparent dimensions.

## Unity Button Integration

Reuse the current `Button` components and `onClick` listeners; **do not replace callbacks**.

Use `Button.Transition = Sprite Swap` (or the project's existing equivalent) with:

- `Image.sprite`: normal sprite.
- `SpriteState.highlightedSprite`: hover sprite.
- `SpriteState.pressedSprite`: pressed sprite.
- `SpriteState.selectedSprite`: hover sprite if keyboard/controller focus visuals require it; otherwise preserve the project's existing selected-state behavior.

Ensure the pressed image is shown only while pressed and transitions correctly on release. Do not keep a pressed state permanently after navigation or pointer release.

Use the provided artwork **as a single pre-labeled button image**. Remove or disable redundant child `Text`, `TextMeshProUGUI`, or duplicate icon components on these two corner buttons only; do not inadvertently remove text used by accessibility or another screen.

## Position, Scale, and Interaction

- Keep the **Back** and **Settings** buttons in their existing top-corner positions.
- Preserve the established anchors, safe-area offsets, and intended UI alignment.
- Adjust width/height only as necessary to keep the artwork readable and its aspect ratio correct.
- Do not stretch the sprites non-uniformly.
- Keep each button's `RectTransform` click target large enough to remain easy to use.
- Ensure the button remains above the lobby background and below no pointer-blocking decorative images.
- Preserve existing responsive behavior at the project's supported aspect ratios and resolutions.

## Functional Non-Regression

Do **not** alter the following:

- Back navigation's destination or state-handling behavior.
- Settings panel opening/closing behavior.
- Lobby/background visuals outside the two corner buttons.
- Main menu Start Game / Settings / Exit buttons.
- Stage selection and Deck editor workflows.
- Persistent Deck presets, gameplay saves, or scene routing.

Do not add new menu functionality or modify gameplay systems.

## Verification and Acceptance Criteria

1. The **existing top-left back control** displays `Back_Normal` when idle, `Back_Hover` on hover, and `Back_Pressed` during pointer down.
2. The **existing top-right settings control** displays `Settings_Normal`, `Settings_Hover`, and `Settings_Pressed` for the same states.
3. No state switch causes the buttons to move, flicker, change hitbox, or change size.
4. Original Back and Settings actions still work as before.
5. Mouse and existing keyboard/controller interaction (if supported) still work.
6. No duplicate text or icons appear on the buttons.
7. The imported sprites remain crisp and transparent.
8. The central logo/menu controls, existing lobby background, and unrelated UI are unchanged.
9. Layout is usable at the game's target resolution and representative alternate aspect ratios.

## Implementation Instructions and Report

1. Inspect the current Unity Lobby scene, prefab and scripts to identify exactly which two objects to change.
2. Import/slice the supplied sprite sheet and configure the two existing buttons.
3. Perform applicable editor/build checks and runtime/play-mode visual checks where tools permit. Do not claim manual tests were performed if they were not.
4. Report:
   - Scenes, prefabs, assets, and scripts modified.
   - Final Back/Settings sprite-state mapping.
   - Any sizing/anchor and event-system adjustments.
   - Tests run and pass/fail outcomes.
   - Any unverified items or blockers.

**Do not remove, rewire, or duplicate the original two buttons' click handlers.**
