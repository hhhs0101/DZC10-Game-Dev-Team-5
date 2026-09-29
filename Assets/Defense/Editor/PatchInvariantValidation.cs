using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace Defense.Editor {
// Focused data-invariant regression tests, called by the existing Play Mode runner.
public static class PatchInvariantValidation {
    public static void Run(GameCatalog catalog, Action<bool,string> check) {
        bool duplicateRejected = false;
        try { new PlayerDeck(new[]{catalog.InitialDeck[0],catalog.InitialDeck[1],catalog.InitialDeck[2],catalog.InitialDeck[0],catalog.InitialDeck[4],catalog.InitialDeck[5]}); }
        catch (ArgumentException) { duplicateRejected = true; }
        check(duplicateRejected,"PlayerDeck constructor rejects duplicate definitions");
        var deck = new PlayerDeck(catalog.InitialDeck);
        var editor = new DeckEditorController(deck,catalog.AvailableCards);
        var snapshot = deck.Slots.ToArray(); int notifications = 0;
        deck.Changed += ()=>notifications++;
        check(!editor.Replace(2,deck.Slots[0]) && deck.Slots.SequenceEqual(snapshot) && notifications==0,"Duplicate edit is rejected atomically without Changed notification");
        var replace = typeof(PlayerDeck).GetMethod("Replace",BindingFlags.Instance|BindingFlags.NonPublic);
        check(!(bool)replace.Invoke(deck,new object[]{2,deck.Slots[0]}) && deck.Slots.SequenceEqual(snapshot),"PlayerDeck itself rejects duplicates even if controller is bypassed");
        check(editor.Available.Count==11 && !editor.Available.Intersect(deck.Slots).Any(),"Available is seventeen catalog definitions minus six deck definitions");
        for (int i=0;i<24;i++) {
            int slot = i%6; var incoming = editor.Available[i%editor.Available.Count]; var outgoing = deck.Slots[slot];
            check(editor.Replace(slot,incoming) && deck.Slots.Count==6 && deck.Slots.Distinct().Count()==6 && editor.Available.Contains(outgoing) && !editor.Available.Contains(incoming)
                && editor.Available.SequenceEqual(catalog.AvailableCards.Where(t=>!deck.Slots.Contains(t))),"Available and unique deck stay synchronized after replacement "+i);
        }
        check(notifications==24,"Every successful edit sends exactly one update");
        bool allUnique = true;
        for (int seed=0;seed<20;seed++) {
            var cycle = new RuntimeCardCycle(deck,new System.Random(seed));
            for (int i=0;i<60;i++) {
                allUnique &= cycle.Hand.Count==3 && cycle.UpcomingQueue.Count==3 && cycle.Hand.Select(c=>c.Definition).Distinct().Count()==3
                    && new HashSet<CardDefinition>(cycle.Hand.Concat(cycle.UpcomingQueue).Select(c=>c.Definition)).SetEquals(deck.Slots);
                cycle.Use(cycle.Hand[i%3]);
            }
        }
        check(allUnique,"Twenty shuffles and 1200 rotations preserve the same six unique towers and unique Hand");
        // Simulate an unexpected internal bug and verify defensive validation runs before mutation.
        var broken = new RuntimeCardCycle(deck,new System.Random(1));
        var rawHand = (List<RuntimeCard>)typeof(RuntimeCardCycle).GetField("hand",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(broken);
        rawHand[1] = new RuntimeCard(99,rawHand[0].Definition);
        var before = broken.Hand.ToArray(); var queue = broken.UpcomingQueue.ToArray();
        bool caught = false;
        try { broken.Use(rawHand[0]); } catch (InvalidOperationException) { caught = true; }
        check(caught && broken.Hand.SequenceEqual(before) && broken.UpcomingQueue.SequenceEqual(queue),"Corrupted duplicate cycle fails before any rotation");
    }
}
}
