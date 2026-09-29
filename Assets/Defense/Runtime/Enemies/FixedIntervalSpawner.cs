using UnityEngine;
namespace Defense {
public sealed class FixedIntervalSpawner : MonoBehaviour {
    [SerializeField] private EnemySpawner spawner;
    private EnemyDefinition enemy;
    private float interval;
    private float remaining;
    public void Configure(EnemyDefinition definition, float seconds) {
        enemy = definition; interval = Mathf.Max(0.1f, seconds); remaining = interval;
    }
    private void Update() {
        if (enemy == null || Time.deltaTime <= 0) return;
        remaining -= Time.deltaTime;
        if (remaining > 0) return;
        spawner.Spawn(enemy); remaining += interval;
    }
}
}
