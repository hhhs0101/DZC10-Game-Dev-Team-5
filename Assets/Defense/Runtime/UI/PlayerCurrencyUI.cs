using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Observes the persistent wallet; never owns balances or performs disk I/O.
public sealed class PlayerCurrencyUI : MonoBehaviour {
    private PlayerCurrency currency;
    private Text label;
    public void Initialize(PlayerCurrency owner) {
        currency = owner;
        label = UiFactory.Label(transform,"",new Vector2(.5f,.5f),Vector2.zero,((RectTransform)transform).sizeDelta,21);
        currency.Changed += Refresh; Refresh();
    }
    private void Refresh() => label.text = "Gold: "+currency.Gold+"   Candy: "+currency.Candy;
    private void OnDestroy() { if (currency != null) currency.Changed -= Refresh; }
}
}
