using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// Sole 2D screen/world conversion and slot-query boundary. Never calls 3D Physics.
public sealed class BattlefieldPointer2D : MonoBehaviour {
    [SerializeField] private BattlefieldMap2D map;
    [SerializeField] private LayerMask placementLayers;
    private readonly List<Collider2D> hits = new List<Collider2D>();
    public BattlefieldMap2D Map => map;
    public float Depth => map.PlayableArea.Bounds.center.z;
    public bool TryGetPoint(Vector2 screen, out Vector3 world) {
        world = default; var camera = map.WorldCamera;
        if (!camera.pixelRect.Contains(screen) || GameplayPointer.IsOverUI(screen)) return false;
        var plane = new Plane(Vector3.forward,new Vector3(0,0,Depth));
        var ray = camera.ScreenPointToRay(screen);
        if (!plane.Raycast(ray,out float distance)) return false;
        var hit = ray.GetPoint(distance);
        if (!map.PlayableArea.Contains(hit)) return false;
        world = new Vector3(hit.x,hit.y,Depth); return true;
    }
    public Turret GetPlacedTower(Vector3 point) {
        // Separate query: overlapping slot/decorative colliders never mask a Tower's hit area.
        var filter = new ContactFilter2D { useTriggers=true };
        hits.Clear(); Physics2D.OverlapPoint(point,filter,hits);
        Turret nearest = null; float distance = float.PositiveInfinity;
        foreach (var hit in hits) {
            var tower = hit.GetComponentInParent<Turret>();
            if (tower == null || !tower.isActiveAndEnabled || tower.Definition == null) continue;
            float candidate = PlanarSpace.SqrDistance(point,tower.transform.position,WaypointPlane.XY);
            if (candidate < distance) { nearest = tower; distance = candidate; }
        }
        return nearest;
    }
    public TowerPlacementSlot GetSlot(Vector3 point) {
        var filter = new ContactFilter2D { useLayerMask=true,layerMask=placementLayers,useTriggers=true };
        hits.Clear(); Physics2D.OverlapPoint(point,filter,hits);
        foreach(var hit in hits) {var slot=hit.GetComponentInParent<TowerPlacementSlot>();if(slot != null) return slot;}
        return null;
    }
}
}
