# Codex Main Prompt — Create Ten Veggie Enemy Variants

Inspect the current Unity project before editing code, then read and implement:

- `Veggie_Enemy_Implementation_Spec.md`

The current target is a **native 2D top-down tabletop battlefield**, not the earlier 3D XZ-plane battlefield. Preserve all existing gameplay systems and persistent player data.

## Goal

Add ten independently configurable vegetable-themed enemy variants using the existing Enemy architecture and the supplied individual sprites:

1. `Mushroom.png`
2. `Olive.png`
3. `GreenCabbage.png`
4. `Zucchini.png`
5. `Tomato.png`
6. `Kohlrabi.png`
7. `Turnip.png`
8. `Leek.png`
9. `RedCabbage.png`
10. `Potato.png`

**Use the individual PNGs in preference to the composite sheet** when they are available. Verify that every displayed sprite matches its file/variant name. Do not guess sprite-sheet coordinates if files are missing.

## Inspect First

Locate and inspect:

- current Enemy runtime/base class and Enemy definitions
- existing Enemy prefab(s), visual hierarchy, and SpriteRenderer setup
- Enemy health, speed, death, status modifiers and targeting integration
- current 2D path follower and stage spawner
- Candy drop configuration and death event processing
- the existing five-enemy repeatable Test Stage
- relevant Sprite assets and Unity import settings

Do not assume exact class names, folder names, or Sprite asset paths. Adapt to what the repository actually contains.

## Implement

- Configure ten unique Enemy data definitions/variants with correct Sprite assignments.
- Reuse existing Enemy movement, combat, death, Slow, Candy, and Stage Clear logic.
- Prefer a common runtime prefab with a replaceable SpriteRenderer visual child; avoid ten separate subclasses for visual-only differences.
- Make statistics and Candy drop parameters editable per variant, but do not invent unrequested unique combat abilities or arbitrary balance changes.
- Import pixel art with crisp Point filtering, correct transparency, and consistent world scale.
- Make all ten variants usable in the existing configurable spawner.
- Preserve the original five-enemy Test Stage; use a separate development testing mechanism if necessary to inspect all ten.

## Do Not Change

- Card / Deck / Hand behavior or persistent presets
- Tower placement or targeting rules
- Skill Card interaction or Skill effect semantics
- Elixir, Gold, Candy persistence rules
- Stage Victory, Replay, or Game Over conditions
- the original 3D scene solely for this task

## Verify

Check correct Sprite-to-name mapping, ten variant definitions, spawning, 2D waypoint movement, damage, Toy Throw, Slow Area, death tracking, Candy drop integration, and regression of the five-enemy Test Stage. Run existing relevant tests and add focused tests only when useful.

If the individual PNGs are not in the project, identify the missing files and provide the expected import location rather than creating false or unrelated asset assignments.

## Completion Report

Explain the existing architecture reused, files created/modified, all ten Sprite and definition associations, spawner integration, tests actually executed, and any unverified manual behavior. Do not claim Unity Play Mode testing without having performed it.
