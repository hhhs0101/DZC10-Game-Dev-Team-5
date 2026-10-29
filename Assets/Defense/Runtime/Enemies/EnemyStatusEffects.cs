using UnityEngine;
namespace Defense {
// Unity timer driver for whole enemies. ClusterEnemy drives its sections itself.
public sealed class EnemyStatusEffects : MonoBehaviour {
    private TargetStatusEffects state;
    public TargetStatusEffects State {
        get {
            if (state == null) {
                var owner = GetComponent<Enemy>();
                state = new TargetStatusEffects(owner, () => owner != null && owner.IsAlive,
                    amount => owner.ReceiveDamage(amount));
            }
            return state;
        }
    }
    private void Awake() { var initialized = State; }
    public void ApplyBurn(object source, float dps, float seconds) => State.ApplyBurn(source,dps,seconds);
    public void ApplySlow(object source, float multiplier, float seconds) => State.ApplySlow(source,multiplier,seconds);
    public void Advance(float deltaTime) => State.Advance(deltaTime);
    public void Clear() { if (state != null) state.Clear(); }
    private void Update() => Advance(Time.deltaTime);
    private void OnDisable() => Clear();
}
}
