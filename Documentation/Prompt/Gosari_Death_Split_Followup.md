# Gosari boss split generations

Create child and grandchild Gosari enemies. Each generation must have lower health and higher movement speed. The agreed stage-two boss structure is one parent splitting into two children on combat death; each child splits into two grandchildren, which do not split further. Base arrival does not trigger splitting. Preserve shared enemy lifecycle, other enemies and existing game systems.

Prototype tuning chosen for this implementation: HP 3500 / 1500 / 600; authored movement speed 0.32 / 0.48 / 0.64, before the existing global 0.85 multiplier. Existing parent identity and statistics remain; child Base damage 260 and grandchild 130 conserve the parent's 520 total potential Base damage.
