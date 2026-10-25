using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
// Explicit additive authoring; does not rebuild maps or reset configured gameplay content.
public static class PlacedTowerInspectionSetup {
    public const string PrefabPath="Assets/Defense/Prefabs/TopDown2D/BasicTower2D.prefab";
    public static void Run() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scene edits before authoring inspection dependencies.");
        var previous=EditorSceneManager.GetSceneManagerSetup();
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try {
            var hit=root.transform.Find("Inspection Hit Area");
            if(hit==null){hit=new GameObject("Inspection Hit Area").transform;hit.SetParent(root.transform,false);}
            // Default layer keeps this trigger out of the unchanged PlacementSlot layer queries.
            hit.gameObject.layer=LayerMask.NameToLayer("Default");
            var collider=hit.GetComponent<BoxCollider2D>();if(collider==null)collider=hit.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger=true;collider.offset=new Vector2(0,.5f);collider.size=new Vector2(1,1.8f);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        try {
            foreach(var path in new[]{TopDownStage2Setup.ScenePath,TopDownStage3Setup.ScenePath}){
                var scene=EditorSceneManager.OpenScene(path);
                var placement=UnityEngine.Object.FindAnyObjectByType<TurretPlacementController>();
                var systems=placement.gameObject;
                var controller=systems.GetComponent<PlacedTowerInspectionController>();if(controller==null)controller=systems.AddComponent<PlacedTowerInspectionController>();
                var visual=systems.transform.Find("Placed Tower Range Indicator");
                if(visual==null){visual=new GameObject("Placed Tower Range Indicator").transform;visual.SetParent(systems.transform,false);}
                var indicator=visual.GetComponent<SkillAreaVisual>();if(indicator==null)indicator=visual.gameObject.AddComponent<SkillAreaVisual>();
                var line=visual.GetComponent<LineRenderer>();line.enabled=false;line.positionCount=0;
                PrototypeBuilder.Set(controller,"hand",systems.GetComponent<GameplayHand>());
                PrototypeBuilder.Set(controller,"flow",systems.GetComponent<GameFlow>());
                PrototypeBuilder.Set(controller,"pointer2D",systems.GetComponent<BattlefieldPointer2D>());
                PrototypeBuilder.Set(controller,"rangeIndicator",indicator);
                PrototypeBuilder.Set(placement,"inspection",controller);
                EditorSceneManager.SaveScene(scene);
            }
        } finally {EditorSceneManager.RestoreSceneManagerSetup(previous);}
        Debug.Log("PLACED_TOWER_INSPECTION_READY: additive 2D hit area and inspection dependencies on Stage 2/3.");
    }
}
}
