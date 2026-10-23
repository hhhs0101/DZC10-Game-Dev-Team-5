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
        cooldown = Data.DisruptionCooldown; TryDisruptRandom();
    }
    public bool TryDisruptNearest() => TryDisruptRandom(); // Legacy callers.
    public bool TryDisruptRandom() {
        if(!IsOperational || (Flow != null && !Flow.IsPlaying)) return false;
        Turret nearest = null; int candidates = 0;
        foreach(var slot in Object.FindObjectsByType<TowerPlacementSlot>()) {
            var tower = slot.Occupant;
            if(tower == null || !tower.isActiveAndEnabled || tower.Definition == null || tower.gameObject.scene != Owner.gameObject.scene) continue;
            float distance = PlanarSpace.SqrDistance(Owner.transform.position,tower.transform.position,WaypointPlane.XY);
            if(distance <= Data.DisruptionRange*Data.DisruptionRange && Random.Range(0,++candidates) == 0) nearest = tower;
        }
        LastTarget = nearest;
        if(nearest == null) return false;
        DirtProjectile.Launch(Owner.transform.position,nearest,sourceToken,Data.DisruptionDuration,Flow);
        return true;
    }
    // Applied effects intentionally survive source death until target-owned expiry.
    protected override void OnDeactivated() => LastTarget = null;
}
}
