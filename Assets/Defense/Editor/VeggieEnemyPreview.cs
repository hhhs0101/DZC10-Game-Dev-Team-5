using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Development-only entry uses the existing local interaction sandbox, never PlayerSession or stage assets.
[InitializeOnLoad]
public static class VeggieEnemyPreview {
    private const string Key="Defense.VeggiePreview.Pending";
    static VeggieEnemyPreview(){EditorApplication.playModeStateChanged+=OnPlayState;}
    [MenuItem("Defense/Enemies/Open Veggie Sandbox")]
    public static void Open(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes first.");
        foreach(var name in VeggieEnemySetup.Names){var data=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(VeggieEnemySetup.DataRoot+"/"+name+"Enemy.asset");if(data==null||data.Sprite==null)throw new InvalidOperationException("Import the licensed individual sprites before opening the preview.");}
        EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    private static void OnPlayState(PlayModeStateChange state){
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);EditorApplication.delayCall+=SpawnSamples;}
        if(state==PlayModeStateChange.ExitingPlayMode)SessionState.SetBool(Key,false);
    }
    private static void SpawnSamples(){
        if(!EditorApplication.isPlaying||UnityEngine.Object.FindFirstObjectByType<TopDownInteractionSandbox>()==null)return;
        UnityEngine.Object.FindFirstObjectByType<FixedIntervalSpawner>().enabled=false;
        var spawner=UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();var registry=UnityEngine.Object.FindFirstObjectByType<EnemyRegistry>();
        for(int i=0;i<VeggieEnemySetup.Names.Length;i++){
            var data=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(VeggieEnemySetup.DataRoot+"/"+VeggieEnemySetup.Names[i]+"Enemy.asset");
            var enemy=spawner.Spawn(data);enemy.name="Preview - "+data.DisplayName;enemy.Movement.Stop();
            enemy.transform.position=new Vector3((i%5-2)*3,i<5?1.5f:-1.2f,0);registry.NotifyMoved(enemy);
        }
        Debug.Log("VEGGIE_PREVIEW_READY: ten stationary real Enemies. Inspect names in Hierarchy; Tower/Skills use local sandbox rules. Stop Play Mode to reset; no player saves or Test Stage configuration are changed.");
    }
}
}
