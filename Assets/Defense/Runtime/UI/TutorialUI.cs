using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Defense {
// A replayable, skippable guide. It never spends resources or edits a player's deck.
public sealed class TutorialUI : MonoBehaviour {
    private const string SeenKey="Defense.Tutorial.Seen.v1";
    private static TutorialUI active;
    private static int closedFrame=-1;
    public static bool IsOpen => active!=null;
    public static bool BlocksInput => IsOpen || closedFrame==Time.frameCount;
    public static bool HasSeen => PlayerPrefs.GetInt(SeenKey,0)==1;
    private RectTransform sheet;
    private Text title,body,counter,nextLabel;
    private GameObject legend;
    private Button back;
    private Action closed;
    private GameObject previousSelection;
    private int page;
    private bool finished;
    private static readonly string[] Titles={"Protect the kitchen","Build your defence","Vegetables Abilities","Towers"};
    private static readonly string[] Bodies={
        "Stop the vegetables before they reach your base.\n\nEnemies that reach the end take away Base HP. Clear the stage by surviving the waves and defeating the remaining enemies.\n\nStart with Stage 1, then unlock the later stages.",
        "Each stage starts with a premade deck.\n\nIn battle, select a tower card from your hand, then click or drag to an empty placement plate. The lightning number is its energy cost. Wait for energy to refill if you cannot afford it.\n\nUsing a card cycles it through your deck.",
        "Carrot, Tomato and Eggplant are basic enemies.\n\nBell Pepper shields nearby vegetables.\n\nGarlic speeds up nearby vegetables.\n\nCorn has 5 health bars that are independent.\n\nPotato disrupts towers.\n\nBroccoli is a boss enemy with high HP and regularly summons enemies.",
        ""
    };
    public static void Open(Transform canvas,Action onClosed=null) {
        if(IsOpen)return;
        var panel=UiFactory.Panel("How to Play Overlay",canvas,new Color(.035f,.055f,.065f,.94f));panel.transform.SetAsLastSibling();
        active=panel.AddComponent<TutorialUI>();active.closed=onClosed;
        active.previousSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        active.Build();
    }
    private void Build() {
        sheet=UiFactory.Rect("Tutorial Page",transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,620));
        sheet.gameObject.AddComponent<Image>().color=new Color(.96f,.93f,.84f);
        var tag=UiFactory.Label(sheet,"GUIDE",new Vector2(.5f,1),new Vector2(0,-32),new Vector2(780,28),18);tag.color=new Color(.39f,.36f,.27f);
        title=UiFactory.Label(sheet,"",new Vector2(.5f,1),new Vector2(0,-82),new Vector2(800,60),36);title.color=new Color(.12f,.2f,.18f);
        body=UiFactory.Label(sheet,"",new Vector2(.5f,1),new Vector2(0,-317),new Vector2(788,370),25);body.color=new Color(.16f,.22f,.2f);body.alignment=TextAnchor.UpperLeft;body.resizeTextForBestFit=true;body.resizeTextMinSize=20;body.resizeTextMaxSize=25;body.verticalOverflow=VerticalWrapMode.Truncate;
        legend=UiFactory.Rect("Class Legend",sheet,new Vector2(.5f,1),new Vector2(0,-317),new Vector2(800,370)).gameObject;
        string[] descriptions={
            "Salt, BlackPepper and Paprika are basic towers, having no unique properties and only dealing damage.",
            "Oil deals damage and slows down enemies.",
            "Ketchup deals splash damage to its target and nearby enemies.",
            "Mustard deals damage that ignores enemy shields.",
            "HotSauce deals direct damage and applies burn damage that continues outside its range.",
            "SoySauce has high range and damage."
        };
        CardCombatClass[] classes={CardCombatClass.Basic,CardCombatClass.Slow,CardCombatClass.Splash,CardCombatClass.Pierce,CardCombatClass.Burn,CardCombatClass.Basic};
        for(int i=0;i<descriptions.Length;i++) {
            var row=UiFactory.Rect("Legend "+i,legend.transform,new Vector2(.5f,1),new Vector2(0,-30-i*60),new Vector2(788,58));
            var iconRect=UiFactory.Rect("Icon",row,new Vector2(0,.5f),new Vector2(25,0),new Vector2(38,38));iconRect.gameObject.AddComponent<CanvasRenderer>();var symbol=iconRect.gameObject.AddComponent<CardClassSymbol>();symbol.SetKind(classes[i]);symbol.color=new Color(.14f,.3f,.25f);symbol.raycastTarget=false;
            var text=UiFactory.Label(row,descriptions[i],new Vector2(0,.5f),new Vector2(430,0),new Vector2(704,58),22);text.alignment=TextAnchor.MiddleLeft;text.color=new Color(.16f,.22f,.2f);text.resizeTextForBestFit=true;text.resizeTextMinSize=19;text.resizeTextMaxSize=22;
        }
        back=UiFactory.Button(sheet,"Back",new Vector2(-300,47),()=>Move(-1),new Vector2(.5f,0),new Vector2(140,48));
        var next=UiFactory.Button(sheet,"Next",new Vector2(300,47),()=>{if(page==Titles.Length-1)Close();else Move(1);},new Vector2(.5f,0),new Vector2(140,48));nextLabel=next.GetComponentInChildren<Text>();
        counter=UiFactory.Label(sheet,"",new Vector2(.5f,0),new Vector2(0,52),new Vector2(150,30),19);counter.color=new Color(.3f,.35f,.3f);
        UiFactory.Button(sheet,"Skip / Close",new Vector2(0,22),Close,new Vector2(.5f,0),new Vector2(145,26));
        Render();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(next.gameObject);
    }
    private void Move(int amount){page=Mathf.Clamp(page+amount,0,Titles.Length-1);Render();}
    private void Render(){title.text=Titles[page];body.text=Bodies[page];legend.SetActive(page==3);body.gameObject.SetActive(page!=3);back.interactable=page>0;nextLabel.text=page==Titles.Length-1?"Close":"Next";counter.text=(page+1)+" / "+Titles.Length;}
    private void Update(){var parent=(RectTransform)transform;float scale=Mathf.Min(1,Mathf.Min((parent.rect.width-32)/900f,(parent.rect.height-32)/620f));sheet.localScale=Vector3.one*Mathf.Max(.1f,scale);if(Input.GetKeyDown(KeyCode.Escape))Close();}
    private void Close(){if(finished)return;finished=true;PlayerPrefs.SetInt(SeenKey,1);PlayerPrefs.Save();gameObject.SetActive(false);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(previousSelection);var callback=closed;Destroy(gameObject);callback?.Invoke();}
    private void OnDisable(){if(active==this){active=null;closedFrame=Time.frameCount;}}
    private void OnDestroy(){if(active==this)active=null;}
}
}
