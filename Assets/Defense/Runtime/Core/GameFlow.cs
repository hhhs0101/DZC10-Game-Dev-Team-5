using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense {
public enum GameplayState { Playing, Paused, GameOver, Victory, ConfigurationError }
public sealed class GameFlow : MonoBehaviour {
    [SerializeField] private string mainMenuScene = "MainMenu";
    public GameplayState State { get; private set; } = GameplayState.Playing;
    private bool reloading;
    public string ConfigurationErrorMessage { get; private set; }
    public bool IsTerminal => State == GameplayState.GameOver || State == GameplayState.Victory || State == GameplayState.ConfigurationError;
    public bool IsPlaying => State == GameplayState.Playing;
    public event Action<GameplayState> Changed;
    private void Awake() { Time.timeScale = 1; }
    public void TogglePause() {
        if (IsTerminal) return;
        SetState(IsPlaying ? GameplayState.Paused : GameplayState.Playing);
    }
    public void GameOver() { if (!IsTerminal) SetState(GameplayState.GameOver); }
    public void FailConfiguration(string message) {
        if(IsTerminal) return;
        ConfigurationErrorMessage = message;
        SetState(GameplayState.ConfigurationError);
    }
    internal bool TryVictory() {
        if (!IsPlaying) return false;
        SetState(GameplayState.Victory); return true;
    }
    private void SetState(GameplayState state) {
        if (State == state) return;
        State = state;
        Time.timeScale = IsPlaying ? 1 : 0;
        Changed?.Invoke(State);
    }
    public void Retry() {
        if (!IsTerminal || reloading) return;
        reloading = true;
        Time.timeScale = 1;
        // Reload only scene-owned runtime state. ActiveStage, Selection, Deck and Progression stay intact.
        SceneManager.LoadScene(gameObject.scene.name);
    }
    public void ExitToMenu() {
        Time.timeScale = 1;
        PlayerSession.ReturnToLobby = true;
        SceneManager.LoadScene(mainMenuScene);
    }
    private void OnDestroy() { Time.timeScale = 1; }
}
}
