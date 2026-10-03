using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
namespace Defense.Editor {
[InitializeOnLoad]
public static class PrototypeValidation {
    private const string Key = "Defense.Validation.Running";
    private static int step, assertions, turretIndex;
    private static double started, deadline;
    private static CardDefinition[] persistentSnapshot;
    private static RuntimeCardCycle firstRun, failedRun;
    private static StageDefinition retryStage;
    private static int retryStageIndex;
    private static Enemy[] failedEnemies;
    private static Turret[] failedTurrets;
    private static bool[] progressionBeforeRetry;
    private static Enemy pausedEnemy, combatEnemy;
    private static Vector3 pausedPosition;
    private static float pausedHP;
    private static int pausedCount, balanceBeforeBase;
    private static Turret combatTurret;
    private static EnemyDefinition durableEnemy;
    private static readonly List<float> hits = new List<float>();
    private static readonly List<float> healths = new List<float>();
    private static string lastMessage;
    static PrototypeValidation() { if (SessionState.GetBool(Key,false)) Attach(); }
    [MenuItem("Defense/Run Prototype Validation (Play Mode)")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before validation.");
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");
        Attach(); EditorApplication.isPlaying = true;
    }
    private static void Attach() {
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
    }
    private static void OnLog(string message, string trace, LogType type) {
        if (trace.Contains("UnityEditor.Search.SearchDatabase") && !trace.Contains("Defense.")) {
            Debug.LogWarning("Editor Search indexing issue (outside gameplay): "+message); return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false,message+"\n"+trace);
    }
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Check(bool value, string message) {
        if (!value) throw new Exception(message);
        assertions++; Debug.Log("PASS: "+message);
    }
    private static void Click(string text) {
        var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<Text>().text==text);
        Check(button.interactable,"Button available: "+text); button.onClick.Invoke();
    }
    private static void Card(int index) {
        var button = GameObject.Find("Hand Card "+(index+1)).GetComponent<Button>();
        Check(button.interactable,"Hand card selectable"); button.onClick.Invoke();
    }
    private static void Wait(int next, double seconds=.3) { step = next; deadline = EditorApplication.timeSinceStartup+seconds; }
    private static void Tick() {
        if (!SessionState.GetBool(Key,false)) return;
        try {
            if (EditorApplication.timeSinceStartup-started > 180) throw new Exception("Validation timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<deadline) return;
            switch (step) {
                case 0:
                    if (Find<MainMenuUI>()==null || Find<Button>()==null) return;
                    TestModels();
                    Click("Settings"); Click("Back"); Click("Start Game");
                    TestLobbyAndDeck();
                    Find<LobbyUI>().HandleNavigation(-1,200); Find<LobbyUI>().Carousel.Advance(1);
                    Check(PlayerSession.Selection.SelectedStageIndex==0,"A navigation moves backward to first stage");
                    Click("Enter Stage"); Wait(1); break;
                case 1:
                    if (Find<EnemyRegistry>()==null || Find<EnemyRegistry>().Enemies.Count==0) return;
                    Check(SceneManager.GetActiveScene().name=="TestStage","Unlocked centered stage loads");
                    Check(Find<BaseHealth>().Current==10 && Find<ResourceWallet>().Balance==150,"Stage starts with HP and resources");
                    Check(Find<EnemyRegistry>().Enemies.Count>0,"Fixed interval spawns enemies");
                    TabletopStageValidation.Run(Check);
                    PlacementCameraValidation.Run(Check);
                    TabletopStageValidation.CapturePreview();
                    Find<FixedIntervalSpawner>().enabled=false;
                    foreach (var enemy in Find<EnemyRegistry>().Enemies.ToArray()) enemy.ReceiveDamage(10000);
                    TestPlacementCycle(); TestRewardsAndHealth();
                    CardSkillValidation.Gameplay(Check);
                    foreach (var turret in UnityEngine.Object.FindObjectsByType<Turret>(FindObjectsSortMode.None)) turret.enabled=false;
                    pausedEnemy=Find<EnemySpawner>().Spawn(PlayerSession.Catalog.Stages[0].Enemy);
                    Click("Pause");
                    pausedPosition=pausedEnemy.transform.position; pausedHP=pausedEnemy.CurrentHealth; pausedCount=Find<EnemyRegistry>().Enemies.Count;
                    Check(Find<GameFlow>().State==GameplayState.Paused && Time.timeScale==0,"Pause freezes game time");
                    Check(!Find<GameplayHand>().Select(0),"Pause blocks Hand selection");
                    Check(Find<EnemySpawner>().Spawn(PlayerSession.Catalog.Stages[0].Enemy)==null,"Pause blocks spawning");
                    Click("Settings"); Click("Back");
                    Check(Find<GameFlow>().State==GameplayState.Paused,"Settings Back stays paused"); Wait(2,.5); break;
                case 2:
                    TabletopStageValidation.VerifyFixedCamera(Check);
                    Check(pausedEnemy.transform.position==pausedPosition && pausedEnemy.CurrentHealth==pausedHP && Find<EnemyRegistry>().Enemies.Count==pausedCount,"Paused movement, combat and spawning stay frozen");
                    Click("Resume"); Check(Time.timeScale==1,"Resume restores time");
                    balanceBeforeBase=Find<ResourceWallet>().Balance;
                    pausedEnemy.GetComponent<EnemyPathFollower>().Advance(100);
                    pausedEnemy.GetComponent<EnemyPathFollower>().Advance(100);
                    Check(Find<BaseHealth>().Current==9 && Find<ResourceWallet>().Balance==balanceBeforeBase,"Base arrival damages once and grants no reward");
                    Click("Pause"); Click("Exit"); Click("No");
                    Check(Find<GameFlow>().State==GameplayState.Paused,"Exit cancellation remains paused");
                    Click("Exit"); Click("Yes"); Wait(3); break;
                case 3:
                    Check(Find<LobbyUI>()!=null && Time.timeScale==1,"Exit returns to Lobby");
                    Check(PlayerSession.Deck.Slots.SequenceEqual(persistentSnapshot),"Gameplay and scene exit preserve T1-T6");
                    Click("Deck"); Check(Find<DeckEditorUI>()!=null,"Deck editor reopens with session deck"); Click("Back to Lobby");
                    Click("Enter Stage"); Wait(4); break;
                case 4:
                    Check(Find<GameplayHand>().Cycle!=firstRun,"Stage reentry creates a fresh runtime cycle");
                    Check(Find<GameplayHand>().Cycle.InitialOrder.All(c=>!firstRun.InitialOrder.Contains(c)),"New run owns new card instances");
                    Check(Find<ResourceWallet>().Balance==150 && Find<BaseHealth>().Current==10,"Reentry resets battle HP/resources");
                    Check(PlayerSession.Deck.Slots.SequenceEqual(persistentSnapshot),"New shuffle preserves persistent slot positions");
                    Find<FixedIntervalSpawner>().enabled=false;
                    BeginCombat(); Wait(5,.3); break;
                case 5:
                    Check(combatEnemy.CurrentHealth==1000,"Turret excludes enemy beyond configured range");
                    combatEnemy.transform.position=combatTurret.transform.position+Vector3.right*(PlayerSession.Catalog.AvailableTowers[turretIndex].AttackRange-.1f);
                    Wait(6,.01); break;
                case 6:
                    if (hits.Count<3) return;
                    var definition=PlayerSession.Catalog.AvailableTowers[turretIndex];
                    Check(Mathf.Approximately(healths[0],1000-definition.Damage) && Mathf.Approximately(healths[1],1000-2*definition.Damage),"Configured turret damage: "+definition.DisplayName);
                    Check(hits[1]-hits[0]>=definition.AttackCooldown-.03f && hits[2]-hits[1]>=definition.AttackCooldown-.03f,"Configured turret cooldown: "+definition.DisplayName);
                    combatTurret.enabled=false;
                    UnityEngine.Object.Destroy(combatTurret.gameObject);
                    int balance=Find<ResourceWallet>().Balance;
                    combatEnemy.ReceiveDamage(10000);
                    Check(Find<ResourceWallet>().Balance==balance+durableEnemy.ResourceReward,"Combat death grants resource reward");
                    UnityEngine.Object.Destroy(durableEnemy);
                    turretIndex++;
                    if (turretIndex<PlayerSession.Catalog.AvailableTowers.Count) { BeginCombat(); Wait(5,.3); }
                    else { PrepareRetry(); Wait(7); }
                    break;
                case 7:
                    Check(Find<GameFlow>().State==GameplayState.GameOver && Time.timeScale==0,"Base depletion enters Game Over");
                    Check(!PlayerSession.Progression.IsUnlocked(1),"Game Over does not unlock next stage");
                    Check(!Find<GameplayHand>().Select(0) && Find<EnemySpawner>().Spawn(PlayerSession.Catalog.Stages[0].Enemy)==null,"Game Over blocks selecting and spawning");
                    Find<GameFlow>().TogglePause(); Check(Find<GameFlow>().State==GameplayState.GameOver,"Game Over cannot resume");
                    Click("Retry"); Wait(10); break;
                case 8:
                    Check(PlayerSession.Deck.Slots.SequenceEqual(persistentSnapshot),"All tests leave persistent deck unchanged after gameplay");
                    Check(PlayerSession.CompleteActiveStage() && PlayerSession.Progression.IsUnlocked(1),"Explicit completion hook unlocks next stage");
                    var lobby=Find<LobbyUI>(); lobby.HandleNavigation(1,300); lobby.Carousel.Advance(1);
                    Check(PlayerSession.Selection.SelectedStageIndex==1 && PlayerSession.Progression.IsUnlocked(1),"Unlocked stage can be selected");
                    Click("Enter Stage"); Wait(9); break;
                case 9:
                    Check(PlayerSession.ActiveStage==PlayerSession.Catalog.Stages[1] && Find<GameplayHand>()!=null,"Next stage entry reuses playable scene with selected definition");
                    PrepareRetry(); Click("Retry"); Wait(12); break;
                case 10:
                    CheckRetry();
                    Wait(11,retryStage.SpawnInterval+.2f); break;
                case 11:
                    Check(Find<EnemyRegistry>().Enemies.Count>0,"Retry restarts periodic spawning");
                    Find<BaseHealth>().ReceiveDamage(100); Click("Lobby"); Wait(8); break;
                case 12:
                    CheckRetry();
                    Check(Find<ResourceWallet>().Balance==175 && PlayerSession.Selection.SelectedStageIndex==1,"Retry keeps non-first stage definition and its own starting resources");
                    var mixedEditor = new DeckEditorController(PlayerSession.Deck,PlayerSession.Catalog.AvailableCards);
                    var skillCards = PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>().ToArray();
                    Check(mixedEditor.Replace(4,skillCards[0]) && mixedEditor.Replace(5,skillCards[1]),"Session deck accepts both prototype skills before mixed Retry");
                    persistentSnapshot = PlayerSession.Deck.Slots.ToArray();
                    Find<GameplayHand>().Initialize(PlayerSession.Deck,Find<TurretPlacementController>(),Find<GameFlow>());
                    PrepareRetry(); Click("Retry"); Wait(13); break;
                case 13:
                    CheckRetry();
                    Check(Find<GameplayHand>().Cycle.InitialOrder.Count(c=>c.Definition is SkillCardDefinition)==2,"Retry rebuilds the same mixed tower/skill deck");
                    Finish(true,"Assertions: "+assertions); break;
            }
        } catch (Exception e) { Finish(false,e.ToString()); }
    }
    private static void TestModels() {
        var catalog=PlayerSession.Catalog;
        PatchInvariantValidation.Run(catalog,Check);
        CardSkillValidation.Models(catalog,Check);
        Check(catalog.Stages.Count==10 && catalog.Stages.Select(s=>s.DisplayName).SequenceEqual(Enumerable.Range(1,10).Select(i=>"1-"+i)),"Catalog contains stages 1-1 through 1-10 in order");
        Check(catalog.AvailableTowers.Count==15 && catalog.AvailableTowers.Distinct().Count()==15,"Catalog contains fifteen distinct test tower definitions");
        Check(catalog.AvailableTowers.All(t=>t.Prefab!=null && t.Targeting!=null && t.Damage>0 && t.AttackRange>0 && t.AttackCooldown>0 && t.Cost>0),"All test towers have valid combat settings and references");
        var progression=new StageProgressionState(4);
        Check(progression.IsUnlocked(0) && !progression.IsUnlocked(1),"Only first stage initially unlocked");
        Check(!progression.Complete(2) && progression.Complete(0) && progression.IsUnlocked(1) && !progression.IsUnlocked(2),"Sequential completion unlocks only next stage");
        var repeat=new KeyRepeat();
        Check(repeat.Poll(1,0,.4f,.15f)==1 && repeat.Poll(1,.39f,.4f,.15f)==0,"Held key moves immediately then waits initial delay");
        Check(repeat.Poll(1,.4f,.4f,.15f)==1 && repeat.Poll(1,.54f,.4f,.15f)==0 && repeat.Poll(1,.56f,.4f,.15f)==1,"Held key uses fixed repeat interval");
        Check(repeat.Poll(-1,.57f,.4f,.15f)==-1 && repeat.Poll(0,.6f,.4f,.15f)==0 && repeat.Poll(-1,.61f,.4f,.15f)==-1,"Direction change and release reset repeat");
        var deck=new PlayerDeck(catalog.InitialDeck); var snapshot=deck.Slots.ToArray();
        var editor=new DeckEditorController(deck,catalog.AvailableCards);
        Check(!editor.Replace(-1,catalog.AvailableCards[0]) && !editor.Replace(6,catalog.AvailableCards[0]) && !editor.Replace(0,null) && deck.Slots.SequenceEqual(snapshot),"Invalid deck edits are atomic");
        var random=new CountingRandom(1234); var cycle=new RuntimeCardCycle(deck,random);
        Check(random.Draws==5,"Six-entry Fisher-Yates shuffles exactly once with five draws");
        Check(cycle.InitialOrder.Select(c=>c.SourceSlot).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,6)),"Every unique persistent tower occurs exactly once");
        Check(cycle.Hand.SequenceEqual(cycle.InitialOrder.Take(3)) && cycle.UpcomingQueue.SequenceEqual(cycle.InitialOrder.Skip(3)),"Initial shuffled order splits into Hand and Queue");
        Check(cycle.InitialOrder.Select(c=>c.Definition).Distinct().Count()==6 && cycle.Hand.Select(c=>c.Definition).Distinct().Count()==3,"Runtime copy and initial Hand have unique tower definitions");
        Check(deck.Slots.SequenceEqual(snapshot),"Shuffle leaves T1-T6 unchanged");
        var initial=cycle.InitialOrder.ToArray();
        for (int iteration=0;iteration<30;iteration++) {
            var before=cycle.Hand.ToArray(); var queue=cycle.UpcomingQueue.ToArray(); int used=iteration%3;
            Check(cycle.Use(before[used]) && cycle.Hand.SequenceEqual(before.Where((_,i)=>i!=used).Concat(new[]{queue[0]})) && cycle.UpcomingQueue.SequenceEqual(queue.Skip(1).Concat(new[]{before[used]})),"Deterministic cycle step "+iteration);
        }
        Check(random.Draws==5 && cycle.InitialOrder.SequenceEqual(initial) && deck.Slots.SequenceEqual(snapshot),"Repeated cycling never reshuffles or changes PlayerDeck");
        var hand=cycle.Hand.ToArray(); var upcoming=cycle.UpcomingQueue.ToArray();
        Check(!cycle.Use(new RuntimeCard(0,snapshot[0])) && cycle.Hand.SequenceEqual(hand) && cycle.UpcomingQueue.SequenceEqual(upcoming),"Foreign/non-hand card cannot mutate cycle");
        var orders=new HashSet<string>();
        for (int seed=0;seed<20;seed++) orders.Add(string.Join(",",new RuntimeCardCycle(deck,new System.Random(seed)).InitialOrder.Select(c=>c.SourceSlot)));
        Check(orders.Count>1,"Independent seeds produce varying orders without T1-first policy");
    }
    private static void TestLobbyAndDeck() {
        var lobby=Find<LobbyUI>(); Check(lobby!=null,"Start Game opens Lobby");
        Check(Find<LobbyDeckPreview>()!=null && PlayerSession.Deck.Slots.Count==6,"Lobby displays shared six-slot deck");
        for (int i=0;i<20;i++) PlayerSession.Selection.Move(1);
        lobby.Carousel.Advance(1);
        Check(PlayerSession.Selection.SelectedStageIndex==9 && lobby.Carousel.IsSettled,"Carousel reaches 1-10 and clamps at right edge");
        for (int i=0;i<20;i++) PlayerSession.Selection.Move(-1);
        lobby.Carousel.Advance(1);
        Check(PlayerSession.Selection.SelectedStageIndex==0 && lobby.Carousel.IsSettled,"Carousel returns to 1-1 and clamps at left edge");
        lobby.HandleNavigation(1,100);
        Check(PlayerSession.Selection.SelectedStageIndex==1 && lobby.Carousel.VisualIndex==0,"D retargets carousel without teleporting");
        lobby.Carousel.Advance(.1f); Check(lobby.Carousel.VisualIndex>0 && lobby.Carousel.VisualIndex<1,"Carousel position interpolates");
        lobby.Carousel.Advance(1); Check(lobby.Carousel.IsSettled,"Selected stage settles at center");
        Check(!lobby.TryEnter() && SceneManager.GetActiveScene().name=="MainMenu","Locked stage cannot load");
        var left = GameObject.Find("Stage Card 0").GetComponent<Button>();
        Check(left.interactable,"Left side stage is mouse-navigable"); left.onClick.Invoke();
        Check(PlayerSession.Selection.SelectedStageIndex==0 && lobby.Carousel.VisualIndex==1,"Mouse previous uses same step and animation as A");
        lobby.HandleNavigation(0,101);
        lobby.HandleNavigation(1,102);
        Check(PlayerSession.Selection.SelectedStageIndex==1,"Keyboard retarget during mouse animation shares one state");
        lobby.Carousel.Advance(1);
        var center = GameObject.Find("Stage Card 1").GetComponent<Button>(); center.onClick.Invoke();
        Check(!center.interactable && PlayerSession.Selection.SelectedStageIndex==1,"Clicking center does not navigate");
        var right = GameObject.Find("Stage Card 2").GetComponent<Button>();
        Check(right.interactable,"Right side stage is mouse-navigable"); right.onClick.Invoke();
        Check(PlayerSession.Selection.SelectedStageIndex==2 && lobby.Carousel.VisualIndex==1,"Mouse next uses same step and animation as D");
        right.onClick.Invoke();
        Check(PlayerSession.Selection.SelectedStageIndex==2,"Repeated mouse input during animation does not double-step");
        lobby.Carousel.Advance(1);
        GameObject.Find("Stage Card 1").GetComponent<Button>().onClick.Invoke(); lobby.Carousel.Advance(1);
        Click("Deck");
        Check(Find<DeckEditorUI>()!=null && UnityEngine.Object.FindObjectsByType<DeckSlotDrop>(FindObjectsSortMode.None).Length==6,"Deck editor has six fixed drop targets");
        var available=UnityEngine.Object.FindObjectsByType<DeckCardDrag>(FindObjectsSortMode.None);
        Check(available.Length==11 && available.All(c=>!PlayerSession.Deck.Contains(c.Definition)),"Deck editor only shows the eleven cards outside PlayerDeck");
        var source=available.Single(c=>c.Definition==PlayerSession.Catalog.AvailableCards[14]);
        var targets=UnityEngine.Object.FindObjectsByType<DeckSlotDrop>(FindObjectsSortMode.None).OrderBy(t=>t.name).ToArray();
        var before=PlayerSession.Deck.Slots.ToArray();
        var data=new PointerEventData(EventSystem.current) { pointerDrag=source.gameObject, button=PointerEventData.InputButton.Left, position=new Vector2(200,200) };
        ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.endDragHandler);
        Check(PlayerSession.Deck.Slots.SequenceEqual(before),"Cancelled/outside drag changes no deck data");
        ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(targets[2].gameObject,data,ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.endDragHandler);
        Check(PlayerSession.Deck.Slots[2]==source.Definition && Enumerable.Range(0,6).Where(i=>i!=2).All(i=>PlayerSession.Deck.Slots[i]==before[i]),"Drag/drop replaces only T3");
        var visible = UnityEngine.Object.FindObjectsByType<DeckCardDrag>(FindObjectsSortMode.None);
        Check(visible.Length==11 && visible.Any(c=>c.Definition==before[2]) && visible.All(c=>!PlayerSession.Deck.Contains(c.Definition)),"Available UI immediately adds outgoing and removes incoming definition");
        var edited = PlayerSession.Deck.Slots.ToArray();
        ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(targets[0].gameObject,data,ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(source.gameObject,data,ExecuteEvents.endDragHandler);
        Check(PlayerSession.Deck.Slots.SequenceEqual(edited) && edited.Distinct().Count()==6,"Stale/hidden drag source cannot duplicate an assigned tower");
        // Swap T3 back and forth through actual UI events, checking refresh after every operation.
        for (int i=0;i<4;i++) {
            var next = UnityEngine.Object.FindObjectsByType<DeckCardDrag>(FindObjectsSortMode.None).First(c=>c.Definition is TowerCardDefinition);
            var incoming = next.Definition; var outgoing = PlayerSession.Deck.Slots[2];
            data.pointerDrag = next.gameObject;
            ExecuteEvents.Execute(next.gameObject,data,ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(targets[2].gameObject,data,ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(next.gameObject,data,ExecuteEvents.endDragHandler);
            visible = UnityEngine.Object.FindObjectsByType<DeckCardDrag>(FindObjectsSortMode.None);
            Check(PlayerSession.Deck.Slots[2]==incoming && PlayerSession.Deck.Slots.Distinct().Count()==6 && visible.Any(c=>c.Definition==outgoing)
                && visible.All(c=>!PlayerSession.Deck.Contains(c.Definition)),"Repeated drag/drop refresh keeps Available and Deck disjoint");
        }
        Check(UnityEngine.Object.FindObjectsByType<Turret>(FindObjectsSortMode.None).Length==0,"Deck UI creates no combat objects");
        persistentSnapshot=PlayerSession.Deck.Slots.ToArray();
        Click("Settings"); Click("Back"); Check(Find<DeckEditorUI>()!=null && PlayerSession.Deck.Slots.SequenceEqual(persistentSnapshot),"Deck Settings returns without losing deck");
        Click("Back to Lobby");
        Check(PlayerSession.Selection.SelectedStageIndex==1 && Find<LobbyUI>().Carousel.VisualIndex==1,"Deck return preserves carousel selection and position");
        var preview=GameObject.Find("Deck Preview T3").GetComponentInChildren<Text>();
        Check(preview.text.Contains(persistentSnapshot[2].DisplayName),"Lobby preview observes edited T3");
        Click("Settings"); Click("Back");
        Check(PlayerSession.Selection.SelectedStageIndex==1 && Find<LobbyUI>()!=null,"Lobby Settings preserves selected stage");
    }
    private static TowerPlacementSlot[] Slots() => UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None).OrderBy(s=>s.name).ToArray();
    private static void TestPlacementCycle() {
        var hand=Find<GameplayHand>(); var placement=Find<TurretPlacementController>(); var wallet=Find<ResourceWallet>();
        firstRun=hand.Cycle;
        Check(firstRun.InitialOrder.All(c=>c.Definition==persistentSnapshot[c.SourceSlot]),"Stage Hand reads persistent edited PlayerDeck");
        Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.name.StartsWith("Hand Card "))==3 && !UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>().text=="Turrets"),"Only three Hand cards replace turret catalog UI");
        placement.Message+=value=>lastMessage=value;
        Card(1); var before=hand.Cycle.Hand.ToArray(); var queue=hand.Cycle.UpcomingQueue.ToArray();
        wallet.Initialize(0);
        Check(!placement.TryPlace(null) && lastMessage=="You cannot place a turret here.","Invalid slot rejected");
        Check(!placement.TryPlace(Slots()[0]) && lastMessage=="Not enough resources." && wallet.Balance==0,"Insufficient resources rejected without spending");
        Check(hand.Cycle.Hand.SequenceEqual(before) && hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Failed placement leaves Hand and Queue intact");
        hand.CancelSelection();
        Check(!placement.TryPlace(Slots()[0]) && hand.Cycle.Hand.SequenceEqual(before) && hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Selection cancellation does not cycle cards");
        wallet.Initialize(1000); Card(1);
        var slot=Slots()[0]; var available=slot.GetComponentInChildren<PlacementSlotView>().CurrentColor; slot.SetHovered(true);
        Check(slot.GetComponentInChildren<PlacementSlotView>().CurrentColor!=available,"Available slot hover highlights"); slot.SetHovered(false);
        Check(slot.GetComponentInChildren<PlacementSlotView>().CurrentColor==available,"Hover exit restores color");
        Check(placement.TryPlace(slot) && wallet.Balance==1000-((TowerCardDefinition)before[1].Definition).Tower.Cost,"Actual placement spends selected card cost");
        Check(hand.Cycle.Hand.SequenceEqual(new[]{before[0],before[2],queue[0]}) && hand.Cycle.UpcomingQueue.SequenceEqual(new[]{queue[1],queue[2],before[1]}),"Success draws queue front and appends used card to queue back");
        Check(hand.SelectedCard==null && placement.Selected==null,"Success clears selection to avoid stale card reuse");
        Check(slot.GetComponentInChildren<PlacementSlotView>().CurrentColor!=available,"Occupied slot remains visually distinct");
        Card(0); before=hand.Cycle.Hand.ToArray(); queue=hand.Cycle.UpcomingQueue.ToArray(); int balance=wallet.Balance;
        Check(!placement.TryPlace(slot) && lastMessage=="A turret is already placed here." && wallet.Balance==balance && hand.Cycle.Hand.SequenceEqual(before) && hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Occupied placement preserves cost and full cycle");
        Check(PlayerSession.Deck.Slots.SequenceEqual(persistentSnapshot),"Successful cycling never edits persistent deck");
    }
    private static void TestRewardsAndHealth() {
        var spawner=Find<EnemySpawner>(); var wallet=Find<ResourceWallet>();
        foreach (int reward in new[]{7,23,0}) {
            var data=UnityEngine.Object.Instantiate(PlayerSession.Catalog.Stages[0].Enemy);
            var so=new SerializedObject(data); so.FindProperty("resourceReward").intValue=reward; so.ApplyModifiedPropertiesWithoutUndo();
            var enemy=spawner.Spawn(data); int before=wallet.Balance; var bar=enemy.GetComponent<EnemyHealthBar>();
            Check(bar.IsVisible && bar.NormalizedHealth==1,"HP bar starts full");
            enemy.ReceiveDamage(1); Check(wallet.Balance==before && Mathf.Approximately(bar.NormalizedHealth,(enemy.MaximumHealth-1)/enemy.MaximumHealth),"Nonlethal damage updates HP bar without reward");
            enemy.ReceiveDamage(10000); enemy.ReceiveDamage(10000);
            Check(wallet.Balance==before+reward && !bar.IsVisible && !Find<EnemyRegistry>().Enemies.Contains(enemy),"Death pays once and removes target/HP bar");
            UnityEngine.Object.Destroy(data);
        }
        Enemy near=spawner.Spawn(PlayerSession.Catalog.Stages[0].Enemy), far=spawner.Spawn(PlayerSession.Catalog.Stages[0].Enemy);
        near.transform.position=Vector3.right; far.transform.position=Vector3.right*2;
        var strategy=PlayerSession.Catalog.AvailableTowers[0].Targeting;
        Check(strategy.Select(Vector3.zero,3,Find<EnemyRegistry>().Enemies)==near,"Closest targeting selects physical nearest");
        near.transform.position=Vector3.right*4;
        Check(strategy.Select(Vector3.zero,3,Find<EnemyRegistry>().Enemies)==far,"Target reacquisition respects range");
        near.ReceiveDamage(10000); far.ReceiveDamage(10000);
    }
    private static void BeginCombat() {
        var definition=PlayerSession.Catalog.AvailableTowers[turretIndex];
        // Isolate each combat variant using a test-only deck, without modifying session T1-T6.
        var testDeck = new[]{PlayerSession.Catalog.AvailableCards[turretIndex]}.Concat(PlayerSession.Catalog.AvailableCards.Where(t=>t is TowerCardDefinition && t!=PlayerSession.Catalog.AvailableCards[turretIndex]).Take(5)).ToArray();
        var hand = Find<GameplayHand>();
        hand.Initialize(new PlayerDeck(testDeck),Find<TurretPlacementController>(),Find<GameFlow>());
        // Arrange the isolated combat target in Hand using model rotations, with six unique definitions throughout.
        while (!hand.Cycle.Hand.Any(c=>(c.Definition as TowerCardDefinition)?.Tower==definition)) hand.Cycle.Use(hand.Cycle.Hand[0]);
        Find<ResourceWallet>().Initialize(1000);
        Card(hand.Cycle.Hand.ToList().FindIndex(c=>(c.Definition as TowerCardDefinition)?.Tower==definition));
        var slot=Slots().First(s=>!s.IsOccupied);
        Check(Find<TurretPlacementController>().TryPlace(slot) && Find<ResourceWallet>().Balance==1000-definition.Cost,"Variant cost: "+definition.DisplayName);
        combatTurret=slot.Occupant;
        durableEnemy=UnityEngine.Object.Instantiate(PlayerSession.Catalog.Stages[0].Enemy);
        var so=new SerializedObject(durableEnemy); so.FindProperty("health").floatValue=1000; so.ApplyModifiedPropertiesWithoutUndo();
        combatEnemy=Find<EnemySpawner>().Spawn(durableEnemy); combatEnemy.GetComponent<EnemyPathFollower>().Stop();
        combatEnemy.transform.position=combatTurret.transform.position+Vector3.right*(definition.AttackRange+.1f);
        hits.Clear(); healths.Clear();
        combatEnemy.GetComponent<EnemyHealth>().Changed+=(hp,max)=> { if (hp>0) { hits.Add(Time.time); healths.Add(hp); } };
    }
    private static void PrepareRetry() {
        retryStage = PlayerSession.ActiveStage;
        retryStageIndex = PlayerSession.Selection.SelectedStageIndex;
        progressionBeforeRetry = Enumerable.Range(0,PlayerSession.Catalog.Stages.Count).Select(i=>PlayerSession.Progression.IsComplete(i)).ToArray();
        Find<FixedIntervalSpawner>().enabled = false;
        Find<ResourceWallet>().Initialize(1000); Card(Find<GameplayHand>().Cycle.Hand.ToList().FindIndex(c=>c.Definition is TowerCardDefinition));
        var slot = Slots().First(s=>!s.IsOccupied);
        Check(Find<TurretPlacementController>().TryPlace(slot),"Retry fixture includes an occupied slot");
        slot.Occupant.enabled = false;
        var enemy = Find<EnemySpawner>().Spawn(retryStage.Enemy); enemy.GetComponent<EnemyPathFollower>().Stop();
        failedEnemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        failedTurrets = UnityEngine.Object.FindObjectsByType<Turret>(FindObjectsSortMode.None);
        failedRun = Find<GameplayHand>().Cycle;
        Find<ResourceWallet>().Initialize(1);
        Find<ElixirSystem>().TrySpend(Find<ElixirSystem>().Current);
        Find<BaseHealth>().ReceiveDamage(10000);
    }
    private static void CheckRetry() {
        Check(PlayerSession.ActiveStage==retryStage && PlayerSession.Selection.SelectedStageIndex==retryStageIndex && SceneManager.GetActiveScene().name==retryStage.SceneName,"Retry preserves same stage and selected index");
        Check(Find<GameFlow>().IsPlaying && Time.timeScale==1 && Find<BaseHealth>().Current==retryStage.BaseHealth && Find<ResourceWallet>().Balance==retryStage.StartingResources,"Retry resets HP, resources, state and time scale");
        Check(failedEnemies.All(e=>e==null) && failedTurrets.All(t=>t==null) && Find<EnemyRegistry>().Enemies.Count==0 && UnityEngine.Object.FindObjectsByType<Turret>(FindObjectsSortMode.None).Length==0 && Slots().All(s=>!s.IsOccupied),"Retry destroys old enemies/turrets and clears slots/registry");
        Check(Find<FixedIntervalSpawner>().enabled,"Retry restores spawn scheduler");
        Check(Find<ElixirSystem>().Current >= 3 && Find<ElixirSystem>().Current < 3.5f,"Retry restores starting Elixir with only elapsed regeneration");
        var hand = Find<GameplayHand>(); var cycle = hand.Cycle;
        Check(cycle!=failedRun && cycle.InitialOrder.All(c=>!failedRun.InitialOrder.Contains(c)) && cycle.Hand.SequenceEqual(cycle.InitialOrder.Take(3)) && cycle.UpcomingQueue.SequenceEqual(cycle.InitialOrder.Skip(3)),"Retry creates a fresh shuffled cycle split into untouched Hand/Queue");
        Check(cycle.Hand.Select(c=>c.Definition).Distinct().Count()==3 && new HashSet<CardDefinition>(cycle.InitialOrder.Select(c=>c.Definition)).SetEquals(PlayerSession.Deck.Slots),"Retry cycle contains exactly the six unique PlayerDeck towers");
        Check(hand.SelectedCard==null && Find<TurretPlacementController>().Selected==null,"Retry clears temporary card selection");
        Check(PlayerSession.Deck.Slots.SequenceEqual(persistentSnapshot) && progressionBeforeRetry.SequenceEqual(Enumerable.Range(0,PlayerSession.Catalog.Stages.Count).Select(i=>PlayerSession.Progression.IsComplete(i))),"Retry leaves PlayerDeck and session progression unchanged");
    }
    private sealed class CountingRandom : System.Random {
        public int Draws { get; private set; }
        public CountingRandom(int seed) : base(seed) { }
        public override int Next(int maxValue) { Draws++; return base.Next(maxValue); }
    }
    private static void Finish(bool success,string detail) {
        SessionState.SetBool(Key,false); EditorApplication.update-=Tick; Application.logMessageReceived-=OnLog;
        string result=(success?"DEFENSE_VALIDATION_PASSED ":"DEFENSE_VALIDATION_FAILED ")+detail;
        Debug.Log(result);
        System.IO.Directory.CreateDirectory("Documentation/Report");
        System.IO.File.WriteAllText("Documentation/Report/ValidationResult.txt",result+"\n");
        if (Application.isBatchMode) EditorApplication.Exit(success?0:1); else EditorApplication.isPlaying=false;
    }
}
}
