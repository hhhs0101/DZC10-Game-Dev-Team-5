using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Cards/Skill")]
public sealed class SkillCardDefinition : CardDefinition {
    [SerializeField] private SkillDefinition skill;
    public SkillDefinition Skill => skill;
    public override CardType Type => CardType.Skill;
}
}
