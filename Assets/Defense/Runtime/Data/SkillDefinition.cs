using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Skill Definition")]
public sealed class SkillDefinition : ScriptableObject {
    [SerializeField, Min(0)] private float damage = 40;
    [SerializeField, Min(.01f)] private float radius = 1.5f;
    [SerializeField, Min(0)] private float castingTime;
    [SerializeField] private Skill effect;
    public float Damage => damage;
    public float Radius => radius;
    public float CastingTime => castingTime;
    public Skill Effect => effect;
}
}
