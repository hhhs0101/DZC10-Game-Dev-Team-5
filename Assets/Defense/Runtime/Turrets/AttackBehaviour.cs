using UnityEngine;
namespace Defense {
public abstract class AttackBehaviour : MonoBehaviour {
    public abstract void Attack(CombatTarget target, float damage);
}
}
