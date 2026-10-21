using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Custom EditMode bookkeeping checks plus real PlayMode spawner/combat/lifecycle checks.
[InitializeOnLoad]
public static class EnemyRolesValidation {
    private const string Key="Defense.EnemyRoles.Validation";
    private static int checks,phase,frame,rolls,kills;
    private static double started,deadline;
    private static float stageTime,hp;
    private static Enemy liveTarget;
    private static Turret liveTower;
    private static TowerDisruptionAbility livePotato;
    private static object otherDisable;
    private static UnityEngine.Object[] retired;
    private static PlayerCurrency currency;
    private static string currencyPath;
    private static readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    private static readonly List<GameObject> slots = new List<GameObject>();
    private static T Find<T>() where T:UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>();
    private static EnemyRegistry Registry => Find<EnemyRegistry>();
    private static EnemySpawner Spawner => Find<EnemySpawner>();
    private static GameFlow Flow => Find<GameFlow>();
    private static EnemyDefinition Data(string name) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>(EnemyRolesSetup.PathFor(name));
    private static void Check(bool pass,string message) { if(!pass)throw new Exception(message);checks++;Debug.Log("ENEMY ROLE PASS: "+message); }
    private static void Near(float actual,float expected,string message) => Check(Mathf.Abs(actual-expected)<.001f,message);
    private static void Set(UnityEngine.Object value,string name,object field) {
        var data=new SerializedObject(value);var property=data.FindProperty(name);
        if(field is float f)property.floatValue=f;else if(field is int i)property.intValue=i;else if(field is bool b)property.boolValue=b;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static T Copy<T>(T data) where T:UnityEngine.Object { var copy=UnityEngine.Object.Instantiate(data);owned.Add(copy);return copy; }
    private static EnemyDefinition Tuned(string name,float radius=3) {
        var data=Copy(Data(name));Set(data,"supportArea",radius);Set(data,"disruptionRange",6f);Set(data,"disruptionCooldown",2f);Set(data,"disruptionDuration",1.5f);return data;
    }
    private static Enemy Spawn(EnemyDefinition data,Vector3 position) { var enemy=Spawner.Spawn(data);enemy.Movement.Stop();Move(enemy,position);return enemy; }
    private static void Move(Enemy enemy,Vector3 position) { enemy.transform.position=position;Registry.NotifyMoved(enemy); }
    private static Turret Tower(Vector3 position) {
        var slot=new GameObject("Enemy Role Test Slot");slot.transform.position=position;slots.Add(slot);var placement=slot.AddComponent<TowerPlacementSlot>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlacedTowerInspectionSetup.PrefabPath).GetComponent<Turret>();
        var turret=UnityEngine.Object.Instantiate(prefab,position,Quaternion.identity,slot.transform);
        var data=Copy(AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/BasicTurret.asset"));Set(data,"damage",7f);Set(data,"attackCooldown",.05f);Set(data,"attackRange",3f);
        turret.Initialize(data,Registry);Check(placement.TryOccupy(turret),"Fixture Tower uses an actual occupied placement slot");return turret;
    }
    private static void ClearWorld() {
        foreach(var area in UnityEngine.Object.FindObjectsByType<PersistentSlowArea>())UnityEngine.Object.DestroyImmediate(area.gameObject);
        foreach(var enemy in Registry.Enemies.ToArray())UnityEngine.Object.DestroyImmediate(enemy.gameObject);
        foreach(var slot in slots)if(slot!=null)UnityEngine.Object.DestroyImmediate(slot);slots.Clear();
    }
    static EnemyRolesValidation() { if(SessionState.GetBool(Key,false))Attach(); }
    public static void Run() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Start in Edit Mode.");
        string path=Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
        if(string.IsNullOrWhiteSpace(path)||Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Use an isolated save profile.");
        SessionState.SetString(Key+".Deck",File.ReadAllText(path));SessionState.SetString(Key+".Currency",File.ReadAllText(PlayerCurrency.DefaultPath));
        checks=0;Configuration();Bookkeeping();SessionState.SetInt(Key+".Checks",checks);
        EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach() {
        checks=SessionState.GetInt(Key+".Checks",0);phase=rolls=kills=0;frame=-1;started=EditorApplication.timeSinceStartup;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
    }
    private static void OnLog(string message,string trace,LogType type) {
        if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);
    }
    private static void Configuration() {
        float[] hp={100,85,150,30,200,150,2500},speed={2,3,1.7f,2,1.8f,1.8f,1.2f};int[] damage={100,100,100,20,30,20,0};
        var definitions=EnemyRolesSetup.Names.Select(Data).ToArray();
        Check(definitions.All(d=>d!=null)&&definitions.Select(d=>d.EnemyTypeId).Distinct().Count()==7,"Seven distinct stable role definitions exist");
        for(int i=0;i<definitions.Length;i++) {
            var data=definitions[i];Check(data.Health==hp[i]&&data.MovementSpeed==speed[i]&&data.BaseDamage==damage[i],"Exact specified HP/speed/endpoint damage: "+data.DisplayName);
            Check(data.EnemyTypeId=="enemy_"+EnemyRolesSetup.Names[i].ToLowerInvariant(),"Stable identity is independent from role and artwork: "+data.DisplayName);
            if(i<6) { Check(data.Sprite!=null&&data.Sprite.name==EnemyRolesSetup.Names[i],"Matching individual art, no unrelated substitutes: "+data.DisplayName);var import=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(data.Sprite));Check(import.textureType==TextureImporterType.Sprite&&import.spriteImportMode==SpriteImportMode.Single&&import.filterMode==FilterMode.Point&&import.spritePixelsPerUnit==32&&!import.mipmapEnabled&&import.alphaIsTransparency,"Consistent native 2D import: "+data.DisplayName); }
        }
        Check(Data("Tomato").Role==EnemyRole.Basic&&Data("Potato").Role==EnemyRole.Disruptor&&Data("Garlic").Role==EnemyRole.SpeedSupport&&Data("Broccoli").Role==EnemyRole.ShieldSupport&&Data("Boss").Role==EnemyRole.Boss,"Role identities match the selected first-pass roster");
        Check(Data("Garlic").SpeedIncrease==1.2f&&Data("Broccoli").ShieldValue==100,"Specified speed multiplier and shared shield value");
        Check(new[]{"Garlic","Broccoli","Potato"}.All(n=>!Data(n).AbilityConfigured&&!string.IsNullOrEmpty(Data(n).AbilityConfigurationIssue)),"Unspecified balance fields remain visibly unconfigured");
        Check(!Data("Boss").HasConfiguredBaseDamage&&Data("Boss").Sprite==null,"Boss damage/art remain pending, without invented mechanics");
        Check(AssetDatabase.FindAssets("t:EnemyDefinition").All(g=>AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(g)).EnemyTypeId!="enemy_radish"),"Radish remains an unelected alternative");
        var stage=AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage.asset");Check(stage.TotalEnemies==5&&stage.SpawnSequence.Count==0&&stage.Enemy.name=="RewardTestEnemy"&&stage.BaseHealth==3&&stage.StartingResources==300&&stage.SpawnInterval==.75f&&stage.ClearGoldReward==50,"Original repeatable five-enemy stage configuration is unchanged");
    }
    private static void Bookkeeping() {
        var modifiers=new MovementModifiers();var slow=new object();var boost=new object();var strong=new object();
        modifiers.SetSlow(slow,.5f);modifiers.SetSpeedBoost(boost,1.2f);Near(modifiers.Multiplier,.6f,"Strongest boost multiplies existing strongest slow");
        for(int i=0;i<100;i++)modifiers.SetSpeedBoost(boost,1.2f);Check(modifiers.SourceCount==2,"Repeated same-source boost does not stack");
        modifiers.SetSpeedBoost(strong,2);Near(modifiers.Multiplier,1,"Overlapping Garlic sources use strongest boost, not compounding");
        modifiers.Remove(strong);Near(modifiers.Multiplier,.6f,"Removing strong source restores weaker boost with slow intact");modifiers.Remove(boost);Near(modifiers.Multiplier,.5f,"Removing all boosts preserves original slow");modifiers.Clear();Near(modifiers.Multiplier,1,"Clear restores unmodified movement");
        bool rejected=false;try{modifiers.SetSpeedBoost(boost,float.NaN);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected&&modifiers.SourceCount==0,"Invalid boost is rejected without corrupting modifiers");
        var status=new AttackDisableStatus();Check(!status.Apply(null,2)&&!status.Apply(boost,0)&&!status.Apply(boost,float.PositiveInfinity),"Invalid disable tuning cannot create permanent disable");
        status.Apply(boost,2);status.Apply(boost,2);status.Apply(strong,4);Check(status.SourceCount==2,"Disable effects are keyed by source");
        status.Advance(2);Check(status.IsDisabled&&status.SourceCount==1,"One expiry cannot remove another source's disable");status.Advance(0);status.Advance(float.NaN);Check(status.SourceCount==1,"Invalid/zero elapsed time does not alter status");status.Advance(2);Check(!status.IsDisabled,"Final disable expiry restores attacks");
    }
    private static void TestRoster() {
        Find<BaseHealth>().Initialize(10000);
        foreach(string name in EnemyRolesSetup.Names) {
            var data=Data(name);var enemy=Spawner.Spawn(data);var p=enemy.transform.position;enemy.Movement.Advance(.05f);
            Check(enemy is BasicEnemy&&enemy.CurrentHealth==data.Health&&enemy.Movement.BaseMovementSpeed==data.MovementSpeed&&Registry.Enemies.Contains(enemy)&&(data.Sprite==null||enemy.GetComponentInChildren<SpriteRenderer>().sprite==data.Sprite),"Shared actor/path/health pipeline: "+name);
            Near(Vector3.Distance(p,enemy.transform.position),data.MovementSpeed*.05f,"Configured XY movement: "+name);
            int before=Find<BaseHealth>().Current,oldRolls=rolls,oldKills=kills;enemy.Movement.Advance(10000);
            Check(!enemy.IsAlive&&!Registry.Enemies.Contains(enemy)&&Find<BaseHealth>().Current==before-data.BaseDamage&&rolls==oldRolls&&kills==oldKills,"Endpoint retires once without loot; pending Boss damage is safe: "+name);
        }
        ClearWorld();
    }
    private static void TestSpeed() {
        var normal=Spawn(Data("Carrot"),Vector3.right);var garlic=Spawn(Tuned("Garlic"),Vector3.zero);var aura=garlic.GetComponent<SpeedSupportAbility>();
        Near(normal.Movement.EffectiveMovementSpeed,2.4f,"Garlic's configured 1.2x affects an existing nearby ally immediately");Near(garlic.Movement.EffectiveMovementSpeed,2,"Garlic does not boost itself");
        for(int i=0;i<100;i++)Registry.NotifyMoved(normal);Check(normal.Movement.Modifiers.SourceCount==1,"Movement notifications never repeatedly stack a Garlic");
        Move(normal,new Vector3(3,0,20));Near(normal.Movement.EffectiveMovementSpeed,2.4f,"Inclusive aura boundary uses XY and ignores render depth");Move(normal,Vector3.right*3.01f);Near(normal.Movement.EffectiveMovementSpeed,2,"Exiting radius removes boost immediately");
        Move(garlic,Vector3.right);Near(normal.Movement.EffectiveMovementSpeed,2.4f,"Moving Garlic moves its aura and refreshes allies");aura.enabled=false;Near(normal.Movement.EffectiveMovementSpeed,2,"Disabling aura removes only its own boost");aura.enabled=true;Near(normal.Movement.EffectiveMovementSpeed,2.4f,"Reenabled living aura restores one source");
        var strong=Tuned("Garlic");Set(strong,"speedIncrease",2f);var other=Spawn(strong,Vector3.right*2);Near(normal.Movement.EffectiveMovementSpeed,4,"Second Garlic uses strongest multiplier only");
        var slow=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Defense/Data/SlowAreaSkill.asset");slow.Effect.Apply(slow,new SkillCastContext(normal.transform.position,Registry,Flow));Near(normal.Movement.EffectiveMovementSpeed,2,"Real Slow Area composes with the strongest Garlic");
        other.ReceiveDamage(10000);Near(normal.Movement.EffectiveMovementSpeed,1.2f,"Source combat death removes its boost while weaker boost and Slow remain");UnityEngine.Object.DestroyImmediate(garlic.gameObject);Near(normal.Movement.EffectiveMovementSpeed,1,"Source despawn removes final boost without removing Slow Area");
        var late=Spawn(Data("Carrot"),Vector3.zero);var moving=Spawn(Tuned("Garlic"),Vector3.zero);var newcomer=Spawner.Spawn(Data("Tomato"));Move(newcomer,Vector3.zero);Near(newcomer.Movement.EffectiveMovementSpeed,3.6f,"Newly spawned/moved allies enter an existing aura");moving.gameObject.SetActive(false);Check(late.Movement.Modifiers.Multiplier<=1&&newcomer.Movement.Modifiers.Multiplier<=1,"Disabling source GameObject cleans up all aura memberships");ClearWorld();
    }
    private static void TestShield() {
        var durable=Copy(Data("Carrot"));Set(durable,"health",2000f);var target=Spawn(durable,Vector3.right);var other=Spawn(durable,Vector3.right*2);var broccoli=Spawn(Tuned("Broccoli"),Vector3.zero);var shield=broccoli.GetComponent<ShieldSupportAbility>();
        int changed=0;target.GetComponent<EnemyHealth>().Changed+=(current,max)=>changed++;int oldRolls=rolls,oldKills=kills;
        target.ReceiveDamage(60);Check(shield.RemainingShield==40&&target.CurrentHealth==2000&&changed==0,"60 damage consumes shared pool, not ally health or HP change events");
        other.ReceiveDamage(50);Check(shield.RemainingShield==0&&other.CurrentHealth==1990&&target.CurrentHealth==2000,"Next ally's 50 absorbs remaining 40 and spills exactly 10");
        Check(rolls==oldRolls&&kills==oldKills,"Shield absorption alone emits no death, resource or Candy event");shield.enabled=false;shield.enabled=true;Check(shield.RemainingShield==0,"Disabling/reenabling exhausted shield never refills it");target.ReceiveDamage(10);Check(target.CurrentHealth==1990,"Exhausted shield is not permanent invulnerability");
        broccoli.ReceiveDamage(10);Check(broccoli.CurrentHealth==190,"Broccoli does not intercept its own health damage");
        broccoli.Initialize(broccoli.Definition,Find<BattlefieldMap2D>().Path,Find<BaseHealth>(),Registry);broccoli.Movement.Stop();Move(broccoli,Vector3.zero);Check(shield.RemainingShield==100,"Explicit spawn/reset initializes one fresh pool");
        target.ReceiveDamage(10);Check(shield.RemainingShield==90,"Reset does not duplicate shield registration");broccoli.ReceiveDamage(100000);float before=target.CurrentHealth;target.ReceiveDamage(10);Check(target.CurrentHealth==before-10,"Source death stops protection immediately");ClearWorld();
        target=Spawn(durable,Vector3.right);var first=Spawn(Tuned("Broccoli"),Vector3.zero);var second=Spawn(Tuned("Broccoli"),Vector3.zero);var x=first.GetComponent<ShieldSupportAbility>();var y=second.GetComponent<ShieldSupportAbility>();
        target.ReceiveDamage(150);Check(x.RemainingShield==0&&y.RemainingShield==50&&target.CurrentHealth==2000,"Overlapping shields route remainder once in stable activation order");target.ReceiveDamage(60);Check(y.RemainingShield==0&&target.CurrentHealth==1990,"Overlapping pool exhaustion conserves all damage without recursion");ClearWorld();
        target=Spawn(durable,Vector3.right*2);broccoli=Spawn(Tuned("Broccoli"),Vector3.zero);shield=broccoli.GetComponent<ShieldSupportAbility>();var turret=Tower(Vector3.right*3);turret.GetComponent<DirectDamageAttack>().Attack(target,10);Check(shield.RemainingShield==90&&target.CurrentHealth==2000,"Existing direct Tower attack passes through central shield interception");
        var toy=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Defense/Data/ToyThrowSkill.asset");toy.Effect.Apply(toy,new SkillCastContext(target.transform.position,Registry,Flow));Check(shield.RemainingShield==90-toy.Damage&&target.CurrentHealth==2000,"Existing damaging Skill uses the same shield path");
        Move(target,Vector3.right*3.01f);target.ReceiveDamage(1);Check(target.CurrentHealth==1999,"Leaving shield radius stops interception immediately");shield.enabled=false;Move(target,Vector3.right);target.ReceiveDamage(1);Check(target.CurrentHealth==1998,"Disabled shield unregisters without dangling interception");shield.enabled=true;
        var slow=AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Defense/Data/SlowAreaSkill.asset");slow.Effect.Apply(slow,new SkillCastContext(target.transform.position,Registry,Flow));Near(target.Movement.EffectiveMovementSpeed,durable.MovementSpeed*slow.SlowMultiplier,"Shields block damage, never Slow Area modifiers");ClearWorld();
        var rewarded=Copy(Data("Tomato"));Set(rewarded,"candyDropChance",1f);Set(rewarded,"candyDropAmount",2);target=Spawn(rewarded,Vector3.right);broccoli=Spawn(Tuned("Broccoli"),Vector3.zero);oldRolls=rolls;oldKills=kills;int candy=currency.Candy,resources=Find<ResourceWallet>().Balance;
        target.ReceiveDamage(60);Check(rolls==oldRolls&&currency.Candy==candy,"Absorption cannot trigger persistent Candy");target.ReceiveDamage(1000);target.ReceiveDamage(1000);
        Check(!target.IsAlive&&rolls==oldRolls+1&&kills==oldKills+1&&currency.Candy==candy+2&&Find<ResourceWallet>().Balance==resources+rewarded.ResourceReward,"Spillover death pays normal Candy/resources exactly once");ClearWorld();
    }
    private static void TestDisruption() {
        var near=Tower(Vector3.right);var far=Tower(Vector3.right*4);var loose=UnityEngine.Object.Instantiate(near,Vector3.zero,Quaternion.identity);owned.Add(loose.gameObject);
        var potato=Spawn(Tuned("Potato"),Vector3.zero);var ability=potato.GetComponent<TowerDisruptionAbility>();Check(ability.TryDisruptNearest()&&ability.LastTarget==near&&near.AttackDisabled&&!far.AttackDisabled&&!loose.AttackDisabled,"Potato picks nearest valid placed Tower, not a closer unplaced actor or empty slot");
        near.AttackStatus.Clear();near.enabled=false;Check(ability.TryDisruptNearest()&&ability.LastTarget==far,"Disabled Tower is not a valid disruption target");near.enabled=true;far.AttackStatus.Clear();Move(potato,Vector3.left*20);Check(!ability.TryDisruptNearest()&&ability.LastTarget==null,"Out-of-range/no-Tower attempt is safe");ability.Advance(.01f);Near(ability.CooldownRemaining,2,"Missed attempt still starts configured cooldown");Move(potato,Vector3.zero);ability.Advance(1);Check(!near.AttackDisabled,"No premature retry before configured cooldown");ability.Advance(1);Check(near.AttackDisabled,"Next configured attempt selects nearest Tower");
        var other=Spawn(Tuned("Potato"),Vector3.zero);other.GetComponent<TowerDisruptionAbility>().TryDisruptNearest();Check(near.AttackStatus.SourceCount==2,"Two Potatoes maintain independent target-owned status sources");
        var inspection=Find<PlacedTowerInspectionController>();Find<GameplayHand>().CancelSelection();Check(inspection.InspectTower(near)&&inspection.InspectedTower==near,"Disruption leaves range inspection and Tower identity intact");inspection.ClearInspection();
        potato.ReceiveDamage(10000);UnityEngine.Object.DestroyImmediate(other.gameObject);Check(near.AttackDisabled&&near.AttackStatus.SourceCount==2,"Applied disruptions survive source death/despawn until their own expiry");near.AttackStatus.Advance(1.4f);Check(near.AttackDisabled,"Disruption persists for configured duration");near.AttackStatus.Advance(.11f);Check(!near.AttackDisabled&&near.EffectiveAttackRange==near.Definition.AttackRange,"Expiry restores attacks without changing range or data");
        var pending=Spawn(Tuned("Potato"),Vector3.zero);UnityEngine.Object.DestroyImmediate(near.gameObject);pending.GetComponent<TowerDisruptionAbility>().TryDisruptNearest();Check(pending.GetComponent<TowerDisruptionAbility>().LastTarget==far,"Destroyed Tower reference is safely skipped");UnityEngine.Object.DestroyImmediate(loose.gameObject);ClearWorld();
    }
    private static void StartLive() {
        var durable=Copy(Data("Carrot"));Set(durable,"health",5000f);liveTarget=Spawn(durable,Vector3.right);liveTower=Tower(Vector3.zero);
        var configured=Tuned("Potato");Set(configured,"disruptionDuration",.25f);Set(configured,"disruptionCooldown",100f);livePotato=Spawn(configured,Vector3.left).GetComponent<TowerDisruptionAbility>();
        Check(livePotato.TryDisruptNearest(),"Live combat fixture receives real Potato disable");otherDisable=new object();liveTower.AttackStatus.Apply(otherDisable,1);hp=liveTarget.CurrentHealth;
        Flow.TogglePause();stageTime=Time.time;deadline=EditorApplication.timeSinceStartup+.15;
    }
    private static void Next(int value) { phase=value;frame=Time.frameCount; }
    private static void Tick() {
        if(!SessionState.GetBool(Key,false))return;
        try {
            if(EditorApplication.timeSinceStartup-started>120)throw new Exception("Role validation timed out at "+phase);
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||frame==Time.frameCount||Find<TopDownInteractionSandbox>()==null||GameObject.Find("Hand Card 1")==null)return;
            if(phase==0) {
                Find<FixedIntervalSpawner>().enabled=false;ClearWorld();currencyPath=Path.Combine(Path.GetTempPath(),"role-currency-"+Guid.NewGuid().ToString("N")+".json");currency=new PlayerCurrency(new JsonFileStore<PlayerCurrencySaveData>(currencyPath,"Role validation"));
                var candy=Registry.gameObject.AddComponent<CandyDropSystem>();candy.Initialize(Spawner,currency,()=>{rolls++;return 0;});Spawner.EnemyDefeated+=e=>kills++;
                TestRoster();TestSpeed();TestShield();TestDisruption();StartLive();Next(1);
            } else if(phase==1) {
                if(EditorApplication.timeSinceStartup<deadline)return;
                Check(Time.time==stageTime&&liveTower.AttackStatus.SourceCount==2&&liveTarget.CurrentHealth==hp,"Actual paused frames freeze attacks and both disruption timers");Flow.TogglePause();stageTime=Time.time;Next(2);
            } else if(phase==2) {
                if(Time.time-stageTime<.1f)return;
                Check(liveTarget.CurrentHealth==hp&&liveTower.AttackDisabled,"Actual Tower Update cannot attack while disrupted");liveTower.AttackStatus.Remove(otherDisable);Check(liveTower.AttackDisabled,"Removing one source does not restore attacks while Potato source remains");livePotato.GetComponent<Enemy>().ReceiveDamage(100000);stageTime=Time.time;Next(3);
            } else if(phase==3) {
                if(Time.time-stageTime<.3f)return;
                Check(!liveTower.AttackDisabled&&liveTarget.CurrentHealth<hp,"Real advancing frames expire source-death disruption and resume normal attacks");
                var aura=Spawn(Tuned("Garlic"),liveTarget.transform.position);var shield=Spawn(Tuned("Broccoli"),liveTarget.transform.position);retired=new UnityEngine.Object[]{liveTower,liveTarget,aura,shield,aura.GetComponent<SpeedSupportAbility>(),shield.GetComponent<ShieldSupportAbility>()};
                Find<BaseHealth>().Initialize(100);var endpoint=Spawner.Spawn(Data("Tomato"));int beforeRolls=rolls;endpoint.Movement.Advance(10000);
                Check(Flow.State==GameplayState.GameOver&&Find<BaseHealth>().Current==0&&rolls==beforeRolls,"Specified Tomato endpoint damage triggers actual Game Over without Candy");Flow.Retry();Next(4);
            } else if(phase==4) {
                Find<FixedIntervalSpawner>().enabled=false;Check(retired.All(o=>o==null)&&Registry.AliveEnemyCount==0&&UnityEngine.Object.FindObjectsByType<EnemyRoleAbility>().Length==0,"Actual Retry destroys aura/shield/disruption targets and starts with no orphaned ability state");
                var fresh=Spawn(Tuned("Broccoli"),Vector3.zero);Check(fresh.GetComponent<ShieldSupportAbility>().RemainingShield==100,"Fresh run starts with a fresh finite shield pool");
                Check(PlayerSession.Presets==null&&PlayerSession.Currency==null&&File.ReadAllText(DeckPersistence.DefaultPath)==SessionState.GetString(Key+".Deck","")&&File.ReadAllText(PlayerCurrency.DefaultPath)==SessionState.GetString(Key+".Currency",""),"Local role harness never initializes persistent player services or changes copied save bytes");ClearWorld();
                var wave=Copy(AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage.asset"));var serial=new SerializedObject(wave);serial.FindProperty("spawnInterval").floatValue=.1f;var sequence=serial.FindProperty("spawnSequence");sequence.arraySize=5;
                string[] names={"Garlic","Broccoli","Carrot","Potato","Tomato"};for(int i=0;i<names.Length;i++)sequence.GetArrayElementAtIndex(i).objectReferenceValue=Tuned(names[i]);serial.ApplyModifiedPropertiesWithoutUndo();
                var schedule=Find<FixedIntervalSpawner>();schedule.Configure(wave);var victory=Registry.gameObject.AddComponent<StageVictoryController>();victory.Initialize(Flow,schedule,Registry,wave,currency,()=>{},true);
                for(int i=0;i<5;i++)schedule.Advance(.1f);Check(schedule.IsFinishedSpawning&&Registry.AliveEnemyCount==5,"Existing finite schedule spawns a mixed configured role wave");
                retired=Registry.Enemies.Cast<UnityEngine.Object>().Concat(UnityEngine.Object.FindObjectsByType<EnemyRoleAbility>()).ToArray();int gold=currency.Gold;
                var toy=Copy(AssetDatabase.LoadAssetAtPath<SkillDefinition>("Assets/Defense/Data/ToyThrowSkill.asset"));Set(toy,"damage",100000f);toy.Effect.Apply(toy,new SkillCastContext(Registry.Enemies[0].transform.position,Registry,Flow));
                Check(Registry.AliveEnemyCount==0&&victory.EvaluateClear()&&Flow.State==GameplayState.Victory&&victory.DefeatedCount==5&&currency.Gold==gold+wave.ClearGoldReward,"Real AoE role deaths clean up all sources and trigger normal Victory/Gold once");Check(!victory.EvaluateClear()&&currency.Gold==gold+wave.ClearGoldReward,"Repeated mixed-role clear cannot duplicate Gold");Flow.Retry();Next(5);
            } else if(phase==5) {
                Find<FixedIntervalSpawner>().enabled=false;Check(retired.All(o=>o==null)&&Registry.AliveEnemyCount==0&&UnityEngine.Object.FindObjectsByType<EnemyRoleAbility>().Length==0,"Actual Victory Replay destroys every prior role/ability instance");var normal=Spawn(Data("Carrot"),Vector3.zero);normal.ReceiveDamage(1);Check(normal.CurrentHealth==99&&normal.Movement.Modifiers.SourceCount==0,"Fresh run has no orphaned shield or speed source");Finish(true,checks+" EditMode/PlayMode assertions");
            }
        } catch(Exception error) { Finish(false,error.ToString()); }
    }
    private static void Finish(bool success,string message) {
        if(!SessionState.GetBool(Key,false))return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;
        if(Registry!=null)ClearWorld();foreach(var value in owned)if(value!=null)UnityEngine.Object.DestroyImmediate(value);owned.Clear();if(!string.IsNullOrEmpty(currencyPath))File.Delete(currencyPath);
        Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/Enemy_Roles_Result.txt",(success?"PASS: ":"FAIL: ")+message);
        if(success)Debug.Log("ENEMY_ROLES_PASSED "+message);else Debug.LogError("ENEMY_ROLES_FAILED "+message);
        EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(Application.isBatchMode)EditorApplication.Exit(success?0:1);
    }
}
}
