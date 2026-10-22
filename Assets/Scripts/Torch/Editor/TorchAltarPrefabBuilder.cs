#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 메뉴: Tools/Torch/횃불 제단 프리팹 만들기
// Assets/Prefabs/Common/TorchAltar.prefab 을 만듦 (이미 있으면 덮어쓸지 물어봄)
// 구성: 받침(사암 상자) + 불꽃(주황 원) + 불빛(Light 2D) + 단단한 콜라이더 + TorchAltar
// 그림이 오면 프리팹을 열어 Base / Flame 의 Sprite만 바꾸면 됨 (중앙 홀 요청서 화로 그림)
public static class TorchAltarPrefabBuilder
{
    private const string Folder = "Assets/Prefabs/Common";
    private const string PrefabPath = Folder + "/TorchAltar.prefab";
    private const string ObjectLayer = "Objects";

    [MenuItem("Tools/Torch/횃불 제단 프리팹 만들기")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
            !EditorUtility.DisplayDialog("횃불 제단", $"{PrefabPath} 가 이미 있습니다. 새로 만들어 덮어쓸까요?\n(씬에 놓인 제단은 프리팹 연결이 유지됩니다)", "덮어쓰기", "취소"))
            return;

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(Folder);

        var root = new GameObject("TorchAltar");
        try
        {
            // 받침 (사암 상자 임시 그림)
            var baseGo = new GameObject("Base");
            baseGo.transform.SetParent(root.transform, false);
            baseGo.transform.localScale = new Vector3(0.9f, 1.1f, 1f);
            var baseSr = baseGo.AddComponent<SpriteRenderer>();
            baseSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            baseSr.color = new Color(0.79f, 0.65f, 0.42f); // 사암 #C9A66B
            baseSr.sortingLayerName = ObjectLayer;

            // 불꽃 (주황 원 임시 그림)
            var flameGo = new GameObject("Flame");
            flameGo.transform.SetParent(root.transform, false);
            flameGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            flameGo.transform.localScale = new Vector3(0.5f, 0.6f, 1f);
            var flameSr = flameGo.AddComponent<SpriteRenderer>();
            flameSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            flameSr.color = new Color(1f, 0.54f, 0.17f); // 횃불 불꽃 #FF8A2B
            flameSr.sortingLayerName = ObjectLayer;
            flameSr.sortingOrder = 1;

            // 불빛
            var light = flameGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightOuterRadius = 3.5f;
            light.pointLightInnerRadius = 0.8f;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.82f, 0.48f);

            // 콜라이더 (지나갈 수 없음 + E 감지)
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 1.1f);

            var altar = root.AddComponent<TorchAltar>();
            var so = new SerializedObject(altar);
            so.FindProperty("flameLight").objectReferenceValue = light;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[TorchAltarPrefabBuilder] {PrefabPath} 만듦. Project 창에서 씬으로 끌어다 놓으면 됨 (Base/Flame Sprite는 그림이 오면 교체)", prefab);
        }
        finally
        {
            Object.DestroyImmediate(root); // 씬에 남은 임시 오브젝트 정리
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
