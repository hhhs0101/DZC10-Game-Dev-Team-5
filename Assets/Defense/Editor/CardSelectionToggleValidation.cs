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
public static class CardSelectionToggleValidation {
    private const string Key="Defense.CardToggle.Validation";
    private static int checks;
    private static double started;
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    private static GameplayHand Hand=>Find<GameplayHand>();
    private static GameFlow Flow=>Find<GameFlow>();
    private static TurretPlacementController Placement=>Find<TurretPlacementController>();
    private static SkillCastingController Casting=>Find<SkillCastingController>();
    private static ElixirSystem Elixir=>Find<ElixirSystem>();
    private static ResourceWallet Wallet=>Find<ResourceWallet>();
    private static SkillAreaVisual Aim=>GameObject.Find("Skill Aim Indicator").GetComponent<SkillAreaVisual>();
    private static bool AimVisible=>Aim.IsVisible;
    static CardSelectionToggleValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){checks=0;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string m,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,m+"\n"+trace);}
    private static void Check(bool pass,string message){if(!pass)throw new Exception(message);checks++;Debug.Log("TOGGLE PASS: "+message);}
    private static Button ButtonAt(int i)=>GameObject.Find("Hand Card "+(i+1)).GetComponent<Button>();
    private static Vector2 Point(int i)=>RectTransformUtility.WorldToScreenPoint(null,ButtonAt(i).transform.position);
    private static Vector2 World(Vector3 p)=>Camera.main.WorldToScreenPoint(p);
    // Exercise real uGUI press/release matching and its existing Button listener, not Button.onClick.Invoke.
    // Only this Editor test uses reflection to drive the installed legacy module's protected dispatcher.
    private static void Mouse(PointerEventData pointer,Vector2 position,PointerEventData.FramePressState state){
        pointer.position=position;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);pointer.pointerCurrentRaycast=hits.FirstOrDefault();
        var method=typeof(StandaloneInputModule).GetMethod("ProcessMousePress",BindingFlags.Instance|BindingFlags.NonPublic);
        if(method==null)throw new InvalidOperationException("Installed uGUI mouse dispatcher not available.");
        method.Invoke(EventSystem.current.currentInputModule,new object[]{new PointerInputModule.MouseButtonEventData{buttonData=pointer,buttonState=state}});
    }
    private static PointerEventData Pointer()=>new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=-101};
    private static void Click(int i){
        Check(ButtonAt(i).interactable,"Hand button is interactive");Canvas.ForceUpdateCanvases();var p=Point(i);var pointer=Pointer();
        Mouse(pointer,p,PointerEventData.FramePressState.Pressed);
        Check(GameplayPointer.IsOverUI(p)&&!Placement.ProcessPointer(p,true),"Card press hits uGUI and cannot click through into Tower placement");
        Casting.ProcessPointer(p,true,false);Casting.ProcessPointer(p,false,true);
        Mouse(pointer,p,PointerEventData.FramePressState.Released);
    }
    private static void Visuals(){
        for(int i=0;i<3;i++)Check(ButtonAt(i).image.color==(Hand.Cycle.Hand[i]==Hand.SelectedCard?new Color(.2f,.55f,.36f):Hand.Cycle.Hand[i].Definition.Color),"Exactly the authoritative runtime selection controls card tint: "+i);
        if(Hand.SelectedCard==null)Check(!UnityEngine.Object.FindObjectsByType<Text>().Any(t=>t.text.StartsWith("Selected: ")),"Deselect clears selected-card context text");
    }
    private sealed class Snapshot {
        private readonly RuntimeCard[] hand=Hand.Cycle.Hand.ToArray(),queue=Hand.Cycle.UpcomingQueue.ToArray();
        private readonly float elixir=Elixir.Current;
        private readonly int resources=Wallet.Balance,towers=UnityEngine.Object.FindObjectsByType<Tower>().Length,areas=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>().Length,pending=Casting.PendingCastCount;
        public void Unchanged(string why)=>Check(Hand.Cycle.Hand.SequenceEqual(hand)&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue)&&Elixir.Current==elixir&&Wallet.Balance==resources&&UnityEngine.Object.FindObjectsByType<Tower>().Length==towers&&UnityEngine.Object.FindObjectsByType<PersistentSlowArea>().Length==areas&&Casting.PendingCastCount==pending,why+": no spending, world action or Hand/queue mutation");
    }
    private static void Verify(){
        Find<FixedIntervalSpawner>().enabled=false;foreach(var enemy in Find<EnemyRegistry>().Enemies.ToArray())UnityEngine.Object.DestroyImmediate(enemy.gameObject);
        var deck=Find<TopDownInteractionSandbox>().Deck;var original=deck.Slots.ToArray();
        Hand.CancelSelection();while(!Hand.Cycle.Hand.Any(c=>c.Definition is SkillCardDefinition))Hand.Cycle.Use(Hand.Cycle.Hand[0]);Hand.CancelSelection();
        int tower=Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition is TowerCardDefinition),skill=Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition is SkillCardDefinition);
        Check(tower>=0&&skill>=0,"Local mixed deck fixture exposes both Tower and Skill");
        var snapshot=new Snapshot();int changes=0;Action changed=()=>changes++;Hand.Changed+=changed;
        Click(tower);Check(Hand.SelectedCard==Hand.Cycle.Hand[tower]&&Placement.Selected==((TowerCardDefinition)Hand.SelectedCard.Definition).Tower&&changes==1,"One Tower click selects once through authoritative model");Visuals();
        var slot=Find<BattlefieldMap2D>().PlacementSlots[0];Placement.ProcessPointer(World(slot.PlacementPoint.position),false);Check(slot.IsHovered,"Selected Tower retains normal available-slot hover feedback");
        Click(tower);Check(Hand.SelectedCard==null&&Placement.Selected==null&&!slot.IsHovered&&changes==2,"Second Tower click clears selection, placement and residual slot preview immediately");Visuals();snapshot.Unchanged("Tower deselect");
        Click(skill);Check(Hand.SelectedCard==Hand.Cycle.Hand[skill]&&Placement.Selected==null&&!Casting.IsAiming&&!AimVisible,"Skill click selects without beginning aim or cast");Visuals();
        Click(skill);Check(Hand.SelectedCard==null&&!Casting.IsAiming&&!AimVisible,"Second Skill click deselects and leaves no targeting preview");Visuals();snapshot.Unchanged("Skill deselect");
        Click(tower);Click(skill);Check(Hand.SelectedCard==Hand.Cycle.Hand[skill]&&Placement.Selected==null,"Different-card click directly switches Tower to Skill");Visuals();
        Click(tower);Check(Hand.SelectedCard==Hand.Cycle.Hand[tower],"Different-card click directly switches Skill to Tower");Visuals();snapshot.Unchanged("Direct switching");
        Hand.CancelSelection();Click(skill);var selected=Hand.SelectedCard;var world=World(Vector3.zero);var pointer=Pointer();
        Mouse(pointer,world,PointerEventData.FramePressState.Pressed);Casting.ProcessPointer(world,true,false);Check(Casting.IsAiming&&AimVisible,"Selected Skill still begins normal hold-to-aim on the gameplay surface");
        Casting.ProcessPointer(Point(skill),false,false);Check(Casting.IsAiming&&!AimVisible,"Moving held pointer over card hides aim without starting a new click");
        // Release order may vary by component execution; test both orders on consecutive gestures.
        Mouse(pointer,Point(skill),PointerEventData.FramePressState.Released);Casting.ProcessPointer(Point(skill),false,true);
        Check(Hand.SelectedCard==selected&&!Casting.IsAiming&&!AimVisible,"World press then origin-card release cancels aim without toggling still-selected Skill");snapshot.Unchanged("Release-to-card cancellation");
        Click(skill);Check(Hand.SelectedCard==null,"A subsequent deliberate Skill click deselects after release cancellation");
        Click(skill);pointer=Pointer();Mouse(pointer,world,PointerEventData.FramePressState.Pressed);Casting.ProcessPointer(world,true,false);
        Casting.ProcessPointer(Point(tower),false,true);Mouse(pointer,Point(tower),PointerEventData.FramePressState.Released);
        Check(Hand.SelectedCard==selected&&!Casting.IsAiming,"Release over different Card also cancels without selecting that Card, independent of callback order");snapshot.Unchanged("Release-to-other-card cancellation");
        Hand.CancelSelection();Check(!Hand.ToggleSelection(-1)&&!Hand.ToggleSelection(3)&&Hand.SelectedCard==null,"Invalid indices cannot change selection");
        Click(tower);var before=Hand.SelectedCard;Flow.TogglePause();Check(!Hand.ToggleSelection(tower)&&Hand.SelectedCard==before,"Pause blocks click toggling and preserves established selection policy");Flow.TogglePause();
        // Reselect after deselection, then commit using the unchanged production screen-coordinate path.
        Click(tower);Click(tower);Wallet.Initialize(10000);Elixir.Advance(100);var card=Hand.SelectedCard;var hand=Hand.Cycle.Hand.ToArray();var queue=Hand.Cycle.UpcomingQueue.ToArray();float energy=Elixir.Current;
        Check(Placement.ProcessPointer(World(slot.PlacementPoint.position),true)&&slot.IsOccupied,"Valid Tower placement works after deselect/reselect");slot.Occupant.enabled=false;
        Check(Hand.SelectedCard==null&&Placement.Selected==null&&Wallet.Balance==10000-((TowerCardDefinition)card.Definition).Tower.Cost&&Elixir.Current==energy-card.Definition.ElixirCost&&Hand.Cycle.Hand.SequenceEqual(hand.Where(c=>c!=card).Concat(new[]{queue[0]})),"Tower use still spends exactly once, rotates once and clears stale selection");Visuals();
        Hand.Initialize(deck,Placement,Flow);while(!Hand.Cycle.Hand.Any(c=>c.Definition is SkillCardDefinition))Hand.Cycle.Use(Hand.Cycle.Hand[0]);Hand.CancelSelection();skill=Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition is SkillCardDefinition);
        Click(skill);Click(skill);Click(skill);Elixir.Advance(100);card=Hand.SelectedCard;hand=Hand.Cycle.Hand.ToArray();queue=Hand.Cycle.UpcomingQueue.ToArray();energy=Elixir.Current;
        Check(Casting.ProcessPointer(world,true,false)==CardUseResult.Cancelled&&Casting.IsAiming,"Skill reselect still waits for a valid world release");
        Check(Casting.ProcessPointer(world,false,true)==CardUseResult.Success&&!Casting.IsAiming&&Hand.SelectedCard==null&&!AimVisible,"Valid Skill cast works after deselect/reselect and clears its preview");
        Check(Elixir.Current==energy-card.Definition.ElixirCost&&Hand.Cycle.Hand.SequenceEqual(hand.Where(c=>c!=card).Concat(new[]{queue[0]}))&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue.Skip(1).Concat(new[]{card})),"Successful Skill use preserves exact payment and queue rotation");Visuals();
        Hand.Select(0);Hand.Initialize(deck,Placement,Flow);Check(Hand.SelectedCard==null&&Placement.Selected==null&&!Casting.IsAiming,"Hand reset creates no stale selection or preview");Visuals();
        Hand.Select(0);Flow.GameOver();Check(!Hand.ToggleSelection(0)&&!Casting.IsAiming,"Terminal Game Over blocks toggling and clears active aim under existing rules");
        Check(deck.Slots.SequenceEqual(original)&&PlayerSession.Presets==null&&PlayerSession.Currency==null,"Selection tests preserve the local source deck and never initialize persistent player services");Hand.Changed-=changed;
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Card toggle acceptance timeout");
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||Find<TopDownInteractionSandbox>()==null||GameObject.Find("Hand Card 1")==null)return;
        Verify();Finish(true,checks+" input-routing/toggle assertions");
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Finish(bool pass,string message){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Card_Toggle_Result.txt",(pass?"PASS: ":"FAIL: ")+message);if(pass)Debug.Log("CARD_TOGGLE_PASSED "+message);else Debug.LogError("CARD_TOGGLE_FAILED "+message);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
}
}
