# Unity Patch — Elliptical Skill Range and Collision

## Objective

Use the provided `Glowing White Elliptical Ground Marker(1).png` as **both** the visible skill-area boundary and the basis of the **actual gameplay hit area**. The game uses a tilted/pseudo-top-down kitchen presentation; do **not** force the elliptical marker into a circle.

This patch **supersedes the circular-boundary requirements** in `Skill_Range_Glow_Marker_Replacement_Patch.md`. The user has deliberately requested an **elliptical gameplay area**, not merely an elliptical visual approximation of circular damage.

Source image available in the conversation. Find/import the actual image into the Unity project. Do not refer to the assistant sandbox file path from runtime code.

## Intended behavior

```text
Select skill card
 -> begin the existing aim interaction
 -> render the glow ellipse at the current target location
 -> scale the ellipse from the selected skill's effective Radius
 -> commit the cast under existing validity / Elixir / card rules
 -> affect enemies whose gameplay positions lie INSIDE the visible oval
 -> preserve current visual/effect durations and cleanup rules
```

For Toy Throw, apply its configured damage to valid enemies inside the ellipse at the established impact moment. For Persistent Slow Area, slow only enemies currently inside the same ellipse; remove that source's slow immediately when they leave, the source expires, or the enemy is destroyed.

## 1. Meaning of Radius and aspect ratio

- Interpret `SkillDefinition.Radius` (or current resolved effective radius) as the **horizontal semi-axis** of the visible range: `rx = Radius`.
- Preserve the provided **visible bright rim** aspect ratio while resizing: `ry = rx * ellipseAspectRatio`.
- Default `ellipseAspectRatio` should be calibrated from the supplied sprite's visible bright outer rim. In the supplied 1774 x 887 image, alpha >= 128 yields a rim bounding box approximately `x=128..1645`, `y=311..710`, i.e. **1518 x 400**, so `ellipseAspectRatio` is approximately `400 / 1518 = 0.2635`.
- Store this ratio in an easily adjustable serialized setting or marker profile so it can be fine-tuned to match the Unity camera composition. Do **not** confuse full PNG dimensions (1774 x 887) with the visible rim dimensions.
- A skill of Radius 3, for example, produces `rx=3` and `ry≈0.7905` world units using the default ratio. This is a **gameplay change**, not only a rendering change.
- Treat the interior enclosed by the bright outer rim as the area of effect; the transparent center still counts as inside. The faint outside glow is decorative and must **not** extend the hitbox.
- Use one effective radius / aspect ratio / center calculation for both the preview and effect queries. Never tune a visual scale and collider radius independently.

## 2. Shared ellipse geometry

Add a focused reusable helper/component only if there is no suitable existing equivalent. Its responsibility is ellipse geometry, not skill/card state. Conceptually:

```csharp
public readonly struct EllipseFootprint
{
    public readonly Vector2 Center;
    public readonly float RadiusX;
    public readonly float RadiusY;

    public bool Contains(Vector2 point)
    {
        if (RadiusX <= 0f || RadiusY <= 0f) return false;
        float dx = (point.x - Center.x) / RadiusX;
        float dy = (point.y - Center.y) / RadiusY;
        return dx * dx + dy * dy <= 1f;
    }
}
```

This code demonstrates an **axis-aligned ellipse** in the current 2D gameplay plane. If the active visual has rotation or inherited nonuniform transform scale, transform the candidate point into the marker's local space (or otherwise account for those transforms) so the rendered rim and hit test remain aligned.

Do **not** test only the PNG's nontransparent pixels: that would detect the white ring but incorrectly exclude its center. Do not use `SpriteRenderer.bounds.Contains` or a rectangular `BoxCollider2D` as the final damage-area test.

## 3. Drawing the same footprint

- Reuse the project's current skill-aiming indicator component/prefab and replace its artwork with the provided PNG where necessary; do not introduce duplicate competing previews.
- Keep the artwork's **elliptical proportions**. Use uniform scaling for the visual if its parent transform is not distorted.
- Calibrate the measured **bright rim width**, not the full texture width, to match the desired diameter `2 * rx`. Scale its visible rim height to `2 * ry` from the same uniform factor.
- Align the rim's center with the gameplay target. The rim center in image pixel coordinates is approximately `(886.5, 510.5)` from the top-left; the image canvas center is different. Compensate via sprite pivot or **visual-child offset**, never by offsetting the actual gameplay impact point.
- Derive the preview footprint from the selected skill's **effective** range; derive the runtime footprint from the same geometry at the committed impact position. If the effect is delayed, use the impact center required by the existing skill behavior.
- Preserve transparency, sorting, mouse tracking, UI blocking, and aiming/cancellation rules. Avoid black opaque backgrounds.

## 4. Apply ellipse containment to skills

### Toy Throw / instant area damage

1. At the existing impact time, collect candidate enemies efficiently using a broad-phase query (for example a circle with `max(rx, ry)` or an enclosing bounding region), or the project's existing enemy collection.
2. Filter every candidate using the shared ellipse `Contains` predicate and the enemy's gameplay target position (use the current project's established target position conventions consistently).
3. Damage valid enemies inside once per impact, without double hits from multiple colliders.
4. Enemies outside the ellipse must take no damage, even if they lie inside the old circular radius.

### Persistent Slow Area

1. At each required update, determine which valid enemies are inside the **same elliptical footprint** as its visual effect.
2. Apply the area's existing source-aware slowdown only while those enemies are within the ellipse.
3. Remove this area's modifier on exit, expiry, destroy, stage transition, or target destruction.
4. Preserve the existing overlap policy: identical slows do not multiply or stack unexpectedly, and unrelated speed modifiers must remain intact.
5. Preserve the existing skill duration and visual fade behavior; the fade must not extend gameplay slowdown.

Other area skills using the same aiming/runtime-range abstraction should use the same ellipse definition where they are intended to share this marker. Do not silently change tower attack range, installed-tower inspection, or unrelated circles.

## 5. Existing systems that must remain unchanged

- Skill press/hold/release workflow, target validation and UI cancellation.
- Re-click selected card to deselect; change cards to cancel old previews.
- Elixir is spent only on successfully committed casts; canceled/invalid casts consume no Elixir and do not rotate the Hand.
- Tower placement on wooden plates, Tower combat and range inspection.
- Card/Deck/preset persistence; Gold/Candy; finite stage completion, Victory/Retry.
- Existing enemy movement/pathfinding and existing enemy-damage/death semantics except geometric inclusion in the requested skill AoE.
- Existing special-effect durations, slow-source cleanup and skill visual lifecycle.

## 6. Acceptance checks

1. The provided PNG displays as an **ellipse** with its original visual rim proportions, not a forced circle.
2. The displayed rim width changes exactly in proportion to the current selected skill Radius. Doubling Radius doubles both rim axes without changing the aspect ratio.
3. The visible bright rim (rather than transparent PNG margins) defines the hit boundary.
4. An enemy at the center and enemies at points strictly inside the ellipse are affected. An enemy just outside the side or top/bottom of the bright rim is not.
5. In particular, an enemy within the old circular radius but **outside the new ellipse** receives no damage or slow.
6. Toy Throw damage and persistent Slow Area behavior both use this exact footprint.
7. Persistent Slow Area immediately stops affecting enemies that leave its ellipse; expire/overlap behavior works correctly.
8. Card deselection, switching cards, releasing on UI, canceled casts, stage end and Retry clear all transient markers as before.
9. Attack costs, Hand cycle, Tower interaction and save systems are unchanged.
10. Automated tests cover ellipse center, boundary, outside corner, vertically outside, different radius values, and source-aware slow removal. Verify visuals in Unity if runtime/editor access is available.

## 7. Implementation notes and limitations

- A `CircleCollider2D`/`Physics2D.OverlapCircleAll` is **not** the final hit test for an ellipse. It may be used as a *broad phase* followed by exact ellipse containment.
- `PolygonCollider2D` with an approximate elliptical outline is an alternative, but a shared mathematical `Contains` function is preferable for deterministic skills and keeping visuals/logic synchronized. Do not create a pixel-perfect collision system for the glowing texture.
- If scene transforms or camera projection create nonuniform apparent distortion, calibrate the footprint based on the **actual on-screen rendered rim**, and document which world-space positions are used for enemy inclusion. Do not introduce a new coordinate system or change the camera for this patch.
- If the user's exact in-game art bounds differ due to Unity import/trim/pivot, measure once in Unity and adjust the serialized rim calibration. Keep this a data adjustment, not a custom mathematical equation per skill.

## 8. Final implementation report

Report: scripts/prefabs/assets modified, current Skill/Enemy classes reused, how `Radius`, aspect ratio and visible rim are calibrated, how candidate detection and exact containment work, what was verified (separating editor/play mode vs code-only tests), and any mismatch still requiring manual adjustment.
