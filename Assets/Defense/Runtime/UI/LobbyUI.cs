using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Defense {
public sealed class LobbyUI : MonoBehaviour {
    [SerializeField, Min(.01f)] private float initialHoldDelay = .4f;
    [SerializeField, Min(.01f)] private float repeatInterval = .15f;
    private readonly KeyRepeat repeat = new KeyRepeat();
    private Text feedback;
    public StageCarouselView Carousel { get; private set; }
    public void Initialize(Action openDeck, Action settings, Action back, MainMenuSkin skin = null) {
        UiFactory.Label(transform,"Lobby",new Vector2(.5f,1),new Vector2(0,-45),new Vector2(350,60),36);
        var backButton = UiFactory.Button(transform,"Back",new Vector2(95,-40),()=>back(),new Vector2(0,1),new Vector2(160,48));
        var settingsButton = UiFactory.Button(transform,"Settings",new Vector2(-100,-40),()=>settings(),new Vector2(1,1),new Vector2(180,48));
        if (skin != null) {
            UiFactory.ApplyButtonSkin(backButton,skin.LobbyBack);
            UiFactory.ApplyButtonSkin(settingsButton,skin.LobbySettings);
        }
        var viewport = UiFactory.Rect("Stage Carousel",transform,new Vector2(.5f,1),new Vector2(0,-230),new Vector2(970,290));
        Carousel = viewport.gameObject.AddComponent<StageCarouselView>();
        Carousel.Initialize(PlayerSession.Catalog,PlayerSession.Selection,PlayerSession.Progression,skin);
        feedback = UiFactory.Label(transform,"A / D or click side cards   |   Enter: play",new Vector2(.5f,1),new Vector2(0,-405),new Vector2(850,48),21);
        var enterStageButton = UiFactory.Button(transform,"Enter Stage",new Vector2(0,-460),()=>TryEnter(),new Vector2(.5f,1),new Vector2(420,78));
        var deckButton = UiFactory.Button(transform,"Deck",new Vector2(0,50),()=>openDeck(),new Vector2(.5f,0),new Vector2(420,78));
        if (skin != null) {
            UiFactory.ApplyButtonSkin(enterStageButton,skin.LobbyEnterStage);
            UiFactory.ApplyButtonSkin(deckButton,skin.LobbyDeck);
        }
    }
    private void Update() {
        int direction = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        HandleNavigation(direction,Time.unscaledTime);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) TryEnter();
    }
    public void HandleNavigation(int direction, float time) {
        int move = repeat.Poll(direction,time,initialHoldDelay,repeatInterval);
        if (move < 0) PlayerSession.Selection.MovePrevious();
        else if (move > 0) PlayerSession.Selection.MoveNext();
    }
    public bool TryEnter() {
        if (!Carousel.IsSettled) { feedback.text = "Wait for the selected stage to center."; return false; }
        if (!PlayerSession.TrySelectStageForPlay(out var issue)) { feedback.text = issue; return false; }
        SceneManager.LoadScene(PlayerSession.ActiveBattleScene); return true;
    }
    private void OnDisable() { repeat.Reset(); }
}
}
