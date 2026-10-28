using System;
using System.Collections.Generic;
namespace Defense {
public sealed class StageProgressionState {
    private readonly bool[] completed,unlocked,alwaysUnlocked;
    private readonly string[] stageIds;
    private readonly StageProgressionPersistence persistence;
    // Keep unknown IDs so temporarily removed content can recover its history later.
    private readonly HashSet<string> completedIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> unlockedIds = new HashSet<string>(StringComparer.Ordinal);
    public bool LastSaveSucceeded { get; private set; } = true;
    public string SavePath => persistence?.SavePath;
    public event Action Changed;
    public StageProgressionState(int count) {
        if(count<0)throw new ArgumentOutOfRangeException(nameof(count));
        completed=new bool[count];unlocked=new bool[count];alwaysUnlocked=new bool[count];
    }
    // Storage is optional, preserving existing in-memory fixtures and pure domain callers.
    public StageProgressionState(IReadOnlyList<StageDefinition> stages,StageProgressionPersistence storage = null) : this(stages?.Count ?? throw new ArgumentNullException(nameof(stages))) {
        persistence=storage;stageIds=new string[stages.Count];var unique=new HashSet<string>(StringComparer.Ordinal);
        for(int i=0;i<stages.Count;i++) {
            if(stages[i]==null)throw new ArgumentException("Progression contains a missing stage.",nameof(stages));
            string id=stages[i].StageId;
            if(string.IsNullOrWhiteSpace(id)||!unique.Add(id))throw new ArgumentException("Stage progression IDs must be nonempty and unique: "+id,nameof(stages));
            stageIds[i]=id;alwaysUnlocked[i]=stages[i].AlwaysUnlocked;
        }
        var data=persistence?.Load();
        if(data!=null){ReadIds(data.CompletedStageIds,completedIds);ReadIds(data.UnlockedStageIds,unlockedIds);}
        for(int i=0;i<stages.Count;i++) {
            completed[i]=completedIds.Contains(stageIds[i]);unlocked[i]=unlockedIds.Contains(stageIds[i]);
        }
        for(int i=0;i<stages.Count;i++)if(completed[i])UnlockAfter(i);
        LastSaveSucceeded=persistence==null||persistence.CanWrite;
    }
    private static void ReadIds(string[] values,HashSet<string> target) {
        if(values!=null)foreach(var id in values)if(!string.IsNullOrWhiteSpace(id))target.Add(id);
    }
    public bool IsUnlocked(int index) => index>=0&&index<completed.Length&&(alwaysUnlocked[index]||index==0||completed[index]||unlocked[index]||(index>0&&completed[index-1]));
    public bool IsComplete(int index) => index>=0&&index<completed.Length&&completed[index];
    public bool Complete(int index) {
        if(!IsUnlocked(index))return false;
        if(completed[index]){if(!LastSaveSucceeded)Save();return false;}
        completed[index]=true;
        if(stageIds!=null)completedIds.Add(stageIds[index]);
        UnlockAfter(index);Save();Changed?.Invoke();return true;
    }
    private void UnlockAfter(int index) {
        unlocked[index]=true;if(stageIds!=null)unlockedIds.Add(stageIds[index]);
        if(index+1<unlocked.Length){unlocked[index+1]=true;if(stageIds!=null)unlockedIds.Add(stageIds[index+1]);}
    }
    private void Save() {
        if(persistence==null)return;
        if(stageIds.Length>0)unlockedIds.Add(stageIds[0]);
        var clear=new List<string>(completedIds);clear.Sort(StringComparer.Ordinal);
        var open=new List<string>(unlockedIds);open.Sort(StringComparer.Ordinal);
        LastSaveSucceeded=persistence.Save(new StageProgressionSaveData {Version=StageProgressionPersistence.SaveVersion,CompletedStageIds=clear.ToArray(),UnlockedStageIds=open.ToArray()});
    }
}
}
