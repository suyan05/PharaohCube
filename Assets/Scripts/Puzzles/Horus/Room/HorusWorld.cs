using UnityEngine;

// 호루스 방 '몸으로 푸는 퍼즐'(H1 거울, H4 돌배, H5 고리)이 함께 쓰는 도우미
// - Prop: 방 안 물건(도트 그림). 보통 조명을 받고(Lit), 막히는 충돌 상자가 있으면 YSortSystem이 발밑 높이로 앞뒤 정렬
//         -> 플레이어가 물건 위로 지나가거나 물건 밑에 깔리지 않음
// - Floor: 바닥에 붙은 그림(물, 별 자리). 이름에 Floor가 들어가서 YSortSystem이 '항상 아래'로 보냄
// - SafeFocus: 연출 카메라가 맵 끝에 있는 대상을 화면 안쪽에 담도록 줌 크기를 정해 줌
public static class HorusWorld
{
    // 호루스 본당 카메라 경계 (HorusRoomMap: 벽 포함 x -1~25, y -9~17)
    private const float MinX = -1f, MaxX = 25f, MinY = -9f, MaxY = 17f;

    public static SpriteRenderer Prop(Transform parent, string objName, Sprite sprite, Vector3 pos, float width, Vector2 solidSize)
    {
        var go = new GameObject(objName);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = HorusFx.Layer;
        sr.sortingOrder = 0;
        float k = sprite != null ? width / Mathf.Max(sprite.bounds.size.x, 0.01f) : 1f;
        go.transform.localScale = new Vector3(k, k, 1f);
        if (solidSize.x > 0f) AddBox(go, solidSize, k, false);
        return sr;
    }

    // 바닥 그림: 원래 순서(order)는 0~3 정도 (500 + order로 바뀜)
    public static SpriteRenderer Floor(Transform parent, string objName, Sprite sprite, Vector3 pos, float width, int order)
    {
        SpriteRenderer sr = Prop(parent, "Floor_" + objName, sprite, pos, width, Vector2.zero);
        sr.sortingOrder = order;
        return sr;
    }

    // 충돌 상자: 그림 아래쪽(발밑)에 붙임. size는 월드 크기
    public static BoxCollider2D AddBox(GameObject go, Vector2 size, float scale, bool trigger)
    {
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = trigger;
        box.size = size / Mathf.Max(scale, 0.001f);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float bottom = sr.sprite.bounds.min.y;
            box.offset = new Vector2(0f, bottom + box.size.y * 0.5f);
        }
        return box;
    }

    // p가 화면 가운데에서 너무 멀어지지 않게 하는 줌 크기 (CameraFollow2D가 맵 경계로 카메라를 막기 때문)
    // wanted보다 크지 않게, p와 가장 가까운 맵 끝까지의 거리로 계산
    public static float SafeZoom(Vector3 p, float wanted)
    {
        Camera cam = Camera.main;
        float aspect = cam != null ? cam.aspect : 16f / 9f;
        float edgeX = Mathf.Min(p.x - MinX, MaxX - p.x);
        float edgeY = Mathf.Min(p.y - MinY, MaxY - p.y);
        // 대상이 화면 가로/세로의 가운데 60% 안에 들어오려면: 화면 반쪽 크기 <= 끝까지 거리 / (1 - 0.6)
        float byX = edgeX / 0.4f / aspect;
        float byY = edgeY / 0.4f;
        return Mathf.Clamp(Mathf.Min(wanted, byX, byY), 2.2f, wanted);
    }

    public static void SafeFocus(Vector3 p, float wanted)
    {
        CineCamera.Focus(p);
        CineCamera.Zoom(SafeZoom(p, wanted));
    }

    // 플레이어 (발밑 위치)
    public static Transform Player()
    {
        return CineActor.Player();
    }

    // 플레이어를 순간 이동 (되돌리기용)
    public static void Teleport(Transform player, Vector3 pos)
    {
        if (player == null) return;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = pos;
            rb.linearVelocity = Vector2.zero;
        }
        player.position = pos;
    }

    public static Vector2Int InputDir()
    {
        float h = Input.GetAxisRaw("Horizontal"), v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) < 0.5f && Mathf.Abs(v) < 0.5f) return Vector2Int.zero;
        return Mathf.Abs(h) >= Mathf.Abs(v) ? new Vector2Int(h > 0f ? 1 : -1, 0) : new Vector2Int(0, v > 0f ? 1 : -1);
    }

    public static void Say(params string[] lines)
    {
        var d = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++) d[i] = new DialogueLine("나", lines[i]);
        DialogueBox.Play(d);
    }
}
