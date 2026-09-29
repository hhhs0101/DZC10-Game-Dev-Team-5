using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Stage Definition")]
public sealed class StageDefinition : ScriptableObject {
    [SerializeField] private string displayName = "Test Stage";
    [SerializeField, Tooltip("Scene name included in Build Settings.")] private string sceneName = "TestStage";
    [SerializeField, Min(1)] private int baseHealth = 10;
    [SerializeField, Min(0)] private int startingResources = 150;
    [SerializeField] private EnemyDefinition enemy;
    [SerializeField, Min(0.1f)] private float spawnInterval = 2;
    [SerializeField] private TurretDefinition[] availableTurrets;
    public string DisplayName => displayName;
    public string SceneName => sceneName;
    public int BaseHealth => baseHealth;
    public int StartingResources => startingResources;
    public EnemyDefinition Enemy => enemy;
    public float SpawnInterval => spawnInterval;
    public System.Collections.Generic.IReadOnlyList<TurretDefinition> AvailableTurrets => availableTurrets;
}
}
