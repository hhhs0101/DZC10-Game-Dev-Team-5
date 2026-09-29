using System;
namespace Defense {
public sealed class StageProgressionState {
    private readonly bool[] completed;
    public event Action Changed;
    public StageProgressionState(int count) { completed = new bool[count]; }
    public bool IsUnlocked(int index) => index >= 0 && index < completed.Length && (index == 0 || completed[index-1]);
    public bool IsComplete(int index) => index >= 0 && index < completed.Length && completed[index];
    public bool Complete(int index) {
        if (!IsUnlocked(index) || completed[index]) return false;
        completed[index] = true; Changed?.Invoke(); return true;
    }
}
}
