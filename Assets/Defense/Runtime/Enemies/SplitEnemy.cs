using System.Collections.Generic;
namespace Defense {
// Retain the serialized component identity. Corn now has layered health instead of children.
public sealed class SplitEnemy : Enemy {
    public List<Enemy> Split() => new List<Enemy>();
}
}
