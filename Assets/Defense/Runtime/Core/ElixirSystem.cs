using System;
using UnityEngine;
namespace Defense {
public sealed class ElixirSystem : MonoBehaviour {
    public const int Maximum = 10, Starting = 2;
    public const float RegenerationInterval = 2.8f, Regeneration = 1f / RegenerationInterval;
    private GameFlow flow;
    private double regenerationElapsed;
    public int CurrentEnergy { get; private set; } = Starting;
    public int MaxEnergy => Maximum;
    // Retain the existing consumer name; spendable energy is always an integer.
    public int Current => CurrentEnergy;
    public event Action<int> Changed;
    public void Initialize(GameFlow gameFlow) {
        flow = gameFlow; regenerationElapsed = 0; CurrentEnergy = Starting;
        Changed?.Invoke(CurrentEnergy);
    }
    public void Advance(float deltaTime) {
        if (flow == null || !flow.IsPlaying || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        if (CurrentEnergy == Maximum) { regenerationElapsed = 0; return; }
        regenerationElapsed += deltaTime;
        // Bound before casting so even an extremely long finite frame cannot overflow.
        int restored = (int)Math.Min(Maximum - CurrentEnergy, Math.Floor(regenerationElapsed / RegenerationInterval));
        if (restored == 0) return;
        regenerationElapsed -= restored * RegenerationInterval;
        CurrentEnergy += restored;
        if (CurrentEnergy == Maximum) regenerationElapsed = 0;
        Changed?.Invoke(CurrentEnergy);
    }
    private void Update() => Advance(Time.deltaTime);
    public bool CanAfford(int cost) => cost >= 0 && cost <= CurrentEnergy;
    public bool TrySpend(int cost) {
        if (!CanAfford(cost)) return false;
        if (cost == 0) return true;
        CurrentEnergy -= cost; Changed?.Invoke(CurrentEnergy); return true;
    }
    // Legacy API accepts only exactly integral costs; it never silently rounds.
    public bool CanAfford(float cost) => !float.IsNaN(cost) && !float.IsInfinity(cost) && cost >= 0 && cost <= Maximum && cost == Mathf.Floor(cost) && CanAfford((int)cost);
    public bool TrySpend(float cost) => CanAfford(cost) && TrySpend((int)cost);
}
}
