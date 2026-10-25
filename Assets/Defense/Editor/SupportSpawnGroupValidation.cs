using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense.Editor {
public static class SupportSpawnGroupValidation {
    private static void Check(bool ok,string message) {if(!ok)throw new Exception(message);}
    private static void Set(object target,string name,object value) => target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static T Get<T>(object target,string name) => (T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    [MenuItem("Defense/Validate Support Spawn Groups")]
    public static void Run() {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Run in Edit Mode.");
        var catalog=Resources.Load<EnemyDefinitionCatalog>(EnemyDefinitionCatalog.ResourcePath);
        Check(catalog.TryBuildLookup(out var lookup,out var issue),issue);
        foreach(int n in new[]{2,3}) {
            var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Defense/Data/SpawnPlans/Stage"+n+".json");
            Check(SpawnPlanLoader.TryLoad(asset,"Stage "+n,catalog,out var plan,out issue),issue);
            var groups=plan.Events.Where(e=>e.RouteGroup!=null).GroupBy(e=>e.RouteGroup).ToArray();
            Check(groups.Length==12,"Expected twelve support groups in Stage "+n);
            Check(plan.Events.Where(e=>e.Enemy.Role==EnemyRole.SpeedSupport||e.Enemy.Role==EnemyRole.ShieldSupport).All(e=>e.RouteGroup!=null),"Every support enemy is grouped");
            foreach(var group in groups) {
                Check(group.Count()>1&&group.Any(e=>e.Enemy.Role!=EnemyRole.SpeedSupport&&e.Enemy.Role!=EnemyRole.ShieldSupport),"Support has other vegetables");
                Check(group.Skip(1).All(e=>e.Delay<=1.5f),"Support escorts remain within their authored burst");
            }
        }
        foreach(var field in new[]{"\"routeGroup\":\"\"","\"routeGroup\":null","\"routeGroup\":1","\"routeGroup\":\"g\",\"routeGroup\":\"g\""}) {
            var bad=new TextAsset("{\"stageId\":\"test\",\"spawns\":[{\"enemyId\":\"enemy_carrot\",\"delay\":0,"+field+"}]}");
            try {Check(!SpawnPlanLoader.TryLoad(bad,"test",catalog,out _,out _),"Invalid group must be rejected");}
            finally {UnityEngine.Object.DestroyImmediate(bad);}
        }
        var previous=SceneManager.GetActiveScene();var randomState=UnityEngine.Random.state;float timeScale=Time.timeScale;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject("Support group fixture");
        try {
            Func<string,GameObject> child=name=>{var go=new GameObject(name);go.transform.SetParent(root.transform);return go;};
            Func<string,WaypointPath> route=name=>{
                var go=child(name);var a=child(name+" start").transform;var b=child(name+" end").transform;b.position=Vector3.right*100;
                var p=go.AddComponent<WaypointPath>();p.Configure(WaypointPlane.XY,new[]{a,b});return p;
            };
            var main=route("Main");var left=route("Left");var right=route("Right");var exit=route("Exit");
            var flow=child("Flow").AddComponent<GameFlow>();var registry=child("Registry").AddComponent<EnemyRegistry>();
            var spawner=child("Spawner").AddComponent<EnemySpawner>();
            Set(spawner,"path",main);Set(spawner,"flow",flow);Set(spawner,"registry",registry);Set(spawner,"baseHealth",child("Base").AddComponent<BaseHealth>());Set(spawner,"enemyRoot",root.transform);
            for(int mode=0;mode<2;mode++) {
                Set(spawner,"branchPaths",mode==0?new[]{left,right}:new[]{main});
                spawner.ConfigureDivergingRoute(mode==0?null:left,mode==0?null:exit,3);
                var seen=new HashSet<WaypointPath>();
                for(int i=0;i<8;i++) {
                    string group="fixture_"+i;
                    var leader=spawner.Spawn(lookup["enemy_bellpepper"],group);
                    var selected=leader.Movement.Route;var continuation=Get<WaypointPath>(leader.Movement,"continuationPath");
                    seen.Add(selected);UnityEngine.Object.DestroyImmediate(leader.gameObject);
                    var follower=spawner.Spawn(lookup["enemy_eggplant"],group);
                    var garlic=spawner.Spawn(lookup["enemy_garlic"],group);
                    Check(follower.Movement.Route==selected&&garlic.Movement.Route==selected,"Group uses leader route even after leader destruction");
                    Check(Get<WaypointPath>(follower.Movement,"continuationPath")==continuation&&Get<WaypointPath>(garlic.Movement,"continuationPath")==continuation,"Group preserves tunnel exit");
                    if(continuation!=null)Check(Get<float>(follower.Movement,"continuationDelay")==3,"Tunnel delay preserved");
                    UnityEngine.Object.DestroyImmediate(follower.gameObject);UnityEngine.Object.DestroyImmediate(garlic.gameObject);
                }
                Check(seen.Count==2,"Groups can use either route");
                var boss=spawner.Spawn(lookup["enemy_broccoli"],"fixture_0");Check(boss.Movement.Route==main,"Boss stays on main route");UnityEngine.Object.DestroyImmediate(boss.gameObject);
            }
            var text=new TextAsset("{\"stageId\":\"fixture\",\"spawns\":[{\"enemyId\":\"enemy_bellpepper\",\"delay\":1,\"routeGroup\":\"fixture_0\"},{\"enemyId\":\"enemy_carrot\",\"delay\":0,\"routeGroup\":\"fixture_0\"}]}");
            var stage=ScriptableObject.CreateInstance<StageDefinition>();
            try {
                Set(stage,"spawnPlanJson",text);var schedule=child("Schedule").AddComponent<FixedIntervalSpawner>();Set(schedule,"spawner",spawner);
                schedule.Configure(stage,catalog);
                Check(Get<System.Collections.IDictionary>(spawner,"groupRoutes").Count==0,"New schedule clears cached routes");
                var actors=new List<Enemy>();schedule.EnemyScheduled+=actors.Add;schedule.Advance(1);
                Check(actors.Count==2&&actors[0].Movement.Route==actors[1].Movement.Route&&schedule.IsFinishedSpawning,"Zero-delay group spawns together on one route");
            } finally {UnityEngine.Object.DestroyImmediate(stage);UnityEngine.Object.DestroyImmediate(text);}
            Debug.Log("SUPPORT SPAWN GROUPS PASS: stage data, parser, both branches, tunnel exit, leader deletion, boss rule and schedule reset.");
        } finally {
            UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);UnityEngine.Random.state=randomState;Time.timeScale=timeScale;
        }
    }
}
}
