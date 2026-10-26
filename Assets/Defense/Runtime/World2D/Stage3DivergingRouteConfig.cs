using UnityEngine;
namespace Defense {
public static class Stage3DivergingRouteConfig {
    public const string ResourceSpritePath = "Kitchen2D/GinghamRouteStage3";
    public const float PixelsPerUnit = 80f;
    public const float HideDurationSeconds = 3f;
    public const string LeftDivergingObjectName = "WaypointPath2D (Stage3 Left Diverge)";
    public const string RightEmergenceObjectName = "WaypointPath2D (Stage3 Right Emerge)";

    public static readonly Vector2[] LeftDivergingPixels = {
        new Vector2(210f, 385f),
        new Vector2(268f, 396f),
        new Vector2(315f, 425f),
        new Vector2(342f, 464f),
        new Vector2(343f, 505f),
        new Vector2(328f, 549f),
        new Vector2(296f, 599f),
        new Vector2(267f, 647f),
        new Vector2(257f, 685f),
        new Vector2(277f, 723f),
        new Vector2(319f, 757f),
        new Vector2(374f, 778f),
        new Vector2(438f, 786f),
        new Vector2(452f, 835f),
        new Vector2(460f, 885f),
        new Vector2(462f, 935f),
        new Vector2(464f, 980f),
        new Vector2(464f, 1024f)
    };

    public static readonly Vector2[] RightEmergencePixels = {
        new Vector2(1230f, 1024f),
        new Vector2(1234f, 980f),
        new Vector2(1248f, 945f),
        new Vector2(1236f, 905f),
        new Vector2(1225f, 860f),
        new Vector2(1236f, 817f),
        new Vector2(1291f, 814f),
        new Vector2(1347f, 801f),
        new Vector2(1396f, 775f),
        new Vector2(1441f, 742f),
        new Vector2(1477f, 703f),
        new Vector2(1504f, 662f),
        new Vector2(1519f, 620f),
        new Vector2(1522f, 580f),
        new Vector2(1509f, 543f),
        new Vector2(1487f, 511f),
        new Vector2(1454f, 476f),
        new Vector2(1423f, 441f),
        new Vector2(1410f, 408f),
        new Vector2(1423f, 382f)
    };

    public const string RightPlacementSlotName = "Slot 9 (Stage 3 Right)";
    public static readonly Vector3 RightPlacementSlotPosition = new Vector3(6.8f,-1.4f,0f);
    private static Sprite cachedRouteSprite;

    public static bool IsStage3(StageDefinition definition) =>
        definition != null && (definition.name == "Stage1_3" || definition.DisplayName == "Stage 3");

    public static Vector3 RouteWorld(Vector2 pixel) =>
        new Vector3((pixel.x - 836f) / PixelsPerUnit, (470.5f - pixel.y) / PixelsPerUnit + .5f, 0f);

    public static Sprite LoadRouteSprite() {
        if (cachedRouteSprite != null) return cachedRouteSprite;
        cachedRouteSprite = Resources.Load<Sprite>(ResourceSpritePath);
        if (cachedRouteSprite != null) return cachedRouteSprite;
        var tex = Resources.Load<Texture2D>(ResourceSpritePath);
        if (tex == null) return null;
        tex.filterMode = FilterMode.Point;
        cachedRouteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(.5f, .5f), PixelsPerUnit);
        return cachedRouteSprite;
    }

    public static void Apply(BattlefieldMap2D map, EnemySpawner spawner) {
        if (map == null) return;
        EnsureRightPlacementSlot(map);
        var root = map.transform;
        var routeTransform = root.Find("GinghamPathSprite");
        if (routeTransform != null) {
            var renderer = routeTransform.GetComponent<SpriteRenderer>();
            var sprite = LoadRouteSprite();
            if (renderer != null && sprite != null) {
                renderer.sprite = sprite;
                routeTransform.localPosition = new Vector3(0f, .5f, 0f);
                routeTransform.localScale = Vector3.one;
            }
        }
        var leftPath = BuildOrUpdatePath(root, LeftDivergingObjectName, LeftDivergingPixels, map.EnemySpawnRoot, null);
        var rightPath = BuildOrUpdatePath(root, RightEmergenceObjectName, RightEmergencePixels, null, map.BaseDestination);
        if (spawner != null) {
            spawner.ConfigureDivergingRoute(leftPath, rightPath, HideDurationSeconds);
            // Include leading escorts, even when the first enemy is not a support.
            spawner.ConfigureMainRouteGroups("stage3_support_01", "stage3_support_02");
        }
        if (map.PlacementSlots != null && map.PlacementSlots.Count > 2 && map.PlacementSlots[2] != null) {
            map.PlacementSlots[2].transform.position = RouteWorld(new Vector2(860.48f, 650.00f));
        }
    }

    private static void EnsureRightPlacementSlot(BattlefieldMap2D map) {
        if(map.PlacementSlots == null || map.PlacementSlots.Count == 0) return;
        var template = map.PlacementSlots[0];
        if(template == null || template.transform.parent == null) return;
        var parent = template.transform.parent;
        var existing = parent.Find(RightPlacementSlotName);
        if(existing != null) {
            map.RegisterPlacementSlot(existing.GetComponent<TowerPlacementSlot>());
            return;
        }
        // Clone the authored plate, collider, layer, anchor and hover view together.
        var slot = Object.Instantiate(template,RightPlacementSlotPosition,template.transform.rotation,parent);
        slot.gameObject.name = RightPlacementSlotName;
        map.RegisterPlacementSlot(slot);
    }

    private static WaypointPath BuildOrUpdatePath(Transform root, string objectName, Vector2[] pixels, Transform firstAnchor, Transform lastAnchor) {
        var pathTransform = root.Find(objectName);
        if (pathTransform == null) {
            pathTransform = new GameObject(objectName).transform;
            pathTransform.SetParent(root, false);
        }
        for (int i = pathTransform.childCount - 1; i >= 0; i--) {
            var child = pathTransform.GetChild(i).gameObject;
            if (Application.isPlaying) Object.Destroy(child);
            else Object.DestroyImmediate(child);
        }
        var waypoints = new Transform[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) {
            Transform point;
            if (i == 0 && firstAnchor != null) {
                point = firstAnchor;
            } else if (i == pixels.Length - 1 && lastAnchor != null) {
                point = lastAnchor;
            } else {
                point = new GameObject("Waypoint " + i.ToString("00")).transform;
                point.SetParent(pathTransform, false);
                point.position = RouteWorld(pixels[i]);
            }
            waypoints[i] = point;
        }
        var path = pathTransform.GetComponent<WaypointPath>() ?? pathTransform.gameObject.AddComponent<WaypointPath>();
        path.Configure(WaypointPlane.XY, waypoints);
        return path;
    }
}
}

