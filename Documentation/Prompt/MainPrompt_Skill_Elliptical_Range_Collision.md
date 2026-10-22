# Codex Main Prompt — Elliptical Skill Range + Actual Enemy Hit Detection

Inspect the current Unity project and implement the attached `Skill_Elliptical_Range_Collision_Patch.md`.

This is a **gameplay collision change**, not a visual-only replacement. The battlefield is viewed at an angle; the supplied `Glowing White Elliptical Ground Marker(1).png` must remain elliptical rather than being stretched into a circle.

## Required outcome

1. Use each skill's existing effective `Radius` as the horizontal semi-axis of the ellipse.
2. Preserve the marker's visible bright-rim aspect ratio, scaling both axes proportionally when Radius changes. The supplied PNG is 1774x887, but its bright outer rim is approximately 1518x400 pixels (aspect ratio height/width ~0.2635). Calibrate actual visual bounds/pivot in Unity rather than scaling by the entire texture rectangle.
3. Use the ellipse **enclosed by the visible outer rim** as the actual area-of-effect hit region. The transparent interior counts as inside; the outer glow does not enlarge the hitbox.
4. Use one common ellipse geometry/containment implementation to drive both visual range and actual enemy effects, including Toy Throw damage and Persistent Slow Area entry/exit behavior. Do not leave an old circular hitbox behind.
5. Keep existing Card selection, Toggle cancel, aiming/casting interaction, Elixir, Hand cycle, Tower systems, stage progression, save data and source-aware slowing behavior intact.
6. Reuse the current SkillDefinition, aiming indicator, enemy detection and skill runtime paths; avoid rebuilding working components. Implement a targeted patch rather than a new system.
7. Perform available tests for preview scaling, inside/outside/boundary detection and slow removal. Report all modifications and which visual/play-mode tests were actually completed.

IMPORTANT: Any prior prompt that demanded a circular displayed rim or preserved circular area damage is superseded by this patch. The user expressly wants elliptical gameplay coverage matching the elliptical image.
