using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
// Explicit one-time authoring operation. Never runs on import, scene load or Play Mode.
public static class CondimentTowerSetup {
    public const string ArtRoot = "Assets/Defense/Art/Condiments";
    public const string SharedPrefab = "Assets/Defense/Prefabs/TopDown2D/BasicTurret2D.prefab";
    public const float PixelsPerUnit = 128;
    public const float VisibleHeight = 1.35f;
    public static readonly string[] Names = {"Vinegar","Olive Oil","Hot Sauce","Black Pepper","Paprika Powder","Salt","Brown Sauce","Soy Sauce","Mustard","Mayonnaise","Ketchup"};
    public static readonly string[] Files = {
        "Pixel Art Vinegar Bottle128nb.png", "Pixel-Art Golden Oil Bottle128nb.png",
        "Pixel-Art Hot Sauce Bottle128nb.png", "Pixel-Art Pepper Shaker128nb.png",
        "Pixel-Art Red Spice Jar Icon128nb.png", "Pixel-Art Salt Shaker Sprite128nb.png",
        "Pixel-Art Sauce Bottle Icon128nb.png", "Pixel-Art Soy Sauce Bottle(1).png",
        "Pixel-Art Yellow Mustard Bottle (1)128nb.png", "Retro Pixel Art Mayonnaise Bottle128nb.png",
        "Retro Pixel-Art Ketchup Bottle128nb.png"
    };
    public static string ArtPath(int index) => ArtRoot+"/"+Names[index].Replace(" ","")+".png";
    public static void Apply(string sourceFolder) {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring Tower content.");
        var catalog = Resources.Load<GameCatalog>("GameCatalog");
        var sources = Files.Select(f=>Path.Combine(sourceFolder,f)).ToArray();
        // The supplied directory contains exactly this clearly equivalent Soy Sauce filename.
        if (!File.Exists(sources[7])) sources[7] = Path.Combine(sourceFolder,"Pixel-Art Soy Sauce Bottle.png");
        for (int i=0;i<Names.Length;i++) {
            if (!File.Exists(sources[i])) throw new FileNotFoundException("Missing required Tower art: "+Files[i]);
            var tower = AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/TestTurret"+(i+1)+".asset");
            var card = AssetDatabase.LoadAssetAtPath<TowerCardDefinition>("Assets/Defense/Data/TowerCard"+(i+1)+".asset");
            if (tower == null || card == null || card.Tower != tower || card.CardId != "tower.test."+(i+1) ||
                !catalog.AvailableTowers.Contains(tower) || !catalog.AvailableCards.Contains(card))
                throw new InvalidOperationException("Ambiguous numbered Tower/Card mapping at "+(i+1));
        }
        Directory.CreateDirectory(ArtRoot);
        for (int i=0;i<Names.Length;i++) {
            var path = ArtPath(i);
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(sources[i]))) File.Copy(sources[i],path,true);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 256;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings); importer.SaveAndReimport();
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            float scale; Vector2 offset;
            try {
                if (!texture.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid PNG: "+path);
                var pixels = texture.GetPixels32(); int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
                for (int y=0;y<texture.height;y++) for (int x=0;x<texture.width;x++) {
                    // Ignore nearly invisible export halos for alignment; preserve all original PNG bytes/alpha.
                    if (pixels[y*texture.width+x].a < 16) continue;
                    minX=Math.Min(minX,x); maxX=Math.Max(maxX,x); minY=Math.Min(minY,y); maxY=Math.Max(maxY,y);
                }
                if (maxY<0) throw new InvalidOperationException("Empty Tower artwork: "+path);
                scale = VisibleHeight*PixelsPerUnit/(maxY-minY+1);
                offset = new Vector2((texture.width*.5f-(minX+maxX+1)*.5f)/PixelsPerUnit,
                    (texture.height*.5f-minY)/PixelsPerUnit)*scale;
            } finally { UnityEngine.Object.DestroyImmediate(texture); }
            var tower = AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/TestTurret"+(i+1)+".asset");
            var data = new SerializedObject(tower);
            data.FindProperty("displayName").stringValue=Names[i];
            data.FindProperty("sprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            data.FindProperty("spriteScale").floatValue=scale; data.FindProperty("spriteOffset").vector2Value=offset;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(tower);
            var card = new SerializedObject(AssetDatabase.LoadAssetAtPath<TowerCardDefinition>("Assets/Defense/Data/TowerCard"+(i+1)+".asset"));
            card.FindProperty("displayName").stringValue=Names[i]; card.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(card.targetObject);
        }
        var root=PrefabUtility.LoadPrefabContents(SharedPrefab);
        try {
            var view=root.GetComponent<TurretSpriteView>() ?? root.AddComponent<TurretSpriteView>();
            var data=new SerializedObject(view); data.FindProperty("appearance").objectReferenceValue=root.transform.Find("Visual").GetComponent<SpriteRenderer>();
            data.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root,SharedPrefab);
        } finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("CONDIMENT_TOWERS_READY: 1-11 mapped in numeric order; 12-15 and all stable IDs/stats retained. Soy Sauce source: "+Path.GetFileName(sources[7]));
    }
}
}
