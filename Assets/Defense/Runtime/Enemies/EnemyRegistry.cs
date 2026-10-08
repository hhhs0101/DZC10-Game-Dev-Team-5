using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class EnemyRegistry : MonoBehaviour {
    private readonly List<Enemy> enemies = new List<Enemy>();
    public IReadOnlyList<Enemy> Enemies => enemies;
    public event Action<Enemy> Registered;
    public event Action<Enemy> Unregistered;
    public event Action<Enemy> Moved;
    public void Register(Enemy enemy) {
        if (enemies.Contains(enemy)) return;
        enemies.Add(enemy); Registered?.Invoke(enemy);
    }
    public void Unregister(Enemy enemy) { if (enemies.Remove(enemy)) Unregistered?.Invoke(enemy); }
    public void NotifyMoved(Enemy enemy) => Moved?.Invoke(enemy);
}
}
