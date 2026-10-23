using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Isolated local Stage2 fixture; test-only tuning never writes back to EnemyDefinition assets.
[InitializeOnLoad]
public static class EnemyRolesDevelopmentHarness {
    private const string Key="Defense.EnemyRoles.DevelopmentHarness";
    private static readonly List<EnemyDefinition> copies=new List<EnemyDefinition>();
    static EnemyRolesDevelopmentHarness() {
        EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode){foreach(var copy in copies)if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);copies.Clear();SessionState.SetBool(Key,false);}};
    }
    [MenuItem("Defense/Enemies/Start Role Sandbox (Test Tuning)")]
    public static void Run() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scene edits first.");
        if(EnemyRolesSetup.Names.Any(n=>AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyRolesSetup.PathFor(n))==null))throw new InvalidOperationException("Apply the specified Enemy role data first.");
        EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    public static void VerifyBatch() { SessionState.SetBool(Key+".Verify",true);Run(); }
    private static void Tick() {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        var fixture=UnityEngine.Object.FindAnyObjectByType<TopDownInteractionSandbox>();if(fixture==null||GameObject.Find("Hand Card 1")==null)return;
        SessionState.SetBool(Key,false);var systems=fixture.gameObject;
        systems.GetComponent<FixedIntervalSpawner>().enabled=false;
        foreach(var enemy in systems.GetComponent<EnemyRegistry>().Enemies.ToArray())UnityEngine.Object.Destroy(enemy.gameObject);
        systems.GetComponent<BaseHealth>().Initialize(10000);systems.GetComponent<ResourceWallet>().Initialize(10000);systems.GetComponent<ElixirSystem>().Advance(100);
        var path=UnityEngine.Object.FindAnyObjectByType<BattlefieldMap2D>().Path;
        for(int i=0;i<EnemyRolesSetup.Names.Length;i++) {
            var data=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyRolesSetup.PathFor(EnemyRolesSetup.Names[i])));copies.Add(data);
            var values=new SerializedObject(data);values.FindProperty("supportArea").floatValue=3;values.FindProperty("disruptionRange").floatValue=6;values.FindProperty("disruptionCooldown").floatValue=2;values.FindProperty("disruptionDuration").floatValue=1.5f;values.ApplyModifiedPropertiesWithoutUndo();
            var enemy=systems.GetComponent<EnemySpawner>().Spawn(data);enemy.name="Role Sandbox — "+data.DisplayName;
            // Leave the initial centerline while moving to each test point through the same movement API.
            enemy.Movement.Advance(i*1.5f);enemy.Movement.Stop();
        }
        if(SessionState.GetBool(Key+".Verify",false)) {
            SessionState.SetBool(Key+".Verify",false);
            var all=systems.GetComponent<EnemyRegistry>().Enemies;
            bool valid=all.Count==EnemyRolesSetup.Names.Length&&all.Select(e=>e.Definition.EnemyTypeId).Distinct().Count()==EnemyRolesSetup.Names.Length&&
                all.Count(e=>e.GetComponent<EnemyRoleAbility>()!=null&&e.GetComponent<EnemyRoleAbility>().IsOperational)==3&&
                EnemyRolesSetup.Names.All(n=>{var original=AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyRolesSetup.PathFor(n));return original.AbilityConfigured;})&&
                PlayerSession.Presets==null&&PlayerSession.Currency==null;
            if(valid)Debug.Log("ENEMY_ROLE_HARNESS_PASSED: eight unique runtime clones; three operational support/disruption abilities; authored tuning is configured; no persistent services.");
            else Debug.LogError("ENEMY_ROLE_HARNESS_FAILED: isolated initial sandbox setup contract");
            EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(valid?0:1);return;
        }
        Debug.Log("ROLE_SANDBOX_TEST_TUNING: Stage2 local fixture, no persistent services. Cloned definitions only: radius 3, disruption range 6, cooldown 2s, duration 1.5s. Place Towers using the existing Hand. Enemies are stopped along the normal path for inspection; use EnemyRolesValidation for movement, Pause and cleanup checks. Boss summons and tower damage are active. ShieldSupportAbility reduces damage by 60%. Stop Play Mode to discard fixtures.");
    }
}
}
