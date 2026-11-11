using System;
using UnityEngine;
using UnityEngine.UI;

// 화면을 기어가는 아펩 (UI) - 그림 한 장을 미는 게 아니라 몸통 마디 수십 개가 길을 따라 움직임
// - 길: 오른쪽 밖 -> 왼쪽 밖으로 가면서 고리를 그림 (파피루스의 똬리 튼 뱀처럼 몸이 스스로 겹침)
// - SetHead(거리): 머리가 길 위 어디까지 왔는지. 몸통 마디는 그 뒤를 일정 간격으로 따라옴
// - 그림: Resources/Horus/Mural/MU_Apep_seg (몸통 한 토막), MU_Apep_head (머리, 오른쪽을 봄)
public class ApepSerpent : MonoBehaviour
{
    private const string ArtPath = "Horus/Mural/MU_Apep_";
    private const int SegCount = 70;
    private const float Spacing = 34f;      // 마디 사이 거리 (화면 px, 1920x1080 기준)
    private const float Thick = 118f;       // 몸통 굵기
    private const int PathPoints = 700;

    private Image[] body;
    private Image head;
    private Vector2[] path;
    private float[] dist;

    public float PathLength => dist != null ? dist[dist.Length - 1] : 0f;
    public float BodyLength => SegCount * Spacing;

    public static ApepSerpent Create(RectTransform parent, Color tint)
    {
        try
        {
            RectTransform root = UIBuild.Rect(parent, "ApepSerpent", Vector2.zero, new Vector2(1920f, 1080f));
            ApepSerpent s = root.gameObject.AddComponent<ApepSerpent>();
            s.BuildPath();
            return s.BuildBody(root, tint) ? s : null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[아펩] 만들기 오류: {e}");
            return null;
        }
    }

    // 고리를 그리며 왼쪽으로 가는 길 (늘어진 사이클로이드): 반지름이 전진 속도보다 커서 몸이 제 몸을 넘어감
    private void BuildPath()
    {
        path = new Vector2[PathPoints];
        dist = new float[PathPoints];
        const float advance = 175f, loopX = 350f, loopY = 250f, turns = 19.5f;
        for (int i = 0; i < PathPoints; i++)
        {
            float u = i / (float)(PathPoints - 1) * turns;
            path[i] = new Vector2(1750f - advance * u + loopX * Mathf.Sin(u), 40f + loopY * Mathf.Cos(u) - 30f * Mathf.Sin(u * 0.37f));
            dist[i] = i == 0 ? 0f : dist[i - 1] + Vector2.Distance(path[i - 1], path[i]);
        }
    }

    // 꼬리부터 만들어서 머리 쪽 마디가 위에 그려지게 함
    private bool BuildBody(RectTransform root, Color tint)
    {
        Sprite seg = Resources.Load<Sprite>(ArtPath + "seg");
        Sprite headArt = Resources.Load<Sprite>(ArtPath + "head");
        if (seg == null || headArt == null)
        {
            Debug.LogWarning($"[아펩] 그림 없음: {ArtPath}seg / {ArtPath}head");
            return false;
        }
        body = new Image[SegCount];
        for (int i = SegCount - 1; i >= 0; i--)
        {
            body[i] = UIBuild.Image(root, "Seg", Vector2.zero, new Vector2(Spacing * 2.1f, Thick), seg, tint);
            body[i].preserveAspect = false;
            body[i].color = tint;
        }
        float k = Thick / seg.rect.height;
        head = UIBuild.Image(root, "Head", Vector2.zero, headArt.rect.size * k, headArt, tint);
        head.preserveAspect = false;
        head.color = tint;
        head.rectTransform.pivot = new Vector2(0.2f, 0.5f); // 목 쪽을 기준으로 돌림
        SetHead(0f);
        return true;
    }

    public void SetHead(float headDist)
    {
        if (body == null || head == null) return;
        Place(head.rectTransform, headDist, 1f);
        head.enabled = headDist > 0f && headDist < PathLength;
        for (int i = 0; i < SegCount; i++)
        {
            float d = headDist - (i + 1) * Spacing;
            float taper = i < SegCount * 0.65f ? 1f : Mathf.Lerp(1f, 0.22f, (i - SegCount * 0.65f) / (SegCount * 0.35f));
            body[i].enabled = d > 0f && d < PathLength;
            if (body[i].enabled) Place(body[i].rectTransform, d, taper);
        }
    }

    private void Place(RectTransform rt, float d, float thickScale)
    {
        Vector2 p = Sample(d, out Vector2 dir);
        rt.anchoredPosition = p;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        rt.localScale = new Vector3(1f, thickScale, 1f);
    }

    // 길 위 거리 d의 위치와 진행 방향 (이분 탐색)
    private Vector2 Sample(float d, out Vector2 dir)
    {
        d = Mathf.Clamp(d, 0f, PathLength - 0.01f);
        int lo = 0, hi = PathPoints - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (dist[mid] <= d) lo = mid; else hi = mid;
        }
        float span = Mathf.Max(0.001f, dist[hi] - dist[lo]);
        dir = (path[hi] - path[lo]).normalized;
        return Vector2.Lerp(path[lo], path[hi], (d - dist[lo]) / span);
    }
}
