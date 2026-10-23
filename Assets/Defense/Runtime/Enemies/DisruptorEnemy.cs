namespace Defense {
public sealed class DisruptorEnemy : Enemy {
    public bool SabotageTower() => GetComponent<TowerDisruptionAbility>()?.TryDisruptRandom() ?? false;
}
}
