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
        PlayerSession.Ensure();
        if (PlayerSession.ActiveStage != null && PlayerSession.ActiveStage.SceneName == gameObject.scene.name)
            definition = PlayerSession.ActiveStage;
        var placement = GetComponent<TurretPlacementController>();
        var hand = GetComponent<GameplayHand>();
        if (hand == null) hand = gameObject.AddComponent<GameplayHand>();
        var elixir = GetComponent<ElixirSystem>() ?? gameObject.AddComponent<ElixirSystem>();
        elixir.Initialize(flow);
        hand.Initialize(PlayerSession.Deck,placement,flow);
        var skills = GetComponent<SkillCastingController>() ?? gameObject.AddComponent<SkillCastingController>();
        skills.Initialize(hand, flow, GetComponent<EnemyRegistry>(), Camera.main);
        wallet.Initialize(definition.StartingResources);
        baseHealth.Initialize(definition.BaseHealth);
        baseHealth.Depleted += flow.GameOver;
        schedule.Configure(definition.Enemy, definition.SpawnInterval);
    }
    private void OnDestroy() { if (baseHealth != null) baseHealth.Depleted -= flow.GameOver; }
}
}
