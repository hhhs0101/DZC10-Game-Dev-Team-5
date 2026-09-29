using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class DeckEditorUI : MonoBehaviour {
    private PlayerDeck deck;
    private FadingMessageUI warning;
    private DeckEditorController editor;
    private RectTransform content;
    private readonly Dictionary<CardDefinition, RectTransform> availableViews = new Dictionary<CardDefinition, RectTransform>();
    private readonly Text[] labels = new Text[6];
    public void Initialize(RectTransform canvas, Action back, Action settings) {
        deck = PlayerSession.Deck;
        editor = new DeckEditorController(deck,PlayerSession.Catalog.AvailableCards);
        UiFactory.Button(transform,"Back to Lobby",new Vector2(140,-40),()=>back(),new Vector2(0,1),new Vector2(250,48));
        UiFactory.Button(transform,"Settings",new Vector2(-100,-40),()=>settings(),new Vector2(1,1),new Vector2(180,48));
        UiFactory.Label(transform,"Available Cards",new Vector2(.25f,1),new Vector2(0,-105),new Vector2(500,55),30);
        UiFactory.Label(transform,"Current Deck",new Vector2(.75f,1),new Vector2(0,-105),new Vector2(500,55),30);
        var divider = UiFactory.Rect("Book Divider",transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(3,550));
        divider.gameObject.AddComponent<Image>().color = new Color(.4f,.45f,.5f);
        var viewport = UiFactory.Rect("Available Cards List",transform,new Vector2(.25f,.5f),new Vector2(0,-25),new Vector2(540,450));
        viewport.gameObject.AddComponent<Image>().color = new Color(.1f,.14f,.2f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        content = UiFactory.Rect("Available Cards",viewport,new Vector2(.5f,1),Vector2.zero,new Vector2(530,Mathf.Max(450,Mathf.CeilToInt(editor.Available.Count/3f)*140)));
        content.pivot = new Vector2(.5f,1); scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        for (int i=0;i<PlayerSession.Catalog.AvailableCards.Count;i++) {
            var definition = PlayerSession.Catalog.AvailableCards[i];
            if (definition == null || availableViews.ContainsKey(definition)) continue;
            var card = UiFactory.Rect("Available "+definition.DisplayName,content,new Vector2(.5f,1),new Vector2((i%3-1)*175,-75-i/3*140),new Vector2(165,120));
            card.gameObject.AddComponent<Image>().color = new Color(.2f,.32f,.4f);
            UiFactory.Label(card,definition.DisplayName+"\n"+definition.CostDescription,new Vector2(.5f,.5f),Vector2.zero,new Vector2(160,115),19);
            card.gameObject.AddComponent<DeckCardDrag>().Initialize(definition,canvas);
            availableViews.Add(definition,card);
        }
        for (int i=0;i<6;i++) {
            var card = UiFactory.Rect("Deck Slot T"+(i+1),transform,new Vector2(.75f,1),new Vector2((i%2-.5f)*240,-235-i/2*155),new Vector2(220,135));
            card.gameObject.AddComponent<Image>().color = new Color(.2f,.28f,.25f);
            labels[i] = UiFactory.Label(card,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(215,130),23);
            card.gameObject.AddComponent<DeckSlotDrop>().Initialize(editor,i);
        }
        UiFactory.Label(transform,"Drag an available card onto T1-T6. Changes apply immediately.",new Vector2(.5f,0),new Vector2(0,35),new Vector2(1100,50),20);
        warning = UiFactory.Label(transform,"",new Vector2(.5f,0),new Vector2(0,80),new Vector2(1100,45),22).gameObject.AddComponent<FadingMessageUI>();
        deck.Rejected += ShowWarning;
        deck.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        for (int i=0;i<6;i++) labels[i].text = "T"+(i+1)+"\n"+deck.Slots[i].DisplayName;
        var available = editor.Available;
        var visible = new HashSet<CardDefinition>(available);
        // Reuse views. Hiding a consumed source safely cancels its drag ghost during OnDrop.
        foreach (var pair in availableViews) pair.Value.gameObject.SetActive(visible.Contains(pair.Key));
        for (int i=0;i<available.Count;i++)
            availableViews[available[i]].anchoredPosition = new Vector2((i%3-1)*175,-75-i/3*140);
        content.sizeDelta = new Vector2(530,Mathf.Max(450,Mathf.CeilToInt(available.Count/3f)*140));
    }
    private void ShowWarning(string message) => warning.Show(message,3);
    private void OnDestroy() { if (deck != null) { deck.Changed -= Refresh; deck.Rejected -= ShowWarning; } }
}
}
