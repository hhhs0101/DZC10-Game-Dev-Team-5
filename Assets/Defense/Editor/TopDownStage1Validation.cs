using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
[InitializeOnLoad]
public static class TopDownStage1Validation {
    private const string Key="Defense.TopDownStage1Validation";
    private static int assertions,step;
    private static double started;
    private static float sampledTime;
    private static int sampledFrame;
    private static Vector3 sampledPosition;
    static TopDownStage1Validation() { if(SessionState.GetBool(Key,false)) Attach(); }
    private static T Find<T>() where T:UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Check(bool ok,string text) {if(!ok)throw new Exception(text);assertions++;Debug.Log("TOPDOWN PASS: "+text);}
    private static void Set(UnityEngine.Object target,string name,UnityEngine.Object value)=>PrototypeBuilder.Set(target,name,value);
    [MenuItem("Defense/2D Migration/Validate Stage 1 (Edit Mode)")]
    public static void RunEditMode() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=TopDownStage1Setup.ScenePath)throw new InvalidOperationException("Open TopDownStage1 first.");
        assertions=0;
        var map=Find<BattlefieldMap2D>();var camera=map.WorldCamera;var area=map.PlayableArea;var path=map.Path;
        Check(camera.orthographic && Vector3.Dot(camera.transform.forward,Vector3.forward)>.999f,"Native XY Orthographic camera has no perspective/isometric angle");
        var frame=camera.GetComponent<TopDownCameraFrame>();
        foreach(float aspect in new[]{16f/9,4f/3,9f/16}) {
            camera.aspect=aspect;frame.Refresh();var b=area.Bounds;
            foreach(var corner in new[]{new Vector3(b.min.x,b.min.y,0),new Vector3(b.min.x,b.max.y,0),new Vector3(b.max.x,b.min.y,0),new Vector3(b.max.x,b.max.y,0)}) {
                var v=camera.WorldToViewportPoint(corner);Check(v.x>0 && v.x<1 && v.y>0 && v.y<1 && v.z>0,"Entire playable area fits aspect "+aspect);
            }
        }
        camera.ResetAspect();frame.Refresh();
        Check(area.Contains(Vector2.zero) && !area.Contains(new Vector2(30,30)),"Logical Collider2D area accepts table and rejects off-table points");
        Check(path.Plane==WaypointPlane.XY && path.Count>=2 && path.GetPoint(0).x>path.GetPoint(path.Count-1).x,"Editable XY route runs from right spawn to left Base");
        var view=Find<PathVisualization>();view.Refresh();var line=view.GetComponent<LineRenderer>();
        Check(line.positionCount==path.Count && Enumerable.Range(0,path.Count).All(i=>line.GetPosition(i)==path.GetPoint(i)),"Path helper renders the exact configured waypoint sequence");
        Check(map.PlacementSlots.Count==8 && map.PlacementSlots.All(s=>area.Contains(s.PlacementPoint.position)),"All eight authored slots lie inside logical map bounds");
        Check(map.PlacementSlots.All(s=>s.GetComponent<Collider2D>()!=null && s.GetComponent<Collider>()==null),"Slots use Collider2D only");
        Check(map.PlacementSlots.All(s=> {var renderer=s.GetComponentInChildren<SpriteRenderer>();return renderer!=null && Vector2.Distance(s.GetComponent<Collider2D>().bounds.center,renderer.bounds.center)<.001f && renderer.bounds.size.x<=s.GetComponent<Collider2D>().bounds.size.x+.001f && renderer.bounds.size.y<=s.GetComponent<Collider2D>().bounds.size.y+.001f;}),"Slot click bounds cover the centered visual without stretching its aspect ratio");
        Check(map.PlacementSlots.All(s=>Enumerable.Range(1,path.Count-1).All(i=>DistanceToSegment(s.transform.position,path.GetPoint(i-1),path.GetPoint(i))>.8f)),"Authored placement slots do not overlap the travel path");
        var slot=map.PlacementSlots[0];var spriteView=slot.GetComponentInChildren<PlacementSlotSpriteView>();var original=spriteView.CurrentColor;
        slot.SetHovered(true);Check(spriteView.CurrentColor!=original && slot.IsHovered,"Highlight observes common TowerPlacementSlot state");slot.SetHovered(false);
        var temporary=new GameObject("Stage 1 verification fixture");
        try {
            var turret=temporary.AddComponent<BasicTurret>();turret.enabled=false;
            Check(slot.TryOccupy(turret) && spriteView.CurrentColor!=original,"Occupied presentation observes real slot occupancy without combat");
            spriteView.enabled=false;Check(slot.IsOccupied,"Disabling slot presentation leaves slot state intact");spriteView.enabled=true;
            var colliderObject=new GameObject("Polygon shape fixture");colliderObject.transform.SetParent(temporary.transform,false);colliderObject.transform.position=new Vector3(50,50,0);
            var polygon=colliderObject.AddComponent<PolygonCollider2D>();polygon.points=new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1)};
            var polygonArea=colliderObject.AddComponent<PlayableArea2D>();Set(polygonArea,"shape",polygon);Physics2D.SyncTransforms();
            Check(polygonArea.Contains(new Vector2(49.5f,49.5f)) && !polygonArea.Contains(new Vector2(50.8f,50.8f)),"PlayableArea supports nonrectangular map geometry independently of Sprite bounds");
            TestMovement(temporary.transform);
            var position=slot.transform.position;slot.transform.position+=new Vector3(.25f,.25f,0);Physics2D.SyncTransforms();
            Check(slot.GetComponent<Collider2D>().OverlapPoint(slot.transform.position),"Slot position is editable scene configuration with matching hit region");slot.transform.position=position;Physics2D.SyncTransforms();
            var data=new SerializedObject(map);var array=data.FindProperty("placementSlots");int count=array.arraySize;array.arraySize=count+1;array.GetArrayElementAtIndex(count).objectReferenceValue=slot;data.ApplyModifiedPropertiesWithoutUndo();
            Check(map.PlacementSlots.Count==count+1,"Slot count is serialized map configuration rather than runtime fixed layout");array.arraySize=count;data.ApplyModifiedPropertiesWithoutUndo();
        } finally {UnityEngine.Object.DestroyImmediate(temporary);slot.SetHovered(false);spriteView.Refresh();}
        Check(Find<StageInitializer>()==null && Find<TurretPlacementController>()==null && Find<SkillCastingController>()==null && Find<Collider>()==null,"Isolated scene has no session bootstrap or active 3D input/physics components");
        Check(Find<PathMovementSample2D>().transform.Find("HealthBarAnchor")!=null && map.TowerRuntimeRoot.GetChild(0).Find("ProjectileOrigin")!=null,"Portable feedback and future projectile anchors are independent from scaled visuals");
        Check(Find<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay,"Reserved HUD uses screen space");
        // Inspect old serialized scene without changing routing or starting its persistence bootstrap.
        var old=EditorSceneManager.OpenScene("Assets/Defense/Scenes/TestStage.unity",OpenSceneMode.Additive);
        try {Check(old.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TabletopSurface>(true)).Any(),"Original 3D scene still loads with its TabletopSurface");}
        finally {EditorSceneManager.CloseScene(old,true);}
        Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/2D_Stage1_EditMode_Result.txt","PASS: "+assertions+" assertions");
        Debug.Log("TOPDOWN_STAGE1_EDIT_PASSED "+assertions);
    }
    private static float DistanceToSegment(Vector2 p,Vector2 a,Vector2 b) {var d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
    private static void TestMovement(Transform root) {
        var route=new GameObject("Movement route").AddComponent<WaypointPath>();route.transform.SetParent(root,false);route.transform.position=new Vector3(0,0,7);
        var data=new SerializedObject(route);data.FindProperty("plane").enumValueIndex=(int)WaypointPlane.XY;var points=data.FindProperty("points");points.arraySize=4;
        var coordinates=new[]{new Vector3(2,0,90),new Vector3(1,0,100),new Vector3(1,2,-5),new Vector3(-2,2,3)};
        for(int i=0;i<4;i++){var t=new GameObject("Point "+i).transform;t.SetParent(route.transform,false);t.position=coordinates[i];points.GetArrayElementAtIndex(i).objectReferenceValue=t;}data.ApplyModifiedPropertiesWithoutUndo();
        var follower=new GameObject("XY follower").AddComponent<EnemyPathFollower>();follower.transform.SetParent(root,false);int ends=0;follower.ReachedEnd+=()=>ends++;follower.Initialize(route,2);
        Check(follower.transform.position==new Vector3(2,0,7),"XY path fixes gameplay Z independently of waypoint visual depth");
        follower.Advance(1);Check(follower.transform.position==new Vector3(1,1,7),"Existing movement carries unused distance around XY corners at configured speed");
        var slow=new object();follower.Modifiers.SetSlow(slow,.5f);follower.Advance(1);Check(follower.transform.position==new Vector3(1,2,7),"Same MovementModifiers change effective XY movement speed");
        follower.Modifiers.Remove(slow);follower.Advance(100);follower.Advance(1);Check(ends==1 && follower.transform.position==new Vector3(-2,2,7),"Route end fires exactly once and clamps to final XY point");
        var legacy=new GameObject("Default legacy path").AddComponent<WaypointPath>();legacy.transform.SetParent(root,false);Check(legacy.Plane==WaypointPlane.XZ && legacy.Constrain(new Vector3(2,5,3))==new Vector3(2,0,3),"Old paths retain XZ serialized default");
    }
    [MenuItem("Defense/2D Migration/Validate Stage 1 (Play Mode)")]
    public static void RunPlayMode() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        EditorSceneManager.OpenScene(TopDownStage1Setup.ScenePath);SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach() {started=EditorApplication.timeSinceStartup;step=0;assertions=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string message,string trace,LogType type) {
        if((type==LogType.Error || type==LogType.Exception) && !trace.Contains("UnityEditor.Search.SearchDatabase")) Finish(false,message);
    }
    private static void Tick() {
        if(!SessionState.GetBool(Key,false))return;
        try {
            if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Play Mode sample timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var sample=Find<PathMovementSample2D>();if(sample==null)return;var follower=sample.GetComponent<EnemyPathFollower>();
            if(step==0 && Time.frameCount>3) {sampledPosition=sample.transform.position;sampledTime=Time.time;sampledFrame=Time.frameCount;step=1;}
            else if(step==1 && Time.time-sampledTime>=.7f) {
                Check(Time.frameCount>sampledFrame && sample.transform.position.x<sampledPosition.x,"Actual Play Mode frames advance and sample moves right-to-left");
                Check(Mathf.Abs(Vector3.Distance(sampledPosition,sample.transform.position)-follower.EffectiveMovementSpeed*(Time.time-sampledTime))<.05f,"Actual sample movement uses existing EnemyDefinition speed");
                TabletopStageValidation.CapturePreview(Path.GetFullPath("Documentation/Report/2D_Stage1_Preview.png"));
                Find<PathVisualization>().enabled=false;sample.transform.Find("Visual").GetComponent<SpriteRenderer>().enabled=false;
                sampledPosition=sample.transform.position;sampledTime=Time.time;step=2;
            } else if(step==2 && Time.time-sampledTime>=.7f) {
                Check(sample.transform.position!=sampledPosition,"Disabling route and Enemy visual helpers does not stop movement");
                Find<PathVisualization>().enabled=true;sample.transform.Find("Visual").GetComponent<SpriteRenderer>().enabled=true;step=3;
            } else if(step==3 && sample.ReachedEnd) {
                var map=Find<BattlefieldMap2D>();Check(sample.transform.position==map.Path.GetPoint(map.Path.Count-1),"Actual sample reaches configured left-side endpoint");
                Check(PlayerSession.Currency==null && PlayerSession.Presets==null,"Stage 1 Play Mode never initializes persistent player services");Finish(true,assertions+" assertions");
            }
        }catch(Exception e){Finish(false,e.ToString());}
    }
    private static void Finish(bool success,string message) {
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
        Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/2D_Stage1_PlayMode_Result.txt",(success?"PASS: ":"FAIL: ")+message);
        if(success)Debug.Log("TOPDOWN_STAGE1_PLAY_PASSED "+message);else Debug.LogError("TOPDOWN_STAGE1_PLAY_FAILED "+message);
        EditorApplication.isPlaying=false;
    }
}
}
