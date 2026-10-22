using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
[InitializeOnLoad]
public static class SkillEllipseValidation {
    private const string Key="Defense.SkillEllipse.Validation";
    private static int phase,checks,frame;
    private static double started;
    private static UnityEngine.Object[] oldMarkers;
    private static GameFlow oldFlow;
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static EnemyRegistry Registry=>Find<EnemyRegistry>();
    private static GameFlow Flow=>Find<GameFlow>();
    private static SkillDefinition Toy=>AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Defense/Data/ToyThrowSkill.asset");
    private static SkillDefinition Slow=>AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Defense/Data/SlowAreaSkill.asset");
    static SkillEllipseValidation(){if(SessionState.GetBool(Key,false))Attach();}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("ELLIPSE PASS: "+message);}
    // Inspect actual renderer geometry from measured pixel bounds; never trust SpriteRenderer.bounds as a hit test.
    public static void AssertMarker(SkillAreaVisual view,Vector3 center,float radius,Action<bool,string> check) {
        var profile=Toy.RangeProfile;var renderer=view.MarkerRenderer;
        check(renderer!=null&&renderer.enabled&&renderer.sprite==profile.MarkerSprite&&!view.GetComponent<LineRenderer>().enabled,"Exactly one Sprite preview is visible; the old Skill circle is hidden");
        var local=(profile.BrightRimPixels.center-renderer.sprite.rect.position-renderer.sprite.pivot)/renderer.sprite.pixelsPerUnit;
        var c=renderer.transform.TransformPoint(local);
        var x=renderer.transform.TransformVector(Vector3.right*profile.BrightRimPixels.width/(2*renderer.sprite.pixelsPerUnit));
        var y=renderer.transform.TransformVector(Vector3.up*profile.BrightRimPixels.height/(2*renderer.sprite.pixelsPerUnit));
        check(Vector3.Distance(c,center)<.0001f&&Vector3.Distance(x,Vector3.right*radius)<.0001f&&Vector3.Distance(y,Vector3.up*radius*profile.AspectRatio)<.0001f,"Measured bright rim center and both world semi-axes match the committed ellipse");
        check(renderer.sortingOrder==30&&renderer.GetComponent<Collider2D>()==null&&renderer.GetComponent<UnityEngine.UI.Graphic>()==null,"Marker is sorted below actors and never intercepts world/UI input");
    }
    private static void Set(UnityEngine.Object target,string name,float value){var so=new SerializedObject(target);var field=so.FindProperty(name);if(field.propertyType==SerializedPropertyType.Integer)field.intValue=(int)value;else field.floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Models(){
        var profile=Toy.RangeProfile;
        Check(profile!=null&&Slow.RangeProfile==profile&&profile.BrightRimPixels==new Rect(128,176,1518,400)&&Mathf.Abs(profile.AspectRatio-400f/1518)<.000001f,"Both existing skills share the measured bright-rim profile");
        var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(profile.MarkerSprite));
        Check(profile.MarkerSprite.texture.width==1774&&profile.MarkerSprite.texture.height==887&&importer.alphaIsTransparency&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Original texture dimensions/transparency preserved without compression or mipmaps");
        foreach(float radius in new[]{.01f,1.5f,3,6}){
            var ellipse=profile.CreateFootprint(new Vector2(1,-2),radius);Check(ellipse.Contains(ellipse.Center),"Transparent center counts as inside at radius "+radius);
            for(int i=0;i<16;i++){
                var angle=i*Mathf.PI/8;var p=ellipse.Boundary(angle);
                Check(ellipse.Contains(p)&&ellipse.Contains(ellipse.Center+(p-ellipse.Center)*.99f)&&!ellipse.Contains(ellipse.Center+(p-ellipse.Center)*1.01f),"Inclusive boundary and strict inside/outside at angle "+i+", radius "+radius);
            }
            Check(!ellipse.Contains(ellipse.Center+new Vector2(radius*.9f,ellipse.RadiusY*.9f)),"Bounding-rectangle corner is outside ellipse");
            Check(!ellipse.Contains(ellipse.Center+Vector2.up*(radius*.75f)),"Enemy inside old circle but vertically outside ellipse is excluded");
            Check(!ellipse.Contains(new Vector2(float.NaN,0)),"Nonfinite query point is rejected");
        }
        Check(!new EllipseFootprint(Vector2.zero,0,1).Contains(Vector2.zero)&&!new EllipseFootprint(Vector2.zero,1,0).Contains(Vector2.zero)&&!new EllipseFootprint(Vector2.zero,float.NaN,1).Contains(Vector2.zero)&&!new EllipseFootprint(Vector2.zero,1,float.PositiveInfinity).Contains(Vector2.zero),"Degenerate/nonfinite footprint never hits");
    }
    private static void Visuals(){
        var root=new GameObject("Distorted presentation parent");root.transform.position=new Vector3(5,7,3);root.transform.rotation=Quaternion.Euler(0,0,35);root.transform.localScale=new Vector3(2,.5f,1);
        var owner=new GameObject("Ellipse visual fixture");owner.transform.SetParent(root.transform,false);var view=owner.AddComponent<SkillAreaVisual>();
        try{
            foreach(float radius in new[]{1.5f,3,6}){
                var definition=UnityEngine.Object.Instantiate(Toy);Set(definition,"radius",radius);
                view.ShowSkill(new Vector3(1,-1,0),definition,Color.white);AssertMarker(view,new Vector3(1,-1,0),radius,Check);
                Check(Mathf.Abs(view.MarkerRenderer.transform.localScale.x-view.MarkerRenderer.transform.localScale.y)<.0001f,"Default art scaling is uniform and retains original rim aspect");
                UnityEngine.Object.DestroyImmediate(definition);
            }
            var adjusted=UnityEngine.Object.Instantiate(Toy.RangeProfile);Set(adjusted,"ellipseAspectRatio",.5f);
            var custom=UnityEngine.Object.Instantiate(Toy);var data=new SerializedObject(custom);data.FindProperty("rangeProfile").objectReferenceValue=adjusted;data.ApplyModifiedPropertiesWithoutUndo();
            view.ShowSkill(Vector3.zero,custom,Color.white);var height=adjusted.BrightRimPixels.height/custom.RangeProfile.MarkerSprite.pixelsPerUnit*view.MarkerRenderer.transform.lossyScale.y;
            Check(Mathf.Abs(height-2*custom.Radius*.5f)<.0001f&&custom.CreateFootprint(Vector2.zero).RadiusY==custom.Radius*.5f,"Inspector aspect tuning changes visual and collision together");
            UnityEngine.Object.DestroyImmediate(custom);UnityEngine.Object.DestroyImmediate(adjusted);
            view.Show(Vector3.zero,2,Color.cyan,WaypointPlane.XY);var line=view.GetComponent<LineRenderer>();
            Check(!view.MarkerRenderer.enabled&&line.enabled&&Enumerable.Range(0,line.positionCount).All(i=>Mathf.Abs(((Vector2)line.GetPosition(i)).sqrMagnitude-4)<.001f),"Unrelated Tower inspection API remains a circle");
            view.Hide();Check(!view.IsVisible,"Hide disables both presentation paths");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    private static Enemy Spawn(Vector3 p,EnemyDefinition definition){var e=Find<EnemySpawner>().Spawn(definition);e.Movement.Stop();e.transform.position=p;Registry.NotifyMoved(e);return e;}
    private static void ClearActors(){foreach(var e in Registry.Enemies.ToArray())UnityEngine.Object.DestroyImmediate(e.gameObject);}
    private static EnemyDefinition EnemyData(){var data=UnityEngine.Object.Instantiate(Find<TopDownInteractionSandbox>().TestStage.Enemy);Set(data,"health",500);Set(data,"resourceReward",0);Set(data,"candyDropChance",0);return data;}
    private static void Damage(){
        var enemy=EnemyData();try{
            foreach(float radius in new[]{1.5f,3,6}){
                var skill=UnityEngine.Object.Instantiate(Toy);Set(skill,"radius",radius);var center=new Vector2(1,-1);float y=radius*skill.RangeProfile.AspectRatio;
                var offsets=new[]{Vector2.zero,Vector2.right*radius,Vector2.up*y,new Vector2(radius*.5f,y*.5f),Vector2.right*(radius+.01f),Vector2.up*(y+.01f),new Vector2(radius*.9f,y*.9f),Vector2.up*(radius*.75f)};
                var actors=offsets.Select((p,i)=>Spawn(new Vector3(center.x+p.x,center.y+p.y,i%2==0?9:-9),enemy)).ToArray();
                var child=new GameObject("Extra enemy hitbox");child.transform.SetParent(actors[0].transform,false);child.AddComponent<CircleCollider2D>();
                skill.Effect.Apply(skill,new SkillCastContext(center,Registry,Flow));
                for(int i=0;i<actors.Length;i++)Check(actors[i].CurrentHealth==500-(i<4?skill.Damage:0),"Actual Toy Throw exact inclusion, no multi-collider duplication: radius="+radius+", point="+i);
                ClearActors();UnityEngine.Object.DestroyImmediate(skill);
            }
        }finally{ClearActors();UnityEngine.Object.DestroyImmediate(enemy);}
    }
    private static PersistentSlowArea Area(Vector3 center){var before=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None);Slow.Effect.Apply(Slow,new SkillCastContext(center,Registry,Flow));return UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None).Except(before).Single();}
    private static void Slowing(){
        var data=EnemyData();float y=Slow.Radius*Slow.RangeProfile.AspectRatio;var enemy=Spawn(Vector3.zero,data);var boost=new object();enemy.Movement.Modifiers.SetSpeedBoost(boost,1.2f);var speed=enemy.Movement.BaseMovementSpeed;
        var a=Area(Vector3.zero);var b=Area(Vector3.up*y);AssertMarker(a.GetComponentInChildren<SkillAreaVisual>(),Vector3.zero,Slow.Radius,Check);
        Check(enemy.Movement.Modifiers.SourceCount==3&&Mathf.Abs(enemy.Movement.EffectiveMovementSpeed-speed*.6f)<.0001f,"Overlapping ellipse slows use strongest slow once and retain unrelated speed boost");
        enemy.transform.position=Vector3.up*(y*1.5f);Registry.NotifyMoved(enemy);
        Check(a.AffectedCount==0&&b.AffectedCount==1&&enemy.Movement.Modifiers.SourceCount==2,"Vertical exit removes only the exited ellipse's source immediately");
        enemy.transform.position=Vector3.up*(y*2+.01f);Registry.NotifyMoved(enemy);
        Check(a.AffectedCount==0&&b.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==1&&Mathf.Abs(enemy.Movement.EffectiveMovementSpeed-speed*1.2f)<.0001f,"Inside old circle but outside both ellipses preserves only unrelated boost");
        enemy.transform.position=Vector3.zero;Registry.NotifyMoved(enemy);a.Advance(Slow.ActiveDuration+.01f);
        Check(!a.IsActive&&a.VisualOpacity>0&&a.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==2,"Expiry removes gameplay slow before glow fade; other ellipse/boost remain");
        var opacity=a.VisualOpacity;Flow.TogglePause();a.Advance(1);Check(a.VisualOpacity==opacity,"Pause freezes glow fade");Flow.TogglePause();a.Advance(Slow.FadeDuration);
        Check(!a.GetComponentInChildren<SkillAreaVisual>().IsVisible,"Completed fade hides marker immediately");
        UnityEngine.Object.DestroyImmediate(b.gameObject);Check(enemy.Movement.Modifiers.SourceCount==1,"Destroying final area removes its own modifier only");
        b=Area(Vector3.zero);enemy.ReceiveDamage(10000);Check(b.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==0,"Enemy death removes membership and all movement sources");UnityEngine.Object.DestroyImmediate(b.gameObject);
        enemy=Spawn(Vector3.zero,data);b=Area(Vector3.zero);enemy.gameObject.SetActive(false);Check(b.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==0,"Enemy disable/despawn removes ellipse slow immediately");
        UnityEngine.Object.DestroyImmediate(enemy.gameObject);UnityEngine.Object.DestroyImmediate(b.gameObject);UnityEngine.Object.DestroyImmediate(data);
    }
    private static void DelayedImpact(){
        var hand=Find<GameplayHand>();var casting=Find<SkillCastingController>();var placement=Find<TurretPlacementController>();var energy=Find<ElixirSystem>();
        var original=Find<TopDownInteractionSandbox>().Deck;var toy=original.Slots.OfType<SkillCardDefinition>().Single(c=>c.Skill.Effect is CircularDamageSkill);
        var card=UnityEngine.Object.Instantiate(toy);var definition=UnityEngine.Object.Instantiate(toy.Skill);Set(definition,"castingTime",.2f);Set(definition,"travelTime",.3f);
        var data=new SerializedObject(card);data.FindProperty("skill").objectReferenceValue=definition;data.ApplyModifiedPropertiesWithoutUndo();
        var enemyData=EnemyData();var enemy=Spawn(Vector3.zero,enemyData);
        try{
            hand.Initialize(new PlayerDeck(original.Slots.Select(c=>c==toy?card:c).ToArray()),placement,Flow);
            for(int run=0;run<2;run++){
                hand.CancelSelection();for(int i=0;i<6&&!hand.Cycle.Hand.Any(c=>c.Definition==card);i++)hand.Cycle.Use(hand.Cycle.Hand[0]);
                energy.Initialize(Flow);Check(hand.Select(hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card))&&casting.BeginAim(Vector2.zero,false),"Delayed ellipse uses existing selection/aim commitment");
                Check(casting.ReleaseAim(Vector2.zero,false)==CardUseResult.Success&&energy.Current==0&&casting.PendingCastCount==1,"Delayed cast pays and cycles once before impact");
                enemy.transform.position=run==0?Vector3.up*(definition.Radius*.75f):Vector3.up*(definition.Radius*definition.RangeProfile.AspectRatio*.5f);
                casting.Advance(.2f);Check(enemy.CurrentHealth==500,"Casting phase applies no early damage");casting.Advance(.31f);casting.Advance(1);
                Check(enemy.CurrentHealth==500-(run==0?0:definition.Damage)&&casting.PendingCastCount==0&&energy.Current==0,"Impact checks current enemy position against the committed target ellipse, exactly once");
            }
        }finally{casting.CancelAim();hand.Initialize(original,placement,Flow);energy.Initialize(Flow);ClearActors();UnityEngine.Object.DestroyImmediate(enemyData);UnityEngine.Object.DestroyImmediate(card);UnityEngine.Object.DestroyImmediate(definition);}
    }
    private static void TerminalCleanup(){
        var data=EnemyData();
        foreach(var state in new[]{GameplayState.GameOver,GameplayState.Victory,GameplayState.ConfigurationError}){
            var fixture=new GameObject("Terminal cleanup fixture");var localFlow=fixture.AddComponent<GameFlow>();var enemy=Spawn(Vector3.zero,data);
            var previous=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None);Slow.Effect.Apply(Slow,new SkillCastContext(Vector3.zero,Registry,localFlow));
            var area=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None).Except(previous).Single();var visual=area.GetComponentInChildren<SkillAreaVisual>();
            Check(visual.IsVisible&&enemy.Movement.Modifiers.SourceCount==1,"Terminal fixture starts with a visible active ellipse");
            if(state==GameplayState.GameOver)localFlow.GameOver();
            else if(state==GameplayState.Victory)typeof(GameFlow).GetMethod("TryVictory",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(localFlow,null);
            else localFlow.FailConfiguration("Synthetic test configuration error");
            Check(!visual.IsVisible&&!area.IsActive&&enemy.Movement.Modifiers.SourceCount==0,"Terminal state clears transient glow and its slow immediately: "+state);
            UnityEngine.Object.DestroyImmediate(fixture);ClearActors();
        }
        UnityEngine.Object.DestroyImmediate(data);
    }
    public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;SessionState.SetBool(Key,true);EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);Attach();EditorApplication.isPlaying=true;}
    private static void Attach(){phase=checks=0;frame=-1;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);}
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Ellipse validation timed out at "+phase);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||GameObject.Find("Hand Card 1")==null)return;
        if(phase==0){Find<FixedIntervalSpawner>().enabled=false;ClearActors();frame=Time.frameCount;phase=1;}
        else if(phase==1&&Time.frameCount>frame+2){Check(Time.frameCount>frame+2,"Actual Play Mode frames advance");Models();Visuals();Damage();Slowing();DelayedImpact();TerminalCleanup();
            Check(PlayerSession.Currency==null&&PlayerSession.Presets==null,"Focused geometry/effect tests never initialize persistent services");
            oldMarkers=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.name=="Skill Glow Marker").Cast<UnityEngine.Object>().ToArray();oldFlow=Flow;SceneManager.LoadScene(TopDownStage2Setup.ScenePath);phase=2;}
        else if(phase==2&&Flow!=oldFlow){Find<FixedIntervalSpawner>().enabled=false;Check(oldMarkers.All(o=>o==null),"Scene reload destroys all captured preview/slow marker roots without orphans");Finish(true,checks+" geometry/visual/damage/slow assertions");}
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Finish(bool pass,string message){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Skill_Ellipse_Result.txt",(pass?"PASS: ":"FAIL: ")+message);if(pass)Debug.Log("SKILL_ELLIPSE_PASSED "+message);else Debug.LogError("SKILL_ELLIPSE_FAILED "+message);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
    public static string CapturePreview(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits before capture.");
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return "NOT_TESTED: Graphics device is Null.";
        var previous=EditorSceneManager.GetSceneManagerSetup();
        try{
            EditorSceneManager.OpenScene(TopDownStage3Setup.ScenePath);var map=Find<BattlefieldMap2D>();
            var first=new GameObject("Toy preview fixture").AddComponent<SkillAreaVisual>();first.ShowSkill(new Vector3(-3,-1.7f,0),Toy,Color.white);
            var second=new GameObject("Slow preview fixture").AddComponent<SkillAreaVisual>();second.ShowSkill(new Vector3(2.5f,-1.7f,0),Slow,Color.white);
            var circle=new GameObject("Tower circle comparison").AddComponent<SkillAreaVisual>();circle.Show(new Vector3(0,1,0),1,Color.cyan,WaypointPlane.XY);
            AssertMarker(first,new Vector3(-3,-1.7f,0),Toy.Radius,(v,m)=>{if(!v)throw new Exception(m);});AssertMarker(second,new Vector3(2.5f,-1.7f,0),Slow.Radius,(v,m)=>{if(!v)throw new Exception(m);});
            Directory.CreateDirectory("Documentation/Report/Skill_Elliptical_Range");BattlefieldPreview.Capture("Documentation/Report/Skill_Elliptical_Range/Unity_Rendered_Preview.png");
            UnityEngine.Object.DestroyImmediate(first.gameObject);UnityEngine.Object.DestroyImmediate(second.gameObject);UnityEngine.Object.DestroyImmediate(circle.gameObject);
            return "PASS: Unity camera rendered two calibrated ellipses and unchanged Tower circle on the current kitchen map; camera/layout not saved or changed.";
        }finally{EditorSceneManager.RestoreSceneManagerSetup(previous);}
    }
}
}
