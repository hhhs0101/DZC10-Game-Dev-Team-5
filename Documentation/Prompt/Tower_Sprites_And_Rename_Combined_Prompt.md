# Combined Codex Implementation Prompt — Tower Sprite Replacement and Ordered Renaming

## Objective

Update the **existing 2D top-down Unity tower-defense project** to use the supplied condiment and seasoning pixel-art sprites as its Tower visuals. **At the same time**, rename the existing numbered Towers (Tower 1, Tower 2, etc.) to the corresponding condiment names in strict numeric order.

This is a **visual asset and player-facing naming migration**, not a redesign of the Tower gameplay system.

Use the current working project, not a previous commit. Before editing anything, inspect the project's existing Tower definitions, Tower Card definitions, prefabs, art references, Deck Editor, Hand UI, runtime Tower instantiation, and persistence code.

### Source Asset Directory

The source PNG files are on the local machine here:

```text
/Users/hyunseungjeon/Downloads/Asset/Game Size
```

Locate and inspect the source images there. **Copy/import the required sprites into an appropriate folder inside the Unity project's `Assets/` directory.** Do not load assets from the Downloads directory at runtime, and do not leave runtime references pointing to absolute user-specific filesystem paths.

## 1. Exact Ordered Tower Mapping

Use the following mapping. The order is authoritative and must not be rearranged based on alphabetical sorting or guesses about visual style.

| Existing Tower | New player-facing name | Source PNG filename |
|---|---|---|
| Tower 1 | Vinegar | `Pixel Art Vinegar Bottle128nb.png` |
| Tower 2 | Olive Oil | `Pixel-Art Golden Oil Bottle128nb.png` |
| Tower 3 | Hot Sauce | `Pixel-Art Hot Sauce Bottle128nb.png` |
| Tower 4 | Black Pepper | `Pixel-Art Pepper Shaker128nb.png` |
| Tower 5 | Paprika Powder | `Pixel-Art Red Spice Jar Icon128nb.png` |
| Tower 6 | Salt | `Pixel-Art Salt Shaker Sprite128nb.png` |
| Tower 7 | Brown Sauce | `Pixel-Art Sauce Bottle Icon128nb.png` |
| Tower 8 | Soy Sauce | `Pixel-Art Soy Sauce Bottle(1).png` |
| Tower 9 | Mustard | `Pixel-Art Yellow Mustard Bottle (1)128nb.png` |
| Tower 10 | Mayonnaise | `Retro Pixel Art Mayonnaise Bottle128nb.png` |
| Tower 11 | Ketchup | `Retro Pixel-Art Ketchup Bottle128nb.png` |

**Important:** The filename for Tower 2 says “Golden Oil,” but its requested Tower display name is **Olive Oil**. Similarly, use **Black Pepper**, **Paprika Powder**, **Brown Sauce**, and **Mustard** as the requested display names even if the source filenames differ.

Before applying the mapping, enumerate the actual existing numbered Tower definitions. If the project has fewer than 11 playable Towers, map every existing numbered Tower in order, and import or catalogue any remaining images for future assignment. **Do not invent new combat behaviors, add playable Tower cards, or alter deck content merely to use every image.** If all 11 numbered Towers already exist, complete all 11 mappings. If the existing numbering is ambiguous, inspect how Tower definitions and cards correspond; do not silently assign sprites to the wrong Tower.

If any named file is missing, check for a clearly equivalent filename in the specified directory and document it; do not silently substitute a different asset.

## 2. Rename Existing Tower Names Without Breaking Identity

Update the **player-facing names** of mapped Towers from numbered placeholders to the exact names in the table.

Where they are driven by Tower/card data, the new names should appear consistently in:

- Deck Editor / Available Cards
- Current Deck slots and Deck presets
- Gameplay Hand and Card details
- Tower selection/inspection information, if implemented
- TowerDefinition/CardDefinition display fields

Keep **persistent internal Card IDs / Tower IDs stable** when they are used to resolve saved Deck presets or existing references. Do not rename IDs merely for aesthetics. A rename must not cause saved Decks to fail to load, replace mapped cards with default cards, or change their order.

For ScriptableObject assets and prefabs, use the smallest safe set of asset renames. Do not break serialized Unity references, GUIDs, prefab links, or persistence lookup. If asset renaming risks such breakage, leave the internal asset filename intact and change the displayed name instead.

## 3. Import and Configure the Pixel-Art Sprites

Import each applicable file as a 2D Sprite using settings compatible with the project:

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`, unless the source asset genuinely requires another mode
- Preserve source transparency/alpha
- `Filter Mode: Point (no filter)` for crisp pixel-art edges
- Avoid compression or resizing that visibly blurs the sprites
- Choose consistent Pixels Per Unit and size based on the existing top-down world and slot scale
- Preserve aspect ratio

Do not upscale or downscale the source artwork by destructive resampling merely to fit Tower sprites. Prefer Unity import settings, per-Tower visual scales, and prefab visual offsets.

## 4. Apply Sprites Through the Existing Tower Architecture

Reuse the project's existing Tower runtime class hierarchy, `TowerDefinition`/`TowerCardDefinition` model, and Tower prefabs. Prefer assigning a Sprite reference at the current data or prefab visual layer rather than branching gameplay code by Tower number or display name.

A conceptual structure is:

```text
TowerDefinition / TowerCardDefinition
    -> Existing gameplay identity and stats
    -> DisplayName (updated)
    -> Sprite reference (updated)

Tower Runtime Object
    -> Existing combat/targeting components
    -> Visual child / SpriteRenderer (new bottle artwork)
```

Ensure the correct sprite is visible not only in the Deck Editor, but also on the **actual instantiated Tower in the battle scene**. If the card preview uses a separate icon reference, configure it consistently without replacing card gameplay identity.

Do not add a separate 3D renderer, 3D physics requirement, or unrelated combat class for each bottle image.

## 5. Align Bottles with the Wooden Plate Placement Slots

The current 2D battlefield uses wooden-plate sprites as Tower placement-slot visuals. Each bottle should appear visually placed **on top of its wooden plate**.

- Use a suitable bottom-center visual anchor/pivot or a dedicated local visual offset.
- The visible base of each bottle should align with the center/appropriate surface point of the wooden plate.
- Account for transparent padding inside source textures; do not assume the bottom edge of the PNG equals the visible bottle base.
- Keep bottle sizes visually consistent while preserving distinctive silhouette proportions.
- Towers must render above the plate slots using the existing `Sorting Layer`/`Order in Layer` system.
- Preserve world-space HP/range/selection visuals if they currently exist.

**Do not change the logical Tower placement point, slot positions, attack origin, targeting range, or Collider2D dimensions just to compensate for sprite padding.** Prefer adjusting presentation only. Preserve current slot clickability and occupied-state behavior.

## 6. Preserve All Existing Gameplay and Persistence

Do not intentionally change:

- Tower damage, attack speed, cooldown, range, or targeting behavior
- Tower inheritance / runtime combat architecture
- Tower selection and placement validation
- Wooden-plate slot availability/occupancy logic
- Elixir and Card-use costs
- Six-card Deck rules, uniqueness, minimum-one-Tower rule
- Three Deck presets, currently selected preset, or stored Deck composition
- Hand rotation / Upcoming Queue / shuffle behavior
- Skill cards (including Toy Throw and Slow Area)
- Enemy path-following and Stage spawn logic
- Stage Clear, Gold rewards, Candy drops, Pause, Game Over, or Retry
- Stored currencies, stage progression, and other persistent save data

In particular, never migrate saved presets by replacing stable card IDs with newly generated IDs based on their renamed display text.

## 7. Implementation Sequence

1. Inspect current Tower numbering, definitions, sprites, prefabs, Card display names, and save-ID resolution.
2. Locate the exact PNG files in the source asset directory.
3. Import/copy the PNGs into the Unity project as reusable assets, keeping Unity asset references valid.
4. Apply the exact ordered mapping to existing Tower definitions.
5. Rename **player-facing names** to the corresponding condiment names while retaining stable identities.
6. Update Tower visuals in both the Deck/Card UI and instantiated gameplay Towers.
7. Adjust presentation-only scale, pivot/offset, and sorting relative to wooden-plate placement slots.
8. Verify Tower placement, attacks, Deck/Hand behavior, preset switching and save compatibility.
9. Report any remaining unused sprites separately.

Do not duplicate Tower definitions or introduce parallel Tower systems.

## 8. Verification and Acceptance Criteria

Verify the following to the extent allowed by available Unity Editor/automation access:

1. Each existing numbered Tower maps to the exact requested name and sprite in numeric order.
2. The renamed labels are visible in Deck Editor, Deck preview, and gameplay Hand wherever names appear.
3. The correct bottle art is shown by the **placed Tower**, not only by its card icon.
4. Sprite transparency remains intact and pixel edges stay crisp.
5. Tower images are correctly centered/aligned above wooden-plate slots, without changing the gameplay placement point.
6. Tower / wooden-plate SpriteRenderer sorting is correct.
7. Selecting and placing existing Towers works.
8. Occupied slots cannot be reused incorrectly.
9. Existing attack, target selection, damage and cooldown mechanics work without behavior changes.
10. Elixir is spent only under the existing successful-use rules, and Card cycling remains correct.
11. All three Deck presets and persistent card IDs remain valid after renaming.
12. Existing save data can still resolve and display the renamed cards after an application restart, if that test can be performed.
13. Any Tower visuals not yet used in playable cards are reported, not silently discarded.
14. No unrelated scenes or gameplay systems are modified unnecessarily.

If manual UI interaction, visual comparison, or an actual restart cannot be performed, explicitly mark those as **not verified**. Do not claim tests were run unless they were actually executed.

## 9. Final Implementation Report

After making the changes, provide:

1. Existing Tower definitions discovered and their count.
2. Final table of `Existing Tower -> New Display Name -> PNG -> Unity Asset/Prefab`.
3. Source files missing or substituted, if any.
4. Files, asset references, prefabs, and data definitions changed.
5. Explanation of how display names changed while stable Card/Tower IDs were preserved.
6. Sprite import, pivot, scale, offset, and sorting decisions.
7. Whether all 11 images are actively assigned, or which are reserved for future Towers.
8. Automated tests actually run and results.
9. Manual Editor, placement, gameplay and restart checks actually performed.
10. Remaining issues or unverified items.

**Do not stop after importing the PNG files; complete the visual mapping and name update for the existing playable Towers.**
