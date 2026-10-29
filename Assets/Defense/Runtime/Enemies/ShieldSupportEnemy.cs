namespace Defense {
public sealed class ShieldSupportEnemy : SupportEnemy {
    // Refresh the existing aura; protection is evaluated when damage arrives.
    public void ShieldEnemies() => GetComponent<ShieldSupportAbility>()?.ShieldEnemies();
}
}
