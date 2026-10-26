using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Defense.Editor {
[InitializeOnLoad]
public static class SegmentedEnergyValidation {
    private const string Key="Defense.SegmentedEnergy.Validation";
    private static int checks,phase,frame;
    private static double started;
    private static float sampleTime;
    static SegmentedEnergyValidation(){if(SessionState.GetBool(Key,false))Attach();}
    [MenuItem("Defense/Validate Segmented Energy (Play Mode)")]
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        foreach(var path in new[]{DeckPersistence.DefaultPath,PlayerCurrency.DefaultPath})if(Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Isolated save paths required.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/TopDownStage3.unity");Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){checks=phase=0;frame=-1;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("SEGMENTED_ENERGY PASS: "+message);}
    private static void Model(){
        var go=new GameObject("Integer Energy model");var flow=go.AddComponent<GameFlow>();var energy=go.AddComponent<ElixirSystem>();energy.enabled=false;
        try {
            energy.Initialize(flow);Check(energy.CurrentEnergy==3&&energy.MaxEnergy==10,"Integer starting value and maximum");
            energy.Advance(ElixirSystem.RegenerationInterval*.75f);Check(energy.CurrentEnergy==3,"No fractional spendable Energy before interval");
            flow.TogglePause();energy.Advance(100);flow.TogglePause();energy.Advance(ElixirSystem.RegenerationInterval*.25f+.0001f);Check(energy.CurrentEnergy==4,"Pause preserves partial timer without advancing it");
            energy.Advance(ElixirSystem.RegenerationInterval);Check(energy.CurrentEnergy==5,"Exactly two integer points after two active intervals");
            energy.Initialize(flow);energy.Advance(ElixirSystem.RegenerationInterval*2.5f);Check(energy.CurrentEnergy==5,"Long frame restores multiple points");energy.Advance(ElixirSystem.RegenerationInterval*.5f+.0001f);Check(energy.CurrentEnergy==6,"Long-frame remainder is preserved");
            energy.Advance(100);Check(energy.CurrentEnergy==10,"Long frames clamp at ten");energy.Advance(1000);energy.TrySpend(1);energy.Advance(ElixirSystem.RegenerationInterval-.01f);Check(energy.CurrentEnergy==9,"Full bar banks no hidden regeneration");energy.Advance(.02f);Check(energy.CurrentEnergy==10,"Spending from full starts a fresh interval");
            Check(!energy.TrySpend(-1)&&!energy.TrySpend(11)&&!energy.TrySpend(.5f)&&!energy.TrySpend(float.NaN)&&!energy.TrySpend(float.PositiveInfinity)&&energy.CurrentEnergy==10,"Invalid/fractional/insufficient costs never spend");
            energy.Advance(float.NaN);energy.Advance(float.PositiveInfinity);energy.Advance(-10);Check(energy.CurrentEnergy==10,"Invalid elapsed time ignored");
            energy.TrySpend(10);energy.Advance(float.MaxValue);Check(energy.CurrentEnergy==10,"Extremely long finite frames cannot overflow");
            foreach(var state in new[]{GameplayState.GameOver,GameplayState.Victory,GameplayState.ConfigurationError}){
                var other=new GameObject("Terminal Energy");var terminal=other.AddComponent<GameFlow>();var wallet=other.AddComponent<ElixirSystem>();wallet.enabled=false;wallet.Initialize(terminal);
                if(state==GameplayState.GameOver)terminal.GameOver();else if(state==GameplayState.ConfigurationError)terminal.FailConfiguration("test");else typeof(GameFlow).GetMethod("TryVictory",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(terminal,null);
                wallet.Advance(100);Check(wallet.CurrentEnergy==3,"No regeneration during "+state);UnityEngine.Object.DestroyImmediate(other);
            }
        } finally {UnityEngine.Object.DestroyImmediate(go);Time.timeScale=1;}
    }
    private static void UI(){
        var energy=Find<ElixirSystem>();var flow=Find<GameFlow>();var bar=Find<ElixirBarUI>();energy.enabled=false;
        var root=GameObject.Find("Energy Bar");var frameImage=root.GetComponent<Image>();var blocks=root.transform.Find("Energy Segments").GetComponentsInChildren<Image>(true).OrderBy(b=>b.rectTransform.anchorMin.y).ToArray();
        Check(blocks.Length==10&&blocks.Select(b=>b.sprite).Distinct().Single()==Resources.Load<Sprite>("UI/EnergySegment"),"Exactly ten instances reuse one yellow sprite");
        Check(frameImage.sprite==Resources.Load<Sprite>("UI/EnergyBarFrame")&&frameImage.preserveAspect&&!frameImage.raycastTarget,"Empty frame uses original artwork and does not block input");
        Check(GameObject.Find("Energy Fill")==null&&GameObject.Find("Energy Icon")==null&&GameObject.Find("Energy Amount")==null,"Old fill rectangle, detached bar icon, and n/10 amount label removed");
        var bolt=root.transform.Find("Energy Bolt Overlay");
        Check(bolt!=null&&bolt.GetSiblingIndex()>blocks[0].transform.parent.GetSiblingIndex()&&bolt.GetComponent<Image>().sprite==Resources.Load<Sprite>("UI/EnergyBarBolt")&&bolt.GetComponent<Image>().preserveAspect&&!bolt.GetComponent<Image>().raycastTarget,"Lightning overlay is a final sibling above all fill segments and never blocks input");
        Canvas.ForceUpdateCanvases();var parent=(RectTransform)blocks[0].transform.parent;
        foreach(var block in blocks){var corners=new Vector3[4];block.rectTransform.GetWorldCorners(corners);Check(corners.Select(p=>parent.InverseTransformPoint(p)).All(p=>p.x>=parent.rect.xMin-.01f&&p.x<=parent.rect.xMax+.01f&&p.y>=parent.rect.yMin-.01f&&p.y<=parent.rect.yMax+.01f)&&!block.raycastTarget&&block.preserveAspect,"Segment stays within dark fill area: "+block.name);}
        foreach(int amount in new[]{0,1,5,10}){
            energy.Initialize(flow);energy.TrySpend(energy.CurrentEnergy);energy.Advance(amount*2);
            Check(bar.DisplayedSegments==amount&&blocks.Select((b,i)=>b.gameObject.activeSelf==(i<amount)).All(value=>value)&&bolt.Find("Energy Number").GetComponent<Text>().text==amount.ToString(),"Immediate bottom-to-top display at "+amount);
        }
        foreach(var card in PlayerSession.Catalog.AvailableCards){var field=new SerializedObject(card).FindProperty("elixirCost");Check(field.propertyType==SerializedPropertyType.Integer&&field.intValue==card.ElixirCost,"Exact integer authored cost preserved: "+card.CardId);}
    }
    private static void Select(GameplayHand hand,CardDefinition definition){while(!hand.Cycle.Hand.Any(c=>c.Definition==definition))hand.Cycle.Use(hand.Cycle.Hand[0]);hand.Select(hand.Cycle.Hand.ToList().FindIndex(c=>c.Definition==definition));}
    private static void Actions(){
        var hand=Find<GameplayHand>();var placement=Find<TurretPlacementController>();var flow=Find<GameFlow>();var energy=Find<ElixirSystem>();var casting=Find<SkillCastingController>();var bar=Find<ElixirBarUI>();
        var skills=PlayerSession.Catalog.AvailableCards.OfType<SkillCardDefinition>().ToArray();var towers=PlayerSession.Catalog.AvailableCards.OfType<TowerCardDefinition>().Take(4).ToArray();
        hand.Initialize(new PlayerDeck(towers.Cast<CardDefinition>().Concat(skills).ToArray()),placement,flow);
        var point=(Vector2)Find<BattlefieldMap2D>().PlayableArea.Bounds.center;
        foreach(var skill in skills){
            Select(hand,skill);energy.Initialize(flow);energy.TrySpend(energy.CurrentEnergy);var order=hand.Cycle.Hand.ToArray();var queue=hand.Cycle.UpcomingQueue.ToArray();
            Check(casting.BeginAim(point,false)&&casting.ReleaseAim(point,true)==CardUseResult.Cancelled&&energy.CurrentEnergy==0&&order.SequenceEqual(hand.Cycle.Hand)&&queue.SequenceEqual(hand.Cycle.UpcomingQueue),"Cancelled skill preserves Energy and Hand: "+skill.CardId);
            casting.BeginAim(point,false);Check(casting.ReleaseAim(point,false)==CardUseResult.Failure&&energy.CurrentEnergy==0&&order.SequenceEqual(hand.Cycle.Hand)&&queue.SequenceEqual(hand.Cycle.UpcomingQueue),"Insufficient Energy rejects skill without cycling: "+skill.CardId);
            energy.Advance(100);var used=hand.SelectedCard;casting.BeginAim(point,false);
            Check(casting.ReleaseAim(point,false)==CardUseResult.Success&&energy.CurrentEnergy==10-skill.ElixirCost&&bar.DisplayedSegments==energy.CurrentEnergy&&hand.Cycle.UpcomingQueue.Last()==used,"Skill commits integer cost and updates UI/cycle once: "+skill.CardId);
            int paid=energy.CurrentEnergy;Check(casting.ReleaseAim(point,false)==CardUseResult.Cancelled&&energy.CurrentEnergy==paid,"Repeated release cannot double charge");
        }
        Select(hand,towers[0]);energy.Initialize(flow);energy.Advance(100);var before=hand.Cycle.Hand.ToArray();
        Check(!placement.TryPlace(null)&&energy.CurrentEnergy==10&&before.SequenceEqual(hand.Cycle.Hand),"Invalid placement preserves Energy and Hand");
        var slot=UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>().First(s=>!s.IsOccupied);
        Check(placement.TryPlace(slot)&&energy.CurrentEnergy==10-towers[0].ElixirCost&&bar.DisplayedSegments==energy.CurrentEnergy,"Valid tower placement pays own integer cost and updates bar");
        Select(hand,towers[0]);int current=energy.CurrentEnergy;before=hand.Cycle.Hand.ToArray();Check(!placement.TryPlace(slot)&&energy.CurrentEnergy==current&&before.SequenceEqual(hand.Cycle.Hand),"Occupied placement spends nothing and does not cycle");
        hand.CancelSelection();Check(energy.CurrentEnergy==current,"Card deselection spends nothing");
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>30)throw new Exception("Energy validation timeout");
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(frame<0){if(Find<ElixirBarUI>()==null||GameObject.Find("Energy Bar")==null)return;frame=Time.frameCount;return;}
        if(Time.frameCount-frame<3)return;
        var energy=Find<ElixirSystem>();
        switch(phase){
            case 0:Find<FixedIntervalSpawner>().enabled=false;Model();TowerLifetimeValidation.Run();UI();Actions();energy.Initialize(Find<GameFlow>());energy.enabled=true;sampleTime=Time.time;phase=1;break;
            case 1:if(Time.time-sampleTime<2.05f)return;Check(energy.CurrentEnergy==4&&Find<ElixirBarUI>().DisplayedSegments==4,"Live frame updates regenerate one point after two seconds");phase=2;break;
            case 2:if(Time.time-sampleTime<4.05f)return;Check(energy.CurrentEnergy==5&&Find<ElixirBarUI>().DisplayedSegments==5,"Live frame updates regenerate a second point after four seconds");Finish(true,checks+" assertions");break;
        }
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Log(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);}
    private static void Finish(bool ok,string message){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(ok)Debug.Log("SEGMENTED_ENERGY_PASSED "+message);else Debug.LogError("SEGMENTED_ENERGY_FAILED "+message);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);else EditorApplication.isPlaying=false;}
}
}
