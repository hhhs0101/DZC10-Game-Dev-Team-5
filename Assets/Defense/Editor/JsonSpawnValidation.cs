using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
[InitializeOnLoad]
public static class JsonSpawnValidation {
    private const string Key="Defense.JsonSpawn.Validation";
    private const string Timeline="{\"stageId\":\"relative_fixture\",\"spawns\":[{\"enemyId\":\"enemy_mushroom\",\"delay\":1},{\"enemyId\":\"enemy_tomato\",\"delay\":2},{\"enemyId\":\"enemy_potato\",\"delay\":0.5}]}";
    private static int phase,checks,expectedErrors,deaths,errorFrame;
    private static bool allowConfigurationError;
    private static double started,deadline;
    private static float startTime,pausedTime;
    private static int startGold,startCandy,resources;
    private static string deckBytes,tempJson;
    private static StageDefinition stage,invalidStage;
    private static RuntimeCardCycle firstCycle;
    private static GameFlow previous;
    private static readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
    private static readonly List<Enemy> observed=new List<Enemy>();
    private static readonly List<float> times=new List<float>();
    private static T Find<T>() where T:UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>();
    private static FixedIntervalSpawner Schedule=>Find<FixedIntervalSpawner>();
    private static EnemyRegistry Registry=>Find<EnemyRegistry>();
    private static GameFlow Flow=>Find<GameFlow>();
    private static EnemyDefinitionCatalog Catalog=>Resources.Load<EnemyDefinitionCatalog>(EnemyDefinitionCatalog.ResourcePath);
    private static void Check(bool value,string message) {if(!value)throw new Exception(message);checks++;Debug.Log("JSON SPAWN PASS: "+message);}
    private static void Near(float actual,float expected,string message) => Check(Mathf.Abs(actual-expected)<.001f,message);
    private static T Own<T>(T value) where T:UnityEngine.Object {owned.Add(value);return value;}
    private static T Copy<T>(T value) where T:UnityEngine.Object => Own(UnityEngine.Object.Instantiate(value));
    private static TextAsset Text(string json) {var value=Own(new TextAsset(json));value.name="validation-json";return value;}
    private static void Set(UnityEngine.Object target,string name,object value) {
        var data=new SerializedObject(target);var field=data.FindProperty(name);
        if(value is int i)field.intValue=i;else if(value is float f)field.floatValue=f;else if(value is bool b)field.boolValue=b;else if(value is string s)field.stringValue=s;else field.objectReferenceValue=(UnityEngine.Object)value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static StageDefinition Fixture(string json) {var value=Copy(AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage2.asset"));Set(value,"spawnPlanJson",Text(json));return value;}
    private static void Sequence(StageDefinition value,params EnemyDefinition[] entries) {var data=new SerializedObject(value);var list=data.FindProperty("spawnSequence");list.arraySize=entries.Length;for(int i=0;i<entries.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=entries[i];data.ApplyModifiedPropertiesWithoutUndo();}
    private static EnemyDefinitionCatalog Lookup(params EnemyDefinition[] entries) {var value=Own(ScriptableObject.CreateInstance<EnemyDefinitionCatalog>());var data=new SerializedObject(value);var list=data.FindProperty("definitions");list.arraySize=entries.Length;for(int i=0;i<entries.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=entries[i];data.ApplyModifiedPropertiesWithoutUndo();return value;}
    private static EnemyDefinition Enemy(string id)=>Catalog.Definitions.Single(d=>d.EnemyTypeId==id);
    private static void Reject(string json,string reason,EnemyDefinitionCatalog catalog=null) {
        Check(!SpawnPlanLoader.TryLoad(Text(json),"Validation Stage",catalog!=null?catalog:Catalog,out var plan,out var issue)&&plan==null&&issue.Contains("Validation Stage")&&issue.Contains("validation-json"),reason+": explicit contextual error before any spawn");
    }
    private static void Parsing() {
        Check(Catalog!=null&&Catalog.TryBuildLookup(out var lookup,out _)&&lookup.Count==15,"Player-loadable catalog resolves all fifteen existing stable-ID definitions");
        Check(SpawnPlanLoader.TryLoad(Text(Timeline),"Validation Stage",Catalog,out var plan,out _)&&plan.Events.Count==3&&plan.Events.Select(e=>e.Enemy.EnemyTypeId).SequenceEqual(new[]{"enemy_mushroom","enemy_tomato","enemy_potato"}),"Minimal JSON schema resolves real existing definitions in order");
        Check(plan.Events.Select(e=>e.Delay).SequenceEqual(new[]{1f,2f,.5f})&&plan.StageId=="relative_fixture","Relative numeric delays and diagnostic stageId are preserved");
        string prefix="{\"stageId\":\"bad\",\"spawns\":[",suffix="]}";
        foreach(string delay in new[]{"-1","null","\"1\"","true","NaN","Infinity","1e999","01",".5","1.","+1"})Reject(prefix+"{\"enemyId\":\"enemy_mushroom\",\"delay\":"+delay+"}"+suffix,"Invalid delay "+delay);
        foreach(string entry in new[]{"{}","null","{\"enemyId\":\"enemy_mushroom\"}","{\"delay\":0}","{\"enemyId\":\" \",\"delay\":0}","{\"enemyId\":null,\"delay\":0}","{\"enemyId\":\"enemy_unknown\",\"delay\":0}","{\"enemyId\":\"enemy_mushroom\",\"delay\":0,\"delay\":1}","{\"enemyId\":\"enemy_mushroom\",\"delay\":0,\"count\":5}"})Reject(prefix+entry+suffix,"Invalid event "+entry);
        foreach(string json in new[]{"","[]","null","{","{}","{\"stageId\":\"bad\"}","{\"stageId\":\"bad\",\"spawns\":[]}","{\"stageId\":\"bad\",\"spawns\":null}","{\"spawns\":[]}","{\"stageId\":\"bad\",\"spawns\":[],}",Timeline+" true",Timeline.Replace("\"stageId\"","\"wrongField\"")})Reject(json,"Malformed/missing/empty plan");
        var mushroom=Enemy("enemy_mushroom");var duplicate=Copy(mushroom);Reject(Timeline,"Duplicate catalog ID",Lookup(mushroom,duplicate));Reject(Timeline,"Null catalog reference",Lookup(mushroom,null));
        var blank=Copy(mushroom);Set(blank,"enemyTypeId","");Reject(Timeline,"Blank catalog ID",Lookup(blank));var missing=Copy(mushroom);Set(missing,"prefab",null);Reject(Timeline,"Missing prefab",Lookup(missing));Reject(Timeline,"Empty catalog",Lookup());
        Check(!SpawnPlanLoader.TryLoad(Text(Timeline),"Validation Stage",null,out _,out var error)&&error.Contains("catalog"),"Missing catalog is diagnosed explicitly");
        var culture=CultureInfo.CurrentCulture;try {CultureInfo.CurrentCulture=new CultureInfo("fr-FR");Check(SpawnPlanLoader.TryLoad(Text(Timeline.Replace("0.5","5e-1").Replace("enemy_mushroom","enemy_\\u006dushroom")),"Validation Stage",Catalog,out var parsed,out _)&&parsed.Events[2].Delay==.5f,"Strict reader handles Unicode IDs/exponents independently of OS number culture");}finally{CultureInfo.CurrentCulture=culture;}
        var sample=AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage2.asset");ResolvedSpawnPlan actual=null;Check(sample.SpawnPlanJson!=null&&sample.SceneName==PlayerSession.TopDownSceneName&&SpawnPlanLoader.TryLoad(sample.SpawnPlanJson,sample.DisplayName,Catalog,out actual,out _)&&actual.Events.Count==10,"Saved TestStage2 uses its real linked JSON and original integrated scene");
        Check(actual.Events.Select(e=>e.Enemy).SequenceEqual(sample.SpawnSequence)&&actual.Events.All(e=>e.Delay==1),"Actual sample preserves ten original vegetables/order/one-second cadence and legacy references");
        foreach(var value in owned)if(value!=null)UnityEngine.Object.DestroyImmediate(value);owned.Clear();
    }
    static JsonSpawnValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");if(string.IsNullOrWhiteSpace(path)||Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Use an isolated existing save profile.");
        if(Path.GetFullPath(PlayerCurrency.DefaultPath).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Currency must also use an isolated save profile.");SessionState.SetString(Key+".CurrencyPath",Path.GetFullPath(PlayerCurrency.DefaultPath));
        checks=0;Parsing();SessionState.SetInt(Key+".Checks",checks);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){phase=expectedErrors=deaths=0;checks=SessionState.GetInt(Key+".Checks",0);started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string message,string trace,LogType type) {
        if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;
        if(allowConfigurationError&&type==LogType.Error&&message.StartsWith("Stage '")) {expectedErrors++;return;}
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);
    }
    private static void Click(string text) {var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text==text);Check(button.interactable,"Actual UI callback enabled: "+text);button.onClick.Invoke();}
    private static void ClearEnemies(){foreach(var enemy in Registry.Enemies.ToArray())UnityEngine.Object.DestroyImmediate(enemy.gameObject);}
    private static void Bind(StageDefinition definition)=>typeof(PlayerSession).GetMethod("BindActiveStage",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{definition});
    private static void Fresh() {Schedule.enabled=false;Check(PlayerSession.ActiveStage==stage&&Schedule.UsesJsonPlan&&Schedule.PlannedSpawnCount==10&&Schedule.SpawnedCount==0&&!Schedule.IsFinishedSpawning&&Flow.IsPlaying,"Real stage initialization/reload creates exactly one fresh JSON schedule");Check(File.ReadAllText(DeckPersistence.DefaultPath)==deckBytes,"Stage entry/Retry preserves preset save bytes");}
    private static void RuntimeScheduling() {
        var a=Enemy("enemy_mushroom");var b=Enemy("enemy_tomato");var legacy=Copy(stage);Set(legacy,"spawnPlanJson",null);Sequence(legacy,a,b,a);Set(legacy,"spawnInterval",.2f);Set(legacy,"totalEnemies",999);Set(legacy,"finiteSpawning",false);
        var seen=new List<Enemy>();Action<Enemy> note=e=>{seen.Add(e);e.Movement.Stop();};Schedule.EnemyScheduled+=note;
        Schedule.Configure(legacy);for(int i=0;i<3;i++)Schedule.Advance(.2f);Check(!Schedule.UsesJsonPlan&&Schedule.IsFinishedSpawning&&seen.Select(e=>e.Definition).SequenceEqual(new[]{a,b,a}),"No-JSON legacy sequence retains fixed cadence, repeated references and list-derived count");ClearEnemies();seen.Clear();
        Sequence(legacy);Set(legacy,"enemy",a);Set(legacy,"finiteSpawning",true);Set(legacy,"totalEnemies",2);Schedule.Configure(legacy);Schedule.Advance(.2f);Schedule.Advance(.2f);Schedule.Advance(10);Check(seen.Count==2&&Schedule.IsFinishedSpawning,"No-JSON single enemy retains finite count");ClearEnemies();seen.Clear();
        Set(legacy,"finiteSpawning",false);Schedule.Configure(legacy);for(int i=0;i<5;i++)Schedule.Advance(.2f);Check(seen.Count==5&&!Schedule.IsFinishedSpawning&&Schedule.PlannedSpawnCount==0,"No-JSON single enemy retains infinite behavior");ClearEnemies();seen.Clear();
        var zero=Fixture("{\"stageId\":\"zeros\",\"spawns\":[{\"enemyId\":\"enemy_mushroom\",\"delay\":0},{\"enemyId\":\"enemy_tomato\",\"delay\":0},{\"enemyId\":\"enemy_mushroom\",\"delay\":0}]}");
        Sequence(zero);Set(zero,"enemy",null);Set(zero,"finiteSpawning",false);Set(zero,"totalEnemies",999);Set(zero,"spawnInterval",99f);string before=EditorJsonUtility.ToJson(zero);
        Schedule.Configure(zero);Schedule.Advance(.01f);Check(Schedule.UsesJsonPlan&&Schedule.PlannedSpawnCount==3&&Schedule.IsFinishedSpawning&&seen.Select(e=>e.Definition).SequenceEqual(new[]{a,b,a}),"JSON overrides every legacy scheduling field and processes ordered zero-delay repeats in one call");Check(EditorJsonUtility.ToJson(zero)==before&&!zero.FiniteSpawning,"Runtime JSON finite mode never overwrites legacy stage fields, even finiteSpawning=false");ClearEnemies();seen.Clear();
        var relative=Fixture(Timeline);Schedule.Configure(relative);Schedule.Advance(10);Check(seen.Count==1,"Large frame spawns first due entry without borrowing time for the next positive relative delay");Schedule.Advance(1.9f);Check(seen.Count==1,"Next positive delay starts after actual successful preceding spawn");Flow.TogglePause();Schedule.Advance(100);Check(seen.Count==1,"Pause blocks explicit scheduler time advancement");Flow.TogglePause();Schedule.Advance(.11f);Schedule.Advance(.5f);Check(Schedule.IsFinishedSpawning&&seen.Count==3,"Resume continues relative delay and ends after exactly three events");ClearEnemies();seen.Clear();
        tempJson="Assets/Defense/Data/SpawnPlans/ValidationTemp_"+Guid.NewGuid().ToString("N")+".json";File.WriteAllText(tempJson,Timeline);AssetDatabase.ImportAsset(tempJson);var file=AssetDatabase.LoadAssetAtPath<TextAsset>(tempJson);var edited=Copy(stage);Set(edited,"spawnPlanJson",file);before=EditorJsonUtility.ToJson(edited);Schedule.Configure(edited);Schedule.Advance(1);
        File.WriteAllText(tempJson,"{\"stageId\":\"edited\",\"spawns\":[{\"enemyId\":\"enemy_potato\",\"delay\":0},{\"enemyId\":\"enemy_mushroom\",\"delay\":0.1}]}");AssetDatabase.ImportAsset(tempJson,ImportAssetOptions.ForceUpdate);Schedule.Advance(2);Schedule.Advance(.5f);Check(seen.Select(e=>e.Definition).SequenceEqual(new[]{a,b,Enemy("enemy_potato")}),"Changing JSON asset does not hot-reload or mutate an active resolved schedule");ClearEnemies();seen.Clear();Schedule.Configure(edited);Schedule.Advance(.01f);Schedule.Advance(.1f);Check(seen.Count==2&&seen[0].Definition==Enemy("enemy_potato")&&seen[1].Definition==a&&EditorJsonUtility.ToJson(edited)==before,"Editing only JSON controls order/delays/count on next run without stage/code changes");ClearEnemies();seen.Clear();
        Schedule.Configure(relative);Schedule.Configure(relative);Schedule.Advance(1);Check(seen.Count==1&&Schedule.SpawnedCount==1,"Repeated initialization replaces rather than duplicates the schedule");ClearEnemies();Schedule.EnemyScheduled-=note;
    }
    private static void Observe(Enemy enemy){observed.Add(enemy);times.Add(Time.time-startTime);Check(enemy.transform.position==Find<BattlefieldMap2D>().Path.GetPoint(0),"JSON enemy uses existing refrigerator path origin");enemy.Movement.Stop();}
    private static void Error(StageDefinition value){allowConfigurationError=true;Schedule.Configure(value);Check(Flow.State==GameplayState.ConfigurationError&&Schedule.HasConfigurationError&&!Schedule.IsFinishedSpawning&&Schedule.SpawnedCount==0&&Registry.AliveEnemyCount==0,"Invalid assigned JSON validates all events first and enters a blocking error, without legacy fallback");}
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try {
        if(EditorApplication.timeSinceStartup-started>120)throw new Exception("JSON integration timeout at phase "+phase);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(phase==0){if(GameObject.Find("Start Game")==null)return;Click("Start Game");phase=1;}
        else if(phase==1){var lobby=Find<LobbyUI>();if(lobby==null)return;if(Path.GetFullPath(PlayerSession.Currency.SavePath)!=SessionState.GetString(Key+".CurrencyPath",""))throw new Exception("Currency path changed after entering Play Mode.");stage=PlayerSession.Catalog.Stages.Single(s=>s.DisplayName=="Test Stage 2");while(PlayerSession.Selection.SelectedStageIndex<PlayerSession.Catalog.Stages.ToList().IndexOf(stage))PlayerSession.Selection.MoveNext();lobby.Carousel.Advance(1);deckBytes=File.ReadAllText(DeckPersistence.DefaultPath);startGold=PlayerSession.Currency.Gold;startCandy=PlayerSession.Currency.Candy;Click("Enter Stage");phase=2;}
        else if(phase==2){if(Find<GameplayUI>()==null||GameObject.Find("Hand Card 1")==null)return;Fresh();ClearEnemies();RuntimeScheduling();observed.Clear();times.Clear();Schedule.Configure(Fixture(Timeline));firstCycle=Find<GameplayHand>().Cycle;resources=Find<ResourceWallet>().Balance;startTime=Time.time;Schedule.EnemyScheduled+=Observe;Find<CandyDropSystem>().Initialize(Find<EnemySpawner>(),PlayerSession.Currency,()=>0);Find<EnemySpawner>().EnemyDefeated+=e=>deaths++;Schedule.enabled=true;phase=3;}
        else if(phase==3){if(observed.Count<1)return;Flow.TogglePause();pausedTime=Time.time;deadline=EditorApplication.timeSinceStartup+.2;phase=4;}
        else if(phase==4){if(EditorApplication.timeSinceStartup<deadline)return;Check(Time.time==pausedTime&&observed.Count==1,"Actual paused frames consume no JSON delay");Flow.TogglePause();phase=5;}
        else if(phase==5){if(observed.Count<3)return;Schedule.enabled=false;Schedule.EnemyScheduled-=Observe;Check(times.Count==3&&Mathf.Abs(times[0]-1)<.15f&&Mathf.Abs(times[1]-3)<.2f&&Mathf.Abs(times[2]-3.5f)<.25f,"Actual advancing frames produce 1.0/3.0/3.5 gameplay-second timeline within frame tolerance");Check(Schedule.IsFinishedSpawning&&Registry.AliveEnemyCount==3&&Flow.IsPlaying&&!Find<StageVictoryController>().EvaluateClear(),"Completing JSON schedule does not clear while enemies remain alive");foreach(var enemy in observed)enemy.ReceiveDamage(100000);var victory=Find<StageVictoryController>();Check(victory.EvaluateClear()&&Flow.State==GameplayState.Victory&&PlayerSession.Currency.Gold==startGold+50&&PlayerSession.Currency.Candy==startCandy+3&&deaths==3&&Find<ResourceWallet>().Balance==resources+observed.Sum(e=>e.ResourceReward),"Actual JSON deaths award normal resources/Candy and one Stage Clear Gold reward");Check(!victory.EvaluateClear()&&PlayerSession.Currency.Gold==startGold+50,"Repeated clear cannot duplicate JSON rewards");previous=Flow;Click("Replay");phase=6;}
        else if(phase==6){if(Flow==previous||GameObject.Find("Hand Card 1")==null)return;Fresh();Check(Find<GameplayHand>().Cycle!=firstCycle&&PlayerSession.Currency.Gold==startGold+50,"Replay restarts original asset JSON and fresh Hand while preserving earned currency");Schedule.Advance(1);var escape=Registry.Enemies.Single();Find<BaseHealth>().Initialize(1);escape.Movement.Advance(10000);Check(Flow.State==GameplayState.GameOver&&Find<BaseHealth>().Current==0&&!Schedule.IsFinishedSpawning,"Lethal path arrival interrupts a partially consumed JSON plan");Schedule.Advance(100);Check(Schedule.SpawnedCount==1&&PlayerSession.Currency.Gold==startGold+50&&PlayerSession.Currency.Candy==startCandy+3,"Game Over cancels remaining JSON events and pays no escape/clear reward");previous=Flow;Click("Retry");phase=7;}
        else if(phase==7){if(Flow==previous||GameObject.Find("Hand Card 1")==null)return;Fresh();invalidStage=Fixture("{\"stageId\":\"broken_stage\",\"spawns\":[{\"enemyId\":\"enemy_mushroom\",\"delay\":0},{\"enemyId\":\"unknown\",\"delay\":1}]}");Error(invalidStage);Check(!Find<StageVictoryController>().EvaluateClear()&&PlayerSession.Currency.Gold==startGold+50&&PlayerSession.Currency.Candy==startCandy+3,"Configuration failure cannot clear or award currency");errorFrame=Time.frameCount;phase=71;}
        else if(phase==71){if(Time.frameCount==errorFrame)return;Check(GameObject.Find("Stage Configuration Error")!=null&&GameplayPointer.IsOverUI(new Vector2(Screen.width/2,Screen.height/2))&&!Find<GameplayHand>().Select(0),"Blocking error UI raycasts prevent apparently active Card/world interaction");Bind(invalidStage);previous=Flow;Click("Retry");phase=8;}
        else if(phase==8){if(Flow==previous||GameObject.Find("Hand Card 1")==null)return;Check(Flow.State==GameplayState.ConfigurationError&&GameObject.Find("Stage Configuration Error")!=null&&Schedule.SpawnedCount==0&&expectedErrors==2,"Invalid plan also blocks correctly from StageInitializer.Awake before GameplayUI.Start");Bind(stage);previous=Flow;Click("Retry");phase=9;}
        else if(phase==9){if(Flow==previous||GameObject.Find("Hand Card 1")==null)return;allowConfigurationError=false;Fresh();Error(invalidStage);Check(expectedErrors==3,"All deliberate configuration errors were explicitly diagnosed");Bind(stage);Click("Return to Lobby");phase=10;}
        else if(phase==10){if(Find<LobbyUI>()==null)return;Check(Schedule==null&&Registry==null&&File.ReadAllText(DeckPersistence.DefaultPath)==deckBytes,"Error UI returns to Lobby, tears down scheduling, and preserves deck save bytes");var saved=JsonUtility.FromJson<PlayerCurrencySaveData>(File.ReadAllText(PlayerCurrency.DefaultPath));Check(saved.Gold==startGold+50&&saved.Candy==startCandy+3,"Only real combat/clear rewards persist; errors/retries add no currency");Finish(true,checks+" parser/catalog/timing/UI/integration assertions; expected configuration errors="+expectedErrors);}
    }catch(Exception error){Finish(false,error.ToString());}}
    private static void Finish(bool pass,string message){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;if(!string.IsNullOrEmpty(tempJson))AssetDatabase.DeleteAsset(tempJson);foreach(var value in owned)if(value!=null)UnityEngine.Object.DestroyImmediate(value);owned.Clear();Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Json_Spawn_Result.txt",(pass?"PASS: ":"FAIL: ")+message);if(pass)Debug.Log("JSON_SPAWN_PASSED "+message);else Debug.LogError("JSON_SPAWN_FAILED "+message);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
}
}
