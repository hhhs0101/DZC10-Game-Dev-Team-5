using System.Collections.Generic;
namespace Defense {
public abstract class SupportEnemy : Enemy {
    public List<Enemy> GetNearbyEnemies(float radius) {
        var result = new List<Enemy>();
        foreach(var enemy in Registry.Enemies)
            if(enemy.IsAlive && PlanarSpace.SqrDistance(transform.position,enemy.transform.position,Registry.Plane) <= radius*radius) result.Add(enemy);
        return result;
    }
}
}
