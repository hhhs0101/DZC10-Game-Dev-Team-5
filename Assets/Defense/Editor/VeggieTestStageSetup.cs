using System;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
public static class VeggieTestStageSetup {
    public const string StagePath = "Assets/Defense/Data/TestStage2.asset";
    [MenuItem("Defense/Enemies/Create Test Stage 2")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var entries = new EnemyDefinition[VeggieEnemySetup.Names.Length];
        for (int i = 0; i < entries.Length; i++) {
            entries[i] = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(VeggieEnemySetup.DataRoot + "/" + VeggieEnemySetup.Names[i] + "Enemy.asset");
            if (entries[i] == null) throw new InvalidOperationException("Import the ten veggie Enemy definitions first.");
        }
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Defense/Resources/GameCatalog.asset");
        if (catalog == null) throw new InvalidOperationException("The existing GameCatalog is required.");
        var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(StagePath);
        if (stage == null) {
            stage = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage.asset"));
            stage.name = "TestStage2";
            var data = new SerializedObject(stage);
            data.FindProperty("displayName").stringValue = "Test Stage 2";
            data.FindProperty("sceneName").stringValue = PlayerSession.TopDownSceneName;
            data.FindProperty("baseHealth").intValue = 10;
            data.FindProperty("startingResources").intValue = 300;
            data.FindProperty("spawnInterval").floatValue = 1;
            data.FindProperty("enemy").objectReferenceValue = entries[0];
            data.FindProperty("finiteSpawning").boolValue = true;
            data.FindProperty("totalEnemies").intValue = entries.Length;
            data.FindProperty("alwaysUnlocked").boolValue = true;
            var sequence = data.FindProperty("spawnSequence"); sequence.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++) sequence.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            data.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(stage, StagePath);
        }
        var serialized = new SerializedObject(catalog); var stages = serialized.FindProperty("stages");
        bool present = false;
        for (int i = 0; i < stages.arraySize; i++) if (stages.GetArrayElementAtIndex(i).objectReferenceValue == stage) present = true;
        if (!present) { int end = stages.arraySize; stages.arraySize++; stages.GetArrayElementAtIndex(end).objectReferenceValue = stage; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        AssetDatabase.SaveAssets();
        Debug.Log("TEST_STAGE_2_READY: a separate unlocked 2D stage, one of each veggie in definition order.");
    }
}
}
