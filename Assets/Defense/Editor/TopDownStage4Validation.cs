using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Final migration checks run against isolated save files and a temporary project copy.
[InitializeOnLoad]
public static class TopDownStage4Validation {
    private const string Key="Defense.TopDownStage4.Validation";
    private static int step,checks,zoomIndex,clears,startGold,startCandy;
    private static double started,deadline;
    private static string deckBytes,loadedDeck,loadedCurrency;
    private static UnityEngine.Object[] oldObjects;
    private static readonly float[] zooms={7,9,12};
    private static bool restarting;
    [Serializable] private sealed class Snapshot {public string Deck,Currency;public int Gold,Candy,Active;}
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static GameFlow Flow=>Find<GameFlow>();
    private static BattlefieldMap2D Map=>Find<BattlefieldMap2D>();
    private static EnemyRegistry Registry=>Find<EnemyRegistry>();
    private static FixedIntervalSpawner Schedule=>Find<FixedIntervalSpawner>();
    private static StageVictoryController Victory=>Find<StageVictoryController>();
    private static GameplayHand Hand=>Find<GameplayHand>();
    private static string SnapshotPath=>Path.Combine(Path.GetDirectoryName(DeckPersistence.DefaultPath),"stage4-expected.json");
    static TopDownStage4Validation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run()=>Start(false);
    public static void ReadRestart()=>Start(true);
    private static void Start(bool restart){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");var real=Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar;
        if(string.IsNullOrWhiteSpace(path)||Path.GetFullPath(path).StartsWith(real)||Path.GetFullPath(PlayerCurrency.DefaultPath).StartsWith(real)||Path.GetFullPath(path)==Path.GetFullPath(PlayerCurrency.DefaultPath))throw new InvalidOperationException("Use distinct isolated Deck/currency test files.");
        SessionState.SetString(Key+".Deck",File.ReadAllText(path));SessionState.SetString(Key+".Currency",File.ReadAllText(PlayerCurrency.DefaultPath));
        SessionState.SetBool(Key+".Restart",restart);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){step=checks=zoomIndex=clears=0;started=EditorApplication.timeSinceStartup;deadline=0;restarting=SessionState.GetBool(Key+".Restart",false);loadedDeck=SessionState.GetString(Key+".Deck","");loadedCurrency=SessionState.GetString(Key+".Currency","");EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string m,string trace,LogType kind){
        if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense.")){Directory.CreateDirectory("Documentation/Report");File.AppendAllText("Documentation/Report/2D_Stage4_EditorSearch_Warnings.txt",m+"\n"+trace+"\n");return;}
        if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)Finish(false,m+"\n"+trace);
    }
    private static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;Debug.Log("TOPDOWN4 PASS: "+message);}
    private static void Click(string text){var b=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(x=>x.GetComponentInChildren<UnityEngine.UI.Text>().text==text);Check(b.interactable,"UI callback available: "+text);b.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    private static void Next(int next,double wait=.1){step=next;deadline=EditorApplication.timeSinceStartup+wait;}
    private static void TestStage(){while(PlayerSession.Selection.SelectedStageIndex>0)PlayerSession.Selection.MovePrevious();int target=PlayerSession.Catalog.Stages.ToList().FindIndex(s=>s.DisplayName=="Test Stage");for(int i=0;i<target;i++)PlayerSession.Selection.MoveNext();Find<LobbyUI>().Carousel.Advance(1);}
    private static void ConfigurePreset(){
        Click("Deck");if(PlayerSession.Presets.ActivePresetIndex!=1)PlayerSession.Presets.SwitchPreset(1);
        var edit=new DeckEditorController(PlayerSession.Deck,PlayerSession.Catalog.AvailableCards);
        foreach(var skill in PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>())if(!PlayerSession.Deck.Contains(skill)){
            int index=PlayerSession.Deck.Slots.ToList().FindLastIndex(c=>c is TowerCardDefinition);Check(edit.Replace(index,skill),"Preset 2 edit adds actual Skill through existing Deck Editor model");
        }
        // An explicit valid edit, even when the copied profile already contains both Skills.
        var replacement=edit.Available.OfType<TowerCardDefinition>().First();Check(edit.Replace(PlayerSession.Deck.Slots.ToList().FindIndex(c=>c is TowerCardDefinition),replacement),"Preset 2 receives a unique Tower edit before persistence sequence");
        deckBytes=File.ReadAllText(DeckPersistence.DefaultPath);Click("Back to Lobby");
    }
    private static void VerifyProfile()=>Check(PlayerSession.Presets.ActivePresetIndex==1&&File.ReadAllText(DeckPersistence.DefaultPath)==deckBytes,"Transient actions preserve active Preset 2 and all saved preset slot positions");
    private static void Fill(){for(int i=0;i<8;i++)Schedule.Advance(PlayerSession.ActiveStage.SpawnInterval);Check(Schedule.SpawnedCount==5&&Registry.AliveEnemyCount==5,"Test Stage still schedules exactly five actors");foreach(var e in Registry.Enemies)e.Movement.Stop();}
    private static void Kill(){foreach(var e in Registry.Enemies.ToArray())e.ReceiveDamage(100000);}
    private static void Fresh(){
        Schedule.enabled=false;Check(Flow.IsPlaying&&Schedule.SpawnedCount==0&&Registry.AliveEnemyCount==0&&Victory.DefeatedCount==0&&Victory.GoldEarned==0,"Reload resets state, actors, schedule and reward/death counters");
        Check(Find<Turret>()==null&&Find<PersistentSlowArea>()==null&&Map.PlacementSlots.All(s=>!s.IsOccupied)&&Find<SkillCastingController>().PendingCastCount==0,"Reload clears Towers, slots, active/fading zones and pending casts");
        Check(Find<BaseHealth>().Current==3&&Mathf.Abs(Find<ElixirSystem>().Current-3)<.1f&&Find<ResourceWallet>().Balance==300,"Fresh Test Stage restores existing HP, Elixir and combat resources");
        if(oldObjects!=null)Check(oldObjects.All(o=>o==null),"No captured old scene actors or slow effects survive Retry");VerifyProfile();
    }
    private static void CheckCleanWorld(){
        Check(Map.WorldCamera.orthographic&&Map.Path.Plane==WaypointPlane.XY&&Registry.Plane==WaypointPlane.XY&&Map.Path.GetPoint(0).x<Map.Path.GetPoint(Map.Path.Count-1).x,"Default kitchen world is native orthographic XY with left-to-right route");
        Check(Find<Collider>()==null&&Find<Rigidbody>()==null,"Active 2D route contains no 3D colliders or bodies");
        foreach(var c in new UnityEngine.Object[]{Find<TurretPlacementController>(),Find<SkillCastingController>()}){
            var data=new SerializedObject(c);Check(data.FindProperty("pointer2D").objectReferenceValue!=null&&data.FindProperty("surface")==null,"2D input dependency exists and retired surface field is removed: "+c.GetType().Name);
        }
        Check(UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length==1&&UnityEngine.Object.FindObjectsByType<StageInitializer>().Length==1,"Single EventSystem and authoritative session bootstrap");
        foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script: "+t.name);
        foreach(var slot in Map.PlacementSlots){var visual=slot.GetComponentInChildren<SpriteRenderer>();var shape=slot.GetComponent<Collider2D>();Check(Vector2.Distance(visual.bounds.center,shape.bounds.center)<.001f&&Vector2.Distance(shape.bounds.size,Vector2.one)<.001f&&Vector2.Distance(visual.transform.localScale,new Vector2(1.5f,1.5f))<.001f,"Wooden plate visual is enlarged 1.5x while centered on unchanged slot Collider2D: "+slot.name);}
    }
    private static void EnsureVisibleSkill(){
        Find<ResourceWallet>().Initialize(10000);
        for(int n=0;n<4&&!Hand.Cycle.Hand.Any(c=>c.Definition is SkillCardDefinition);n++){
            Hand.Select(0);Check(Find<TurretPlacementController>().TryPlace(Map.PlacementSlots.First(s=>!s.IsOccupied)),"Production placement brings a Skill into random Hand");
            foreach(var t in UnityEngine.Object.FindObjectsByType<Turret>())t.enabled=false;
        }
        int index=Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition is SkillCardDefinition);Check(index>=0&&Hand.Select(index),"Actual persistent Hand selects a Skill for spatial aiming probes");
    }
    private static Enemy Spawn(Vector3 position,EnemyDefinition data){var e=Find<EnemySpawner>().Spawn(data);e.Movement.Stop();e.transform.position=position;Registry.NotifyMoved(e);return e;}
    private static PersistentSlowArea Area(SkillDefinition data,Vector3 position){var before=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>();data.Effect.Apply(data,new SkillCastContext(position,Registry,Flow));return UnityEngine.Object.FindObjectsByType<PersistentSlowArea>().Except(before).Single();}
    private static void Radius(SkillAreaVisual visual,Vector3 center,float radius){
        SkillEllipseValidation.AssertMarker(visual,center,radius,Check);
        float pixels=radius*Map.WorldCamera.pixelHeight/(2*Map.WorldCamera.orthographicSize),aspect=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Defense/Data/ToyThrowSkill.asset").RangeProfile.AspectRatio;
        Check(Mathf.Abs(Vector2.Distance(Map.WorldCamera.WorldToScreenPoint(center),Map.WorldCamera.WorldToScreenPoint(center+Vector3.right*radius))-pixels)<.05f&&Mathf.Abs(Vector2.Distance(Map.WorldCamera.WorldToScreenPoint(center),Map.WorldCamera.WorldToScreenPoint(center+Vector3.up*radius*aspect))-pixels*aspect)<.05f,"Both ellipse semi-axes project proportionally across camera zoom/aspect without becoming circular");
    }
    private static void Spatial(){
        Canvas.ForceUpdateCanvases();Physics2D.SyncTransforms();var camera=Map.WorldCamera;var pointer=Find<BattlefieldPointer2D>();var casting=Find<SkillCastingController>();
        Check(camera.pixelWidth==Screen.width&&camera.pixelHeight==Screen.height,"Actual Screen, Camera and Game View resolution agree: "+Screen.width+"x"+Screen.height);
        foreach(var slot in Map.PlacementSlots){var screen=camera.WorldToScreenPoint(slot.PlacementPoint.position);Check(camera.pixelRect.Contains(screen)&&pointer.TryGetPoint(screen,out var world)&&Vector3.Distance(world,slot.PlacementPoint.position)<.001f&&pointer.GetSlot(world)==slot,"Visible slot round-trips through screen/XY/Physics2D: "+slot.name);}
        var floor=Map.PlayableArea.Bounds;
        var probes=new[]{new Vector3(floor.min.x+.1f,floor.max.y-.1f,0),new Vector3(floor.max.x-.1f,floor.min.y+.1f,0),Map.Path.GetPoint(2),Map.PlacementSlots[2].PlacementPoint.position+Vector3.right*.55f};
        foreach(var p in probes){Vector2 screen=camera.WorldToScreenPoint(p);bool ui=GameplayPointer.IsOverUI(screen);bool valid=casting.TryGetTabletopPoint(screen,out var world);Check(valid==!ui&&(!valid||Vector3.Distance(world,p)<.002f),"Skill conversion at edge/turn/near-slot respects world coordinates and any HUD interception");if(valid){casting.ProcessPointer(screen,true,false);Check(casting.IsAiming,"Real Skill input aims at edge/turn/near-slot without immediate cast");Radius(UnityEngine.Object.FindObjectsByType<SkillAreaVisual>().Single(v=>v.name=="Skill Aim Indicator"),p,casting.IndicatorRadius);casting.CancelAim();}}
        Check(!pointer.TryGetPoint(camera.WorldToScreenPoint(new Vector3(floor.max.x+.1f,floor.center.y,0)),out _),"Outside logical playable shape rejects input at every camera size");
        var button=GameObject.Find("Hand Card 1").GetComponent<RectTransform>();var original=button.position;
        var root=UnityEngine.Object.FindObjectsByType<Canvas>().Single(c=>c.renderMode==RenderMode.ScreenSpaceOverlay);
        Vector2 target=camera.WorldToScreenPoint(Map.PlacementSlots[2].transform.position);RectTransformUtility.ScreenPointToWorldPointInRectangle(root.GetComponent<RectTransform>(),target,null,out var overlay);button.position=overlay;Canvas.ForceUpdateCanvases();
        Check(GameplayPointer.IsOverUI(target)&&!pointer.TryGetPoint(target,out _)&&!casting.TryGetTabletopPoint(target,out _),"Actual uGUI Hand intercepts 2D slot and Skill queries at this resolution");button.position=original;Canvas.ForceUpdateCanvases();
        var enemyData=UnityEngine.Object.Instantiate(PlayerSession.ActiveStage.Enemy);var data=new SerializedObject(enemyData);data.FindProperty("health").floatValue=200;data.FindProperty("resourceReward").intValue=0;data.FindProperty("candyDropChance").floatValue=0;data.ApplyModifiedPropertiesWithoutUndo();
        var toy=PlayerSession.Deck.Slots.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is CircularDamageSkill).Skill;
        var slow=PlayerSession.Deck.Slots.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is SlowAreaSkill).Skill;float radius=toy.Radius;
        var a=Spawn(new Vector3(radius*.5f,0,8),enemyData);var b=Spawn(new Vector3(radius,0,-8),enemyData);var c=Spawn(new Vector3(radius+.1f,0,0),enemyData);
        var tower=PlayerSession.Deck.Slots.OfType<TowerCardDefinition>().First().Tower;
        Check(tower.Targeting.Select(Vector3.zero,radius,Registry.Enemies,WaypointPlane.XY)==a,"Closest targeting uses XY range, ignoring Sprite render depth");
        toy.Effect.Apply(toy,new SkillCastContext(Vector3.zero,Registry,Flow));Check(a.CurrentHealth==200-toy.Damage&&b.CurrentHealth==200-toy.Damage&&c.CurrentHealth==200,"Toy Throw hits center/boundary and excludes outside at configured world radius");
        Check(Mathf.Abs(a.GetComponent<EnemyHealthBar>().NormalizedHealth-a.CurrentHealth/200)<.001f,"Health bar follows actual damage");
        a.transform.position=new Vector3(slow.Radius*.5f,0,8);b.transform.position=new Vector3(slow.Radius,0,-8);c.transform.position=new Vector3(slow.Radius+.1f,0,0);
        foreach(var enemy in new[]{a,b,c})Registry.NotifyMoved(enemy);
        var area=Area(slow,Vector3.zero);var other=Area(slow,Vector3.right);Radius(area.GetComponentInChildren<SkillAreaVisual>(),Vector3.zero,slow.Radius);
        Check(slow.ActiveDuration==8&&a.Movement.Modifiers.SourceCount==2&&a.Movement.EffectiveMovementSpeed==a.Movement.BaseMovementSpeed*.5f,"Eight-second Slow Areas retain independent sources without multiplying slowdown");
        Check(a.GetComponentInChildren<SpriteRenderer>().sortingOrder>area.GetComponentInChildren<SkillAreaVisual>().MarkerRenderer.sortingOrder&&a.GetComponentInChildren<Canvas>().sortingOrder>a.GetComponentInChildren<SpriteRenderer>().sortingOrder&&root.sortingOrder>a.GetComponentInChildren<Canvas>().sortingOrder,"Sorting puts units above zones, HP above units, and HUD above HP");
        a.ReceiveDamage(100000);Check(a.Movement.Modifiers.SourceCount==0,"Death while inside overlapping zones releases all slow sources");UnityEngine.Object.DestroyImmediate(b.gameObject);Check(area.AffectedCount==0,"Despawn inside overlapping zones removes membership");
        UnityEngine.Object.DestroyImmediate(other.gameObject);Check(c.Movement.Modifiers.SourceCount==0,"Removing final zone releases remaining source");area.Advance(slow.ActiveDuration+.01f);float opacity=area.VisualOpacity;Flow.TogglePause();area.Advance(100);Check(!area.IsActive&&area.VisualOpacity==opacity&&opacity>0,"Pause freezes post-expiry visual fade");Flow.TogglePause();area.Advance(slow.FadeDuration+1);
        foreach(var effect in UnityEngine.Object.FindObjectsByType<PersistentSlowArea>())UnityEngine.Object.DestroyImmediate(effect.gameObject);
        foreach(var e in Registry.Enemies.ToArray())UnityEngine.Object.DestroyImmediate(e.gameObject);UnityEngine.Object.DestroyImmediate(enemyData);
        Check(Victory.DefeatedCount==0&&Schedule.SpawnedCount==0,"Spatial diagnostic actors never satisfy finite stage kill objective");VerifyProfile();
        Debug.Log("TOPDOWN4 MATRIX: "+Screen.width+"x"+Screen.height+" minSize="+zooms[zoomIndex]+" actualSize="+camera.orthographicSize);
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>180)throw new Exception("Stage 4 timeout at phase "+step);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<deadline)return;
        switch(step){
            case 0:
                if(Find<MainMenuUI>()==null||GameObject.Find("Start Game")==null)return;
                Check(File.ReadAllText(DeckPersistence.DefaultPath)==loadedDeck&&File.ReadAllText(PlayerCurrency.DefaultPath)==loadedCurrency,"Copied existing version-1 saves load without reset or rewrite");Click("Start Game");
                if(restarting){RestoreSnapshot();TestStage();Click("Enter Stage");Next(80,.02);break;}
                ConfigurePreset();PlayerSession.Selection.MoveNext();Find<LobbyUI>().Carousel.Advance(1);Check(!Find<LobbyUI>().TryEnter()&&UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t=>t.text=="This stage is locked.")&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu","Default 2D entry respects existing stage locks");
                TestStage();Check(PlayerSession.TrySelectStageForPlay()&&PlayerSession.ActiveBattleScene==PlayerSession.TopDownSceneName,"Parameterless session selection now defaults to 2D");Click("Enter Stage");Next(1,.02);break;
            case 1:
                if(GameObject.Find("Hand Card 1")==null)return;Schedule.enabled=false;CheckCleanWorld();EnsureVisibleSkill();Next(2);break;
            case 2:
                var frame=Map.WorldCamera.GetComponent<TopDownCameraFrame>();var config=new SerializedObject(frame);config.FindProperty("minimumSize").floatValue=zooms[zoomIndex];config.ApplyModifiedPropertiesWithoutUndo();frame.Refresh();Spatial();
                if(++zoomIndex<zooms.Length){Next(2);break;}
                var slow=PlayerSession.Deck.Slots.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is SlowAreaSkill).Skill;
                var fade=Area(slow,Vector3.zero);fade.Advance(slow.ActiveDuration+.01f);Area(slow,Vector3.right);oldObjects=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>().Cast<UnityEngine.Object>().Concat(UnityEngine.Object.FindObjectsByType<Turret>()).ToArray();Flow.GameOver();fade.Advance(100);Check(fade.VisualOpacity==0&&!fade.GetComponentInChildren<SkillAreaVisual>().IsVisible&&oldObjects.OfType<PersistentSlowArea>().All(a=>!a.IsActive&&a.AffectedCount==0),"Terminal state clears active/fading skill markers and slow sources before Retry");Click("Retry");Next(3,.02);break;
            case 3:
                if(GameObject.Find("Hand Card 1")==null)return;Fresh();startGold=PlayerSession.Currency.Gold;startCandy=PlayerSession.Currency.Candy;Fill();Registry.Enemies.First().ReceiveDamage(100000);
                Check(PlayerSession.Currency.Candy==startCandy+1,"Required restart sequence earns Candy automatically from a legitimate death");
                foreach(var e in Registry.Enemies.ToArray()){e.Movement.Initialize(Map.Path,PlayerSession.ActiveStage.Enemy.MovementSpeed);e.Movement.Advance(10000);if(Flow.IsTerminal)break;}Next(4);break;
            case 4:
                Check(Flow.State==GameplayState.GameOver&&PlayerSession.Currency.Gold==startGold&&PlayerSession.Currency.Candy==startCandy+1,"Lose-before-clear sequence retains Candy and awards no Gold");Click("Retry");Next(5,.02);break;
            case 5:
                if(GameObject.Find("Hand Card 1")==null)return;Fresh();Fill();Kill();Next(6);break;
            case 6:
                clears++;Check(Flow.State==GameplayState.Victory&&Victory.DefeatedCount==5&&PlayerSession.Currency.Gold==startGold+50*clears&&PlayerSession.Currency.Candy==startCandy+1+5*clears,"Repeated clear "+clears+" grants each configured Gold/Candy award exactly once");Check(!Victory.EvaluateClear(),"Repeated clear check cannot duplicate rewards");VerifyProfile();
                if(clears<3){Click("Replay");Next(5,.02);break;}Click("Return to Lobby");Next(7);break;
            case 7:
                if(Find<LobbyUI>()==null)return;
                // Campaign configurations are continuous in the original. Exercise unlocked navigation using a progression fixture, without inventing a Next Stage feature.
                Check(PlayerSession.Progression.Complete(0)&&PlayerSession.Progression.IsUnlocked(1)&&!PlayerSession.Progression.Complete(0),"Existing first-clear progression model unlocks next stage idempotently");for(int i=0;i<PlayerSession.Catalog.Stages.Count;i++)PlayerSession.Selection.MovePrevious();PlayerSession.Selection.MoveNext();Find<LobbyUI>().Carousel.Advance(1);Click("Enter Stage");Next(8,.02);break;
            case 8:
                if(GameObject.Find("Hand Card 1")==null)return;Schedule.enabled=false;
                Check(PlayerSession.ActiveStage==PlayerSession.Catalog.Stages[1]&&Find<BaseHealth>().Current==PlayerSession.ActiveStage.BaseHealth&&Find<ResourceWallet>().Balance==PlayerSession.ActiveStage.StartingResources&&Registry.Plane==WaypointPlane.XY,"Unlocked next stage selection loads matching configuration through default 2D route");VerifyProfile();Click("Pause");Click("Exit");Next(9);break;
            case 9:
                if(Find<LobbyUI>()==null)return;WriteSnapshot();Finish(true,checks+" Stage 4 assertions; 3 orthographic camera sizes at native test resolution; lose-before-clear then 3 clears");break;
            case 80:
                if(GameObject.Find("Hand Card 1")==null)return;Schedule.enabled=false;RestoreSnapshot();Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==PlayerSession.TopDownSceneName&&Registry.Plane==WaypointPlane.XY&&Flow.IsPlaying&&Schedule.SpawnedCount==0,"Fresh second process enters default 2D route with clean transient state");
                Check(Hand.Cycle.InitialOrder.Select(c=>c.Definition.CardId).OrderBy(x=>x).SequenceEqual(PlayerSession.Deck.Slots.Select(c=>c.CardId).OrderBy(x=>x)),"Restart battle uses the same six saved active-preset cards");Check(!PlayerSession.Progression.IsComplete(0)&&!PlayerSession.Progression.IsUnlocked(1),"Original session-only progression resets on process restart (known limitation)");Finish(true,checks+" separate-process restart assertions");break;
        }
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void WriteSnapshot(){var s=new Snapshot{Deck=File.ReadAllText(DeckPersistence.DefaultPath),Currency=File.ReadAllText(PlayerCurrency.DefaultPath),Gold=PlayerSession.Currency.Gold,Candy=PlayerSession.Currency.Candy,Active=PlayerSession.Presets.ActivePresetIndex};File.WriteAllText(SnapshotPath,JsonUtility.ToJson(s,true));}
    private static void RestoreSnapshot(){var s=JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));Check(s.Deck==File.ReadAllText(DeckPersistence.DefaultPath)&&s.Currency==File.ReadAllText(PlayerCurrency.DefaultPath),"Second process loads exact saved Deck/currency bytes without rewriting schema or Card IDs");Check(PlayerSession.Presets.ActivePresetIndex==s.Active&&s.Active==1&&PlayerSession.Currency.Gold==s.Gold&&PlayerSession.Currency.Candy==s.Candy,"Saved Preset 2 composition/selection and earned Gold/Candy survive full Editor process restart");}
    private static void Finish(bool pass,string text){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/2D_Stage4_"+(restarting?"Restart":"Validation")+"_Result.txt",(pass?"PASS: ":"FAIL: ")+text);if(pass)Debug.Log("TOPDOWN_STAGE4_PASSED "+text);else Debug.LogError("TOPDOWN_STAGE4_FAILED "+text);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
}
}
