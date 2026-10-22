# Lobby UI Skin Replacement Prompt

Implement a visual UI replacement for the Lobby / Main Menu using the provided menu asset sheet.

## Asset Reference

Use the provided menu asset image as the source for:

- the **main logo**
- the **Start Game** button
- the **Settings** button
- the **Exit** button

The logo at the top should be used **as-is**.

The button sheet appears to contain **three visual state columns**:

1. **Column 1** = default / idle state
2. **Column 2** = hover / highlighted state
3. **Column 3** = pressed / clicked state

Use that interpretation unless inspection of the sliced sprites clearly shows otherwise.

## Scope

Apply this UI skin replacement to the Lobby / Main Menu.

Replace the current menu visuals for:

- title / logo
- Start Game button
- Settings button
- Exit button

This is primarily a **UI art integration task**.

Do **not** redesign the menu flow or button behavior unless required for clean integration.

## Preserve Existing Functionality

Keep the current behavior unchanged:

- Start Game should still open the stage / lobby flow exactly as it currently does.
- Settings should still open the settings screen / panel exactly as it currently does.
- Exit should still perform the same exit behavior it currently does.

Do not break scene routing, button callbacks, input navigation, or menu flow.

## Implementation Requirements

### 1. Sprite slicing / import
Import the provided menu asset as a sprite sheet if needed.

Slice it cleanly into:
- one logo sprite
- Start Game button states
- Settings button states
- Exit button states

### 2. Button state mapping
Map the three button-state columns to Unity UI button states.

Preferred mapping:

- **Normal** -> column 1
- **Highlighted / Hovered** -> column 2
- **Pressed** -> column 3

If the current UI system uses:
- Unity UI Button + SpriteSwap, or
- custom image swapping logic,

reuse the existing architecture where practical.

### 3. Logo
Use the logo graphic at the top **without redesigning it**.

The logo should replace the current title/logo in the Lobby / Main Menu.

Preserve readability and scale it appropriately for the current layout.

### 4. Layout
Update the menu layout so the new graphics fit cleanly.

Goals:
- keep the logo centered and prominent
- keep the three main buttons aligned and readable
- preserve consistent spacing
- do not stretch the art disproportionately
- keep the UI visually coherent in the current game resolution

You may adjust anchors, image sizes, and spacing as needed.

### 5. Text handling
If the text is already baked into the provided button art, do not duplicate text labels on top unless needed for accessibility or the current UI architecture.

If duplicate text would appear, remove or disable the redundant overlay text.

### 6. Button interactivity
Keep the buttons fully clickable and preserve hover / press feedback using the provided visuals.

Do not reduce usability.

### 7. Existing UI architecture
Inspect the current Lobby / Main Menu implementation first.

Reuse existing:
- Canvas
- Button components
- event bindings
- scene transitions
- input handling

Prefer changing visuals and layout, not rewriting the whole menu system.

## Optional Cleanup

If the current menu has placeholder styling, remove or replace it only where needed for the new asset integration.

Do not perform unrelated UI redesign.

## Verification

Verify at minimum:

1. The provided logo appears in the Lobby / Main Menu.
2. The Start Game button uses the correct default, hover, and pressed visuals.
3. The Settings button uses the correct default, hover, and pressed visuals.
4. The Exit button uses the correct default, hover, and pressed visuals.
5. Button callbacks still work correctly.
6. The menu remains visually readable.
7. No duplicate text is rendered unintentionally.
8. Hover and click feedback is visible in play mode.
9. Layout remains usable at the current target resolution.

## Final Report

After implementation, report:

1. Which UI scene/prefabs/files were modified.
2. How the asset was imported/sliced.
3. How button states were mapped.
4. Whether text overlays were removed or preserved.
5. Any layout adjustments made.
6. Tests actually performed.
7. Any remaining limitations or unverified behavior.
