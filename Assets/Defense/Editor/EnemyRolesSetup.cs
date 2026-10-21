using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
// Explicit balance authoring. Does not edit stages, prefabs, IDs, rewards or saved decks.
public static class EnemyRolesSetup {
    public static readonly string[] Names = {"Carrot","Tomato","Eggplant","Garlic","Broccoli","Potato","Boss"};
    public static string PathFor(string name) => name == "Boss" ? "Assets/Defense/Data/Enemies/BossEnemy.asset" : VeggieEnemySetup.DataRoot+"/"+name+"Enemy.asset";
    private static readonly float[] Health = {100,85,150,30,200,150,2500};
    private static readonly float[] Speed = {2,3,1.7f,2,1.8f,1.8f,1.2f};
    private static readonly int[] Damage = {100,100,100,20,30,20,0};
    private static readonly EnemyRole[] Roles = {EnemyRole.Basic,EnemyRole.Basic,EnemyRole.Basic,EnemyRole.SpeedSupport,EnemyRole.ShieldSupport,EnemyRole.Disruptor,EnemyRole.Boss};
    [MenuItem("Defense/Enemies/Apply Specified Enemy Roles and Stats")]
    public static void ApplyDefaults() => Apply(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads/Helm3ts_32x32_fruit_veggie_asset_pack/VEGGIES/VEGGIES_NL"));
    public static void Apply(string sourceFolder) {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring content.");
        Directory.CreateDirectory(VeggieEnemySetup.DataRoot); AssetDatabase.Refresh();
        var basic = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Defense/Data/BasicEnemy.asset");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VeggieEnemySetup.SharedPrefab).GetComponent<Enemy>();
        for(int i=0;i<Names.Length;i++) {
            string name=Names[i],path=PathFor(name);var definition=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);bool created=definition==null;
            if(created) { definition=UnityEngine.Object.Instantiate(basic);definition.name=name+"Enemy"; }
            var data=new SerializedObject(definition);
            if(created) { data.FindProperty("enemyTypeId").stringValue="enemy_"+name.ToLowerInvariant();data.FindProperty("displayName").stringValue=name;data.FindProperty("prefab").objectReferenceValue=prefab;data.FindProperty("sprite").objectReferenceValue=null; }
            data.FindProperty("health").floatValue=Health[i];data.FindProperty("movementSpeed").floatValue=Speed[i];data.FindProperty("baseDamage").intValue=Damage[i];
            data.FindProperty("baseDamageUnconfigured").boolValue=name=="Boss";data.FindProperty("role").enumValueIndex=(int)Roles[i];
            if(name=="Garlic") data.FindProperty("speedIncrease").floatValue=1.2f;
            if(name=="Broccoli") data.FindProperty("shieldValue").floatValue=100;
            data.ApplyModifiedPropertiesWithoutUndo();
            if(created) AssetDatabase.CreateAsset(definition,path);else EditorUtility.SetDirty(definition);
        }
        ImportArtwork(sourceFolder);AssetDatabase.SaveAssets();
        Debug.Log("ENEMY_ROLES_READY: seven specified definitions; radius/disruption tuning and Boss endpoint damage remain explicitly TBD. Existing stages and Radish are untouched.");
    }
    // Narrow authoring entry point: preserve every other configured enemy field.
    public static void ApplySupportBalance() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var garlic=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(PathFor("Garlic"));
        var broccoli=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(PathFor("Broccoli"));
        if(garlic==null||broccoli==null) throw new InvalidOperationException("Enemy role definitions must exist first.");
        var garlicData=new SerializedObject(garlic);garlicData.FindProperty("speedIncrease").floatValue=1.2f;garlicData.ApplyModifiedPropertiesWithoutUndo();
        var broccoliData=new SerializedObject(broccoli);broccoliData.FindProperty("shieldValue").floatValue=100;broccoliData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(garlic);EditorUtility.SetDirty(broccoli);
        AssetDatabase.SaveAssetIfDirty(garlic);AssetDatabase.SaveAssetIfDirty(broccoli);
        Debug.Log("SUPPORT_BALANCE_UPDATED: Garlic speed multiplier 1.2x, Broccoli shared shield 100.");
    }
    [MenuItem("Defense/Enemies/Reimport Role Artwork from Folder...")]
    private static void ChooseArtwork() {
        var path=EditorUtility.OpenFolderPanel("Select the licensed VEGGIES_NL individual PNG folder","","");
        if(!string.IsNullOrEmpty(path))ImportArtwork(path);
    }
    public static void ImportArtwork(string sourceFolder) {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(VeggieEnemySetup.ArtRoot);
        foreach(var name in new[]{"Carrot","Eggplant","Garlic","Broccoli"}) {
            string source=System.IO.Path.Combine(sourceFolder??"",name+".png"),destination=VeggieEnemySetup.ArtRoot+"/"+name+".png";
            if(File.Exists(source) && (!File.Exists(destination)||!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(destination))))File.Copy(source,destination,true);
            if(!File.Exists(destination)) { Debug.LogWarning(name+" artwork not configured: matching licensed PNG is absent. No unrelated image was substituted.");continue; }
            AssetDatabase.ImportAsset(destination);
            var importer=(TextureImporter)AssetImporter.GetAtPath(destination);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.spritePixelsPerUnit=32;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var definition=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(PathFor(name));
            if(definition!=null) { PrototypeBuilder.Set(definition,"sprite",AssetDatabase.LoadAssetAtPath<Sprite>(destination));EditorUtility.SetDirty(definition); }
        }
        AssetDatabase.SaveAssets();
    }
}
}
