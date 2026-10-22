using UnityEngine;
namespace Defense {
// Shares the existing button-art composition without coupling gameplay to menu navigation.
[CreateAssetMenu(menuName="Defense/UI/Pause Menu Skin")]
public sealed class PauseMenuSkin : ScriptableObject {
    [SerializeField] private MainMenuSkin.ButtonSprites resume;
    [SerializeField] private MainMenuSkin.ButtonSprites settings;
    [SerializeField] private MainMenuSkin.ButtonSprites exit;
    public MainMenuSkin.ButtonSprites Resume => resume;
    public MainMenuSkin.ButtonSprites Settings => settings;
    public MainMenuSkin.ButtonSprites Exit => exit;
}
}
