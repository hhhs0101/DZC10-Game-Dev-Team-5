using UnityEngine;
namespace Defense {
// Reuses Hand/Elixir presentation without opening persistent currency, Lobby or result flow.
[RequireComponent(typeof(BuildMenuUI))]
public sealed class TopDownInteractionHUD : MonoBehaviour {
    [SerializeField] private Canvas canvas;
    private UnityEngine.UI.Text status;
    private FadingMessageUI feedback;
    private GameFlow flow;
    private void Start() {
        flow=GetComponent<GameFlow>();
        status=UiFactory.Label(canvas.transform,"",new Vector2(.5f,1),new Vector2(0,-30),new Vector2(900,50));
        UiFactory.Button(canvas.transform,"Pause",new Vector2(-85,-30),flow.TogglePause,new Vector2(1,1),new Vector2(140,45));
        GetComponent<BuildMenuUI>().Initialize(canvas.transform);
        gameObject.AddComponent<ElixirBarUI>().Initialize(canvas.transform,GetComponent<ElixirSystem>());
        feedback=UiFactory.Label(canvas.transform,"",new Vector2(.5f,0),new Vector2(0,165),new Vector2(900,40)).gameObject.AddComponent<FadingMessageUI>();
        GetComponent<GameplayHand>().Message+=Show;GetComponent<TurretPlacementController>().Message+=Show;
    }
    private void Show(string text)=>feedback.Show(text,2);
    private void Update() {
        if(flow==null)return;
        if(Input.GetKeyDown(KeyCode.Escape))flow.TogglePause();
        status.text="Base HP: "+GetComponent<BaseHealth>().Current+"     Resources: "+GetComponent<ResourceWallet>().Balance+"     "+flow.State;
        GetComponent<BuildMenuUI>().SetInteractionEnabled(flow.IsPlaying);
    }
    private void OnDestroy() {GetComponent<GameplayHand>().Message-=Show;GetComponent<TurretPlacementController>().Message-=Show;}
}
}
