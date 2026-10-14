using UnityEngine;
namespace Defense {
public sealed class FixedIntervalSpawner : MonoBehaviour {
    [SerializeField] private EnemySpawner spawner;
    private EnemyDefinition enemy;
    private float interval;
    private float remaining;
    private int total;
    public int SpawnedCount { get; private set; }
    public bool IsFinishedSpawning => total > 0 && SpawnedCount >= total;
    // Zero preserves the existing continuous development schedule.
    public void Configure(EnemyDefinition definition, float seconds, int totalEnemies = 0) {
        enemy = definition; interval = Mathf.Max(.1f,seconds); remaining = interval;
        total = Mathf.Max(0,totalEnemies); SpawnedCount = 0;
    }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime) {
        if (enemy == null || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || !spawner.CanSpawn || IsFinishedSpawning) return;
        remaining -= deltaTime;
        if (remaining > 0) return;
        // Count successful spawns, not requests rejected by the executor.
        if (spawner.Spawn(enemy) != null) { SpawnedCount++; remaining += interval; }
    }
}
}
