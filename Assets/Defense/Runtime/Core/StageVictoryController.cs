using System;
using UnityEngine;
namespace Defense {
public sealed class StageVictoryController : MonoBehaviour {
    private GameFlow flow;
    private FixedIntervalSpawner schedule;
    private EnemyRegistry enemies;
    private StageDefinition stage;
    private PlayerCurrency currency;
    private Action completeProgression;
    private bool processed;
    public int GoldEarned { get; private set; }
    public void Initialize(GameFlow gameFlow, FixedIntervalSpawner spawning, EnemyRegistry registry,
        StageDefinition definition, PlayerCurrency playerCurrency, Action completeStage) {
        flow = gameFlow; schedule = spawning; enemies = registry; stage = definition;
        currency = playerCurrency; completeProgression = completeStage;
        processed = false; GoldEarned = 0;
    }
    // Evaluate after combat callbacks finish: the last Base arrival must apply lethal damage first.
    // Registry owns the population; no scene searches or separate kill counters are needed.
    private void LateUpdate() => EvaluateClear();
    public bool EvaluateClear() {
        if (processed || flow == null || !flow.IsPlaying || !schedule.IsFinishedSpawning || enemies.AliveEnemyCount != 0) return false;
        processed = true;
        GoldEarned = stage.ClearGoldReward <= int.MaxValue-currency.Gold ? stage.ClearGoldReward : 0;
        if (GoldEarned != stage.ClearGoldReward) Debug.LogWarning("Clear Gold could not be credited because the currency limit was reached.");
        if (!flow.TryVictory()) return false;
        completeProgression?.Invoke();
        if (!currency.AddGold(GoldEarned)) {
            GoldEarned = 0;
            Debug.LogWarning("Clear Gold could not be credited because the currency limit was reached.");
        }
        return true;
    }
}
}
