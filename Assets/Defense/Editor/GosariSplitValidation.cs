using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Disposable Play Mode fixture; never initializes the player's session or writes a real save.
[InitializeOnLoad]
public static class GosariSplitValidation {
 const string Key="Defense.GosariSplit.Validation";
 static int frame=-1,checks;
 static double started;
 static GameObject root;
 static GameFlow flow;
 static EnemyRegistry enemies;
 static EnemySpawner spawner;
 static FixedIntervalSpawner schedule;
 static BaseHealth health;
 static WaypointPath path;
 static GosariSplitValidation(){if(SessionState.GetBool(Key,false))Attach();}
 public static void Run(){
  if(EditorApplication.isPlaying)throw new Exception("Start outside Play Mode.");
  if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DEFENSE_TEST_CURRENCY_SAVE_PATH")))throw new Exception("Use isolated test save paths.");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Application.runInBackground=true;SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
 }
 static void Attach(){frame=-1;checks=0;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("GOSARI_SPLIT PASS: "+message);}
 static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
 static T Asset<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>("Assets/Defense/"+path);
 static void Reset(EnemyDefinition data){
  if(root!=null)UnityEngine.Object.DestroyImmediate(root);
  root=new GameObject("Gosari split fixture");flow=root.AddComponent<GameFlow>();enemies=root.AddComponent<EnemyRegistry>();Set(enemies,"plane",WaypointPlane.XY);health=root.AddComponent<BaseHealth>();health.Initialize(1000);health.Depleted+=flow.GameOver;
  var route=new GameObject("Route");route.transform.SetParent(root.transform);path=route.AddComponent<WaypointPath>();
  var points=new[]{new GameObject("Start").transform,new GameObject("Corner").transform,new GameObject("Base").transform};foreach(var point in points)point.SetParent(route.transform);points[1].position=Vector3.right*5;points[2].position=Vector3.right*1000;path.Configure(WaypointPlane.XY,points);
  spawner=root.AddComponent<EnemySpawner>();Set(spawner,"path",path);Set(spawner,"registry",enemies);Set(spawner,"flow",flow);Set(spawner,"baseHealth",health);Set(spawner,"enemyRoot",root.transform);
  // Custom generation behavior must survive a scene's generic sprite-prefab override.
  Set(spawner,"spritePrefab",Asset<GameObject>("Prefabs/TopDown2D/BossEnemy2D.prefab").GetComponent<Enemy>());
  schedule=root.AddComponent<FixedIntervalSpawner>();Set(schedule,"spawner",spawner);schedule.enabled=false;schedule.Configure(data,.1f,1);schedule.Advance(.1f);
 }
 static void Test(){
  var parentData=Asset<EnemyDefinition>("Data/Enemies/GosariBoss.asset");var childData=Asset<EnemyDefinition>("Data/Enemies/GosariChild.asset");var lastData=Asset<EnemyDefinition>("Data/Enemies/GosariGrandchild.asset");
  Check(parentData!=null&&childData!=null&&lastData!=null,"Three authored Gosari definitions exist");
  Check(parentData.EnemyTypeId=="enemy_gosari"&&parentData.Role==EnemyRole.Boss,"Parent stable ID and Boss role preserved");
  Check(parentData.DeathSplitChild==childData&&childData.DeathSplitChild==lastData&&lastData.DeathSplitChild==null,"Finite parent -> child -> grandchild chain");
  Check(parentData.Health>childData.Health&&childData.Health>lastData.Health&&parentData.MovementSpeed<childData.MovementSpeed&&childData.MovementSpeed<lastData.MovementSpeed,"Health decreases and actual speed increases each generation");
  Check(parentData.SpriteScale==1&&childData.SpriteScale==.75f&&lastData.SpriteScale==.5f,"Authored generation sprite sizes are 100 / 75 / 50 percent");
  Check(parentData.Health==2000&&childData.Health==800&&lastData.Health==400,"Prototype generation health values");
  Check(childData.BaseDamage*2==parentData.BaseDamage&&lastData.BaseDamage*2==childData.BaseDamage,"Splits conserve total potential Base damage");
  var catalog=Resources.Load<EnemyDefinitionCatalog>(EnemyDefinitionCatalog.ResourcePath);Check(catalog.TryBuildLookup(out var lookup,out var issue)&&lookup[parentData.EnemyTypeId]==parentData&&lookup[childData.EnemyTypeId]==childData&&lookup[lastData.EnemyTypeId]==lastData,"Stable IDs registered without duplicates: "+issue);
  Reset(parentData);var parent=enemies.Enemies.Single();Check(parent is DeathSplitEnemy,"Scene sprite override preserves custom death-split actor");
  var parentVisual=parent.GetComponentInChildren<SpriteRenderer>();var originalScale=parentVisual.transform.localScale;var originalBounds=parentVisual.bounds.size;var rootScale=parent.transform.localScale;float colliderRadius=parent.GetComponent<CircleCollider2D>().radius;
  parent.Movement.enabled=false;parent.Movement.Advance(10);var deathPoint=parent.transform.position;int waypoint=parent.Movement.NextWaypoint;
  int scheduledRewards=0;spawner.EnemyDefeated+=_=>scheduledRewards++;
  var stage=Asset<StageDefinition>("Data/Stage1_2.asset");var victory=root.AddComponent<StageVictoryController>();victory.enabled=false;
  var currency=new PlayerCurrency(new JsonFileStore<PlayerCurrencySaveData>(Environment.GetEnvironmentVariable("DEFENSE_TEST_CURRENCY_SAVE_PATH"),"Gosari fixture currency"));victory.Initialize(flow,schedule,enemies,stage,currency,()=>{});
  bool gap=false;bool monitor=true;enemies.Unregistered+=_=>{if(monitor&&enemies.AliveEnemyCount==0)gap=true;};
  parent.ReceiveDamage(parentData.Health-1);Check(enemies.AliveEnemyCount==1&&parent.IsAlive,"Nonlethal damage never splits");parent.ReceiveDamage(1);
  var children=enemies.Enemies.ToArray();Check(children.Length==2&&children.All(e=>e.Definition==childData&&e is DeathSplitEnemy),"Combat death produces exactly two first-generation children");
  Check(children.All(e=>Vector3.Distance(e.GetComponentInChildren<SpriteRenderer>().transform.localScale,originalScale*.75f)<.001f&&Vector3.Distance(e.GetComponentInChildren<SpriteRenderer>().bounds.size,originalBounds*.75f)<.001f),"Child sprite transform and visible bounds shrink to 75 percent");
  var view=children[0].GetComponent<EnemySpriteView>();view.enabled=false;view.enabled=true;view.enabled=false;view.enabled=true;
  Check(Vector3.Distance(children[0].GetComponentInChildren<SpriteRenderer>().transform.localScale,originalScale*.75f)<.001f,"Sprite reactivation does not compound shrinkage");
  Check(children.All(e=>e.transform.localScale==rootScale&&e.GetComponent<CircleCollider2D>().radius==colliderRadius),"Sprite scaling preserves root scale and collider size");
  Check(children.All(e=>e.CurrentHealth==childData.Health&&Mathf.Abs(e.Movement.BaseMovementSpeed-childData.MovementSpeed)<.001f&&e.CanSplit),"Child health/speed and splitting permission applied");
  Check(children.All(e=>e.Movement.Route==path&&e.Movement.NextWaypoint>=waypoint)&&children.Any(e=>Vector3.Distance(e.transform.position,deathPoint)<.001f),"Children inherit parent's route and progress instead of restarting");
  Check(Vector3.Distance(children[0].transform.position,children[1].transform.position)>.1f,"Siblings are separated along the track");
  Check(!gap&&!victory.EvaluateClear()&&flow.IsPlaying,"No registry gap or premature Victory after parent death");
  parent.ReceiveDamage(100000);Check(enemies.AliveEnemyCount==2&&scheduledRewards==1,"Repeated parent damage cannot duplicate children or root reward event");
  foreach(var child in children){child.Movement.enabled=false;child.ReceiveDamage(100000);child.ReceiveDamage(100000);}
  var grandchildren=enemies.Enemies.ToArray();Check(grandchildren.Length==4&&grandchildren.All(e=>e.Definition==lastData&&e is DeathSplitEnemy),"Two child deaths produce exactly four grandchildren");
  Check(grandchildren.All(e=>Vector3.Distance(e.GetComponentInChildren<SpriteRenderer>().transform.localScale,originalScale*.5f)<.001f&&Vector3.Distance(e.GetComponentInChildren<SpriteRenderer>().bounds.size,originalBounds*.5f)<.001f),"Grandchild sprite transform and visible bounds shrink to 50 percent");
  Check(grandchildren.All(e=>e.CurrentHealth==lastData.Health&&Mathf.Abs(e.Movement.BaseMovementSpeed-lastData.MovementSpeed)<.001f&&!e.CanSplit),"Grandchildren are weaker/faster and cannot split");
  Check(!gap&&!victory.EvaluateClear()&&flow.IsPlaying,"Grandchildren keep the stage active");
  Check(grandchildren.All(e=>e.GetComponentInChildren<SpriteRenderer>().sprite==parentData.Sprite),"All generations reuse original Gosari artwork");
  monitor=false;foreach(var enemy in grandchildren){enemy.ReceiveDamage(100000);enemy.ReceiveDamage(100000);}
  Check(enemies.AliveEnemyCount==0&&scheduledRewards==1,"Final four deaths create no further enemies or farmable spawn rewards");
  Check(victory.EvaluateClear()&&flow.State==GameplayState.Victory,"Final generation removal permits clear");
  foreach(var definition in new[]{parentData,childData,lastData}){
   Reset(definition);var enemy=enemies.Enemies.Single();enemy.Movement.enabled=false;int kills=0;enemy.Died+=_=>kills++;enemy.Movement.Advance(10000);
   Check(enemies.AliveEnemyCount==0&&health.Current==1000-definition.BaseDamage&&kills==0,"Base arrival never splits or emits a kill: "+definition.EnemyTypeId);
  }
  Reset(parentData);var cancellable=enemies.Enemies.Single();UnityEngine.Object.DestroyImmediate(cancellable.gameObject);Check(enemies.AliveEnemyCount==0,"Scene cleanup/destruction does not trigger splitting");
  Reset(parentData);var beforePause=enemies.Enemies.Single();flow.TogglePause();Check(!spawner.CanSpawn&&enemies.AliveEnemyCount==1,"Pause does not create children");flow.TogglePause();
  var originalBoss=Asset<EnemyDefinition>("Data/Enemies/BossEnemy.asset");Check(originalBoss.Prefab is SummoningEnemy,"Existing periodic-summon boss uses SummoningEnemy");
  UnityEngine.Object.DestroyImmediate(beforePause.gameObject);var broccoli=(SummoningEnemy)spawner.Spawn(originalBoss);broccoli.Movement.enabled=false;broccoli.AdvanceAbilities(originalBoss.SummonInterval);
  Check(enemies.AliveEnemyCount==1+originalBoss.SummonCount,"Existing Broccoli summon behavior still works");
  var napaData=Asset<EnemyDefinition>("Data/Enemies/BaechuBoss.asset");
  Check(napaData.Prefab.GetType()==typeof(BasicEnemy)&&napaData.Role==EnemyRole.Basic&&napaData.Health==2000,"Napa is basic with original HP");
  var napa=spawner.Spawn(napaData);napa.Movement.enabled=false;
  Check(napa.GetType()==typeof(BasicEnemy)&&napa.CurrentHealth==2000,"Napa does not use the scene boss fallback");
  var dorajiData=Asset<EnemyDefinition>("Data/Enemies/DorajiBoss.asset");
  Check(dorajiData.Prefab is SummoningEnemy&&dorajiData.Health==4000,"Doraji uses SummoningEnemy and original HP");
  var doraji=spawner.Spawn(dorajiData) as SummoningEnemy;doraji.Movement.enabled=false;
  int beforeSummon=enemies.AliveEnemyCount;doraji.AdvanceAbilities(dorajiData.SummonInterval);
  Check(enemies.AliveEnemyCount==beforeSummon+dorajiData.SummonCount,"Doraji retains periodic summoning");
 }
 static void Tick(){if(!SessionState.GetBool(Key,false))return;try{if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Gosari validation timed out");if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(frame<0){frame=Time.frameCount;return;}if(Time.frameCount-frame<3)return;Test();Finish(true);}catch(Exception exception){Finish(false,exception.ToString());}}
 static void Log(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);}
 static void Finish(bool ok,string error=null){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;if(root!=null)UnityEngine.Object.DestroyImmediate(root);Time.timeScale=1;if(ok)Debug.Log("GOSARI_SPLIT_PASSED "+checks+" assertions");else Debug.LogError("GOSARI_SPLIT_FAILED "+error);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);else EditorApplication.isPlaying=false;}
}
}
