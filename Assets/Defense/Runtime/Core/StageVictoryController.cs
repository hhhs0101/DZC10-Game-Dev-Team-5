using System;
using System.Collections.Generic;
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
    private readonly HashSet<Enemy> pendingDefeats = new HashSet<Enemy>();
    public int DefeatedCount { get; private set; }
    public int GoldEarned { get; private set; }
    public void Initialize(GameFlow gameFlow, FixedIntervalSpawner spawning, EnemyRegistry registry,
        StageDefinition definition, PlayerCurrency playerCurrency, Action completeStage, bool trackScheduledDefeats = false) {
        Unsubscribe();
        flow = gameFlow; schedule = spawning; enemies = registry; stage = definition;
        currency = playerCurrency; completeProgression = completeStage;
        processed = false; GoldEarned = 0; DefeatedCount = 0;
        if (trackScheduledDefeats) schedule.EnemyScheduled += TrackScheduledEnemy;
    }
    // Evaluate after combat callbacks finish: the last Base arrival must apply lethal damage first.
    // Registry owns clear eligibility. Optional defeat statistics never decide loss or clear.
    private void LateUpdate() => EvaluateClear();
    public bool EvaluateClear() {
        if (processed || flow == null || !flow.IsPlaying || !schedule.IsFinishedSpawning || enemies.AliveEnemyCount != 0) return false;
        processed = true;
        GoldEarned = 0;
        if (!flow.TryVictory()) return false;
        completeProgression?.Invoke();
        return true;
    }
    private void TrackScheduledEnemy(Enemy enemy) {
        if (pendingDefeats.Add(enemy)) enemy.Died += OnScheduledDefeated;
    }
    private void OnScheduledDefeated(Enemy enemy) {
        if (!pendingDefeats.Remove(enemy)) return;
        enemy.Died -= OnScheduledDefeated;
        DefeatedCount++;
    }
    private void Unsubscribe() {
        if (schedule != null) schedule.EnemyScheduled -= TrackScheduledEnemy;
        foreach (var enemy in pendingDefeats) if (enemy != null) enemy.Died -= OnScheduledDefeated;
        pendingDefeats.Clear();
    }
    private void OnDestroy() => Unsubscribe();
}
}
