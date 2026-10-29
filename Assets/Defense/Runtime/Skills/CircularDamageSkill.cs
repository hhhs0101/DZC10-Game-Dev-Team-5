using System.Collections.Generic;
using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Skills/Area Damage")]
public sealed class CircularDamageSkill : Skill {
    public override void Apply(SkillDefinition definition, SkillCastContext context) {
        var registry = context.Registry; var position = PlanarSpace.Project(context.TargetPosition,context.Plane);
        var footprint = definition.CreateFootprint(position);
        // Death unregisters synchronously, so iterate a snapshot.
        var enemies = new List<Enemy>(registry.Enemies);
        foreach (var enemy in enemies)
            if (enemy != null && enemy.IsAlive && footprint.Contains(PlanarSpace.Project(enemy.transform.position,context.Plane)))
                enemy.ReceiveSplashDamage(definition.Damage);
    }
}
}
