using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
public static class TopDownStage3Setup {
    public const string ScenePath = "Assets/Defense/Scenes/TopDownStage3.unity";
    [MenuItem("Defense/2D Migration/Create Stage 3 Integrated Battlefield")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i=0;i<SceneManager.sceneCount;i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null) throw new InvalidOperationException("Stage 3 already exists; existing scene edits are preserved.");
        if (!AssetDatabase.CopyAsset(TopDownStage2Setup.ScenePath,ScenePath)) throw new InvalidOperationException("Stage 2 scene is required.");
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var fixture=UnityEngine.Object.FindFirstObjectByType<TopDownInteractionSandbox>();
        var systems=fixture.gameObject; systems.name="GameplaySystems2D";
        UnityEngine.Object.DestroyImmediate(fixture);
        UnityEngine.Object.DestroyImmediate(systems.GetComponent<TopDownInteractionHUD>());
        var bootstrap=systems.AddComponent<StageInitializer>();
        Set(bootstrap,"definition",AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage.asset"));
        Set(bootstrap,"flow",systems.GetComponent<GameFlow>());
        Set(bootstrap,"wallet",systems.GetComponent<ResourceWallet>());
        Set(bootstrap,"baseHealth",systems.GetComponent<BaseHealth>());
        Set(bootstrap,"schedule",systems.GetComponent<FixedIntervalSpawner>());
        var config=new SerializedObject(bootstrap);config.FindProperty("trackScheduledDefeats").boolValue=true;config.ApplyModifiedPropertiesWithoutUndo();
        var ui=systems.AddComponent<GameplayUI>();
        Set(ui,"canvas",UnityEngine.Object.FindFirstObjectByType<Canvas>());
        Set(ui,"flow",systems.GetComponent<GameFlow>());
        Set(ui,"wallet",systems.GetComponent<ResourceWallet>());
        Set(ui,"baseHealth",systems.GetComponent<BaseHealth>());
        Set(ui,"placement",systems.GetComponent<TurretPlacementController>());
        // Resolve prefabs after opening the copied scene so references survive scene asset unloading.
        Set(systems.GetComponent<EnemySpawner>(),"spritePrefab",AssetDatabase.LoadAssetAtPath<GameObject>(TopDownStage2Setup.Prefabs+"/BasicEnemy2D.prefab").GetComponent<Enemy>());
        Set(systems.GetComponent<TurretPlacementController>(),"spritePrefab",AssetDatabase.LoadAssetAtPath<GameObject>(TopDownStage2Setup.Prefabs+"/BasicTurret2D.prefab").GetComponent<Turret>());
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        // Preserve startup order and the original 3D entry; only append the preview scene.
        if (!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        AssetDatabase.SaveAssets();Debug.Log("TOPDOWN_STAGE3_AUTHORED");
    }
    private static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value)=>PrototypeBuilder.Set(obj,field,value);
}
}
