using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Defense.Editor {
[InitializeOnLoad]
public static class GarlicArtworkRepair {
    private const string DefinitionPath = "Assets/Defense/Data/Enemies/Veggies/GarlicEnemy.asset";
    private static bool queued, repairing;
    static GarlicArtworkRepair() { Queue(); }
    public static void Queue() {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += () => { queued = false; Repair(); };
    }
    private static bool IsGarlic(string name) {
        return !string.IsNullOrEmpty(name) && name.IndexOf("garlic",StringComparison.OrdinalIgnoreCase) >= 0;
    }
    private static int Priority(string path) {
        if (path.StartsWith("Assets/Art/Vegetables/",StringComparison.OrdinalIgnoreCase)) return 3;
        if (path.StartsWith(VeggieEnemySetup.ArtRoot+"/",StringComparison.OrdinalIgnoreCase)) return 2;
        return 1;
    }
    [MenuItem("Defense/Enemies/Reconnect Garlic Image")]
    public static void Repair() {
        if (repairing || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
        if (definition == null) return;
        // Preserve a valid sprite, including a user's explicit selection.
        if (definition.Sprite != null) return;
        repairing = true;
        try {
            var matches = new List<Sprite>();
            int best = 0;
            // Inspect subassets too: a garlic sprite can live inside a vegetable sheet.
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D",new[] {"Assets"})) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool garlicFile = IsGarlic(Path.GetFileNameWithoutExtension(path));
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (garlicFile && importer != null && importer.textureType != TextureImporterType.Sprite) {
                    // Match the existing vegetable importer. Never reslice sprite sheets.
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.spritePixelsPerUnit = 32;
                    importer.SaveAndReimport();
                }
                var sprites = new List<Sprite>();
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite) sprites.Add(sprite);
                foreach (var sprite in sprites) {
                    if (!IsGarlic(sprite.name) && !(garlicFile && sprites.Count == 1)) continue;
                    int priority = Priority(path);
                    if (priority < best) continue;
                    if (priority > best) { matches.Clear(); best = priority; }
                    if (!matches.Contains(sprite)) matches.Add(sprite);
                }
            }
            if (matches.Count == 1) Bind(definition,matches[0]);
            else Debug.LogWarning(matches.Count == 0
                ? "Garlic sprite not found. Select its sprite in the Project window, then choose Defense > Enemies > Use Selected Sprite for Garlic."
                : "Several garlic sprites were found. Select the intended sprite, then choose Defense > Enemies > Use Selected Sprite for Garlic.");
        } finally { repairing = false; }
    }
    [MenuItem("Defense/Enemies/Use Selected Sprite for Garlic")]
    public static void UseSelectedSprite() {
        var sprite = Selection.activeObject as Sprite;
        if (sprite == null) sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(Selection.activeObject));
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
        if (sprite == null || definition == null) {
            Debug.LogWarning("Select the garlic Sprite in the Project window first. For a sheet, expand it and select the garlic sprite.");
            return;
        }
        Bind(definition,sprite);
    }
    private static void Bind(EnemyDefinition definition, Sprite sprite) {
        Undo.RecordObject(definition,"Reconnect garlic artwork");
        var data = new SerializedObject(definition);
        data.FindProperty("sprite").objectReferenceValue = sprite;
        data.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssetIfDirty(definition);
        Debug.Log("Garlic sprite assigned: " + AssetDatabase.GetAssetPath(sprite) + " / " + sprite.name,definition);
    }
}
public sealed class GarlicArtworkImportWatcher : AssetPostprocessor {
    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom) {
        foreach (string path in imported) {
            if (path.EndsWith("GarlicEnemy.asset",StringComparison.OrdinalIgnoreCase) || AssetImporter.GetAtPath(path) is TextureImporter) {
                GarlicArtworkRepair.Queue(); return;
            }
        }
        if (moved.Length > 0) GarlicArtworkRepair.Queue();
    }
}
}
