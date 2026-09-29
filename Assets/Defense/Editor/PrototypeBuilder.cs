using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Defense.Editor {
public static class PrototypeBuilder {
    private const string Root = "Assets/Defense/";
    [MenuItem("Defense/Create Missing Prototype Assets")]
    public static void Build() {
        if (File.Exists(Root + "Scenes/MainMenu.unity") || File.Exists(Root + "Scenes/TestStage.unity")) {
            Debug.Log("Prototype scenes already exist. Generation skipped to preserve edits."); return;
        }
        PlayerSettings.companyName = "Prototype";
        PlayerSettings.productName = "2D Defense";
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
        Sprite sprite = CreateSprite();
        var enemyObject = Visual("BasicEnemy", Vector3.zero, new Vector3(.55f,.55f,1), new Color(.95f,.3f,.3f), sprite);
        var enemy = enemyObject.AddComponent<BasicEnemy>();
        enemyObject.AddComponent<EnemyHealthBar>();
        var enemyPrefab = PrefabUtility.SaveAsPrefabAsset(enemyObject, Root + "Prefabs/BasicEnemy.prefab").GetComponent<Enemy>();
        Object.DestroyImmediate(enemyObject);
        var turretObject = Visual("BasicTurret", Vector3.zero, new Vector3(.65f,.65f,1), new Color(.2f,.7f,1), sprite);
        turretObject.GetComponent<SpriteRenderer>().sortingOrder = 2;
        var turret = turretObject.AddComponent<BasicTurret>();
        Set(turret,"attack",turretObject.AddComponent<DirectDamageAttack>());
        var turretPrefab = PrefabUtility.SaveAsPrefabAsset(turretObject, Root + "Prefabs/BasicTurret.prefab").GetComponent<Turret>();
        Object.DestroyImmediate(turretObject);
        var closest = Asset<ClosestTargeting>("ClosestTargeting");
        var enemyData = Asset<EnemyDefinition>("BasicEnemy"); Set(enemyData,"prefab",enemyPrefab);
        var turretData = Asset<TurretDefinition>("BasicTurret"); Set(turretData,"prefab",turretPrefab); Set(turretData,"targeting",closest);
        var stage = Asset<StageDefinition>("TestStage"); Set(stage,"enemy",enemyData); SetArray(stage,"availableTurrets",new Object[]{
            TestTurret(1,turretPrefab,closest,10,3,1,50),
            TestTurret(2,turretPrefab,closest,30,5,2.5f,75),
            TestTurret(3,turretPrefab,closest,5,2.5f,1f/3f,100)
        });
        AssetDatabase.SaveAssets();
        BuildStage(stage,sprite); BuildMenu(stage);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(Root+"Scenes/MainMenu.unity",true),
            new EditorBuildSettingsScene(Root+"Scenes/TestStage.unity",true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("DEFENSE_PROTOTYPE_CREATED");
    }
    private static void BuildMenu(StageDefinition stage) {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(Root+"Data/TestStage.asset");
        Camera camera = Camera(); camera.backgroundColor = new Color(.07f,.1f,.15f);
        Canvas canvas = Canvas();
        var ui = new GameObject("MainMenuUI").AddComponent<MainMenuUI>();
        Set(ui,"canvas",canvas); SetArray(ui,"stages",new Object[]{stage});
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"Scenes/MainMenu.unity");
    }
    private static void BuildStage(StageDefinition stage, Sprite sprite) {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        Camera camera = Camera();
        var flow = new GameObject("GameFlow").AddComponent<GameFlow>();
        var systems = new GameObject("StageSystems");
        var wallet = systems.AddComponent<ResourceWallet>();
        var registry = systems.AddComponent<EnemyRegistry>();
        var spawner = systems.AddComponent<EnemySpawner>();
        var rewards = systems.AddComponent<EnemyKillRewards>();
        Set(rewards,"spawner",spawner); Set(rewards,"wallet",wallet);
        var schedule = systems.AddComponent<FixedIntervalSpawner>(); Set(schedule,"spawner",spawner);
        var path = new GameObject("WaypointPath").AddComponent<WaypointPath>();
        Vector3[] positions = {new Vector3(-8,2),new Vector3(-3,2),new Vector3(-3,-1),new Vector3(3,-1),new Vector3(3,2),new Vector3(8,2)};
        Object[] points = new Object[positions.Length];
        for (int i=0;i<positions.Length;i++) {
            var point = new GameObject("Waypoint "+i).transform; point.SetParent(path.transform); point.position = positions[i]; points[i] = point;
            if (i == 0) continue;
            Vector3 difference = positions[i]-positions[i-1];
            var line = Visual("Path segment",(positions[i]+positions[i-1])/2,new Vector3(difference.magnitude,.35f,1),new Color(.35f,.38f,.42f),sprite);
            line.transform.SetParent(path.transform); line.transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(difference.y,difference.x)*Mathf.Rad2Deg);
            line.GetComponent<SpriteRenderer>().sortingOrder = -1;
        }
        SetArray(path,"points",points);
        var baseObject = Visual("Base",positions[positions.Length-1],Vector3.one,new Color(.3f,.5f,1),sprite);
        var baseHealth = baseObject.AddComponent<BaseHealth>();
        var enemyRoot = new GameObject("Enemies").transform;
        Set(spawner,"path",path); Set(spawner,"baseHealth",baseHealth); Set(spawner,"registry",registry); Set(spawner,"flow",flow); Set(spawner,"enemyRoot",enemyRoot);
        var slotRoot = new GameObject("PlacementSlots").transform;
        Vector3[] slots = {new Vector3(-6,3.6f),new Vector3(-6,.3f),new Vector3(-1,2),new Vector3(-1,-3),new Vector3(1,1),new Vector3(4.7f,0),new Vector3(5.5f,3.6f),new Vector3(1,-3)};
        for (int i=0;i<slots.Length;i++) {
            var slot = Visual("Slot "+(i+1),slots[i],Vector3.one,new Color(.2f,.45f,.3f),sprite);
            slot.layer = 8; slot.transform.SetParent(slotRoot); slot.AddComponent<TowerPlacementSlot>();
        }
        var placement = systems.AddComponent<TurretPlacementController>();
        Set(placement,"worldCamera",camera); Set(placement,"flow",flow); Set(placement,"wallet",wallet); Set(placement,"registry",registry);
        var placementSettings = new SerializedObject(placement); placementSettings.FindProperty("placementLayers").intValue = 1<<8; placementSettings.ApplyModifiedPropertiesWithoutUndo();
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "PlacementSlot"; tags.ApplyModifiedPropertiesWithoutUndo();
        var initializer = systems.AddComponent<StageInitializer>();
        Set(initializer,"definition",stage); Set(initializer,"flow",flow); Set(initializer,"wallet",wallet); Set(initializer,"baseHealth",baseHealth); Set(initializer,"schedule",schedule);
        Canvas canvas = Canvas();
        var ui = new GameObject("GameplayUI").AddComponent<GameplayUI>();
        Set(ui,"canvas",canvas); Set(ui,"flow",flow); Set(ui,"baseHealth",baseHealth); Set(ui,"wallet",wallet); Set(ui,"placement",placement);
        var build = ui.GetComponent<BuildMenuUI>(); Set(build,"stage",stage); Set(build,"placement",placement);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"Scenes/TestStage.unity");
    }
    private static TurretDefinition TestTurret(int number, Turret prefab, TargetingStrategy strategy, float damage, float range, float cooldown, int cost) {
        var data = Asset<TurretDefinition>("TestTurret"+number);
        Set(data,"prefab",prefab); Set(data,"targeting",strategy);
        var serialized = new SerializedObject(data);
        serialized.FindProperty("displayName").stringValue = "Test Turret " + number;
        serialized.FindProperty("damage").floatValue = damage;
        serialized.FindProperty("attackRange").floatValue = range;
        serialized.FindProperty("attackCooldown").floatValue = cooldown;
        serialized.FindProperty("cost").intValue = cost;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return data;
    }
    private static T Asset<T>(string name) where T : ScriptableObject {
        string path = Root+"Data/"+name+".asset";
        T existing = AssetDatabase.LoadAssetAtPath<T>(path); if (existing != null) return existing;
        T asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path); return asset;
    }
    private static Sprite CreateSprite() {
        string path = Root+"Art/Placeholder.png";
        var texture = new Texture2D(2,2); texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white}); texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture); AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 2; importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static GameObject Visual(string name, Vector3 position, Vector3 scale, Color color, Sprite sprite) {
        var go = new GameObject(name); go.transform.position = position; go.transform.localScale = scale;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; return go;
    }
    private static Camera Camera() {
        var go = new GameObject("Main Camera"); go.tag = "MainCamera"; go.transform.position = new Vector3(0,0,-10);
        var camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f,.13f,.17f); return camera;
    }
    private static Canvas Canvas() {
        var go = new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule)); return canvas;
    }
    public static void Set(Object target, string field, Object value) {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray(Object target, string field, Object[] values) {
        var serialized = new SerializedObject(target); var property = serialized.FindProperty(field); property.arraySize = values.Length;
        for (int i=0;i<values.Length;i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
}
