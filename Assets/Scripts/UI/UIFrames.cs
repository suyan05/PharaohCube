using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 공용 UI 틀 그림 (도트 파피루스 판, 어두운 판, 말풍선 등) - 아트 칸이 비어 있을 때 대신 쓰는 기본 모양
// - UIFrames.Papyrus / UIFrames.Dark : 9-slice 판 (늘려도 테두리가 안 뭉개짐)
// - UIFrames.Get("UI_Bubble")        : 그냥 그림 한 장
// - UIFrames.Apply(image, UIFrames.Papyrus) : Image에 9-slice로 입힘
// 그림: Resources/Horus/PixelFx/UI_*.png (도트를 4배로 키워 저장한 것, 테두리 32px)
public static class UIFrames
{
    private const string Folder = "Horus/PixelFx/";
    private const float Border = 32f;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Papyrus => Sliced("UI_Papyrus");
    public static Sprite Dark => Sliced("UI_Dark");

    public static Sprite Get(string name)
    {
        if (cache.TryGetValue(name, out Sprite s) && s != null) return s;
        s = Resources.Load<Sprite>(Folder + name);
        if (s == null) Debug.LogWarning($"[UIFrames] 그림 없음: {Folder}{name}");
        cache[name] = s;
        return s;
    }

    // 같은 그림으로 테두리 정보가 들어간 스프라이트를 새로 만듦 (임포트 설정을 손대지 않아도 9-slice가 됨)
    private static Sprite Sliced(string name)
    {
        string key = name + "#sliced";
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
        try
        {
            Sprite src = Get(name);
            if (src == null) return null;
            s = Sprite.Create(src.texture, src.rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * Border);
            cache[key] = s;
            return s;
        }
        catch (Exception e)
        {
            Debug.LogError($"[UIFrames] {name} 9-slice 만들기 오류: {e.Message}");
            return null;
        }
    }

    public static void Apply(Image img, Sprite frame)
    {
        if (img == null || frame == null) return;
        img.sprite = frame;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.preserveAspect = false;
    }
}
