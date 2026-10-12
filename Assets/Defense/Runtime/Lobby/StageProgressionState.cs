using System;
namespace Defense {
public sealed class StageProgressionState {
    private readonly bool[] completed;
    private bool[] alwaysUnlocked;
    public event Action Changed;
    public StageProgressionState(int count) { completed = new bool[count]; }
    public StageProgressionState(System.Collections.Generic.IReadOnlyList<StageDefinition> stages) : this(stages.Count) {
        alwaysUnlocked = new bool[stages.Count];
        for (int i=0;i<stages.Count;i++) alwaysUnlocked[i] = stages[i].AlwaysUnlocked;
    }
    public bool IsUnlocked(int index) => index >= 0 && index < completed.Length && ((alwaysUnlocked != null && alwaysUnlocked[index]) || index == 0 || completed[index-1]);
    public bool IsComplete(int index) => index >= 0 && index < completed.Length && completed[index];
    public bool Complete(int index) {
        if (!IsUnlocked(index) || completed[index]) return false;
        completed[index] = true; Changed?.Invoke(); return true;
    }
}
}
