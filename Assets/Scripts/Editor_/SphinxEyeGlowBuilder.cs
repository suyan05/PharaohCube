using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// 스핑크스 눈빛 오브젝트 만들기 (PrologueSphinxBuilder가 부름)
// 눈 위치는 네가 준 Pro_Obj_SphinxHead / _EyeGlow 두 그림의 다른 픽셀로 계산한 값:
//   왼쪽 눈 가운데 (112, 54)px, 오른쪽 눈 (148, 54)px, 각 21 x 5px (256 x 160 캔버스, 위에서부터)
//   -> 바닥 가운데 피벗 기준 유닛: 왼쪽 (-0.48, 3.3), 오른쪽 (0.64, 3.3)
// 머리 그림을 다시 그려서 눈 위치가 바뀌면 아래 숫자만 고치면 됨
public static class SphinxEyeGlowBuilder
{
    private const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    private static readonly Vector2 LeftEye = new Vector2(-0.48f, 3.3f);
    private static readonly Vector2 RightEye = new Vector2(0.64f, 3.3f);
    private static readonly Color Gold = new Color(1f, 0.83f, 0.35f, 0f);

    public static SphinxEyeGlow Create(Transform head, Sprite glowSprite)
    {
        try
        {
            SpriteRenderer headSr = head.GetComponent<SpriteRenderer>();
            int order = headSr != null ? headSr.sortingOrder : 0;
            string layer = headSr != null ? headSr.sortingLayerName : "Default";
            Vector2 mid = (LeftEye + RightEye) * 0.5f;

            var root = new GameObject("EyeGlow");
            root.transform.SetParent(head, false);

            SpriteRenderer overlay = Part(root.transform, "EyeOverlay", Vector2.zero, Vector2.one, layer, order + 1, Color.white);
            overlay.sprite = glowSprite;
            SpriteRenderer haloL = Part(root.transform, "Halo_L", LeftEye, new Vector2(1.8f, 0.8f), layer, order + 2, Gold);
            SpriteRenderer haloR = Part(root.transform, "Halo_R", RightEye, new Vector2(1.8f, 0.8f), layer, order + 2, Gold);
            SpriteRenderer beam = Part(root.transform, "Beam", mid, new Vector2(7f, 0.22f), layer, order + 2, Gold);
            SpriteRenderer aura = Part(root.transform, "Aura", mid - new Vector2(0f, 0.6f), new Vector2(11f, 7f), layer, order - 1, new Color(1f, 0.8f, 0.45f, 0f));

            Light2D light = EyeLight(root.transform, mid);
            return Link(root, overlay, new[] { haloL, haloR }, beam, aura, light);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SphinxEyeGlowBuilder] 눈빛 만들기 오류: {e}");
            return null;
        }
    }

    private static SpriteRenderer Part(Transform parent, string name, Vector2 local, Vector2 scale, string layer, int order, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = new Color(color.r, color.g, color.b, 0f);
        return sr;
    }

    // 금빛 Point Light 2D (값은 SerializedObject로 넣음 -> URP 버전이 달라도 안전)
    private static Light2D EyeLight(Transform parent, Vector2 local)
    {
        var go = new GameObject("EyeLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;

        Light2D light = go.AddComponent<Light2D>();
        var so = new SerializedObject(light);
        SetInt(so, "m_LightType", (int)Light2D.LightType.Point);
        SetFloat(so, "m_PointLightOuterRadius", 5.5f);
        SetFloat(so, "m_PointLightInnerRadius", 0.4f);
        SetFloat(so, "m_Intensity", 0f);
        SerializedProperty color = so.FindProperty("m_Color");
        if (color != null) color.colorValue = new Color(1f, 0.82f, 0.42f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return light;
    }

    private static SphinxEyeGlow Link(GameObject root, SpriteRenderer overlay, SpriteRenderer[] halos, SpriteRenderer beam, SpriteRenderer aura, Light2D light)
    {
        SphinxEyeGlow glow = root.AddComponent<SphinxEyeGlow>();
        var so = new SerializedObject(glow);
        so.FindProperty("eyeOverlay").objectReferenceValue = overlay;
        SerializedProperty arr = so.FindProperty("halos");
        arr.arraySize = halos.Length;
        for (int i = 0; i < halos.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = halos[i];
        so.FindProperty("beam").objectReferenceValue = beam;
        so.FindProperty("aura").objectReferenceValue = aura;
        so.FindProperty("eyeLight").objectReferenceValue = light;
        so.FindProperty("unlitMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
        so.ApplyModifiedPropertiesWithoutUndo();
        return glow;
    }

    private static void SetInt(SerializedObject so, string field, int value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.intValue = value;
        else Debug.LogWarning($"[SphinxEyeGlowBuilder] Light2D에 {field} 칸이 없음 -> EyeLight를 손으로 Point로 바꾸기");
    }

    private static void SetFloat(SerializedObject so, string field, float value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p != null) p.floatValue = value;
        else Debug.LogWarning($"[SphinxEyeGlowBuilder] Light2D에 {field} 칸이 없음 (넘어감)");
    }
}
