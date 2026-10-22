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
public static class PauseMenuSkinValidation {
    private const string Key="Defense.PauseSkin.Validation";
    private static int checks,phase,frame;
    private static double started;
    private static float pausedTime,resumedTime;
    private static Vector3 pausedPosition;
    private static Enemy actor;
    private static CardDefinition[] deck;
    private static int preset;
    static PauseMenuSkinValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        foreach(var path in new[]{DeckPersistence.DefaultPath,PlayerCurrency.DefaultPath})
            if(Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Isolated profile paths required.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Defense/Scenes/TopDownStage3.unity");Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){checks=phase=0;frame=-1;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Debug.Log("PAUSE_SKIN PASS: "+message);}
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
    private static void Tick(){if(!SessionState.GetBool(Key,false))return;try{
        if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Pause verification timeout");
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(frame<0){if(GameObject.Find("Pause")==null||Flow==null)return;frame=Time.frameCount;return;}
        if(Time.frameCount-frame<6)return;
        switch(phase){
            case 0:
                Check(Time.frameCount>frame&&Flow.IsPlaying,"Real frames advance in gameplay");
                UnityEngine.Object.FindAnyObjectByType<FixedIntervalSpawner>().enabled=false;
                actor=UnityEngine.Object.FindAnyObjectByType<EnemySpawner>().Spawn(PlayerSession.ActiveStage.Enemy);
                Check(actor!=null,"Existing enemy spawns for pause/freeze verification");
                deck=PlayerSession.Deck.Slots.ToArray();preset=PlayerSession.Presets.ActivePresetIndex;
                Check(Button("Pause").transition==Selectable.Transition.ColorTint,"Top-right Pause remains unskinned");Click("Pause");break;
            case 1:
                Check(Flow.State==GameplayState.Paused&&Time.timeScale==0,"Pause freezes gameplay through existing GameFlow");
                var skin=AssetDatabase.LoadAssetAtPath<PauseMenuSkin>("Assets/Defense/Data/PauseMenuSkin.asset");
                Visual(Button("Resume"),skin.Resume);Visual(Button("Settings"),skin.Settings);Visual(Button("Exit"),skin.Exit);
                Check(Button("Resume").transform.parent.GetComponent<Image>().color==new Color(0,0,0,.78f),"Original dark overlay unchanged");
                Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1&&Button("Resume").GetComponentInParent<GraphicRaycaster>()!=null,"Original EventSystem/raycaster retained");
                pausedTime=Time.time;pausedPosition=actor.transform.position;Click("Settings");break;
            case 2:
                Check(Button("Back").transform.parent.name=="Settings"&&Time.timeScale==0,"Existing Settings opens while paused");Click("Back");break;
            case 3:
                Check(Button("Resume")!=null&&Flow.State==GameplayState.Paused,"Settings Back returns to Pause");
                Check(Flow.State==GameplayState.Paused&&Time.time==pausedTime&&actor.transform.position==pausedPosition,"Settings keeps simulation frozen");Click("Resume");resumedTime=Time.time;break;
            case 4:
                Check(Flow.IsPlaying&&Time.timeScale==1&&Time.time>resumedTime&&actor.transform.position!=pausedPosition,"Resume advances enemy movement");Click("Pause");break;
            case 5:Click("Exit");break;
            case 6:
                Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu"&&UnityEngine.Object.FindAnyObjectByType<LobbyUI>()!=null&&Time.timeScale==1,"Pause Exit directly returns to Lobby and restores timeScale");
                Check(PlayerSession.Deck.Slots.SequenceEqual(deck)&&PlayerSession.Presets.ActivePresetIndex==preset,"Pause navigation preserves deck and preset");Finish(true,checks+" assertions");return;
        }
        phase++;frame=Time.frameCount;
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void Log(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);}
    private static void Finish(bool ok,string message){SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;Application.runInBackground=SessionState.GetBool(Key+".Background",false);if(ok)Debug.Log("PAUSE_SKIN_PASSED "+message);else Debug.LogError("PAUSE_SKIN_FAILED "+message);if(Application.isBatchMode)EditorApplication.Exit(ok?0:1);else EditorApplication.isPlaying=false;}
}
}
