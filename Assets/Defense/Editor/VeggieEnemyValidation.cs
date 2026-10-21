using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Uses actual EnemySpawner/Turret/Skill/reward implementations with isolated temporary currency.
[InitializeOnLoad]
public static class VeggieEnemyValidation {
    private const string Key="Defense.VeggieValidation.Running";
    private static int step,index,checks,deaths,rolls,expectedCandy;
    private static double started;
    private static float sampleTime;
    private static Vector3 samplePosition;
    private static Enemy probe;
    private static Turret turret;
    private static bool shot;
    private static double sample=.05;
    private static EnemyDefinition[] definitions;
    private static PlayerCurrency currency;
    private static CardDefinition[] deck;
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static EnemyRegistry Registry=>Find<EnemyRegistry>();
    private static EnemySpawner Spawner=>Find<EnemySpawner>();
    private static GameFlow Flow=>Find<GameFlow>();
    static VeggieEnemyValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){step=index=checks=deaths=rolls=expectedCandy=0;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string text,string trace,LogType type){
        if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense.")){Directory.CreateDirectory("Documentation/Report");File.AppendAllText("Documentation/Report/Veggie_EditorSearch_Warnings.txt",text+"\n"+trace+"\n");return;}
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,text+"\n"+trace);
    }
    private static void Check(bool pass,string text){if(!pass)throw new Exception(text);checks++;Debug.Log("VEGGIE PASS: "+text);}
    private static void Set(UnityEngine.Object target,string name,float value){var data=new SerializedObject(target);data.FindProperty(name).floatValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
    private static Enemy Spawn(EnemyDefinition data,Vector3 position){var e=Spawner.Spawn(data);e.Movement.Stop();e.transform.position=position;Registry.NotifyMoved(e);return e;}
    private static void Move(Enemy enemy,Vector3 position){enemy.transform.position=position;Registry.NotifyMoved(enemy);}
    private static PersistentSlowArea Area(SkillDefinition data,Vector3 position){var old=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None);data.Effect.Apply(data,new SkillCastContext(position,Registry,Flow));return UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None).Except(old).Single();}
    private static void ClearAreas(){foreach(var a in UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(a.gameObject);}
    private static void InspectContent(){
        var basic=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Defense/Data/BasicEnemy.asset");
        definitions=VeggieEnemySetup.Names.Select(n=>AssetDatabase.LoadAssetAtPath<EnemyDefinition>(VeggieEnemySetup.DataRoot+"/"+n+"Enemy.asset")).ToArray();
        Check(definitions.Length==10&&definitions.All(d=>d!=null)&&definitions.Distinct().Count()==10&&definitions.Select(d=>d.EnemyTypeId).Distinct().Count()==10,"Ten independently addressable definitions have unique stable enemy IDs");
        for(int i=0;i<definitions.Length;i++){
            var data=definitions[i];string path=VeggieEnemySetup.ArtRoot+"/"+VeggieEnemySetup.Names[i]+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Check(data.Sprite!=null&&data.Sprite.name==VeggieEnemySetup.Names[i]&&AssetDatabase.GetAssetPath(data.Sprite)==path,"Exact individual Sprite/name mapping: "+data.DisplayName);
            Check(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Single&&importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed&&importer.spritePixelsPerUnit==32&&importer.alphaIsTransparency&&importer.alphaSource==TextureImporterAlphaSource.FromInput,"Point/Single/32PPU/uncompressed/no-mipmap/alpha import: "+data.DisplayName);
            Check(data.Sprite.rect.size==new Vector2(32,32)&&Vector2.Distance(data.Sprite.bounds.size,Vector2.one)<.001f,"Original 32x32 canvas is preserved at consistent world scale: "+data.DisplayName);
            Check(AssetDatabase.GetAssetPath(data.Prefab)==VeggieEnemySetup.SharedPrefab,"Common native 2D prefab, no per-type Enemy subclass: "+data.DisplayName);
            bool specified = data.EnemyTypeId == "enemy_tomato" || data.EnemyTypeId == "enemy_potato";
            Check((specified ? (data.EnemyTypeId == "enemy_tomato" ? data.Health==85 && data.MovementSpeed==3 && data.BaseDamage==100 : data.Health==150 && data.MovementSpeed==1.8f && data.BaseDamage==20) : data.Health==basic.Health && data.MovementSpeed==basic.MovementSpeed && data.BaseDamage==basic.BaseDamage) && data.ResourceReward==basic.ResourceReward && data.CandyDropChance==basic.CandyDropChance && data.CandyDropAmount==basic.CandyDropAmount,"Explicit Tomato/Potato patch stats; unrelated variants and all reward policies unchanged: "+data.DisplayName);
        }
    }
    private static void BeginVariant(){
        shot=false;sample=.05;var definition=definitions[index];probe=Spawner.Spawn(definition);var sprite=probe.GetComponentInChildren<SpriteRenderer>();
        Check(probe.GetType()==typeof(BasicEnemy)&&probe.GetComponents<EnemyPathFollower>().Length==1&&probe.GetComponents<EnemyHealth>().Length==1&&probe.GetComponents<EnemySpriteView>().Length==1,"Shared Enemy runtime and one presentation observer: "+definition.DisplayName);
        Check(probe.Definition==definition&&sprite.sprite==definition.Sprite&&sprite.color==Color.white&&sprite.sortingOrder==40,"Definition is visually bound synchronously and untinted before first frame: "+definition.DisplayName);
        var hit=probe.GetComponent<CircleCollider2D>();Check(hit!=null&&hit.isTrigger&&hit.radius==.3f&&probe.GetComponent<Collider>()==null&&probe.GetComponent<Rigidbody>()==null&&Vector2.Distance(hit.bounds.size,new Vector2(.6f,.6f))<.001f&&Vector2.Distance(sprite.bounds.size,new Vector2(1.2f,1.2f))<.001f,"Doubled 1.2-unit Sprite canvas with unchanged .3-radius 2D hitbox, no 3D physics: "+definition.DisplayName);
        Check(probe.CurrentHealth==definition.Health&&probe.Movement.BaseMovementSpeed==definition.MovementSpeed&&Registry.Enemies.Contains(probe),"Definition statistics and alive registry initialize normally: "+definition.DisplayName);
        sampleTime=Time.time;samplePosition=probe.transform.position;
    }
    private static void BeginTower(){
        var data=definitions[index];Check(probe.transform.position.x<samplePosition.x&&Mathf.Abs(probe.transform.position.z)<.001f&&Mathf.Abs(Vector3.Distance(probe.transform.position,samplePosition)-data.MovementSpeed*(Time.time-sampleTime))<.03f,"Actual advancing-frame XY movement uses configured speed: "+data.DisplayName);
        probe.Movement.Stop();probe.ReceiveDamage(5);Check(probe.CurrentHealth==data.Health-5&&Mathf.Abs(probe.GetComponent<EnemyHealthBar>().NormalizedHealth-probe.CurrentHealth/data.Health)<.001f,"Damage API and existing HP bar update: "+data.DisplayName);
        var towerData=AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/BasicTurret.asset");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TopDownStage2Setup.Prefabs+"/BasicTurret2D.prefab").GetComponent<Turret>();
        turret=UnityEngine.Object.Instantiate(prefab,probe.transform.position+Vector3.left*.5f,Quaternion.identity);turret.Initialize(towerData,Registry);
        probe.GetComponent<EnemyHealth>().Changed+=(current,max)=>{if(current<data.Health-5){shot=true;turret.enabled=false;}};
        Check(towerData.Targeting.Select(turret.transform.position,towerData.AttackRange,Registry.Enemies,Registry.Plane)==probe,"Existing Closest targeting selects veggie actor: "+data.DisplayName);
    }
    private static void TestCombatAndRewards(){
        var definition=definitions[index];var towerData=AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/BasicTurret.asset");
        Check(probe.CurrentHealth==definition.Health-5-towerData.Damage,"Actual Turret Update attacks the veggie through common damage behavior: "+definition.DisplayName);
        var skills=AssetDatabase.FindAssets("t:SkillCardDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<SkillCardDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        var toy=skills.First(s=>s.Skill.Effect is CircularDamageSkill).Skill;var slow=skills.First(s=>s.Skill.Effect is SlowAreaSkill).Skill;
        var configured=UnityEngine.Object.Instantiate(definition);Set(configured,"candyDropChance",.5f);var so=new SerializedObject(configured);so.FindProperty("candyDropAmount").intValue=index+2;so.ApplyModifiedPropertiesWithoutUndo();
        var other=Spawn(configured,new Vector3(toy.Radius+.02f,0,-8));Move(probe,new Vector3(0,0,8));int beforeDeaths=deaths,beforeRolls=rolls,beforeResources=Find<ResourceWallet>().Balance;int ownDeaths=0;probe.Died+=e=>ownDeaths++;
        toy.Effect.Apply(toy,new SkillCastContext(Vector3.zero,Registry,Flow));probe.ReceiveDamage(10000);
        expectedCandy+=definition.CandyDropAmount;
        Check(!probe.IsAlive&&probe.CurrentHealth==0&&!probe.GetComponent<EnemyHealthBar>().IsVisible&&other.CurrentHealth==configured.Health,"Toy Throw kills inside XY circle and excludes outside despite render Z: "+definition.DisplayName);
        Check(ownDeaths==1&&deaths==beforeDeaths+1&&rolls==beforeRolls+1&&currency.Candy==expectedCandy&&Find<ResourceWallet>().Balance==beforeResources+definition.ResourceReward&&!Registry.Enemies.Contains(probe),"Combat death unregisters and awards resources/Candy only once: "+definition.DisplayName);
        Move(other,Vector3.zero);var a=Area(slow,Vector3.zero);var b=Area(slow,Vector3.right);float speed=other.Movement.BaseMovementSpeed;
        Check(other.Movement.Modifiers.SourceCount==2&&other.Movement.EffectiveMovementSpeed==speed*.5f,"Two Slow Areas use independent nonmultiplying sources: "+definition.DisplayName);
        Move(other,new Vector3(slow.Radius+.5f,0,20));Check(a.AffectedCount==0&&b.AffectedCount==1&&other.Movement.Modifiers.SourceCount==1,"Leaving A preserves B immediately in XY: "+definition.DisplayName);
        Move(other,Vector3.zero);a.Advance(slow.ActiveDuration+.01f);Check(!a.IsActive&&other.Movement.Modifiers.SourceCount==1,"Area expiry removes only its own slow source: "+definition.DisplayName);b.Advance(slow.ActiveDuration+.01f);Check(other.Movement.Modifiers.SourceCount==0&&other.Movement.EffectiveMovementSpeed==speed,"Final expiry restores original movement speed: "+definition.DisplayName);
        ClearAreas();a=Area(slow,Vector3.zero);b=Area(slow,Vector3.right);sample=.75;other.ReceiveDamage(100000);
        Check(a.AffectedCount==0&&b.AffectedCount==0&&other.Movement.Modifiers.SourceCount==0&&currency.Candy==expectedCandy,"Death clears all active slow sources and respects per-definition failed Candy roll: "+definition.DisplayName);
        sample=.25;var rewarded=Spawn(configured,Vector3.zero);rewarded.ReceiveDamage(100000);rewarded.ReceiveDamage(100000);expectedCandy+=index+2;
        Check(currency.Candy==expectedCandy&&definition.CandyDropChance==.1f&&definition.CandyDropAmount==1,"Independent per-definition chance/amount feed same Candy pipeline: "+definition.DisplayName);
        var escape=Spawner.Spawn(definition);int hp=Find<BaseHealth>().Current;beforeDeaths=deaths;beforeRolls=rolls;escape.Movement.Advance(10000);
        Check(Find<BaseHealth>().Current==hp-definition.BaseDamage&&deaths==beforeDeaths&&rolls==beforeRolls&&currency.Candy==expectedCandy,"Path endpoint deals Base damage and is not a loot-producing death: "+definition.DisplayName);
        var removed=Spawn(definition,Vector3.zero);beforeDeaths=deaths;beforeRolls=rolls;UnityEngine.Object.DestroyImmediate(removed.gameObject);
        Check(Registry.AliveEnemyCount==0&&a.AffectedCount==0&&b.AffectedCount==0&&deaths==beforeDeaths&&rolls==beforeRolls,"Arbitrary despawn inside zones clears tracking without death/rewards: "+definition.DisplayName);
        ClearAreas();UnityEngine.Object.DestroyImmediate(turret.gameObject);UnityEngine.Object.DestroyImmediate(configured);
        Check(JsonUtility.FromJson<PlayerCurrencySaveData>(File.ReadAllText(currency.SavePath)).Candy==expectedCandy,"Successful Candy is immediately saved in isolated fixture: "+definition.DisplayName);
    }
    private static void VerifyFiniteAndFallback(){
        var basic=AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Defense/Data/BasicEnemy.asset");var legacy=Spawner.Spawn(basic);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(VeggieEnemySetup.SharedPrefab).GetComponentInChildren<SpriteRenderer>();var view=legacy.GetComponentInChildren<SpriteRenderer>();
        Check(view.sprite==prefab.sprite&&view.color==prefab.color&&basic.Sprite==null,"Existing BasicEnemy keeps original placeholder Sprite/tint when no artwork is assigned");UnityEngine.Object.DestroyImmediate(legacy.gameObject);
        var stage=AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage.asset");Check(stage.TotalEnemies==5&&stage.ClearGoldReward==50&&stage.Enemy.name=="RewardTestEnemy"&&stage.Enemy.Sprite==null,"Original five-enemy reward Test Stage configuration remains unchanged");
        var victory=Registry.gameObject.AddComponent<StageVictoryController>();int completions=0;var schedule=Find<FixedIntervalSpawner>();victory.Initialize(Flow,schedule,Registry,stage,currency,()=>completions++,true);
        sample=.05;schedule.Configure(definitions[0],.1f,5);for(int i=0;i<8;i++)schedule.Advance(.1f);
        Check(schedule.SpawnedCount==5&&Registry.AliveEnemyCount==5&&Registry.Enemies.All(e=>e.Definition==definitions[0]&&e.GetComponentInChildren<SpriteRenderer>().sprite==definitions[0].Sprite),"Existing configurable finite schedule spawns five veggie actors without new spawner logic");
        foreach(var e in Registry.Enemies.ToArray())e.ReceiveDamage(100000);expectedCandy+=5;
        Check(victory.EvaluateClear()&&Flow.State==GameplayState.Victory&&victory.DefeatedCount==5&&currency.Gold==50&&currency.Candy==expectedCandy&&completions==1,"Scheduled veggie deaths integrate with unchanged Victory/Gold/Candy flow");
        Check(!victory.EvaluateClear()&&currency.Gold==50&&completions==1,"Repeated evaluation cannot duplicate progression or Gold");
        Check(Find<TopDownInteractionSandbox>().Deck.Slots.SequenceEqual(deck)&&PlayerSession.Presets==null&&PlayerSession.Currency==null,"Focused checks never initialize/edit persistent player Deck/Hand or currency services");
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>60)throw new Exception("Veggie validation timeout at variant "+index+" phase "+step);
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||Find<TopDownInteractionSandbox>()==null||GameObject.Find("Hand Card 1")==null)return;
        if(step==0){Find<FixedIntervalSpawner>().enabled=false;Find<BaseHealth>().Initialize(10000);deck=Find<TopDownInteractionSandbox>().Deck.Slots.ToArray();InspectContent();
            var folder=Path.Combine(Path.GetTempPath(),"defense-veggie-"+Guid.NewGuid().ToString("N"));currency=new PlayerCurrency(new JsonFileStore<PlayerCurrencySaveData>(Path.Combine(folder,"currency.json"),"Veggie test currency"));
            var drops=Registry.gameObject.AddComponent<CandyDropSystem>();drops.Initialize(Spawner,currency,()=>{rolls++;return sample;});Spawner.EnemyDefeated+=e=>deaths++;BeginVariant();step=1;
        }else if(step==1&&Time.time-sampleTime>=.2f){BeginTower();step=2;}
        else if(step==2&&shot){TestCombatAndRewards();index++;if(index<definitions.Length){BeginVariant();step=1;}else {VerifyFiniteAndFallback();Finish(true,checks+" assertions across all ten variants");}}
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Finish(bool pass,string message){if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Veggie_Validation_Result.txt",(pass?"PASS: ":"FAIL: ")+message);if(pass)Debug.Log("VEGGIE_VALIDATION_PASSED "+message);else Debug.LogError("VEGGIE_VALIDATION_FAILED "+message);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
}
}
