using UnityEngine;
namespace Defense {
// Composition root: wires stage configuration once; does not run gameplay systems.
[DefaultExecutionOrder(-100)]
public sealed class StageInitializer : MonoBehaviour {
    [SerializeField] private StageDefinition definition;
    [SerializeField] private GameFlow flow;
    [SerializeField] private ResourceWallet wallet;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private FixedIntervalSpawner schedule;
    private void Awake() {
        wallet.Initialize(definition.StartingResources);
        baseHealth.Initialize(definition.BaseHealth);
        baseHealth.Depleted += flow.GameOver;
        schedule.Configure(definition.Enemy, definition.SpawnInterval);
    }
    private void OnDestroy() { if (baseHealth != null) baseHealth.Depleted -= flow.GameOver; }
}
}
