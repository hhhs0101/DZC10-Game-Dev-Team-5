using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Defense.Editor {
[InitializeOnLoad]
public static class TopDownStage2Validation {
    private const string Key="Defense.TopDownStage2.Validation";
    private static int step,checks;
    private static double started;
    private static float sampledTime;
    private static Vector3 sampledPosition;
    private static Enemy probe,combatEnemy,farEnemy;
    private static Tower combatTurret;
    private static TurretDefinition combatData;
    private static readonly List<float> hits=new List<float>();
    private static CardDefinition[] deck;
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    static TopDownStage2Validation(){if(SessionState.GetBool(Key,false))Attach();}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("TOPDOWN2 PASS: "+message);}
    private static void Value(UnityEngine.Object obj,string name,float value){var so=new SerializedObject(obj);var field=so.FindProperty(name);if(field.propertyType==SerializedPropertyType.Integer){if(value!=Mathf.Floor(value))throw new ArgumentException("Fractional cost");field.intValue=(int)value;}else field.floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Reference(UnityEngine.Object obj,string name,UnityEngine.Object value)=>PrototypeBuilder.Set(obj,name,value);
    private static GameFlow Flow=>Find<GameFlow>();
    private static GameplayHand Hand=>Find<GameplayHand>();
    private static EnemyRegistry Registry=>Find<EnemyRegistry>();
    private static EnemySpawner Spawner=>Find<EnemySpawner>();
    private static ElixirSystem Elixir=>Find<ElixirSystem>();
    private static TurretPlacementController Placement=>Find<TurretPlacementController>();
    private static SkillCastingController Casting=>Find<SkillCastingController>();
    private static Vector2 Screen(Vector3 p)=>Camera.main.WorldToScreenPoint(p);
    private static void Select(CardDefinition card){for(int i=0;i<6 && !Hand.Cycle.Hand.Any(c=>c.Definition==card);i++)Hand.Cycle.Use(Hand.Cycle.Hand[0]);var index=Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card);var button=GameObject.Find("Hand Card "+(index+1)).GetComponent<UnityEngine.UI.Button>();if(Hand.SelectedCard!=Hand.Cycle.Hand[index])button.onClick.Invoke();Check(button.interactable && Hand.SelectedCard?.Definition==card,"Select through actual Hand UI button: "+card.DisplayName);Canvas.ForceUpdateCanvases();}
    private static Enemy Spawn(Vector3 p,EnemyDefinition data=null){var e=Spawner.Spawn(data??Find<TopDownInteractionSandbox>().TestStage.Enemy);e.Movement.Stop();e.transform.position=p;Registry.NotifyMoved(e);return e;}
    private static void ClearEnemies(){foreach(var e in Registry.Enemies.ToArray())e.ReceiveDamage(100000);}
    private static void ClearAreas(){foreach(var a in UnityEngine.Object.FindObjectsByType<PersistentSlowArea>())UnityEngine.Object.DestroyImmediate(a.gameObject);}
    [MenuItem("Defense/2D Migration/Validate Stage 2 (Play Mode)")]
    public static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);SessionState.SetBool(Key+".PreviousBackground",Application.runInBackground);Application.runInBackground=true;SessionState.SetBool(Key,true);Attach();EditorApplication.isPlaying=true;}
    private static void Attach(){started=EditorApplication.timeSinceStartup;step=checks=0;hits.Clear();EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;}
    private static void OnLog(string m,string t,LogType type){
        if(m.StartsWith("FMOD failed to switch back to normal output")) {
            File.AppendAllText("Documentation/Report/2D_Stage2_EditorAudio_Warnings.txt",m+Environment.NewLine);
            return; // Native Editor audio device failure; retained separately, never mask gameplay errors.
        }
        if((type==LogType.Error||type==LogType.Exception)&&!t.Contains("UnityEditor.Search.SearchDatabase"))Finish(false,m+"\n"+t);}
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Stage 2 validation timeout");
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling||Find<TopDownInteractionSandbox>()==null||GameObject.Find("Hand Card 1")==null)return;
        if(step==0){
            Find<FixedIntervalSpawner>().enabled=false;deck=Find<TopDownInteractionSandbox>().Deck.Slots.ToArray();ClearEnemies();
            Check(Registry.Plane==WaypointPlane.XY && Find<Collider>()==null && Find<StageInitializer>()==null,"2D fixture uses XY registry and no active 3D collider/session bootstrap");
            Check(Elixir.Current>=3 && Elixir.Current<3.5f && Find<ResourceWallet>().Balance==150,"Existing starting Elixir/resource values remain intact");
            probe=Spawner.Spawn(Find<TopDownInteractionSandbox>().TestStage.Enemy);Check(probe.GetComponent<Collider2D>()!=null && probe.GetComponentInChildren<MeshRenderer>()==null,"Spawner instantiates the native 2D BasicEnemy actor");sampledPosition=probe.transform.position;sampledTime=Time.time;step=1;
        }else if(step==1 && Time.time-sampledTime>=.4f){
            Check(probe.transform.position.x<sampledPosition.x && Mathf.Abs(probe.transform.position.z)<.001f,"Actual BasicEnemy moves right-to-left in XY during advancing Play Mode frames");
            Check(Mathf.Abs(Vector3.Distance(sampledPosition,probe.transform.position)-probe.Movement.EffectiveMovementSpeed*(Time.time-sampledTime))<.05f,"Actual Enemy movement retains configured logical speed");ClearEnemies();TestPlacement();BeginCombat();step=2;
        }else if(step==2 && hits.Count>=2){
            Check(combatEnemy.CurrentHealth==200-2*combatData.Damage && farEnemy.CurrentHealth==200,"Actual 2D turret applies configured damage and excludes Enemy beyond XY range");
            Check(hits[1]-hits[0]>=combatData.AttackCooldown-.03f,"Actual turret attacks respect configured cooldown");combatTurret.enabled=false;ClearEnemies();
            TestSkills();TestSlow();TestBaseArrival();TestDelays();
            Check(PlayerSession.Currency==null && PlayerSession.Presets==null,"Interaction tests never initialize persistent player services");
            Check(Find<TopDownInteractionSandbox>().Deck.Slots.SequenceEqual(deck),"All local runtime rotations leave the six-card source Deck unchanged");
            Finish(true,checks+" assertions");
        }
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void TestPlacement(){
        var map=Find<BattlefieldMap2D>();var input=Find<BattlefieldPointer2D>();var wallet=Find<ResourceWallet>();var slot=map.PlacementSlots[0];
        var slotView=slot.GetComponentInChildren<PlacementSlotSpriteView>();var slotRenderer=slot.GetComponentInChildren<SpriteRenderer>();var availableColor=slotView.CurrentColor;
        Check(map.PlacementSlots.All(s=>{var v=s.GetComponentInChildren<SpriteRenderer>();var hit=s.GetComponent<BoxCollider2D>();return AssetDatabase.GetAssetPath(v.sprite)=="Assets/Defense/Art/TopDown2D/WoodenPlate.png"&&v.sortingOrder==20&&hit.size==Vector2.one&&Mathf.Abs(v.bounds.size.x-1.5f)<.001f&&Mathf.Abs(v.bounds.size.y-1.5f*102/154)<.001f;}),"All slots reuse the 1.5x wooden plate with original proportions, unchanged 1x1 click area and sorting order");
        Check(availableColor==Color.white,"Available plate preserves original untinted wooden artwork");
        Check(input.TryGetPoint(Screen(new Vector3(1,2,0)),out var point)&&Vector3.Distance(point,new Vector3(1,2,0))<.001f,"Unified screen-to-XY conversion uses gameplay depth rather than camera near clip");
        Check(!input.TryGetPoint(Screen(new Vector3(11,0,0)),out _),"Off-table background rejects world input");
        var card=deck.OfType<TowerCardDefinition>().First();Select(card);var before=Hand.Cycle.Hand.ToArray();var queue=Hand.Cycle.UpcomingQueue.ToArray();var selected=Hand.SelectedCard;
        wallet.Initialize(1000);Elixir.TrySpend(Elixir.Current);string message=null;Placement.Message+=m=>message=m;
        Check(!Placement.ProcessPointer(Screen(Vector3.zero),true)&&message=="You cannot place a turret here."&&Hand.Cycle.Hand.SequenceEqual(before)&&wallet.Balance==1000,"Non-slot click preserves Hand and resources");
        wallet.Initialize(0);Check(!Placement.ProcessPointer(Screen(slot.transform.position),true)&&message=="Not enough resources."&&!slot.IsOccupied&&Elixir.Current==0&&Hand.Cycle.Hand.SequenceEqual(before),"Insufficient resources never place, spend Elixir or cycle");wallet.Initialize(1000);
        var rect=GameObject.Find("Hand Card 1").GetComponent<RectTransform>();var original=rect.position;Vector2 screen=Screen(slot.transform.position);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(GameObject.Find("Canvas").GetComponent<RectTransform>(),screen,null,out var overlay);rect.position=overlay;Canvas.ForceUpdateCanvases();
        Check(GameplayPointer.IsOverUI(screen),"Fixture places actual uGUI Hand button over target screen coordinate");
        Check(!Placement.ProcessPointer(screen,true)&&!slot.IsOccupied&&Hand.SelectedCard==selected,"uGUI intercepts click above 2D slot");rect.position=original;Canvas.ForceUpdateCanvases();
        Flow.TogglePause();Check(!Placement.ProcessPointer(screen,true)&&!slot.IsOccupied,"Paused 2D input rejects placement");Flow.TogglePause();
        var collider=slot.GetComponent<Collider2D>();collider.enabled=false;var child=new GameObject("Child collider fixture");child.transform.SetParent(slot.transform,false);child.layer=slot.gameObject.layer;child.AddComponent<BoxCollider2D>().isTrigger=true;
        var anchor=slot.PlacementPoint.localPosition;slot.PlacementPoint.localPosition=new Vector3(.2f,.1f,0);Physics2D.SyncTransforms();
        Check(Placement.ProcessPointer(screen,false)==false&&slot.IsHovered,"2D child collider highlights owning slot without consuming card");
        Check(slotView.CurrentColor!=availableColor&&slotRenderer.sprite.name=="WoodenPlate","Actual pointer hover highlights the same wooden Sprite");
        Placement.ProcessPointer(Screen(Vector3.zero),false);Check(!slot.IsHovered&&slotView.CurrentColor==availableColor,"Pointer exit restores available plate color without consuming a card");
        Check(Placement.ProcessPointer(screen,true)&&slot.IsOccupied&&slot.Occupant.GetComponentInChildren<SpriteRenderer>()!=null&&slot.Occupant.GetComponentInChildren<MeshRenderer>()==null,"Valid 2D slot click places common Tower with sprite presentation");
        Check(slotView.CurrentColor==availableColor&&slotRenderer.color==Color.white&&slot.Occupant.GetComponentInChildren<SpriteRenderer>().sortingOrder>slotRenderer.sortingOrder,"Occupied plate retains original wooden colors and the Tower renders above it");
        slot.Occupant.enabled=false;combatTurret=slot.Occupant;combatData=card.Tower;
        Check(Vector3.Distance(slot.Occupant.transform.position,slot.PlacementPoint.position)<.001f,"2D placement uses configured independent PlacementPoint");
        Check(wallet.Balance==1000-card.Tower.Cost && Elixir.Current==0 && Hand.SelectedCard==null,"Zero-Elixir Tower commits at zero Elixir and pays its own resource cost");
        Check(Hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=selected).Concat(new[]{queue[0]}))&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue.Skip(1).Concat(new[]{selected})),"Successful placement rotates exactly once");
        Select(card);before=Hand.Cycle.Hand.ToArray();queue=Hand.Cycle.UpcomingQueue.ToArray();int balance=wallet.Balance;
        Check(!Placement.ProcessPointer(screen,true)&&message=="A turret is already placed here."&&wallet.Balance==balance&&Hand.Cycle.Hand.SequenceEqual(before)&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Occupied slot rejects without resource spend or cycle");
        UnityEngine.Object.DestroyImmediate(child);collider.enabled=true;slot.PlacementPoint.localPosition=anchor;Physics2D.SyncTransforms();
        var paid=UnityEngine.Object.Instantiate(card);Value(paid,"elixirCost",2);Hand.Initialize(new PlayerDeck(deck.Select(c=>c==card?paid:c).ToArray()),Placement,Flow);Select(paid);
        var freeSlot=map.PlacementSlots[1];before=Hand.Cycle.Hand.ToArray();
        Check(!Placement.ProcessPointer(Screen(freeSlot.transform.position),true)&&!freeSlot.IsOccupied&&Hand.Cycle.Hand.SequenceEqual(before)&&wallet.Balance==balance,"Unaffordable nonzero Tower Elixir leaves all placement state unchanged");
        Hand.Initialize(new PlayerDeck(deck),Placement,Flow);UnityEngine.Object.Destroy(paid);
    }
    private static void BeginCombat(){
        var data=UnityEngine.Object.Instantiate(Find<TopDownInteractionSandbox>().TestStage.Enemy);Value(data,"health",200);
        combatEnemy=Spawn(combatTurret.transform.position+Vector3.right*.5f+Vector3.forward*10,data);farEnemy=Spawn(combatTurret.transform.position+Vector3.up*(combatData.AttackRange+.1f),data);
        var context=Registry;Check(combatData.Targeting.Select(combatTurret.transform.position,combatData.AttackRange,context.Enemies,context.Plane)==combatEnemy,"Closest targeting filters XY range rather than XZ");
        float previous=combatEnemy.CurrentHealth;combatEnemy.GetComponent<EnemyHealth>().Changed+=(current,max)=>{if(current<previous)hits.Add(Time.time);previous=current;};combatTurret.enabled=true;
        UnityEngine.Object.Destroy(data);
    }
    private static void AssertRadius(SkillAreaVisual view,Vector3 center,float radius)=>SkillEllipseValidation.AssertMarker(view,center,radius,Check);
    private static void TestSkills(){
        var toy=deck.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is CircularDamageSkill);
        foreach(float radius in new[]{1.5f,3f,4f}){
            var card=UnityEngine.Object.Instantiate(toy);var skill=UnityEngine.Object.Instantiate(toy.Skill);Value(skill,"radius",radius);Reference(card,"skill",skill);
            Hand.Initialize(new PlayerDeck(deck.Select(c=>c==toy?card:c).ToArray()),Placement,Flow);Elixir.Initialize(Flow);Select(card);
            Check(!Casting.IsAiming,"Selecting Skill Card alone does not cast or aim");var before=Hand.Cycle.Hand.ToArray();var queue=Hand.Cycle.UpcomingQueue.ToArray();float energy=Elixir.Current;
            var screen=Screen(Vector3.zero);Casting.ProcessPointer(screen,true,false);Check(Casting.IsAiming,"LMB press on valid 2D tabletop begins aiming");
            Casting.ProcessPointer(Screen(Vector3.right),false,false);Check(Casting.AimPosition==Vector2.right,"Hold moves XY aim indicator");
            AssertRadius(UnityEngine.Object.FindObjectsByType<SkillAreaVisual>().Single(v=>v.name=="Skill Aim Indicator"),Vector3.right,radius);
            var rect=GameObject.Find("Hand Card "+(Hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==card)+1)).GetComponent<RectTransform>();
            Check(Casting.ProcessPointer(rect.position,false,true)==CardUseResult.Cancelled&&!Casting.IsAiming&&Elixir.Current==energy&&Hand.Cycle.Hand.SequenceEqual(before)&&Hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Release on origin Hand Card cancels without cost/cycle");
            Casting.ProcessPointer(screen,true,false);Check(Casting.ProcessPointer(Screen(new Vector3(11,0,0)),false,true)==CardUseResult.Cancelled&&Elixir.Current==energy,"Off-table release cancels without cost");
            Casting.ProcessPointer(screen,true,false);Flow.TogglePause();Check(!Casting.IsAiming&&Elixir.Current==energy,"Pause cancels XY aiming without payment");Flow.TogglePause();
            var data=UnityEngine.Object.Instantiate(Find<TopDownInteractionSandbox>().TestStage.Enemy);Value(data,"health",200);
            var a=Spawn(new Vector3(0,0,8),data);var b=Spawn(new Vector3(radius,0,-8),data);var c=Spawn(new Vector3(radius+.02f,0,0),data);
            Casting.ProcessPointer(screen,true,false);Check(Casting.ProcessPointer(screen,false,true)==CardUseResult.Success,"Valid 2D release commits Toy Throw");
            Check(a.CurrentHealth==200-skill.Damage&&b.CurrentHealth==200-skill.Damage&&c.CurrentHealth==200,"Toy Throw includes center/boundary, excludes outside and ignores visual Z");
            Check(Mathf.Abs(a.GetComponent<EnemyHealthBar>().NormalizedHealth-a.CurrentHealth/a.MaximumHealth)<.001f,"Reusable HP bar refreshes from actual Enemy damage");
            Check(Mathf.Abs(Elixir.Current-(energy-card.ElixirCost))<.001f&&Hand.Cycle.UpcomingQueue.Last().Definition==card,"Skill pays Elixir and cycles once on successful release");
            Select(card);var hp=c.CurrentHealth;before=Hand.Cycle.Hand.ToArray();Casting.ProcessPointer(screen,true,false);
            Check(Casting.ProcessPointer(screen,false,true)==CardUseResult.Failure&&c.CurrentHealth==hp&&Hand.Cycle.Hand.SequenceEqual(before),"Insufficient Skill Elixir rejects without effect or rotation");
            ClearEnemies();Hand.Initialize(new PlayerDeck(deck),Placement,Flow);UnityEngine.Object.Destroy(card);UnityEngine.Object.Destroy(skill);UnityEngine.Object.Destroy(data);
        }
    }
    private static PersistentSlowArea Area(SkillDefinition skill,Vector3 point){var before=UnityEngine.Object.FindObjectsByType<PersistentSlowArea>();skill.Effect.Apply(skill,new SkillCastContext(point,Registry,Flow));return UnityEngine.Object.FindObjectsByType<PersistentSlowArea>().Except(before).Single();}
    private static void Move(Enemy enemy,Vector3 point){enemy.transform.position=point;Registry.NotifyMoved(enemy);}
    private static void TestSlow(){
        var slow=deck.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is SlowAreaSkill);var skill=slow.Skill;Check(skill.ActiveDuration==8,"Existing eight-second Slow Area default is unchanged");
        var enemy=Spawn(Vector3.zero);float speed=enemy.Movement.BaseMovementSpeed;Select(slow);Elixir.Initialize(Flow);var before=Hand.Cycle.Hand.ToArray();
        Casting.ProcessPointer(Screen(Vector3.zero),true,false);Check(Casting.ProcessPointer(Screen(Vector3.zero),false,true)==CardUseResult.Success,"Slow Area uses same 2D press/release commitment");
        var a=UnityEngine.Object.FindFirstObjectByType<PersistentSlowArea>();var visual=a.GetComponentInChildren<SkillAreaVisual>();AssertRadius(visual,Vector3.zero,skill.Radius);
        Check(a.Plane==WaypointPlane.XY&&a.AffectedCount==1&&enemy.Movement.Modifiers.SourceCount==1&&enemy.Movement.EffectiveMovementSpeed==speed*.5f,"Active zone applies a single half-speed source on XY membership");
        Move(enemy,new Vector3(skill.Radius+.01f,0,50));Check(a.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==0,"XY exit synchronously removes area source regardless of visual depth");
        Move(enemy,new Vector3(0,skill.Radius*skill.RangeProfile.AspectRatio,20));Check(a.AffectedCount==1,"Inclusive XY boundary ignores render Z");
        Move(enemy,Vector3.zero);var b=Area(skill,Vector3.right);
        Check(enemy.Movement.Modifiers.SourceCount==2&&enemy.Movement.EffectiveMovementSpeed==speed*.5f,"Overlapping areas retain independent sources without multiplication");
        Move(enemy,Vector3.right*(skill.Radius+.5f));Check(a.AffectedCount==0&&b.AffectedCount==1&&enemy.Movement.Modifiers.SourceCount==1,"Leaving A while inside B preserves B source");
        Move(enemy,Vector3.zero);visual.enabled=false;Check(enemy.Movement.Modifiers.SourceCount==2,"Disabling area presentation does not remove slow behavior");visual.enabled=true;
        Flow.TogglePause();a.Advance(20);b.Advance(20);Check(a.IsActive&&b.IsActive&&a.VisualOpacity==1,"Pause freezes area lifetime");Flow.TogglePause();
        a.Advance(skill.ActiveDuration-.01f);Check(a.IsActive,"Zone stays active until configured deadline");a.Advance(.02f);
        Check(!a.IsActive&&a.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==1&&a.VisualOpacity>0,"Expiry removes gameplay source immediately before visual fade completes");
        UnityEngine.Object.DestroyImmediate(b.gameObject);Check(enemy.Movement.Modifiers.SourceCount==0&&enemy.Movement.EffectiveMovementSpeed==speed,"Destroying other zone restores effective speed without changing base speed");
        float alpha=a.VisualOpacity;Flow.TogglePause();a.Advance(1);Check(a.VisualOpacity==alpha,"Pause also freezes post-expiry fade");Flow.TogglePause();a.Advance(skill.FadeDuration/2);Check(a.VisualOpacity>0&&a.VisualOpacity<alpha,"Visual fade progresses only after gameplay expiration");a.Advance(skill.FadeDuration);Check(a.VisualOpacity==0&&!visual.IsVisible,"Fade completion hides ellipse and retires visual");ClearAreas();
        var c=Area(skill,Vector3.zero);Move(enemy,new Vector3(0,0,0));enemy.gameObject.SetActive(false);Check(c.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==0,"Enemy disable/despawn immediately clears source references");UnityEngine.Object.DestroyImmediate(enemy.gameObject);ClearAreas();
        enemy=Spawn(Vector3.zero);c=Area(skill,Vector3.zero);enemy.ReceiveDamage(10000);Check(c.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==0,"Combat death releases source ownership immediately");ClearAreas();
        enemy=Spawn(Vector3.zero);c=Area(skill,Vector3.zero);enemy.transform.position=Vector3.right*(skill.Radius+1);c.RefreshMembership();Check(c.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==0,"Membership reconciliation clears teleported enemies without trigger-exit dependence");ClearAreas();ClearEnemies();
        foreach(float radius in new[]{1.25f,4.25f}) {
            var configured=UnityEngine.Object.Instantiate(skill);Value(configured,"radius",radius);
            enemy=Spawn(new Vector3(0,radius*configured.RangeProfile.AspectRatio-.01f,5));c=Area(configured,Vector3.zero);
            AssertRadius(c.GetComponentInChildren<SkillAreaVisual>(),Vector3.zero,radius);
            Check(c.AffectedCount==1 && enemy.Movement.Modifiers.SourceCount==1,"Configured Slow radius includes inside XY point: "+radius);
            Move(enemy,new Vector3(0,radius*configured.RangeProfile.AspectRatio+.01f,-5));Check(c.AffectedCount==0 && enemy.Movement.Modifiers.SourceCount==0,"Configured Slow radius excludes outside XY point: "+radius);
            ClearAreas();ClearEnemies();UnityEngine.Object.Destroy(configured);
        }
        // Real path-driven movement must notify membership before Advance returns.
        var root=new GameObject("XY slow exit route");var route=root.AddComponent<WaypointPath>();var data=new SerializedObject(route);data.FindProperty("plane").enumValueIndex=1;var points=data.FindProperty("points");points.arraySize=2;
        for(int i=0;i<2;i++){var t=new GameObject("Point").transform;t.SetParent(root.transform);t.position=Vector3.right*(i*20);points.GetArrayElementAtIndex(i).objectReferenceValue=t;}data.ApplyModifiedPropertiesWithoutUndo();
        enemy=Spawner.Spawn(Find<TopDownInteractionSandbox>().TestStage.Enemy);enemy.Movement.Initialize(route,speed);c=Area(skill,Vector3.zero);enemy.Movement.Advance((skill.Radius+.1f)/(speed*.5f));
        Check(c.AffectedCount==0&&enemy.Movement.Modifiers.SourceCount==0&&enemy.Movement.EffectiveMovementSpeed==speed,"Actual slowed XY movement exits and clears its modifier before returning");ClearAreas();ClearEnemies();UnityEngine.Object.DestroyImmediate(root);
    }
    private static void TestBaseArrival(){int defeated=0;Action<Enemy> onDeath=e=>defeated++;Spawner.EnemyDefeated+=onDeath;var hp=Find<BaseHealth>();int previous=hp.Current;int count=Registry.AliveEnemyCount;var e=Spawner.Spawn(Find<TopDownInteractionSandbox>().TestStage.Enemy);e.Movement.Advance(1000);Check(hp.Current==previous-e.Definition.BaseDamage&&Registry.AliveEnemyCount==count&&defeated==0,"XY Base arrival deals damage and retires Enemy without a combat-death reward event");Spawner.EnemyDefeated-=onDeath;}
    private static void TestDelays(){
        var toy=deck.OfType<SkillCardDefinition>().First(c=>c.Skill.Effect is CircularDamageSkill);var card=UnityEngine.Object.Instantiate(toy);var skill=UnityEngine.Object.Instantiate(toy.Skill);Value(skill,"castingTime",.5f);Value(skill,"travelTime",.75f);Reference(card,"skill",skill);
        Hand.Initialize(new PlayerDeck(deck.Select(c=>c==toy?card:c).ToArray()),Placement,Flow);Elixir.Initialize(Flow);Select(card);var data=UnityEngine.Object.Instantiate(Find<TopDownInteractionSandbox>().TestStage.Enemy);Value(data,"health",200);var enemy=Spawn(Vector3.zero,data);
        Casting.ProcessPointer(Screen(Vector3.zero),true,false);Casting.ProcessPointer(Screen(Vector3.zero),false,true);Casting.Advance(.5f);Check(Casting.PendingCastCount==1&&enemy.CurrentHealth==200,"Casting delay and travel delay remain separate in 2D");
        Flow.TogglePause();Casting.Advance(10);Check(enemy.CurrentHealth==200,"Pause freezes delayed cast");Flow.TogglePause();Casting.Advance(.74f);Check(enemy.CurrentHealth==200,"Travel delay does not resolve early");Casting.Advance(.02f);Casting.Advance(10);Check(enemy.CurrentHealth==200-skill.Damage&&Casting.PendingCastCount==0&&Elixir.Current==0,"Delayed arrival applies once without second payment");
        Elixir.Initialize(Flow);Select(card);Casting.ProcessPointer(Screen(Vector3.zero),true,false);Casting.ProcessPointer(Screen(Vector3.zero),false,true);Flow.GameOver();Check(Casting.PendingCastCount==0&&!Casting.IsAiming,"Game Over discards committed pending effects in 2D");
        UnityEngine.Object.Destroy(card);UnityEngine.Object.Destroy(skill);UnityEngine.Object.Destroy(data);
    }
    private static void Finish(bool pass,string message){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;Directory.CreateDirectory("Documentation/Report");File.WriteAllText("Documentation/Report/2D_Stage2_Validation_Result.txt",(pass?"PASS: ":"FAIL: ")+message);if(pass)Debug.Log("TOPDOWN_STAGE2_PASSED "+message);else Debug.LogError("TOPDOWN_STAGE2_FAILED "+message);EditorApplication.isPlaying=false;Application.runInBackground=SessionState.GetBool(Key+".PreviousBackground",false);if(Application.isBatchMode)EditorApplication.Exit(pass?0:1);}
}
}
