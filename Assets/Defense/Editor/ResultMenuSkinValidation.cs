using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Defense.Editor {
// Exercises existing uGUI callbacks in Play Mode using an isolated player profile.
[InitializeOnLoad]
public static class ResultMenuSkinValidation {
    private const string Key="Defense.ResultSkin.Validation";
    private static int checks,phase,frame;
    private static double started;
    private static CardDefinition[] deck;
    private static int preset;
    static ResultMenuSkinValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        foreach(var path in new[]{DeckPersistence.DefaultPath,PlayerCurrency.DefaultPath})
            if(Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Isolated profile paths required.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Defense/Scenes/TopDownStage3.unity");Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){checks=phase=0;frame=-1;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("RESULT_SKIN PASS: "+message);}
    private static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name==name);
    private static GameFlow Flow=>UnityEngine.Object.FindAnyObjectByType<GameFlow>();
    private static void Click(string name){
        var b=Button(name);Canvas.ForceUpdateCanvases();var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=-190,position=RectTransformUtility.WorldToScreenPoint(null,b.transform.position)};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);p.pointerCurrentRaycast=hits.FirstOrDefault();
        Check(ExecuteEvents.GetEventHandler<Button>(p.pointerCurrentRaycast.gameObject)==b.gameObject,"Native UI raycast reaches "+name);
        var method=typeof(StandaloneInputModule).GetMethod("ProcessMousePress",BindingFlags.NonPublic|BindingFlags.Instance);
        foreach(var state in new[]{PointerEventData.FramePressState.Pressed,PointerEventData.FramePressState.Released})method.Invoke(EventSystem.current.currentInputModule,new object[]{new PointerInputModule.MouseButtonEventData{buttonData=p,buttonState=state}});
    }
    private static void Visual(Button b,MainMenuSkin.ButtonSprites art){
        Check(b.image.sprite==art.Normal&&b.transition==Selectable.Transition.SpriteSwap&&b.image.color==Color.white&&b.image.preserveAspect,"Aspect-preserved, untinted SpriteSwap: "+b.name);
        Check(b.spriteState.highlightedSprite==art.Highlighted&&b.spriteState.selectedSprite==art.Highlighted&&b.spriteState.pressedSprite==art.Pressed&&b.spriteState.disabledSprite==null,"Correct three-column states and keyboard focus: "+b.name);
        Check(b.GetComponentsInChildren<Text>().All(t=>!t.enabled),"No duplicate rendered text: "+b.name);
        var rect=(RectTransform)b.transform;Check(rect.sizeDelta==new Vector2(360,120),"Usable consistent hitbox: "+b.name);
        foreach(var sprite in new[]{art.Normal,art.Highlighted,art.Pressed}){
            Check(sprite!=null&&sprite.rect.size==new Vector2(488,180)&&sprite.pivot==new Vector2(244,90),"Aligned dimensions/pivot: "+sprite.name);
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
            Check(importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Crisp uncompressed import: "+sprite.name);
        }
        var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        EventSystem.current.SetSelectedGameObject(null);b.OnPointerExit(p);b.OnDeselect(new BaseEventData(EventSystem.current));
        Check(b.image.overrideSprite==art.Normal,"Idle shows first column: "+b.name);
        b.OnPointerEnter(p);Check(b.image.overrideSprite==art.Highlighted,"Hover shows second column: "+b.name);
        b.OnPointerDown(p);Check(b.image.overrideSprite==art.Pressed,"Press shows third column: "+b.name);
        b.OnPointerUp(p);Check(b.image.overrideSprite==art.Highlighted,"Release restores highlight: "+b.name);
        b.OnPointerExit(p);EventSystem.current.SetSelectedGameObject(null);b.OnDeselect(new BaseEventData(EventSystem.current));
        b.Select();Check(b.image.overrideSprite==art.Highlighted,"Keyboard focus highlights: "+b.name);EventSystem.current.SetSelectedGameObject(null);
    }
    private static StageDefinition stage;
    private static void Fresh(){Check(Flow.IsPlaying&&Time.timeScale==1&&PlayerSession.ActiveStage==stage,"Restart/entry preserves stage and restores timeScale");}
    private static void Defeat(){UnityEngine.Object.FindAnyObjectByType<FixedIntervalSpawner>().enabled=false;UnityEngine.Object.FindAnyObjectByType<BaseHealth>().ReceiveDamage(100000);}
    private static void Lobby(){Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu"&&UnityEngine.Object.FindAnyObjectByType<LobbyUI>()!=null&&Time.timeScale==1,"Returns directly to Lobby with normal timeScale");}
    private static void Clear(){
        var schedule=UnityEngine.Object.FindAnyObjectByType<FixedIntervalSpawner>();schedule.enabled=false;schedule.Configure(PlayerSession.ActiveStage.Enemy,.1f,1);schedule.Advance(.1f);
        foreach(var enemy in UnityEngine.Object.FindAnyObjectByType<EnemyRegistry>().Enemies.ToArray())enemy.ReceiveDamage(float.MaxValue);
        var victory=UnityEngine.Object.FindAnyObjectByType<StageVictoryController>();var before=PlayerSession.Currency.Gold;
        Check(victory.EvaluateClear(),"Existing clear evaluation succeeds");
        Check(PlayerSession.Currency.Gold==before+victory.GoldEarned&&!victory.EvaluateClear()&&PlayerSession.Currency.Gold==before+victory.GoldEarned,"One clear pays Gold once, including after replay");
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Pause verification timeout");
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(frame<0){if(GameObject.Find("Pause")==null||Flow==null)return;frame=Time.frameCount;return;}
        if(Time.frameCount-frame<6)return;
        switch(phase){
            case 0:
                deck=PlayerSession.Deck.Slots.ToArray();preset=PlayerSession.Presets.ActivePresetIndex;
                Check(Resources.FindObjectsOfTypeAll<Transform>().All(t=>t.name!="Exit Confirmation"),"No Pause exit confirmation object exists");
                Defeat();break;
            case 1:
                var skin=AssetDatabase.LoadAssetAtPath<ResultMenuSkin>("Assets/Defense/Data/ResultMenuSkin.asset");
                Check(Flow.State==GameplayState.GameOver&&Time.timeScale==0,"Base depletion opens defeat");
                Check(Button("Retry").transform.parent.GetComponentsInChildren<Button>().Length==2&&!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.name=="Replay"),"Defeat has exactly Retry and Lobby choices");
                Visual(Button("Retry"),skin.Retry);Visual(Button("Lobby"),skin.Lobby);
                stage=PlayerSession.ActiveStage;Click("Retry");break;
            case 2:Fresh();Clear();break;
            case 3:
                skin=AssetDatabase.LoadAssetAtPath<ResultMenuSkin>("Assets/Defense/Data/ResultMenuSkin.asset");
                Check(Flow.State==GameplayState.Victory&&Time.timeScale==0,"Clear opens Victory");
                Check(Button("Replay").transform.parent.GetComponentsInChildren<Button>().Length==2&&!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.name=="Retry"),"Victory has exactly Replay and Lobby choices");
                Visual(Button("Replay"),skin.Replay);Visual(Button("Return to Lobby"),skin.Lobby);
                Click("Replay");break;
            case 4:Fresh();Defeat();break;
            case 5:Click("Lobby");break;
            case 6:Lobby();stage=PlayerSession.Catalog.Stages[PlayerSession.Selection.SelectedStageIndex];Click("Enter Stage");break;
            case 7:Fresh();Clear();break;
            case 8:Click("Return to Lobby");break;
            case 9:Lobby();Click("Enter Stage");break;
            case 10:Fresh();Click("Pause");break;
            case 11:Check(Flow.State==GameplayState.Paused&&Time.timeScale==0,"Pause still freezes game");Click("Exit");break;
            case 12:Lobby();Check(PlayerSession.Deck.Slots.SequenceEqual(deck)&&PlayerSession.Presets.ActivePresetIndex==preset,"All result/Pause navigation preserves deck and preset");Finish(true,checks+" assertions");return;
        }
        phase++;frame=Time.frameCount;
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Log(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);}
    private static void Finish(bool ok,string message){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(ok)Debug.Log("RESULT_SKIN_PASSED "+message);else Debug.LogError("RESULT_SKIN_FAILED "+message);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);else EditorApplication.isPlaying=false;}
}
}
