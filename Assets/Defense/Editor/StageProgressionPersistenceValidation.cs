using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
[InitializeOnLoad]
public static class StageProgressionPersistenceValidation {
 const string Key="Defense.ProgressionPersistence.Validation";
 static int checks,frame;
 static double started;
 static readonly List<UnityEngine.Object> temporary=new List<UnityEngine.Object>();
 static StageProgressionPersistenceValidation(){if(SessionState.GetBool(Key,false))Attach();}
 public static void Run()=>Start(0);
 public static void WriteRestartFixture()=>Start(1);
 public static void ReadRestartFixture()=>Start(2);
 static void Start(int mode){
  if(EditorApplication.isPlaying)throw new Exception("Start outside Play Mode");
  foreach(var name in new[]{"DEFENSE_TEST_PROGRESS_SAVE_PATH","DEFENSE_TEST_CURRENCY_SAVE_PATH","DEFENSE_TEST_DECK_SAVE_PATH"})if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))throw new Exception("Isolated test paths required: "+name);
  if(Path.GetFullPath(StageProgressionPersistence.DefaultPath).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new Exception("Never validate real player progress");
  SessionState.SetInt(Key+".Mode",mode);SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Application.runInBackground=true;Attach();EditorApplication.isPlaying=true;
 }
 static void Attach(){checks=0;frame=-1;started=EditorApplication.timeSinceStartup;temporary.Clear();EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("PROGRESS_SAVE PASS: "+message);}
 static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
 static StageDefinition[] Stages(params string[] ids)=>ids.Select(id=>{var stage=ScriptableObject.CreateInstance<StageDefinition>();temporary.Add(stage);stage.name=id;Set(stage,"stageId",id);return stage;}).ToArray();
 static StageProgressionState State(StageDefinition[] stages,string path,List<string> messages=null)=>new StageProgressionState(stages,new StageProgressionPersistence(path,messages==null?(Action<string>)(_=>{}):messages.Add,messages==null?(Action<string>)(_=>{}):messages.Add));
 static void Models(){
  string directory=Path.Combine(Path.GetDirectoryName(StageProgressionPersistence.DefaultPath),"models-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);string path=Path.Combine(directory,"progress.json");var stages=Stages("one","two","three");
  var progress=State(stages,path);Check(progress.IsUnlocked(0)&&!progress.IsUnlocked(1)&&!progress.IsComplete(0),"New profile starts with only first campaign stage unlocked");
  Check(!File.Exists(path)&&!progress.Complete(1)&&!progress.Complete(-1)&&!progress.Complete(3),"Locked/invalid completions create no file or records");
  int changes=0;progress.Changed+=()=>changes++;Check(progress.Complete(0)&&File.Exists(path)&&progress.LastSaveSucceeded,"First clear automatically creates progress save");
  Check(progress.IsComplete(0)&&progress.IsUnlocked(1)&&!progress.IsUnlocked(2)&&changes==1,"First clear unlocks only the next stage and notifies once");
  string saved=File.ReadAllText(path);Check(!progress.Complete(0)&&changes==1&&File.ReadAllText(path)==saved,"Repeat clear is idempotent");
  progress=State(stages,path);Check(progress.IsComplete(0)&&progress.IsUnlocked(1)&&!progress.IsUnlocked(2),"Fresh model restores completion and next-stage unlock");
  Check(progress.Complete(1)&&State(stages,path).IsUnlocked(2),"Second clear persists third-stage unlock");
  var reordered=Stages("two","one","new","three");var restored=State(reordered,path);Check(restored.IsComplete(0)&&restored.IsComplete(1)&&!restored.IsComplete(2),"Completion follows stable IDs after reorder/insertion");
  Check(restored.IsUnlocked(3),"Previously unlocked stage stays unlocked after inserting a locked predecessor");
  stages[0].name="Renamed asset";Set(stages[0],"displayName","New display title");Check(State(stages,path).IsComplete(0),"Asset/display renaming preserves explicit stable-ID progress");
  var memory=new StageProgressionState(3);Check(memory.Complete(0)&&memory.IsUnlocked(1)&&memory.SavePath==null,"Legacy in-memory constructor remains compatible");
  var duplicate=Stages("same","same");bool rejected=false;try{State(duplicate,path);}catch(ArgumentException){rejected=true;}Check(rejected,"Ambiguous stage IDs are rejected");
  string malformed=Path.Combine(directory,"malformed.json");File.WriteAllText(malformed,"{broken JSON");var messages=new List<string>();var repair=State(stages,malformed,messages);Check(messages.Count>0&&repair.IsUnlocked(0)&&!repair.IsComplete(0),"Malformed file falls back safely with warning");
  Check(repair.Complete(0)&&State(stages,malformed).IsComplete(0),"A subsequent clear repairs malformed progress through atomic storage");
  string future=Path.Combine(directory,"future.json");string newer="{\"Version\":99,\"CompletedStageIds\":[\"one\"]}";File.WriteAllText(future,newer);var protectedState=State(stages,future,messages);protectedState.Complete(0);Check(!protectedState.LastSaveSucceeded&&File.ReadAllText(future)==newer,"Unsupported newer version is not overwritten");
  string mixed=Path.Combine(directory,"mixed.json");File.WriteAllText(mixed,"{\"Version\":1,\"CompletedStageIds\":[\"one\",\"one\",null,\"retired\"],\"UnlockedStageIds\":[\"two\",null,\"retired\"]}");
  var mixedState=State(stages,mixed);Check(mixedState.IsComplete(0)&&mixedState.Complete(1),"Duplicate/empty saved entries do not break valid progress");
  var data=JsonUtility.FromJson<StageProgressionSaveData>(File.ReadAllText(mixed));Check(data.CompletedStageIds.Distinct().Count()==data.CompletedStageIds.Length&&data.CompletedStageIds.Contains("retired")&&data.UnlockedStageIds.Contains("retired"),"Unknown temporarily removed stage IDs survive later saves");
  var returning=Stages("one","two","retired");Check(State(returning,mixed).IsComplete(2),"Returning content recovers saved completion");
  var development=Stages("a","b","test");Set(development[2],"alwaysUnlocked",true);Check(new StageProgressionState(development).IsUnlocked(2),"Existing AlwaysUnlocked stage policy is retained");
  string blocked=Path.Combine(directory,"blocking-file");File.WriteAllText(blocked,"block directory creation");var failed=State(stages,Path.Combine(blocked,"progress.json"),messages);Check(failed.Complete(0)&&failed.IsComplete(0)&&failed.IsUnlocked(1)&&!failed.LastSaveSucceeded,"Write failure keeps in-memory progress and exposes failure");
  File.Delete(blocked);Directory.CreateDirectory(blocked);Check(!failed.Complete(0)&&failed.LastSaveSucceeded&&State(stages,Path.Combine(blocked,"progress.json")).IsComplete(0),"Repeated clear retries failed storage successfully");
  string explicitPath=Environment.GetEnvironmentVariable("DEFENSE_TEST_PROGRESS_SAVE_PATH");Environment.SetEnvironmentVariable("DEFENSE_TEST_PROGRESS_SAVE_PATH",null);try{Check(StageProgressionPersistence.DefaultPath==Path.Combine(Path.GetDirectoryName(PlayerCurrency.DefaultPath),StageProgressionPersistence.FileName),"Existing currency/deck test isolation also isolates progress");}finally{Environment.SetEnvironmentVariable("DEFENSE_TEST_PROGRESS_SAVE_PATH",explicitPath);}
 }
 static void Bind(StageDefinition stage)=>typeof(PlayerSession).GetMethod("BindActiveStage",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{stage});
 static GameObject Fixture(StageDefinition stage,out GameFlow flow,out StageVictoryController victory){
  var root=new GameObject("Real completion fixture");temporary.Add(root);flow=root.AddComponent<GameFlow>();var health=root.AddComponent<BaseHealth>();health.Initialize(1000);health.Depleted+=flow.GameOver;var registry=root.AddComponent<EnemyRegistry>();Set(registry,"plane",WaypointPlane.XY);
  var pathObject=new GameObject("Path");pathObject.transform.SetParent(root.transform);var path=pathObject.AddComponent<WaypointPath>();var a=new GameObject("Start").transform;var b=new GameObject("End").transform;a.SetParent(root.transform);b.SetParent(root.transform);b.position=Vector3.right*1000;path.Configure(WaypointPlane.XY,new[]{a,b});
  var spawner=root.AddComponent<EnemySpawner>();Set(spawner,"flow",flow);Set(spawner,"path",path);Set(spawner,"registry",registry);Set(spawner,"baseHealth",health);Set(spawner,"enemyRoot",root.transform);
  var schedule=root.AddComponent<FixedIntervalSpawner>();Set(schedule,"spawner",spawner);schedule.enabled=false;schedule.Configure(stage.Enemy,.1f,1);schedule.Advance(.1f);
  victory=root.AddComponent<StageVictoryController>();victory.enabled=false;victory.Initialize(flow,schedule,registry,stage,PlayerSession.Currency,()=>PlayerSession.CompleteActiveStage());
  foreach(var enemy in registry.Enemies.ToArray())enemy.ReceiveDamage(100000);
  return root;
 }
 static void Integration(){
  PlayerSession.Ensure();var stages=PlayerSession.Catalog.Stages;Check(stages.Count>=3&&stages.Select(s=>s.StageId).Distinct().Count()==stages.Count,"Current campaign has unique stable save IDs");
  string deckPath=PlayerSession.Presets.SavePath;string deck=File.ReadAllText(deckPath),currency=File.ReadAllText(PlayerCurrency.DefaultPath);
  Bind(stages[0]);var fixture=Fixture(stages[0],out var flow,out var victory);Check(victory.EvaluateClear()&&flow.State==GameplayState.Victory,"Actual StageVictoryController invokes completion callback");
  Check(File.Exists(StageProgressionPersistence.DefaultPath)&&PlayerSession.Progression.IsComplete(0)&&PlayerSession.Progression.IsUnlocked(1),"Victory automatically persists clear and unlock before scene exit");
  Check(PlayerSession.PendingTowerUnlockStage==1,"First clear schedules unlock popup");PlayerSession.PendingTowerUnlockStage=0;
  Check(!victory.EvaluateClear()&&!PlayerSession.CompleteActiveStage(),"Duplicate victory/completion cannot duplicate progress");
  Check(PlayerSession.PendingTowerUnlockStage==0,"Duplicate clear does not reschedule unlock popup");
  Check(File.ReadAllText(deckPath)==deck&&File.ReadAllText(PlayerCurrency.DefaultPath)==currency,"Progress save leaves existing deck and currency content unchanged");
  UnityEngine.Object.DestroyImmediate(fixture);
  typeof(PlayerSession).GetMethod("Reset",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);PlayerSession.Ensure();
  Check(PlayerSession.Progression.IsComplete(0)&&PlayerSession.Progression.IsUnlocked(1)&&!PlayerSession.Progression.IsUnlocked(2),"Session restart restores clear/lock state before lobby use");
  string saved=File.ReadAllText(StageProgressionPersistence.DefaultPath);Bind(stages[1]);fixture=Fixture(stages[1],out flow,out victory);flow.GameOver();Check(!victory.EvaluateClear()&&!PlayerSession.Progression.IsComplete(1)&&File.ReadAllText(StageProgressionPersistence.DefaultPath)==saved,"Game Over does not record a clear or unlock");UnityEngine.Object.DestroyImmediate(fixture);
  PlayerSession.Selection.Select(2);PlayerSession.DevMode=false;Check(!PlayerSession.TrySelectStageForPlay(out _),"Restored locked stage remains inaccessible normally");
  PlayerSession.DevMode=true;Check(PlayerSession.TrySelectStageForPlay(out _)&&!PlayerSession.Progression.IsUnlocked(2)&&File.ReadAllText(StageProgressionPersistence.DefaultPath)==saved,"Dev Mode entry bypass does not persist an unlock");PlayerSession.DevMode=false;
 }
 static void Restart(bool writing){
  PlayerSession.Ensure();
  if(writing){Check(!PlayerSession.Progression.IsComplete(0),"Writer starts with fresh temporary progress");Bind(PlayerSession.Catalog.Stages[0]);Check(PlayerSession.CompleteActiveStage()&&File.Exists(StageProgressionPersistence.DefaultPath),"Writer persists real PlayerSession clear");}
  else {Check(PlayerSession.Progression.IsComplete(0)&&PlayerSession.Progression.IsUnlocked(1)&&!PlayerSession.Progression.IsUnlocked(2),"Separate Unity process restores first clear and next unlock");Bind(PlayerSession.Catalog.Stages[1]);Check(PlayerSession.CompleteActiveStage(),"Restored next stage can be completed");var reloaded=new StageProgressionState(PlayerSession.Catalog.Stages,new StageProgressionPersistence(StageProgressionPersistence.DefaultPath));Check(reloaded.IsComplete(0)&&reloaded.IsComplete(1)&&reloaded.IsUnlocked(2),"Second clear also persists across reload");}
 }
 static void Tick(){if(!SessionState.GetBool(Key,false))return;try{if(EditorApplication.timeSinceStartup-started>60)throw new Exception("Progress validation timed out");if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(frame<0){frame=Time.frameCount;return;}if(Time.frameCount-frame<3)return;int mode=SessionState.GetInt(Key+".Mode",0);if(mode==0){Models();Integration();}else Restart(mode==1);Finish(true);}catch(Exception e){Finish(false,e.ToString());}}
 static void Log(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);}
 static void Finish(bool ok,string error=null){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;foreach(var obj in temporary)if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);temporary.Clear();Time.timeScale=1;if(ok)Debug.Log("PROGRESS_SAVE_PASSED "+checks+" assertions");else Debug.LogError("PROGRESS_SAVE_FAILED "+error);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);else EditorApplication.isPlaying=false;}
}
}
