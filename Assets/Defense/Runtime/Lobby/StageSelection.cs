using System;
namespace Defense {
public sealed class StageSelection {
    private readonly int count;
    public int SelectedStageIndex { get; private set; }
    public event Action Changed;
    public StageSelection(int count) { this.count = count; }
    public bool Select(int index) {
        if(index < 0 || index >= count || index == SelectedStageIndex) return false;
        SelectedStageIndex = index; Changed?.Invoke(); return true;
    }
    public bool MovePrevious() => Move(-1);
    public bool MoveNext() => Move(1);
    public bool Move(int direction) {
        int next = Math.Max(0,Math.Min(count-1,SelectedStageIndex + Math.Sign(direction)));
        if (next == SelectedStageIndex) return false;
        SelectedStageIndex = next; Changed?.Invoke(); return true;
    }
}
}
