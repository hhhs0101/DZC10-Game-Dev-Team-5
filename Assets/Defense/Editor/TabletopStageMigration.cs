using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Defense.Editor {
// One-time authoring tool. Updates existing assets in place to preserve GUID references.
public static class TabletopStageMigration {
    private const string Root = "Assets/Defense/";
    [MenuItem("Defense/Migrate Test Stage to 3D Stage 1")]
    public static void Run() {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before migration.");
        var scene = EditorSceneManager.OpenScene(Root+"Scenes/TestStage.unity");
        if(Object.FindFirstObjectByType<TabletopSurface>() != null) { Debug.Log("3D stage already authored; existing edits preserved."); return; }
        ConfigureLayers();
        if(!AssetDatabase.IsValidFolder(Root+"Art/Materials")) AssetDatabase.CreateFolder(Root+"Art","Materials");
        var tableMaterial = Material("Table",new Color(.26f,.17f,.1f));
        var legMaterial = Material("Legs",new Color(.12f,.1f,.09f));
        var enemyMaterial = Material("Enemy3D",new Color(.95f,.22f,.18f));
        var towerMaterial = Material("Tower3D",new Color(.16f,.58f,.9f));
        var slotMaterial = Material("Slot3D",Color.white);
        var pathMaterial = Material("Path3D",new Color(.85f,.73f,.44f));
        MigrateEnemy(enemyMaterial); MigrateTower(towerMaterial);
        var map = new GameObject("Battlefield").transform;
        var camera = Camera.main;
        camera.orthographic = false; camera.fieldOfView = 45; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
        camera.transform.position = new Vector3(10,19,-22); camera.transform.LookAt(new Vector3(0,0,0));
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.055f,.07f,.09f);
        var top = Primitive("Tabletop",PrimitiveType.Cube,map,new Vector3(0,-.25f,0),new Vector3(20,.5f,12),tableMaterial);
        top.layer = LayerMask.NameToLayer("GameplaySurface");
        var surface = top.AddComponent<TabletopSurface>();
        PrototypeBuilder.Set(surface,"worldCamera",camera);
        var surfaceData = new SerializedObject(surface);
        surfaceData.FindProperty("surfaceLayers").intValue = 1<<top.layer;
        surfaceData.FindProperty("placementLayers").intValue = 1<<LayerMask.NameToLayer("PlacementSlot");
        surfaceData.FindProperty("enemyLayers").intValue = 1<<LayerMask.NameToLayer("Enemy"); surfaceData.ApplyModifiedPropertiesWithoutUndo();
        foreach(float x in new[]{-8.5f,8.5f}) foreach(float z in new[]{-4.5f,4.5f}) {
            var leg = Primitive("Table Leg",PrimitiveType.Cube,map,new Vector3(x,-2.1f,z),new Vector3(.7f,3.7f,.7f),legMaterial);
            Object.DestroyImmediate(leg.GetComponent<Collider>());
        }
        var light = new GameObject("Table Light").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50,-30,0); light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f,.5f,.5f);
        var path = Object.FindFirstObjectByType<WaypointPath>(); path.transform.SetParent(map,true); path.transform.position = Vector3.zero;
        foreach(Transform child in path.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
        var points = new Transform[17];
        for(int i=0;i<points.Length;i++) {
            points[i] = new GameObject("Waypoint "+i.ToString("00")).transform;
            points[i].SetParent(path.transform,false); points[i].localPosition = new Vector3(8-i,0,2*Mathf.Sin(i*Mathf.PI/8));
        }
        var route = new SerializedObject(path); var array = route.FindProperty("points"); array.arraySize = points.Length;
        for(int i=0;i<points.Length;i++) array.GetArrayElementAtIndex(i).objectReferenceValue = points[i]; route.ApplyModifiedPropertiesWithoutUndo();
        var routeView = new GameObject("Path Visualization"); routeView.transform.SetParent(map,false);
        var line = routeView.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.widthMultiplier = .18f; line.sharedMaterial = pathMaterial;
        line.alignment = LineAlignment.TransformZ; line.transform.rotation = Quaternion.Euler(90,0,0); line.numCornerVertices = 5; line.numCapVertices = 4;
        var view = routeView.AddComponent<PathVisualization>(); PrototypeBuilder.Set(view,"path",path); view.Refresh();
        var baseHealth = Object.FindFirstObjectByType<BaseHealth>();
        Object.DestroyImmediate(baseHealth.GetComponent<SpriteRenderer>());
        baseHealth.transform.SetParent(map,true); baseHealth.transform.position = points.Last().position; baseHealth.transform.localScale = Vector3.one;
        Primitive("Base Visual",PrimitiveType.Cube,baseHealth.transform,new Vector3(0,.55f,0),new Vector3(1.2f,1.1f,1.2f),towerMaterial);
        var slots = Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None);
        foreach(var slot in slots) {
            var old = slot.transform.position;
            Object.DestroyImmediate(slot.GetComponent<BoxCollider2D>()); Object.DestroyImmediate(slot.GetComponent<SpriteRenderer>());
            slot.transform.position = new Vector3(old.x,0,old.y); slot.transform.localScale = Vector3.one; slot.gameObject.layer = LayerMask.NameToLayer("PlacementSlot");
            var collider = slot.gameObject.AddComponent<BoxCollider>(); collider.center = new Vector3(0,.025f,0); collider.size = new Vector3(1,.1f,1);
            var visual = Primitive("Slot Visual",PrimitiveType.Cube,slot.transform,new Vector3(0,.035f,0),new Vector3(.95f,.06f,.95f),slotMaterial);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var slotView = visual.AddComponent<PlacementSlotView>(); PrototypeBuilder.Set(slotView,"slot",slot); PrototypeBuilder.Set(slotView,"appearance",visual.GetComponent<Renderer>());
            slotView.Refresh();
        }
        slots[0].transform.parent.SetParent(map,true);
        GameObject.Find("Enemies").transform.SetParent(map,true);
        var placement = Object.FindFirstObjectByType<TurretPlacementController>(); PrototypeBuilder.Set(placement,"surface",surface);
        var casting = placement.GetComponent<SkillCastingController>() ?? placement.gameObject.AddComponent<SkillCastingController>(); PrototypeBuilder.Set(casting,"surface",surface);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets(); Debug.Log("TABLETOP_STAGE1_MIGRATED");
    }
    private static void ConfigureLayers() {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); var layers = tags.FindProperty("layers");
        foreach(string name in new[]{"GameplaySurface","PlacementSlot","Enemy"}) {
            if(LayerMask.NameToLayer(name) >= 0) continue;
            int index = Enumerable.Range(8,24).First(i=>string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
            layers.GetArrayElementAtIndex(index).stringValue = name; tags.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    private static Material Material(string name,Color color) {
        string path = Root+"Art/Materials/"+name+".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material != null) return material;
        material = new Material(Shader.Find("Standard")); material.color = color; material.SetFloat("_Glossiness",.15f);
        AssetDatabase.CreateAsset(material,path); return material;
    }
    private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material material) {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false); go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material; return go;
    }
    private static void MigrateEnemy(Material material) {
        string path = Root+"Prefabs/BasicEnemy.prefab"; var root = PrefabUtility.LoadPrefabContents(path);
        if(root.transform.Find("Visual") != null) { PrefabUtility.UnloadPrefabContents(root); return; }
        Object.DestroyImmediate(root.GetComponent<SpriteRenderer>()); root.transform.localScale = Vector3.one; root.layer = LayerMask.NameToLayer("Enemy");
        var visual = new GameObject("Visual").transform; visual.SetParent(root.transform,false);
        var sphere = Primitive("Sphere",PrimitiveType.Sphere,visual,new Vector3(0,.35f,0),Vector3.one*.7f,material); Object.DestroyImmediate(sphere.GetComponent<Collider>());
        var collider = root.AddComponent<SphereCollider>(); collider.center = new Vector3(0,.35f,0); collider.radius = .35f;
        var hp = new SerializedObject(root.GetComponent<EnemyHealthBar>()); hp.FindProperty("offset").vector3Value = new Vector3(0,1.05f,0); hp.FindProperty("size").vector2Value = new Vector2(.9f,.11f); hp.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root,path); PrefabUtility.UnloadPrefabContents(root);
    }
    private static void MigrateTower(Material material) {
        string path = Root+"Prefabs/BasicTurret.prefab"; var root = PrefabUtility.LoadPrefabContents(path);
        if(root.transform.Find("Visual") != null) { PrefabUtility.UnloadPrefabContents(root); return; }
        Object.DestroyImmediate(root.GetComponent<SpriteRenderer>()); root.transform.localScale = Vector3.one;
        var visual = new GameObject("Visual").transform; visual.SetParent(root.transform,false);
        var cuboid = Primitive("Cuboid",PrimitiveType.Cube,visual,new Vector3(0,.65f,0),new Vector3(.65f,1.3f,.65f),material); Object.DestroyImmediate(cuboid.GetComponent<Collider>());
        var muzzle = new GameObject("ProjectileOrigin").transform; muzzle.SetParent(visual,false); muzzle.localPosition = new Vector3(0,1.35f,0);
        PrefabUtility.SaveAsPrefabAsset(root,path); PrefabUtility.UnloadPrefabContents(root);
    }
}
}
