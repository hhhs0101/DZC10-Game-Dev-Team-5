using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
public static class LobbyAssetSetup {
    [MenuItem("Defense/Create Missing Lobby Catalog")]
    public static void CreateCatalog() {
        const string path = "Assets/Defense/Resources/GameCatalog.asset";
        if (AssetDatabase.LoadAssetAtPath<GameCatalog>(path) != null) { CardAssetSetup.Run(); return; }
        if (!AssetDatabase.IsValidFolder("Assets/Defense/Resources")) AssetDatabase.CreateFolder("Assets/Defense","Resources");
        var first = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage.asset");
        var stages = new Object[10];
        for (int i=0;i<10;i++) {
            string stagePath = "Assets/Defense/Data/Stage1_"+(i+1)+".asset";
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(stagePath);
            if (stage == null) {
                stage = Object.Instantiate(first); stage.name = "Stage1_"+(i+1);
                var serialized = new SerializedObject(stage); serialized.FindProperty("displayName").stringValue = "1-"+(i+1);
                serialized.FindProperty("startingResources").intValue = 150+i*25;
                serialized.FindProperty("spawnInterval").floatValue = 2-i*.1f;
                serialized.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(stage,stagePath);
            }
            stages[i] = stage;
        }
        var catalog = ScriptableObject.CreateInstance<GameCatalog>(); AssetDatabase.CreateAsset(catalog,path);
        var so = new SerializedObject(catalog);
        SetArray(so,"stages",stages);
        var towers = new Object[15];
        float[,] stats = { {10f, 3f, 1f, 50f}, {30f, 5f, 2.5f, 75f}, {5f, 2.5f, 0.3333333333333333f, 100f}, {8f, 3.5f, 0.5f, 60f}, {45f, 5.5f, 3f, 125f}, {3f, 2f, 0.2f, 70f}, {18f, 4f, 1.2f, 80f}, {60f, 6f, 4f, 150f}, {12f, 2.8f, 0.6f, 90f}, {22f, 4.5f, 1.5f, 95f}, {6f, 3.2f, 0.3f, 110f}, {35f, 3.8f, 2f, 105f}, {15f, 5.2f, 1f, 120f}, {9f, 2.2f, 0.25f, 130f}, {25f, 4.2f, 0.8f, 150f} };
        for (int i=0;i<towers.Length;i++) {
            string towerPath = "Assets/Defense/Data/TestTurret"+(i+1)+".asset";
            var tower = AssetDatabase.LoadAssetAtPath<TurretDefinition>(towerPath);
            if (tower == null) {
                tower = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/TestTurret1.asset"));
                tower.name = "TestTurret"+(i+1);
                var data = new SerializedObject(tower);
                data.FindProperty("displayName").stringValue = "Test Turret "+(i+1);
                data.FindProperty("damage").floatValue = stats[i,0];
                data.FindProperty("attackRange").floatValue = stats[i,1];
                data.FindProperty("attackCooldown").floatValue = stats[i,2];
                data.FindProperty("cost").intValue = (int)stats[i,3];
                data.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(tower,towerPath);
            }
            towers[i] = tower;
        }
        SetArray(so,"availableTowers",towers);
        
        so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); CardAssetSetup.Run();
    }
    private static void SetArray(SerializedObject so, string field, Object[] values) {
        var array = so.FindProperty(field); array.arraySize = values.Length;
        for (int i=0;i<values.Length;i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
}
