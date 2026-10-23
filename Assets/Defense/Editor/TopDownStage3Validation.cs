using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
// Integration acceptance runner. Only isolated test files are ever written.
[InitializeOnLoad]
public static class TopDownStage3Validation {
    private const string Key="Defense.TopDownStage3.Validation";
    private static int step,checks,progressionChanges,startGold,startCandy;
    private static double started,deadline;
    private static float sampleTime,sampleElixir;
    private static string deckBytes,currencyBytes;
    private static CardDefinition[][] presets;
    private static StageDefinition stage;
    private static RuntimeCardCycle previousCycle;
    private static UnityEngine.Object[] retiredObjects;
    private static bool restarting;
    [Serializable] private sealed class RestartSnapshot { public int Gold,Candy,Active; public string DeckJson,CurrencyJson; }
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static GameFlow Flow=>Find<GameFlow>();
    private static FixedIntervalSpawner Schedule=>Find<FixedIntervalSpawner>();
    private static EnemyRegistry Registry=>Find<EnemyRegistry>();
    private static GameplayHand Hand=>Find<GameplayHand>();
    private static ElixirSystem Elixir=>Find<ElixirSystem>();
    private static TurretPlacementController Placement=>Find<TurretPlacementController>();
    private static StageVictoryController Victory=>Find<StageVictoryController>();
    private static string SnapshotPath=>Path.Combine(Path.GetDirectoryName(DeckPersistence.DefaultPath),"stage3-expected.json");
    static TopDownStage3Validation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run()=>Start(false);
    public static void ReadRestart()=>Start(true);
    private static void Start(bool restart){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
        if(string.IsNullOrWhiteSpace(path) ||
            Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar) ||
            Path.GetFullPath(PlayerCurrency.DefaultPath).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar) ||
            Path.GetFullPath(path)==Path.GetFullPath(PlayerCurrency.DefaultPath))
            throw new InvalidOperationException("Use distinct isolated deck and currency test files before running Stage 3 acceptance.");
        SessionState.SetString(Key+".LoadedDeck",File.Exists(path)?File.ReadAllText(path):"");
        SessionState.SetString(Key+".LoadedCurrency",File.Exists(PlayerCurrency.DefaultPath)?File.ReadAllText(PlayerCurrency.DefaultPath):"");
        SessionState.SetBool(Key+".Restart",restart);SessionState.SetBool(Key+".PreviousBackground",Application.runInBackground);
        Application.runInBackground=true;SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){started=EditorApplication.timeSinceStartup;deadline=0;step=checks=progressionChanges=0;restarting=SessionState.GetBool(Key+".Restart",false);EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string message,string trace,LogType type){
        if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense.")) {
            Directory.CreateDirectory("Documentation/Report");
            File.AppendAllText("Documentation/Report/2D_Stage3_EditorSearch_Warnings.txt",message+"\n"+trace+"\n");
            return; // Known Editor indexing failure; preserve evidence separately, never ignore game errors.
        }
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);
    }
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("TOPDOWN3 PASS: "+message);}
    private static void Click(string label){var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text==label);Check(button.interactable,"Actual UI callback available: "+label);button.onClick.Invoke();Canvas.ForceUpdateCanvases();}
    private static void Next(int phase,double wait=.12){step=phase;deadline=EditorApplication.timeSinceStartup+wait;}
    private static void OnProgression()=>progressionChanges++;
    private static void SelectVisible(int index){var button=GameObject.Find("Hand Card "+(index+1)).GetComponent<UnityEngine.UI.Button>();Check(button.interactable,"Real Hand card button is enabled");if(Hand.SelectedCard!=Hand.Cycle.Hand[index])button.onClick.Invoke();Check(Hand.SelectedCard==Hand.Cycle.Hand[index],"UI selects runtime card");}
    private static void AssertCycleUnchanged(RuntimeCard[] hand,RuntimeCard[] queue,string message)=>Check(Hand.Cycle.Hand.SequenceEqual(hand)&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue),message);
    private static void UseVisible(int index,Vector2 skillPoint){
        Elixir.Advance(100);SelectVisible(index);
        var used=Hand.SelectedCard;var before=Hand.Cycle.Hand.ToArray();var queue=Hand.Cycle.UpcomingQueue.ToArray();float energy=Elixir.Current;
        if(used.Definition is TowerCardDefinition tower){
            var slot=Find<BattlefieldMap2D>().PlacementSlots.First(s=>!s.IsOccupied);int balance=Find<ResourceWallet>().Balance;
            Check(Placement.ProcessPointer(Camera.main.WorldToScreenPoint(slot.PlacementPoint.position),true),"Real screen-to-slot input places persistent-deck Tower");
            Check(Find<ResourceWallet>().Balance==balance-tower.Tower.Cost,"Placement pays selected Tower's resource cost once");
            Check(slot.Occupant.GetComponentInChildren<SpriteRenderer>()!=null,"Real Turret abstraction uses native 2D presentation");slot.Occupant.enabled=false;
        }else{
            var casting=Find<SkillCastingController>();var point=Camera.main.WorldToScreenPoint(new Vector3(skillPoint.x,skillPoint.y,0));
            casting.ProcessPointer(point,true,false);Check(casting.IsAiming,"Actual Skill pointer press begins XY aiming");
            Check(casting.ProcessPointer(point,false,true)==CardUseResult.Success,"Actual Skill pointer release commits cast");casting.Advance(100);
        }
        Check(Mathf.Abs(Elixir.Current-(energy-used.Definition.ElixirCost))<.001f,"Successful use spends configured Elixir exactly once");
        Check(Hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=used).Concat(new[]{queue[0]}))&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue.Skip(1).Concat(new[]{used})),"Successful use rotates Hand/queue once without reshuffling");
    }
    // Bring a card into the Hand using only successful production actions, never Cycle.Use fixtures.
    private static int Bring(CardDefinition card){
        for(int i=0;i<6&&!Hand.Cycle.Hand.Any(c=>c.Definition==card);i++)UseVisible(0,(Vector2)Find<BattlefieldMap2D>().PlayableArea.Bounds.center);
        return Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card);
    }
    private static void PrepareDeck(){
        Click("Deck");if(PlayerSession.Presets.ActivePresetIndex!=1)Click("Preset 2");
        var target=PlayerSession.Catalog.AvailableCards.OfType<TowerCardDefinition>().Take(4).Cast<CardDefinition>().Concat(PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>()).ToArray();
        var editor=new DeckEditorController(PlayerSession.Deck,PlayerSession.Catalog.AvailableCards);
        foreach(var card in target.Where(c=>!PlayerSession.Deck.Contains(c)).ToArray()){
            int slot=PlayerSession.Deck.Slots.ToList().FindIndex(c=>!target.Contains(c));Check(editor.Replace(slot,card),"Existing Deck Editor model saves a missing test card into Preset 2");
        }
        presets=Enumerable.Range(0,3).Select(i=>PlayerSession.Presets.GetPreset(i).Slots.ToArray()).ToArray();
        deckBytes=File.ReadAllText(DeckPersistence.DefaultPath);currencyBytes=File.ReadAllText(PlayerSession.Currency.SavePath);
        Check(PlayerSession.Presets.ActivePresetIndex==1 && PlayerSession.Deck.Slots.Distinct().Count()==6,"Preset 2 owns six unique persistent cards including both Skills");
        Click("Back to Lobby");
    }
    private static void ChooseTest(){stage=PlayerSession.Catalog.Stages.Single(s=>s.DisplayName=="Test Stage");while(PlayerSession.Selection.SelectedStageIndex>0)PlayerSession.Selection.MovePrevious();for(int i=0;i<PlayerSession.Catalog.Stages.ToList().IndexOf(stage);i++)PlayerSession.Selection.MoveNext();Find<LobbyUI>().Carousel.Advance(1);}
    private static void VerifyDeck(){Check(PlayerSession.Presets.ActivePresetIndex==1&&Enumerable.Range(0,3).All(i=>PlayerSession.Presets.GetPreset(i).Slots.SequenceEqual(presets[i]))&&File.ReadAllText(DeckPersistence.DefaultPath)==deckBytes,"Battle draws/rotation/reloads preserve all three saved presets, T1-T6 positions and active Preset 2 bytes");}
    private static void VerifyFresh(){
        Schedule.enabled=false;
        Check(SceneManager.GetActiveScene().name==PlayerSession.TopDownSceneName&&PlayerSession.ActiveStage==stage,"Same authoritative StageDefinition is routed to 2D scene");
        Check(Flow.IsPlaying&&Registry.AliveEnemyCount==0&&Schedule.SpawnedCount==0&&Victory.GoldEarned==0&&Victory.DefeatedCount==0,"Retry resets state, registry, spawn and victory counters");
        Check(Find<BaseHealth>().Current==stage.BaseHealth&&Find<ResourceWallet>().Balance==stage.StartingResources&&Mathf.Abs(Elixir.Current-3)<.1f,"Retry restores configured HP/resources and starting Elixir");
        Check(Find<Turret>()==null&&Find<PersistentSlowArea>()==null&&Find<BattlefieldMap2D>().PlacementSlots.All(s=>!s.IsOccupied),"Retry removes Towers, slow zones and slot occupancy");
        Check(!ReferenceEquals(previousCycle,Hand.Cycle)&&Hand.SelectedCard==null&&Hand.Cycle.Hand.Count==3&&Hand.Cycle.UpcomingQueue.Count==3,"Retry creates a fresh three-plus-three runtime cycle");
        if(retiredObjects!=null)Check(retiredObjects.All(o=>o==null),"Scene reload destroys all captured old stage actors/effects");
        VerifyDeck();
    }
    private static void Fill(){for(int i=0;i<8;i++)Schedule.Advance(stage.SpawnInterval);foreach(var e in Registry.Enemies)e.Movement.Stop();Check(Schedule.SpawnedCount==5&&Registry.AliveEnemyCount==5&&Schedule.IsFinishedSpawning,"Finite Test Stage schedules five actors and refuses a sixth");}
    private static void KillAll(){foreach(var e in Registry.Enemies.ToArray()){e.ReceiveDamage(100000);e.ReceiveDamage(100000);}}
    private static void CheckVictory(int run){
        Check(Flow.State==GameplayState.Victory&&Time.timeScale==0&&Victory.DefeatedCount==5,"Actual LateUpdate clears the finished schedule after all five combat deaths");
        Check(PlayerSession.Currency.Gold==startGold+50*run&&PlayerSession.Currency.Candy==startCandy+5*run,"Each clear pays 50 Gold again and grants one Candy per Enemy");
        Check(progressionChanges==1&&PlayerSession.Progression.IsComplete(PlayerSession.Catalog.Stages.ToList().IndexOf(stage)),"First completion changes progression once; repeats have no duplicate side effect");
        Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t=>t.text=="Gold Earned: 50"),"Existing Victory UI displays configured clear reward");
        Check(!Victory.EvaluateClear(),"Repeated clear evaluation is idempotent");Flow.GameOver();Flow.TogglePause();
        Check(Flow.State==GameplayState.Victory&&PlayerSession.Currency.Gold==startGold+50*run,"Terminal Victory rejects Game Over/pause and duplicate reward");
        var disk=JsonUtility.FromJson<PlayerCurrencySaveData>(File.ReadAllText(PlayerSession.Currency.SavePath));Check(disk.Gold==PlayerSession.Currency.Gold&&disk.Candy==PlayerSession.Currency.Candy,"Gold and Candy are already persisted before leaving results UI");VerifyDeck();previousCycle=Hand.Cycle;
    }
    private static void TestActions(){
        Find<ResourceWallet>().Initialize(10000); // Fixture budget permits exercising all six random cards.
        var tower=PlayerSession.Deck.Slots.OfType<TowerCardDefinition>().First();int index=Bring(tower);SelectVisible(index);
        var hand=Hand.Cycle.Hand.ToArray();var queue=Hand.Cycle.UpcomingQueue.ToArray();int balance=Find<ResourceWallet>().Balance;
        Check(!Placement.ProcessPointer(Camera.main.WorldToScreenPoint(Vector3.zero),true),"Invalid non-slot input rejects placement");AssertCycleUnchanged(hand,queue,"Invalid location leaves real persistent-deck Hand/queue intact");
        Find<ResourceWallet>().Initialize(0);var slot=Find<BattlefieldMap2D>().PlacementSlots.First(s=>!s.IsOccupied);
        Check(!Placement.ProcessPointer(Camera.main.WorldToScreenPoint(slot.transform.position),true)&&Find<ResourceWallet>().Balance==0&&!slot.IsOccupied,"Insufficient resources neither place nor spend");AssertCycleUnchanged(hand,queue,"Unaffordable placement preserves Hand/queue");Find<ResourceWallet>().Initialize(balance);
        UseVisible(index,Vector2.zero);
        var slow=PlayerSession.Deck.Slots.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is SlowAreaSkill);index=Bring(slow);SelectVisible(index);
        hand=Hand.Cycle.Hand.ToArray();queue=Hand.Cycle.UpcomingQueue.ToArray();float energy=Elixir.Current;
        var cast=Find<SkillCastingController>();cast.BeginAim(Vector2.zero,false);
        Check(cast.ReleaseAim(new Vector2(100,100),false)==CardUseResult.Cancelled&&Elixir.Current==energy,"Off-map Skill cancellation preserves Elixir");AssertCycleUnchanged(hand,queue,"Cancelled Skill preserves runtime queue");
        UseVisible(index,Vector2.zero);Check(Find<PersistentSlowArea>()!=null,"Existing Slow Area Skill creates active XY gameplay effect");
        var toy=PlayerSession.Deck.Slots.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is CircularDamageSkill);UseVisible(Bring(toy),(Vector2)Find<BattlefieldMap2D>().PlayableArea.Bounds.center+Vector2.right*5);
        VerifyDeck();Check(File.ReadAllText(PlayerSession.Currency.SavePath)==currencyBytes,"Entering battle and using cards do not overwrite persistent currency");
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>100)throw new Exception("Stage 3 integration acceptance timeout at phase "+step);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<deadline)return;
        switch(step){
            case 0:
                if(Find<MainMenuUI>()==null||GameObject.Find("Start Game")==null)return;
                Check(PlayerSession.Catalog!=null&&Find<LobbyUI>()==null,"Real Main Menu initializes existing session services");
                if(SessionState.GetString(Key+".LoadedDeck","")!="")Check(File.ReadAllText(DeckPersistence.DefaultPath)==SessionState.GetString(Key+".LoadedDeck",""),"Existing copied deck save loads without reset or rewritten Card IDs");
                if(SessionState.GetString(Key+".LoadedCurrency","")!="")Check(File.ReadAllText(PlayerCurrency.DefaultPath)==SessionState.GetString(Key+".LoadedCurrency",""),"Existing copied currency save loads without resetting Gold/Candy");
                Click("Start Game");
                if(restarting){VerifyRestart();ChooseTest();Click("Enter Stage");Next(80);break;}
                PrepareDeck();ChooseTest();startGold=PlayerSession.Currency.Gold;startCandy=PlayerSession.Currency.Candy;
                PlayerSession.Progression.Changed+=OnProgression;
                Check(GameObject.Find("Enter Stage")!=null&&GameObject.Find("Play 3D Fallback")==null,"Lobby exposes only the configured 2D stage entry");
                Click("Enter Stage");Next(1,.02);break;
            case 1:
                if(Find<GameplayUI>()==null||GameObject.Find("Hand Card 1")==null)return;
                Schedule.enabled=false;
                Check(Find<TopDownInteractionSandbox>()==null&&Find<TopDownInteractionHUD>()==null&&Registry.Plane==WaypointPlane.XY,"Integrated scene uses real bootstrap/HUD and XY actors, without Stage 2 local session");
                Check(PlayerSession.ActiveStage==stage&&Find<BaseHealth>().Current==3&&Find<ResourceWallet>().Balance==300&&Schedule.SpawnedCount==0,"Selected Test Stage retains original five-enemy configuration and starting balances");
                Check(Hand.Cycle.InitialOrder.Take(3).SequenceEqual(Hand.Cycle.Hand)&&Hand.Cycle.InitialOrder.Skip(3).SequenceEqual(Hand.Cycle.UpcomingQueue)&&Hand.Cycle.InitialOrder.Select(c=>c.Definition).OrderBy(c=>c.CardId).SequenceEqual(PlayerSession.Deck.Slots.OrderBy(c=>c.CardId)),"Initial shuffle copies the six active preset cards into Hand and queue");
                VerifyDeck();sampleElixir=Elixir.Current;sampleTime=Time.time;Next(2,.35);break;
            case 2:
                Check(Time.time>sampleTime+.15f&&Mathf.Abs(Elixir.Current-sampleElixir-(Time.time-sampleTime)*.36f)<.02f,"Advancing Play Mode frames regenerate real Elixir at 0.36 per second");
                Click("Pause");sampleTime=Time.time;sampleElixir=Elixir.Current;Next(3,.3);break;
            case 3:
                Check(Flow.State==GameplayState.Paused&&Time.time==sampleTime&&Elixir.Current==sampleElixir&&Schedule.SpawnedCount==0,"Pause freezes gameplay time, Elixir and spawning");
                Click("Settings");Click("Back");Check(Flow.State==GameplayState.Paused&&GameObject.Find("Paused")!=null,"Placeholder Settings returns to Pause");Click("Resume");
                TestActions();Check(!Victory.EvaluateClear(),"Empty population before finite schedule completion cannot clear");Schedule.enabled=true;sampleTime=Time.time;Next(4);break;
            case 4:
                if(!Schedule.IsFinishedSpawning)return;
                Schedule.enabled=false;Check(Time.time>sampleTime+3&&Schedule.SpawnedCount==5&&Registry.AliveEnemyCount==5,"Real frame-driven timer spawns exactly five living 2D Enemies");
                foreach(var e in Registry.Enemies)e.Movement.Stop();
                Check(Registry.Enemies.All(e=>e.GetComponent<Collider2D>()!=null&&e.GetComponent<EnemyHealthBar>()!=null),"Scheduled BasicEnemy instances use sprite actors and reusable HP bars");
                KillAll();Check(Victory.DefeatedCount==5&&PlayerSession.Currency.Candy==startCandy+5,"Repeated lethal damage counts each actual death and Candy drop once");
                retiredObjects=UnityEngine.Object.FindObjectsByType<Turret>().Cast<UnityEngine.Object>().Concat(UnityEngine.Object.FindObjectsByType<PersistentSlowArea>()).ToArray();Next(5);break;
            case 5:
                CheckVictory(1);Check(retiredObjects.Length>0,"Replay fixture contains actual old Towers/slow effects");Click("Replay");Next(6,.02);break;
            case 6:
                if(GameObject.Find("Hand Card 1")==null)return;VerifyFresh();Check(PlayerSession.Currency.Gold==startGold+50&&PlayerSession.Currency.Candy==startCandy+5,"Replay retains both currencies");Fill();KillAll();Next(7);break;
            case 7:
                CheckVictory(2);Click("Replay");Next(8,.02);break;
            case 8:
                if(GameObject.Find("Hand Card 1")==null)return;VerifyFresh();
                var diagnostic=Find<EnemySpawner>().Spawn(stage.Enemy);diagnostic.ReceiveDamage(100000);
                Check(Victory.DefeatedCount==0&&Schedule.SpawnedCount==0,"An extra unscheduled combat kill does not increment scheduled defeat statistics");Fill();
                var leaking=Registry.Enemies.First();leaking.Movement.Initialize(Find<BattlefieldMap2D>().Path,stage.Enemy.MovementSpeed);leaking.Movement.Advance(10000);KillAll();
                Check(Find<BaseHealth>().Current==2&&Victory.DefeatedCount==4,"One nonlethal Base arrival is not a combat death");Next(9);break;
            case 9:
                Check(Flow.State==GameplayState.Victory&&Find<BaseHealth>().Current==2&&!Victory.EvaluateClear()&&PlayerSession.Currency.Gold==startGold+150,"Four kills plus a nonlethal Base arrival clear the finished schedule with positive HP and award Gold once");
                Check(PlayerSession.Currency.Candy==startCandy+15,"Only combat deaths grant Candy; the nonlethal Base arrival grants none");previousCycle=Hand.Cycle;Click("Replay");Next(10,.02);break;
            case 10:
                if(GameObject.Find("Hand Card 1")==null)return;VerifyFresh();Fill();Registry.Enemies.First().ReceiveDamage(100000);
                foreach(var e in Registry.Enemies.ToArray()){e.Movement.Initialize(Find<BattlefieldMap2D>().Path,stage.Enemy.MovementSpeed);e.Movement.Advance(10000);if(Flow.IsTerminal)break;}Next(11);break;
            case 11:
                Check(Flow.State==GameplayState.GameOver&&Find<BaseHealth>().Current==0&&!Victory.EvaluateClear()&&PlayerSession.Currency.Gold==startGold+150,"Lethal Base damage enters Game Over exclusively without clear Gold");
                var disk=JsonUtility.FromJson<PlayerCurrencySaveData>(File.ReadAllText(PlayerSession.Currency.SavePath));Check(PlayerSession.Currency.Candy==startCandy+16&&disk.Candy==PlayerSession.Currency.Candy,"Candy collected before lethal failure remains saved");
                sampleElixir=Elixir.Current;Elixir.Advance(100);Check(Elixir.Current==sampleElixir,"Game Over stops Elixir regeneration");previousCycle=Hand.Cycle;Click("Retry");Next(12,.02);break;
            case 12:
                if(GameObject.Find("Hand Card 1")==null)return;VerifyFresh();Fill();
                // The final arrival empties the registry before damaging Base. Lethal damage must win.
                foreach(var e in Registry.Enemies.Take(4).ToArray())e.ReceiveDamage(100000);
                Find<BaseHealth>().ReceiveDamage(2);
                var last=Registry.Enemies.Single();last.Movement.Initialize(Find<BattlefieldMap2D>().Path,stage.Enemy.MovementSpeed);last.Movement.Advance(10000);
                Check(Registry.AliveEnemyCount==0&&Find<BaseHealth>().Current==0&&Flow.State==GameplayState.GameOver,"Lethal final arrival enters Game Over synchronously even when registry becomes empty");Next(30);break;
            case 30:
                Check(Flow.State==GameplayState.GameOver&&!Victory.EvaluateClear()&&PlayerSession.Currency.Gold==startGold+150&&PlayerSession.Currency.Candy==startCandy+20,"LateUpdate cannot replace lethal final-arrival failure with Victory or grant Gold");previousCycle=Hand.Cycle;Click("Retry");Next(31,.02);break;
            case 31:
                if(GameObject.Find("Hand Card 1")==null)return;VerifyFresh();Find<BaseHealth>().Initialize(10);Fill();
                foreach(var e in Registry.Enemies.ToArray()){e.Movement.Initialize(Find<BattlefieldMap2D>().Path,stage.Enemy.MovementSpeed);e.Movement.Advance(10000);}
                Check(Find<BaseHealth>().Current==5&&Registry.AliveEnemyCount==0&&Victory.DefeatedCount==0&&Find<ResourceWallet>().Balance==stage.StartingResources,"All five nonlethal arrivals damage Base without fake kills or combat resources");Next(32);break;
            case 32:
                Check(Flow.State==GameplayState.Victory&&Victory.GoldEarned==50&&PlayerSession.Currency.Gold==startGold+200&&PlayerSession.Currency.Candy==startCandy+20,"Surviving the entire finite schedule clears even with zero kills and no Candy drops");
                Check(!Victory.EvaluateClear()&&PlayerSession.Currency.Gold==startGold+200&&progressionChanges==1,"Arrival-only clear pays once and does not repeat progression changes");previousCycle=Hand.Cycle;Click("Replay");Next(33,.02);break;
            case 33:
                if(GameObject.Find("Hand Card 1")==null)return;VerifyFresh();Click("Pause");Click("Exit");Next(13);break;
            case 13:
                if(Find<LobbyUI>()==null)return;
                VerifyDeck();Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t=>t.text=="Gold: "+PlayerSession.Currency.Gold+"   Candy: "+PlayerSession.Currency.Candy),"Return to Lobby reads authoritative earned currency");
                Click("Enter Stage");Next(14,.02);break;
            case 14:
                if(Find<GameplayUI>()==null||GameObject.Find("Hand Card 1")==null)return;
                Check(SceneManager.GetActiveScene().name==stage.SceneName&&PlayerSession.ActiveStage==stage&&Registry.Plane==WaypointPlane.XY,"Lobby reentry opens the configured 2D scene with the same selected stage");
                Schedule.enabled=false;Click("Pause");Click("Exit");Next(15);break;
            case 15:
                if(Find<LobbyUI>()==null)return;for(int i=0;i<PlayerSession.Catalog.Stages.Count;i++)PlayerSession.Selection.MovePrevious();Find<LobbyUI>().Carousel.Advance(1);Click("Enter Stage");Next(16,.02);break;
            case 16:
                if(Find<GameplayUI>()==null||GameObject.Find("Hand Card 1")==null)return;Schedule.enabled=false;
                Check(PlayerSession.ActiveStage==PlayerSession.Catalog.Stages[0]&&Find<BaseHealth>().Current==10&&Find<ResourceWallet>().Balance==150,"2D route uses chosen campaign Stage stats instead of overwriting with Test Stage default");
                Schedule.Advance(100);Check(Schedule.SpawnedCount==1&&!Schedule.IsFinishedSpawning&&Registry.Enemies[0].Definition==PlayerSession.ActiveStage.Enemy,"Campaign continuous schedule retains selected Enemy configuration");
                VerifyDeck();WriteRestart();Finish(true,checks+" integration assertions");break;
            case 80:
                if(Find<GameplayUI>()==null||GameObject.Find("Hand Card 1")==null)return;Schedule.enabled=false;VerifyRestart();
                Check(PlayerSession.ActiveStage==stage&&Hand.Cycle.InitialOrder.Select(c=>c.Definition).OrderBy(c=>c.CardId).SequenceEqual(PlayerSession.Deck.Slots.OrderBy(c=>c.CardId)),"Second application process loads saved active deck into fresh 2D runtime cycle");
                Check(Flow.IsPlaying&&Schedule.SpawnedCount==0&&Registry.AliveEnemyCount==0&&Find<BaseHealth>().Current==3,"Second process starts a fresh run without restoring transient battle state");
                Check(!PlayerSession.Progression.IsComplete(PlayerSession.Catalog.Stages.ToList().IndexOf(stage)),"Existing session-only stage progression is not persisted (documented pre-existing limitation)");
                Finish(true,checks+" application-restart assertions");break;
        }
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void WriteRestart(){var data=new RestartSnapshot{Gold=PlayerSession.Currency.Gold,Candy=PlayerSession.Currency.Candy,Active=PlayerSession.Presets.ActivePresetIndex,DeckJson=File.ReadAllText(DeckPersistence.DefaultPath),CurrencyJson=File.ReadAllText(PlayerSession.Currency.SavePath)};File.WriteAllText(SnapshotPath,JsonUtility.ToJson(data,true));}
    private static void VerifyRestart(){var expected=JsonUtility.FromJson<RestartSnapshot>(File.ReadAllText(SnapshotPath));Check(PlayerSession.Currency.Gold==expected.Gold&&PlayerSession.Currency.Candy==expected.Candy&&PlayerSession.Presets.ActivePresetIndex==expected.Active,"Fresh application restores saved Gold, Candy and active preset");Check(File.ReadAllText(DeckPersistence.DefaultPath)==expected.DeckJson&&File.ReadAllText(PlayerSession.Currency.SavePath)==expected.CurrencyJson,"Valid pre-migration save schema and all preset IDs load without rewriting bytes");}
    private static void Finish(bool pass,string message){
        if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
        if(PlayerSession.Progression!=null)PlayerSession.Progression.Changed-=OnProgression;
        Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/2D_Stage3_"+(restarting?"Restart":"Integration")+"_Result.txt",(pass?"PASS: ":"FAIL: ")+message);
        if(pass)Debug.Log("TOPDOWN_STAGE3_PASSED "+message);else Debug.LogError("TOPDOWN_STAGE3_FAILED "+message);
        EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".PreviousBackground",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);
    }
}
}
