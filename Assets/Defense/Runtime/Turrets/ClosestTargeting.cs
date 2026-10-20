using UnityEngine;
using System.Collections.Generic;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Targeting/Closest")]
public sealed class ClosestTargeting : TargetingStrategy {
    public override Enemy Select(Vector3 origin, float range, IReadOnlyList<Enemy> enemies, WaypointPlane plane = WaypointPlane.XZ) {
        Enemy best = null; float distance = range * range;
        foreach (Enemy enemy in enemies) {
            if (enemy == null || !enemy.IsAlive) continue;
            float candidate = PlanarSpace.SqrDistance(enemy.transform.position,origin,plane);
            if (candidate <= distance) { best = enemy; distance = candidate; }
        }
        return best;
    }
}
}
