namespace Defense {
public sealed class SpeedSupportEnemy : SupportEnemy {
    public void BuffEnemies() => GetComponent<SpeedSupportAbility>()?.RefreshMembership();
}
}
