using UnityEngine;
namespace Defense {
// Captured at commitment; target stays fixed even if selection or the pointer changes.
public readonly struct SkillCastContext {
    public Vector3 TargetPosition { get; }
    public Transform ThrowOrigin { get; }
    public WaypointPlane Plane { get; }
    public EnemyRegistry Registry { get; }
    public GameFlow Flow { get; }
    public SkillCastContext(Vector3 targetPosition, EnemyRegistry registry, GameFlow flow, Transform throwOrigin = null) {
        TargetPosition = targetPosition; Registry = registry; Flow = flow; ThrowOrigin = throwOrigin; Plane = registry.Plane;
    }
}
}
