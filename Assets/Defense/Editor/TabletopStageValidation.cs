using System;
using System.Linq;
using UnityEngine;
namespace Defense.Editor {
public static class TabletopStageValidation {
    private static Vector3 cameraPosition;
    private static Quaternion cameraRotation;
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    public static void Run(Action<bool,string> check) {
        var surface = Find<TabletopSurface>(); var camera = Camera.main; var path = Find<WaypointPath>();
        check(surface != null && surface.GetComponent<BoxCollider>() != null && Mathf.Approximately(surface.Height,0),"3D tabletop has a collider and logical surface at Y=0");
        check(!camera.orthographic && camera.transform.position.y>0 && surface.WorldCamera==camera,"Fixed elevated perspective camera is assigned through scene configuration");
        cameraPosition=camera.transform.position; cameraRotation=camera.transform.rotation;
        check(surface.gameObject.layer==LayerMask.NameToLayer("GameplaySurface") && surface.SurfaceLayers==(1<<surface.gameObject.layer),"GameplaySurface collider and raycast mask configured");
        check(surface.Contains(Vector2.zero) && !surface.Contains(new Vector2(11,0)),"Tabletop bounds reject positions outside the table");
        check(path.Count>=3 && path.GetPoint(0).x>path.GetPoint(path.Count-1).x,"Configured 3D route starts right and ends left");
        float length=0; for(int i=1;i<path.Count;i++) length+=Vector3.Distance(path.GetPoint(i-1),path.GetPoint(i));
        check(length>Vector3.Distance(path.GetPoint(0),path.GetPoint(path.Count-1))+.1f && Enumerable.Range(0,path.Count).All(i=>surface.Contains(PlanarSpace.Project(path.GetPoint(i))) && path.GetPoint(i).y==surface.Height),"Curved waypoint route stays on the tabletop XZ plane");
        var routeView=Find<PathVisualization>(); routeView.Refresh(); var line=routeView.GetComponent<LineRenderer>();
        check(line.positionCount==path.Count && Enumerable.Range(0,path.Count).All(i=>PlanarSpace.Project(line.GetPosition(i))==PlanarSpace.Project(path.GetPoint(i))),"Path renderer derives all positions from actual waypoints");
        var slots=UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None);
        check(slots.Length==8 && slots.All(s=>surface.Contains(PlanarSpace.Project(s.transform.position)) && s.transform.position.y==0 && s.GetComponent<BoxCollider>()!=null && s.gameObject.layer==LayerMask.NameToLayer("PlacementSlot")),"Eight editable scene slots have tabletop positions and 3D placement colliders");
        check(UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None).Length==0,"Battle scene has no 2D colliders");
        check(Enumerable.Range(0,path.Count).Select(i=>path.GetPoint(i)).Concat(slots.Select(s=>s.transform.position)).All(p=> {var v=camera.WorldToViewportPoint(p); return v.z>0 && v.x>.03f && v.x<.97f && v.y>.2f && v.y<.95f;}),"Camera projection includes all path and slot anchors above the Hand area at test aspect");
        Physics.SyncTransforms();
        check(Physics.Raycast(new Ray(new Vector3(9,10,4),Vector3.down),out var hit,30,surface.SurfaceLayers) && hit.collider==surface.GetComponent<Collider>(),"Prepared 3D surface mask supports tabletop raycasts");
        var slot=slots[0];
        check(Physics.Raycast(new Ray(slot.transform.position+Vector3.up*5,Vector3.down),out hit,10,surface.PlacementLayers) && hit.collider.GetComponent<TowerPlacementSlot>()==slot,"Prepared placement mask hits slot collider");
        var enemy=Find<EnemySpawner>().Spawn(PlayerSession.ActiveStage.Enemy); var follower=enemy.GetComponent<EnemyPathFollower>();
        check(enemy.GetComponent<SphereCollider>()!=null && enemy.gameObject.layer==LayerMask.NameToLayer("Enemy") && enemy.transform.Find("Visual/Sphere").GetComponent<MeshFilter>()!=null && enemy.transform.Find("Visual/Sphere").GetComponent<Enemy>()==null,"Enemy runtime root is separate from replaceable Sphere visual");
        var start=enemy.transform.position; enemy.transform.position+=Vector3.up*50;
        follower.Advance(.1f);
        check(enemy.transform.position.y==surface.Height && Mathf.Abs(Vector2.Distance(PlanarSpace.Project(start),PlanarSpace.Project(enemy.transform.position))-PlayerSession.ActiveStage.Enemy.MovementSpeed*.1f)<.001f,"Path movement ignores height and advances at unchanged planar speed");
        routeView.enabled=false; start=enemy.transform.position; follower.Advance(.1f);
        check(!line.enabled && enemy.transform.position!=start,"Disabling route helper leaves movement functional"); routeView.enabled=true;
        follower.Stop(); enemy.transform.position=new Vector3(0,100,.25f);
        check(PlayerSession.Catalog.AvailableTowers[0].Targeting.Select(new Vector3(0,-100,0),.5f,Find<EnemyRegistry>().Enemies)==enemy,"Closest targeting and range ignore visual height");
        var hp=enemy.GetComponent<EnemyHealthBar>(); hp.enabled=false; float health=enemy.CurrentHealth; enemy.ReceiveDamage(1);
        check(!hp.IsVisible && enemy.CurrentHealth==health-1,"Removing HP presentation from updates does not affect health logic"); hp.enabled=true;
        check(hp.IsVisible && enemy.transform.Find("Enemy HP").GetComponent<Canvas>().renderMode==RenderMode.WorldSpace,"Reusable HP bar uses world-space Canvas and reconnects to health");
        var effect=PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>().Last().Skill;
        health=enemy.CurrentHealth; effect.Effect.Apply(effect,Vector2.zero,Find<EnemyRegistry>());
        check(Mathf.Approximately(enemy.CurrentHealth,Mathf.Max(0,health-effect.Damage)),"Skill AoE uses planar XZ radius regardless of height");
        enemy.ReceiveDamage(10000);
        var root=new GameObject("Isolated slot without view"); var isolated=root.AddComponent<TowerPlacementSlot>();
        var tower=UnityEngine.Object.Instantiate(PlayerSession.Catalog.AvailableTowers[0].Prefab,root.transform); tower.enabled=false;
        check(isolated.TryOccupy(tower) && isolated.IsOccupied,"Placement state remains functional with no view component");
        check(tower.transform.Find("Visual/Cuboid").GetComponent<MeshFilter>()!=null && tower.transform.Find("Visual/ProjectileOrigin")!=null && tower.transform.Find("Visual/Cuboid").GetComponent<Turret>()==null,"Turret behavior stays on root with replaceable Cuboid and unused projectile origin");
        UnityEngine.Object.DestroyImmediate(root);
    }
    public static void CapturePreview() {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera=Camera.main; var previous=camera.targetTexture; var active=RenderTexture.active;
        var target=new RenderTexture(1280,720,24); var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try {
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
            System.IO.File.WriteAllBytes("/tmp/defense-stage1-preview.png",texture.EncodeToPNG());
        } finally {
            camera.targetTexture=previous; RenderTexture.active=active; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
    public static void VerifyFixedCamera(Action<bool,string> check) => check(Camera.main.transform.position==cameraPosition && Camera.main.transform.rotation==cameraRotation,"Camera stays fixed across gameplay and Pause frames");
}
}
