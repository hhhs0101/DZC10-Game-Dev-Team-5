using System;
using System.IO;
using UnityEngine;
namespace Defense {
[Serializable]
public sealed class PlayerCurrencySaveData {
    public int Version;
    public int Gold;
    public int Candy;
}
// Persistent player economy, separate from the stage-only ResourceWallet and Elixir.
public sealed class PlayerCurrency {
    public const string FileName = "player-currency.json";
    private readonly JsonFileStore<PlayerCurrencySaveData> storage;
    public int Gold { get; private set; }
    public int Candy { get; private set; }
    public bool LastSaveSucceeded { get; private set; } = true;
    public string SavePath => storage.SavePath;
    public event Action Changed;
    public static string DefaultPath {
        get {
#if UNITY_EDITOR
            var isolated = Environment.GetEnvironmentVariable("DEFENSE_TEST_CURRENCY_SAVE_PATH");
            if (!string.IsNullOrWhiteSpace(isolated)) return isolated;
            var deckTest = Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
            if (!string.IsNullOrWhiteSpace(deckTest)) return Path.Combine(Path.GetDirectoryName(deckTest),FileName);
#endif
            return Path.Combine(Application.persistentDataPath,FileName);
        }
    }
    public PlayerCurrency(JsonFileStore<PlayerCurrencySaveData> persistence,Action<string> logWarning = null) {
        storage = persistence;
        var warning = logWarning ?? (message=>Debug.LogWarning(message));
        var data = storage.Load();
        bool repair = data == null;
        if (data != null && data.Version != 1) {
            warning("Unsupported currency save version; restoring zero balances."); data = null; repair = true;
        }
        if (data != null) {
            Gold = Math.Max(0,data.Gold); Candy = Math.Max(0,data.Candy);
            if (Gold != data.Gold || Candy != data.Candy) { warning("Negative currency balances repaired to zero."); repair = true; }
        }
        if (repair) Save();
    }
    public bool AddGold(int amount) {
        if (amount < 0 || amount > int.MaxValue-Gold) return false;
        if (amount == 0) return true;
        Gold += amount; Commit(); return true;
    }
    public bool AddCandy(int amount) {
        if (amount < 0 || amount > int.MaxValue-Candy) return false;
        if (amount == 0) return true;
        Candy += amount; Commit(); return true;
    }
    public bool TrySpendGold(int amount) {
        if (amount < 0 || amount > Gold) return false;
        if (amount == 0) return true;
        Gold -= amount; Commit(); return true;
    }
    public bool TrySpendCandy(int amount) {
        if (amount < 0 || amount > Candy) return false;
        if (amount == 0) return true;
        Candy -= amount; Commit(); return true;
    }
    private void Commit() { Save(); Changed?.Invoke(); }
    private void Save() => LastSaveSucceeded = storage.Save(new PlayerCurrencySaveData { Version=1,Gold=Gold,Candy=Candy });
}
}
