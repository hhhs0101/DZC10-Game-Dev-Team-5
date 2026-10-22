# Unity Codex Implementation Prompt — Wooden Stage Boards for the Lobby Carousel

## Goal

Replace the three current rectangular stage-preview cards in the existing **Lobby stage-selection carousel** with the supplied pixel-art wooden stage-board sprites.

**Critical state rule:** A stage in the **center / currently selected carousel position** uses the **bright sprite from column 1**. Stages in the **left and right side positions** use the **muted sprite from column 2**. This is a **carousel focus state**, **not** a stage unlocked/locked state, a hover state, or a pressed state.

Use the attached `Pixel Wood Stage Button Set(3).png` as the artwork. Do not redesign or regenerate the image.

## 1. Inspect the Existing Implementation First

Locate and inspect the Lobby scene, carousel controller, stage-preview card prefab/UI hierarchy, stage-selection input handling, stage-unlock checks, and `StageDefinition` references. Modify the existing view components with the smallest safe change. Do not rebuild the carousel or modify the data model unnecessarily.

Keep the current Lobby background, logo, Back/Settings buttons, Enter Stage button, Deck button, Gold/Candy display, and hand/deck preview unchanged.

## 2. Slice the Sprite Sheet

The source PNG has a transparent background and contains **three rows × two columns**:

| Stage board art | Column 1: selected/center | Column 2: side/unselected |
|---|---|---|
| Stage 1 | bright `Stage 1` board | muted `Stage 1` board |
| Stage 2 | bright `Stage 2` board | muted `Stage 2` board |
| Stage 3 | bright `Stage 3` board | muted `Stage 3` board |

Import as `Sprite (2D and UI)`, `Multiple`, preserve alpha, use Point filtering and appropriate pixel-art settings. Slice into **six separate, clean sprites**, cropping transparent padding without cutting off outlines or shadows. The source is 1402 × 1122 pixels. For reference, content bounding boxes measured in **top-left-origin image coordinates** are approximately:

- Row 1, column 1: `(x=88, y=133, width=593, height=267)`
- Row 1, column 2: `(x=721, y=133, width=595, height=267)`
- Row 2, column 1: `(x=88, y=425, width=593, height=268)`
- Row 2, column 2: `(x=721, y=424, width=595, height=269)`
- Row 3, column 1: `(x=88, y=718, width=593, height=266)`
- Row 3, column 2: `(x=721, y=717, width=595, height=267)`

These bounds are guidance, not mandatory exact import rectangles; Unity's Sprite Editor coordinate origin differs. Visually verify all six sprites.

## 3. Per-Stage Sprite Mapping

Each of the three stage entries needs a stable stage-art index (1, 2, or 3) that determines the **row**, while the item's position in the carousel determines the **column**.

For the current three-item carousel, map rows by the **existing stage order** unless the current project already has an explicit stage number mapping:

- First stage entry → `Stage 1` board pair
- Second stage entry → `Stage 2` board pair
- Third stage entry → `Stage 3` board pair

**Important existing-data mismatch:** The current Lobby screenshot labels stage entries `1-2`, `1-3`, and `Test Stage`, whereas the new art has baked-in text `Stage 1`, `Stage 2`, and `Stage 3`. Treat those numbers as **art/ordinal labels**, not replacements for stage IDs or `StageDefinition.displayName`. Do not silently rename stages, change stage order, overwrite stage unlock progress, or use the baked-in artwork text for stage loading. If the exact stage identity matters to players, retain the actual stage display name in a separate, unobtrusive metadata area outside the baked-in board text (for example under the center board). Avoid rendering duplicate large labels over the baked-in `Stage N` text. Report this discrepancy explicitly after implementation.

If the carousel supports more than three stages, do not display a wrong `Stage 1/2/3` label for subsequent stages. Keep a safe existing-card fallback (or equivalent generic appearance if already available) until matching artwork exists. Do not create fake stage artwork or invent names.

## 4. Carousel Focus Logic

Drive the sprite selection from the existing carousel selection index; never hard-code 'left = Stage 1' or 'center = Stage 2'. The stage at the center changes as the carousel moves, and **its sprite changes with it**.

Conceptual behavior:

```csharp
// Pseudocode; adapt to existing project architecture.
Sprite GetStageBoardSprite(int stageArtIndex, bool isCentered)
{
    return isCentered
        ? boardSprites[stageArtIndex].selectedCenter
        : boardSprites[stageArtIndex].unselectedSide;
}

void RefreshCarouselStageVisuals()
{
    foreach (var card in visibleStageCards)
    {
        bool isCentered = card.StageIndex == selectedStageIndex;
        card.Image.sprite = GetStageBoardSprite(card.StageArtIndex, isCentered);
        // Preserve existing center/side positions, size, and interaction.
    }
}
```

Expected behavior:

- At any moment, the center stage uses the bright first-column art for its row.
- Both neighboring stages use the muted second-column art for their respective rows.
- When the user presses A/D, arrow keys, or clicks a side stage (whichever controls are supported now), update the selected index and **refresh all relevant sprites immediately**.
- If the current carousel animates transitions, switch the art coherently with the existing animation, without introducing flicker or an intermediate incorrect state.
- Only the centered stage is visually highlighted; do not accidentally highlight a side stage because of pointer hover.
- Clicking the already-centered stage must preserve existing behavior; do not introduce unintended stage entry or deselection.

## 5. Lock Status Is Independent of Visual Focus

**Do not use the gray second-column board as the meaning of 'Locked'.** The muted board simply means the stage is on the left or right side of the carousel.

- A **locked** stage that is currently centered must still use its **bright center board**, together with the existing `Locked` status indicator and blocked play action.
- An **unlocked** stage on the side must still use its **muted side board**.
- Preserve the existing `Selected Stage` indicator, locked/unlocked status text, and any other meaningful stage metadata. Move or restyle metadata only as needed for readability.
- Do not change progression, `alwaysUnlocked` settings, stage loading, or the underlying `StageDefinition` objects.

## 6. Layout and Rendering

- Replace only the current three blue/gray rectangular stage card backgrounds with wooden boards.
- Match the original artwork aspect ratio; avoid non-uniform stretching or blurred pixel art.
- Preserve the existing center-card emphasis (larger card, if implemented) and side-card positioning in the current Lobby screenshot.
- Keep sufficient separation from the title/currency display above and Enter Stage button below.
- Use UI `Image` components and an appropriate Canvas sorting order; the artwork must not block the existing buttons or absorb clicks beyond its intended stage-card hit area.
- If existing plain card titles would render over the large baked-in `Stage N` words, remove/hide those **duplicate visual labels only**. Keep underlying data and accessibility/status labels intact.
- Keep gameplay and menu resolution/scaling behavior stable.

## 7. Preserve Existing Functionality

Do not change:

- A/D, keyboard navigation, click-to-select, or other currently implemented carousel controls
- Selection index and stage carousel ordering
- Enter-to-play behavior, Enter Stage button callback, and stage scene routing
- Locked stage restrictions and unlock/progression state
- Stage definitions, rewards, save files, Gold/Candy balances
- Deck management, cards, current Lobby HUD, and any gameplay behavior

## 8. Verification and Acceptance Criteria

Test or inspect the following:

1. All six sprites are sliced correctly and have transparent backgrounds without leftover sheet content.
2. Center stage always uses the corresponding row's **column 1** image.
3. Left and right stages use their respective rows' **column 2** image.
4. Advancing the carousel left and right swaps state artwork correctly, including cycling back.
5. A locked center stage is still bright but remains locked and cannot be entered.
6. An unlocked side stage is muted and becomes bright upon selection.
7. The real stage-to-scene mapping and stage IDs stay unchanged; the baked-in `Stage N` number is not misused as a scene or save ID.
8. The selected-stage indicator, locked state, and Enter Stage button continue to function.
9. The current 2D Lobby scene, background, other buttons, Deck preview, and currency display remain unchanged.
10. Report a fallback behavior for stage entries beyond the three supplied designs.
11. Confirm layout, aspect ratio, and pixel-art sharpness in Unity Play Mode if possible. Do not claim manual verification if it was not performed.

## 9. Final Implementation Report

Provide:

- Exact files, prefabs, UI components, and imported sprites modified/created
- Mapping of existing stage entries to `Stage 1/2/3` board rows
- The mechanism for switching center versus side sprites
- How lock state is kept separate from carousel selection state
- How the baked-in art labels relate to existing stage names (`1-2`, `1-3`, `Test Stage`)
- Automated tests and manual tests actually performed, plus anything unverified

**Implement now. Do not stop after only describing the approach.**
