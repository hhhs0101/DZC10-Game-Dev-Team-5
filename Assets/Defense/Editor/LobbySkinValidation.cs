using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Defense.Editor {
// Sends native uGUI events to the existing menu; all player writes use an isolated test profile.
[InitializeOnLoad]
public static class LobbySkinValidation {
    private const string Key="Defense.LobbySkin.Validation";
    private static int checks,phase,stageBeforeSettings,presetBeforeDeckSettings;
    private static CardDefinition[] deckBeforeSettings;
    private static double started;
    private static int firstFrame;
    static LobbySkinValidation(){if(SessionState.GetBool(Key,false))Attach();}
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        foreach(var path in new[]{DeckPersistence.DefaultPath,PlayerCurrency.DefaultPath})
            if(Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar))throw new InvalidOperationException("Isolated deck and currency environment paths are required.");
        SessionState.SetBool(Key+".Background",Application.runInBackground);Application.runInBackground=true;
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Quit",false);
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");Attach();EditorApplication.isPlaying=true;
    }
    private static void Attach(){checks=phase=0;firstFrame=-1;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
    private static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Debug.Log("LOBBY_SKIN PASS: "+message);}
    private static Button Button(string name)=>UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.name==name);
    private static void Mouse(PointerEventData p,Vector2 point,PointerEventData.FramePressState state){
        p.position=point;var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(p,hits);p.pointerCurrentRaycast=hits.FirstOrDefault();
        typeof(StandaloneInputModule).GetMethod("ProcessMousePress",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(EventSystem.current.currentInputModule,new object[]{new PointerInputModule.MouseButtonEventData{buttonData=p,buttonState=state}});
    }
    private static void Click(string name){
        var b=Button(name);Canvas.ForceUpdateCanvases();var point=RectTransformUtility.WorldToScreenPoint(null,b.transform.position);
        var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=-190};
        Mouse(p,point,PointerEventData.FramePressState.Pressed);Check(ExecuteEvents.GetEventHandler<Button>(p.pointerCurrentRaycast.gameObject)==b.gameObject,"Raycast reaches "+name+" (point="+point+", hit="+(p.pointerCurrentRaycast.gameObject!=null?p.pointerCurrentRaycast.gameObject.name:"<none>")+", screen="+Screen.width+"x"+Screen.height+")");
        Mouse(p,point,PointerEventData.FramePressState.Released);
    }
    private static void Visual(Button b,MainMenuSkin.ButtonSprites art){
        Check(b.transition==Selectable.Transition.SpriteSwap&&b.image.sprite==art.Normal&&b.targetGraphic==b.image,"Original Button uses normal art: "+b.name);
        Check(b.image.color==Color.white&&b.image.preserveAspect&&b.image.raycastTarget,"Art stays untinted, preserves aspect, receives clicks: "+b.name);
        Check(b.GetComponentsInChildren<Text>().All(t=>!t.enabled),"Baked lettering has no duplicate rendered overlay: "+b.name);
        Check(b.spriteState.highlightedSprite==art.Highlighted&&b.spriteState.selectedSprite==art.Highlighted&&b.spriteState.pressedSprite==art.Pressed,"Columns map to hover / keyboard selection / press: "+b.name);
        var p=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        EventSystem.current.SetSelectedGameObject(null);b.OnPointerExit(p);b.OnDeselect(new BaseEventData(EventSystem.current));
        Check(b.image.overrideSprite==art.Normal,"Idle actually displays column 1: "+b.name);
        b.OnPointerEnter(p);Check(b.image.overrideSprite==art.Highlighted,"Pointer hover actually displays column 2: "+b.name);
        b.OnPointerDown(p);Check(b.image.overrideSprite==art.Pressed,"Pointer down actually displays column 3: "+b.name);
        b.OnPointerUp(p);Check(b.image.overrideSprite==art.Highlighted,"Release restores hover art: "+b.name);
        b.OnPointerExit(p);EventSystem.current.SetSelectedGameObject(null);b.OnDeselect(new BaseEventData(EventSystem.current));
        b.Select();Check(b.image.overrideSprite==art.Highlighted,"Keyboard selection has visible highlight: "+b.name);
        EventSystem.current.SetSelectedGameObject(null);b.OnDeselect(new BaseEventData(EventSystem.current));
    }
    private static void Layout(GameObject menu){
        var test=new GameObject("Layout Test Canvas",typeof(RectTransform),typeof(Canvas));test.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        try{
            menu=UnityEngine.Object.Instantiate(menu,test.transform,false);
            var background=UnityEngine.Object.Instantiate(GameObject.Find("Menu Background"),test.transform,false).GetComponent<Image>();
            foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080),new Vector2(1024,768),new Vector2(2560,1080)}){
                ((RectTransform)test.transform).sizeDelta=size;Canvas.ForceUpdateCanvases();var parentRect=((RectTransform)menu.transform).rect;
                foreach(var image in menu.GetComponentsInChildren<Image>().Where(i=>i.gameObject!=menu)){
                    var rect=image.rectTransform;var corners=new Vector3[4];rect.GetLocalCorners(corners);
                    var bounds=corners.Select(c=>menu.transform.InverseTransformPoint(rect.TransformPoint(c))).ToArray();
                    Check(bounds.All(p=>parentRect.Contains(new Vector2(Mathf.Clamp(p.x,parentRect.xMin+.001f,parentRect.xMax-.001f),Mathf.Clamp(p.y,parentRect.yMin+.001f,parentRect.yMax-.001f)))&&p.x>=parentRect.xMin-.01f&&p.x<=parentRect.xMax+.01f&&p.y>=parentRect.yMin-.01f&&p.y<=parentRect.yMax+.01f),"Artwork remains inside menu at "+size+": "+image.name);
                }
                var bg=background.rectTransform.rect;
                Check(bg.width>=size.x-.01f&&bg.height>=size.y-.01f&&Mathf.Abs(bg.width/bg.height-background.sprite.rect.width/background.sprite.rect.height)<.0001f,"Background covers the Canvas without stretching at "+size);
                var buttons=menu.GetComponentsInChildren<Button>().OrderByDescending(b=>((RectTransform)b.transform).anchoredPosition.y).ToArray();
                Check(buttons.All(b=>((RectTransform)b.transform).rect.width>200&&((RectTransform)b.transform).rect.height>90),"Button hit areas remain usable at "+size);
            }
        }finally{UnityEngine.Object.DestroyImmediate(test);Canvas.ForceUpdateCanvases();}
    }
    private static void Verify(){
        var menu=GameObject.Find("Start Screen");var skin=AssetDatabase.LoadAssetAtPath<MainMenuSkin>("Assets/Defense/Data/MainMenuSkin.asset");
        Check(skin!=null&&skin.Logo!=null,"Authored menu skin and original logo are present");
        var background=GameObject.Find("Menu Background").GetComponent<Image>();
        Check(background.sprite==skin.Background&&!background.raycastTarget&&background.transform.GetSiblingIndex()==0,"Harvest background uses the shared skin and stays behind all UI without blocking clicks");
        Check(background.GetComponent<AspectRatioFitter>().aspectMode==AspectRatioFitter.AspectMode.EnvelopeParent,"Background uses proportional full-screen cover");
        Check(menu.GetComponent<Image>().color==new Color(0,0,0,.08f),"Main Menu uses a subtle 8 percent dark readability shade");
        Check(UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1&&menu.GetComponentInParent<GraphicRaycaster>()!=null,"Original single EventSystem and Canvas raycaster retained");
        var logo=menu.transform.Find("Food Defense Logo").GetComponent<Image>();
        Check(logo.sprite==skin.Logo&&logo.preserveAspect&&!logo.raycastTarget,"Logo uses source artwork without blocking input");
        Check(menu.GetComponentsInChildren<Button>().Length==3&&!menu.GetComponentsInChildren<Text>().Any(t=>t.enabled),"Start screen renders exactly three art buttons and no duplicate labels");
        Visual(Button("Start Game"),skin.StartGame);Visual(Button("Settings"),skin.Settings);Visual(Button("Exit"),skin.Exit);Layout(menu);
        Button("Start Game").Select();Button("Start Game").OnMove(new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down});
        Check(EventSystem.current.currentSelectedGameObject==Button("Settings").gameObject,"Existing downward keyboard navigation reaches Settings");
        Button("Settings").OnMove(new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down});
        Check(EventSystem.current.currentSelectedGameObject==Button("Exit").gameObject,"Existing downward keyboard navigation reaches Exit");
        EventSystem.current.SetSelectedGameObject(null);
        Click("Settings");Check(!menu.activeSelf&&Button("Back")!=null,"Settings callback opens the existing placeholder panel");
    }
    private static void Corners(){
        var lobby=UnityEngine.Object.FindAnyObjectByType<LobbyUI>();
        var skin=AssetDatabase.LoadAssetAtPath<MainMenuSkin>("Assets/Defense/Data/MainMenuSkin.asset");
        var back=lobby.GetComponentsInChildren<Button>().Single(b=>b.name=="Back");
        var settings=lobby.GetComponentsInChildren<Button>().Single(b=>b.name=="Settings");
        var buttons=new[]{back,settings};var art=new[]{skin.LobbyBack,skin.LobbySettings};
        for(int i=0;i<2;i++){
            var rect=(RectTransform)buttons[i].transform;var pos=rect.anchoredPosition;var size=rect.sizeDelta;
            Check(rect.anchorMin==(i==0?new Vector2(0,1):new Vector2(1,1))&&rect.anchorMax==rect.anchorMin&&pos==(i==0?new Vector2(95,-40):new Vector2(-100,-40))&&size==(i==0?new Vector2(160,48):new Vector2(180,48)),"Corner anchors, offsets and click-target dimensions retain their original values: "+buttons[i].name);
            Visual(buttons[i],art[i]);
            Check(rect.anchoredPosition==pos&&rect.sizeDelta==size,"Corner state transitions never change position or hitbox: "+buttons[i].name);
            Check(buttons[i].spriteState.disabledSprite==null,"Third-column Pressed art is not assigned as Disabled: "+buttons[i].name);
            Check(new[]{art[i].Normal,art[i].Highlighted,art[i].Pressed}.All(sprite=>sprite!=null&&sprite.rect.size==new Vector2(536,192)&&sprite.pivot==new Vector2(268,96)),"Corner state sprites have consistent dimensions and center pivots: "+buttons[i].name);
        }
        var test=new GameObject("Corner Layout Test",typeof(RectTransform),typeof(Canvas));test.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        try{
            var copies=buttons.Select(b=>UnityEngine.Object.Instantiate(b,test.transform,false)).ToArray();
            foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080),new Vector2(1024,768),new Vector2(2560,1080)}){
                ((RectTransform)test.transform).sizeDelta=size;Canvas.ForceUpdateCanvases();var parent=((RectTransform)test.transform).rect;
                foreach(var copy in copies){
                    var rect=(RectTransform)copy.transform;var points=new Vector3[4];rect.GetWorldCorners(points);
                    Check(points.Select(point=>test.transform.InverseTransformPoint(point)).All(point=>point.x>=parent.xMin-.01f&&point.x<=parent.xMax+.01f&&point.y>=parent.yMin-.01f&&point.y<=parent.yMax+.01f),"Corner hitbox stays on-screen at "+size+": "+copy.name);
                }
            }
        }finally{UnityEngine.Object.DestroyImmediate(test);Canvas.ForceUpdateCanvases();}
        EventSystem.current.SetSelectedGameObject(null);stageBeforeSettings=PlayerSession.Selection.SelectedStageIndex;
    }
    private static void DeckCorners(){
        var deck=UnityEngine.Object.FindAnyObjectByType<DeckEditorUI>();
        var skin=AssetDatabase.LoadAssetAtPath<MainMenuSkin>("Assets/Defense/Data/MainMenuSkin.asset");
        var back=deck.GetComponentsInChildren<Button>().Single(b=>b.name=="Back to Lobby");
        var settings=deck.GetComponentsInChildren<Button>().Single(b=>b.name=="Settings");
        Visual(back,skin.LobbyBack);Visual(settings,skin.LobbySettings);
        var a=(RectTransform)back.transform;var b=(RectTransform)settings.transform;
        Check(a.anchorMin==new Vector2(0,1)&&a.anchorMax==a.anchorMin&&a.anchoredPosition==new Vector2(140,-40)&&a.sizeDelta==new Vector2(250,48),"Deck Back retains its original anchor, position and hitbox after all visual states");
        Check(b.anchorMin==new Vector2(1,1)&&b.anchorMax==b.anchorMin&&b.anchoredPosition==new Vector2(-100,-40)&&b.sizeDelta==new Vector2(180,48),"Deck Settings retains its original anchor, position and hitbox after all visual states");
        Check(back.spriteState.disabledSprite==null&&settings.spriteState.disabledSprite==null,"Shared Deck skin does not map Pressed as Disabled");
        deckBeforeSettings=PlayerSession.Deck.Slots.ToArray();presetBeforeDeckSettings=PlayerSession.Presets.ActivePresetIndex;
        EventSystem.current.SetSelectedGameObject(null);
    }
    private static void Actions(){
        var skin=AssetDatabase.LoadAssetAtPath<MainMenuSkin>("Assets/Defense/Data/MainMenuSkin.asset");
        var enter=Button("Enter Stage");var deck=Button("Deck");
        Visual(enter,skin.LobbyEnterStage);Visual(deck,skin.LobbyDeck);
        var a=(RectTransform)enter.transform;var b=(RectTransform)deck.transform;
        Check(a.anchorMin==new Vector2(.5f,1)&&a.anchorMax==a.anchorMin&&a.anchoredPosition==new Vector2(0,-460)&&a.sizeDelta==new Vector2(420,78),"Enter Stage retains its anchor/position and enlarged 420x78 hitbox through state swaps");
        Check(b.anchorMin==new Vector2(.5f,0)&&b.anchorMax==b.anchorMin&&b.anchoredPosition==new Vector2(0,50)&&b.sizeDelta==new Vector2(420,78),"Deck retains its anchor/position and enlarged 420x78 hitbox through state swaps");
        Check(new[]{skin.LobbyEnterStage,skin.LobbyDeck}.SelectMany(art=>new[]{art.Normal,art.Highlighted,art.Pressed}).All(sprite=>sprite!=null&&sprite.rect.size==new Vector2(720,240)&&sprite.pivot==new Vector2(360,120)),"Six action sprites share dimensions and center pivots");
        Check(enter.spriteState.disabledSprite==null&&deck.spriteState.disabledSprite==null,"Muted Pressed art is not substituted for Disabled");
        var slots=PlayerSession.Deck.Slots.ToArray();int preset=PlayerSession.Presets.ActivePresetIndex;var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        enter.interactable=false;deck.interactable=false;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};enter.OnPointerClick(pointer);deck.OnPointerClick(pointer);
        Check(UnityEngine.Object.FindAnyObjectByType<LobbyUI>()!=null&&UnityEngine.SceneManagement.SceneManager.GetActiveScene()==scene&&PlayerSession.Deck.Slots.SequenceEqual(slots)&&PlayerSession.Presets.ActivePresetIndex==preset,"Non-interactable action buttons cannot navigate or modify the deck");
        enter.interactable=true;deck.interactable=true;EventSystem.current.SetSelectedGameObject(null);
    }
    private static void Boards(){
        var lobby=UnityEngine.Object.FindAnyObjectByType<LobbyUI>();var selection=PlayerSession.Selection;
        var skin=AssetDatabase.LoadAssetAtPath<MainMenuSkin>("Assets/Defense/Data/MainMenuSkin.asset");
        var stages=PlayerSession.Catalog.Stages.ToArray();var names=stages.Select(stage=>stage.DisplayName).ToArray();var scenes=stages.Select(stage=>stage.SceneName).ToArray();
        var unlocked=Enumerable.Range(0,stages.Length).Select(i=>PlayerSession.Progression.IsUnlocked(i)).ToArray();
        Check(names.Take(3).SequenceEqual(new[]{"1-1","1-2","1-3"}),"Actual ordered stage definitions retain 1-1/1-2/1-3 names rather than baked-in art names");
        var sheet=AssetDatabase.LoadAllAssetsAtPath("Assets/Defense/Art/UI/StageBoards/StageBoardSheet.png").OfType<Sprite>().ToArray();
        var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/Defense/Art/UI/StageBoards/StageBoardSheet.png");
        Check(sheet.Length==6&&importer.spriteImportMode==SpriteImportMode.Multiple&&importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Six crisp Multiple-sheet board sub-assets imported with transparency");
        Check(sheet.All(sprite=>sprite.rect.size==new Vector2(624,288)&&sprite.pivot==new Vector2(312,144)),"Board outlines use equally padded dimensions and center pivots");
        System.Func<int,Button> card=i=>lobby.Carousel.transform.Find("Stage Card "+i).GetComponent<Button>();
        System.Action verify=()=>{
            for(int i=0;i<3;i++){
                var button=card(i);var image=button.image;var label=button.GetComponentInChildren<Text>();bool centered=i==selection.SelectedStageIndex;
                Check(image.sprite==skin.GetStageBoard(i,centered)&&image.color==Color.white&&image.preserveAspect,"Catalog row "+(i+1)+" uses focus column immediately; selected="+selection.SelectedStageIndex);
                Check(button.transition==Selectable.Transition.None&&image.canvasRenderer.GetColor()==Color.white,"Hover/press/disabled Button tint cannot change wooden focus art: "+i);
                Check(label.text.Contains(names[i])&&label.text.Contains(PlayerSession.Progression.IsUnlocked(i)?"Unlocked":"Locked")&&label.text.Contains("Selected Stage")==centered,"Real identity, lock and selected metadata remain independent: "+i);
                Check(label.rectTransform.anchoredPosition==new Vector2(0,-96)&&label.fontSize==16,"Stage metadata is outside baked-in board lettering: "+i);
            }
        };
        while(selection.SelectedStageIndex>0)selection.MovePrevious();lobby.Carousel.Advance(1);verify();
        Check(!PlayerSession.Progression.IsUnlocked(1),"Second stage fixture is locked before focus testing");
        var side=card(1);var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};var sideSprite=side.image.sprite;
        side.OnPointerEnter(pointer);side.OnPointerDown(pointer);Check(side.image.sprite==sideSprite&&side.image.canvasRenderer.GetColor()==Color.white,"Hover and press leave a side board muted");side.OnPointerUp(pointer);side.OnPointerExit(pointer);EventSystem.current.SetSelectedGameObject(null);
        side.onClick.Invoke();Check(selection.SelectedStageIndex==1&&!lobby.Carousel.IsSettled,"Existing side callback changes shared selection without teleporting");verify();
        lobby.Carousel.Advance(.1f);verify();lobby.Carousel.Advance(1);verify();
        Check(!lobby.TryEnter()&&UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu"&&card(1).image.sprite==skin.GetStageBoard(1,true),"Locked center remains bright, displays Locked and cannot be entered");
        Check(PlayerSession.Progression.IsUnlocked(0)&&card(0).image.sprite==skin.GetStageBoard(0,false),"Unlocked side stage remains muted");
        var selected=selection.SelectedStageIndex;card(selected).onClick.Invoke();Check(selection.SelectedStageIndex==selected,"Clicking center still has no stage-entry or deselection side effect");
        selection.MoveNext();verify();lobby.Carousel.Advance(1);verify();
        selection.MovePrevious();lobby.Carousel.Advance(1);selection.MovePrevious();lobby.Carousel.Advance(1);verify();
        for(int index=3;index<stages.Length;index++){
            while(selection.SelectedStageIndex<index)selection.MoveNext();lobby.Carousel.Advance(1);
            Check(skin.GetStageBoard(index,true)==null&&card(index).image.sprite==null&&card(index).GetComponentInChildren<Text>().text.Contains(names[index]),"Unmapped later stage uses its safe original card and real name: "+names[index]);
        }
        while(selection.SelectedStageIndex>0)selection.MovePrevious();lobby.Carousel.Advance(1);EventSystem.current.SetSelectedGameObject(null);
        Check(PlayerSession.Catalog.Stages.SequenceEqual(stages)&&stages.Select(stage=>stage.DisplayName).SequenceEqual(names)&&stages.Select(stage=>stage.SceneName).SequenceEqual(scenes)&&Enumerable.Range(0,stages.Length).Select(i=>PlayerSession.Progression.IsUnlocked(i)).SequenceEqual(unlocked),"Board focus navigation changes no stage IDs/order/scenes/unlock progress");
    }
    private static void Tick(){if(!SessionState.GetBool(Key,false)||SessionState.GetBool(Key+".Quit",false))return;try{
        if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Menu verification timeout");
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(firstFrame<0){if(GameObject.Find("Start Game")==null)return;firstFrame=Time.frameCount;return;}
        if(Time.frameCount-firstFrame<3)return;
        switch(phase){
            case 0: Check(Time.frameCount>firstFrame,"Real Play Mode frames advance before UI verification");Verify();break;
            case 1: Click("Back");break;
            case 2: Check(GameObject.Find("Start Screen")!=null,"Settings Back returns to skinned start screen");Click("Start Game");break;
            case 3:
                Check(UnityEngine.Object.FindAnyObjectByType<LobbyUI>()!=null&&Button("Enter Stage")!=null,"Start callback opens unchanged stage carousel Lobby");
                Check(GameObject.Find("Menu Background").activeInHierarchy&&UnityEngine.Object.FindAnyObjectByType<LobbyUI>().GetComponent<Image>().color==new Color(0,0,0,.20f),"Same harvest background remains visible in Lobby beneath a 20 percent readability shade");
                Boards();Actions();Corners();Click("Settings");break;
            case 4: Check(Button("Back").transform.parent.name=="Settings","Skinned corner Settings opens the original placeholder panel");Click("Back");break;
            case 5: Check(UnityEngine.Object.FindAnyObjectByType<LobbyUI>()!=null&&PlayerSession.Selection.SelectedStageIndex==stageBeforeSettings,"Settings Back restores Lobby and preserves selected stage");Click("Deck");break;
            case 6: Check(UnityEngine.Object.FindAnyObjectByType<DeckEditorUI>()!=null,"Existing Deck editor navigation still works");DeckCorners();Click("Settings");break;
            case 7: Check(Button("Back").transform.parent.name=="Settings","Deck skinned Settings opens the same settings panel");Click("Back");break;
            case 8: Check(UnityEngine.Object.FindAnyObjectByType<DeckEditorUI>()!=null&&PlayerSession.Presets.ActivePresetIndex==presetBeforeDeckSettings&&PlayerSession.Deck.Slots.SequenceEqual(deckBeforeSettings),"Settings Back returns to Deck without changing preset or cards");Click("Back to Lobby");break;
            case 9: Check(UnityEngine.Object.FindAnyObjectByType<LobbyUI>()!=null&&PlayerSession.Deck.Slots.SequenceEqual(deckBeforeSettings),"Deck skinned Back returns to Lobby with deck unchanged");Click("Back");break;
            case 10:
                Check(GameObject.Find("Start Screen").GetComponentsInChildren<Button>().Length==3,"Skinned Lobby Back restores the same three central art buttons");
                SessionState.SetInt(Key+".Checks",checks);SessionState.SetBool(Key+".Quit",true);Click("Exit");SessionState.SetInt(Key+".Checks",checks);break;
        }
        phase++;firstFrame=Time.frameCount;
    }catch(Exception e){Finish(false,e.ToString());}}
    private static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false)&&SessionState.GetBool(Key+".Quit",false)){checks=SessionState.GetInt(Key+".Checks",0);Check(!EditorApplication.isPlaying,"Original Exit callback stops Editor Play Mode");Finish(true,checks+" menu sprite/state/layout/navigation assertions");}}
    private static void Log(string message,string trace,LogType type){if(trace.Contains("UnityEditor.Search.SearchDatabase")&&!trace.Contains("Defense."))return;if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Finish(false,message+"\n"+trace);}
    private static void Finish(bool success,string message){
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;Application.logMessageReceived-=Log;
        Application.runInBackground=SessionState.GetBool(Key+".Background",false);
        if(success)Debug.Log("LOBBY_SKIN_PASSED "+message);else Debug.LogError("LOBBY_SKIN_FAILED "+message);
        if(Application.isBatchMode)EditorApplication.Exit(success?0:1);else EditorApplication.isPlaying=false;
    }
}
}
