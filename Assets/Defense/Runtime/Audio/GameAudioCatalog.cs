using UnityEngine;
namespace Defense {
public sealed class GameAudioCatalog : ScriptableObject {
    [Header("BGM")]
    public AudioClip introTheme;
    public AudioClip homeBgm;
    public AudioClip stageBgm;
    public AudioClip gameOverBgm;
    public AudioClip victoryBgm;

    [Header("SFX")]
    public AudioClip buttonClick;
    public AudioClip cardUse;
    public AudioClip ketchupMayoMustard;
    public AudioClip oilSoya;
    public AudioClip paprika;
    public AudioClip pepper;
    public AudioClip salt;
    public AudioClip enemyAliveReaching;
    public AudioClip enemyDeadReaching;
    public AudioClip enemyNoHp;
}
}

