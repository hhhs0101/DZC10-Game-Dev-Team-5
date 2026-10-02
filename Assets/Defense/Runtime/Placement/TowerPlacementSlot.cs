using System;
using UnityEngine;
namespace Defense {
// Owns slot state only; a removable view observes Changed.
public sealed class TowerPlacementSlot : MonoBehaviour {
    public Turret Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;
    public bool IsHovered { get; private set; }
    public event Action Changed;
    public void SetHovered(bool value) { IsHovered = value; Changed?.Invoke(); }
    public bool TryOccupy(Turret turret) {
        if(IsOccupied || turret == null) return false;
        Occupant = turret; Changed?.Invoke(); return true;
    }
}
}
