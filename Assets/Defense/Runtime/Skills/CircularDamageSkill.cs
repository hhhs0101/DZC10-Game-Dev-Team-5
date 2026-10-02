using System.Collections.Generic;
using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Skills/Circular Damage")]
public sealed class CircularDamageSkill : Skill {
    public override void Apply(SkillDefinition definition, Vector2 position, EnemyRegistry registry) {
        // Death unregisters synchronously, so iterate a snapshot.
        var enemies = new List<Enemy>(registry.Enemies);
        foreach (var enemy in enemies)
            if (enemy != null && enemy.IsAlive && (PlanarSpace.Project(enemy.transform.position)-position).sqrMagnitude <= definition.Radius*definition.Radius)
                enemy.ReceiveDamage(definition.Damage);
    }
}
}
