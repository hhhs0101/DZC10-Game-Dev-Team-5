using UnityEngine;
using UnityEngine.UI;
namespace Defense {
[RequireComponent(typeof(SettingsUI), typeof(BuildMenuUI))]
public sealed class GameplayUI : MonoBehaviour {
    [SerializeField] private Canvas canvas;
    [SerializeField, Tooltip("Artwork for Resume / Settings / Exit inside the Pause overlay only.")] private PauseMenuSkin pauseSkin;
    [SerializeField, Tooltip("Replay / Retry / Lobby artwork for result overlays only.")] private ResultMenuSkin resultSkin;
    [SerializeField] private GameFlow flow;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private ResourceWallet wallet;
    [SerializeField] private TurretPlacementController placement;
    private Text hpLabel, remainingEnemiesLabel, messageLabel;
    private GameObject pause, gameOver, victory, configurationError;
    private UnityEngine.UI.Text configurationDetails;
    private SettingsUI settings;
    private Button pauseButton;
    private FadingMessageUI feedback;
    private GameplayHand hand;
    private FixedIntervalSpawner schedule;
    private EnemyRegistry enemies;
    private int lastRemainingEnemies = -1;
    private void Start() {
        Transform root = canvas.transform;
        schedule = GetComponent<FixedIntervalSpawner>();
        enemies = GetComponent<EnemyRegistry>();
        var sidebar = UiFactory.Rect("Right HUD Sidebar",root,Vector2.zero,Vector2.zero,Vector2.zero);
        sidebar.anchorMin = new Vector2(1,0); sidebar.anchorMax = new Vector2(1,1);
        sidebar.pivot = new Vector2(1,.5f); sidebar.anchoredPosition = Vector2.zero; sidebar.sizeDelta = new Vector2(220,0);
        var sidebarImage = sidebar.gameObject.AddComponent<Image>();
        sidebarImage.sprite = Resources.Load<Sprite>("UI/SidebarBanner");
        sidebarImage.color = Color.white;
        sidebarImage.preserveAspect = false;
        var bannerRect = UiFactory.Rect("Top Status Banner",root,new Vector2(.414f,1),new Vector2(0,-48),new Vector2(680,96));
        var bannerImage = bannerRect.gameObject.AddComponent<Image>();
        bannerImage.sprite = Resources.Load<Sprite>("UI/TopStatusBanner");
        bannerImage.preserveAspect = true;
        bannerImage.raycastTarget = false;
        bannerImage.color = bannerImage.sprite != null ? Color.white : new Color(.38f,.24f,.12f,.95f);
        hpLabel = UiFactory.Label(bannerRect,"",new Vector2(.5f,.5f),new Vector2(-140,1),new Vector2(260,48),25);
        StyleBannerLabel(hpLabel);
        remainingEnemiesLabel = UiFactory.Label(bannerRect,"",new Vector2(.5f,.5f),new Vector2(135,1),new Vector2(280,48),25);
        StyleBannerLabel(remainingEnemiesLabel);
        pauseButton = UiFactory.Button(root,"Pause",new Vector2(-110,-34),flow.TogglePause,new Vector2(1,1),new Vector2(176,42));
        messageLabel = UiFactory.Label(root,"",new Vector2(.414f,0),new Vector2(0,22),new Vector2(760,38),20);
        AddOutline(messageLabel);
        feedback = messageLabel.gameObject.AddComponent<FadingMessageUI>();
        hand = placement.GetComponent<GameplayHand>();
        hand.Message += ShowMessage;
        gameObject.AddComponent<ElixirBarUI>().Initialize(root,placement.GetComponent<ElixirSystem>());
        GetComponent<BuildMenuUI>().Initialize(root);
        pause = Overlay("Pause",root);
        var pauseTitle = UiFactory.Label(pause.transform,"Paused",new Vector2(.5f,.5f),new Vector2(0,150),new Vector2(500,60),36);
        var resumeButton = UiFactory.Button(pause.transform,"Resume",new Vector2(0,50),flow.TogglePause);
        var pauseSettingsButton = UiFactory.Button(pause.transform,"Settings",new Vector2(0,-20),() => { pause.SetActive(false); settings.Open(() => { pause.SetActive(true); GameAudio.PlayStageBgm(); }); });
        var exitButton = UiFactory.Button(pause.transform,"Exit",new Vector2(0,-90),flow.ExitToMenu);
        if (pauseSkin != null) {
            if (pauseSkin.PauseButton != null && pauseSkin.PauseButton.Normal != null)
                UiFactory.ApplyButtonSkin(pauseButton, pauseSkin.PauseButton);
            pauseTitle.rectTransform.anchoredPosition = new Vector2(0,210);
            SkinMenuButton(resumeButton,pauseSkin.Resume,new Vector2(0,80));
            SkinMenuButton(pauseSettingsButton,pauseSkin.Settings,new Vector2(0,-50));
            SkinMenuButton(exitButton,pauseSkin.Exit,new Vector2(0,-180));
        }
        gameOver = Overlay("Game Over",root);
        var defeatTitle = UiFactory.Label(gameOver.transform,"Game Over",new Vector2(.5f,.5f),new Vector2(0,90),new Vector2(600,70),42);
        var retryButton = UiFactory.Button(gameOver.transform,"Retry",new Vector2(0,0),flow.Retry);
        var defeatLobbyButton = UiFactory.Button(gameOver.transform,"Lobby",new Vector2(0,-70),flow.ExitToMenu);
        victory = Overlay("Victory",root);
        var clearTitle = UiFactory.Label(victory.transform,"Stage Clear",new Vector2(.5f,.5f),new Vector2(0,150),new Vector2(600,70),42);
        var replayButton = UiFactory.Button(victory.transform,"Replay",new Vector2(0,0),flow.Retry);
        var clearLobbyButton = UiFactory.Button(victory.transform,"Return to Lobby",new Vector2(0,-70),flow.ExitToMenu);
        if (resultSkin != null) {
            defeatTitle.rectTransform.anchoredPosition = new Vector2(0,170);
            clearTitle.rectTransform.anchoredPosition = new Vector2(0,220);
            SkinMenuButton(retryButton,resultSkin.Retry,new Vector2(0,20));
            SkinMenuButton(defeatLobbyButton,resultSkin.Lobby,new Vector2(0,-115));
            SkinMenuButton(replayButton,resultSkin.Replay,new Vector2(0,20));
            SkinMenuButton(clearLobbyButton,resultSkin.Lobby,new Vector2(0,-115));
        }
        configurationError = Overlay("Stage Configuration Error",root);
        UiFactory.Label(configurationError.transform,"Stage Configuration Error",new Vector2(.5f,.5f),new Vector2(0,160),new Vector2(850,65),36);
        configurationDetails = UiFactory.Label(configurationError.transform,"",new Vector2(.5f,.5f),new Vector2(0,45),new Vector2(1000,150),20);
        UiFactory.Button(configurationError.transform,"Retry",new Vector2(0,-85),flow.Retry);
        UiFactory.Button(configurationError.transform,"Return to Lobby",new Vector2(0,-155),flow.ExitToMenu);
        settings = GetComponent<SettingsUI>(); settings.Initialize(root);
        baseHealth.Changed += UpdateHealth;
        flow.Changed += UpdateState; placement.Message += ShowMessage;
        UpdateHealth(baseHealth.Current); UpdateRemainingEnemies(); UpdateState(flow.State);
    }
    private static void StyleBannerLabel(Text text) {
        text.font = UiFactory.BannerFont;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(.22f, .09f, .03f, 1f);
        var highlight = text.gameObject.AddComponent<Shadow>();
        highlight.effectColor = new Color(1f, .96f, .88f, .75f);
        highlight.effectDistance = new Vector2(0f, -1.5f);
    }
    private static void AddOutline(Text text) {
        var outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.16f,.07f,.02f,.95f);
        outline.effectDistance = new Vector2(1.5f,-1.5f);
    }
    private static void SkinMenuButton(UnityEngine.UI.Button button, MainMenuSkin.ButtonSprites sprites, Vector2 position) {
        UiFactory.ApplyButtonSkin(button,sprites);
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = position; rect.sizeDelta = new Vector2(360,120);
    }
    private static GameObject Overlay(string name, Transform parent) => UiFactory.Panel(name,parent,new Color(0,0,0,.78f));
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) flow.TogglePause();
        UpdateRemainingEnemies();
    }
    private void UpdateRemainingEnemies() {
        if (remainingEnemiesLabel == null) return;
        int unspawned = schedule != null && schedule.PlannedSpawnCount > 0
            ? Mathf.Max(0, schedule.PlannedSpawnCount - schedule.SpawnedCount)
            : 0;
        int alive = enemies != null ? enemies.AliveEnemyCount : 0;
        int remaining = unspawned + alive;
        if (remaining != lastRemainingEnemies) {
            lastRemainingEnemies = remaining;
            remainingEnemiesLabel.text = "Remain Enemies: " + remaining;
        }
    }
    private void UpdateHealth(int health) { hpLabel.text = "Base HP: " + health; }
    private void ShowMessage(string message) { feedback.Show(message,message == GameplayHand.InsufficientElixirMessage ? 1.5f : 3); }
    private void UpdateState(GameplayState state) {
        GetComponent<BuildMenuUI>().SetInteractionEnabled(state == GameplayState.Playing);
        settings.Hide();
        pause.SetActive(state == GameplayState.Paused); gameOver.SetActive(state == GameplayState.GameOver);
        victory.SetActive(state == GameplayState.Victory);
        configurationError.SetActive(state == GameplayState.ConfigurationError);
        if(state == GameplayState.ConfigurationError) configurationDetails.text = flow.ConfigurationErrorMessage;
        pauseButton.interactable = !flow.IsTerminal;
        if (state == GameplayState.Playing || state == GameplayState.Paused) GameAudio.PlayStageBgm();
        else if (state == GameplayState.GameOver) GameAudio.PlayGameOverBgm();
        else if (state == GameplayState.Victory) GameAudio.PlayVictoryBgm();
    }
    private void OnDestroy() {
        if (baseHealth != null) baseHealth.Changed -= UpdateHealth;
        if (flow != null) flow.Changed -= UpdateState;
        if (placement != null) placement.Message -= ShowMessage;
        if (hand != null) hand.Message -= ShowMessage;
    }
}
}
