using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class EnemyRegistry : MonoBehaviour {
    private readonly List<Enemy> enemies = new List<Enemy>();
    public IReadOnlyList<Enemy> Enemies => enemies;
    public void Register(Enemy enemy) { if (!enemies.Contains(enemy)) enemies.Add(enemy); }
    public void Unregister(Enemy enemy) { enemies.Remove(enemy); }
}
}
