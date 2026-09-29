using UnityEngine;
namespace Defense {
public sealed class ElixirSystem : MonoBehaviour {
    public const float Maximum = 10, Starting = 3, Regeneration = .36f;
    private GameFlow flow;
    public float Current { get; private set; } = Starting;
    public void Initialize(GameFlow gameFlow) { flow = gameFlow; Current = Starting; }
    public void Advance(float deltaTime) { if (flow != null && flow.IsPlaying && deltaTime > 0 && !float.IsInfinity(deltaTime)) Current = Mathf.Clamp(Current + Regeneration * deltaTime,0,Maximum); }
    private void Update() => Advance(Time.deltaTime);
    public bool CanAfford(float cost) => cost >= 0 && cost <= Current;
    public bool TrySpend(float cost) { if (!CanAfford(cost)) return false; Current -= cost; return true; }
}
}
