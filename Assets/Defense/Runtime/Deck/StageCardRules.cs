using System;
using System.Collections.Generic;
namespace Defense {
public static class StageCardRules {
    public static IReadOnlyList<CardDefinition> Available(StageDefinition stage, GameCatalog catalog) => stage != null && stage.HasCardRestrictions ? stage.AllowedCards : catalog.AvailableCards;
    public static IReadOnlyList<CardDefinition> Defaults(StageDefinition stage, GameCatalog catalog) => stage != null && stage.HasCardRestrictions ? stage.DefaultDeck : catalog.InitialDeck;
    public static void Validate(StageDefinition stage, GameCatalog catalog) {
        var available = Available(stage,catalog);
        var known = new HashSet<CardDefinition>(catalog.AvailableCards);
        var unique = new HashSet<CardDefinition>();
        foreach(var card in available) if(card == null || !known.Contains(card) || !unique.Add(card))
            throw new InvalidOperationException("Stage card pool contains a missing, duplicate or unknown card.");
        var defaults = new PlayerDeck(Defaults(stage,catalog));
        if(stage != null && stage.HasCardRestrictions && defaults.Slots.Count != Math.Min(PlayerDeck.SlotCount,unique.Count))
            throw new InvalidOperationException("Stage default deck must use the available cards up to the six-card limit.");
        foreach(var card in defaults.Slots) if(!unique.Contains(card)) throw new InvalidOperationException("Stage default deck contains a locked card.");
    }
    public static string SavePath(StageDefinition stage,string basePath) {
        if(stage == null || !stage.HasCardRestrictions) return basePath;
        return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(basePath) ?? "",
            System.IO.Path.GetFileNameWithoutExtension(basePath)+"-"+stage.name+System.IO.Path.GetExtension(basePath));
    }
    public static bool Allows(StageDefinition stage, CardDefinition card) {
        if(stage == null || !stage.HasCardRestrictions) return true;
        foreach(var allowed in stage.AllowedCards) if(allowed == card) return true;
        return false;
    }
}
}
