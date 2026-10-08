using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Skills/Persistent Slow Area")]
public sealed class SlowAreaSkill : Skill {
    public override void Apply(SkillDefinition definition, SkillCastContext context) {
        var root = new GameObject("Persistent Slow Area");
        root.transform.SetParent(context.Registry.transform,false);
        var area = root.AddComponent<PersistentSlowArea>();
        area.Initialize(definition,context);
        var visual = new GameObject("Visual"); visual.transform.SetParent(root.transform,false);
        visual.AddComponent<SkillAreaVisual>().Observe(area);
    }
}
}
