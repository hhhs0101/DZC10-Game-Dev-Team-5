using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Defense.Editor {
public static class CardSkillValidation {
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Set(UnityEngine.Object target,string property,float value) {
        var so = new SerializedObject(target); so.FindProperty(property).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Models(GameCatalog catalog,Action<bool,string> check) {
        check(catalog.AvailableCards.Count==17 && catalog.AvailableCards.Select(c=>c.CardId).Distinct().Count()==17,"Seventeen unique stable Card IDs: fifteen towers and two skills");
        check(catalog.AvailableCards.OfType<TowerCardDefinition>().All(c=>c.ElixirCost==0 && catalog.AvailableTowers.Contains(c.Tower)),"Tower cards reuse original turret definitions with zero Elixir cost");
        var skills = catalog.AvailableCards.OfType<SkillCardDefinition>().ToArray();
        check(skills.Length==2 && skills.All(c=>c.ElixirCost>0 && c.Skill.CastingTime==0 && c.Skill.Effect!=null) && skills[0].Skill.Radius<skills[1].Skill.Radius && skills[0].Skill.Damage>skills[1].Skill.Damage,"Fireball and Arrow Rain have configurable contrasting instant AoE data");
        var mixed = catalog.InitialDeck.Take(4).Concat(skills).ToArray();
        var deck = new PlayerDeck(mixed); var editor = new DeckEditorController(deck,catalog.AvailableCards);
        check(deck.Slots.OfType<SkillCardDefinition>().Count()==2 && editor.Available.Count==11 && !editor.Available.Intersect(deck.Slots).Any(),"Mixed deck excludes both skill and tower cards from Available");
        var cycle = new RuntimeCardCycle(deck,new System.Random(41));
        for(int i=0;i<60;i++) cycle.Use(cycle.Hand[i%3]);
        check(cycle.Hand.Concat(cycle.UpcomingQueue).Select(c=>c.Definition).Distinct().Count()==6 && deck.Slots.SequenceEqual(mixed),"Mixed cycle keeps six unique cards and immutable persistent positions");
        var synthetic = Enumerable.Range(0,6).Select(_=>UnityEngine.Object.Instantiate(skills[0])).ToArray();
        bool rejected = false;
        try { new PlayerDeck(synthetic); } catch(ArgumentException) { rejected=true; }
        check(rejected,"Domain constructor rejects six unique skills without a tower");
        var oneTower = new PlayerDeck(new[]{catalog.InitialDeck[0]}.Concat(synthetic.Take(5)) .ToArray());
        string message=null; oneTower.Rejected += value=>message=value;
        var edit = new DeckEditorController(oneTower,new[]{catalog.InitialDeck[0]}.Concat(synthetic).ToArray());
        var before = oneTower.Slots.ToArray();
        check(!edit.Replace(0,synthetic[5]) && oneTower.Slots.SequenceEqual(before) && message==PlayerDeck.MinimumTowerMessage,"Last tower replacement rejects atomically with required Korean message");
        foreach(var value in synthetic) UnityEngine.Object.Destroy(value);
        var go = new GameObject("Message test",typeof(RectTransform),typeof(Text));
        var warning = go.AddComponent<FadingMessageUI>(); var label=go.GetComponent<Text>();
        foreach(float hold in new[]{1.5f,3f}) {
            warning.Show("warning",hold); warning.Advance(hold);
            check(label.text=="warning" && label.color.a==1,"Warning holds for "+hold+" seconds");
            warning.Advance(.15f); check(label.color.a>0 && label.color.a<1,"Warning fades rather than abruptly disappearing");
            warning.Advance(.2f); check(label.text=="" && label.color.a==0,"Warning automatically clears");
        }
        UnityEngine.Object.Destroy(go);
    }
    private static void Select(GameplayHand hand,CardDefinition card) {
        while(!hand.Cycle.Hand.Any(c=>c.Definition==card)) hand.Cycle.Use(hand.Cycle.Hand[0]);
        hand.Select(hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card));
    }
    public static void Gameplay(Action<bool,string> check) {
        var hand=Find<GameplayHand>(); var flow=Find<GameFlow>(); var elixir=Find<ElixirSystem>();
        var placement=Find<TurretPlacementController>(); var casting=Find<SkillCastingController>();
        var registry=Find<EnemyRegistry>(); var spawner=Find<EnemySpawner>(); var wallet=Find<ResourceWallet>();
        foreach(var turret in UnityEngine.Object.FindObjectsByType<Turret>(FindObjectsSortMode.None)) turret.enabled=false;
        elixir.Initialize(flow); check(elixir.Current==3,"Fresh Elixir starts at exactly three");
        elixir.Advance(2); check(Mathf.Approximately(elixir.Current,3.72f),"Playing regenerates exactly .36 per second");
        flow.TogglePause(); float paused=elixir.Current; elixir.Advance(100); check(elixir.Current==paused,"Pause stops Elixir regeneration");
        flow.TogglePause(); elixir.Advance(100); check(elixir.Current==10,"Resume regenerates and clamps at maximum ten");
        check(!elixir.TrySpend(11) && !elixir.TrySpend(-1) && !elixir.TrySpend(float.NaN) && elixir.Current==10,"Invalid and unaffordable spending preserves Elixir");
        check(elixir.TrySpend(10) && elixir.Current==0,"Spending reaches but never crosses minimum zero");
        var catalog=PlayerSession.Catalog; var skills=catalog.AvailableCards.OfType<SkillCardDefinition>().ToArray();
        var mixed=new PlayerDeck(catalog.InitialDeck.Take(4).Concat(skills).ToArray());
        hand.Initialize(mixed,placement,flow);
        string feedback=null; hand.Message += value=>feedback=value;
        foreach(var card in skills) {
            Select(hand,card); var used=hand.SelectedCard; var before=hand.Cycle.Hand.ToArray(); var queue=hand.Cycle.UpcomingQueue.ToArray();
            check(!casting.IsAiming && placement.Selected==null,"Selecting "+card.DisplayName+" does not cast or select a turret");
            check(!casting.BeginAim(Vector2.zero,true) && !casting.BeginAim(new Vector2(100,100),false),"Aiming cannot start over UI or outside gameplay");
            check(casting.BeginAim(Vector2.zero,false) && casting.IsAiming && casting.IndicatorRadius==card.Skill.Radius,"Aiming uses actual "+card.DisplayName+" radius");
            casting.MoveAim(Vector2.one); check(casting.AimPosition==Vector2.one,"Indicator follows aiming position");
            var indicator=casting.GetComponentInChildren<LineRenderer>();
            check(indicator.enabled && Mathf.Approximately(Vector2.Distance(PlanarSpace.Project(indicator.GetPosition(0)),Vector2.one),card.Skill.Radius),"Rendered indicator vertices use the configured gameplay radius");
            Canvas.ForceUpdateCanvases();
            var selectedButton=GameObject.Find("Hand Card "+(hand.Cycle.Hand.ToList().IndexOf(used)+1)).GetComponent<RectTransform>();
            bool cardUi=GameplayPointer.IsOverUI(RectTransformUtility.WorldToScreenPoint(null,selectedButton.position));
            check(cardUi,"Current-position UI raycast recognizes originating Hand card");
            check(casting.ReleaseAim(Vector2.one,cardUi)==CardUseResult.Cancelled && !casting.IsAiming && elixir.Current==0 && hand.Cycle.Hand.SequenceEqual(before) && hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Release over UI cancels without cost or cycle");
            casting.BeginAim(Vector2.zero,false);
            var pauseButton=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<Text>().text=="Pause");
            bool otherUi=GameplayPointer.IsOverUI(RectTransformUtility.WorldToScreenPoint(null,pauseButton.transform.position));
            check(otherUi && casting.ReleaseAim(Vector2.zero,otherUi)==CardUseResult.Cancelled && hand.Cycle.Hand.SequenceEqual(before) && elixir.Current==0,"Release over another actual UI element cancels without spending or cycling");
            casting.BeginAim(Vector2.zero,false);
            check(casting.ReleaseAim(new Vector2(100,100),false)==CardUseResult.Cancelled,"Release outside gameplay cancels");
            casting.BeginAim(Vector2.zero,false); flow.TogglePause(); check(!casting.IsAiming,"Pause hides and cancels active aim"); flow.TogglePause();
            casting.BeginAim(Vector2.zero,false);
            check(casting.ReleaseAim(Vector2.zero,false)==CardUseResult.Failure && hand.SelectedCard==used && hand.Cycle.Hand.SequenceEqual(before) && elixir.Current==0 && feedback==GameplayHand.InsufficientElixirMessage,"Insufficient Elixir rejects skill, preserves selection and displays Korean feedback");
            elixir.Advance(100);
            var enemyData=UnityEngine.Object.Instantiate(catalog.Stages[0].Enemy); Set(enemyData,"health",100);
            var targets=new[]{spawner.Spawn(enemyData),spawner.Spawn(enemyData),spawner.Spawn(enemyData)};
            for(int i=0;i<3;i++) { targets[i].GetComponent<EnemyPathFollower>().Stop(); targets[i].transform.position=Vector3.right*(i==2 ? card.Skill.Radius+.1f : i*.2f); }
            casting.BeginAim(Vector2.zero,false);
            check(casting.ReleaseAim(Vector2.zero,false)==CardUseResult.Success && Mathf.Approximately(elixir.Current,10-card.ElixirCost),"Successful "+card.DisplayName+" spends Elixir once");
            check(targets[0].CurrentHealth==100-card.Skill.Damage && targets[1].CurrentHealth==100-card.Skill.Damage && targets[2].CurrentHealth==100,"AoE damages every inside enemy and excludes outside enemy");
            check(hand.Cycle.UpcomingQueue.Last()==used && hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=used).Concat(new[]{queue[0]})),"Skill success uses the shared exact Hand/Queue rotation");
            float spent=elixir.Current;
            check(casting.ReleaseAim(Vector2.zero,false)==CardUseResult.Cancelled && elixir.Current==spent,"Repeated release cannot double charge or recast");
            foreach(var enemy in targets) enemy.ReceiveDamage(10000); UnityEngine.Object.Destroy(enemyData);
            Select(hand,card); casting.BeginAim(new Vector2(7,3),false);
            check(casting.ReleaseAim(new Vector2(7,3),false)==CardUseResult.Success,"Empty-area skill cast still succeeds");
            elixir.TrySpend(elixir.Current);
        }
        var lethalTargets=new[]{spawner.Spawn(catalog.Stages[0].Enemy),spawner.Spawn(catalog.Stages[0].Enemy)};
        foreach(var enemy in lethalTargets) { enemy.GetComponent<EnemyPathFollower>().Stop(); enemy.transform.position=Vector3.zero; }
        int goldBefore=wallet.Balance;
        skills[0].Skill.Effect.Apply(skills[0].Skill,Vector2.zero,registry);
        check(lethalTargets.All(e=>!e.IsAlive && !registry.Enemies.Contains(e)) && wallet.Balance==goldBefore+2*catalog.Stages[0].Enemy.ResourceReward,"Lethal AoE safely removes multiple registry entries and grants normal kill rewards");
        // Non-zero future cast time: charge and cycle now, apply later without checking funds again.
        var delayed=UnityEngine.Object.Instantiate(skills[0]); var delayedData=UnityEngine.Object.Instantiate(delayed.Skill);
        Set(delayedData,"castingTime",1); var serialized=new SerializedObject(delayed); serialized.FindProperty("skill").objectReferenceValue=delayedData; serialized.ApplyModifiedPropertiesWithoutUndo();
        hand.Initialize(new PlayerDeck(catalog.InitialDeck.Take(5).Concat(new[]{delayed}).ToArray()),placement,flow);
        elixir.Initialize(flow); Select(hand,delayed);
        var target=spawner.Spawn(catalog.Stages[0].Enemy); target.GetComponent<EnemyPathFollower>().Stop(); target.transform.position=Vector3.zero;
        float hp=target.CurrentHealth; casting.BeginAim(Vector2.zero,false);
        check(casting.ReleaseAim(Vector2.zero,false)==CardUseResult.Success && elixir.Current==0 && target.CurrentHealth==hp,"Delayed cast spends and cycles immediately, before damage");
        flow.TogglePause(); casting.Advance(10); check(target.CurrentHealth==hp,"Pending cast waits while paused"); flow.TogglePause();
        casting.Advance(.5f); check(target.CurrentHealth==hp,"Pending cast respects nonzero CastingTime");
        casting.Advance(.5f); check(!target.IsAlive && elixir.Current==0,"Committed effect applies later without rechecking Elixir");
        UnityEngine.Object.Destroy(delayed); UnityEngine.Object.Destroy(delayedData);
        // Paid tower uses the same shared Elixir field and validation pipeline.
        var paid=UnityEngine.Object.Instantiate((TowerCardDefinition)catalog.InitialDeck[0]); Set(paid,"elixirCost",2);
        hand.Initialize(new PlayerDeck(new CardDefinition[]{paid}.Concat(catalog.InitialDeck.Skip(1)).ToArray()),placement,flow);
        Select(hand,paid); wallet.Initialize(1000);
        var free=UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None).First(s=>!s.IsOccupied);
        var towerCycle=hand.Cycle.Hand.ToArray();
        check(!placement.TryPlace(free) && !free.IsOccupied && wallet.Balance==1000 && hand.Cycle.Hand.SequenceEqual(towerCycle),"Paid tower insufficient Elixir preserves gold, slot and cycle");
        elixir.Initialize(flow);
        check(!placement.TryPlace(null) && elixir.Current==3,"Invalid tower placement spends no Elixir");
        check(placement.TryPlace(free) && elixir.Current==1 && wallet.Balance==1000-paid.Tower.Cost,"Successful tower placement spends gold and common Elixir cost exactly once");
        free.Occupant.enabled=false; UnityEngine.Object.Destroy(paid);
        var bar=Find<ElixirBarUI>(); bar.Advance(100); float display=bar.DisplayedFill;
        elixir.Advance(100); bar.Advance(.1f);
        check(bar.DisplayedFill>display && bar.DisplayedFill<1 && elixir.Current==10,"Elixir fill smoothly approaches authoritative value without changing it");
        hand.Initialize(PlayerSession.Deck,placement,flow); hand.CancelSelection(); elixir.Initialize(flow);
        var isolated=new GameObject("Game Over Elixir test"); var isolatedFlow=isolated.AddComponent<GameFlow>(); var isolatedElixir=isolated.AddComponent<ElixirSystem>(); isolatedElixir.Initialize(isolatedFlow);
        isolatedFlow.GameOver(); isolatedElixir.Advance(100); check(isolatedElixir.Current==3,"Game Over stops Elixir regeneration");
        UnityEngine.Object.DestroyImmediate(isolated); Time.timeScale=1;
    }
}
}
