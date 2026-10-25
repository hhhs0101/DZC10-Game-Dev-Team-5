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
            tower.AttackStatus.Apply(new object(),10);tower.Attack(victim);
            Near(victim.CurrentHealth,10000,"Disrupted damage");Near(events,0,"Disrupted event");tower.AttackStatus.Clear();
            victim.StatusEffects.SendMessage("Awake");
            tower.Attack(victim);
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
            if(data.TowerType==TowerType.Mayonnaise) {
                var otherData=AssetDatabase.LoadAssetAtPath<TurretDefinition>("Assets/Defense/Data/TestTurret6.asset");
                var other=UnityEngine.Object.Instantiate(otherData.Prefab,root.transform);other.Initialize(otherData,registry);
                child("Support slot").AddComponent<TowerPlacementSlot>().TryOccupy(tower);
                child("Target slot").AddComponent<TowerPlacementSlot>().TryOccupy(other);
                Near(other.EffectiveDamage,otherData.Damage*data.DamageBoost,"Support multiplier");
                other.AttackStatus.Apply(new object(),100);
                tower.AdvanceSupport(data.CleanseInterval-.01f);
                if(!other.IsDisrupted) throw new Exception("Early cleanse");
                tower.AdvanceSupport(.02f);
                if(other.IsDisrupted) throw new Exception("Missed cleanse");
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
