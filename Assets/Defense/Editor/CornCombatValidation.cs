using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
public static class CornCombatValidation {
    private static void Check(bool ok, string message) { if(!ok) throw new Exception(message); }
    private static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual-expected)<.001f,message);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    [MenuItem("Defense/Validate Corn Combat Targets (Play Mode)")]
    public static void Run() {
        if(!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode in an empty scene first.");
        var scene = SceneManager.CreateScene("Corn validation " + Guid.NewGuid().ToString("N"));
        var root = new GameObject("Corn validation");
        SceneManager.MoveGameObjectToScene(root,scene);
        var temporary = new List<UnityEngine.Object>();
        try {
            var registry = root.AddComponent<EnemyRegistry>();
            var path = root.AddComponent<WaypointPath>();
            var a = new GameObject("Start"); a.transform.SetParent(root.transform,false);
            var b = new GameObject("End"); b.transform.SetParent(root.transform,false); b.transform.position = Vector3.right*1000;
            path.Configure(WaypointPlane.XZ,new[]{a.transform,b.transform});
            var data = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/Defense/Data/Enemies/Veggies/CornEnemy.asset");
            Check(data != null && data.Health == 40 && data.SplitCount == 5,"Corn balance must remain 5 x 40 HP.");
            float targetHp = data.Health;
            var bodyObject = new GameObject("Corn"); bodyObject.transform.SetParent(root.transform,false);
            var body = bodyObject.AddComponent<ClusterEnemy>();
            body.Initialize(data,path,null,registry);
            // Public damage entry points must dispatch to ClusterEnemy through base/interface references.
            IDamageable damageable = body;
            Enemy actor = body;
            damageable.ReceiveDamage(7);
            Near(body.Targets[0].CurrentHealth,targetHp-7,"Interface damage reaches the first target.");
            Near(body.Targets[1].CurrentHealth,targetHp,"Direct body damage leaves siblings untouched.");
            actor.ReceiveSplashDamage(3);
            Near(body.Targets[0].CurrentHealth,targetHp-10,"Base-reference splash hits the first target once.");
            Near(body.Targets[1].CurrentHealth,targetHp-3,"Base-reference splash reaches siblings.");
            body.Initialize(data,path,null,registry);
            Check(registry.CombatTargets.Count == 5 && registry.AliveEnemyCount == 1,
                "Reinitializing a living body must replace targets without duplicate registry entries.");
            var parts = body.Targets;
            Check(parts.Count == 5 && registry.CombatTargets.Count == 5 && registry.AliveEnemyCount == 1,
                "Five combat targets must count as one spawned body.");
            Check(!System.Linq.Enumerable.Contains(registry.CombatTargets,body),"The body must not be a sixth target.");
            body.Movement.Advance(1);
            foreach(var part in parts) {
                Check(part.transform.position == body.transform.position && part.Movement == body.Movement,"All targets must share body movement.");
                Check(!part.GetComponent<EnemyPathFollower>().enabled,"Part movement must be disabled.");
            }
            parts[1].ReceiveDamage(7);
            Near(parts[1].CurrentHealth,targetHp-7,"Chosen target must receive direct damage.");
            Near(parts[0].CurrentHealth,targetHp,"Other targets must keep their HP.");
            Near(body.GetComponent<EnemyHealth>().GetLayerHealth(1),targetHp-7,"Body UI must mirror the actual target.");
            parts[1].StatusEffects.ApplyBurn(new object(),3,10);
            parts[1].StatusEffects.Advance(1);
            Near(parts[1].CurrentHealth,targetHp-10,"Burn must stay on the chosen target.");
            Near(parts[0].CurrentHealth,targetHp,"Burn must not affect siblings.");
            var sameSource = new object();
            parts[0].StatusEffects.ApplySlow(sameSource,.5f,1);
            parts[2].StatusEffects.ApplySlow(sameSource,.7f,4);
            Near(body.Movement.EffectiveMovementSpeed,data.MovementSpeed*.5f,"Strongest part slow must affect shared movement.");
            parts[0].StatusEffects.Advance(1);
            Near(body.Movement.EffectiveMovementSpeed,data.MovementSpeed*.7f,"One part's slow expiry must not erase another part's slow.");
            parts[2].ReceiveDamage(1000);
            Near(body.Movement.EffectiveMovementSpeed,data.MovementSpeed,"Dead target must clear its slow.");
            Check(registry.CombatTargets.Count == 4 && body.IsAlive,"Dead target must leave combat while body survives.");
            parts[1].ReceiveDamage(1000);
            parts[1].StatusEffects.Advance(10);
            Near(parts[0].CurrentHealth,targetHp,"Burn on a dead target must never jump to a sibling.");
            var targeting = ScriptableObject.CreateInstance<ClosestTargeting>(); temporary.Add(targeting);
            var towerData = ScriptableObject.CreateInstance<TurretDefinition>(); temporary.Add(towerData);
            Set(towerData,"targeting",targeting); Set(towerData,"attackRange",100f);
            Set(towerData,"towerType",TowerType.Ketchup); Set(towerData,"damage",6f);
            var towerObject = new GameObject("Ketchup"); towerObject.transform.SetParent(root.transform,false);
            var tower = towerObject.AddComponent<BasicTower>(); tower.Initialize(towerData,registry);
            Check(tower.FindTarget() is CornTarget && tower.FindTarget().IsAlive,"Tower targeting must select a living part.");
            tower.Attack(body);
            Near(parts[0].CurrentHealth,targetHp-6,"Ketchup must damage the first survivor once.");
            Near(parts[3].CurrentHealth,targetHp-6,"Ketchup must damage every survivor once.");
            Near(parts[4].CurrentHealth,targetHp-6,"Ketchup must damage the last survivor once.");
            Near(body.CurrentHealth,3*(targetHp-6),"Aggregate UI must match independent target health.");
            int deaths = 0; body.Died += enemy => deaths++;
            Set(towerData,"damage",100f); tower.Attack(parts[3]);
            Check(deaths == 1 && !body.IsAlive && registry.CombatTargets.Count == 0 && registry.AliveEnemyCount == 0,
                "Last targets dying together must produce one body death and clear both registries.");
            body.ReceiveSplashDamage(1000); Check(deaths == 1,"Repeated damage must not repeat rewards/death.");
            body.Initialize(data,path,null,registry);
            Check(registry.CombatTargets.Count == 5 && registry.AliveEnemyCount == 1,"Reinitialization must replace old combat targets.");
            parts = body.Targets;
            Set(towerData,"towerType",TowerType.HotSauce); Set(towerData,"damage",2f);
            Set(towerData,"burnDamagePerSecond",3f); Set(towerData,"burnDuration",4f);
            tower.Attack(body);
            Near(parts[0].CurrentHealth,targetHp-2,"A tower given the body must resolve an individual target.");
            parts[0].StatusEffects.Advance(1);
            Near(parts[0].CurrentHealth,targetHp-5,"Hot Sauce must attach its burn to that same target.");
            Near(parts[1].CurrentHealth,targetHp,"Hot Sauce must leave sibling targets unaffected.");
            parts[0].ReceiveDamage(1000);
            parts[0].StatusEffects.Advance(10);
            Near(parts[1].CurrentHealth,targetHp,"Hot Sauce burn must stop with its target.");
            bodyObject.SetActive(false);
            Check(registry.CombatTargets.Count == 0 && registry.AliveEnemyCount == 0,"Disabling a body must remove every target.");
            // ClusterEnemy owns the behavior regardless of vegetable identity.
            var genericData = UnityEngine.Object.Instantiate(data); temporary.Add(genericData);
            Set(genericData,"vegetableType",VegetableType.Other);
            bodyObject.SetActive(true);
            body.Initialize(genericData,path,null,registry);
            Check(body.Targets.Count == 5 && registry.CombatTargets.Count == 5,
                "ClusterEnemy must create sections without a Corn vegetable flag.");
            UnityEngine.Object.DestroyImmediate(bodyObject);
            Check(registry.CombatTargets.Count == 0 && registry.AliveEnemyCount == 0,
                "Destroying a living ClusterEnemy must retire all sections.");
            Debug.Log("CORN COMBAT VALIDATION PASSED");
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
            foreach(var item in temporary) UnityEngine.Object.DestroyImmediate(item);
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
}
