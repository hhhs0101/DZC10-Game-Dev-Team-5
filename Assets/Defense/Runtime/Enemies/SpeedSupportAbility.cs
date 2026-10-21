using System.Collections.Generic;
namespace Defense {
public sealed class SpeedSupportAbility : SupportEnemyAbility {
    private readonly HashSet<Enemy> affected = new HashSet<Enemy>();
    public int AffectedCount => affected.Count;
    protected override void OnActivated() {
        Registry.Registered += RefreshEnemy; Registry.Moved += OnMoved; Registry.Unregistered += RemoveEnemy;
        RefreshMembership();
    }
    // Movement notifications make entering/leaving and movement of the aura itself immediate.
    private void OnMoved(Enemy enemy) { if(enemy == Owner) RefreshMembership(); else RefreshEnemy(enemy); }
    public void RefreshMembership() { if(IsOperational) foreach(var enemy in Registry.Enemies) RefreshEnemy(enemy); }
    private void RefreshEnemy(Enemy enemy) {
        if(Protects(enemy)) { affected.Add(enemy); enemy.Movement.Modifiers.SetSpeedBoost(this,Data.SpeedIncrease); }
        else RemoveEnemy(enemy);
    }
    private void RemoveEnemy(Enemy enemy) {
        if(affected.Remove(enemy) && enemy != null) enemy.Movement.Modifiers.Remove(this);
    }
    protected override void OnDeactivated() {
        if(Registry != null) { Registry.Registered -= RefreshEnemy; Registry.Moved -= OnMoved; Registry.Unregistered -= RemoveEnemy; }
        foreach(var enemy in affected) if(enemy != null) enemy.Movement.Modifiers.Remove(this);
        affected.Clear();
    }
}
}
