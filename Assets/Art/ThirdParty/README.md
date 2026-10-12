# Ready-to-use 3D asset packs

Open a pack's Prefabs folder in Unity and drag a prefab into the Scene or Hierarchy.

| Pack | Prefabs | Example |
|---|---:|---|
| TinyTreatsKitchen | 118 | table_A, fridge, kettle |
| KayKitRestaurant | 144 | food_burger, cuttingboard, chair_A |
| KenneyFurniture | 140 | table, chair, kitchenFridge |

Models contains the original Unity-compatible FBX files. Tiny Treats and KayKit also include their atlas PNG beside the models. Kenney uses imported material colors, so no missing texture needs to be filled in. Prefabs are simple roots containing a nested model named Visual; materials retain their imported source references. These assets target the current Built-in rendering pipeline and use Standard materials.

Keep Models and their .meta files together with Prefabs. Do not move or rename files outside Unity after they have references. Original ZIPs remain under Free-3D-Assets. Each pack includes its original CC0 License.txt.

No combat scripts, automatic collision, placement behavior or enemy behavior were added. Use these as scenery, or place a chosen visual under an existing gameplay prefab while preserving its scripts, placement points and colliders. Existing gameplay scenes have not been replaced. Adjust instance scale to suit the battlefield.

Known source warnings: Unity discards self-intersecting polygons in six Tiny Treats wall models (wall_plain_kitchen and wall_tiles_kitchen, each straight/corner_inner/corner_outer). Inspect these particular wall pieces visually before using them. An importer also reported a duplicate Mesh name Group; all generated prefab material and renderer references validated, but this source warning should be considered when reimporting models. Original source geometry is preserved.
