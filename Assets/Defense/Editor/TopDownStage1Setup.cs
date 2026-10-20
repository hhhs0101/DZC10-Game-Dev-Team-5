using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
// Explicit authoring only. Never rewrites the old scene, catalog, Stage definitions or save files.
public static class TopDownStage1Setup {
    public const string ScenePath = "Assets/Defense/Scenes/TopDownStage1.unity";
    public const string Prefabs = "Assets/Defense/Prefabs/TopDown2D";
    public const string Art = "Assets/Defense/Art/TopDown2D";
    [MenuItem("Defense/2D Migration/Create Stage 1 Prototype")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before authoring.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) throw new InvalidOperationException("2D scene already exists; edit its Inspector configuration instead of overwriting it.");
        for(int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your open scenes before creating the prototype.");
        Directory.CreateDirectory(Prefabs); Directory.CreateDirectory(Art); AssetDatabase.Refresh();
        var square = AssetDatabase.LoadAllAssetsAtPath("Assets/Defense/Art/Placeholder.png").OfType<Sprite>().First();
        var circle = CreateCircle();
        var enemyPrefab = VisualPrefab("EnemySpritePlaceholder",circle,new Color(.95f,.3f,.25f),new Vector2(.6f,.6f));
        var towerPrefab = VisualPrefab("TowerSpritePlaceholder",square,new Color(.2f,.65f,.95f),new Vector2(.8f,.8f));
        var slotPrefab = SlotPrefab(square);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root = new GameObject("Battlefield2D").transform;
        var camera = Child("WorldCamera",root).gameObject.AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f,.12f,.16f);
        camera.nearClipPlane = .1f; camera.farClipPlane = 50; camera.orthographic = true;
        camera.gameObject.AddComponent<AudioListener>();
        var map = Child("MapRoot",root);
        // Placeholder authoring defaults only; all resulting positions/shapes are editable scene data.
        var table = Sprite("TabletopVisual",map,square,new Color(.57f,.37f,.2f),new Vector2(20,10),0);
        var area = Child("PlayableArea2D",map).gameObject.AddComponent<PlayableArea2D>();
        area.gameObject.layer = LayerMask.NameToLayer("GameplaySurface");
        var shape = area.gameObject.AddComponent<BoxCollider2D>(); shape.size = new Vector2(20,10); shape.isTrigger = true;
        PrototypeBuilder.Set(area,"shape",shape);
        var frame = camera.gameObject.AddComponent<TopDownCameraFrame>(); PrototypeBuilder.Set(frame,"playableArea",area); frame.Refresh();
        var path = Child("WaypointPath2D",map).gameObject.AddComponent<WaypointPath>();
        var pathData = new SerializedObject(path); pathData.FindProperty("plane").enumValueIndex = (int)WaypointPlane.XY;
        var positions = new[]{new Vector2(9,0),new Vector2(6,0),new Vector2(4,1.5f),new Vector2(0,1.5f),new Vector2(-2,-1.5f),new Vector2(-6,-1.5f),new Vector2(-9,0)};
        var points = pathData.FindProperty("points"); points.arraySize = positions.Length;
        for(int i=0;i<positions.Length;i++) {var point=Child("Waypoint "+i.ToString("00"),path.transform);point.localPosition=positions[i];points.GetArrayElementAtIndex(i).objectReferenceValue=point;}
        pathData.ApplyModifiedPropertiesWithoutUndo();
        var route = Child("PathVisualization",map).gameObject;
        var line = route.AddComponent<LineRenderer>(); line.useWorldSpace=true; line.alignment=LineAlignment.View;
        line.widthMultiplier=.13f; line.numCapVertices=4; line.numCornerVertices=4; line.sortingOrder=10;
        const string materialPath=Art+"/Path2D.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,materialPath);}
        line.sharedMaterial=material; line.startColor=line.endColor=new Color(.9f,.73f,.39f);
        var view=route.AddComponent<PathVisualization>();PrototypeBuilder.Set(view,"path",path);var viewData=new SerializedObject(view);viewData.FindProperty("surfaceOffset").floatValue=0;viewData.ApplyModifiedPropertiesWithoutUndo();view.Refresh();
        var slotRoot=Child("PlacementSlots2D",map);
        var slotPositions=new[]{new Vector2(-7,2.8f),new Vector2(-3,3.2f),new Vector2(1,3.2f),new Vector2(6,2.8f),new Vector2(-7,-3.3f),new Vector2(-3,-3.3f),new Vector2(2,-3.3f),new Vector2(7,-2.5f)};
        var slots=new TowerPlacementSlot[slotPositions.Length];
        for(int i=0;i<slots.Length;i++) {var instance=(GameObject)PrefabUtility.InstantiatePrefab(slotPrefab,slotRoot);instance.name="Slot "+(i+1);instance.transform.localPosition=slotPositions[i];slots[i]=instance.GetComponent<TowerPlacementSlot>();}
        var enemyRoot=Child("EnemyRuntimeRoot",root);var towerRoot=Child("TowerRuntimeRoot",root);var skillRoot=Child("SkillEffectRoot",root);
        var spawnRoot=Child("EnemySpawnRoot",root);spawnRoot.position=path.GetPoint(0);
        Sprite("SpawnMarker",spawnRoot,circle,new Color(.95f,.8f,.3f),new Vector2(.85f,.85f),20);
        var baseAnchor=Child("BaseVisualAnchor",map);baseAnchor.position=path.GetPoint(path.Count-1);
        Sprite("BasePlaceholder",baseAnchor,square,new Color(.35f,.7f,.9f),new Vector2(.95f,.95f),40);
        var enemy=(GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab,enemyRoot);enemy.name="Movement Sample (not combat Enemy)";enemy.transform.position=path.GetPoint(0);
        enemy.AddComponent<EnemyPathFollower>();var sample=enemy.AddComponent<PathMovementSample2D>();PrototypeBuilder.Set(sample,"path",path);
        PrototypeBuilder.Set(sample,"definition",AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Defense/Data/BasicEnemy.asset"));
        var tower=(GameObject)PrefabUtility.InstantiatePrefab(towerPrefab,towerRoot);tower.name="Tower Visual Preview (not placed turret)";tower.transform.position=slots[0].PlacementPoint.position;
        // Reserved mount points only: Stage 2 effects and Stage 3 UI attach here later.
        Child("SkillAreaVisualAnchor",skillRoot);
        var hud=new GameObject("ScreenSpaceHUD (reserved)",typeof(RectTransform),typeof(Canvas));hud.transform.SetParent(root,false);
        hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;hud.GetComponent<Canvas>().sortingOrder=100;
        var config=map.gameObject.AddComponent<BattlefieldMap2D>();
        PrototypeBuilder.Set(config,"worldCamera",camera);PrototypeBuilder.Set(config,"playableArea",area);PrototypeBuilder.Set(config,"path",path);
        PrototypeBuilder.Set(config,"enemySpawnRoot",spawnRoot);PrototypeBuilder.Set(config,"enemyRuntimeRoot",enemyRoot);PrototypeBuilder.Set(config,"towerRuntimeRoot",towerRoot);PrototypeBuilder.Set(config,"skillEffectRoot",skillRoot);
        var data=new SerializedObject(config);var slotArray=data.FindProperty("placementSlots");slotArray.arraySize=slots.Length;
        for(int i=0;i<slots.Length;i++)slotArray.GetArrayElementAtIndex(i).objectReferenceValue=slots[i];data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        Debug.Log("TOPDOWN_STAGE1_AUTHORED: "+ScenePath);
    }
    private static Transform Child(string name,Transform parent) {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    private static SpriteRenderer Sprite(string name,Transform parent,Sprite sprite,Color color,Vector2 size,int order) {
        var view=Child(name,parent).gameObject.AddComponent<SpriteRenderer>();view.sprite=sprite;view.color=color;view.sortingOrder=order;
        view.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);return view;
    }
    private static GameObject VisualPrefab(string name,Sprite sprite,Color color,Vector2 size) {
        var root=new GameObject(name);
        try {
            var visual=Sprite("Visual",root.transform,sprite,color,size,40);
            // Anchors belong to the root rather than the scaled Sprite so future replacements retain positions.
            Child(name.StartsWith("Enemy") ? "HealthBarAnchor" : "ProjectileOrigin",root.transform).localPosition=new Vector3(0,.55f,0);
            return PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/"+name+".prefab");
        }finally {UnityEngine.Object.DestroyImmediate(root);}
    }
    private static GameObject SlotPrefab(Sprite square) {
        var root=new GameObject("PlacementSlot2D");
        try {
            root.layer=LayerMask.NameToLayer("PlacementSlot");var slot=root.AddComponent<TowerPlacementSlot>();
            var collider=root.AddComponent<BoxCollider2D>();collider.size=Vector2.one;collider.isTrigger=true;
            PrototypeBuilder.Set(slot,"placementPoint",Child("PlacementPoint",root.transform));
            var visual=Sprite("SlotVisual",root.transform,square,Color.white,Vector2.one,20);
            var view=visual.gameObject.AddComponent<PlacementSlotSpriteView>();PrototypeBuilder.Set(view,"slot",slot);PrototypeBuilder.Set(view,"appearance",visual);view.Refresh();
            return PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/PlacementSlot2D.prefab");
        }finally {UnityEngine.Object.DestroyImmediate(root);}
    }
    private static Sprite CreateCircle() {
        string path=Art+"/Circle2D.png";
        if(!File.Exists(path)) {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float a=Mathf.Clamp01(32-Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32)));texture.SetPixel(x,y,new Color(1,1,1,a));}
            texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=64;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
}
