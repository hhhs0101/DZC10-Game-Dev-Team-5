using UnityEngine;
namespace Defense {
public sealed class DirtProjectile : MonoBehaviour {
    private static Sprite dirtSprite;
    private Tower target;
    private object token;
    private float duration, elapsed;
    private Vector3 start;
    private GameFlow flow;
    public static void Launch(Vector3 position, Tower target, object token, float duration, GameFlow flow) {
        var go = new GameObject("Thrown dirt");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,target.gameObject.scene);
        var renderer = go.AddComponent<SpriteRenderer>();
        if(dirtSprite == null) dirtSprite = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f),16);
        renderer.sprite = dirtSprite; renderer.color = new Color(.35f,.18f,.07f); renderer.sortingOrder = 110;
        go.transform.position = position;
        var shot = go.AddComponent<DirtProjectile>(); shot.start = position; shot.target = target;
        shot.token = token; shot.duration = duration; shot.flow = flow;
    }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime) {
        if(target == null || !target.isActiveAndEnabled || (flow != null && flow.IsTerminal)) { Destroy(gameObject); return; }
        if(flow != null && !flow.IsPlaying) return;
        if(deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        elapsed += deltaTime;
        float t = Mathf.Clamp01(elapsed / .25f);
        transform.position = Vector3.Lerp(start,target.transform.position,t) + Vector3.up * Mathf.Sin(t*Mathf.PI)*.5f;
        if(t < 1) return;
        target.AttackStatus.Apply(token,duration); Destroy(gameObject);
    }
}
}
