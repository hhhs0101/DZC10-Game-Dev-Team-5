using UnityEngine;
namespace Defense {
public sealed class TowerDisruptionAbility : EnemyRoleAbility {
    private object sourceToken;
    private float cooldown;
    public Turret LastTarget { get; private set; }
    public float CooldownRemaining => cooldown;
    protected override void ResetState() { sourceToken = new object(); cooldown = 0; LastTarget = null; }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime) {
        if(!IsOperational || (Flow != null && !Flow.IsPlaying) || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        cooldown = Mathf.Max(0,cooldown-deltaTime);
        if(cooldown > 0) return;
        // First attempt is immediate; missed attempts also wait the configured interval.
        cooldown = Data.DisruptionCooldown; TryDisruptNearest();
    }
    public bool TryDisruptNearest() {
        if(!IsOperational || (Flow != null && !Flow.IsPlaying)) return false;
        Turret nearest = null; float nearestDistance = float.PositiveInfinity;
        foreach(var slot in Object.FindObjectsByType<TowerPlacementSlot>()) {
            var tower = slot.Occupant;
            if(tower == null || !tower.isActiveAndEnabled || tower.Definition == null || tower.gameObject.scene != Owner.gameObject.scene) continue;
            float distance = PlanarSpace.SqrDistance(Owner.transform.position,tower.transform.position,WaypointPlane.XY);
            if(distance <= Data.DisruptionRange*Data.DisruptionRange && distance < nearestDistance) { nearest = tower; nearestDistance = distance; }
        }
        LastTarget = nearest;
        return nearest != null && nearest.AttackStatus.Apply(sourceToken,Data.DisruptionDuration);
    }
    // Applied effects intentionally survive source death until target-owned expiry.
    protected override void OnDeactivated() => LastTarget = null;
}
}
