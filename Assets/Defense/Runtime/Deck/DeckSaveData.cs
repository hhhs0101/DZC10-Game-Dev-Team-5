using System;
namespace Defense {
[Serializable]
public sealed class DeckSaveData {
    public int Version;
    public int ActivePresetIndex;
    public DeckPresetData[] Presets;
}
[Serializable]
public sealed class DeckPresetData {
    public string[] CardIds;
}
}
