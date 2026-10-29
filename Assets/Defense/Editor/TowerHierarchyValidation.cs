using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
// Explicit Edit Mode regression checks; no user scene or balance asset is modified.
public static class TowerHierarchyValidation {
    private static void Near(float actual, float expected, string label) {
        if(Mathf.Abs(actual-expected) > .01f) throw new Exception(label+": expected "+expected+", got "+actual);
    }
    private static Type Expected(TowerType type) {
        switch(type) {
            case TowerType.Ketchup: return typeof(SplashTower);
            case TowerType.Mustard: return typeof(ShieldPiercingTower);
            case TowerType.Mayonnaise: return typeof(SupportTower);
            case TowerType.HotSauce: return typeof(BurningTower);
            case TowerType.Oil: return typeof(SlowingTower);
            default: return typeof(BasicTower);
        }
    }
    [MenuItem("Defense/Validate Tower Hierarchy")]
    public static void Run() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
        var privateFields=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly;
        if(typeof(Tower).GetField("cooldown",privateFields)==null || typeof(AttackTower).GetField("cooldown",privateFields)!=null)
            throw new Exception("Tower must own the shared cooldown.");
        if(typeof(Tower).GetProperty("range")==null || typeof(Tower).GetProperty("energyCost")==null)
            throw new Exception("Tower must expose range and energyCost.");
        if(typeof(SupportTower).GetMethod("BoostDamageNearbyTower")==null)
            throw new Exception("SupportTower must expose BoostDamageNearbyTower.");
        int count=0;
        foreach(var guid in AssetDatabase.FindAssets("t:TurretDefinition",new[]{"Assets/Defense/Data"})) {
            var data=AssetDatabase.LoadAssetAtPath<TurretDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if(data.Prefab == null || data.Prefab.GetType() != Expected(data.TowerType)) throw new Exception(data.name+": incorrect prefab class");
            CheckCombat(data); count++;
        }
        Debug.Log("TOWER_HIERARCHY_PASS: "+count+" definitions; role mapping, shield/splash damage, burn, slow, support, disruption and events.");
    }
    private static void CheckCombat(TurretDefinition data) {
        var previous=SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject("Tower hierarchy fixture"); SceneManager.MoveGameObjectToScene(root,scene);
        var enemyData=ScriptableObject.CreateInstance<EnemyDefinition>();
        var shieldData=ScriptableObject.CreateInstance<EnemyDefinition>();
        try {
            Func<string,GameObject> child=name=>{var go=new GameObject(name);go.transform.SetParent(root.transform);return go;};
            var registry=child("Registry").AddComponent<EnemyRegistry>();
            var path=child("Path").AddComponent<WaypointPath>();
            var a=child("A").transform; var b=child("B").transform;b.position=Vector3.right*100;
            path.Configure(WaypointPlane.XZ,new[]{a,b});
            var ed=new SerializedObject(enemyData);ed.FindProperty("health").floatValue=10000;ed.ApplyModifiedPropertiesWithoutUndo();
            var sd=new SerializedObject(shieldData);sd.FindProperty("health").floatValue=10000;sd.FindProperty("role").enumValueIndex=(int)EnemyRole.ShieldSupport;sd.FindProperty("supportArea").floatValue=100;sd.FindProperty("shieldValue").floatValue=.6f;sd.ApplyModifiedPropertiesWithoutUndo();
            var victim=child("Target").AddComponent<BasicEnemy>(); victim.Initialize(enemyData,path,null,registry);
            var neighbour=child("Neighbour").AddComponent<BasicEnemy>();neighbour.Initialize(enemyData,path,null,registry);
            var shield=child("Shield").AddComponent<BasicEnemy>();shield.Initialize(shieldData,path,null,registry);
            var tower=UnityEngine.Object.Instantiate(data.Prefab,root.transform);tower.Initialize(data,registry);
            int events=0;tower.Attacked+=e=>events++;
            var damageTower=tower as AttackTower;
            var supportTower=tower as SupportTower;
            tower.AttackStatus.Apply(new object(),10);
            if(damageTower!=null)damageTower.Attack(victim);else supportTower.AdvanceSupport(data.CleanseInterval);
            Near(victim.CurrentHealth,10000,"Disrupted damage");Near(events,0,"Disrupted event");tower.AttackStatus.Clear();
            victim.StatusEffects.SendMessage("Awake");
            if(damageTower!=null)damageTower.Attack(victim);else supportTower.AdvanceSupport(data.CleanseInterval);
            float expected=data.TowerType==TowerType.Mayonnaise?0:data.Damage*(data.TowerType==TowerType.Mustard?1:.4f);
            Near(victim.CurrentHealth,10000-expected,"Initial damage "+data.name);
            Near(neighbour.CurrentHealth,10000-(data.TowerType==TowerType.Ketchup?data.Damage*.4f:0),"Splash membership");
            Near(events,1,"Attack event");
            // Invoke Awake explicitly: these fixtures are constructed in Edit Mode.
            if(data.TowerType==TowerType.HotSauce || data.TowerType==TowerType.Oil) {
                victim.StatusEffects.SendMessage("Awake");victim.StatusEffects.Advance(1);
                if(data.TowerType==TowerType.HotSauce) Near(victim.CurrentHealth,10000-expected-data.BurnDamagePerSecond*Mathf.Min(1,data.BurnDuration)*.4f,"Burn tick");
                else {
                    Near(victim.Movement.Modifiers.Multiplier,data.SlowDuration>1?data.SlowMultiplier:1,"Slow tick");
                    victim.StatusEffects.Advance(data.SlowDuration);Near(victim.Movement.Modifiers.Multiplier,1,"Slow expiry");
                }
            }
            if(damageTower != null) {
                // Verify the shared damage-tower cadence after extracting it from Tower.
                damageTower.Initialize(data,registry);
                var tick=typeof(AttackTower).GetMethod("AdvanceBehavior",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                int before=events;
                damageTower.AttackStatus.Apply(new object(),100);
                tick.Invoke(damageTower,new object[]{data.AttackCooldown+1f});
                Near(events,before,"Disruption blocks automatic attacks");
                damageTower.AttackStatus.Clear();
                tick.Invoke(damageTower,new object[]{.001f});
                Near(events,before+1,"Ready damage tower attacks once");
                tick.Invoke(damageTower,new object[]{data.AttackCooldown*.5f});
                Near(events,before+1,"Cooldown prevents early automatic attacks");
                tick.Invoke(damageTower,new object[]{data.AttackCooldown*.5f+.001f});
                Near(events,before+2,"Cooldown expiry permits the next attack");
                var flow=registry.gameObject.AddComponent<GameFlow>();
                flow.TogglePause();damageTower.Attack(victim);
                Near(events,before+2,"Pause blocks explicit damage attacks");
                flow.TogglePause();
            }
            if(data.TowerType==TowerType.Mayonnaise) {
                var otherData=AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/TestTurret6.asset");
                var other=(AttackTower)UnityEngine.Object.Instantiate(otherData.Prefab,root.transform);other.Initialize(otherData,registry);
                child("Support slot").AddComponent<TowerPlacementSlot>().TryOccupy(tower);
                child("Target slot").AddComponent<TowerPlacementSlot>().TryOccupy(other);
                Near(other.FinalDamage,otherData.Damage*data.DamageBoost,"Support multiplier");
                supportTower.AttackStatus.Apply(new object(),10);
                Near(other.FinalDamage,otherData.Damage,"Disrupted support contributes no boost");
                supportTower.AttackStatus.Clear();
                var supportPosition=supportTower.transform.position;
                supportTower.transform.position+=Vector3.right*(data.AttackRange+100);
                Near(other.FinalDamage,otherData.Damage,"Out-of-range support contributes no boost");
                supportTower.transform.position=supportPosition;
                Near(other.FinalDamage,otherData.Damage*data.DamageBoost,"Returning support restores its boost");
                other.AttackStatus.Apply(new object(),100);
                supportTower.AdvanceSupport(data.CleanseInterval-.01f);
                if(!other.IsDisrupted) throw new Exception("Early cleanse");
                supportTower.AdvanceSupport(.02f);
                if(other.IsDisrupted) throw new Exception("Missed cleanse");
            }
            if(data.TowerType==TowerType.Salt) {
                // Concrete base classes must work without a specialized subclass.
                var passive=child("Passive Tower").AddComponent<Tower>(); passive.Initialize(data,registry);
                int passiveEvents=0;passive.Attacked+=e=>passiveEvents++;
                var passiveTick=typeof(Tower).GetMethod("AdvanceBehavior",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                float beforePassive=victim.CurrentHealth;
                passiveTick.Invoke(passive,new object[]{10f});
                Near(victim.CurrentHealth,beforePassive,"Concrete Tower is passive");
                Near(passiveEvents,0,"Concrete Tower emits no attack pulse");
                var plain=child("Plain Attacker").AddComponent<AttackTower>();plain.Initialize(data,registry);
                int plainEvents=0;plain.Attacked+=e=>plainEvents++;
                float beforePlain=victim.CurrentHealth;
                plain.Attack(victim);
                Near(victim.CurrentHealth,beforePlain-data.Damage*.4f,"Concrete AttackTower defaults to direct damage");
                plain.Initialize(data,registry);
                plain.Attack(victim);
                Near(victim.CurrentHealth,beforePlain-2*data.Damage*.4f,"Reinitialization replaces rather than stacks callbacks");
                Near(plainEvents,2,"One event per concrete attacker hit");
            }
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(enemyData);UnityEngine.Object.DestroyImmediate(shieldData);
            EditorSceneManager.CloseScene(scene,true);
            SceneManager.SetActiveScene(previous);
        }
    }
}
}
