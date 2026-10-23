using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
[InitializeOnLoad]
public static class KitchenMap2DValidation {
    private const string Key="Defense.KitchenMap.Validation";
    private static int checks,phase;
    private static double started;
    private static float sampleTime;
    private static Enemy probe;
    private static Vector3 samplePosition;
    private static Texture2D mask;
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static BattlefieldMap2D Map=>Find<BattlefieldMap2D>();
    static KitchenMap2DValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run(){
        var path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
        if(string.IsNullOrWhiteSpace(path)||Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Use an isolated deck/currency test profile.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        EditorSceneManager.OpenScene(KitchenMap2DSetup.ScenePath);SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){checks=phase=0;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string message,string trace,LogType kind){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)Finish(false,message+"\n"+trace);}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("KITCHEN PASS: "+message);}
    private static SpriteRenderer Route=>GameObject.Find("GinghamPathSprite").GetComponent<SpriteRenderer>();
    private static bool OnCloth(Vector3 world){var local=Route.transform.InverseTransformPoint(world);int x=Mathf.RoundToInt(local.x*80+836),y=Mathf.RoundToInt(local.y*80+470.5f);return x>=0&&x<mask.width&&y>=0&&y<mask.height&&mask.GetPixel(x,y).a>.75f;}
    private static bool Visible(Bounds b){return new[]{b.min,b.max,new Vector3(b.min.x,b.max.y,0),new Vector3(b.max.x,b.min.y,0)}.All(p=>{var v=Map.WorldCamera.WorldToViewportPoint(p);return v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1;});}
    private static void Inspect(){
        mask=new Texture2D(2,2);mask.LoadImage(File.ReadAllBytes(KitchenMap2DSetup.ArtRoot+"/GinghamRoute.png"));
        var background=GameObject.Find("KitchenBackgroundSprite").GetComponent<SpriteRenderer>();
        foreach(var file in new[]{"KitchenInterior.png","GinghamRoute.png"}){var importer=(TextureImporter)AssetImporter.GetAtPath(KitchenMap2DSetup.ArtRoot+"/"+file);Check(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.filterMode==FilterMode.Point&&importer.spritePixelsPerUnit==80&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Original canvas imports consistently as crisp Sprite: "+file);}
        Check(background.sprite.rect.size==new Vector2(1672,1024)&&Route.sprite.rect.size==new Vector2(1672,941)&&background.color==Color.white&&Route.color==Color.white,"Matched full image canvases and untinted SpriteRenderers");
        Check(background.sortingOrder==0&&Route.sortingOrder==10&&background.GetComponent<Collider2D>()==null&&Route.GetComponent<Collider2D>()==null,"Background and transparent cloth are separate sorted art, never gameplay colliders");
        Check(Mathf.Approximately(background.bounds.size.x/background.bounds.size.y,1672f/1024)&&Mathf.Approximately(Route.bounds.size.x/Route.bounds.size.y,1672f/941)&&Visible(Route.bounds),"Orthographic camera covers scene and displays Route without clipping");
        Check(mask.GetPixel(0,0).a==0&&Map.WorldCamera.orthographic&&Map.Path.Plane==WaypointPlane.XY,"Cloth transparency is preserved on native XY world");
        var pathData=new SerializedObject(Map.Path);var refs=pathData.FindProperty("points");
        Check(refs.GetArrayElementAtIndex(0).objectReferenceValue==Map.EnemySpawnRoot&&refs.GetArrayElementAtIndex(Map.Path.Count-1).objectReferenceValue==Map.BaseDestination,"Spawn and Base anchors ARE editable first/last waypoints");
        Check(Map.Path.Count==48&&Map.Path.GetPoint(0).x<Map.Path.GetPoint(Map.Path.Count-1).x&&Map.Path.GetPoint(0)==Map.EnemySpawnRoot.position&&Map.Path.GetPoint(Map.Path.Count-1)==Map.BaseDestination.position,"Full replacement 48-point route runs refrigerator left to right Base");
        float length=0;int samples=0;
        for(int i=1;i<Map.Path.Count;i++){var a=Map.Path.GetPoint(i-1);var b=Map.Path.GetPoint(i);length+=Vector3.Distance(a,b);int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.04f);for(int n=0;n<=count;n++){var p=Vector3.Lerp(a,b,(float)n/count);if(!OnCloth(p)||!Map.PlayableArea.Contains(p)||GameplayPointer.IsOverUI(Map.WorldCamera.WorldToScreenPoint(p)))throw new Exception("Route crosses transparent artwork, floor boundary or HUD at segment "+i);samples++;}}
        Check(length>27&&samples>700,"Dense full-centerline samples stay on visible cloth and floor, clear of screen UI");
        foreach(var slot in Map.PlacementSlots){var b=slot.GetComponent<Collider2D>().bounds;bool clear=true;for(int x=0;x<=10;x++)for(int y=0;y<=10;y++){var p=new Vector3(Mathf.Lerp(b.min.x,b.max.x,x/10f),Mathf.Lerp(b.min.y,b.max.y,y/10f),0);clear&=!OnCloth(p)&&Map.PlayableArea.Contains(p);}Check(clear,"Independent placement slot is inside floor and off fabric: "+slot.name);}
        Check(!Map.PlayableArea.Contains(new Vector2(0,4))&&Map.PlayableArea.Contains(Vector2.zero),"Furniture is decorative and floor validation excludes cabinet row");
        Check(!GameObject.Find("MapRoot").transform.Find("PathVisualization").gameObject.activeSelf,"Old path line remains independently toggleable and hidden by default");
    }
    private static void Combat(){
        int before=Find<BaseHealth>().Current,changes=0;Action<int> changed=hp=>changes++;Find<BaseHealth>().Changed+=changed;
        bool allOnCloth=true;int safety=0;
        while(probe.IsAlive&&safety++<3000){allOnCloth&=OnCloth(probe.transform.position);probe.Movement.Advance(.03f);}
        probe.Movement.Advance(100);
        Find<BaseHealth>().Changed-=changed;
        Check(allOnCloth&&safety<3000,"Actual EnemyPathFollower traverses the whole cloth without crossing its transparent edges");
        Check(Find<BaseHealth>().Current==before-probe.Definition.BaseDamage&&changes==1&&!Find<EnemyRegistry>().Enemies.Contains(probe),"Right-side arrival damages existing Base once and retires actor");
        var hand=Find<GameplayHand>();int towerIndex=hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition is TowerCardDefinition);Check(towerIndex>=0&&hand.Select(towerIndex),"Real runtime Hand selects a Tower");
        Find<ResourceWallet>().Initialize(10000);Find<ElixirSystem>().Advance(100);var slot=Map.PlacementSlots[0];
        Check(Find<TurretPlacementController>().ProcessPointer(Map.WorldCamera.WorldToScreenPoint(slot.PlacementPoint.position),true)&&slot.IsOccupied,"Production screen/Physics2D/card path places Tower on new configured slot");
        slot.GetComponentInChildren<Turret>().enabled=false;
        var toy=PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>().Single(c=>c.Skill.Effect is CircularDamageSkill).Skill;
        var slow=PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>().Single(c=>c.Skill.Effect is SlowAreaSkill).Skill;
        var enemy=Find<EnemySpawner>().Spawn(probe.Definition);enemy.Movement.Stop();enemy.transform.position=Map.Path.GetPoint(20);Find<EnemyRegistry>().NotifyMoved(enemy);
        var context=new SkillCastContext(enemy.transform.position,Find<EnemyRegistry>(),Find<GameFlow>());slow.Effect.Apply(slow,context);var area=Find<PersistentSlowArea>();
        Check(area!=null&&enemy.Movement.EffectiveMovementSpeed==enemy.Movement.BaseMovementSpeed*.5f,"Existing Slow affects an actor on the kitchen cloth");
        SkillEllipseValidation.AssertMarker(area.GetComponentInChildren<SkillAreaVisual>(),area.transform.position,slow.Radius,Check);
        area.Advance(slow.ActiveDuration+.01f);Check(enemy.Movement.Modifiers.SourceCount==0,"Expiry cleans slow source on this map");
        toy.Effect.Apply(toy,context);Check(!enemy.IsAlive&&Find<EnemyRegistry>().AliveEnemyCount==0,"Existing Toy Throw kills and unregisters cloth actor");
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>90)throw new Exception("Kitchen validation timed out.");
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||Map==null||GameObject.Find("Hand Card 1")==null)return;
        if(phase==0){Find<FixedIntervalSpawner>().enabled=false;Inspect();probe=Find<EnemySpawner>().Spawn(AssetDatabase.LoadAssetAtPath<EnemyDefinition>(VeggieEnemySetup.DataRoot+"/MushroomEnemy.asset"));sampleTime=Time.time;samplePosition=probe.transform.position;phase=1;}
        else if(phase==1&&Time.time-sampleTime>.15f){Check(Vector3.Distance(samplePosition,probe.transform.position)>.1f,"Actual Play Mode frames advance Enemy on kitchen route");Combat();Finish(true,checks+" focused assertions");}
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Finish(bool pass,string message){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;if(mask!=null)UnityEngine.Object.DestroyImmediate(mask);Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Kitchen_2D_Validation_Result.txt",(pass?"PASS: ":"FAIL: ")+message);if(pass)Debug.Log("KITCHEN_2D_PASSED "+message);else Debug.LogError("KITCHEN_2D_FAILED "+message);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
}
}
