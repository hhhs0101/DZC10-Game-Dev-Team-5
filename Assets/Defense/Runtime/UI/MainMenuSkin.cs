using System;
using UnityEngine;
namespace Defense {
// Presentation data only; navigation and callbacks remain in MainMenuUI.
[CreateAssetMenu(menuName="Defense/UI/Main Menu Skin")]
public sealed class MainMenuSkin : ScriptableObject {
    [Serializable]
    public sealed class ButtonSprites {
        [SerializeField] private Sprite normal;
        [SerializeField] private Sprite highlighted;
        [SerializeField] private Sprite pressed;
        public ButtonSprites() {}
        public ButtonSprites(Sprite normal, Sprite highlighted, Sprite pressed) {
            this.normal = normal;
            this.highlighted = highlighted;
            this.pressed = pressed;
        }
        public Sprite Normal => normal;
        public Sprite Highlighted => highlighted;
        public Sprite Pressed => pressed;
    }
    [Serializable]
    public sealed class StageBoardSprites {
        [SerializeField] private Sprite selectedCenter;
        [SerializeField] private Sprite unselectedSide;
        public Sprite SelectedCenter => selectedCenter;
        public Sprite UnselectedSide => unselectedSide;
    }
    [SerializeField, Tooltip("Art rows for the first catalog entries only. Later stages keep their existing cards.")] private StageBoardSprites[] stageBoards = Array.Empty<StageBoardSprites>();
    public Sprite GetStageBoard(int stageIndex, bool isCentered) {
        if (stageBoards == null || stageIndex < 0 || stageIndex >= stageBoards.Length || stageBoards[stageIndex] == null) return null;
        return isCentered ? stageBoards[stageIndex].SelectedCenter : stageBoards[stageIndex].UnselectedSide;
    }
    [SerializeField, Tooltip("Full-screen Main Menu and Lobby background, fitted without stretching.")] private Sprite background;
    [SerializeField] private Sprite logo;
    [SerializeField] private ButtonSprites startGame;
    [SerializeField] private ButtonSprites settings;
    [SerializeField] private ButtonSprites exit;
    [SerializeField] private ButtonSprites lobbyBack;
    [SerializeField] private ButtonSprites lobbySettings;
    [SerializeField] private ButtonSprites lobbyEnterStage;
    [SerializeField] private ButtonSprites lobbyDeck;
    public Sprite Background => background;
    public Sprite Logo => logo;
    public ButtonSprites StartGame => startGame;
    public ButtonSprites Settings => settings;
    public ButtonSprites Exit => exit;
    public ButtonSprites LobbyBack => lobbyBack;
    public ButtonSprites LobbySettings => lobbySettings;
    public ButtonSprites LobbyEnterStage => lobbyEnterStage;
    public ButtonSprites LobbyDeck => lobbyDeck;
}
}
