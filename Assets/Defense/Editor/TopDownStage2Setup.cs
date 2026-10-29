using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
public static class TopDownStage2Setup {
    public const string ScenePath="Assets/Defense/Scenes/TopDownStage2.unity";
    public const string Prefabs="Assets/Defense/Prefabs/TopDown2D";
    [MenuItem("Defense/2D Migration/Create Stage 2 Interaction Sandbox")]
    public static void Run() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save your open scenes first.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)!=null)throw new InvalidOperationException("Stage 2 already exists; existing scene edits are preserved.");
        var enemyPrefab=CreateEnemy();var towerPrefab=CreateTurret();
        AssetDatabase.CopyAsset(TopDownStage1Setup.ScenePath,ScenePath);
        var scene=EditorSceneManager.OpenScene(ScenePath);var map=UnityEngine.Object.FindFirstObjectByType<BattlefieldMap2D>();
        foreach(Transform child in map.EnemyRuntimeRoot.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach(Transform child in map.TowerRuntimeRoot.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        var frame=new SerializedObject(map.WorldCamera.GetComponent<TopDownCameraFrame>());frame.FindProperty("minimumSize").floatValue=7;frame.ApplyModifiedPropertiesWithoutUndo();map.WorldCamera.GetComponent<TopDownCameraFrame>().Refresh();
        var systems=new GameObject("InteractionSystems2D");systems.transform.SetParent(map.transform.parent,false);
        var flow=systems.AddComponent<GameFlow>();var wallet=systems.AddComponent<ResourceWallet>();var hp=systems.AddComponent<BaseHealth>();
        var registry=systems.AddComponent<EnemyRegistry>();var registryData=new SerializedObject(registry);registryData.FindProperty("plane").enumValueIndex=(int)WaypointPlane.XY;registryData.ApplyModifiedPropertiesWithoutUndo();
        var input=systems.AddComponent<BattlefieldPointer2D>();Set(input,"map",map);Mask(input,"placementLayers",LayerMask.GetMask("PlacementSlot"));
        var placement=systems.AddComponent<TurretPlacementController>();Set(placement,"worldCamera",map.WorldCamera);Set(placement,"flow",flow);Set(placement,"wallet",wallet);Set(placement,"registry",registry);Set(placement,"pointer2D",input);Set(placement,"spritePrefab",towerPrefab);
        var elixir=systems.AddComponent<ElixirSystem>();
        var casting=systems.AddComponent<SkillCastingController>();Set(casting,"pointer2D",input);
        var spawner=systems.AddComponent<EnemySpawner>();Set(spawner,"path",map.Path);Set(spawner,"baseHealth",hp);Set(spawner,"registry",registry);Set(spawner,"flow",flow);Set(spawner,"enemyRoot",map.EnemyRuntimeRoot);Set(spawner,"spritePrefab",enemyPrefab);
        var schedule=systems.AddComponent<FixedIntervalSpawner>();Set(schedule,"spawner",spawner);
        var rewards=systems.AddComponent<EnemyKillRewards>();Set(rewards,"spawner",spawner);Set(rewards,"wallet",wallet);
        var bootstrap=systems.AddComponent<TopDownInteractionSandbox>();Set(bootstrap,"testStage",AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/Stage1_1.asset"));
        var catalog=Resources.Load<GameCatalog>("GameCatalog");var deck=catalog.InitialDeck.Take(4).Concat(catalog.AvailableCards.OfType<SkillCardDefinition>()).ToArray();
        var data=new SerializedObject(bootstrap);var array=data.FindProperty("testDeck");array.arraySize=deck.Length;for(int i=0;i<deck.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=deck[i];data.ApplyModifiedPropertiesWithoutUndo();
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();canvas.name="Canvas";
        var scaler=canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
        var hud=systems.AddComponent<TopDownInteractionHUD>();Set(hud,"canvas",canvas);Set(systems.GetComponent<BuildMenuUI>(),"placement",placement);
        BindActorPrefabs();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("TOPDOWN_STAGE2_AUTHORED");
    }
    public static void BindActorPrefabs() {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.path!=ScenePath)throw new InvalidOperationException("Open the Stage 2 scene in Edit Mode first.");
        // Opening a different scene may unload assets held only by authoring locals: resolve after scene load.
        var enemy=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/BasicEnemy2D.prefab").GetComponent<Enemy>();
        var turret=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/BasicTower2D.prefab").GetComponent<Tower>();
        if(enemy==null || turret==null)throw new InvalidOperationException("2D actor prefab components are missing.");
        Set(UnityEngine.Object.FindFirstObjectByType<EnemySpawner>(),"spritePrefab",enemy);
        Set(UnityEngine.Object.FindFirstObjectByType<TurretPlacementController>(),"spritePrefab",turret);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    private static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value)=>PrototypeBuilder.Set(obj,field,value);
    private static void Mask(UnityEngine.Object obj,string field,int value){var s=new SerializedObject(obj);s.FindProperty(field).intValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    private static Enemy CreateEnemy() {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/EnemySpritePlaceholder.prefab");var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
        try {
            root.name="BasicEnemy2D";root.layer=LayerMask.NameToLayer("Enemy");root.AddComponent<BasicEnemy>();var hit=root.AddComponent<CircleCollider2D>();hit.radius=.3f;hit.isTrigger=true;
            var view=root.AddComponent<EnemyHealthBar>();var data=new SerializedObject(view);data.FindProperty("offset").vector3Value=new Vector3(0,.65f,0);data.FindProperty("sortingOrder").intValue=60;data.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/BasicEnemy2D.prefab").GetComponent<Enemy>();
        }finally {UnityEngine.Object.DestroyImmediate(root);}
    }
    private static Tower CreateTurret() {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/TowerSpritePlaceholder.prefab");var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
        try {
            root.name="BasicTower2D";var turret=root.AddComponent<BasicTower>();
            return PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/BasicTower2D.prefab").GetComponent<Tower>();
        }finally {UnityEngine.Object.DestroyImmediate(root);}
    }
}
}
