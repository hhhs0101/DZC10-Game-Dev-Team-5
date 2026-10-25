using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
// Synchronous, isolated Play Mode integration checks. No PlayerSession or disk saves.
public static class EnemyRolesValidation {
    private static GameObject root;
    private static EnemyRegistry registry;
    private static EnemySpawner spawner;
    private static GameFlow flow;
    private static WaypointPath path;
    private static int checks;
    private static void Check(bool ok,string message) { if(!ok) throw new Exception(message); checks++; }
    private static void Near(float actual,float expected,string message) => Check(Mathf.Abs(actual-expected)<.01f,message+": "+actual+" expected "+expected);
    private static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static T Asset<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>("Assets/Defense/"+path);
    private static GameObject Child(string name) { var go=new GameObject(name);go.transform.SetParent(root.transform);return go; }
    private static Enemy Spawn(string name,Vector3 position) {
        var e=spawner.Spawn(Asset<EnemyDefinition>("Data/Enemies/Veggies/"+name+"Enemy.asset"));
        Check(e!=null,"Spawn "+name);e.Movement.Stop();e.transform.position=position;registry.NotifyMoved(e);return e;
    }
    private static Tower Tower(int index,Vector3 position) {
        var slot=Child("Test slot").AddComponent<TowerPlacementSlot>();slot.transform.position=position;
        var prefab=Asset<GameObject>("Prefabs/TopDown2D/BasicTower2D.prefab").GetComponent<Tower>();
        var tower=UnityEngine.Object.Instantiate(prefab,position,Quaternion.identity,slot.transform);
        tower.Initialize(Asset<TurretDefinition>("Data/TestTurret"+index+".asset"),registry);
        Check(slot.TryOccupy(tower),"Occupy test slot");tower.StartLifetime(slot,flow,3);return tower;
    }
    private static void ClearActors() {
        foreach(var e in registry.Enemies.ToArray())UnityEngine.Object.DestroyImmediate(e.gameObject);
        foreach(var slot in root.GetComponentsInChildren<TowerPlacementSlot>())UnityEngine.Object.DestroyImmediate(slot.gameObject);
        foreach(var shot in UnityEngine.Object.FindObjectsByType<DirtProjectile>())if(shot.gameObject.scene==root.scene)UnityEngine.Object.DestroyImmediate(shot.gameObject);
    }
    [MenuItem("Defense/Enemies/Validate New Combat Rules (Play Mode)")]
    public static void Run() {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode in an empty scene, then run this validation.");
        float scale=Time.timeScale;var random=UnityEngine.Random.state;
        var scene=SceneManager.CreateScene("Combat validation "+Guid.NewGuid().ToString("N"));
        root=new GameObject("Combat validation");SceneManager.MoveGameObjectToScene(root,scene);checks=0;
        try {
            flow=root.AddComponent<GameFlow>();registry=root.AddComponent<EnemyRegistry>();Set(registry,"plane",WaypointPlane.XY);
            var health=root.AddComponent<BaseHealth>();health.Initialize(10000);
            path=Child("Path").AddComponent<WaypointPath>();Set(path,"plane",WaypointPlane.XY);
            var a=Child("Start").transform;var b=Child("End").transform;b.position=Vector3.right*1000;
            Set(path,"points",new[]{a,b});spawner=root.AddComponent<EnemySpawner>();
            Set(spawner,"path",path);Set(spawner,"registry",registry);Set(spawner,"flow",flow);Set(spawner,"baseHealth",health);Set(spawner,"enemyRoot",root.transform);
            Check(Asset<EnemyDefinitionCatalog>("Resources/EnemyDefinitionCatalog.asset").TryBuildLookup(out var lookup,out var error),"Catalog: "+error);
            Check(lookup.ContainsKey("enemy_bellpepper")&&lookup.ContainsKey("enemy_corn"),"New enemy IDs registered");
            TestAuras();ClearActors();TestTowers();ClearActors();TestSplit();ClearActors();TestBoss();ClearActors();TestDisruption();ClearActors();TestCosts();ClearActors();TestVictory();
            Debug.Log("COMBAT VALIDATION PASSED: "+checks+" checks. No persistent player state used.");
        } finally {
            ClearActors();UnityEngine.Object.DestroyImmediate(root);SceneManager.UnloadSceneAsync(scene);
            Time.timeScale=scale;UnityEngine.Random.state=random;
        }
    }
    private static void TestAuras() {
        var carrot=Spawn("Carrot",Vector3.right);var garlic=Spawn("Garlic",Vector3.zero);
        Check(garlic is SpeedSupportEnemy,"Actual speed support subclass");
        Near(carrot.Movement.EffectiveMovementSpeed,3,"1.5x speed aura");Near(garlic.Movement.EffectiveMovementSpeed,2,"Garlic excludes itself");
        garlic.transform.position=Vector3.left*10;registry.NotifyMoved(garlic);Near(carrot.Movement.EffectiveMovementSpeed,2,"Aura exit removes boost");
        var pepper=Spawn("BellPepper",Vector3.zero);Check(pepper is ShieldSupportEnemy,"Actual shield subclass");
        carrot.ReceiveDamage(50);Near(carrot.CurrentHealth,80,"60% absorption");pepper.ReceiveDamage(50);Near(pepper.CurrentHealth,180,"Shield includes itself");
        Spawn("BellPepper",Vector3.zero);carrot.ReceiveDamage(50);Near(carrot.CurrentHealth,60,"Overlapping shields do not compound");
        carrot.ReceiveDamage(10,true);Near(carrot.CurrentHealth,50,"Piercing bypasses shield");
        carrot.transform.position=Vector3.right*10;carrot.ReceiveDamage(10);Near(carrot.CurrentHealth,40,"Outside aura receives full damage");
    }
    private static void TestTowers() {
        var a=Spawn("Carrot",Vector3.zero);var b=Spawn("Carrot",Vector3.right);var c=Spawn("Eggplant",Vector3.right*8);
        var ketchup=Tower(11,Vector3.up);float damage=ketchup.EffectiveDamage;
        ketchup.Attack(a);Near(a.CurrentHealth,100-damage,"Ketchup target damage");Near(b.CurrentHealth,100-damage,"Ketchup splash damage");Near(c.CurrentHealth,150,"Splash excludes distant enemy");
        var pepper=Spawn("BellPepper",Vector3.zero);var mustard=Tower(9,Vector3.up);
        mustard.Attack(pepper);Near(pepper.CurrentHealth,pepper.MaximumHealth-mustard.EffectiveDamage,"Mustard bypasses Bell Pepper");
        UnityEngine.Object.DestroyImmediate(pepper.gameObject);
        var hot=Tower(3,Vector3.up);float direct=hot.EffectiveDamage, burn=hot.Definition.BurnDamagePerSecond, duration=hot.Definition.BurnDuration;
        hot.Attack(c);Near(c.CurrentHealth,150-direct,"Hot Sauce direct damage");
        UnityEngine.Object.DestroyImmediate(hot.gameObject);c.transform.position=Vector3.right*30;
        flow.TogglePause();c.StatusEffects.Advance(2);Near(c.CurrentHealth,150-direct,"Burn pauses");flow.TogglePause();
        c.StatusEffects.Advance(duration);Near(c.CurrentHealth,Mathf.Max(0,150-direct-burn*duration),"Burn persists after tower destruction and leaving range");
        float afterBurn=c.CurrentHealth;c.StatusEffects.Advance(duration);Near(c.CurrentHealth,afterBurn,"Burn expires");
        var oil=Tower(2,Vector3.up);oil.Attack(a);Near(a.Movement.EffectiveMovementSpeed,2*oil.Definition.SlowMultiplier,"Oil slow");
        a.StatusEffects.Advance(oil.Definition.SlowDuration);Near(a.Movement.EffectiveMovementSpeed,2,"Oil slow expires");
        var salt=Tower(6,Vector3.zero);var mayo=Tower(10,Vector3.right);
        Near(salt.EffectiveDamage,salt.Definition.Damage*mayo.Definition.DamageBoost,"Mayonnaise damage boost");salt.AttackStatus.Apply(new object(),30);
        mayo.AdvanceSupport(4);Check(salt.AttackDisabled,"No early cleanse");mayo.AdvanceSupport(1);Check(!salt.AttackDisabled,"Five-second cleanse");
        UnityEngine.Object.DestroyImmediate(mayo.gameObject);Near(salt.EffectiveDamage,salt.Definition.Damage,"Removing Mayonnaise removes buff");
    }
    private static void TestSplit() {
        int kills=0;spawner.EnemyDefeated+=OnKill;
        void OnKill(Enemy enemy) { kills++; }
        var corn=Spawn("Corn",Vector3.right*40);Check(corn is SplitEnemy,"Actual split subclass");
        corn.ReceiveDamage(100);Check(registry.AliveEnemyCount==5,"Death spawns exactly five children");
        foreach(var child in registry.Enemies.ToArray()) {
            Near(child.transform.position.x,40,"Split preserves position");Check(child.Movement.NextWaypoint==corn.Movement.NextWaypoint,"Split preserves path progress");
            child.ReceiveDamage(100);
        }
        Check(registry.AliveEnemyCount==0,"Children do not split recursively");Check(kills==1,"Children do not farm kill rewards");spawner.EnemyDefeated-=OnKill;
    }
    private static void TestBoss() {
        var boss=Spawn("Broccoli",Vector3.zero) as BossEnemy;Check(boss!=null,"Actual BossEnemy subclass");
        var tower=Tower(6,Vector3.right);var slot=tower.GetComponentInParent<TowerPlacementSlot>();
        boss.AdvanceAbilities(4);Check(registry.AliveEnemyCount==1,"Summons wait five seconds");
        flow.TogglePause();boss.AdvanceAbilities(10);Check(registry.AliveEnemyCount==1,"Boss pauses");flow.TogglePause();
        boss.AdvanceAbilities(1);Check(registry.AliveEnemyCount==4,"Boss summons three");Near(tower.CurrentHealth,tower.MaximumHealth,"Broccoli does not damage towers");
        var children=registry.Enemies.Where(e=>e!=boss).ToArray();Check(children.Select(e=>e.Definition).Distinct().Count()==3,"Three random distinct types");Check(children.All(e=>e.Definition.Role!=EnemyRole.Boss),"Never summons another boss");
        foreach(var child in children)UnityEngine.Object.DestroyImmediate(child.gameObject);
        boss.AdvanceAbilities(5);Check(slot.IsOccupied&&tower.gameObject.activeSelf,"Broccoli leaves tower and slot intact");Near(tower.CurrentHealth,tower.MaximumHealth,"Repeated boss updates do not damage towers");
        flow.GameOver();int count=registry.AliveEnemyCount;boss.AdvanceAbilities(20);Check(registry.AliveEnemyCount==count,"Terminal state blocks summons");
        // Restore this isolated fixture only; normal gameplay terminal states remain irreversible.
        UnityEngine.Object.DestroyImmediate(flow);flow=root.AddComponent<GameFlow>();Set(spawner,"flow",flow);
    }
    private static void TestDisruption() {
        var potato=Spawn("Potato",Vector3.zero);Check(potato is DisruptorEnemy,"Actual disruptor subclass");
        var one=Tower(6,Vector3.right);var two=Tower(4,Vector3.left);var far=Tower(8,Vector3.right*10);
        var ability=potato.GetComponent<TowerDisruptionAbility>();var picked=new System.Collections.Generic.HashSet<Tower>();UnityEngine.Random.InitState(123);
        for(int i=0;i<40;i++) { Check(ability.TryDisruptRandom(),"Dirt throw succeeds");picked.Add(ability.LastTarget); }
        Check(picked.Contains(one)&&picked.Contains(two)&&!picked.Contains(far),"Random selection covers in-range towers only");
        foreach(var shot in UnityEngine.Object.FindObjectsByType<DirtProjectile>().Where(s=>s.gameObject.scene==root.scene))shot.Advance(.25f);
        Check(one.AttackDisabled&&two.AttackDisabled,"Dirt applies disruption on impact");
        UnityEngine.Object.DestroyImmediate(potato.gameObject);one.AttackStatus.Advance(3);Check(!one.AttackDisabled,"Disruption expires after source death");
    }
    private static void TestVictory() {
        var schedule=root.AddComponent<FixedIntervalSpawner>();Set(schedule,"spawner",spawner);
        schedule.Configure(Asset<EnemyDefinition>("Data/Enemies/Veggies/CornEnemy.asset"),.1f,1);schedule.Advance(.1f);
        var victory=root.AddComponent<StageVictoryController>();bool completed=false;
        victory.Initialize(flow,schedule,registry,Asset<StageDefinition>("Data/Stage1_1.asset"),null,()=>completed=true);
        Check(schedule.IsFinishedSpawning&&!victory.EvaluateClear(),"Living scheduled enemy blocks Victory");
        registry.Enemies.Single().ReceiveDamage(100);
        Check(registry.AliveEnemyCount==5&&!victory.EvaluateClear(),"Split children block premature Victory");
        foreach(var child in registry.Enemies.ToArray())child.ReceiveDamage(100);
        Check(victory.EvaluateClear()&&completed&&flow.State==GameplayState.Victory,"Victory after final child dies");
        Check(!victory.EvaluateClear(),"Victory processes only once");
    }
    private static void TestCosts() {
        var wallet=root.AddComponent<ResourceWallet>();wallet.Initialize(300);
        var elixir=root.AddComponent<ElixirSystem>();elixir.Initialize(flow);elixir.Advance(100);
        var placement=root.AddComponent<TurretPlacementController>();Set(placement,"flow",flow);Set(placement,"registry",registry);Set(placement,"wallet",wallet);
        Set(placement,"spritePrefab",Asset<GameObject>("Prefabs/TopDown2D/BasicTower2D.prefab").GetComponent<Tower>());
        var hand=root.GetComponent<GameplayHand>();var cards=new[]{2,3,4,6,9,11}.Select(i=>(CardDefinition)Asset<TowerCardDefinition>("Data/TowerCard"+i+".asset")).ToArray();
        hand.Initialize(new PlayerDeck(cards),placement,flow);hand.Select(0);var card=hand.SelectedCard;
        var slot=Child("Purchase slot").AddComponent<TowerPlacementSlot>();
        Check(!placement.TryPlace(null)&&wallet.Balance==300&&elixir.Current==10&&hand.SelectedCard==card,"Invalid placement charges neither currency");
        Check(placement.TryPlace(slot),"Successful purchase");Check(wallet.Balance==300,"Placement does not charge resources");Near(elixir.Current,10-card.Definition.ElixirCost,"Energy charged once");
        Check(slot.Occupant.GetComponent<TowerLifetime>()!=null,"Purchased tower keeps countdown");
        wallet.Initialize(0);elixir.Advance(100);hand.Select(0);var next=hand.SelectedCard;float energy=elixir.Current;
        Check(placement.TryPlace(Child("Zero-resource slot").AddComponent<TowerPlacementSlot>()),"Placement succeeds with zero resources");
        Check(wallet.Balance==0,"Resource balance remains unchanged");Near(elixir.Current,energy-next.Definition.ElixirCost,"Only energy is spent");
        elixir.TrySpend(elixir.Current);hand.Select(0);next=hand.SelectedCard;
        Check(!placement.TryPlace(Child("Insufficient energy slot").AddComponent<TowerPlacementSlot>()),"Energy requirement remains enforced");
        Check(hand.SelectedCard==next&&wallet.Balance==0&&elixir.Current==0,"Failed placement preserves hand and currencies");
    }
}
}
