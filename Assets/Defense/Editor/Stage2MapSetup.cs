using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
public static class Stage2MapSetup {
    public const string ArtRoot = "Assets/Defense/Art/Kitchen2D";
    public const string RouteSpriteFile = "GinghamRouteStage2.png";
    public const string SceneName = "TopDownStage1_2";
    public const string ScenePath = "Assets/Defense/Scenes/TopDownStage1_2.unity";
    public const float PixelsPerUnit = 80f;
    public const float BackgroundPixelsPerUnit = 41.625f;
    public static readonly Vector3 BackgroundLocalPosition = new Vector3(2.78f, .5f, 0f);

    public static readonly Vector2[] UpperBranchPixels = {
        new Vector2(179.1f,439.4f), new Vector2(218.6f,437.3f), new Vector2(266.1f,443.5f), new Vector2(313.6f,453.8f),
        new Vector2(353.1f,470.2f), new Vector2(389.5f,494.9f), new Vector2(424.3f,522.6f), new Vector2(448.1f,542.2f),
        new Vector2(479.7f,512.4f), new Vector2(516.1f,484.6f), new Vector2(558.9f,462.0f), new Vector2(614.3f,436.3f),
        new Vector2(677.6f,411.6f), new Vector2(740.9f,395.2f), new Vector2(804.2f,385.9f), new Vector2(867.5f,381.8f),
        new Vector2(930.8f,383.9f), new Vector2(994.1f,392.1f), new Vector2(1057.4f,405.5f), new Vector2(1120.7f,424.0f),
        new Vector2(1184.0f,447.6f), new Vector2(1247.3f,473.3f), new Vector2(1310.6f,499.0f), new Vector2(1358.0f,514.4f),
        new Vector2(1397.6f,522.6f), new Vector2(1437.2f,505.2f), new Vector2(1476.7f,490.8f), new Vector2(1513.1f,484.6f),
        new Vector2(1544.8f,486.7f)
    };

    public static readonly Vector2[] MiddleBranchPixels = {
        new Vector2(179.1f,439.4f), new Vector2(218.6f,437.3f), new Vector2(266.1f,443.5f), new Vector2(313.6f,453.8f),
        new Vector2(353.1f,470.2f), new Vector2(389.5f,494.9f), new Vector2(424.3f,522.6f), new Vector2(448.1f,542.2f),
        new Vector2(487.7f,560.7f), new Vector2(535.1f,579.2f), new Vector2(582.6f,604.9f), new Vector2(638.0f,635.7f),
        new Vector2(693.4f,658.3f), new Vector2(756.7f,671.7f), new Vector2(820.0f,676.8f), new Vector2(883.3f,677.9f),
        new Vector2(946.6f,675.8f), new Vector2(1009.9f,666.6f), new Vector2(1073.2f,651.1f), new Vector2(1136.5f,627.5f),
        new Vector2(1199.8f,597.7f), new Vector2(1263.1f,569.9f), new Vector2(1318.5f,546.3f), new Vector2(1361.2f,531.9f),
        new Vector2(1397.6f,522.6f), new Vector2(1437.2f,505.2f), new Vector2(1476.7f,490.8f), new Vector2(1513.1f,484.6f),
        new Vector2(1544.8f,486.7f)
    };

    public static readonly Vector2[] BottomBranchPixels = {
        new Vector2(179.1f,439.4f), new Vector2(218.6f,437.3f), new Vector2(266.1f,443.5f), new Vector2(313.6f,453.8f),
        new Vector2(353.1f,470.2f), new Vector2(389.5f,494.9f), new Vector2(424.3f,522.6f), new Vector2(448.1f,542.2f),
        new Vector2(421.2f,584.3f), new Vector2(389.5f,630.6f), new Vector2(353.1f,682.0f), new Vector2(316.7f,728.2f),
        new Vector2(291.4f,764.2f), new Vector2(300.9f,791.0f), new Vector2(337.3f,807.4f), new Vector2(400.6f,815.6f),
        new Vector2(487.7f,820.8f), new Vector2(598.4f,828.0f), new Vector2(725.0f,833.1f), new Vector2(867.5f,835.2f),
        new Vector2(1009.9f,835.2f), new Vector2(1152.3f,834.1f), new Vector2(1271.0f,831.0f), new Vector2(1366.0f,817.7f),
        new Vector2(1437.2f,795.1f), new Vector2(1487.8f,756.0f), new Vector2(1506.8f,710.8f), new Vector2(1487.8f,659.4f),
        new Vector2(1453.0f,608.0f), new Vector2(1421.3f,558.6f), new Vector2(1397.6f,522.6f), new Vector2(1437.2f,505.2f),
        new Vector2(1476.7f,490.8f), new Vector2(1513.1f,484.6f), new Vector2(1544.8f,486.7f)
    };

    private static readonly Vector2[][] AllBranches = {
        UpperBranchPixels,
        MiddleBranchPixels,
        BottomBranchPixels
    };

    private static readonly string[] BranchObjectNames = {
        "WaypointPath2D",
        "WaypointPath2D (Middle)",
        "WaypointPath2D (Bottom)"
    };

    private static readonly Vector2[] SlotPixels = {
        new Vector2(685.0f,532.0f),
        new Vector2(885.0f,530.0f),
        new Vector2(1085.0f,532.0f),
        new Vector2(520.0f,718.0f),
        new Vector2(1250.0f,718.0f),
        new Vector2(165.0f,565.0f),
        new Vector2(125.0f,755.0f),
        new Vector2(1365.0f,405.0f)
    };

    private static Vector3 RouteWorld(Vector2 pixel) => new Vector3((pixel.x - 836f) / PixelsPerUnit, (470.5f - pixel.y) / PixelsPerUnit + .5f, 0f);

    [MenuItem("Defense/Maps/Apply Stage 1-2 Split Route Layout")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene edits before applying a layout.");

        Import("KitchenInterior.png", BackgroundPixelsPerUnit);
        Import(RouteSpriteFile, PixelsPerUnit);

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) {
            if (!AssetDatabase.CopyAsset(TopDownStage3Setup.ScenePath, ScenePath))
                throw new InvalidOperationException("Could not copy base 2D battlefield scene to " + ScenePath);
        }

        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = UnityEngine.Object.FindFirstObjectByType<BattlefieldMap2D>();
        var root = map.transform;

        var background = Render("KitchenBackgroundSprite", root, "KitchenInterior.png", 0);
        background.transform.localPosition = BackgroundLocalPosition;
        background.transform.localScale = Vector3.one;

        var route = Render("GinghamPathSprite", root, RouteSpriteFile, 10);
        route.transform.localPosition = new Vector3(0f, .5f, 0f);
        route.transform.localScale = Vector3.one;

        var old = root.Find("TabletopVisual");
        if (old != null) old.gameObject.SetActive(false);

        var area = map.PlayableArea.GetComponent<BoxCollider2D>();
        area.size = new Vector2(19.6f, 7.5f);
        area.offset = Vector2.zero;
        area.transform.localPosition = new Vector3(0f, -1.7f, 0f);

        var camera = map.WorldCamera;
        var frame = camera.GetComponent<TopDownCameraFrame>();
        PrototypeBuilder.Set(frame, "framingVisual", background);
        var framing = new SerializedObject(frame);
        framing.FindProperty("minimumSize").floatValue = 8.5f;
        framing.FindProperty("padding").floatValue = .75f;
        framing.ApplyModifiedPropertiesWithoutUndo();
        frame.Refresh();

        var debug = root.Find("PathVisualization");
        if (debug != null) debug.gameObject.SetActive(false);

        var destination = root.Find("BaseVisualAnchor");
        if (destination == null) {
            destination = new GameObject("BaseVisualAnchor").transform;
            destination.SetParent(root, false);
        }
        var spawn = map.EnemySpawnRoot;
        spawn.name = "EnemySpawnRoot (Refrigerator)";
        destination.name = "BaseVisualAnchor";
        PrototypeBuilder.Set(map, "baseDestination", destination);

        foreach (var visual in destination.GetComponentsInChildren<SpriteRenderer>(true)) {
            var color = visual.color;
            color.a = 0f;
            visual.color = color;
        }
        var marker = spawn.GetComponentInChildren<SpriteRenderer>();
        if (marker != null) marker.enabled = false;

        var builtBranches = new WaypointPath[AllBranches.Length];
        for (int b = 0; b < AllBranches.Length; b++) {
            Transform pathTransform;
            if (b == 0) {
                pathTransform = map.Path.transform;
                pathTransform.name = BranchObjectNames[0];
            } else {
                pathTransform = root.Find(BranchObjectNames[b]);
                if (pathTransform == null) {
                    pathTransform = new GameObject(BranchObjectNames[b]).transform;
                    pathTransform.SetParent(root, false);
                }
            }
            var branchPath = pathTransform.GetComponent<WaypointPath>() ?? pathTransform.gameObject.AddComponent<WaypointPath>();
            BuildWaypoints(branchPath, AllBranches[b], spawn, destination);
            builtBranches[b] = branchPath;
        }

        var mapData = new SerializedObject(map);
        mapData.FindProperty("path").objectReferenceValue = builtBranches[0];
        var mapBranchesProp = mapData.FindProperty("branchPaths");
        mapBranchesProp.arraySize = builtBranches.Length;
        for (int b = 0; b < builtBranches.Length; b++)
            mapBranchesProp.GetArrayElementAtIndex(b).objectReferenceValue = builtBranches[b];
        mapData.ApplyModifiedPropertiesWithoutUndo();

        var spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        var spawnerData = new SerializedObject(spawner);
        spawnerData.FindProperty("path").objectReferenceValue = builtBranches[0];
        var spawnerBranchesProp = spawnerData.FindProperty("branchPaths");
        spawnerBranchesProp.arraySize = builtBranches.Length;
        for (int b = 0; b < builtBranches.Length; b++)
            spawnerBranchesProp.GetArrayElementAtIndex(b).objectReferenceValue = builtBranches[b];
        spawnerData.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < map.PlacementSlots.Count; i++)
            map.PlacementSlots[i].transform.position = RouteWorld(SlotPixels[i]);

        var stage1_2 = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/Stage1_2.asset");
        var initializer = UnityEngine.Object.FindFirstObjectByType<StageInitializer>();
        if (initializer != null && stage1_2 != null)
            PrototypeBuilder.Set(initializer, "definition", stage1_2);

        if (stage1_2 != null) {
            var stageData = new SerializedObject(stage1_2);
            stageData.FindProperty("sceneName").stringValue = SceneName;
            stageData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage1_2);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath)) {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
                .ToArray();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("STAGE_1_2_SPLIT_ROUTE_READY: " + ScenePath);
    }

    private static void BuildWaypoints(WaypointPath path, Vector2[] pixels, Transform spawn, Transform destination) {
        for (int i = path.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(path.transform.GetChild(i).gameObject);

        var data = new SerializedObject(path);
        data.FindProperty("plane").enumValueIndex = (int)WaypointPlane.XY;
        var points = data.FindProperty("points");
        points.arraySize = pixels.Length;
        for (int i = 0; i < pixels.Length; i++) {
            Transform point;
            if (i == 0) point = spawn;
            else if (i == pixels.Length - 1) point = destination;
            else {
                point = new GameObject("Waypoint " + i.ToString("00")).transform;
                point.SetParent(path.transform, false);
            }
            point.position = RouteWorld(pixels[i]);
            points.GetArrayElementAtIndex(i).objectReferenceValue = point;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Import(string name, float pixelsPerUnit = PixelsPerUnit) {
        string path = ArtRoot + "/" + name;
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing supplied artwork: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static SpriteRenderer Render(string name, Transform parent, string file, int order) {
        var child = parent.Find(name);
        if (child == null) {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }
        var view = child.GetComponent<SpriteRenderer>() ?? child.gameObject.AddComponent<SpriteRenderer>();
        view.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot + "/" + file);
        view.color = Color.white;
        view.sortingOrder = order;
        return view;
    }
}
}

