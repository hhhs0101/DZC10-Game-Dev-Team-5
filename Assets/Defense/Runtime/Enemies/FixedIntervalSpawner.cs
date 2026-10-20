using UnityEngine;
namespace Defense {
public sealed class FixedIntervalSpawner : MonoBehaviour {
    [SerializeField] private EnemySpawner spawner;
    private EnemyDefinition enemy;
    private EnemyDefinition[] sequence;
    private float interval;
    private float remaining;
    private int total;
    // Observers may count scheduled combat deaths separately from diagnostic spawns.
    public event System.Action<Enemy> EnemyScheduled;
    public int SpawnedCount { get; private set; }
    public bool IsFinishedSpawning => total > 0 && SpawnedCount >= total;
    // Zero preserves the existing continuous development schedule.
    public void Configure(EnemyDefinition definition, float seconds, int totalEnemies = 0) {
        sequence = null;
        enemy = definition; interval = Mathf.Max(.1f,seconds); remaining = interval;
        total = Mathf.Max(0,totalEnemies); SpawnedCount = 0;
    }
    // Snapshot stage content once per run; advancing/retrying never mutates authored data.
    public void Configure(StageDefinition stage) {
        if (stage == null) throw new System.ArgumentNullException(nameof(stage));
        var entries = stage.SpawnSequence;
        var copy = new EnemyDefinition[entries.Count];
        for (int i = 0; i < entries.Count; i++) {
            if (entries[i] == null) throw new System.ArgumentException("Stage spawn sequence contains an empty Enemy definition.", nameof(stage));
            copy[i] = entries[i];
        }
        Configure(stage.Enemy, stage.SpawnInterval, stage.FiniteSpawning ? stage.TotalEnemies : 0);
        if (copy.Length > 0) sequence = copy;
    }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime) {
        if (IsFinishedSpawning) return;
        var next = sequence != null ? sequence[SpawnedCount] : enemy;
        if (next == null || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || !spawner.CanSpawn || IsFinishedSpawning) return;
        remaining -= deltaTime;
        if (remaining > 0) return;
        // Count successful spawns, not requests rejected by the executor.
        var spawned = spawner.Spawn(next);
        if (spawned != null) { SpawnedCount++; remaining += interval; EnemyScheduled?.Invoke(spawned); }
    }
}
}
