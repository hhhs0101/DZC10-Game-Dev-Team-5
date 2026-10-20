using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
public static class VeggieEnemySetup {
    public const string ArtRoot="Assets/Art/ThirdParty/Helm3tsVeggies";
    public const string DataRoot="Assets/Defense/Data/Enemies/Veggies";
    public const string SharedPrefab="Assets/Defense/Prefabs/TopDown2D/BasicEnemy2D.prefab";
    public static readonly string[] Names={"Mushroom","Olive","GreenCabbage","Zucchini","Tomato","Kohlrabi","Turnip","Leek","RedCabbage","Potato"};
    private static readonly string[] Labels={"Mushroom","Olive","Green Cabbage","Zucchini","Tomato","Kohlrabi","Turnip","Leek","Red Cabbage","Potato"};
    private static readonly string[] Ids={"enemy_mushroom","enemy_olive","enemy_green_cabbage","enemy_zucchini","enemy_tomato","enemy_kohlrabi","enemy_turnip","enemy_leek","enemy_red_cabbage","enemy_potato"};
    [MenuItem("Defense/Enemies/Import Veggies from Downloads")]
    public static void Run()=>Import(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads/Helm3ts_32x32_fruit_veggie_asset_pack/VEGGIES/VEGGIES_NL"));
    [MenuItem("Defense/Enemies/Import Veggies from Folder...")]
    private static void ChooseFolder(){var folder=EditorUtility.OpenFolderPanel("Select the licensed VEGGIES individual PNG folder","","");if(!string.IsNullOrEmpty(folder))Import(folder);}
    public static void Import(string sourceFolder){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before importing enemy content.");
        var missing=Names.Where(n=>!File.Exists(Path.Combine(sourceFolder,n+".png"))).ToArray();
        if(missing.Length!=0)throw new FileNotFoundException("Missing named individual sprites: "+string.Join(", ",missing)+". Expected in "+sourceFolder);
        Directory.CreateDirectory(ArtRoot);Directory.CreateDirectory(DataRoot);
        foreach(var name in Names){var source=Path.Combine(sourceFolder,name+".png");var target=ArtRoot+"/"+name+".png";if(!File.Exists(target)||!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target)))File.Copy(source,target,true);}
        var license=Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(sourceFolder)),"Licence and details.txt");
        if(File.Exists(license))File.Copy(license,ArtRoot+"/Licence and details.txt",true);
        AssetDatabase.Refresh();
        foreach(var name in Names){
            string path=ArtRoot+"/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.spritePixelsPerUnit=32;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        BindSharedView();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefab).GetComponent<Enemy>();
        var basic=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Defense/Data/BasicEnemy.asset");
        for(int i=0;i<Names.Length;i++){
            string path=DataRoot+"/"+Names[i]+"Enemy.asset";var definition=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);bool created=definition==null;
            if(created){definition=UnityEngine.Object.Instantiate(basic);definition.name=Names[i]+"Enemy";}
            var data=new SerializedObject(definition);
            if(created){data.FindProperty("enemyTypeId").stringValue=Ids[i];data.FindProperty("displayName").stringValue=Labels[i];}
            data.FindProperty("sprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot+"/"+Names[i]+".png");
            if(created||definition.Prefab==null)data.FindProperty("prefab").objectReferenceValue=prefab;
            data.ApplyModifiedPropertiesWithoutUndo();
            if(created)AssetDatabase.CreateAsset(definition,path);else EditorUtility.SetDirty(definition);
        }
        AssetDatabase.SaveAssets();Debug.Log("VEGGIE_CONTENT_READY: ten data variants; original gameplay stats copied only on initial creation.");
    }
    private static void BindSharedView(){
        var root=PrefabUtility.LoadPrefabContents(SharedPrefab);
        try {
            var view=root.GetComponent<EnemySpriteView>()??root.AddComponent<EnemySpriteView>();
            var appearance=root.transform.Find("Visual").GetComponent<SpriteRenderer>();
            PrototypeBuilder.Set(view,"appearance",appearance);
            PrefabUtility.SaveAsPrefabAsset(root,SharedPrefab);
        }finally {PrefabUtility.UnloadPrefabContents(root);}
    }
}
}
