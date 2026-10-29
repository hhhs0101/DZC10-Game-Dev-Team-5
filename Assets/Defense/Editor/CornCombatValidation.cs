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
            var flow = root.AddComponent<GameFlow>();
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
            var healthView = bodyObject.AddComponent<EnemyHealthBar>();
            void CheckIndependentBars() {
                var fills=(RectTransform[])typeof(EnemyHealthBar).GetField("fills",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(healthView);
                Check(fills.Length==5,"Corn must display five distinct HP bars, not one aggregate bar.");
                for(int i=0;i<5;i++)
                    Near(fills[i].anchorMax.x,body.Targets[i].CurrentHealth/body.Targets[i].MaximumHealth,
                        "Each visible bar must show only its own section health.");
            }
            CheckIndependentBars();
            // Public damage entry points must dispatch to ClusterEnemy through base/interface references.
            IDamageable damageable = body;
            Enemy actor = body;
            damageable.ReceiveDamage(7);
            Near(body.Targets[0].CurrentHealth,targetHp-7,"Interface damage reaches the first target.");
            Near(body.Targets[1].CurrentHealth,targetHp,"Direct body damage leaves siblings untouched.");
            CheckIndependentBars();
            actor.ReceiveSplashDamage(3);
            Near(body.Targets[0].CurrentHealth,targetHp-10,"Base-reference splash hits the first target once.");
            Near(body.Targets[1].CurrentHealth,targetHp-3,"Base-reference splash reaches siblings.");
            CheckIndependentBars();
            var oldTarget = body.Targets[0];
            body.Initialize(data,path,null,registry);
            oldTarget.ReceiveDamage(1000);
            oldTarget.StatusEffects.ApplySlow(new object(),.1f,10);
            Near(body.Targets[0].CurrentHealth,targetHp,"Retired sections cannot damage replacement sections.");
            Near(body.Movement.Modifiers.Multiplier,1,"Retired sections cannot slow the replacement body.");
            Check(registry.AliveEnemyCount == 1,
                "Reinitializing a living body must replace targets without duplicate registry entries.");
            var parts = body.Targets;
            Check(parts.Count == 5 && registry.AliveEnemyCount == 1,
                "Five sections must count as one spawned body.");
            Check(registry.Enemies[0] == body,"Only the body is registered for targeting and wave counting.");
            Check(body.GetComponentsInChildren<Enemy>().Length == 1 && body.GetComponentsInChildren<EnemyPathFollower>().Length == 1,
                "A cluster has one enemy actor and one movement component, with no section GameObjects.");
            body.Movement.Advance(1);
            foreach(var part in parts) {
                Check(!typeof(Enemy).IsAssignableFrom(typeof(CornTarget)) && !typeof(Component).IsAssignableFrom(typeof(CornTarget)),
                    "Sections must be plain objects, not enemy actors or Unity components.");
            }
            parts[1].ReceiveDamage(7);
            Near(parts[1].CurrentHealth,targetHp-7,"Chosen target must receive direct damage.");
            Near(parts[0].CurrentHealth,targetHp,"Other targets must keep their HP.");
            Near(body.GetComponent<EnemyHealth>().GetLayerHealth(1),targetHp-7,"Body UI must mirror the actual target.");
            parts[1].StatusEffects.ApplyBurn(new object(),3,10);
            parts[1].StatusEffects.Advance(1);
            Near(parts[1].CurrentHealth,targetHp-10,"Burn must stay on the chosen target.");
            CheckIndependentBars();
            Near(parts[0].CurrentHealth,targetHp,"Burn must not affect siblings.");
            var sameSource = new object();
            parts[0].StatusEffects.ApplySlow(sameSource,.5f,1);
            parts[2].StatusEffects.ApplySlow(sameSource,.7f,4);
            Near(body.Movement.EffectiveMovementSpeed,data.MovementSpeed*.5f,"Strongest part slow must affect shared movement.");
            parts[0].StatusEffects.Advance(1);
            Near(body.Movement.EffectiveMovementSpeed,data.MovementSpeed*.7f,"One part's slow expiry must not erase another part's slow.");
            parts[2].ReceiveDamage(1000);
            Near(body.Movement.EffectiveMovementSpeed,data.MovementSpeed,"Dead target must clear its slow.");
            Check(!parts[2].IsAlive && body.IsAlive && registry.AliveEnemyCount == 1,"A dead section leaves its body alive and targetable.");
            parts[1].ReceiveDamage(1000);
            parts[1].StatusEffects.Advance(10);
            Near(parts[0].CurrentHealth,targetHp,"Burn on a dead target must never jump to a sibling.");
            var targeting = ScriptableObject.CreateInstance<ClosestTargeting>(); temporary.Add(targeting);
            var towerData = ScriptableObject.CreateInstance<TurretDefinition>(); temporary.Add(towerData);
            Set(towerData,"targeting",targeting); Set(towerData,"attackRange",100f);
            Set(towerData,"towerType",TowerType.Ketchup); Set(towerData,"damage",6f);
            AttackerTower tower = null;
            void UseTower<T>() where T : AttackerTower {
                if (tower != null) UnityEngine.Object.DestroyImmediate(tower.gameObject);
                var towerObject = new GameObject(typeof(T).Name); towerObject.transform.SetParent(root.transform,false);
                tower = towerObject.AddComponent<T>(); tower.Initialize(towerData,registry);
            }
            UseTower<SplashTower>();
            Check(tower.FindTarget() == body,"Towers target the cluster body, never individual sections.");
            tower.Attack(body);
            Near(parts[0].CurrentHealth,targetHp-6,"Ketchup must damage the first survivor once.");
            Near(parts[3].CurrentHealth,targetHp-6,"Ketchup must damage every survivor once.");
            Near(parts[4].CurrentHealth,targetHp-6,"Ketchup must damage the last survivor once.");
            Near(body.CurrentHealth,3*(targetHp-6),"Body health must equal the sum of the independent sections.");
            CheckIndependentBars();
            int deaths = 0; body.Died += enemy => deaths++;
            Set(towerData,"damage",100f); tower.Attack(body);
            Check(deaths == 1 && !body.IsAlive && registry.AliveEnemyCount == 0,
                "Last targets dying together must produce one body death and clear the body registry.");
            body.ReceiveSplashDamage(1000); Check(deaths == 1,"Repeated damage must not repeat rewards/death.");
            Check(!healthView.IsVisible,"All bars hide when all sections are destroyed.");
            body.Initialize(data,path,null,registry);
            Check(registry.AliveEnemyCount == 1,"Reinitialization must replace old sections.");
            parts = body.Targets;
            Set(towerData,"towerType",TowerType.HotSauce); Set(towerData,"damage",2f);
            Set(towerData,"burnDamagePerSecond",3f); Set(towerData,"burnDuration",4f);
            UseTower<BurningTower>();
            tower.Attack(body);
            Near(parts[0].CurrentHealth,targetHp-2,"A tower given the body must resolve an individual target.");
            parts[0].StatusEffects.Advance(1);
            Near(parts[0].CurrentHealth,targetHp-5,"Hot Sauce must attach its burn to that same target.");
            Near(parts[1].CurrentHealth,targetHp,"Hot Sauce must leave sibling targets unaffected.");
            parts[0].ReceiveDamage(1000);
            parts[0].StatusEffects.Advance(10);
            Near(parts[1].CurrentHealth,targetHp,"Hot Sauce burn must stop with its target.");
            // A lethal hit must not move its status effect to the next living section.
            body.Initialize(data,path,null,registry);
            Set(towerData,"damage",targetHp);
            tower.Attack(body); body.AdvanceSections(1);
            Check(!body.Targets[0].IsAlive,"A lethal Hot Sauce hit destroys the first section.");
            Near(body.Targets[1].CurrentHealth,targetHp,"A lethal hit must not attach burn to the next section.");
            Set(towerData,"towerType",TowerType.Oil);
            UseTower<SlowingTower>();
            tower.Attack(body);
            Near(body.Movement.Modifiers.Multiplier,1,"A lethal Oil hit must not attach slow to the next section.");

            var specializedObject = new GameObject("Specialized burn"); specializedObject.transform.SetParent(root.transform,false);
            var specialized = specializedObject.AddComponent<BurningTower>(); specialized.Initialize(towerData,registry);
            body.Initialize(data,path,null,registry); Set(towerData,"damage",2f);
            specialized.Attack(body); body.AdvanceSections(1);
            Near(body.Targets[0].CurrentHealth,targetHp-5,"Dedicated BurningTower damages and burns the same section.");
            Near(body.Targets[1].CurrentHealth,targetHp,"Dedicated burn leaves sibling sections untouched.");
            UnityEngine.Object.DestroyImmediate(specializedObject);

            // Shields protect each section; piercing bypasses that same protection.
            body.Initialize(data,path,null,registry);
            var shieldData = UnityEngine.Object.Instantiate(data); temporary.Add(shieldData);
            Set(shieldData,"role",EnemyRole.ShieldSupport); Set(shieldData,"supportArea",100f);
            Set(shieldData,"shieldValue",.6f); Set(shieldData,"vegetableType",VegetableType.Other);
            var shieldObject = new GameObject("Shield"); shieldObject.transform.SetParent(root.transform,false);
            var shield = shieldObject.AddComponent<BasicEnemy>(); shield.Initialize(shieldData,path,null,registry);
            var addressed = body.Targets[1];
            addressed.ReceiveDamage(10);
            Near(body.Targets[1].CurrentHealth,targetHp-4,"Shields reduce addressed section damage.");
            addressed.ReceiveDamage(10,true);
            Near(body.Targets[1].CurrentHealth,targetHp-14,"Piercing bypasses section shields.");
            body.ReceiveSplashDamage(5);
            Near(body.Targets[1].CurrentHealth,targetHp-16,"Section splash uses shield reduction once.");
            Near(body.Targets[0].CurrentHealth,targetHp-2,"Body splash applies shield reduction once to each living section.");
            shield.ReceiveDamage(10,true);
            Near(shield.CurrentHealth,targetHp-10,"Ordinary target damage still reaches enemy health.");
            UnityEngine.Object.DestroyImmediate(shieldObject);
            Check(registry.AliveEnemyCount == 1,"Destroyed ordinary enemies leave only the cluster in the registry.");

            body.Initialize(data,path,null,registry);
            body.Targets[2].StatusEffects.ApplyBurn(new object(),3,2);
            flow.TogglePause(); body.AdvanceSections(1);
            Near(body.Targets[2].CurrentHealth,targetHp,"Paused clusters must not advance section burns.");
            flow.TogglePause(); body.AdvanceSections(1);
            Near(body.Targets[2].CurrentHealth,targetHp-3,"Cluster update advances its section timers.");
            Near(body.Targets[1].CurrentHealth,targetHp,"Section timers do not affect siblings.");

            // Area skills visit the body once; the body forwards damage to each living section.
            body.Initialize(data,path,null,registry);
            var skill = ScriptableObject.CreateInstance<CircularDamageSkill>(); temporary.Add(skill);
            var skillData = ScriptableObject.CreateInstance<SkillDefinition>(); temporary.Add(skillData);
            var profile = ScriptableObject.CreateInstance<SkillRangeProfile>(); temporary.Add(profile);
            Set(skillData,"damage",5f); Set(skillData,"rangeProfile",profile);
            skill.Apply(skillData,new SkillCastContext(body.transform.position,registry,flow));
            foreach(var section in body.Targets) Near(section.CurrentHealth,targetHp-5,"Area skills hit each section once.");
            Near(body.CurrentHealth,5*(targetHp-5),"Area damage keeps the aggregate UI synchronized.");
            bodyObject.SetActive(false);
            Check(registry.AliveEnemyCount == 0,"Disabling a body must remove every target.");
            // ClusterEnemy owns the behavior regardless of vegetable identity.
            var genericData = UnityEngine.Object.Instantiate(data); temporary.Add(genericData);
            Set(genericData,"vegetableType",VegetableType.Other);
            bodyObject.SetActive(true);
            body.Initialize(genericData,path,null,registry);
            Check(body.Targets.Count == 5 && registry.AliveEnemyCount == 1,
                "ClusterEnemy must create sections without a Corn vegetable flag.");
            UnityEngine.Object.DestroyImmediate(bodyObject);
            Check(registry.AliveEnemyCount == 0,
                "Destroying a living ClusterEnemy must retire all sections.");
            // Base arrival belongs to the body, not its sections.
            var baseHealth = root.AddComponent<BaseHealth>(); baseHealth.Initialize(1000);
            var endpointObject = new GameObject("Endpoint cluster"); endpointObject.transform.SetParent(root.transform,false);
            var endpoint = endpointObject.AddComponent<ClusterEnemy>();
            endpoint.Initialize(data,path,baseHealth,registry);
            int endpointDeaths = 0; endpoint.Died += enemy => endpointDeaths++;
            var endpointTarget = endpoint.Targets[0];
            endpoint.Movement.Advance(10000);
            Check(baseHealth.Current == 1000-data.BaseDamage && endpointDeaths == 0,
                "Base arrival damages the base once and gives no combat kill reward.");
            Check(!endpointTarget.IsAlive && registry.AliveEnemyCount == 0,
                "Base arrival retires every section and the body.");
            Debug.Log("CORN COMBAT VALIDATION PASSED");
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
            foreach(var item in temporary) UnityEngine.Object.DestroyImmediate(item);
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
}
