using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/UI/Result Menu Skin")]
public sealed class ResultMenuSkin : ScriptableObject {
    [SerializeField] private MainMenuSkin.ButtonSprites replay;
    [SerializeField] private MainMenuSkin.ButtonSprites retry;
    [SerializeField] private MainMenuSkin.ButtonSprites lobby;
    [SerializeField] private MainMenuSkin.ButtonSprites nextStage;
    public MainMenuSkin.ButtonSprites Replay => replay;
    public MainMenuSkin.ButtonSprites Retry => retry;
    public MainMenuSkin.ButtonSprites Lobby => lobby;
    public MainMenuSkin.ButtonSprites NextStage => nextStage;
}
}
