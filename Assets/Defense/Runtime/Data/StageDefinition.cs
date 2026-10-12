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
    [Header("Finite stage and rewards")]
    [SerializeField] private bool finiteSpawning;
    [SerializeField, Min(1)] private int totalEnemies = 5;
    [SerializeField, Min(0)] private int clearGoldReward = 50;
    [SerializeField, Tooltip("Allows development stages to be selected without earlier campaign clears.")] private bool alwaysUnlocked;
    [SerializeField] private TurretDefinition[] availableTurrets;
    public bool FiniteSpawning => finiteSpawning;
    public int TotalEnemies => Mathf.Max(1,totalEnemies);
    public int ClearGoldReward => Mathf.Max(0,clearGoldReward);
    public bool AlwaysUnlocked => alwaysUnlocked;
    public string DisplayName => displayName;
    public string SceneName => sceneName;
    public int BaseHealth => baseHealth;
    public int StartingResources => startingResources;
    public EnemyDefinition Enemy => enemy;
    public float SpawnInterval => spawnInterval;
    public System.Collections.Generic.IReadOnlyList<TurretDefinition> AvailableTurrets => availableTurrets;
}
}
