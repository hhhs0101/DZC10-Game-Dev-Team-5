using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// Planar membership deliberately ignores visual height and does not depend on a trigger or mesh.
[DefaultExecutionOrder(-50)]
public sealed class PersistentSlowArea : MonoBehaviour {
    private SkillDefinition definition;
    private EnemyRegistry registry;
    private GameFlow flow;
    private float elapsed;
    private bool subscribed;
    private readonly HashSet<Enemy> affected = new HashSet<Enemy>();
    public bool IsActive { get; private set; }
    public float Radius => definition != null ? definition.Radius : 0;
    public float VisualOpacity { get; private set; }
    public int AffectedCount => affected.Count;
    public event Action Changed;
    public void Initialize(SkillDefinition data, SkillCastContext context) {
        definition = data; registry = context.Registry; flow = context.Flow;
        transform.position = context.TargetPosition;
        elapsed = 0; VisualOpacity = 1; IsActive = data.ActiveDuration > 0;
        registry.Registered += RefreshEnemy; registry.Moved += RefreshEnemy; registry.Unregistered += RemoveEnemy;
        subscribed = true;
        if (IsActive) RefreshMembership(); else EndEffect();
    }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime) {
        if (definition == null || flow == null || !flow.IsPlaying || deltaTime <= 0) return;
        elapsed += deltaTime;
        if (IsActive && elapsed >= definition.ActiveDuration) EndEffect();
        if (IsActive) RefreshMembership();
        else {
            float fadeElapsed = Mathf.Max(0,elapsed-definition.ActiveDuration);
            VisualOpacity = definition.FadeDuration > 0 ? Mathf.Clamp01(1-fadeElapsed/definition.FadeDuration) : 0;
            if (VisualOpacity <= 0) Destroy(gameObject);
        }
        Changed?.Invoke();
    }
    public void RefreshMembership() {
        if (!IsActive) return;
        foreach (var enemy in registry.Enemies) RefreshEnemy(enemy);
    }
    private void RefreshEnemy(Enemy enemy) {
        if (enemy == null) return;
        bool inside = IsActive && enemy.IsAlive &&
            PlanarSpace.SqrDistance(enemy.transform.position,transform.position) <= Radius*Radius;
        if (inside) {
            affected.Add(enemy); enemy.Movement.Modifiers.SetSlow(this,definition.SlowMultiplier);
        } else RemoveEnemy(enemy);
    }
    private void RemoveEnemy(Enemy enemy) {
        if (!affected.Remove(enemy)) return;
        if (enemy != null && enemy.Movement != null) enemy.Movement.Modifiers.Remove(this);
    }
    private void EndEffect() {
        IsActive = false;
        foreach (var enemy in affected) if (enemy != null && enemy.Movement != null) enemy.Movement.Modifiers.Remove(this);
        affected.Clear();
        if (subscribed && registry != null) {
            registry.Registered -= RefreshEnemy; registry.Moved -= RefreshEnemy; registry.Unregistered -= RemoveEnemy;
        }
        subscribed = false;
    }
    private void OnDisable() { EndEffect(); VisualOpacity = 0; Changed?.Invoke(); }
    private void OnDestroy() => EndEffect();
}
}
