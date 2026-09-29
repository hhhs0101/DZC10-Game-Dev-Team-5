using UnityEngine;
using System.Collections.Generic;
namespace Defense {
// Stateless asset shared between turret instances, assignable in the Inspector.
public abstract class TargetingStrategy : ScriptableObject, ITargetingStrategy {
    public abstract Enemy Select(Vector3 origin, float range, IReadOnlyList<Enemy> enemies);
}
}
