using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class EnemyRegistry : MonoBehaviour {
    [SerializeField] private WaypointPlane plane;
    public WaypointPlane Plane => plane;
    private readonly List<Enemy> enemies = new List<Enemy>();
    // Stable activation order; each finite pool is offered the remainder once, never recursively.
    private readonly List<ShieldSupportAbility> shields = new List<ShieldSupportAbility>();
    internal void RegisterShield(ShieldSupportAbility shield) { if(!shields.Contains(shield)) shields.Add(shield); }
    internal void UnregisterShield(ShieldSupportAbility shield) => shields.Remove(shield);
    internal float ResolveDamage(Enemy target,float amount) {
        if(amount <= 0 || float.IsNaN(amount)) return 0;
        foreach(var shield in shields) {
            if(shield != null) amount = shield.AbsorbDamage(target,amount);
            if(amount <= 0) break;
        }
        return amount;
    }
    public IReadOnlyList<Enemy> Enemies => enemies;
    public int AliveEnemyCount => enemies.Count;
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
