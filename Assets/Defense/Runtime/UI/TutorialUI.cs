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
    private static readonly string[] Titles={"Protect the Soup","Defense","Vegetables","Towers"};
    private static readonly string[] Bodies={
        "Stop the vegetables before they reach your base.\n\nEnemies that reach the end take away Base HP. Clear the stage by surviving the waves and defeating the remaining enemies.\n\nStart with Stage 1, then unlock the later stages.",
        "Each stage starts with a premade deck. When there are more cards unlocked than available slots for the deck, you can change the cards from the premade deck (Stage 3).\n\nIn battle, select a tower card from your hand, then click or drag to an empty placement plate. The lightning number is its energy cost. Wait for energy to refill if you cannot afford it.\n\nUsing a card cycles it through your deck.",
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
        sheet=UiFactory.Rect("Tutorial Page",transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(920,630));
        var sheetImage=sheet.gameObject.AddComponent<Image>();
        var woodSprite=Resources.Load<Sprite>("UI/TowerCardsWoodPanel");
        if(woodSprite!=null){sheetImage.sprite=woodSprite;sheetImage.color=Color.white;}
        else{sheetImage.color=new Color(.96f,.93f,.84f);}
        sheetImage.raycastTarget=true;

        var innerPanel=UiFactory.Rect("Inner Backdrop",sheet,new Vector2(.5f,.5f),new Vector2(0,16),new Vector2(836,490));
        var innerImage=innerPanel.gameObject.AddComponent<Image>();
        innerImage.color=new Color(0.98f,0.95f,0.88f,0.94f);
        innerImage.raycastTarget=false;

        title=UiFactory.Label(sheet,"",new Vector2(.5f,1),new Vector2(0,-92),new Vector2(800,56),34);
        title.color=new Color(.12f,.2f,.18f);
        title.fontStyle=FontStyle.Bold;

        body=UiFactory.Label(sheet,"",new Vector2(.5f,1),new Vector2(0,-310),new Vector2(788,360),24);
        body.color=new Color(.16f,.22f,.2f);
        body.alignment=TextAnchor.UpperLeft;
        body.resizeTextForBestFit=true;
        body.resizeTextMinSize=19;
        body.resizeTextMaxSize=24;
        body.verticalOverflow=VerticalWrapMode.Truncate;

        legend=UiFactory.Rect("Class Legend",sheet,new Vector2(.5f,1),new Vector2(0,-310),new Vector2(800,360)).gameObject;
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
            var row=UiFactory.Rect("Legend "+i,legend.transform,new Vector2(.5f,1),new Vector2(0,-28-i*58),new Vector2(788,56));
            var iconRect=UiFactory.Rect("Icon",row,new Vector2(0,.5f),new Vector2(25,0),new Vector2(38,38));
            iconRect.gameObject.AddComponent<CanvasRenderer>();
            var symbol=iconRect.gameObject.AddComponent<CardClassSymbol>();
            symbol.SetKind(classes[i]);
            symbol.color=new Color(.14f,.3f,.25f);
            symbol.raycastTarget=false;
            var text=UiFactory.Label(row,descriptions[i],new Vector2(0,.5f),new Vector2(430,0),new Vector2(704,56),21);
            text.alignment=TextAnchor.MiddleLeft;
            text.color=new Color(.16f,.22f,.2f);
            text.resizeTextForBestFit=true;
            text.resizeTextMinSize=18;
            text.resizeTextMaxSize=21;
        }

        back=UiFactory.Button(sheet,"Back",new Vector2(-280,48),()=>Move(-1),new Vector2(.5f,0),new Vector2(150,54));
        SkinTutorialButton(back,"back");

        var skip=UiFactory.Button(sheet,"Skip",new Vector2(0,48),Close,new Vector2(.5f,0),new Vector2(150,54));
        SkinTutorialButton(skip,"skip");

        var next=UiFactory.Button(sheet,"Next",new Vector2(280,48),()=>{if(page==Titles.Length-1)Close();else Move(1);},new Vector2(.5f,0),new Vector2(150,54));
        SkinTutorialButton(next,"next");
        nextLabel=next.GetComponentInChildren<Text>();

        counter=UiFactory.Label(sheet,"",new Vector2(.5f,0),new Vector2(0,108),new Vector2(150,28),18);
        counter.color=new Color(.38f,.34f,.26f);
        counter.fontStyle=FontStyle.Bold;

        Render();
        if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(next.gameObject);
    }
    private static void SkinTutorialButton(Button button,string baseName) {
        var normal=Resources.Load<Sprite>("UI/Tutorial/"+baseName+"_normal");
        var hover=Resources.Load<Sprite>("UI/Tutorial/"+baseName+"_hover");
        var pressed=Resources.Load<Sprite>("UI/Tutorial/"+baseName+"_pressed");
        if(normal==null)return;
        button.image.sprite=normal;
        button.image.color=Color.white;
        button.image.preserveAspect=true;
        button.transition=Selectable.Transition.SpriteSwap;
        button.spriteState=new SpriteState{
            highlightedSprite=hover!=null?hover:normal,
            selectedSprite=hover!=null?hover:normal,
            pressedSprite=pressed!=null?pressed:normal,
            disabledSprite=normal
        };
        foreach(var label in button.GetComponentsInChildren<Text>())label.enabled=false;
    }
    private void Move(int amount){page=Mathf.Clamp(page+amount,0,Titles.Length-1);Render();}
    private void Render(){
        title.text=Titles[page];
        body.text=Bodies[page];
        legend.SetActive(page==3);
        body.gameObject.SetActive(page!=3);
        back.interactable=page>0;
        back.image.color=back.interactable?Color.white:new Color(1f,1f,1f,.45f);
        nextLabel.text=page==Titles.Length-1?"Close":"Next";
        counter.text=(page+1)+" / "+Titles.Length;
    }
    private void Update(){var parent=(RectTransform)transform;float scale=Mathf.Min(1,Mathf.Min((parent.rect.width-32)/920f,(parent.rect.height-32)/630f));sheet.localScale=Vector3.one*Mathf.Max(.1f,scale);if(Input.GetKeyDown(KeyCode.Escape))Close();}
    private void Close(){if(finished)return;finished=true;PlayerPrefs.SetInt(SeenKey,1);PlayerPrefs.Save();gameObject.SetActive(false);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(previousSelection);var callback=closed;Destroy(gameObject);callback?.Invoke();}
    private void OnDisable(){if(active==this){active=null;closedFrame=Time.frameCount;}}
    private void OnDestroy(){if(active==this)active=null;}
}
}
