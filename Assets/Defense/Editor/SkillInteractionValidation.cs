using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Defense.Editor {
public static class SkillInteractionValidation {
    private static PersistentSlowArea expiredArea;
    public static void VerifyNextFrame(Action<bool,string> check) => check(expiredArea==null,"Expired area is actually destroyed after the frame, including its visual");
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Set(UnityEngine.Object target,string field,float value) {
        var data = new SerializedObject(target); data.FindProperty(field).floatValue=value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Select(GameplayHand hand,CardDefinition card) {
        while (!hand.Cycle.Hand.Any(c=>c.Definition==card)) hand.Cycle.Use(hand.Cycle.Hand[0]);
        hand.Select(hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card));
    }
    private static void ClearAreas() {
        foreach (var area in UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(area.gameObject);
    }
    public static void Run(Action<bool,string> check) {
        ClearAreas();
        var hand=Find<GameplayHand>(); var placement=Find<TurretPlacementController>(); var flow=Find<GameFlow>();
        var casting=Find<SkillCastingController>(); var elixir=Find<ElixirSystem>(); var registry=Find<EnemyRegistry>();
        var surface=Find<TabletopSurface>(); var camera=Camera.main; var spawner=Find<EnemySpawner>();
        var cards=PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>().ToArray();
        var toy=cards.Single(c=>c.DisplayName=="Toy Throw"); var slow=cards.Single(c=>c.DisplayName=="Slow Area");
        check(toy.Skill.Effect is CircularDamageSkill && slow.Skill.Effect is SlowAreaSkill && slow.Skill.Damage==0,"Toy Throw reuses AoE strategy; Slow Area uses a distinct zero-damage strategy");
        check(slow.Skill.ActiveDuration==8 && slow.Skill.FadeDuration==.5f && slow.Skill.SlowMultiplier==.5f && toy.Skill.TravelTime==0,"Development duration, fade, slow strength and travel values configured");
        hand.Initialize(new PlayerDeck(PlayerSession.Catalog.InitialDeck.Take(4).Concat(cards).ToArray()),placement,flow);
        Vector2 point=camera.WorldToScreenPoint(PlanarSpace.World(Vector2.zero,surface.Height));
        Vector2 movedPoint=camera.WorldToScreenPoint(PlanarSpace.World(new Vector2(1,1),surface.Height));
        Physics.SyncTransforms(); Canvas.ForceUpdateCanvases();
        check(casting.TryGetTabletopPoint(point,out var hit) && (hit-PlanarSpace.World(Vector2.zero,surface.Height)).sqrMagnitude<.0001f,"Camera screen ray resolves the real tabletop at XZ zero");
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube); var ray=camera.ScreenPointToRay(point); blocker.transform.position=ray.GetPoint(5);
        foreach (var layer in new[]{"Default","Enemy","PlacementSlot"}) {
            blocker.layer=LayerMask.NameToLayer(layer); Physics.SyncTransforms();
            check(casting.TryGetTabletopPoint(point,out hit) && PlanarSpace.Project(hit).sqrMagnitude<.0001f,"Table targeting ignores nearer "+layer+" geometry");
        }
        UnityEngine.Object.DestroyImmediate(blocker);
        check(!casting.TryGetTabletopPoint(new Vector2(-100,-100),out _) && !casting.TryGetTabletopPoint(camera.WorldToScreenPoint(new Vector3(40,0,0)),out _),"Offscreen and outside-table rays reject");
        foreach (var card in cards) {
            elixir.Initialize(flow); Select(hand,card);
            var used=hand.SelectedCard; var before=hand.Cycle.Hand.ToArray(); var queue=hand.Cycle.UpcomingQueue.ToArray(); float funds=elixir.Current;
            check(!casting.IsAiming && placement.Selected==null,"Skill selection alone does not aim, cast, or activate placement");
            casting.ProcessPointer(point,true,false);
            check(casting.IsAiming && casting.IndicatorRadius==card.Skill.Radius,"LMB down through the real camera ray begins skill aim");
            casting.ProcessPointer(movedPoint,false,false);
            var line=casting.GetComponentInChildren<LineRenderer>();
            check((casting.AimPosition-Vector2.one).sqrMagnitude<.0001f && Mathf.Abs(line.GetPosition(0).y-surface.Height-.06f)<.001f && Mathf.Abs(Vector2.Distance(PlanarSpace.Project(line.GetPosition(0)),casting.AimPosition)-card.Skill.Radius)<.001f,"Cursor moves ring on tabletop using the sole configured Radius");
            Canvas.ForceUpdateCanvases();
            var cardRect=GameObject.Find("Hand Card "+(hand.Cycle.Hand.ToList().IndexOf(used)+1)).transform;
            Vector2 cardPoint=RectTransformUtility.WorldToScreenPoint(null,cardRect.position);
            check(casting.ProcessPointer(cardPoint,false,true)==CardUseResult.Cancelled && !casting.IsAiming && !line.enabled && funds==elixir.Current && before.SequenceEqual(hand.Cycle.Hand) && queue.SequenceEqual(hand.Cycle.UpcomingQueue),"Screen release over originating card cancels without charge or cycle");
            casting.ProcessPointer(point,true,false);
            var pause=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<Text>().text=="Pause");
            check(casting.ProcessPointer(RectTransformUtility.WorldToScreenPoint(null,pause.transform.position),false,true)==CardUseResult.Cancelled && before.SequenceEqual(hand.Cycle.Hand) && funds==elixir.Current,"Screen release over other UI cancels");
            casting.ProcessPointer(point,true,false); casting.ProcessPointer(new Vector2(-100,-100),false,false);
            check(casting.IsAiming && !line.enabled,"Off-table dragging hides indicator without creating an effect");
            check(casting.ProcessPointer(new Vector2(-100,-100),false,true)==CardUseResult.Cancelled && funds==elixir.Current && queue.SequenceEqual(hand.Cycle.UpcomingQueue),"Off-table release cancels without cycle");
            casting.ProcessPointer(point,true,false); flow.TogglePause();
            check(!casting.IsAiming && casting.ProcessPointer(point,true,true)==CardUseResult.Cancelled && funds==elixir.Current,"Pause cancels aiming and blocks screen casting"); flow.TogglePause();
            elixir.TrySpend(elixir.Current); string warning=null; Action<string> listener=s=>warning=s; hand.Message+=listener;
            casting.ProcessPointer(point,true,false);
            check(casting.ProcessPointer(point,false,true)==CardUseResult.Failure && warning==GameplayHand.InsufficientElixirMessage && hand.SelectedCard==used && before.SequenceEqual(hand.Cycle.Hand) && queue.SequenceEqual(hand.Cycle.UpcomingQueue) && elixir.Current==0 && Find<PersistentSlowArea>()==null,"Unaffordable screen cast has no effect and preserves both queues with shared feedback");
            hand.Message-=listener; elixir.Advance(100);
            casting.ProcessPointer(point,true,false);
            check(casting.ProcessPointer(point,false,true)==CardUseResult.Success && Mathf.Approximately(elixir.Current,10-card.ElixirCost) && hand.Cycle.UpcomingQueue.Last()==used && hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=used).Concat(new[]{queue[0]})),"Screen cast spends once and performs common exact card rotation");
            check(casting.ProcessPointer(point,false,true)==CardUseResult.Cancelled && Mathf.Approximately(elixir.Current,10-card.ElixirCost),"A repeated mouse release cannot commit twice");
            if (card==slow) check(Find<PersistentSlowArea>()!=null,"Slow skill creates its persistent runtime through the shared cast pipeline");
            if (card==slow) {
                Select(hand,toy); casting.BeginAim(new Vector2(-4,1),false);
                TabletopStageValidation.CapturePreview("/tmp/defense-skills-preview.png"); casting.CancelAim();
            }
            ClearAreas();
        }
        // True AoE boundary and height independence, with nonlethal test enemies.
        var enemyData=UnityEngine.Object.Instantiate(PlayerSession.Catalog.Stages[0].Enemy); Set(enemyData,"health",200);
        var targets=Enumerable.Range(0,3).Select(_=>spawner.Spawn(enemyData)).ToArray();
        for (int i=0;i<3;i++) { targets[i].Movement.Stop(); targets[i].transform.position=new Vector3(i==0?0:toy.Skill.Radius+(i==2?.02f:0),i*10,0); }
        toy.Skill.Effect.Apply(toy.Skill,new SkillCastContext(Vector3.zero,registry,flow));
        check(targets[0].CurrentHealth==140 && targets[1].CurrentHealth==140 && targets[2].CurrentHealth==200,"Toy Throw damages center and inclusive boundary, excludes outside, ignores Y");
        foreach(var enemy in targets) enemy.ReceiveDamage(10000);
        TestSlowAreas(check,slow.Skill,enemyData,spawner,registry,flow,surface);
        TestDelays(check,toy,hand,placement,casting,elixir,flow,spawner,enemyData);
        ClearAreas();
        expiredArea=Area(slow.Skill,Vector3.zero,registry,flow); expiredArea.Advance(slow.Skill.ActiveDuration+slow.Skill.FadeDuration+.1f);
        UnityEngine.Object.Destroy(enemyData);
        hand.Initialize(PlayerSession.Deck,placement,flow); hand.CancelSelection(); elixir.Initialize(flow);
    }
    private static PersistentSlowArea Area(SkillDefinition data,Vector3 position,EnemyRegistry registry,GameFlow flow) {
        data.Effect.Apply(data,new SkillCastContext(position,registry,flow));
        return UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None).Single(a=>a.transform.position==position);
    }
    private static void TestSlowAreas(Action<bool,string> check,SkillDefinition data,EnemyDefinition enemyData,EnemySpawner spawner,EnemyRegistry registry,GameFlow flow,TabletopSurface surface) {
        var enemy=spawner.Spawn(enemyData); enemy.Movement.Stop(); enemy.transform.position=Vector3.zero;
        float baseSpeed=enemy.Movement.BaseMovementSpeed, health=enemy.CurrentHealth;
        var a=Area(data,Vector3.zero,registry,flow);
        check(a.IsActive && a.AffectedCount==1 && enemy.CurrentHealth==health && Mathf.Approximately(enemy.Movement.EffectiveMovementSpeed,baseSpeed*.5f),"New area immediately slows an inside enemy without any damage");
        var visual=a.GetComponentInChildren<SkillAreaVisual>(); var line=visual.GetComponent<LineRenderer>();
        check(Mathf.Abs(PlanarSpace.SqrDistance(line.GetPosition(0),a.transform.position)-data.Radius*data.Radius)<.001f && a.Radius==data.Radius,"Persistent visual and membership query share SkillDefinition.Radius");
        enemy.transform.position=new Vector3(data.Radius+.01f,100,0); registry.NotifyMoved(enemy);
        check(enemy.Movement.Modifiers.SourceCount==0 && enemy.Movement.EffectiveMovementSpeed==baseSpeed && a.AffectedCount==0,"Moving outside removes this area's source synchronously");
        enemy.transform.position=new Vector3(data.Radius,100,0); registry.NotifyMoved(enemy);
        check(enemy.Movement.Modifiers.SourceCount==1,"Boundary is included and membership ignores height");
        enemy.transform.position=Vector3.zero; registry.NotifyMoved(enemy);
        var b=Area(data,Vector3.right,registry,flow);
        check(enemy.Movement.Modifiers.SourceCount==2 && enemy.Movement.EffectiveMovementSpeed==baseSpeed*.5f,"Overlapping half-speed areas track two sources but do not multiply strength");
        enemy.transform.position=Vector3.right*(data.Radius+.5f); registry.NotifyMoved(enemy);
        check(a.AffectedCount==0 && b.AffectedCount==1 && enemy.Movement.Modifiers.SourceCount==1 && enemy.Movement.EffectiveMovementSpeed==baseSpeed*.5f,"Leaving A while inside B preserves B's independent slow");
        enemy.transform.position=Vector3.right*(data.Radius+2); registry.NotifyMoved(enemy);
        check(enemy.Movement.Modifiers.SourceCount==0 && enemy.Movement.EffectiveMovementSpeed==baseSpeed,"Leaving every area restores original effective speed");
        enemy.transform.position=Vector3.zero; registry.NotifyMoved(enemy);
        visual.enabled=false;
        check(enemy.Movement.Modifiers.SourceCount==2 && !line.enabled,"Disabling presentation leaves the gameplay slow intact"); visual.enabled=true;
        flow.TogglePause(); a.Advance(50); b.Advance(50);
        check(a.IsActive && b.IsActive && a.VisualOpacity==1 && enemy.Movement.Modifiers.SourceCount==2,"Pause freezes active area lifetime"); flow.TogglePause();
        a.Advance(data.ActiveDuration-.01f); check(a.IsActive && a.VisualOpacity==1,"Area remains fully active before its configured deadline");
        a.Advance(.02f);
        check(!a.IsActive && a.AffectedCount==0 && enemy.Movement.Modifiers.SourceCount==1 && enemy.Movement.EffectiveMovementSpeed==baseSpeed*.5f && a.VisualOpacity>0,"A expiry removes only A's source before its visual fade ends");
        UnityEngine.Object.DestroyImmediate(b.gameObject);
        check(enemy.Movement.Modifiers.SourceCount==0 && enemy.Movement.BaseMovementSpeed==baseSpeed && enemy.Movement.EffectiveMovementSpeed==baseSpeed,"Area destruction safely removes remaining source without mutating base speed");
        float opacity=a.VisualOpacity; flow.TogglePause(); a.Advance(1);
        check(a.VisualOpacity==opacity,"Pause freezes visual fade too"); flow.TogglePause();
        a.Advance(data.FadeDuration/2);
        check(a.VisualOpacity>0 && a.VisualOpacity<opacity && Mathf.Abs(line.startColor.a-a.VisualOpacity)<=1f/255f,"Visual alpha fades after gameplay expiry and follows runtime state");
        a.Advance(data.FadeDuration);
        check(a.VisualOpacity==0 && !line.enabled && !a.IsActive,"Fade completion hides the visual and schedules destruction");
        UnityEngine.Object.DestroyImmediate(a.gameObject);
        var c=Area(data,new Vector3(.5f,0,0),registry,flow);
        enemy.ReceiveDamage(10000);
        check(c.AffectedCount==0 && enemy.Movement.Modifiers.SourceCount==0,"Enemy death unregisters and releases all tracked slow references immediately");
        c.Advance(9); UnityEngine.Object.DestroyImmediate(c.gameObject);
        // Exercise actual follower movement notification instead of manually sending membership events.
        var pathRoot=new GameObject("Skill movement test route"); var path=pathRoot.AddComponent<WaypointPath>();
        var start=new GameObject("Start").transform; var end=new GameObject("End").transform;
        start.SetParent(pathRoot.transform); end.SetParent(pathRoot.transform); start.position=Vector3.zero; end.position=Vector3.right*50;
        var serialized=new SerializedObject(path); var points=serialized.FindProperty("points"); points.arraySize=2;
        points.GetArrayElementAtIndex(0).objectReferenceValue=start; points.GetArrayElementAtIndex(1).objectReferenceValue=end; serialized.ApplyModifiedPropertiesWithoutUndo();
        var moving=spawner.Spawn(enemyData); moving.Movement.Initialize(path,baseSpeed);
        var d=Area(data,Vector3.zero,registry,flow);
        moving.Movement.Advance((data.Radius+.1f)/(baseSpeed*.5f));
        check(d.AffectedCount==0 && moving.Movement.Modifiers.SourceCount==0 && moving.Movement.EffectiveMovementSpeed==baseSpeed,"Actual path movement exits an area and removes slow before Advance returns");
        moving.ReceiveDamage(10000); UnityEngine.Object.DestroyImmediate(d.gameObject); UnityEngine.Object.DestroyImmediate(pathRoot);
        // Different configured strengths resolve deterministically, still preserving source identity.
        var modifiers=new MovementModifiers(); var sourceA=new object(); var sourceB=new object();
        modifiers.SetSlow(sourceA,.5f); modifiers.SetSlow(sourceB,.25f);
        check(modifiers.Multiplier==.25f && modifiers.SourceCount==2,"Strongest slow policy handles differing strengths without multiplication");
        modifiers.Remove(sourceB); check(modifiers.Multiplier==.5f,"Removing stronger source reveals weaker remaining source");
    }
    private static void TestDelays(Action<bool,string> check,SkillCardDefinition original,GameplayHand hand,TurretPlacementController placement,SkillCastingController casting,ElixirSystem elixir,GameFlow flow,EnemySpawner spawner,EnemyDefinition enemyData) {
        var card=UnityEngine.Object.Instantiate(original); var data=UnityEngine.Object.Instantiate(card.Skill);
        Set(data,"castingTime",.5f); Set(data,"travelTime",.75f);
        var serialized=new SerializedObject(card); serialized.FindProperty("skill").objectReferenceValue=data; serialized.ApplyModifiedPropertiesWithoutUndo();
        hand.Initialize(new PlayerDeck(PlayerSession.Catalog.InitialDeck.Take(5).Concat(new[]{card}).ToArray()),placement,flow);
        elixir.Initialize(flow); Select(hand,card);
        var enemy=spawner.Spawn(enemyData); enemy.Movement.Stop(); enemy.transform.position=Vector3.zero;
        casting.BeginAim(Vector2.zero,false); casting.ReleaseAim(Vector2.zero,false);
        casting.Advance(.5f);
        check(enemy.CurrentHealth==200 && elixir.Current==0 && hand.Cycle.UpcomingQueue.Last().Definition==card,"CastingTime completes without collapsing TravelTime; card paid and cycled on commitment");
        flow.TogglePause(); casting.Advance(10); check(enemy.CurrentHealth==200,"Pause freezes post-launch travel delay"); flow.TogglePause();
        casting.Advance(.74f); check(enemy.CurrentHealth==200,"TravelTime independently delays arrival after casting");
        casting.Advance(.02f); check(enemy.CurrentHealth==140 && elixir.Current==0,"Arrival applies one AoE without a second affordability check");
        casting.Advance(10); check(enemy.CurrentHealth==140,"Delayed cast cannot apply twice");
        // Isolated flow verifies terminal cancellation without ending the main regression stage.
        var isolated=new GameObject("Skill Game Over fixture"); var isolatedFlow=isolated.AddComponent<GameFlow>();
        var isolatedCasting=isolated.AddComponent<SkillCastingController>(); isolatedCasting.Initialize(hand,isolatedFlow,Find<EnemyRegistry>(),Camera.main);
        Select(hand,card); elixir.Initialize(flow); isolatedCasting.BeginAim(Vector2.zero,false); isolatedCasting.ReleaseAim(Vector2.zero,false);
        check(isolatedCasting.PendingCastCount==1,"Committed delayed skill is queued for this run");
        isolatedFlow.GameOver(); isolatedCasting.Advance(10);
        check(isolatedCasting.PendingCastCount==0 && !isolatedCasting.IsAiming && enemy.CurrentHealth==140,"Game Over discards pending skill effects without later damage");
        UnityEngine.Object.DestroyImmediate(isolated); Time.timeScale=1;
        enemy.ReceiveDamage(10000); UnityEngine.Object.Destroy(card); UnityEngine.Object.Destroy(data);
    }
}
}
