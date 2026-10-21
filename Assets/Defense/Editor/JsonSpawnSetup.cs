using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
// Explicit additive authoring: one new reference on TestStage2; existing fields and GUIDs are preserved.
public static class JsonSpawnSetup {
    public const string PlanPath="Assets/Defense/Data/SpawnPlans/TestStage2.json";
    public const string CatalogPath="Assets/Defense/Resources/EnemyDefinitionCatalog.asset";
    [MenuItem("Defense/Stages/Configure Test Stage 2 JSON Plan")]
    public static void Run() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        AssetDatabase.Refresh();
        var catalog=AssetDatabase.LoadAssetAtPath<EnemyDefinitionCatalog>(CatalogPath);bool created=catalog==null;
        if(created)catalog=ScriptableObject.CreateInstance<EnemyDefinitionCatalog>();
        var definitions=AssetDatabase.FindAssets("t:EnemyDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(d=>d!=null&&!string.IsNullOrWhiteSpace(d.EnemyTypeId)).OrderBy(d=>d.EnemyTypeId,StringComparer.Ordinal).ToArray();
        var data=new SerializedObject(catalog);var values=data.FindProperty("definitions");values.arraySize=definitions.Length;
        for(int i=0;i<definitions.Length;i++)values.GetArrayElementAtIndex(i).objectReferenceValue=definitions[i];data.ApplyModifiedPropertiesWithoutUndo();
        if(!catalog.TryBuildLookup(out var lookup,out var issue))throw new InvalidOperationException(issue);
        var stage=AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage2.asset");var plan=AssetDatabase.LoadAssetAtPath<TextAsset>(PlanPath);
        if(stage==null||plan==null)throw new InvalidOperationException("TestStage2 and its JSON TextAsset must exist.");
        if(!SpawnPlanLoader.TryLoad(plan,stage.DisplayName,catalog,out _,out issue))throw new InvalidOperationException(issue);
        if(created)AssetDatabase.CreateAsset(catalog,CatalogPath);else {EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);}
        var fields=new SerializedObject(stage);fields.FindProperty("spawnPlanJson").objectReferenceValue=plan;fields.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(stage);AssetDatabase.SaveAssetIfDirty(stage);
        Debug.Log("JSON_SPAWN_READY: TestStage2 plan attached; "+lookup.Count+" real stable enemy IDs; legacy fields unchanged.");
    }
}
}
