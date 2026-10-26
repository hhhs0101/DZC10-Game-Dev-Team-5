using UnityEngine;
namespace Defense {
public sealed class FixedIntervalSpawner : MonoBehaviour {
    [SerializeField] private EnemySpawner spawner;
    private EnemyDefinition enemy;
    private EnemyDefinition[] sequence;
    private ResolvedSpawnPlan timedPlan;
    private GameFlow flow;
    private float interval,remaining;
    private int total;
    private bool cancelled;
    public bool UsesJsonPlan { get; private set; }
    public string ConfigurationError { get; private set; }
    public bool HasConfigurationError => !string.IsNullOrEmpty(ConfigurationError);
    public int PlannedSpawnCount => total;
    public event System.Action<Enemy> EnemyScheduled;
    // Raised once per boss just before it spawns; the schedule waits until ResumeAfterBossAnnouncement.
    public event System.Action<EnemyDefinition> BossIncoming;
    private bool awaitingBossAnnouncement;
    private int announcedBossIndex = -1;
    public void ResumeAfterBossAnnouncement() { awaitingBossAnnouncement = false; }
    public int SpawnedCount { get; private set; }
    public bool IsFinishedSpawning => !HasConfigurationError && !cancelled && total > 0 && SpawnedCount >= total;
    private void ResetSchedule() {
        if(flow != null) flow.Changed -= OnStateChanged;
        flow = spawner != null ? spawner.Flow : null;
        if(flow == null) flow = GetComponent<GameFlow>();
        if(flow != null) flow.Changed += OnStateChanged;
        enemy=null;sequence=null;timedPlan=null;remaining=interval=0;total=SpawnedCount=0;
        cancelled=false;UsesJsonPlan=false;ConfigurationError=null;awaitingBossAnnouncement=false;announcedBossIndex=-1;
    }
    // Legacy contract is unchanged: zero total means continuous, interval clamps to 0.1s.
    public void Configure(EnemyDefinition definition,float seconds,int totalEnemies=0) {
        ResetSchedule();enemy=definition;interval=Mathf.Max(.1f,seconds);remaining=interval;total=Mathf.Max(0,totalEnemies);
    }
    public void Configure(StageDefinition stage) => Configure(stage,null);
    // An explicit catalog override supports isolated validation; player stages load one known Resources asset.
    public void Configure(StageDefinition stage,EnemyDefinitionCatalog catalog) {
        if(stage == null) throw new System.ArgumentNullException(nameof(stage));
        if(stage.SpawnPlanJson != null) {
            ResetSchedule();UsesJsonPlan=true;
            if(!SpawnPlanLoader.TryLoad(stage.SpawnPlanJson,stage.DisplayName,catalog!=null?catalog:Resources.Load<EnemyDefinitionCatalog>(EnemyDefinitionCatalog.ResourcePath),out var plan,out var issue)) {Fail(issue);return;}
            if(spawner == null) {Fail("Stage '"+stage.DisplayName+"', spawn JSON '"+stage.SpawnPlanJson.name+"': EnemySpawner reference is missing.");return;}
            if(!spawner.TryValidateSpawnContext(out issue)) {Fail("Stage '"+stage.DisplayName+"', spawn JSON '"+stage.SpawnPlanJson.name+"', stageId '"+plan.StageId+"': "+issue);return;}
            timedPlan=plan;total=plan.Events.Count;remaining=plan.Events[0].Delay;return;
        }
        var entries=stage.SpawnSequence;var copy=new EnemyDefinition[entries.Count];
        for(int i=0;i<entries.Count;i++) {
            if(entries[i]==null)throw new System.ArgumentException("Stage spawn sequence contains an empty Enemy definition.",nameof(stage));
            copy[i]=entries[i];
        }
        Configure(stage.Enemy,stage.SpawnInterval,stage.FiniteSpawning?stage.TotalEnemies:0);
        if(copy.Length>0)sequence=copy;
    }
    private void Fail(string issue) {
        ConfigurationError=issue;timedPlan=null;sequence=null;
        Debug.LogError(issue,this);
        if(flow != null) flow.FailConfiguration(issue);
    }
    private void OnStateChanged(GameplayState state) {
        if(flow != null && flow.IsTerminal && !IsFinishedSpawning) CancelRemaining();
    }
    private void CancelRemaining() {cancelled=true;timedPlan=null;sequence=null;}
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime) {
        if(cancelled || HasConfigurationError || IsFinishedSpawning || awaitingBossAnnouncement || spawner==null || !spawner.CanSpawn || deltaTime<=0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))return;
        remaining-=deltaTime;if(remaining>0)return;
        if(UsesJsonPlan) {
            while(!IsFinishedSpawning && !cancelled && spawner.CanSpawn) {
                var plan=timedPlan;var next=plan.Events[SpawnedCount];
                if(next.Enemy.Role==EnemyRole.Boss && BossIncoming!=null && announcedBossIndex!=SpawnedCount) {
                    announcedBossIndex=SpawnedCount;awaitingBossAnnouncement=true;BossIncoming(next.Enemy);return;
                }
                var spawned=spawner.Spawn(next.Enemy);
                if(spawned==null) {Fail("Spawn JSON stageId '"+plan.StageId+"', event ["+SpawnedCount+"] enemyId '"+next.Enemy.EnemyTypeId+"' could not spawn.");return;}
                SpawnedCount++;bool last=SpawnedCount>=total;
                // Positive delays start at the preceding successful spawn; no overshoot is borrowed.
                remaining=last?0:plan.Events[SpawnedCount].Delay;
                EnemyScheduled?.Invoke(spawned);
                if(last || cancelled || !spawner.CanSpawn || remaining>0)return;
            }
            return;
        }
        var definition=sequence!=null?sequence[SpawnedCount]:enemy;
        if(definition==null)return;
        var actor=spawner.Spawn(definition);
        if(actor!=null) {SpawnedCount++;remaining+=interval;EnemyScheduled?.Invoke(actor);}
    }
    private void OnDestroy() {if(flow!=null)flow.Changed-=OnStateChanged;CancelRemaining();}
}
}
