using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 태양의 배 모형(히든 라의 불씨) 배치 (RaPolishSetup이 부름)
// 자리: 서쪽 방(일식 석탁 방) 왼쪽 아래 구석 (-14.9, -2.85). 옮기고 싶으면 씬에서 RaSunBoat를 끌어서 옮기면 됨
// 그림: Assets/image/Ra/World/Ra_Obj_SunBoat.png (48x40, 임시 도트), Ra_FX_SoftGlow.png (64x64)
public static class RaSunBoatBuilder
{
    private const string ObjectName = "RaSunBoat";
    private const string SpriteName = "Ra_Obj_SunBoat";
    private static readonly Vector3 Position = new Vector3(-14.9f, -2.85f, 0f);
    private static readonly Vector3 EmberLocal = new Vector3(0f, 0.67f, 0f);
    private static readonly Color EmberColor = new Color(1f, 0.62f, 0.25f);

    public static void Build()
    {
        GameObject old = GameObject.Find(ObjectName);
        if (old != null)
        {
            Debug.Log("[RaSunBoatBuilder] 배 모형이 이미 있어서 건너뜀 (다시 만들려면 RaSunBoat를 지우고 메뉴 다시 누르기)");
            return;
        }

        GameObject go = CreateBody();
        SpriteRenderer body = go.GetComponent<SpriteRenderer>();
        SpriteRenderer glow = CreateGlow(go.transform, body);
        Light2D light = CreateLight(go.transform);

        RaSunBoat boat = go.AddComponent<RaSunBoat>();
        RaPolishSetup.Edit(boat, so =>
        {
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("emberLight").objectReferenceValue = light;
            so.FindProperty("emberGlow").objectReferenceValue = glow;
        });
        RaPolishExtras.ExcludeFromNight(light);
        Undo.RegisterCreatedObjectUndo(go, "태양의 배 모형");
        Debug.Log($"[RaSunBoatBuilder] 태양의 배 모형 배치 {Position} (서쪽 방 구석)");
    }

    private static GameObject CreateBody()
    {
        var go = new GameObject(ObjectName);
        GameObject parent = GameObject.Find("Interactables");
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.transform.position = Position;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrologueAssetFinder.PixelSprite(SpriteName, true);
        if (sr.sprite == null) Debug.LogWarning($"[RaSunBoatBuilder] {SpriteName}.png 없음 -> 그림 없이 만듦");
        CopySorting(sr);

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1.4f, 0.5f);
        box.offset = new Vector2(0f, 0.25f);
        return go;
    }

    // 다른 퍼즐 물건(일식 석탁)과 같은 정렬 레이어
    private static void CopySorting(SpriteRenderer sr)
    {
        GameObject refGo = GameObject.Find("OBJ_007_EclipseTable_P3");
        SpriteRenderer src = refGo != null ? refGo.GetComponent<SpriteRenderer>() : null;
        if (src == null) return;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = src.sortingOrder;
        sr.sharedMaterial = src.sharedMaterial;
    }

    private static SpriteRenderer CreateGlow(Transform parent, SpriteRenderer body)
    {
        var go = new GameObject("EmberGlow");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = EmberLocal;
        go.transform.localScale = Vector3.one * 0.55f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SoftSprite();
        sr.color = new Color(EmberColor.r, EmberColor.g, EmberColor.b, 0f);
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder + 1;
        sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        return sr;
    }

    private static Light2D CreateLight(Transform parent)
    {
        var go = new GameObject("EmberLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = EmberLocal;

        Light2D light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = EmberColor;
        light.pointLightInnerRadius = 0.3f;
        light.pointLightOuterRadius = 3.2f;
        light.intensity = 0f;
        RaPolishExtras.CopyLightLayers(light);
        return light;
    }

    // 부드러운 원 그림 Ra_FX_SoftGlow.png (64x64, PPU 32 = 2칸 -> 크기 0.55배로 씀)
    private static Sprite SoftSprite()
    {
        Sprite sprite = PrologueAssetFinder.PixelSprite("Ra_FX_SoftGlow", false);
        if (sprite == null) Debug.LogWarning("[RaSunBoatBuilder] Ra_FX_SoftGlow.png 없음 -> 배 안 불빛 그림 없이 빛만");
        return sprite;
    }
}
