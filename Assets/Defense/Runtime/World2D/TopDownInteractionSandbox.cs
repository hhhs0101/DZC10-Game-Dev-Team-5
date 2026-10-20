using UnityEngine;
namespace Defense {
// Stage 2 local fixture only. Stage 3 replaces this bootstrap with real session/stage integration.
[DefaultExecutionOrder(-100)]
public sealed class TopDownInteractionSandbox : MonoBehaviour {
    [SerializeField] private StageDefinition testStage;
    [SerializeField] private CardDefinition[] testDeck;
    public StageDefinition TestStage => testStage;
    public PlayerDeck Deck { get; private set; }
    private void Awake() {
        var flow=GetComponent<GameFlow>();var health=GetComponent<BaseHealth>();
        GetComponent<ResourceWallet>().Initialize(testStage.StartingResources);
        health.Initialize(testStage.BaseHealth);health.Depleted+=flow.GameOver;
        GetComponent<ElixirSystem>().Initialize(flow);
        Deck=new PlayerDeck(testDeck);
        GetComponent<GameplayHand>().Initialize(Deck,GetComponent<TurretPlacementController>(),flow);
        GetComponent<SkillCastingController>().Initialize(GetComponent<GameplayHand>(),flow,GetComponent<EnemyRegistry>(),Camera.main);
        GetComponent<FixedIntervalSpawner>().Configure(testStage.Enemy,testStage.SpawnInterval);
    }
    private void OnDestroy() {GetComponent<BaseHealth>().Depleted-=GetComponent<GameFlow>().GameOver;}
}
}
