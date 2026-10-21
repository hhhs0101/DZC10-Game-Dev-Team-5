namespace Defense {
// Conceptual SupportEnemy role: abilities compose an Enemy instead of duplicating its health/movement.
public abstract class SupportEnemyAbility : EnemyRoleAbility {
    public float Radius => Data != null ? Data.SupportArea : 0;
    protected bool Protects(Enemy enemy) => IsOperational && enemy != null && enemy != Owner && enemy.IsAlive &&
        PlanarSpace.SqrDistance(Owner.transform.position,enemy.transform.position,WaypointPlane.XY) <= Radius*Radius;
}
}
