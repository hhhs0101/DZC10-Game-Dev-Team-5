using UnityEngine;
namespace Defense {
// Stateless effect strategy. Runtime casting state belongs to SkillCastingController.
public abstract class Skill : ScriptableObject {
    public abstract void Apply(SkillDefinition definition, Vector2 position, EnemyRegistry registry);
}
}
