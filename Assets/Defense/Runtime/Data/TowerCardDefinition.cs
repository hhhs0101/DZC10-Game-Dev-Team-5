using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Cards/Tower")]
public sealed class TowerCardDefinition : CardDefinition {
    [SerializeField] private TurretDefinition tower;
    public TurretDefinition Tower => tower;
    public override CardType Type => CardType.Tower;
}
}
