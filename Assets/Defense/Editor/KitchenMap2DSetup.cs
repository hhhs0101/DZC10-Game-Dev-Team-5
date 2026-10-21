using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Explicit scene authoring only. Runtime enemies read editable scene Transforms, never this source trace.
public static class KitchenMap2DSetup {
    public const string ArtRoot = "Assets/Defense/Art/Kitchen2D";
    public const string ScenePath = TopDownStage3Setup.ScenePath;
    public const float PixelsPerUnit = 80;
    public static readonly Vector2[] CenterlinePixels = {
        new Vector2(210f,385f),
        new Vector2(268f,396f),
        new Vector2(315f,425f),
        new Vector2(342f,464f),
        new Vector2(343f,505f),
        new Vector2(328f,549f),
        new Vector2(296f,599f),
        new Vector2(267f,647f),
        new Vector2(257f,685f),
        new Vector2(277f,723f),
        new Vector2(319f,757f),
        new Vector2(374f,778f),
        new Vector2(438f,786f),
        new Vector2(501f,782f),
        new Vector2(561f,768f),
        new Vector2(618f,745f),
        new Vector2(668f,712f),
        new Vector2(704f,674f),
        new Vector2(733f,632f),
        new Vector2(760f,591f),
        new Vector2(794f,558f),
        new Vector2(838f,536f),
        new Vector2(884f,525f),
        new Vector2(932f,524f),
        new Vector2(979f,535f),
        new Vector2(1020f,555f),
        new Vector2(1049f,585f),
        new Vector2(1064f,622f),
        new Vector2(1070f,666f),
        new Vector2(1080f,707f),
        new Vector2(1097f,749f),
        new Vector2(1133f,783f),
        new Vector2(1180f,805f),
        new Vector2(1236f,817f),
        new Vector2(1291f,814f),
        new Vector2(1347f,801f),
        new Vector2(1396f,775f),
        new Vector2(1441f,742f),
        new Vector2(1477f,703f),
        new Vector2(1504f,662f),
        new Vector2(1519f,620f),
        new Vector2(1522f,580f),
        new Vector2(1509f,543f),
        new Vector2(1487f,511f),
        new Vector2(1454f,476f),
        new Vector2(1423f,441f),
        new Vector2(1410f,408f),
        new Vector2(1423f,382f)
    };
    private static readonly Vector2[] SlotPixels = { new Vector2(558.0800f,577.3400f), new Vector2(480.0000f,670.0000f), new Vector2(687.6800f,447.7400f), new Vector2(882.0800f,426.1400f), new Vector2(1170.0000f,565.0000f), new Vector2(1224.0800f,674.5400f), new Vector2(860.4800f,764.5400f), new Vector2(120.0000f,650.0000f) };
    private static Vector3 RouteWorld(Vector2 pixel) => new Vector3((pixel.x-836)/PixelsPerUnit,(470.5f-pixel.y)/PixelsPerUnit+.5f,0);
    [MenuItem("Defense/Maps/Apply Kitchen 2D Layout")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene edits before applying a layout.");
        Import("KitchenInterior.png"); Import("GinghamRoute.png");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var map = UnityEngine.Object.FindFirstObjectByType<BattlefieldMap2D>(); var root = map.transform;
        var background = Render("KitchenBackgroundSprite", root, "KitchenInterior.png", 0);
        background.transform.localPosition = new Vector3(0,.5f,0); background.transform.localScale = Vector3.one;
        var route = Render("GinghamPathSprite", root, "GinghamRoute.png", 10);
        route.transform.localPosition = background.transform.localPosition; route.transform.localScale = Vector3.one;
        var old = root.Find("TabletopVisual"); if (old != null) old.gameObject.SetActive(false);
        var area = map.PlayableArea.GetComponent<BoxCollider2D>(); area.size = new Vector2(19.6f,7.5f); area.offset = Vector2.zero;
        area.transform.localPosition = new Vector3(0,-1.7f,0);
        var camera = map.WorldCamera; var frame = camera.GetComponent<TopDownCameraFrame>();
        PrototypeBuilder.Set(frame,"framingVisual",background);
        var framing = new SerializedObject(frame); framing.FindProperty("minimumSize").floatValue = 8.5f; framing.FindProperty("padding").floatValue = .75f; framing.ApplyModifiedPropertiesWithoutUndo(); frame.Refresh();
        var path = map.Path;
        var debug = root.Find("PathVisualization"); if(debug != null) debug.gameObject.SetActive(false);
        var destination = root.Find("BaseVisualAnchor");
        if(destination == null) {destination = new GameObject("BaseVisualAnchor").transform;destination.SetParent(root,false);}
        var spawn = map.EnemySpawnRoot; spawn.name = "EnemySpawnRoot (Refrigerator)"; destination.name = "BaseVisualAnchor";
        PrototypeBuilder.Set(map,"baseDestination",destination);
        // Keep the logical endpoint active; hide only its temporary blue marker.
        foreach (var visual in destination.GetComponentsInChildren<SpriteRenderer>(true)) {
            var color = visual.color; color.a = 0; visual.color = color;
        }
        // Replace only authored path-point children; other gameplay/visual objects remain intact.
        for(int i=path.transform.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(path.transform.GetChild(i).gameObject);
        var data = new SerializedObject(path); data.FindProperty("plane").enumValueIndex = (int)WaypointPlane.XY;
        var points = data.FindProperty("points"); points.arraySize = CenterlinePixels.Length;
        for(int i=0;i<CenterlinePixels.Length;i++) {
            Transform point;
            if(i==0) point=spawn;
            else if(i==CenterlinePixels.Length-1) point=destination;
            else {point=new GameObject("Waypoint "+i.ToString("00")).transform;point.SetParent(path.transform,false);}
            point.position = RouteWorld(CenterlinePixels[i]); points.GetArrayElementAtIndex(i).objectReferenceValue = point;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        var marker = spawn.GetComponentInChildren<SpriteRenderer>(); if(marker!=null) marker.enabled=false;
        for(int i=0;i<map.PlacementSlots.Count;i++) map.PlacementSlots[i].transform.position = RouteWorld(SlotPixels[i]);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("KITCHEN_2D_READY: full left-to-right cloth centerline, editable anchors/slots, existing gameplay systems.");
    }
    private static void Import(string name) {
        string path=ArtRoot+"/"+name; AssetDatabase.ImportAsset(path);
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null) throw new InvalidOperationException("Missing supplied artwork: "+path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=PixelsPerUnit; importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
        importer.alphaSource=TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency=true; importer.wrapMode=TextureWrapMode.Clamp;
        var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.FullRect; settings.spriteAlignment=(int)SpriteAlignment.Center; importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
    private static SpriteRenderer Render(string name,Transform parent,string file,int order) {
        var child=parent.Find(name); if(child==null){child=new GameObject(name).transform;child.SetParent(parent,false);}
        var view=child.GetComponent<SpriteRenderer>(); if(view==null) view=child.gameObject.AddComponent<SpriteRenderer>();
        view.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot+"/"+file);view.color=Color.white;view.sortingOrder=order;return view;
    }
}
}
