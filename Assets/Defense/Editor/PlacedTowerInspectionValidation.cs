using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Defense.Editor {
[InitializeOnLoad]
public static class PlacedTowerInspectionValidation {
    private const string Key="Defense.PlacedRange.Validation";
    private static int phase,checks,frame,invoked;
    private static Button probe;
    private static double started;
    private static Tower a,b;
    private static TurretDefinition original,clone;
    private static Vector3 originalPosition;
    private static UnityEngine.Object[] retired;
    private static string currencyFixture;
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    private static PlacedTowerInspectionController Inspection=>Find<PlacedTowerInspectionController>();
    private static BattlefieldPointer2D Pointer=>Find<BattlefieldPointer2D>();
    private static BattlefieldMap2D Map=>Find<BattlefieldMap2D>();
    private static GameplayHand Hand=>Find<GameplayHand>();
    private static GameFlow Flow=>Find<GameFlow>();
    private static TurretPlacementController Placement=>Find<TurretPlacementController>();
    private static SkillCastingController Casting=>Find<SkillCastingController>();
    private static EnemyRegistry Registry=>Find<EnemyRegistry>();
    private static LineRenderer Ring=>GameObject.Find("Placed Tower Range Indicator").GetComponent<LineRenderer>();
    static PlacedTowerInspectionValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run(){
        var path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
        if(EditorApplication.isPlaying||string.IsNullOrWhiteSpace(path)||Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Use an isolated save profile in Edit Mode.");
        SessionState.SetString(Key+".Deck",File.ReadAllText(path));SessionState.SetString(Key+".Currency",File.ReadAllText(PlayerCurrency.DefaultPath));
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){phase=-1;checks=invoked=0;frame=-1;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string m,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,m+"\n"+trace);}
    private static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;Debug.Log("PLACED RANGE PASS: "+message);}
    private static Vector2 Screen(Vector3 point)=>Camera.main.WorldToScreenPoint(point);
    private static Vector3 Hit(Tower tower)=>tower.transform.position+Vector3.up*.6f;
    private static void ClickWorld(Vector3 point){Physics2D.SyncTransforms();Placement.ProcessPointer(Screen(point),true);}
    private static int Bring(CardDefinition card){
        Hand.CancelSelection();for(int i=0;i<6&&!Hand.Cycle.Hand.Any(c=>c.Definition==card);i++)Hand.Cycle.Use(Hand.Cycle.Hand[0]);Hand.CancelSelection();
        return Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card);
    }
    private static Tower Place(int slotIndex,TowerCardDefinition card){
        var index=Bring(card);Hand.Select(index);Find<ResourceWallet>().Initialize(10000);Find<ElixirSystem>().Advance(100);
        var slot=Map.PlacementSlots[slotIndex];Physics2D.SyncTransforms();
        Check(Placement.ProcessPointer(Screen(slot.PlacementPoint.position),true)&&slot.IsOccupied,"Production Card/Physics2D path places "+card.DisplayName);
        Check(Hand.SelectedCard==null&&Inspection.InspectedTower==null&&!Ring.enabled,"Successful placement never automatically inspects its newly placed Tower");return slot.Occupant;
    }
    private static void AssertRing(Tower tower){
        var line=Ring;float range=tower.AttackRange;var center=tower.transform.position;
        Check(Inspection.InspectedTower==tower&&line.enabled&&line.loop&&line.positionCount==96&&line.useWorldSpace,"One reused world-space ring belongs to the inspected runtime Tower");
        Check(Enumerable.Range(0,line.positionCount).All(i=>Mathf.Abs(PlanarSpace.SqrDistance(center,line.GetPosition(i),WaypointPlane.XY)-range*range)<.003f&&Mathf.Abs(line.GetPosition(i).z-center.z)<.001f),"Every circle vertex matches actual effective XY targeting radius");
        var average=Enumerable.Range(0,line.positionCount).Select(line.GetPosition).Aggregate(Vector3.zero,(sum,p)=>sum+p)/line.positionCount;
        Check(Vector3.Distance(average,center)<.001f&&Mathf.Abs(line.GetPosition(0).x-line.GetPosition(48).x-2*range)<.001f,"Circle center and diameter use Tower root and two times runtime range, independent of Sprite scale");
        Check(line.sortingOrder==30&&line.GetComponent<Collider2D>()==null&&line.GetComponent<Collider>()==null&&line.GetComponent<UnityEngine.UI.Graphic>()==null,"Range ring is correctly sorted and cannot intercept physics or UI clicks");
    }
    private sealed class Snapshot {
        private readonly RuntimeCard[] hand=Hand.Cycle.Hand.ToArray(),queue=Hand.Cycle.UpcomingQueue.ToArray();
        private readonly float energy=Find<ElixirSystem>().Current;
        private readonly int balance=Find<ResourceWallet>().Balance,towers=UnityEngine.Object.FindObjectsByType<Tower>().Length,pending=Casting.PendingCastCount,spawns=Find<FixedIntervalSpawner>().SpawnedCount;
        public void Unchanged(string reason)=>Check(Hand.Cycle.Hand.SequenceEqual(hand)&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue)&&Find<ElixirSystem>().Current==energy&&Find<ResourceWallet>().Balance==balance&&UnityEngine.Object.FindObjectsByType<Tower>().Length==towers&&Casting.PendingCastCount==pending&&Find<FixedIntervalSpawner>().SpawnedCount==spawns,reason+": inspection spends nothing, performs no Card action and preserves Hand/queue");
    }
    private static void Mouse(PointerEventData pointer,Vector2 point,PointerEventData.FramePressState state){
        pointer.position=point;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);pointer.pointerCurrentRaycast=hits.FirstOrDefault();
        typeof(StandaloneInputModule).GetMethod("ProcessMousePress",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(EventSystem.current.currentInputModule,new object[]{new PointerInputModule.MouseButtonEventData{buttonData=pointer,buttonState=state}});
    }
    private static void ClickUI(Button button){
        Canvas.ForceUpdateCanvases();Vector2 point=RectTransformUtility.WorldToScreenPoint(null,button.transform.position);var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=-102};
        Mouse(pointer,point,PointerEventData.FramePressState.Pressed);Placement.ProcessPointer(point,true);Casting.ProcessPointer(point,true,false);Casting.ProcessPointer(point,false,true);Mouse(pointer,point,PointerEventData.FramePressState.Released);
    }
    private static void DismissAt(Vector3 point,string reason){Inspection.InspectTower(a);ClickWorld(point);Check(Inspection.InspectedTower==null&&!Ring.enabled,reason);}
    private static void Core(){
        Find<FixedIntervalSpawner>().enabled=false;foreach(var existingEnemy in Registry.Enemies.ToArray())UnityEngine.Object.DestroyImmediate(existingEnemy.gameObject);
        var localDeck=Find<TopDownInteractionSandbox>().Deck;var saved=localDeck.Slots.ToArray();var cards=saved.OfType<TowerCardDefinition>().ToArray();
        a=Place(0,cards[0]);b=Place(3,cards[1]);Hand.CancelSelection();Physics2D.SyncTransforms();
        Check(Pointer.GetSlot(a.transform.position)==Map.PlacementSlots[0]&&Pointer.GetPlacedTower(a.transform.position)==a,"Overlapping wooden-plate collider does not mask the placed Tower hit area");
        var hit=a.transform.Find("Inspection Hit Area").GetComponent<BoxCollider2D>();Check(hit.isTrigger&&hit.gameObject.layer==LayerMask.NameToLayer("Default")&&Map.PlacementSlots[0].GetComponent<BoxCollider2D>().size==Vector2.one,"Dedicated selection trigger is independent of unchanged slot click area");
        var snapshot=new Snapshot();ClickWorld(Hit(a));AssertRing(a);var visual=Ring;
        ClickWorld(Hit(a));Check(Inspection.InspectedTower==null&&!Ring.enabled,"Second click of inspected Tower clears it");
        ClickWorld(Hit(a));ClickWorld(Hit(b));AssertRing(b);Check(Ring==visual&&UnityEngine.Object.FindObjectsByType<PlacedTowerInspectionController>().Length==1,"Direct Tower switching reuses exactly one inspection owner and visual");snapshot.Unchanged("Inspect/toggle/switch");
        var ground=new Vector3(0,-3.8f,0);Check(Pointer.GetPlacedTower(ground)==null,"Ground fixture contains no Tower");DismissAt(ground,"Empty ground click dismisses inspection");
        var path=Enumerable.Range(0,Map.Path.Count).Select(Map.Path.GetPoint).First(p=>Pointer.GetPlacedTower(p)==null&&Map.PlayableArea.Contains(p));DismissAt(path,"Path click dismisses inspection");
        DismissAt(Map.PlacementSlots[5].PlacementPoint.position,"Empty placement-slot click dismisses inspection");DismissAt(a.transform.position+Vector3.right*.65f,"Bare occupied plate edge outside Tower hit area dismisses inspection");
        var enemy=Find<EnemySpawner>().Spawn(Find<TopDownInteractionSandbox>().TestStage.Enemy);enemy.Movement.Stop();enemy.transform.position=ground;float hp=enemy.CurrentHealth;DismissAt(ground,"Non-Tower Enemy click dismisses inspection");Check(enemy.CurrentHealth==hp,"Inspecting/clicking away never damages the Enemy");UnityEngine.Object.DestroyImmediate(enemy.gameObject);
        Inspection.InspectTower(a);Placement.ProcessPointer(new Vector2(-100,-100),true);Check(Inspection.InspectedTower==null&&!Ring.enabled,"Non-battlefield/screen-exterior click dismisses inspection");snapshot.Unchanged("Click-away dismissal");
        
        RectTransformUtility.ScreenPointToWorldPointInRectangle(GameObject.Find("Canvas").GetComponent<RectTransform>(),Screen(Hit(a)),null,out var overlay);probe.transform.position=overlay;Canvas.ForceUpdateCanvases();
        Inspection.InspectTower(a);ClickUI(probe);Check(invoked==1&&Inspection.InspectedTower==null&&!Ring.enabled,"Non-card UI clears inspection without swallowing its action or clicking through to Tower (actions="+invoked+", inspected="+(Inspection.InspectedTower!=null)+", ring="+Ring.enabled+", overUI="+GameplayPointer.IsOverUI(RectTransformUtility.WorldToScreenPoint(null,probe.transform.position))+")");UnityEngine.Object.DestroyImmediate(probe.gameObject);snapshot.Unchanged("UI-only dismissal");
        int index=Bring(cards[0]);Inspection.InspectTower(a);var button=GameObject.Find("Hand Card "+(index+1)).GetComponent<Button>();ClickUI(button);
        Check(Hand.SelectedCard?.Definition==cards[0]&&Inspection.InspectedTower==null&&!Ring.enabled,"Tower Card selection clears inspection immediately and selects normally");
        snapshot=new Snapshot();ClickWorld(a.transform.position);Check(Hand.SelectedCard?.Definition==cards[0]&&Inspection.InspectedTower==null,"Selected Tower Card keeps occupied-slot action priority over inspection");snapshot.Unchanged("Occupied Card action");
        ClickUI(button);Check(Hand.SelectedCard==null&&Inspection.InspectedTower==null,"Same-card toggle deselects without automatically inspecting a Tower");
        ClickWorld(Hit(a));AssertRing(a);ClickUI(button);var card=Hand.SelectedCard;var before=Hand.Cycle.Hand.ToArray();var queue=Hand.Cycle.UpcomingQueue.ToArray();int cost=Find<ResourceWallet>().Balance;
        Check(Placement.ProcessPointer(Screen(Map.PlacementSlots[2].PlacementPoint.position),true)&&Hand.SelectedCard==null&&Inspection.InspectedTower==null&&Find<ResourceWallet>().Balance==cost-((TowerCardDefinition)card.Definition).Tower.Cost,"Tower placement after inspection preserves successful-use behavior");
        Check(Hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=card).Concat(new[]{queue[0]})),"Tower placement still rotates Hand exactly once");
        var skill=saved.OfType<SkillCardDefinition>().First();index=Bring(skill);Inspection.InspectTower(a);ClickUI(GameObject.Find("Hand Card "+(index+1)).GetComponent<Button>());
        Check(Hand.SelectedCard?.Definition==skill&&Inspection.InspectedTower==null&&!Casting.IsAiming,"Skill Card selection clears inspection without aiming or casting");
        Find<ElixirSystem>().Advance(100);float energy=Find<ElixirSystem>().Current;before=Hand.Cycle.Hand.ToArray();queue=Hand.Cycle.UpcomingQueue.ToArray();card=Hand.SelectedCard;
        Placement.ProcessPointer(Screen(Hit(a)),true);Casting.ProcessPointer(Screen(Hit(a)),true,false);
        Check(Casting.IsAiming&&Inspection.InspectedTower==null,"Skill hold over a placed Tower still aims instead of inspecting");
        Check(Casting.ProcessPointer(Screen(Hit(a)),false,true)==CardUseResult.Success&&Hand.SelectedCard==null&&Inspection.InspectedTower==null,"Skill release commits normally and never auto-inspects under the pointer");
        Check(Find<ElixirSystem>().Current==energy-skill.ElixirCost&&Hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=card).Concat(new[]{queue[0]}))&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue.Skip(1).Concat(new[]{card})),"Skill Elixir payment and Hand/Upcoming Queue remain unchanged");
        Inspection.InspectTower(a);Hand.Initialize(localDeck,Placement,Flow);Check(Inspection.InspectedTower==null&&!Ring.enabled,"In-place Hand/run reset clears inspection");
        Inspection.InspectTower(a);Flow.TogglePause();Check(Inspection.InspectedTower==null&&!Ring.enabled&&!Inspection.InspectTower(a),"Pause clears inspection and blocks inspecting");Flow.TogglePause();Check(Inspection.InspectedTower==null,"Resume does not automatically restore inspection");
        Check(localDeck.Slots.SequenceEqual(saved)&&PlayerSession.Currency==null&&PlayerSession.Presets==null,"Read-only inspection preserves local Deck and never initializes persistent services");
        original=a.Definition;originalPosition=a.transform.position;clone=UnityEngine.Object.Instantiate(original);a.Initialize(clone,Registry);Inspection.InspectTower(a);
        var data=new SerializedObject(clone);data.FindProperty("attackRange").floatValue=4.25f;data.ApplyModifiedPropertiesWithoutUndo();a.transform.position+=Vector3.right*.25f;
    }
    private static void RetireSnapshot(){retired=new UnityEngine.Object[]{Inspection,Ring,a};}
    private static void Next(int next){phase=next;frame=Time.frameCount;}
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>100)throw new Exception("Placed Tower inspection timeout at "+phase);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||Time.frameCount==frame)return;
        if(phase==6){if(Find<MainMenuUI>()==null)return;Check(retired.All(o=>o==null)&&Find<PlacedTowerInspectionController>()==null&&GameObject.Find("Placed Tower Range Indicator")==null,"Scene exit destroys inspection owner, Tower and range visual");Check(File.ReadAllText(DeckPersistence.DefaultPath)==SessionState.GetString(Key+".Deck","")&&File.ReadAllText(PlayerCurrency.DefaultPath)==SessionState.GetString(Key+".Currency",""),"Entering Lobby after inspection preserves copied original profile bytes");Finish(true,checks+" inspection/range/lifecycle assertions");return;}
        if(Find<TopDownInteractionSandbox>()==null||GameObject.Find("Hand Card 1")==null)return;
        switch(phase){
            case -1:probe=UiFactory.Button(GameObject.Find("Canvas").transform,"Inspection UI Probe",Vector2.zero,()=>invoked++,size:new Vector2(120,60));Next(0);break;
            case 0:Core();Next(1);break;
            case 1:
                Check(a.AttackRange==4.25f,"Runtime range accessor reflects changed cloned definition");AssertRing(a);
                var enemy=Find<EnemySpawner>().Spawn(Find<TopDownInteractionSandbox>().TestStage.Enemy);enemy.Movement.Stop();enemy.transform.position=a.transform.position+Vector3.right*a.AttackRange+Vector3.forward*9;
                Check(a.Definition.Targeting.Select(a.transform.position,a.AttackRange,new[]{enemy},WaypointPlane.XY)==enemy,"Displayed circle shares inclusive XY targeting boundary, ignoring visual Z");enemy.transform.position+=Vector3.right*.05f;Check(a.Definition.Targeting.Select(a.transform.position,a.AttackRange,new[]{enemy},WaypointPlane.XY)==null,"Target outside the displayed runtime radius remains ineligible");UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                a.transform.position=originalPosition;a.Initialize(original,Registry);UnityEngine.Object.DestroyImmediate(clone);clone=null;Inspection.InspectTower(b);b.enabled=false;Next(2);break;
            case 2:Check(Inspection.InspectedTower==null&&!Ring.enabled,"Actual LateUpdate clears disabled Tower inspection");b.enabled=true;Inspection.InspectTower(b);UnityEngine.Object.Destroy(b.gameObject);Next(3);break;
            case 3:
                Check(Inspection.InspectedTower==null&&!Ring.enabled,"Actual LateUpdate safely clears destroyed Tower inspection");Inspection.InspectTower(a);Inspection.enabled=false;Check(Inspection.InspectedTower==null&&!Ring.enabled,"Disabling the inspection component hides its visual");Inspection.enabled=true;
                Inspection.InspectTower(a);Flow.GameOver();Check(Inspection.InspectedTower==null&&!Ring.enabled&&!Inspection.InspectTower(a),"Game Over clears inspection and blocks further input");RetireSnapshot();Flow.Retry();Next(4);break;
            case 4:
                Find<FixedIntervalSpawner>().enabled=false;Check(retired.All(o=>o==null)&&Inspection.InspectedTower==null&&!Ring.enabled&&Hand.SelectedCard==null,"Retry reloads fresh inspection/Hand state without orphaned visuals");
                a=Place(0,Find<TopDownInteractionSandbox>().Deck.Slots.OfType<TowerCardDefinition>().First());Inspection.InspectTower(a);
                var stage=Find<TopDownInteractionSandbox>().TestStage;var schedule=Find<FixedIntervalSpawner>();schedule.Configure(stage.Enemy,.1f,1);
                currencyFixture=Path.Combine(Path.GetTempPath(),"inspection-reward-"+Guid.NewGuid().ToString("N")+".json");var currency=new PlayerCurrency(new JsonFileStore<PlayerCurrencySaveData>(currencyFixture,"Inspection validation"));
                var victory=Inspection.gameObject.AddComponent<StageVictoryController>();victory.Initialize(Flow,schedule,Registry,stage,currency,()=>{});schedule.Advance(.1f);foreach(var e in Registry.Enemies.ToArray())e.ReceiveDamage(100000);
                Check(victory.EvaluateClear()&&Flow.State==GameplayState.Victory&&Inspection.InspectedTower==null&&!Ring.enabled&&currency.Gold==stage.ClearGoldReward,"Existing Victory/reward path clears inspection without changing clear Gold");RetireSnapshot();Flow.Retry();Next(5);break;
            case 5:
                Find<FixedIntervalSpawner>().enabled=false;Check(retired.All(o=>o==null)&&Inspection.InspectedTower==null&&!Ring.enabled,"Victory Replay also destroys prior inspection and starts clean");a=Place(0,Find<TopDownInteractionSandbox>().Deck.Slots.OfType<TowerCardDefinition>().First());Inspection.InspectTower(a);RetireSnapshot();Flow.ExitToMenu();Next(6);break;
        }
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Finish(bool pass,string message){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);if(!string.IsNullOrEmpty(currencyFixture))File.Delete(currencyFixture);Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Placed_Tower_Range_Result.txt",(pass?"PASS: ":"FAIL: ")+message);if(pass)Debug.Log("PLACED_TOWER_RANGE_PASSED "+message);else Debug.LogError("PLACED_TOWER_RANGE_FAILED "+message);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
}
}
