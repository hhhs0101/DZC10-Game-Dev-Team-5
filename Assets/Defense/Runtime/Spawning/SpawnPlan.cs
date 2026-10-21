using System;
using System.Collections.Generic;
namespace Defense {
[Serializable]
public sealed class SpawnPlanData {
    public string stageId;
    public SpawnEventData[] spawns;
}
[Serializable]
public sealed class SpawnEventData {
    public string enemyId;
    public float delay;
}
public readonly struct ResolvedSpawnEvent {
    public EnemyDefinition Enemy { get; }
    public float Delay { get; }
    public ResolvedSpawnEvent(EnemyDefinition enemy,float delay) {Enemy=enemy;Delay=delay;}
}
// Fresh, read-only snapshot per run. Editing a TextAsset never mutates an active schedule.
public sealed class ResolvedSpawnPlan {
    public string StageId { get; }
    public IReadOnlyList<ResolvedSpawnEvent> Events { get; }
    public ResolvedSpawnPlan(string stageId,ResolvedSpawnEvent[] events) {
        StageId=stageId;Events=Array.AsReadOnly((ResolvedSpawnEvent[])events.Clone());
    }
}
}
