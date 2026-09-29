using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public interface ITargetingStrategy {
    Enemy Select(Vector3 origin, float range, IReadOnlyList<Enemy> enemies);
}
}
