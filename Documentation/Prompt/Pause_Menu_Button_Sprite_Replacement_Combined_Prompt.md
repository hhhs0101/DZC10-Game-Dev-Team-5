# Pause Menu Button Sprite Replacement Combined Prompt

Implement a UI skin replacement for the in-game Pause menu using the provided sprite sheet.

## Goal

Replace the current Pause menu buttons with the provided pixel-art button sprites.

Apply this to the Pause menu buttons:

- Resume
- Settings
- Exit

## Asset Interpretation

The provided sprite sheet appears to use 3 columns per button row:

- **Column 1** = Normal / Idle
- **Column 2** = Hover / Highlighted
- **Column 3** = Pressed / Clicked

Use this mapping unless inspection of the current UI implementation clearly requires a different state assignment.

## Scope

This is a **Pause menu UI visual replacement task**.

Replace only the visual presentation of the Pause menu buttons while preserving the existing Pause menu behavior.

Do not redesign the pause flow.

## Preserve Existing Functionality

Keep all current Pause menu functionality unchanged:

- **Resume** should unpause the game and close the Pause menu.
- **Settings** should open the existing settings screen/panel exactly as it does now.
- **Exit** should keep the existing exit flow and confirmation behavior.
  - If the current implementation asks for confirmation before leaving the stage, preserve that behavior.
  - If the current implementation routes back to the Lobby or Main Menu, preserve that routing.

Also preserve:

- Esc key pause/resume behavior
- overlay / darkened background behavior
- existing callbacks
- navigation / focus behavior
- timeScale pause logic
- any existing confirmation popup behavior

## Implementation Requirements

### 1. Import / Slice the Sprite Sheet
Import the provided image properly as a UI sprite sheet if needed.

Slice the asset into:

- Resume (Normal / Hover / Pressed)
- Settings (Normal / Hover / Pressed)
- Exit (Normal / Hover / Pressed)

### 2. Replace Existing Pause Menu Button Visuals
Update the current Pause menu buttons so they use the new sprites.

Preferred button-state mapping:

- Normal -> Column 1
- Highlighted / Hovered -> Column 2
- Pressed -> Column 3

If the existing UI uses:
- Unity UI Button with SpriteSwap,
- Transition settings,
- multiple Images,
- or custom button image logic,

reuse the current architecture where practical.

### 3. Text Handling
The provided button art already includes text and icons.

Therefore:
- avoid rendering duplicate overlay text on top of the button art
- remove or disable redundant text labels if they would duplicate the baked-in button text

If the current UI architecture requires text objects to remain for layout or accessibility, keep them hidden or non-overlapping.

### 4. Layout and Sizing
Adjust button size and layout so the new art fits cleanly within the Pause menu.

Goals:
- preserve readability
- preserve consistent spacing
- avoid distorting the pixel art
- keep the menu centered and clean
- maintain usability at the current target resolution

You may adjust:
- RectTransforms
- anchors
- button image sizes
- spacing between buttons

Do not change the overall Pause menu meaning or button order unless required for clean integration.

### 5. Pixel-Art Presentation
If the project uses pixel-art UI presentation, keep import settings and filtering consistent with the rest of the UI.

Avoid blurry scaling where practical.

### 6. Confirmation Flow
If Exit currently opens a confirmation dialog such as “Really exit this stage?” with Yes/No:
- keep that logic unchanged
- only replace the Pause menu button art itself unless a separate task later asks to skin the confirmation popup too

## Verification

Verify at minimum:

1. Opening the Pause menu still works.
2. Resume button shows the correct Normal, Hover, and Pressed sprites.
3. Settings button shows the correct Normal, Hover, and Pressed sprites.
4. Exit button shows the correct Normal, Hover, and Pressed sprites.
5. Resume still unpauses correctly.
6. Settings still opens the correct screen/panel.
7. Exit still preserves its current flow and confirmation behavior.
8. No duplicate text appears on top of the button art.
9. Button alignment and readability remain acceptable.
10. Esc-based pause flow still works.

## Final Report

After implementation, report:

1. Which Pause menu scene/prefabs/files were modified.
2. How the sprite sheet was imported/sliced.
3. How button states were mapped.
4. Whether redundant text objects were removed/disabled.
5. Any layout/size adjustments made.
6. Tests actually performed.
7. Any remaining issues or unverified behavior.
