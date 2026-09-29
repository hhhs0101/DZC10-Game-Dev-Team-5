using UnityEngine;
using UnityEngine.UI;
namespace Defense {
[RequireComponent(typeof(SettingsUI), typeof(BuildMenuUI))]
public sealed class GameplayUI : MonoBehaviour {
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameFlow flow;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private ResourceWallet wallet;
    [SerializeField] private TurretPlacementController placement;
    private Text hpLabel, resourcesLabel, messageLabel;
    private GameObject pause, confirmation, gameOver;
    private SettingsUI settings;
    private Button pauseButton;
    private FadingMessageUI feedback;
    private GameplayHand hand;
    private void Start() {
        Transform root = canvas.transform;
        hpLabel = UiFactory.Label(root,"",new Vector2(0,1),new Vector2(120,-35),new Vector2(220,50));
        resourcesLabel = UiFactory.Label(root,"",new Vector2(0,1),new Vector2(370,-35),new Vector2(260,50));
        pauseButton = UiFactory.Button(root,"Pause",new Vector2(-90,-35),flow.TogglePause,new Vector2(1,1),new Vector2(140,48));
        messageLabel = UiFactory.Label(root,"",new Vector2(.5f,0),new Vector2(0,163),new Vector2(950,50));
        feedback = messageLabel.gameObject.AddComponent<FadingMessageUI>();
        hand = placement.GetComponent<GameplayHand>();
        hand.Message += ShowMessage;
        gameObject.AddComponent<ElixirBarUI>().Initialize(root,placement.GetComponent<ElixirSystem>());
        GetComponent<BuildMenuUI>().Initialize(root);
        pause = Overlay("Pause",root);
        UiFactory.Label(pause.transform,"Paused",new Vector2(.5f,.5f),new Vector2(0,150),new Vector2(500,60),36);
        UiFactory.Button(pause.transform,"Resume",new Vector2(0,50),flow.TogglePause);
        UiFactory.Button(pause.transform,"Settings",new Vector2(0,-20),() => { pause.SetActive(false); settings.Open(() => pause.SetActive(true)); });
        UiFactory.Button(pause.transform,"Exit",new Vector2(0,-90),() => { pause.SetActive(false); confirmation.SetActive(true); });
        confirmation = Overlay("Exit Confirmation",root);
        UiFactory.Label(confirmation.transform,"Really exit this stage?",new Vector2(.5f,.5f),new Vector2(0,90),new Vector2(600,60),32);
        UiFactory.Button(confirmation.transform,"Yes",new Vector2(0,0),flow.ExitToMenu);
        UiFactory.Button(confirmation.transform,"No",new Vector2(0,-70),() => { confirmation.SetActive(false); pause.SetActive(true); });
        gameOver = Overlay("Game Over",root);
        UiFactory.Label(gameOver.transform,"Game Over",new Vector2(.5f,.5f),new Vector2(0,90),new Vector2(600,70),42);
        UiFactory.Button(gameOver.transform,"Retry",new Vector2(0,0),flow.Retry);
        UiFactory.Button(gameOver.transform,"Lobby",new Vector2(0,-70),flow.ExitToMenu);
        settings = GetComponent<SettingsUI>(); settings.Initialize(root);
        baseHealth.Changed += UpdateHealth; wallet.Changed += UpdateResources;
        flow.Changed += UpdateState; placement.Message += ShowMessage;
        UpdateHealth(baseHealth.Current); UpdateResources(wallet.Balance); UpdateState(flow.State);
    }
    private static GameObject Overlay(string name, Transform parent) => UiFactory.Panel(name,parent,new Color(0,0,0,.78f));
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) flow.TogglePause();

    }
    private void UpdateHealth(int health) { hpLabel.text = "Base HP: " + health; }
    private void UpdateResources(int resources) { resourcesLabel.text = "Resources: " + resources; }
    private void ShowMessage(string message) { feedback.Show(message,message == GameplayHand.InsufficientElixirMessage ? 1.5f : 3); }
    private void UpdateState(GameplayState state) {
        GetComponent<BuildMenuUI>().SetInteractionEnabled(state == GameplayState.Playing);
        settings.Hide(); confirmation.SetActive(false);
        pause.SetActive(state == GameplayState.Paused); gameOver.SetActive(state == GameplayState.GameOver);
        pauseButton.interactable = state != GameplayState.GameOver;
    }
    private void OnDestroy() {
        if (baseHealth != null) baseHealth.Changed -= UpdateHealth;
        if (wallet != null) wallet.Changed -= UpdateResources;
        if (flow != null) flow.Changed -= UpdateState;
        if (placement != null) placement.Message -= ShowMessage;
        if (hand != null) hand.Message -= ShowMessage;
    }
}
}
